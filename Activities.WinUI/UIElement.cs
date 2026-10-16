using System;
using System.Windows.Automation;

namespace Activities.WinUI
{
    /// <summary>Обёртка над AutomationElement с высокоуровневыми операциями.</summary>
    public sealed class UIElement
    {
        public UIElement(AutomationElement element)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
        }

        public AutomationElement Element { get; }

        public string Name => Element.Current.Name;
        public string AutomationId => Element.Current.AutomationId;
        public string ClassName => Element.Current.ClassName;
        public ControlType ControlType => Element.Current.ControlType;
        public int ProcessId => Element.Current.ProcessId;
        public int NativeWindowHandle => Element.Current.NativeWindowHandle;

        public void Click()
        {
            if (Element.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern invoke)
            {
                invoke.Invoke();
                return;
            }

            if (Element.GetCurrentPattern(TogglePattern.Pattern) is TogglePattern toggle)
            {
                toggle.Toggle();
                return;
            }

            if (Element.GetCurrentPattern(ExpandCollapsePattern.Pattern) is ExpandCollapsePattern expandCollapse)
            {
                expandCollapse.Expand();
                return;
            }

            throw new NotSupportedException(
                "Element does not support InvokePattern, TogglePattern or ExpandCollapsePattern.");
        }

        public void SetFocus() => Element.SetFocus();

        public void SetValue(string value)
        {
            if (Element.GetCurrentPattern(ValuePattern.Pattern) is ValuePattern valuePattern)
            {
                valuePattern.SetValue(value ?? string.Empty);
                return;
            }

            throw new NotSupportedException("Element does not support ValuePattern.");
        }

        public string GetText()
        {
            if (Element.GetCurrentPattern(ValuePattern.Pattern) is ValuePattern valuePattern)
                return valuePattern.Current.Value ?? string.Empty;

            if (Element.GetCurrentPattern(TextPattern.Pattern) is TextPattern textPattern)
                return textPattern.DocumentRange.GetText(-1) ?? string.Empty;

            return Element.Current.Name ?? string.Empty;
        }

        public override string ToString() => $"UIElement '{Name}' ({ControlType.ProgrammaticName})";
    }
}
