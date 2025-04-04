using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenFrp.Service;

namespace OpenFrp.Launcher
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        internal readonly TaskCompletionSource<Yue3.Model.OpenFrp.Response.Data.UserInfoData?> UserInfoCallback = new TaskCompletionSource<Yue3.Model.OpenFrp.Response.Data.UserInfoData?>();

        public LoginWindow()
        {
            InitializeComponent();

            SetBinding(iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty, new Binding
            {
                Source = App.Settings,
                Path = new PropertyPath(nameof(App.Settings.ApplicationTheme)),
                Mode = BindingMode.OneWay
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            BindingOperations.ClearBinding(this, iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty);

            base.OnClosing(e);
        }

        public LoginWindow(Window parent) : this()
        {
            Owner = parent;
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