using System.Linq;
using Activities.WinUI;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class UiaNodePathParserTests
    {
        private const string Leaf =
            "/Button[LocalizedControlType=\"кнопка\"][ClassName=\"Windows.UI.Core.CoreComponentInputSource\"][Name=\"Пуск\"][AutomationId=\"StartButton\"][x=10][y=1080][width=48][height=48][lx=0][ly=0][position()=1][RuntimeId=\"42.123\"]\n";

        private const string Parent =
            "/Pane[LocalizedControlType=\"панель\"][ClassName=\"Shell_TrayWnd\"][Name=\"Панель задач\"][AutomationId=\"\"][x=0][y=1080][width=1920][height=48][lx=10][ly=10][position()=1][RuntimeId=\"42.1\"]\n";

        [Fact]
        public void Parse_LeafAndParent_ReturnsNodesInSameOrder()
        {
            var nodes = UiaNodePathParser.Parse(Leaf + Parent);

            Assert.Equal(2, nodes.Count);
            Assert.Equal("Button", nodes[0].Tag);
            Assert.Equal("Pane", nodes[1].Tag);
        }

        [Fact]
        public void Parse_ParsesAllAttributes()
        {
            var node = UiaNodePathParser.Parse(Leaf).Single();

            Assert.Equal("кнопка", node.LocalizedControlType);
            Assert.Equal("Windows.UI.Core.CoreComponentInputSource", node.ClassName);
            Assert.Equal("Пуск", node.Name);
            Assert.Equal("StartButton", node.AutomationId);
            Assert.Equal(10, node.X);
            Assert.Equal(1080, node.Y);
            Assert.Equal(48, node.Width);
            Assert.Equal(48, node.Height);
            Assert.Equal(0, node.LocalX);
            Assert.Equal(0, node.LocalY);
            Assert.Equal(1, node.Position);
            Assert.Equal("42.123", node.RuntimeId);
        }

        [Fact]
        public void Parse_EmptyInput_ReturnsEmpty()
        {
            Assert.Empty(UiaNodePathParser.Parse(null));
            Assert.Empty(UiaNodePathParser.Parse(string.Empty));
            Assert.Empty(UiaNodePathParser.Parse("   \r\n"));
        }

        [Fact]
        public void Parse_DecodesXmlEncodedValues()
        {
            string raw =
                "/Edit[LocalizedControlType=\"edit\"][ClassName=\"\"][Name=\"A &amp; B\"][AutomationId=\"C:\\\\folder\\\\x&quot;y\"][x=0][y=0][width=10][height=10][lx=1][ly=1][position()=2][RuntimeId=\"42.7\"]\n";

            var node = UiaNodePathParser.Parse(raw).Single();

            Assert.Equal("A & B", node.Name);
            Assert.Equal("C:\\folder\\x\"y", node.AutomationId);
        }
    }
}
