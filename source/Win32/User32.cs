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
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPStruct), In] COPYDATASTRUCT lParam);



#if NETFRAMEWORK
        [DllImport("user32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint message, ChangeFilterAction action, in ChangeFilterStruct pChangeFilterStruct);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, SW_TYPE nCmdShow);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
	    internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        

        [DllImport("user32.dll")]
        internal static extern bool EnableWindow(IntPtr hWnd,bool bEnable);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
	    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	    [DllImport("user32.dll", CharSet = CharSet.Auto)]
	    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
#elif NET
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ChangeWindowMessageFilterEx(nint hWnd, uint message, ChangeFilterAction action, in ChangeFilterStruct pChangeFilterStruct);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ShowWindow(nint hWnd, SW_TYPE nCmdShow);

        [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        internal static partial nint GetForegroundWindow();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetForegroundWindow(nint hWnd);

        [LibraryImport("user32.dll", EntryPoint = "SendMessageA")]
        public static partial int SendMessage(nint hWnd, int msg, int wParam, int lParam);


        

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EnableWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrA")]
        public static partial int GetWindowLong(nint hWnd, int nIndex);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrA")]
        public static partial int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
#endif

        [StructLayout(LayoutKind.Sequential)]
        public class COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        public enum SW_TYPE : int
        {
            SW_HIDE = 0,
            SW_NORMAL = 1,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_RESTORE = 9,
        }
        // https://github.com/XIU2/TileTool/blob/3bca7c8ded1e6422349b0ce4168162f8b24803af/FileDropAdmin.cs#L82
        internal enum ChangeFilterAction : uint
        {
            MSGFLT_RESET,
            MSGFLT_ALLOW,
            MSGFLT_DISALLOW
        }

        internal enum ChangeFilterStatu : uint
        {
            MSGFLTINFO_NONE,
            MSGFLTINFO_ALREADYALLOWED_FORWND,
            MSGFLTINFO_ALREADYDISALLOWED_FORWND,
            MSGFLTINFO_ALLOWED_HIGHER
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ChangeFilterStruct
        {
            public uint CbSize;
            public ChangeFilterStatu ExtStatus;
        }
    }
}
