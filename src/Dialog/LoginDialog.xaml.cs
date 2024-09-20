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
using ModernWpf.Controls;

namespace OpenFrp.Launcher.Dialog
{
    /// <summary>
    /// LoginDialog.xaml 的交互逻辑
    /// </summary>
    public partial class LoginDialog : ContentDialog
    {
        public LoginDialog()
        {
            InitializeComponent();
        }

        public TaskCompletionSource<Awe.Model.OpenFrp.Response.Data.UserInfo> TaskCompletionSource { get; set; } = new TaskCompletionSource<Awe.Model.OpenFrp.Response.Data.UserInfo>();

        public new async Task<Awe.Model.OpenFrp.Response.Data.UserInfo?> ShowAsync()
        {
            try
            {
                var showDialog = base.ShowAsync();
                var task = await Task.WhenAny(TaskCompletionSource.Task, showDialog);

                base.Hide();

                if (!task.Equals(showDialog) && await TaskCompletionSource.Task is { } t)
                {
                    return t;
                }
            }
            catch(TaskCanceledException)
            {
                
            }
            base.Hide();
            return default;
        }
    }
}
