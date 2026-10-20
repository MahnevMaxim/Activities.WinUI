namespace Activities.WinUI
{
    /// <summary>
    /// Абстракция над нативной библиотекой UIXPathLib.dll. Позволяет в будущем заменить
    /// нативный захват на управляемую реализацию без изменения вызывающего кода.
    /// </summary>
    public interface IUiPathProvider
    {
        void Init();

        void Shutdown();

        /// <summary>
        /// Возвращает путь UI-узлов от листа к корню (по одному узлу на строку),
        /// либо null/пустую строку, если под точкой нет захватываемого элемента.
        /// </summary>
        string GetUiXPath(int left, int top);
    }
}
