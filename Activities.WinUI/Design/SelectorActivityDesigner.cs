using System;
using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Activities.WinUI.Activities;
using Microsoft.Win32;

namespace Activities.WinUI.Design
{
    /// <summary>
    /// Дизайнер активности с кнопкой, запускающей WPF-редактор селекторов (WinUIExplorer).
    /// </summary>
    public sealed class SelectorActivityDesigner : ActivityDesigner
    {
        private readonly TextBlock _preview;

        public SelectorActivityDesigner()
        {
            _preview = new TextBlock
            {
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50)),
                Margin = new Thickness(0, 0, 6, 0)
            };

            var grid = new Grid { Margin = new Thickness(6, 3, 6, 3) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "Селектор:",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 4, 0)
            };

            var button = new Button
            {
                Content = "Выбрать селектор…",
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(8, 0, 0, 0)
            };
            button.Click += OnPickSelector;

            Grid.SetColumn(label, 0);
            Grid.SetColumn(_preview, 1);
            Grid.SetColumn(button, 2);

            grid.Children.Add(label);
            grid.Children.Add(_preview);
            grid.Children.Add(button);

            Content = grid;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            RefreshPreview();
        }

        private void OnPickSelector(object sender, RoutedEventArgs e)
        {
            string current = ReadCurrentSelector();
            string inputFile = Path.GetTempFileName();
            string outputFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(inputFile, current);
                File.WriteAllText(outputFile, string.Empty);

                string explorer = LocateExplorer();
                if (explorer == null)
                {
                    MessageBox.Show(
                        "Не найден WinUIExplorer.exe рядом со сборкой Activities.WinUI.dll. " +
                        "Укажите его вручную.",
                        "Редактор селекторов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var startInfo = new ProcessStartInfo(explorer)
                {
                    Arguments = string.Format("\"{0}\" --output \"{1}\"", inputFile, outputFile),
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(explorer)
                };

                var process = Process.Start(startInfo);
                process?.WaitForExit();

                if (File.Exists(outputFile))
                {
                    string result = File.ReadAllText(outputFile).Trim();
                    if (!string.IsNullOrEmpty(result))
                    {
                        var property = ModelItem?.Properties["Selector"];
                        property?.SetValue(new InArgument<string>(result));
                        RefreshPreview();
                    }
                }
            }
            finally
            {
                TryDelete(inputFile);
                TryDelete(outputFile);
            }
        }

        private void RefreshPreview()
        {
            if (_preview != null)
                _preview.Text = ReadCurrentSelector();
        }

        private string ReadCurrentSelector()
        {
            if (ModelItem == null) return string.Empty;
            ModelProperty property = ModelItem.Properties["Selector"];
            if (property == null) return string.Empty;

            var argument = property.ComputedValue as InArgument<string>;
            if (argument == null && property.Value != null)
                argument = property.Value.GetCurrentValue() as InArgument<string>;

            return SelectorArgument.ToDisplayString(argument);
        }

        private static string LocateExplorer()
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string candidate = Path.Combine(directory, "WinUIExplorer.exe");
            if (File.Exists(candidate)) return candidate;

            var dialog = new OpenFileDialog
            {
                Title = "Укажите WinUIExplorer.exe",
                Filter = "WinUIExplorer.exe|WinUIExplorer.exe"
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* ignore */ }
        }
    }
}
