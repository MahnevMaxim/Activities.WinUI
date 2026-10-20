using System;
using System.Activities;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Activities.WinUI.Design;

namespace Activities.WinUI.Activities
{
    [Designer(typeof(SelectorActivityDesigner))]
    public sealed class HighlightElement : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var element = SelectorArgument.Resolve(context, Selector);
            element.SetFocus();
            WindowHighlighter.Flash(element.Element.Current.NativeWindowHandle);
        }
    }

    internal static class WindowHighlighter
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        private const uint FLASHW_ALL = 3;

        public static void Flash(int hwnd)
        {
            if (hwnd == 0) return;
            var info = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = new IntPtr(hwnd),
                dwFlags = FLASHW_ALL,
                uCount = 3,
                dwTimeout = 0
            };
            FlashWindowEx(ref info);
        }
    }
}
