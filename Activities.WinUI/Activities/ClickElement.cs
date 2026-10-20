using System.Activities;
using System.ComponentModel;
using Activities.WinUI.Design;

namespace Activities.WinUI.Activities
{
    [Designer(typeof(SelectorActivityDesigner))]
    public sealed class ClickElement : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override void Execute(CodeActivityContext context)
            => SelectorArgument.Resolve(context, Selector).Click();
    }
}
