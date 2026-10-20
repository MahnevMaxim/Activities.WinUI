using System.Collections.Generic;

namespace Activities.WinUI
{
    /// <summary>
    /// Один узел пути, возвращаемого UIXPathLib.GetUiXPath (формат WinAppDriverUIRecorder):
    /// /Tag[LocalizedControlType="..."][ClassName="..."][Name="..."][AutomationId="..."][x=..][y=..][width=..][height=..][lx=..][ly=..][position()=..][RuntimeId="..."]
    /// </summary>
    public sealed class UiaPathNode
    {
        /// <summary>Программное имя ControlType без префикса, например "Button", "ListItem", "Pane".</summary>
        public string Tag { get; set; }

        public string LocalizedControlType { get; set; }

        public string ClassName { get; set; }

        public string Name { get; set; }

        public string AutomationId { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int LocalX { get; set; }

        public int LocalY { get; set; }

        /// <summary>Порядковый номер среди однотипных соседей (1-based), аналог position().</summary>
        public int Position { get; set; }

        public string RuntimeId { get; set; }
    }
}
