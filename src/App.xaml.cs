using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace OpenFrp.Launcher
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var wind = new MainWindow();

            wind.Show();

            if (Awe.UI.Win32.UserUxtheme.IsSupportDarkMode)
            {
                Awe.UI.Win32.UserUxtheme.AllowDarkModeForApp(true);
                Awe.UI.Win32.UserUxtheme.ShouldAppsUseDarkMode();
                Awe.UI.Win32.UserUxtheme.ShouldSystemUseDarkMode();
            }

            var handle = new WindowInteropHelper(wind).EnsureHandle();

            if (Environment.OSVersion.Version.Major >= 10)
            {
                int backdropPvAttribute = (int)Awe.UI.Win32.DwmApi.DWMSBT.DWMSBT_TABBEDWINDOW;
                Awe.UI.Win32.DwmApi.DwmSetWindowAttribute(handle, Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdropPvAttribute,
                    Marshal.SizeOf(typeof(int)));

                var pvAttribute = (int)Awe.UI.Win32.DwmApi.PvAttribute.Enable;
                var dwAttribute = Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

                if (Environment.OSVersion.Version < new Version(10, 0, 18985))
                {
                    dwAttribute = Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
                }

                var vk = Awe.UI.Win32.DwmApi.DwmSetWindowAttribute(handle, dwAttribute,
                    ref pvAttribute,
                    Marshal.SizeOf(typeof(int)));


                if (vk is not 0)
                {
                    wind.Tag = "non-success-dark";
                }
                else
                {
                    wind.Width += 1;
                    wind.Width -= 1;
                }

            }

            

            Awe.UI.Win32.UserUxtheme.SetWindowLong(handle, -16, Awe.UI.Win32.UserUxtheme.GetWindowLong(handle, -16) & ~0x80000);

              
        }

        
    }
}
