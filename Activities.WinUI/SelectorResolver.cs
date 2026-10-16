using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Automation;

namespace Activities.WinUI
{
    public sealed class ElementNotFoundException : Exception
    {
        public ElementNotFoundException(string message) : base(message) { }
        public ElementNotFoundException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Поиск элемента(ов) по селектору через UIAutomation.</summary>
    public static class SelectorResolver
    {
        public static AutomationElement FindFirst(Selector selector)
            => FindFirst(AutomationElement.RootElement, selector);

        public static AutomationElement FindFirst(AutomationElement scope, Selector selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            AutomationElement current = scope ?? AutomationElement.RootElement;

            foreach (var item in selector.Items)
            {
                var criterion = SelectorConditions.Build(item);

                AutomationElementCollection matches;
                try
                {
                    matches = current.FindAll(TreeScope.Descendants, criterion.Condition);
                }
                catch (Exception ex)
                {
                    throw new ElementNotFoundException($"Failed to search for element <{item.Tag} .../>.", ex);
                }

                AutomationElement next = SelectMatch(matches, criterion.ProcessName, criterion.Index);
                if (next == null)
                    throw new ElementNotFoundException(
                        $"Element not found for node <{item.Tag} .../> in selector: {selector}");
                current = next;
            }

            return current;
        }

        public static IReadOnlyList<AutomationElement> FindAll(AutomationElement scope, SelectorItem item)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (item == null) throw new ArgumentNullException(nameof(item));

            var criterion = SelectorConditions.Build(item);
            var list = new List<AutomationElement>();
            foreach (AutomationElement el in scope.FindAll(TreeScope.Descendants, criterion.Condition))
            {
                if (string.IsNullOrEmpty(criterion.ProcessName) || ProcessNameMatches(el, criterion.ProcessName))
                    list.Add(el);
            }
            return list;
        }

        private static AutomationElement SelectMatch(AutomationElementCollection matches, string processName, int index)
        {
            int found = 0;
            foreach (AutomationElement el in matches)
            {
                if (!string.IsNullOrEmpty(processName) && !ProcessNameMatches(el, processName))
                    continue;

                found++;
                if (found == index)
                    return el;
            }
            return null;
        }

        private static bool ProcessNameMatches(AutomationElement el, string expected)
        {
            try
            {
                using (var process = Process.GetProcessById(el.Current.ProcessId))
                {
                    string name = process.ProcessName;
                    return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(name + ".exe", expected, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(name, expected + ".exe", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
