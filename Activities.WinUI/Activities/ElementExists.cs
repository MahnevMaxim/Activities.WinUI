using System.Activities;
using System.ComponentModel;
using Activities.WinUI.Design;

namespace Activities.WinUI.Activities
{
    [Designer(typeof(SelectorActivityDesigner))]
    public sealed class ElementExists : CodeActivity<bool>
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override bool Execute(CodeActivityContext context)
        {
            try
            {
                SelectorArgument.Resolve(context, Selector);
                return true;
            }
            catch (ElementNotFoundException)
            {
                return false;
            }
        }
    }
}
