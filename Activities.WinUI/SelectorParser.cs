using System;
using System.Xml.Linq;

namespace Activities.WinUI
{
    /// <summary>Парсинг UiPath-совместимого XML-селектора.</summary>
    public static class SelectorParser
    {
        public static Selector Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Selector cannot be null or empty.", nameof(text));

            XDocument doc;
            try
            {
                doc = XDocument.Parse("<root>" + text + "</root>");
            }
            catch (Exception ex)
            {
                throw new FormatException("Invalid selector XML: " + ex.Message, ex);
            }

            var selector = new Selector();
            foreach (var element in doc.Root.Elements())
            {
                var item = new SelectorItem(element.Name.LocalName);
                foreach (var attribute in element.Attributes())
                    item.Properties.Add(new SelectorProperty(attribute.Name.LocalName, attribute.Value));
                selector.Items.Add(item);
            }

            if (selector.Items.Count == 0)
                throw new FormatException("Selector does not contain any elements.");

            return selector;
        }
    }
}
