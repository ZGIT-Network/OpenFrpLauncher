using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

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
            ViewModels.MainWindowViewModel.ShutdownApp();
        }

        private void CloseLauncher_Click(object sender, RoutedEventArgs e)
        {
            App.TaskBarIcon?.CloseTrayPopup();

            switch (App.Current.MainWindow)
            {
                case MainWindow mw:
                    {
                        iNKORE.UI.WPF.Modern.Controls.ContentDialog.GetOpenDialog(mw)?.Hide();

                        mw.HideByHwndCC();
                    }
                     ; break;
                case LoginWindow lw:
                    {
                        iNKORE.UI.WPF.Modern.Controls.ContentDialog.GetOpenDialog(lw)?.Hide();

                        lw.HideByHwndCC();
                        //CloseLauncher_Click(sender, e);
                        //lw.ShowByHwndCC();

                    }
                     ; break;
            }
            try
            {
                BindingOperations.ClearBinding(App.Current.MainWindow, iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty);
            }
            catch
            {

            }

            Helpers.UsrTokenService.WriteConfig();
            App.Settings.Save();

            App.TaskBarIcon?.Dispose();

            App.Current.Shutdown(0);
        }

        private static void DisplayWindowRequest(object sender,RoutedEventArgs e)
        {
            switch (App.Current.MainWindow)
            {
                case MainWindow mw:
                    {
                        mw.ShowByHwndCC();
                    };break;
                case LoginWindow lw:
                    {
                        lw.ShowByHwndCC();
                    }
                    ; break;
            }
            App.TaskBarIcon?.CloseTrayPopup();
        }
    }
}
