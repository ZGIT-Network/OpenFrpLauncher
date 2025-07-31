using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Interop;

namespace OpenFrp.Launcher
{
    public abstract partial class AppWindow : System.Windows.Window
    {
        private IntPtr hWnd = IntPtr.Zero;

        public AppWindow()
        {

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            SetBinding(iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty, new Binding
            {
                Source = App.Settings,
                Path = new PropertyPath(nameof(App.Settings.ApplicationTheme)),
                Mode = BindingMode.OneWay
            });
            iNKORE.UI.WPF.Modern.Controls.Helpers.WindowHelper.SetSystemBackdropType(this, App.Settings.BackdropType);

            hWnd = new WindowInteropHelper(this).EnsureHandle();

            if (hWnd != IntPtr.Zero)
            {
                if (App.IsAdministrator())
                {
                    var status = new Win32.User32.ChangeFilterStruct() { CbSize = 8 };

                    if (!Win32.User32.ChangeWindowMessageFilterEx(hWnd, 0x004A,action: Win32.User32.ChangeFilterAction.MSGFLT_ALLOW,in status))
                    {

                    }
                }
                HwndSource.FromHwnd(hWnd).AddHook(WndProc);
            }
            base.OnSourceInitialized(e);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x004A:
                    {
                        var copyData = Marshal.PtrToStructure<Win32.User32.COPYDATASTRUCT>(lParam);

                        if (copyData is not null) 
                        {
                            switch (copyData.dwData)
                            {
                   
                                case (nint)0x01 when copyData.cbData > 0:
                                    {
                                        byte[] buffer = GetCopyDataByteBuffer(ref copyData);

                                        string fromLauncherModulePath = Encoding.UTF8.GetString(buffer);

                                        if (App.LauncherMainModulePath.Equals(fromLauncherModulePath, StringComparison.Ordinal))
                                        {
                                            this.ShowByHANDLE();
                                        }
                                    }
                                    ;break;
                                case (nint)0x00:
                                    {

                                    }
                                    ; break;
                                default:
                                    {
                                        if (copyData.cbData > 0)
                                        {
                                            byte[] buffer = GetCopyDataByteBuffer(ref copyData);

                                            AcceptWindowCopyData(copyData.dwData, buffer);
                                        }
                                        else
                                        {
                                            AcceptWindowCopyData(copyData.dwData, null);
                                        }
                                    }; break;
                            };
                        }

                        handled = true;

                        break;
                    }
            }
            return IntPtr.Zero;
        }

        private static byte[] GetCopyDataByteBuffer(ref Win32.User32.COPYDATASTRUCT cdt)
        {
            byte[] buffer = new byte[cdt.cbData];

            if (cdt.cbData is 0) return buffer;

            try
            {
                Marshal.Copy(cdt.lpData, buffer, 0, buffer.Length);
            }
            catch
            {

            }

            return buffer;
        }

        protected abstract void AcceptWindowCopyData(nint type, byte[]? buffer = null);

        public abstract void CancelControl();

        public void ShowByHANDLE()
        {
            if (hWnd != IntPtr.Zero)
            {
                ShowInTaskbar = true;

                Win32.User32.ShowWindow(hWnd, Win32.User32.SW_TYPE.SW_SHOW);

                if (WindowState is WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                if (Win32.User32.GetForegroundWindow() != hWnd)
                {
                    Win32.User32.SetForegroundWindow(hWnd);
                }
            }
        }

        public void HideByHANDLE()
        {
            if (hWnd != IntPtr.Zero)
            {
                Win32.User32.ShowWindow(hWnd, Win32.User32.SW_TYPE.SW_HIDE);
            }
        }

        public void SetWindowEnableState(bool flag)
        {
            if (hWnd != IntPtr.Zero)
            {
                Win32.User32.EnableWindow(hWnd, flag);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!e.Cancel)
            {
                BindingOperations.ClearBinding(this, iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty);
            }
        }
    }
}
