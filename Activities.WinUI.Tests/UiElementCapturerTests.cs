using Activities.WinUI;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class UiElementCapturerTests
    {
        private sealed class FakeProvider : IUiPathProvider
        {
            private readonly string _result;

            public FakeProvider(string result) => _result = result;

            public void Init() { }
            public void Shutdown() { }
            public string GetUiXPath(int left, int top) => _result;
        }

        private const string Raw =
            "/Button[LocalizedControlType=\"кнопка\"][ClassName=\"X\"][Name=\"Пуск\"][AutomationId=\"StartButton\"][x=10][y=1080][width=48][height=48][lx=0][ly=0][position()=1][RuntimeId=\"42.123\"]\n" +
            "/Pane[LocalizedControlType=\"панель\"][ClassName=\"Shell_TrayWnd\"][Name=\"Панель задач\"][AutomationId=\"\"][x=0][y=1080][width=1920][height=48][lx=10][ly=10][position()=1][RuntimeId=\"42.1\"]\n";

        [Fact]
        public void Capture_ReturnsNormalizedElement()
        {
            var capturer = new UiElementCapturer(new FakeProvider(Raw));

            var captured = capturer.Capture(24, 1104);

            Assert.NotNull(captured);
            Assert.Equal("Button", captured.Leaf.Tag);
            Assert.Equal(2, captured.NodesLeafToRoot.Count);
            Assert.Equal(2, captured.NodesRootToLeaf.Count);
            Assert.Equal("Pane", captured.NodesRootToLeaf[0].Tag);
            Assert.Equal(new System.Windows.Rect(10, 1080, 48, 48), captured.Bounds);
        }

        [Fact]
        public void Capture_EmptyResult_ReturnsNull()
        {
            var capturer = new UiElementCapturer(new FakeProvider(string.Empty));

            Assert.Null(capturer.Capture(0, 0));
        }
    }
}
