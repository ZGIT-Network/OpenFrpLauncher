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
using OpenFrp.Service;

namespace OpenFrp.Launcher.Views.LoginWindow
{
    /// <summary>
    /// OAuthLoginDisplay.xaml 的交互逻辑
    /// </summary>
    public partial class OAuthLoginView : UserControl
    {
        public OAuthLoginView() : this(delegate { })
        {
            InitializeComponent();
        }

        public OAuthLoginView(Action<string> callbackAction)
        {
            AuthorizationCodeWaiter = new TaskCompletionSource<string>();

            this.CallbackAction = callbackAction;

            InitializeComponent();
        }

        internal TaskCompletionSource<string> AuthorizationCodeWaiter;

        internal readonly Action<string> CallbackAction = delegate { };

        public async Task<string?> WaitForFinish(CancellationToken cancellationToken = default)
        {
            if (AuthorizationCodeWaiter.Task.IsCompleted)
            {
                ResetAuthorizationCodeWaiter();
            }
            return await AuthorizationCodeWaiter.Task.WaitAsync(cancellationToken);
        }

        public void ResetAuthorizationCodeWaiter()
        {
            AuthorizationCodeWaiter = new TaskCompletionSource<string>();
        }
    }
}
