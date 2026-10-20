using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Activities.WinUI.Design;

namespace Activities.WinUI.Activities
{
    [Designer(typeof(SelectorActivityDesigner))]
    public sealed class WaitForElement : CodeActivity<UIElement>
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        public InArgument<int> TimeoutMs { get; set; } = 10000;

        protected override UIElement Execute(CodeActivityContext context)
        {
            string text = SelectorArgument.Get(context, Selector);
            int timeout = TimeoutMs == null ? 10000 : TimeoutMs.Get(context);
            if (timeout < 0) timeout = 0;

            var selector = global::Activities.WinUI.Selector.Parse(text);
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    return new UIElement(SelectorResolver.FindFirst(selector));
                }
                catch (ElementNotFoundException)
                {
                    if (stopwatch.ElapsedMilliseconds >= timeout)
                        throw new ElementNotFoundException($"Element not found within {timeout} ms for selector: {text}");

                    Thread.Sleep(100);
                }
            }
        }
    }
}
