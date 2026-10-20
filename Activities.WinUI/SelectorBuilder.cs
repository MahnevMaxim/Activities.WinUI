using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Automation;

namespace Activities.WinUI
{
    /// <summary>Параметры построения селектора из элемента UI.</summary>
    public sealed class SelectorBuildOptions
    {
        public bool IncludeClassName { get; set; } = true;
        public bool IncludeName { get; set; } = true;
        public bool IncludeAutomationId { get; set; } = true;
    }

    /// <summary>Строит UiPath-совместимый селектор из AutomationElement.</summary>
    public static class SelectorBuilder
    {
        public static Selector Build(AutomationElement element)
            => Build(element, new SelectorBuildOptions());

        public static Selector Build(AutomationElement element, SelectorBuildOptions options)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            options = options ?? new SelectorBuildOptions();

            var chain = BuildChain(element);

            var selector = new Selector();
            for (int i = 0; i < chain.Count; i++)
            {
                var el = chain[i];

                // Первый узел цепочки — верхнеуровневое окно (HWND). Для панели задач
                // Shell_TrayWnd имеет ControlType.Pane, а не Window, но его всё равно нужно
                // сериализовать как <wnd>, чтобы селектор корректно резолвился от RootElement.
                bool isWindow = i == 0 || el.Current.ControlType == ControlType.Window;
                var item = new SelectorItem(isWindow ? "wnd" : "ctrl");

                if (isWindow)
                {
                    item.SetProperty("app", GetProcessName(el));
                    if (options.IncludeClassName && !string.IsNullOrEmpty(el.Current.ClassName))
                        item.SetProperty("cls", el.Current.ClassName);
                    if (options.IncludeName && !string.IsNullOrEmpty(el.Current.Name))
                        item.SetProperty("title", el.Current.Name);
                }
                else
                {
                    item.SetProperty("role", ControlTypeResolver.ToRoleName(el.Current.ControlType));
                    if (options.IncludeAutomationId && !string.IsNullOrEmpty(el.Current.AutomationId))
                        item.SetProperty("automationid", el.Current.AutomationId);
                    else if (options.IncludeName && !string.IsNullOrEmpty(el.Current.Name))
                        item.SetProperty("name", el.Current.Name);
                }

                selector.Items.Add(item);
            }

            return selector;
        }

        private static List<AutomationElement> BuildChain(AutomationElement element)
        {
            var chain = new List<AutomationElement>();
            AutomationElement current = element;

            // Не включаем корень рабочего стола: SelectorResolver ищет от RootElement
            // через TreeScope.Descendants, а корень не является своим потомком.
            while (current != null && !IsDesktopRoot(current))
            {
                chain.Add(current);
                if (current.Current.ControlType == ControlType.Window) break;
                current = TreeWalker.ControlViewWalker.GetParent(current);
            }

            chain.Reverse();
            return chain;
        }

        private static bool IsDesktopRoot(AutomationElement element)
            => Automation.Compare(element, AutomationElement.RootElement);

        public static string GetProcessName(AutomationElement element)
        {
            try
            {
                using (var process = Process.GetProcessById(element.Current.ProcessId))
                    return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
