using System;
using System.Collections.Generic;

namespace Activities.WinUI
{
    /// <summary>
    /// Разбор сырой строки, возвращаемой UIXPathLib.GetUiXPath (узлы идут от листа к корню,
    /// каждый узел на отдельной строке). Чистая логика — тестируется без рабочего стола.
    /// </summary>
    public static class UiaNodePathParser
    {
        public static List<UiaPathNode> Parse(string raw)
        {
            var result = new List<UiaPathNode>();
            if (string.IsNullOrEmpty(raw))
                return result;

            foreach (string rawLine in raw.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("/", StringComparison.Ordinal))
                    continue;

                UiaPathNode node = ParseNode(line);
                if (node != null)
                    result.Add(node);
            }

            return result;
        }

        private static UiaPathNode ParseNode(string line)
        {
            int firstBracket = line.IndexOf('[');
            string tag = firstBracket < 0
                ? line.Substring(1).Trim()
                : line.Substring(1, firstBracket - 1).Trim();

            if (tag.Length == 0)
                return null;

            var node = new UiaPathNode { Tag = tag };

            int i = firstBracket;
            while (i >= 0 && i < line.Length)
            {
                if (line[i] != '[')
                {
                    i++;
                    continue;
                }

                int end = FindAttributeEnd(line, i);
                if (end < 0)
                    break;

                string attribute = line.Substring(i + 1, end - i - 1);
                ApplyAttribute(node, attribute);
                i = end + 1;
            }

            return node;
        }

        private static int FindAttributeEnd(string line, int startBracket)
        {
            int p = startBracket + 1;
            if (p < line.Length && line[p] == '"')
            {
                int closeQuote = line.IndexOf('"', p + 1);
                if (closeQuote < 0)
                    return line.IndexOf(']', p);

                return line.IndexOf(']', closeQuote + 1);
            }

            return line.IndexOf(']', p);
        }

        private static void ApplyAttribute(UiaPathNode node, string attribute)
        {
            int eq = attribute.IndexOf('=');
            if (eq <= 0)
                return;

            string key = attribute.Substring(0, eq).Trim();
            string value = attribute.Substring(eq + 1).Trim();

            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                value = DecodeValue(value.Substring(1, value.Length - 2));

            switch (key)
            {
                case "LocalizedControlType": node.LocalizedControlType = value; break;
                case "ClassName": node.ClassName = value; break;
                case "Name": node.Name = value; break;
                case "AutomationId": node.AutomationId = value; break;
                case "x": node.X = ParseInt(value); break;
                case "y": node.Y = ParseInt(value); break;
                case "width": node.Width = ParseInt(value); break;
                case "height": node.Height = ParseInt(value); break;
                case "lx": node.LocalX = ParseInt(value); break;
                case "ly": node.LocalY = ParseInt(value); break;
                case "position()": node.Position = ParseInt(value); break;
                case "RuntimeId": node.RuntimeId = value; break;
            }
        }

        private static int ParseInt(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }

        /// <summary>Обратное преобразование экранирования, которое выполняет UIXPathLib.XmlEncode.</summary>
        internal static string DecodeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;

            // UIXPathLib удваивает обратный слэш, затем кодирует XML-сущности.
            string result = value.Replace("\\\\", "\\");
            result = result.Replace("&quot;", "\"");
            result = result.Replace("&apos;", "'");
            result = result.Replace("&lt;", "<");
            result = result.Replace("&gt;", ">");
            result = result.Replace("&amp;", "&");
            return result;
        }
    }
}
