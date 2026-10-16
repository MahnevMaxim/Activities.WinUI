using System.Activities;
using Activities.WinUI.Activities;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class SelectorArgumentTests
    {
        [Fact]
        public void ToDisplayString_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, SelectorArgument.ToDisplayString(null));
        }

        [Fact]
        public void ToDisplayString_Literal_ReturnsValue()
        {
            var argument = new InArgument<string>("<wnd cls='Notepad' />");
            Assert.Equal("<wnd cls='Notepad' />", SelectorArgument.ToDisplayString(argument));
        }
    }
}
