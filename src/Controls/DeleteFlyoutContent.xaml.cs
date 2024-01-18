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
using CommunityToolkit.Mvvm.Input;

namespace OpenFrp.Launcher.Controls
{
    /// <summary>
    /// DeleteFlyoutContent.xaml 的交互逻辑
    /// </summary>
    public partial class DeleteFlyoutContent : UserControl
    {
        public DeleteFlyoutContent()
        {
            InitializeComponent();
        }



        public Awe.Model.OpenFrp.Response.Data.UserTunnel UserTunnel
        {
            get { return (Awe.Model.OpenFrp.Response.Data.UserTunnel)GetValue(UserTunnelProperty); }
            set { SetValue(UserTunnelProperty, value); }
        }

        public FrameworkElement Description
        {
            get { return (FrameworkElement)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Description.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(FrameworkElement), typeof(DeleteFlyoutContent), new PropertyMetadata());

        // Using a DependencyProperty as the backing store for UserTunnel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserTunnelProperty =
            DependencyProperty.Register("UserTunnel", typeof(Awe.Model.OpenFrp.Response.Data.UserTunnel), typeof(DeleteFlyoutContent), new PropertyMetadata());

        public Func<Task> InvokeAction { get; set; } = new Func<Task>(async delegate { await Task.CompletedTask; });

        public override void OnApplyTemplate()
        {
            if (FindName("closeWind") is Awe.UI.Controls.NavigationButton nb)
            {
                nb.Click += delegate { Awe.UI.Helper.FlyoutHelper.RemoveAllMask(); };
            }
            if (FindName("primary") is Button btn)
            {
                btn.Command = new AsyncRelayCommand(InvokeAction);
            }
            if (FindName("cancel") is Button canBtn)
            {
                canBtn.Click += delegate { Awe.UI.Helper.FlyoutHelper.RemoveAllMask();  };
            }

            base.OnApplyTemplate();
        }
    }
}
