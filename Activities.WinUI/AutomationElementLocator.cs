using System.Windows.Automation;

namespace Activities.WinUI
{
    /// <summary>Проверка, является ли элемент реальной целью захвата при hit-testing UIAutomation.</summary>
    public static class AutomationElementLocator
    {
        /// <summary>
        /// Цель валидна, если элемент не null, не является корневым элементом рабочего
        /// стола и не принадлежит собственному процессу (окно редактора/подсветки).
        /// </summary>
        public static bool IsTarget(AutomationElement element, int currentProcessId)
        {
            if (element == null) return false;

            // Корень рабочего стола имеет ненулевой ProcessId (владелец — оболочка),
            // поэтому его нельзя отфильтровать по PID: сравниваем по runtime-id.
            if (Automation.Compare(element, AutomationElement.RootElement)) return false;

            return element.Current.ProcessId != currentProcessId;
        }
    }
}
