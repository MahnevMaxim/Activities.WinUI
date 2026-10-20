using System;
using System.Linq;
using System.Reflection;
using Activities.WinUI.Activities;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class DesignerRegistrationTests
    {
        private static readonly Type[] ActivityTypes =
        {
            typeof(FindElement),
            typeof(ElementExists),
            typeof(WaitForElement),
            typeof(ClickElement),
            typeof(SetText),
            typeof(GetText),
            typeof(GetElements),
            typeof(HighlightElement),
        };

        [Fact]
        public void AllSelectorActivities_HaveSelectorActivityDesigner()
        {
            foreach (Type activityType in ActivityTypes)
            {
                object designer = activityType
                    .GetCustomAttributes(false)
                    .FirstOrDefault(a => a.GetType().FullName == "System.ComponentModel.DesignerAttribute");

                Assert.True(designer != null, $"Активность {activityType.Name} не имеет DesignerAttribute.");

                string typeName = (string)designer
                    .GetType()
                    .GetProperty("DesignerTypeName", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(designer);

                Assert.Contains("SelectorActivityDesigner", typeName);
            }
        }
    }
}
