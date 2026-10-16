using System.Windows.Automation;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class SelectorConditionsTests
    {
        [Fact]
        public void Build_ClassName_CreatesPropertyCondition()
        {
            var item = new SelectorItem("wnd");
            item.SetProperty("cls", "Notepad");

            var criterion = SelectorConditions.Build(item);

            var pc = Assert.IsType<PropertyCondition>(criterion.Condition);
            Assert.Equal(AutomationElement.ClassNameProperty, pc.Property);
            Assert.Equal("Notepad", pc.Value);
            Assert.Equal(1, criterion.Index);
            Assert.Null(criterion.ProcessName);
        }

        [Fact]
        public void Build_MultipleProperties_CreatesAndCondition()
        {
            var item = new SelectorItem("ctrl");
            item.SetProperty("role", "editable");
            item.SetProperty("automationid", "15");

            var criterion = SelectorConditions.Build(item);

            var and = Assert.IsType<AndCondition>(criterion.Condition);
            Assert.Equal(2, and.GetConditions().Length);
        }

        [Fact]
        public void Build_AppAndIdx_AreCapturedSeparately()
        {
            var item = new SelectorItem("wnd");
            item.SetProperty("app", "notepad.exe");
            item.SetProperty("idx", "3");

            var criterion = SelectorConditions.Build(item);

            Assert.Equal("notepad.exe", criterion.ProcessName);
            Assert.Equal(3, criterion.Index);
            Assert.Equal(Condition.TrueCondition, criterion.Condition);
        }

        [Fact]
        public void Build_Title_MapsToNameProperty()
        {
            var item = new SelectorItem("wnd");
            item.SetProperty("title", "Notepad");

            var criterion = SelectorConditions.Build(item);

            var pc = Assert.IsType<PropertyCondition>(criterion.Condition);
            Assert.Equal(AutomationElement.NameProperty, pc.Property);
            Assert.Equal("Notepad", pc.Value);
        }

        [Fact]
        public void Build_UnknownAttribute_IsIgnored()
        {
            var item = new SelectorItem("wnd");
            item.SetProperty("weird", "x");

            var criterion = SelectorConditions.Build(item);

            Assert.Equal(Condition.TrueCondition, criterion.Condition);
        }

        [Fact]
        public void Build_Role_MapsToControlTypeProperty()
        {
            var item = new SelectorItem("ctrl");
            item.SetProperty("role", "editable");

            var criterion = SelectorConditions.Build(item);

            var pc = Assert.IsType<PropertyCondition>(criterion.Condition);
            Assert.Equal(AutomationElement.ControlTypeProperty, pc.Property);
            Assert.Equal(ControlType.Edit.Id, (int)pc.Value);
        }
    }
}
