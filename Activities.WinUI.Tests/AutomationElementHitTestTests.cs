using System;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace Activities.WinUI.Tests
{
    /// <summary>
    /// Тесты надёжного hit-testing: спуск по raw-дереву до самого глубокого
    /// элемента, содержащего точку (подход WinAppDriver UIRecorder).
    /// </summary>
    public class AutomationElementHitTestTests : IDisposable
    {
        private const string WindowAutomationId = "HitTestWindow";
        private const string TextAutomationId = "HitTestText";

        private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _stop = new ManualResetEventSlim(false);
        private readonly Thread _thread;

        public AutomationElementHitTestTests()
        {
            _thread = new Thread(WindowMain);
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Start();

            if (!_ready.Wait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("STA window did not start.");
        }

        private void WindowMain()
        {
            try
            {
                var window = new Window
                {
                    Title = "HitTest " + WindowAutomationId,
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = 60,
                    Top = 60
                };
                AutomationProperties.SetAutomationId(window, WindowAutomationId);

                var text = new TextBlock { Text = "Hit me", Width = 200, Height = 30 };
                AutomationProperties.SetAutomationId(text, TextAutomationId);
                window.Content = text;

                window.Show();
                _ready.Set();

                while (!_stop.Wait(40))
                    DoEvents();

                window.Close();
            }
            catch
            {
                _ready.Set();
            }
        }

        private static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        public void Dispose()
        {
            _stop.Set();
            if (!_thread.Join(TimeSpan.FromSeconds(10)))
                _thread.Abort();
        }

        private static AutomationElement FindElement(string automationId)
        {
            return AutomationElement.RootElement.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
        }

        [Fact]
        public void BoundsContainPoint_InsideElement_ReturnsTrue()
        {
            var text = FindElement(TextAutomationId);
            Assert.NotNull(text);

            var bounds = text.Current.BoundingRectangle;
            var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

            Assert.True(AutomationElementHitTest.BoundsContainPoint(text, center));
            Assert.False(AutomationElementHitTest.BoundsContainPoint(text, new Point(bounds.Left - 5, bounds.Top - 5)));
        }

        [Fact]
        public void FindDeepestElementAtPoint_ReturnsLeafUnderPoint()
        {
            var window = FindElement(WindowAutomationId);
            var text = FindElement(TextAutomationId);
            Assert.NotNull(window);
            Assert.NotNull(text);

            var bounds = text.Current.BoundingRectangle;
            var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

            var result = AutomationElementHitTest.FindDeepestElementAtPoint(window, center);

            Assert.NotNull(result);
            Assert.Equal(TextAutomationId, result.Current.AutomationId);
        }
    }
}
