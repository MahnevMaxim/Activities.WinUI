using System;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using Activities.WinUI.Activities;

namespace Activities.WinUI.Design
{
    /// <summary>
    /// Регистрирует кастомный дизайнер для активностей с селектором.
    /// Visual Studio автоматически находит реализации IRegisterMetadata при загрузке сборки;
    /// при перехостинге дизайнера хост вызывает Register() вручную.
    /// </summary>
    public sealed class ActivitiesMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            AddDesigner(builder, typeof(FindElement));
            AddDesigner(builder, typeof(ElementExists));
            AddDesigner(builder, typeof(WaitForElement));
            AddDesigner(builder, typeof(ClickElement));
            AddDesigner(builder, typeof(SetText));
            AddDesigner(builder, typeof(GetText));
            AddDesigner(builder, typeof(GetElements));
            AddDesigner(builder, typeof(HighlightElement));
            MetadataStore.AddAttributeTable(builder.CreateTable());
        }

        private static void AddDesigner(AttributeTableBuilder builder, Type activityType)
        {
            builder.AddCustomAttributes(activityType, new DesignerAttribute(typeof(SelectorActivityDesigner)));
        }
    }
}
