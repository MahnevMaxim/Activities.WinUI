using System;
using System.Collections.Generic;
using System.Linq;

namespace Activities.WinUI
{
    /// <summary>Одно условие (атрибут) в узле селектора, например name='Notepad'.</summary>
    public sealed class SelectorProperty
    {
        public SelectorProperty() { }

        public SelectorProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>Узел селектора (wnd, ctrl и т.п.) с набором условий.</summary>
    public sealed class SelectorItem
    {
        public SelectorItem() { }

        public SelectorItem(string tag)
        {
            Tag = tag;
        }

        public string Tag { get; set; } = "ctrl";

        public List<SelectorProperty> Properties { get; } = new List<SelectorProperty>();

        public string GetProperty(string name)
        {
            return Properties.FirstOrDefault(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        public void SetProperty(string name, string value)
        {
            var existing = Properties.FirstOrDefault(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                existing.Value = value;
            else
                Properties.Add(new SelectorProperty(name, value));
        }
    }

    /// <summary>Селектор — упорядоченный список узлов, описывающих путь к элементу UI.</summary>
    public sealed class Selector
    {
        public List<SelectorItem> Items { get; } = new List<SelectorItem>();

        public static Selector Parse(string text) => SelectorParser.Parse(text);

        public override string ToString() => SelectorSerializer.Serialize(this);
    }
}
