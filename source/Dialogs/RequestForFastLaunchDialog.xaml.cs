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
    /// RequestForFastLaunchDialog.xaml 的交互逻辑
    /// </summary>
    public partial class RequestForFastLaunchDialog : ContentDialog
    {
        public RequestForFastLaunchDialog()
        {
            InitializeComponent();
        }

        public RequestForFastLaunchDialog(Dictionary<string,string> solveQuery) : this()
        {
            

            if (solveQuery.TryGetValue("proxy", out var strTid))
            {
                SetValue(TunnelIdPropertyKey, int.TryParse(strTid, out var tid) ? tid : -1);
            }
            SetValue(TunnelNamePropertyKey, solveQuery.TryGetValue("name", out var name) ? name : "");
            SetValue(UserTokenPropertyKey, solveQuery.TryGetValue("user", out var token) ? token : "");
        }

        public string TunnelName
        {
            get { return (string)GetValue(TunnelNameProperty); }
        }

        public static DependencyProperty TunnelNameProperty => TunnelNamePropertyKey.DependencyProperty;

        public static readonly DependencyPropertyKey TunnelNamePropertyKey =
            DependencyProperty.RegisterReadOnly("TunnelName", typeof(string), typeof(RequestForFastLaunchDialog), new PropertyMetadata(""));

        public string UserToken
        {
            get { return (string)GetValue(UserTokenProperty); }
        }

        public static DependencyProperty UserTokenProperty => UserTokenPropertyKey.DependencyProperty;

        public static readonly DependencyPropertyKey UserTokenPropertyKey =
            DependencyProperty.RegisterReadOnly("UserToken", typeof(string), typeof(RequestForFastLaunchDialog), new PropertyMetadata(""));


        public int TunnelId
        {
            get { return (int)GetValue(TunnelIdProperty); }
        }

        public static DependencyProperty TunnelIdProperty => TunnelIdPropertyKey.DependencyProperty;

        public static readonly DependencyPropertyKey TunnelIdPropertyKey =
            DependencyProperty.RegisterReadOnly("TunnelId", typeof(int), typeof(RequestForFastLaunchDialog), new PropertyMetadata(-1));


    }
}
