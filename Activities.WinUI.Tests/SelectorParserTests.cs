using System;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class SelectorParserTests
    {
        [Fact]
        public void Parse_SingleWindow_ReturnsOneItemWithProperties()
        {
            var selector = SelectorParser.Parse("<wnd app='notepad.exe' cls='Notepad' />");

            Assert.Single(selector.Items);
            Assert.Equal("wnd", selector.Items[0].Tag);
            Assert.Equal("notepad.exe", selector.Items[0].GetProperty("app"));
            Assert.Equal("Notepad", selector.Items[0].GetProperty("cls"));
        }

        [Fact]
        public void Parse_MultipleNodes_KeepsOrder()
        {
            var selector = SelectorParser.Parse("<wnd cls='Notepad' />\n<ctrl role='editable' automationid='15' />");

            Assert.Equal(2, selector.Items.Count);
            Assert.Equal("ctrl", selector.Items[1].Tag);
            Assert.Equal("editable", selector.Items[1].GetProperty("role"));
            Assert.Equal("15", selector.Items[1].GetProperty("automationid"));
        }

        [Fact]
        public void Parse_XmlEntities_AreUnescaped()
        {
            var selector = SelectorParser.Parse("<wnd title='It&apos;s &amp; Co' />");
            Assert.Equal("It's & Co", selector.Items[0].GetProperty("title"));
        }

        [Fact]
        public void Parse_NullOrEmpty_Throws()
        {
            Assert.Throws<ArgumentException>(() => SelectorParser.Parse(null));
            Assert.Throws<ArgumentException>(() => SelectorParser.Parse(""));
            Assert.Throws<ArgumentException>(() => SelectorParser.Parse("   "));
        }

        [Fact]
        public void Parse_MalformedXml_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => SelectorParser.Parse("<wnd cls='Notepad'"));
        }

        [Fact]
        public void Parse_NoElements_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => SelectorParser.Parse("<!-- no elements -->"));
        }
    }
}
