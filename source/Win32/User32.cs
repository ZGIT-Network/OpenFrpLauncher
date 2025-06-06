using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Win32
{
    public static partial class User32
    {
#if NETFRAMEWORK
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, SW_TYPE nCmdShow);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
	    internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool EnableWindow(IntPtr hWnd,bool bEnable);
#elif NET
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ShowWindow(IntPtr hWnd, SW_TYPE nCmdShow);

        [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        internal static partial IntPtr GetForegroundWindow();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EnableWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);
#endif

        public enum SW_TYPE : int
        {
            SW_HIDE = 0,
            SW_NORMAL = 1,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_RESTORE = 9,
        }
    }
}
