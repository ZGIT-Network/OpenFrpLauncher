using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using iNKORE.UI.WPF.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;

namespace OpenFrp.Launcher.Controls
{
    public partial class AppPopup
    {
        public AppPopup()
        {
            this.InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            displayWindow.Click += DisplayWindowRequest;
            closeLauncher.Click += CloseLauncher_Click;
            closeAll.Click += CloseAll_Click;

            base.OnApplyTemplate();
        }

        


        private void CloseAll_Click(object sender, RoutedEventArgs e)
        {
            App.TaskBarIcon?.CloseTrayPopup();

            ViewModels.MainWindowViewModel.ShutdownApp();
        }

        private void CloseLauncher_Click(object sender, RoutedEventArgs e)
        {
            App.TaskBarIcon?.CloseTrayPopup();

            if (App.Current.MainWindow is AppWindow ap)
            {
                iNKORE.UI.WPF.Modern.Controls.ContentDialog.GetOpenDialog(ap)?.Hide();

                ap.HideByHANDLE();
                ap.CancelControl();
            }
            //switch (App.Current.MainWindow)
            //{
            //    case MainWindow mw:
            //        {
            //            iNKORE.UI.WPF.Modern.Controls.ContentDialog.GetOpenDialog(mw)?.Hide();

            //            mw.HideByHANDLE();
            //        }
            //         ; break;
            //    case LoginWindow lw:
            //        {
            //            iNKORE.UI.WPF.Modern.Controls.ContentDialog.GetOpenDialog(lw)?.Hide();

            //            lw.HideByHANDLE();
            //            //CloseLauncher_Click(sender, e);
            //            //lw.ShowByHwndCC();

            //        }
            //         ; break;
            //}
            try
            {
                BindingOperations.ClearBinding(App.Current.MainWindow, iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty);
            }
            catch
            {

            }

            Helpers.UsrTokenService.WriteConfig();
            App.Settings.Save();

            if (OSVersionHelper.IsWindows10OrGreater)
            {
                try
                {
                    Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.Uninstall();
                    //ToastNotificationManagerCompat.History.Clear();
                }
                catch { }
            }
            App.TaskBarIcon?.Dispose();

            App.Current.Shutdown(0);
        }

        private static void DisplayWindowRequest(object sender,RoutedEventArgs e)
        {
            switch (App.Current.MainWindow)
            {
                case LoginWindow lw:
                    {
                        lw.ShowByHANDLE();
                    }
                    ; break;
                case MainWindow mw:
                    {
                        mw.ShowByHANDLE();
                    };break;

            }
            App.TaskBarIcon?.CloseTrayPopup();
        }
    }
}
