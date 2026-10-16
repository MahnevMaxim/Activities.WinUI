using System.Activities;

namespace Activities.WinUI.Activities
{
    public sealed class SetText : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        [RequiredArgument]
        public InArgument<string> Text { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            string text = Text == null ? null : Text.Get(context);
            SelectorArgument.Resolve(context, Selector).SetValue(text ?? string.Empty);
        }
    }
}
