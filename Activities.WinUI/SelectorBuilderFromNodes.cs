using System;
using System.Collections.Generic;
using System.Linq;

namespace Activities.WinUI
{
    /// <summary>
    /// Строит UiPath-совместимый селектор из узлов, захваченных UIXPathLib.GetUiXPath
    /// (узлы передаются в порядке «корень -> лист»).
    /// </summary>
    public static class SelectorBuilderFromNodes
    {
        public static Selector Build(IReadOnlyList<UiaPathNode> rootToLeaf, string processName, SelectorBuildOptions options = null)
        {
            if (rootToLeaf == null) throw new ArgumentNullException(nameof(rootToLeaf));
            options = options ?? new SelectorBuildOptions();

            var nodes = ExcludeDesktopRoot(rootToLeaf);

            var selector = new Selector();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                bool isWindow = i == 0 || string.Equals(node.Tag, "Window", StringComparison.OrdinalIgnoreCase);

                var item = new SelectorItem(isWindow ? "wnd" : "ctrl");

                if (isWindow)
                {
                    if (!string.IsNullOrEmpty(processName))
                        item.SetProperty("app", processName);
                    if (options.IncludeClassName && !string.IsNullOrEmpty(node.ClassName))
                        item.SetProperty("cls", node.ClassName);
                    if (options.IncludeName && !string.IsNullOrEmpty(node.Name))
                        item.SetProperty("title", node.Name);
                }
                else
                {
                    item.SetProperty("role", ToRole(node));

                    string automationId = node.AutomationId;
                    string name = node.Name;

                    if (options.IncludeAutomationId && !string.IsNullOrEmpty(automationId))
                        item.SetProperty("automationid", automationId);
                    else if (options.IncludeName && !string.IsNullOrEmpty(name))
                        item.SetProperty("name", name);
                    else if (node.Position > 0)
                        item.SetProperty("idx", node.Position.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                selector.Items.Add(item);
            }

            return selector;
        }

        private static List<UiaPathNode> ExcludeDesktopRoot(IReadOnlyList<UiaPathNode> rootToLeaf)
        {
            var result = rootToLeaf.ToList();
            // Корень рабочего стола в raw-дереве — Pane с классом "#32769". Селектор должен
            // начинаться с реального верхнего окна (например, Shell_TrayWnd), а не с рабочего стола.
            while (result.Count > 0 && IsDesktopRoot(result[0]))
                result.RemoveAt(0);
            return result;
        }

        private static bool IsDesktopRoot(UiaPathNode node)
        {
            return node != null
                && string.Equals(node.Tag, "Pane", StringComparison.OrdinalIgnoreCase)
                && string.Equals(node.ClassName, "#32769", StringComparison.OrdinalIgnoreCase);
        }

        private static string ToRole(UiaPathNode node)
        {
            if (node == null)
                return "custom";

            ControlTypeResolver.TryResolve(node.Tag, out var controlType);
            if (controlType != null)
                return ControlTypeResolver.ToRoleName(controlType);

            return string.IsNullOrEmpty(node.Tag) ? "custom" : node.Tag.ToLowerInvariant();
        }
    }
}
