using Xunit;

namespace Activities.WinUI.Tests
{
    public class SelectorSerializerTests
    {
        [Fact]
        public void Serialize_Empty_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, SelectorSerializer.Serialize(new Selector()));
        }

        [Fact]
        public void Serialize_SingleItem_UiPathStyle()
        {
            var selector = new Selector();
            var item = new SelectorItem("wnd");
            item.SetProperty("app", "notepad.exe");
            item.SetProperty("cls", "Notepad");
            selector.Items.Add(item);

            Assert.Equal("<wnd app='notepad.exe' cls='Notepad' />", SelectorSerializer.Serialize(selector));
        }

        [Fact]
        public void Serialize_EscapesSpecialCharacters()
        {
            var item = new SelectorItem("wnd");
            item.SetProperty("title", "It's & <ok>");
            var selector = new Selector();
            selector.Items.Add(item);

            Assert.Equal("<wnd title='It&apos;s &amp; &lt;ok&gt;' />", SelectorSerializer.Serialize(selector));
        }

        [Fact]
        public void RoundTrip_ParseThenSerialize_IsStable()
        {
            const string text = "<wnd app='notepad.exe' cls='Notepad' />\n<ctrl role='editable' automationid='15' />";

            Assert.Equal(text, SelectorSerializer.Serialize(Selector.Parse(text)));
        }
    }
}
