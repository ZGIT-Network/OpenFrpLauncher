using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Service;


namespace OpenFrp.Launcher
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : AppWindow
    {
        internal readonly TaskCompletionSource<Yue3.Model.OpenFrp.Response.Data.UserInfoData?> UserInfoCallback = new TaskCompletionSource<Yue3.Model.OpenFrp.Response.Data.UserInfoData?>();

        public LoginWindow() : base()
        {
            this.InitializeComponent();

            if (!App.StartupArguments.Contains("--minimize"))
            {
                WindowState = WindowState.Normal;
            }
            if (App.StartupArguments.Contains("--updateFrpClient"))
            {
  
            }
            if (App.StartupArguments.Where(x => x.StartsWith("openfrp://")) is { } tl && tl.Any())
            {
                App.StartupArguments.RemoveWhere(x => x.StartsWith("openfrp://"));

                DisplayFastLinkDialog(tl.First());
            }
        }

        public LoginWindow(Window parent) : this()
        {
            Owner = parent;
        }

        public override void CancelControl()
        {
            if (this.DataContext is ViewModels.LoginWindowViewModel lwm)
            {
                lwm.conve_TryDetectFrpcCancelCommand.Execute(default);
                lwm.conve_RelayPrepareCancelCommand.Execute(default);

                lwm.event_CancelLoginCommand.Execute(default);
            }
        }

        protected override void AcceptWindowCopyData(nint type, byte[]? buffer)
        {
            switch (type)
            {
                case 0x02 when buffer is not null:
                    {
                        string callPath = Encoding.UTF8.GetString(buffer);

                        if (!callPath.StartsWith("openfrp://"))
                        {
                            return;
                        }

                        DisplayFastLinkDialog(callPath);
                    }
                    ;break;
            }
        }


        protected void DisplayFastLinkDialog(string callPath)
        {
            Dispatcher.BeginInvoke(async () =>
            {
                var contentDialog = new iNKORE.UI.WPF.Modern.Controls.ContentDialog
                {
                    Title = "一键启动",
                    Content = new TextBlock()
                    {
                        Text = "\"一键启动\"需要在启动器主页面进行，是否直接进入主页面且启动相关隧道？",
                        TextWrapping = TextWrapping.Wrap
                    },
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消",
                    DefaultButton = iNKORE.UI.WPF.Modern.Controls.ContentDialogButton.Primary
                };
                if (await contentDialog.ShowAsync() is iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
                {
                    App.StartupArguments.Add(callPath);

                    if (DataContext is ViewModels.LoginWindowViewModel lvvm)
                    {
                        lvvm.event_GotoMainWindowCommand.Execute(null);
                    }
                }
            }, priority: System.Windows.Threading.DispatcherPriority.Background);
        }

        public Task<Yue3.Model.OpenFrp.Response.Data.UserInfoData?> LoginWndProcAsync(CancellationToken cancellationToken = default)
        {
            _ = Dispatcher.InvokeAsync(ShowDialog);

            var a = cancellationToken.Register(() => Dispatcher.Invoke(() =>
            {
                if (this.DataContext is ViewModels.LoginWindowViewModel v)
                {
                    v.CanCancelLogin = true;

                    v.event_CancelLoginCommand.Execute(default);
                }
                this.Close();
            }));

            return UserInfoCallback.Task.ContinueWith((t) => { a.Dispose(); return t.Result; }).WaitAsync(cancellationToken);
        }
    }
}