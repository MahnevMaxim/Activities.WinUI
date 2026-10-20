using System;
using System.Windows;
using System.Windows.Automation;

namespace Activities.WinUI
{
    /// <summary>
    /// Надёжный hit-testing UIAutomation.
    /// Решает проблему панели задач: System.Windows.Automation.AutomationElement.FromPoint
    /// над Shell_TrayWnd возвращает корень рабочего стола, поэтому нужен спуск по
    /// raw-дереву от реального окна под курсором (подход WinAppDriver UIRecorder /
    /// ElementContainingPoint).
    /// </summary>
    public static class AutomationElementHitTest
    {
        /// <summary>Возвращает true, если прямоугольник элемента содержит точку экрана.</summary>
        public static bool BoundsContainPoint(AutomationElement element, Point point)
        {
            if (element == null) return false;

            try
            {
                Rect bounds = element.Current.BoundingRectangle;
                return !bounds.IsEmpty && bounds.Contains(point);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ищет самый глубокий (наименьший по площади) элемент в raw-дереве,
        /// чей BoundingRectangle содержит заданную точку.
        /// </summary>
        public static AutomationElement FindDeepestElementAtPoint(AutomationElement root, Point point)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            AutomationElement best = null;
            double bestArea = double.MaxValue;
            Walk(root, point, ref best, ref bestArea);
            return best;
        }

        private static void Walk(AutomationElement node, Point point, ref AutomationElement best, ref double bestArea)
        {
            Rect bounds;
            try { bounds = node.Current.BoundingRectangle; }
            catch { return; }

            if (bounds.IsEmpty || !bounds.Contains(point))
                return;

            double area = bounds.Width * bounds.Height;
            if (area < bestArea)
            {
                bestArea = area;
                best = node;
            }

            AutomationElement child = TreeWalker.RawViewWalker.GetFirstChild(node);
            while (child != null)
            {
                // Защита от провайдеров, которые возвращают себя же как ребёнка
                // (баг bing-плиток на IE11, зафиксированный в UIRecorder).
                if (!Automation.Compare(child, node))
                    Walk(child, point, ref best, ref bestArea);

                child = TreeWalker.RawViewWalker.GetNextSibling(child);
            }
        }
    }
}
