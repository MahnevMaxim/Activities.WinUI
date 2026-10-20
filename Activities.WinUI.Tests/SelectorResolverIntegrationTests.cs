using System;
using System.Activities;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Activities.WinUI.Activities;
using Xunit;

namespace Activities.WinUI.Tests
{
    /// <summary>
    /// Интеграционные тесты: реальное WPF-окно на STA-потоке, поиск через UIAutomation.
    /// </summary>
    public class SelectorResolverIntegrationTests : IDisposable
    {
        private const string WindowAutomationId = "TestRootWindow";
        private const string ButtonAutomationId = "TestButton";

        private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _stop = new ManualResetEventSlim(false);
        private readonly Thread _thread;

        public SelectorResolverIntegrationTests()
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
                    Title = "Test " + WindowAutomationId,
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = 50,
                    Top = 50
                };
                AutomationProperties.SetAutomationId(window, WindowAutomationId);

                var button = new Button { Content = "Click me" };
                AutomationProperties.SetAutomationId(button, ButtonAutomationId);
                window.Content = button;

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

        [Fact]
        public void BuildAndFind_Window_RoundTrips()
        {
            var window = FindWindow();
            Assert.NotNull(window);

            var selector = SelectorBuilder.Build(window);
            var found = SelectorResolver.FindFirst(selector);

            Assert.NotNull(found);
            Assert.Equal(WindowAutomationId, found.Current.AutomationId);
        }

        [Fact]
        public void BuildAndFind_Button_InsideWindow_RoundTrips()
        {
            var button = AutomationElement.RootElement.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, ButtonAutomationId));
            Assert.NotNull(button);

            var selector = SelectorBuilder.Build(button);
            var found = SelectorResolver.FindFirst(selector);

            Assert.NotNull(found);
            Assert.Equal(ButtonAutomationId, found.Current.AutomationId);
        }

        [Fact]
        public void FindElementActivity_ReturnsElement()
        {
            var selectorText = SelectorSerializer.Serialize(SelectorBuilder.Build(FindWindow()));

            var activity = new FindElement { Selector = new InArgument<string>(selectorText) };
            var result = WorkflowInvoker.Invoke(activity);

            Assert.NotNull(result);
            Assert.Equal(WindowAutomationId, result.AutomationId);
        }

        [Fact]
        public void Build_Button_StartsWithWindowNode_NotDesktopRoot()
        {
            var button = AutomationElement.RootElement.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, ButtonAutomationId));
            var window = FindWindow();
            Assert.NotNull(button);
            Assert.NotNull(window);

            var selector = SelectorBuilder.Build(button);

            Assert.True(selector.Items.Count >= 2);
            Assert.Equal("wnd", selector.Items[0].Tag);
            Assert.Equal(window.Current.ClassName, selector.Items[0].GetProperty("cls"));
            Assert.Equal("button", selector.Items[selector.Items.Count - 1].GetProperty("role"));
            Assert.Equal(ButtonAutomationId, selector.Items[selector.Items.Count - 1].GetProperty("automationid"));
        }

        private static AutomationElement FindWindow()
        {
            return AutomationElement.RootElement.FindFirst(TreeScope.Children,
                new PropertyCondition(AutomationElement.AutomationIdProperty, WindowAutomationId));
        }
    }
}
