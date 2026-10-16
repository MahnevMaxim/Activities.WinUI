using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
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

        private AutomationElement _selectedElement;
        private readonly ObservableCollection<PropertyRow> _properties = new ObservableCollection<PropertyRow>();

        private Window _overlay;
        private Canvas _overlayCanvas;
        private Border _highlightBorder;
        private bool _capturing;
        private bool _updating;

        public MainWindow()
        {
            InitializeComponent();
            ParseArgs(Environment.GetCommandLineArgs().Skip(1).ToArray());
            PropertyList.ItemsSource = _properties;

            if (!string.IsNullOrEmpty(_inputFile) && File.Exists(_inputFile))
                SelectorTextBox.Text = File.ReadAllText(_inputFile).Trim();
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
            EnsureOverlay();
            _capturing = true;
            _highlightBorder.Visibility = Visibility.Visible;
            _overlay.MouseMove += OnOverlayMouseMove;
            _overlay.MouseLeftButtonDown += OnOverlayMouseDown;
            _overlay.KeyDown += OnOverlayKeyDown;

            Hide();
            _overlay.Show();
            _overlay.Activate();
            _overlay.Focus();
        }

        private void OnOverlayMouseMove(object sender, MouseEventArgs e)
        {
            AutomationElement element = ElementFromCursor();
            if (element != null)
                SetHighlightRect(element.Current.BoundingRectangle);
        }

        private void OnOverlayMouseDown(object sender, MouseButtonEventArgs e)
        {
            AutomationElement element = ElementFromCursor();
            StopCapture();

            if (element != null)
                ShowElement(element, populateTree: true);
        }

        private void OnOverlayKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                StopCapture();
        }

        private void StopCapture()
        {
            if (!_capturing) return;
            _capturing = false;
            _overlay.MouseMove -= OnOverlayMouseMove;
            _overlay.MouseLeftButtonDown -= OnOverlayMouseDown;
            _overlay.KeyDown -= OnOverlayKeyDown;
            _overlay.Hide();
            Show();
            Activate();
        }

        private AutomationElement ElementFromCursor()
        {
            if (!NativeMethods.GetCursorPos(out var pt)) return null;
            try
            {
                return AutomationElement.FromPoint(new Point(pt.X, pt.Y));
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Overlay / подсветка

        private void EnsureOverlay()
        {
            if (_overlay != null) return;

            _overlay = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                Topmost = true,
                ShowInTaskbar = false,
                ShowActivated = true,
                Focusable = true,
                Left = SystemParameters.VirtualScreenLeft,
                Top = SystemParameters.VirtualScreenTop,
                Width = SystemParameters.VirtualScreenWidth,
                Height = SystemParameters.VirtualScreenHeight
            };

            _overlayCanvas = new Canvas();
            _highlightBorder = new Border
            {
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)),
                Visibility = Visibility.Collapsed
            };
            _overlayCanvas.Children.Add(_highlightBorder);
            _overlay.Content = _overlayCanvas;
        }

        private void SetHighlightRect(Rect bounds)
        {
            if (_highlightBorder == null) return;

            Point topLeft = new Point(bounds.Left, bounds.Top);
            Point bottomRight = new Point(bounds.Right, bounds.Bottom);

            var source = PresentationSource.FromVisual(_overlay);
            if (source?.CompositionTarget != null)
            {
                var matrix = source.CompositionTarget.TransformFromDevice;
                topLeft = matrix.Transform(topLeft);
                bottomRight = matrix.Transform(bottomRight);
            }

            Canvas.SetLeft(_highlightBorder, topLeft.X - _overlay.Left);
            Canvas.SetTop(_highlightBorder, topLeft.Y - _overlay.Top);
            _highlightBorder.Width = Math.Max(0, bottomRight.X - topLeft.X);
            _highlightBorder.Height = Math.Max(0, bottomRight.Y - topLeft.Y);
            _highlightBorder.Visibility = Visibility.Visible;
        }

        private void HighlightElement(AutomationElement element)
        {
            EnsureOverlay();
            SetHighlightRect(element.Current.BoundingRectangle);
            _overlay.Show();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
            timer.Tick += (s, e) => { timer.Stop(); _overlay.Hide(); };
            timer.Start();
        }

        #endregion

        #region Отображение элемента / свойств

        private void ShowElement(AutomationElement element, bool populateTree)
        {
            _updating = true;
            try
            {
                _selectedElement = element;
                PopulateProperties(element);
                SelectorTextBox.Text = SelectorSerializer.Serialize(SelectorBuilder.Build(element));
                ElementInfoText.Text = Describe(element);
                if (populateTree) PopulateTree(element);
            }
            finally
            {
                _updating = false;
            }
        }

        private string Describe(AutomationElement element)
        {
            var c = element.Current;
            return $"Тип: {c.ControlType.ProgrammaticName}   Имя: \"{c.Name}\"   AutomationId: \"{c.AutomationId}\"   Класс: \"{c.ClassName}\"";
        }

        private void PopulateProperties(AutomationElement element)
        {
            foreach (var row in _properties)
                row.PropertyChanged -= OnPropertyRowChanged;
            _properties.Clear();

            bool isWindow = element.Current.ControlType == ControlType.Window;

            if (!string.IsNullOrEmpty(element.Current.ClassName))
                AddPropertyRow("cls", "ClassName", element.Current.ClassName);

            if (!string.IsNullOrEmpty(element.Current.Name))
                AddPropertyRow(isWindow ? "title" : "name", "Name", element.Current.Name);

            if (!string.IsNullOrEmpty(element.Current.AutomationId))
                AddPropertyRow("automationid", "AutomationId", element.Current.AutomationId);
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
            if (_selectedElement == null) return;

            var options = new SelectorBuildOptions
            {
                IncludeClassName = GetIncluded("cls"),
                IncludeName = GetIncluded("name") || GetIncluded("title"),
                IncludeAutomationId = GetIncluded("automationid")
            };

            _updating = true;
            try
            {
                SelectorTextBox.Text = SelectorSerializer.Serialize(SelectorBuilder.Build(_selectedElement, options));
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

        private void PopulateTree(AutomationElement target)
        {
            ElementTree.Items.Clear();

            var chain = new List<AutomationElement>();
            AutomationElement current = target;
            while (current != null)
            {
                chain.Add(current);
                if (current.Current.ControlType == ControlType.Window) break;
                current = TreeWalker.ControlViewWalker.GetParent(current);
            }
            chain.Reverse();

            var nodes = new List<TreeViewItem>();
            TreeViewItem parent = null;
            foreach (var el in chain)
            {
                var node = CreateTreeNode(el);
                nodes.Add(node);
                if (parent == null) ElementTree.Items.Add(node);
                else parent.Items.Add(node);
                parent = node;
            }

            foreach (var node in nodes.Take(nodes.Count - 1))
                node.IsExpanded = true;

            if (nodes.Count > 0)
            {
                nodes[nodes.Count - 1].IsSelected = true;
                nodes[nodes.Count - 1].BringIntoView();
            }
        }

        private TreeViewItem CreateTreeNode(AutomationElement element)
        {
            var node = new TreeViewItem { Header = HeaderFor(element), Tag = element };

            bool hasChildren = false;
            try { hasChildren = element.FindFirst(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition) != null; }
            catch { hasChildren = false; }

            if (hasChildren)
            {
                node.Items.Add(new TreeViewItem { Header = "…" });
                node.Expanded += OnTreeNodeExpanded;
            }

            return node;
        }

        private void OnTreeNodeExpanded(object sender, RoutedEventArgs e)
        {
            var node = (TreeViewItem)sender;
            node.Expanded -= OnTreeNodeExpanded;
            node.Items.Clear();

            var element = (AutomationElement)node.Tag;
            int count = 0;
            foreach (AutomationElement child in element.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition))
            {
                if (count++ >= 30) break;
                node.Items.Add(CreateTreeNode(child));
            }
        }

        private string HeaderFor(AutomationElement element)
        {
            string type = ControlTypeResolver.ToRoleName(element.Current.ControlType);
            string name = element.Current.Name;
            return string.IsNullOrEmpty(name) ? type : $"{type} \"{name}\"";
        }

        private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is AutomationElement element)
                ShowElement(element, populateTree: false);
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
            try
            {
                var selector = SelectorParser.Parse(SelectorTextBox.Text);
                var element = SelectorResolver.FindFirst(selector);
                StatusText.Text = "Элемент найден: " + Describe(element);
                HighlightElement(element);
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка: " + ex.Message;
                MessageBox.Show(ex.Message, "Проверка селектора", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(SelectorTextBox.Text);
            StatusText.Text = "Селектор скопирован в буфер обмена.";
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_outputFile)) return;

            try
            {
                File.WriteAllText(_outputFile, SelectorTextBox.Text.Trim());
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}

