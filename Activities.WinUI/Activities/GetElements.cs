using System.Activities;
using System.Collections.Generic;
using System.Windows.Automation;

namespace Activities.WinUI.Activities
{
    public sealed class GetElements : CodeActivity<ICollection<UIElement>>
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override ICollection<UIElement> Execute(CodeActivityContext context)
        {
            var selector = global::Activities.WinUI.Selector.Parse(SelectorArgument.Get(context, Selector));
            if (selector.Items.Count == 0) return new List<UIElement>();

            AutomationElement scope;
            SelectorItem lastItem;
            if (selector.Items.Count == 1)
            {
                scope = AutomationElement.RootElement;
                lastItem = selector.Items[0];
            }
            else
            {
                var parentSelector = new Selector();
                for (int i = 0; i < selector.Items.Count - 1; i++)
                    parentSelector.Items.Add(selector.Items[i]);

                scope = SelectorResolver.FindFirst(parentSelector);
                lastItem = selector.Items[selector.Items.Count - 1];
            }

            var result = new List<UIElement>();
            foreach (var element in SelectorResolver.FindAll(scope, lastItem))
                result.Add(new UIElement(element));

            return result;
        }
    }
}
