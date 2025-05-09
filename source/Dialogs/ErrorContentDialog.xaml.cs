using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Dialogs
{
    /// <summary>
    /// ErrorContentDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorContentDialog : ContentDialog
    {
        public ErrorContentDialog()
        {
            InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (GetValue(Controls.ErrorViewer.ExceptionProperty) is Exception ex)
            {
                try
                {
                    Clipboard.SetText(ex.ToString());
                    Clipboard.Flush();
                }
                catch
                {

                }
            }
        }
    }
}
