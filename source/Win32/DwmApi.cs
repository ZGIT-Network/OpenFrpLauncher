using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Win32
{
    internal partial class DwmApi
    {
#if NET
        [LibraryImport("Dwmapi.dll", SetLastError = true)]
        public static partial nint DwmExtendFrameIntoClientArea(nint hwnd, ref Margins margins);
#else
        [DllImport("Dwmapi.dll", SetLastError = true)]
        public static extern IntPtr DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);
#endif



        [StructLayout(LayoutKind.Sequential)]
        public struct Margins
        {
            public int LeftWidth;
            public int RightWidth;
            public int TopHeight;
            public int BottomHeight;
        }


    }
}
