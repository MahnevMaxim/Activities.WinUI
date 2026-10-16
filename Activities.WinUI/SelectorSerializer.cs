using System;
using System.Text;

namespace Activities.WinUI
{
    /// <summary>Сериализация селектора в UiPath-совместимый XML.</summary>
    public static class SelectorSerializer
    {
        public static string Serialize(Selector selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            var sb = new StringBuilder();
            for (int i = 0; i < selector.Items.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                AppendItem(sb, selector.Items[i]);
            }
            return sb.ToString();
        }

        private static void AppendItem(StringBuilder sb, SelectorItem item)
        {
            sb.Append('<').Append(item.Tag);
            foreach (var prop in item.Properties)
            {
                sb.Append(' ').Append(prop.Name).Append("='");
                sb.Append(Escape(prop.Value));
                sb.Append('\'');
            }
            sb.Append(" />");
        }

        private static string Escape(string value)
        {
            if (value == null) return string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&apos;");
        }
    }
}
