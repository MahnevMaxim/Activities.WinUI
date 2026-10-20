using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Activities.WinUI;

namespace WinUIExplorer
{
    /// <summary>
    /// Редактор селекторов: захват элемента с экрана, дерево UIA, правка свойств,
    /// проверка/подсветка и возврат селектора в дизайнер активности.
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _inputFile;
        private string _outputFile;

        private readonly NativeUiPathProvider _uiPathProvider = new NativeUiPathProvider();
        private readonly UiCaptureController _captureController;
        private CapturedElement _capturedElement;
        private string _capturedProcessName;
        private Point _lastCapturePoint;
        private bool _hasLastCapturePoint;
        private Point _pendingCapturePoint;

        private readonly ObservableCollection<PropertyRow> _properties = new ObservableCollection<PropertyRow>();

        private bool _capturing;
        private bool _updating;

        private IntPtr _mouseHook;
        private IntPtr _keyboardHook;
        private NativeMethods.LowLevelMouseProc _mouseProc;
        private NativeMethods.LowLevelKeyboardProc _keyboardProc;
        private DateTime _lastHighlightQuery;

        private Window _highlightWindow;
        private Rect _lastHighlightBounds;

        private sealed class ValidationOutcome
        {
            public bool Success { get; set; }
            public string Description { get; set; }
            public string Error { get; set; }
            public Rect Bounds { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            _captureController = new UiCaptureController(_uiPathProvider, a => { Dispatcher.BeginInvoke(a); });
            _captureController.HoverCaptured += OnHoverCaptured;
            _captureController.HoverLost += OnHoverLost;
            _captureController.ElementCaptured += OnElementCaptured;
            try { _uiPathProvider.Init(); }
            catch (Exception ex) { Logger.Error("Не удалось инициализировать UIXPathLib", ex); }
            ParseArgs(Environment.GetCommandLineArgs().Skip(1).ToArray());
            PropertyList.ItemsSource = _properties;

            Logger.Info("=== Сессия WinUIExplorer началась ===");
            if (!string.IsNullOrEmpty(_inputFile))
                Logger.Info("Входной файл селектора: " + _inputFile);
            if (!string.IsNullOrEmpty(_outputFile))
                Logger.Info("Выходной файл селектора: " + _outputFile);

            if (!string.IsNullOrEmpty(_inputFile) && File.Exists(_inputFile))
                SelectorTextBox.Text = File.ReadAllText(_inputFile).Trim();
        }

        protected override void OnClosed(EventArgs e)
        {
            try { _captureController.EndSession(); } catch { }
            try { _uiPathProvider.Shutdown(); } catch { }
            base.OnClosed(e);
        }

        #region Аргументы командной строки

        private void ParseArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--input": if (i + 1 < args.Length) _inputFile = args[++i]; break;
                    case "--output": if (i + 1 < args.Length) _outputFile = args[++i]; break;
                    default:
                        if (i == 0 && !args[i].StartsWith("--")) _inputFile = args[i];
                        break;
                }
            }

            if (!string.IsNullOrEmpty(_outputFile))
                ApplyButton.Visibility = Visibility.Visible;
        }

        #endregion

        #region Захват элемента

        private void OnCapture(object sender, RoutedEventArgs e)
        {
            _capturing = true;
            _captureController.BeginSession();
            Hide();
            InstallHooks();
        }

        private IntPtr OnMouseHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == NativeMethods.WM_MOUSEMOVE)
                {
                    UpdateHighlightAtCursor();
                }
                else if (msg == NativeMethods.WM_LBUTTONDOWN)
                {
                    if (NativeMethods.GetCursorPos(out var pt))
                    {
                        _pendingCapturePoint = new Point(pt.X, pt.Y);
                        _captureController.RequestElementCapture(pt.X, pt.Y);
                    }
                    return new IntPtr(1);
                }
                else if (msg == NativeMethods.WM_RBUTTONDOWN)
                {
                    StopCapture();
                    return new IntPtr(1);
                }
            }
            return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private IntPtr OnKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam.ToInt32() == NativeMethods.WM_KEYDOWN)
            {
                var info = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
                if (info.vkCode == NativeMethods.VK_ESCAPE)
                {
                    StopCapture();
                    return new IntPtr(1);
                }
            }
            return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private void OnHoverCaptured(CapturedElement captured)
        {
            if (!_capturing) return;
            if (captured != null && !captured.Bounds.IsEmpty)
                ShowHighlight(captured.Bounds);
            else
                HideHighlight();
        }

        private void OnHoverLost()
        {
            if (_capturing)
                HideHighlight();
        }

        private void OnElementCaptured(CapturedElement captured)
        {
            if (!_capturing) return;

            string processName = GetProcessNameAtPoint((int)_pendingCapturePoint.X, (int)_pendingCapturePoint.Y);
            StopCapture();

            if (captured != null)
            {
                Logger.Info("Захвачен элемент: " + DescribeNode(captured.Leaf));
                ShowCaptured(captured, processName);
            }
            else
            {
                Logger.Warn("Захват элемента: элемент под курсором не найден.");
            }
        }

        private void InstallHooks()
        {
            IntPtr module = NativeMethods.GetModuleHandle(null);
            _mouseProc = OnMouseHook;
            _keyboardProc = OnKeyboardHook;
            _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, module, 0);
            _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, module, 0);
        }

        private void UninstallHooks()
        {
            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
            if (_keyboardHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
            _mouseProc = null;
            _keyboardProc = null;
        }

        private void StopCapture()
        {
            if (!_capturing) return;
            _capturing = false;

            _captureController.EndSession();
            UninstallHooks();
            HideHighlight();
            Show();
            Activate();
        }

        private void UpdateHighlightAtCursor()
        {
            if ((DateTime.UtcNow - _lastHighlightQuery).TotalMilliseconds < 100)
                return;
            _lastHighlightQuery = DateTime.UtcNow;

            if (!NativeMethods.GetCursorPos(out var pt))
                return;

            var point = new Point(pt.X, pt.Y);
            if (_hasLastCapturePoint
                && Math.Abs(point.X - _lastCapturePoint.X) < 15
                && Math.Abs(point.Y - _lastCapturePoint.Y) < 15)
                return;

            _lastCapturePoint = point;
            _hasLastCapturePoint = true;

            _captureController.RequestHoverCapture(pt.X, pt.Y);
        }

        private string GetProcessNameAtPoint(int x, int y)
        {
            IntPtr hwnd = NativeMethods.WindowFromPoint(x, y);
            if (hwnd == IntPtr.Zero)
                return string.Empty;

            uint pid;
            NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0)
                return string.Empty;

            try { return Process.GetProcessById((int)pid).ProcessName; }
            catch { return string.Empty; }
        }

        #endregion

        #region Подсветка элемента (рамка по краям, FlaUI-style)

        private const double HighlightThickness = 3;
        private const double HighlightBoundsTolerance = 1;

        // Оранжевая рамка (тот же оттенок, что у UiPath UiExplorer для узла дерева — #F29A35).
        private static readonly Color HighlightColor = Color.FromRgb(0xF2, 0x9A, 0x35);

        private void ShowHighlight(Rect bounds)
        {
            if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            {
                HideHighlight();
                return;
            }

            Rect r = ScreenToDips(bounds);

            // Не перерисовываем, если рамка уже стоит на этом же элементе, — иначе
            // при движении курсора внутри одного элемента окно подсветки мигало бы.
            if (_highlightWindow != null && AreBoundsEqual(_lastHighlightBounds, r))
                return;

            EnsureHighlightWindow();

            _highlightWindow.Left = r.Left;
            _highlightWindow.Top = r.Top;
            _highlightWindow.Width = Math.Max(HighlightThickness, r.Width);
            _highlightWindow.Height = Math.Max(HighlightThickness, r.Height);
            _lastHighlightBounds = r;
        }

        private void EnsureHighlightWindow()
        {
            if (_highlightWindow != null)
                return;

            var frame = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(HighlightColor),
                BorderThickness = new Thickness(HighlightThickness)
            };

            _highlightWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Content = frame,
                Topmost = true,
                ShowInTaskbar = false,
                ShowActivated = false,
                Focusable = false,
                ResizeMode = ResizeMode.NoResize,
                IsHitTestVisible = false
            };
            _highlightWindow.Show();

            // Click-through: окно не перехватывает мышь, поэтому AutomationElement.FromPoint
            // под рамкой возвращает реальный элемент (в т.ч. кнопки панели задач).
            var hwnd = new WindowInteropHelper(_highlightWindow).Handle;
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_TRANSPARENT
                     | NativeMethods.WS_EX_TOOLWINDOW
                     | NativeMethods.WS_EX_TOPMOST
                     | NativeMethods.WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
        }

        private void HideHighlight()
        {
            if (_highlightWindow != null)
            {
                try { _highlightWindow.Close(); } catch { /* ignore */ }
                _highlightWindow = null;
            }
            _lastHighlightBounds = Rect.Empty;
        }

        private static bool AreBoundsEqual(Rect a, Rect b)
        {
            return Math.Abs(a.Left - b.Left) <= HighlightBoundsTolerance
                && Math.Abs(a.Top - b.Top) <= HighlightBoundsTolerance
                && Math.Abs(a.Width - b.Width) <= HighlightBoundsTolerance
                && Math.Abs(a.Height - b.Height) <= HighlightBoundsTolerance;
        }

        private Rect ScreenToDips(Rect bounds)
        {
            var source = PresentationSource.FromVisual(this);
            var matrix = source?.CompositionTarget?.TransformFromDevice;
            if (matrix == null) return bounds;

            var topLeft = matrix.Value.Transform(new Point(bounds.Left, bounds.Top));
            var bottomRight = matrix.Value.Transform(new Point(bounds.Right, bounds.Bottom));
            return new Rect(topLeft, bottomRight);
        }

        private void HighlightElement(AutomationElement element)
            => HighlightBounds(element.Current.BoundingRectangle);

        private void HighlightBounds(Rect bounds)
        {
            ShowHighlight(bounds);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
            timer.Tick += (s, e) => { timer.Stop(); HideHighlight(); };
            timer.Start();
        }

        #endregion

        #region Отображение элемента / свойств

        private void ShowCaptured(CapturedElement captured, string processName)
        {
            _updating = true;
            try
            {
                _capturedElement = captured;
                _capturedProcessName = processName;
                PopulatePropertiesFromNode(captured.Leaf);
                SelectorTextBox.Text = SelectorSerializer.Serialize(SelectorBuilderFromNodes.Build(captured.NodesRootToLeaf, processName));
                ElementInfoText.Text = DescribeNode(captured.Leaf);
                PopulateTreeFromNodes(captured.NodesRootToLeaf);
            }
            finally
            {
                _updating = false;
            }
        }

        private string DescribeNode(UiaPathNode node)
        {
            if (node == null) return string.Empty;
            return $"Тип: {node.Tag}   Имя: \"{node.Name}\"   AutomationId: \"{node.AutomationId}\"   Класс: \"{node.ClassName}\"";
        }

        private string Describe(AutomationElement element)
        {
            var c = element.Current;
            return $"Тип: {c.ControlType.ProgrammaticName}   Имя: \"{c.Name}\"   AutomationId: \"{c.AutomationId}\"   Класс: \"{c.ClassName}\"";
        }

        private void PopulatePropertiesFromNode(UiaPathNode node)
        {
            foreach (var row in _properties)
                row.PropertyChanged -= OnPropertyRowChanged;
            _properties.Clear();

            if (node == null)
                return;

            bool isWindow = string.Equals(node.Tag, "Window", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(node.ClassName))
                AddPropertyRow("cls", "ClassName", node.ClassName);

            if (!string.IsNullOrEmpty(node.Name))
                AddPropertyRow(isWindow ? "title" : "name", "Name", node.Name);

            if (!string.IsNullOrEmpty(node.AutomationId))
                AddPropertyRow("automationid", "AutomationId", node.AutomationId);
        }

        private void AddPropertyRow(string attribute, string displayName, string value)
        {
            var row = new PropertyRow { Attribute = attribute, DisplayName = displayName, Value = value, IsIncluded = true };
            row.PropertyChanged += OnPropertyRowChanged;
            _properties.Add(row);
        }

        private void OnPropertyRowChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_updating || e.PropertyName != nameof(PropertyRow.IsIncluded)) return;
            RebuildSelectorFromOptions();
        }

        private void RebuildSelectorFromOptions()
        {
            if (_capturedElement == null) return;

            var options = new SelectorBuildOptions
            {
                IncludeClassName = GetIncluded("cls"),
                IncludeName = GetIncluded("name") || GetIncluded("title"),
                IncludeAutomationId = GetIncluded("automationid")
            };

            _updating = true;
            try
            {
                SelectorTextBox.Text = SelectorSerializer.Serialize(
                    SelectorBuilderFromNodes.Build(_capturedElement.NodesRootToLeaf, _capturedProcessName, options));
            }
            finally
            {
                _updating = false;
            }
        }

        private bool GetIncluded(string attribute)
        {
            return _properties.FirstOrDefault(p => p.Attribute == attribute)?.IsIncluded ?? false;
        }

        #endregion

        #region Дерево элементов

        private void PopulateTreeFromNodes(IReadOnlyList<UiaPathNode> rootToLeaf)
        {
            ElementTree.Items.Clear();

            TreeViewItem parent = null;
            foreach (var node in rootToLeaf)
            {
                var item = new TreeViewItem { Header = HeaderFor(node), Tag = node };
                if (parent == null) ElementTree.Items.Add(item);
                else parent.Items.Add(item);
                parent = item;
            }

            if (parent != null)
            {
                parent.IsSelected = true;
                parent.BringIntoView();
            }
        }

        private string HeaderFor(UiaPathNode node)
        {
            string role = ControlTypeResolver.TryResolve(node.Tag, out var controlType)
                ? ControlTypeResolver.ToRoleName(controlType)
                : (node.Tag ?? "custom").ToLowerInvariant();

            return string.IsNullOrEmpty(node.Name) ? role : role + " \"" + node.Name + "\"";
        }

        private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is UiaPathNode node)
            {
                ElementInfoText.Text = DescribeNode(node);
                ShowHighlight(new Rect(node.X, node.Y, node.Width, node.Height));
            }
        }

        #endregion

        #region Действия

        private void OnSelectorTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_updating) return;
            try
            {
                SelectorParser.Parse(SelectorTextBox.Text);
                ValidationText.Text = "✓ селектор валиден";
                ValidationText.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                ValidationText.Text = "✗ " + ex.Message;
                ValidationText.Foreground = Brushes.Red;
            }
        }

        private void OnValidate(object sender, RoutedEventArgs e)
        {
            Selector selector;
            try
            {
                selector = SelectorParser.Parse(SelectorTextBox.Text);
            }
            catch (Exception ex)
            {
                Logger.Error("Проверка селектора: ошибка парсинга. " + ex.Message);
                StatusText.Text = "Ошибка: " + ex.Message;
                MessageBox.Show(ex.Message, "Проверка селектора", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectorText = SelectorTextBox.Text;
            Logger.Info("Проверка селектора: " + selectorText);

            ValidateButton.IsEnabled = false;
            StatusText.Text = "Проверка...";

            Task.Run(() =>
            {
                try
                {
                    var element = SelectorResolver.FindFirst(selector);
                    return new ValidationOutcome
                    {
                        Success = true,
                        Description = Describe(element),
                        Bounds = element.Current.BoundingRectangle
                    };
                }
                catch (Exception ex)
                {
                    return new ValidationOutcome { Success = false, Error = ex.Message };
                }
            }).ContinueWith(t =>
            {
                if (Dispatcher.HasShutdownStarted)
                    return;

                ValidateButton.IsEnabled = true;
                var outcome = t.Result;
                if (outcome.Success)
                {
                    Logger.Info("Проверка селектора: элемент найден — " + outcome.Description);
                    StatusText.Text = "Элемент найден: " + outcome.Description;
                    HighlightBounds(outcome.Bounds);
                }
                else
                {
                    Logger.Error("Проверка селектора не удалась: " + outcome.Error);
                    StatusText.Text = "Ошибка: " + outcome.Error;
                    MessageBox.Show(outcome.Error, "Проверка селектора", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(SelectorTextBox.Text);
            StatusText.Text = "Селектор скопирован в буфер обмена.";
        }

        private void OnOpenLog(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Пользователь открыл файл лога.");
                Process.Start(new ProcessStartInfo(Logger.FilePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось открыть файл лога.", ex);
                MessageBox.Show("Не удалось открыть файл лога: " + ex.Message, "Лог", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_outputFile)) return;

            try
            {
                File.WriteAllText(_outputFile, SelectorTextBox.Text.Trim());
                Logger.Info("Селектор записан в выходной файл: " + _outputFile);
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось записать выходной файл.", ex);
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}

