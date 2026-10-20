using System.Diagnostics;
using System.Windows.Automation;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class AutomationElementLocatorTests
    {
        [Fact]
        public void IsTarget_Null_ReturnsFalse()
        {
            Assert.False(AutomationElementLocator.IsTarget(null, 1234));
        }

        [Fact]
        public void IsTarget_DesktopRoot_ReturnsFalse()
        {
            // Регрессия: AutomationElement.FromPoint над панелью задач возвращает
            // корневой элемент рабочего стола. У него ненулевой ProcessId
            // (владелец — оболочка), поэтому фильтр только по PID его пропускал,
            // из-за чего панель задач не захватывалась.
            var root = AutomationElement.RootElement;

            Assert.NotEqual(0, root.Current.ProcessId); // sanity: у корня PID != 0
            Assert.False(AutomationElementLocator.IsTarget(root, Process.GetCurrentProcess().Id));
        }
    }
}
