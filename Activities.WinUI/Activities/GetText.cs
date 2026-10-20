using System.Activities;
using System.ComponentModel;
using Activities.WinUI.Design;

namespace Activities.WinUI.Activities
{
    [Designer(typeof(SelectorActivityDesigner))]
    public sealed class GetText : CodeActivity<string>
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override string Execute(CodeActivityContext context)
            => SelectorArgument.Resolve(context, Selector).GetText();
    }
}
