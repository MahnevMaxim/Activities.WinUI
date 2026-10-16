using System.Activities;

namespace Activities.WinUI.Activities
{
    public sealed class GetText : CodeActivity<string>
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override string Execute(CodeActivityContext context)
            => SelectorArgument.Resolve(context, Selector).GetText();
    }
}
