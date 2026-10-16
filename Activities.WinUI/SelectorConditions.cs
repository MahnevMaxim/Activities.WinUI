using System;
using System.Collections.Generic;
using System.Windows.Automation;

namespace Activities.WinUI
{
    /// <summary>Результат разбора одного узла селектора в критерии поиска.</summary>
    public sealed class SelectorCriterion
    {
        public Condition Condition { get; set; } = Condition.TrueCondition;
        public string ProcessName { get; set; }
        public int Index { get; set; } = 1;
    }

    /// <summary>Чистая логика преобразования узла селектора в UIA-условия (тестируется без рабочего стола).</summary>
    public static class SelectorConditions
    {
        public static SelectorCriterion Build(SelectorItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var conditions = new List<Condition>();
            string processName = null;
            int index = 1;

            foreach (var prop in item.Properties)
            {
                if (string.IsNullOrEmpty(prop.Name)) continue;

                switch (prop.Name.ToLowerInvariant())
                {
                    case "app":
                        processName = prop.Value;
                        break;

                    case "idx":
                        if (!int.TryParse(prop.Value, out index) || index < 1)
                            index = 1;
                        break;

                    case "cls":
                    case "class":
                    case "classname":
                        conditions.Add(new PropertyCondition(AutomationElement.ClassNameProperty, prop.Value));
                        break;

                    case "title":
                    case "name":
                        conditions.Add(new PropertyCondition(AutomationElement.NameProperty, prop.Value));
                        break;

                    case "automationid":
                    case "ctrlid":
                    case "id":
                        conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, prop.Value));
                        break;

                    case "role":
                        if (ControlTypeResolver.TryResolve(prop.Value, out var controlType))
                            conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
                        break;

                    case "processid":
                    case "pid":
                        if (int.TryParse(prop.Value, out var pid))
                            conditions.Add(new PropertyCondition(AutomationElement.ProcessIdProperty, pid));
                        break;

                    case "hwnd":
                    case "nativewindowhandle":
                        if (int.TryParse(prop.Value, out var hwnd))
                            conditions.Add(new PropertyCondition(AutomationElement.NativeWindowHandleProperty, hwnd));
                        break;

                    default:
                        // Неизвестный атрибут игнорируется для совместимости.
                        break;
                }
            }

            Condition combined;
            if (conditions.Count == 0)
                combined = Condition.TrueCondition;
            else if (conditions.Count == 1)
                combined = conditions[0];
            else
                combined = new AndCondition(conditions.ToArray());

            return new SelectorCriterion { Condition = combined, ProcessName = processName, Index = index };
        }
    }
}
