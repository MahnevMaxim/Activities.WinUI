using System;
using System.Activities;
using System.Activities.Expressions;

namespace Activities.WinUI.Activities
{
    public static class SelectorArgument
    {
        public static string Get(CodeActivityContext context, InArgument<string> selector)
        {
            if (selector == null)
                throw new InvalidOperationException("The Selector argument is required.");

            string text = selector.Get(context);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("The Selector argument is required.");

            return text;
        }

        public static UIElement Resolve(CodeActivityContext context, InArgument<string> selector)
        {
            return new UIElement(SelectorResolver.FindFirst(global::Activities.WinUI.Selector.Parse(Get(context, selector))));
        }

        /// <summary>Возвращает читаемое значение селектора для предпросмотра в дизайнере.</summary>
        public static string ToDisplayString(InArgument<string> argument)
        {
            if (argument == null) return string.Empty;

            if (argument.Expression is Literal<string> literal)
                return literal.Value ?? string.Empty;

            return argument.Expression?.ToString() ?? string.Empty;
        }
    }
}
