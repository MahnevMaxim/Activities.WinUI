using System.Activities;
using System.ComponentModel;
using Activities.WinUI.Design;

namespace Activities.WinUI.Activities
{
    [Designer(typeof(SelectorActivityDesigner))]
    public sealed class FindElement : CodeActivity<UIElement>
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override UIElement Execute(CodeActivityContext context)
            => SelectorArgument.Resolve(context, Selector);
    }
}
