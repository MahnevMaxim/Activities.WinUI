using Activities.WinUI;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class SelectorBuilderFromNodesTests
    {
        [Fact]
        public void Build_TaskbarWindowAndButton_ProducesWndAndCtrl()
        {
            var nodes = new[]
            {
                new UiaPathNode { Tag = "Pane", ClassName = "Shell_TrayWnd", Name = "Панель задач", Position = 1 },
                new UiaPathNode { Tag = "Button", ClassName = "Windows.UI.Core.CoreComponentInputSource", Name = "Пуск", AutomationId = "StartButton", Position = 1 },
            };

            var selector = SelectorBuilderFromNodes.Build(nodes, "explorer");

            Assert.Equal(2, selector.Items.Count);

            Assert.Equal("wnd", selector.Items[0].Tag);
            Assert.Equal("explorer", selector.Items[0].GetProperty("app"));
            Assert.Equal("Shell_TrayWnd", selector.Items[0].GetProperty("cls"));
            Assert.Equal("Панель задач", selector.Items[0].GetProperty("title"));

            Assert.Equal("ctrl", selector.Items[1].Tag);
            Assert.Equal("button", selector.Items[1].GetProperty("role"));
            Assert.Equal("StartButton", selector.Items[1].GetProperty("automationid"));
        }

        [Fact]
        public void Build_DesktopRootIsSkipped()
        {
            var nodes = new[]
            {
                new UiaPathNode { Tag = "Pane", ClassName = "#32769", Name = "Desktop", Position = 1 },
                new UiaPathNode { Tag = "Pane", ClassName = "Shell_TrayWnd", Name = "Панель задач", Position = 1 },
                new UiaPathNode { Tag = "Button", Name = "Пуск", AutomationId = "StartButton", Position = 1 },
            };

            var selector = SelectorBuilderFromNodes.Build(nodes, "explorer");

            Assert.Equal(2, selector.Items.Count);
            Assert.Equal("wnd", selector.Items[0].Tag);
            Assert.Equal("Shell_TrayWnd", selector.Items[0].GetProperty("cls"));
        }

        [Fact]
        public void Build_CtrlFallsBackToNameThenIndex()
        {
            var withName = new[]
            {
                new UiaPathNode { Tag = "Window", ClassName = "Notepad", Name = "Untitled", Position = 1 },
                new UiaPathNode { Tag = "ListItem", Name = "Item 1", AutomationId = "", Position = 3 },
            };

            var selector = SelectorBuilderFromNodes.Build(withName, "notepad");
            Assert.Equal("listitem", selector.Items[1].GetProperty("role"));
            Assert.Equal("Item 1", selector.Items[1].GetProperty("name"));
            Assert.Null(selector.Items[1].GetProperty("automationid"));

            var withoutName = new[]
            {
                new UiaPathNode { Tag = "Window", ClassName = "Notepad", Name = "Untitled", Position = 1 },
                new UiaPathNode { Tag = "ListItem", Name = "", AutomationId = "", Position = 3 },
            };

            var selector2 = SelectorBuilderFromNodes.Build(withoutName, "notepad");
            Assert.Equal("3", selector2.Items[1].GetProperty("idx"));
        }

        [Fact]
        public void Build_RoleMapping_ListItem()
        {
            var nodes = new[]
            {
                new UiaPathNode { Tag = "Window", ClassName = "Shell_TrayWnd", Name = "Панель задач", Position = 1 },
                new UiaPathNode { Tag = "ListItem", Name = "Google Chrome", AutomationId = "", Position = 2 },
            };

            var selector = SelectorBuilderFromNodes.Build(nodes, "explorer");

            Assert.Equal("listitem", selector.Items[1].GetProperty("role"));
        }
    }
}
