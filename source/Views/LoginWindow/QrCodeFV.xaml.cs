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
    /// QrCodeFV.xaml 的交互逻辑
    /// </summary>
    public partial class QrCodeFV : UserControl
    {
        public QrCodeFV() : this(delegate { })
        {

        }

        public QrCodeFV(Action<string> callbackAction)
        {
            AuthorizationCodeWaiter = new TaskCompletionSource<string>();

            this.CallbackAction = callbackAction;

            InitializeComponent();
        }

        internal readonly TaskCompletionSource<string> AuthorizationCodeWaiter;

        internal readonly Action<string> CallbackAction = delegate { };

        public async Task<string?> WaitForFinish(CancellationToken cancellationToken = default)
        {
            return await AuthorizationCodeWaiter.Task.WhenAnyTime(cancellationToken);
        }
    }
}
