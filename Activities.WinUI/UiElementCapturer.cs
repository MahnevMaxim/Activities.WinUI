using System;
using System.Collections.Generic;
using System.Windows;

namespace Activities.WinUI
{
    /// <summary>Результат захвата элемента в точке экрана.</summary>
    public sealed class CapturedElement
    {
        public CapturedElement(string rawPath, IReadOnlyList<UiaPathNode> leafToRoot, IReadOnlyList<UiaPathNode> rootToLeaf)
        {
            RawPath = rawPath;
            NodesLeafToRoot = leafToRoot;
            NodesRootToLeaf = rootToLeaf;
        }

        /// <summary>Сырой результат GetUiXPath (узлы от листа к корню).</summary>
        public string RawPath { get; }

        public IReadOnlyList<UiaPathNode> NodesLeafToRoot { get; }

        public IReadOnlyList<UiaPathNode> NodesRootToLeaf { get; }

        public UiaPathNode Leaf => NodesLeafToRoot != null && NodesLeafToRoot.Count > 0 ? NodesLeafToRoot[0] : null;

        /// <summary>Границы листового элемента по данным узла (x, y, width, height).</summary>
        public Rect Bounds
        {
            get
            {
                var leaf = Leaf;
                if (leaf == null)
                    return Rect.Empty;

                return new Rect(leaf.X, leaf.Y, leaf.Width, leaf.Height);
            }
        }
    }

    /// <summary>
    /// Оркестратор захвата: запрашивает сырой путь у IUiPathProvider, разбирает узлы
    /// и отдаёт нормализованный CapturedElement. Не зависит от Win32/рабочего стола.
    /// </summary>
    public sealed class UiElementCapturer
    {
        private readonly IUiPathProvider _provider;

        public UiElementCapturer(IUiPathProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public CapturedElement Capture(int left, int top)
        {
            string raw = _provider.GetUiXPath(left, top);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            List<UiaPathNode> leafToRoot = UiaNodePathParser.Parse(raw);
            if (leafToRoot == null || leafToRoot.Count == 0)
                return null;

            var rootToLeaf = new List<UiaPathNode>(leafToRoot);
            rootToLeaf.Reverse();

            return new CapturedElement(raw, leafToRoot, rootToLeaf);
        }
    }
}
