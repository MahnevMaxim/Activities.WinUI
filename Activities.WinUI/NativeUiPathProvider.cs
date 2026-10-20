using System.Runtime.InteropServices;
using System.Text;

namespace Activities.WinUI
{
    /// <summary>
    /// P/Invoke-обёртка над нативным UIXPathLib.dll (подход WinAppDriverUIRecorder).
    /// Двоичный файл поставляется в проекте; управляемая замена появится позже.
    /// </summary>
    public sealed class NativeUiPathProvider : IUiPathProvider
    {
        private const int BufferSize = 4096 * 4; // должно совпадать с BUFFERSIZE в UiTreeWalk.h

        public void Init()
        {
            InitUiTreeWalk();
        }

        public void Shutdown()
        {
            UnInitUiTreeWalk();
        }

        public string GetUiXPath(int left, int top)
        {
            var sb = new StringBuilder(BufferSize);
            GetUiXPathNative(left, top, sb, BufferSize);
            return sb.ToString();
        }

        [DllImport("UIXPathLib.dll", EntryPoint = "GetUiXPath", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private static extern int GetUiXPathNative(int left, int top, StringBuilder sb, int nMaxCount);

        [DllImport("UIXPathLib.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void InitUiTreeWalk();

        [DllImport("UIXPathLib.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void UnInitUiTreeWalk();
    }
}
