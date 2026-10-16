using System.Activities;

namespace Activities.WinUI.Activities
{
    public sealed class ClickElement : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override void Execute(CodeActivityContext context)
            => SelectorArgument.Resolve(context, Selector).Click();
    }
}
