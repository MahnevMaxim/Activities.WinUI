using System.Activities;

namespace Activities.WinUI.Activities
{
    public sealed class FindElement : CodeActivity<UIElement>
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override UIElement Execute(CodeActivityContext context)
            => SelectorArgument.Resolve(context, Selector);
    }
}
