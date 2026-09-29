// MuroSOC - NativeMethods
using System;
using System.Runtime.InteropServices;

namespace MuroSoc
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForSystem();
    }
}
