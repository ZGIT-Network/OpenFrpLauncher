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

namespace OpenFrp.Launcher.Controls
{
    /// <summary>
    /// TunnelConfig.xaml 的交互逻辑
    /// </summary>
    public partial class TunnelConfig : Control
    {
        public TunnelConfig()
        {
            InitializeComponent();
        }



        public Awe.Model.OpenFrp.Response.Data.UserTunnel TunnelData
        {
            get { return (Awe.Model.OpenFrp.Response.Data.UserTunnel)GetValue(TunnelDataProperty); }
            set { SetValue(TunnelDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TunnelData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TunnelDataProperty =
            DependencyProperty.Register("TunnelData", typeof(Awe.Model.OpenFrp.Response.Data.UserTunnel), typeof(TunnelConfig), new PropertyMetadata(new Awe.Model.OpenFrp.Response.Data.UserTunnel { }));



        public bool IsCreateMode
        {
            get { return (bool)GetValue(IsCreateModeProperty); }
            set { SetValue(IsCreateModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsCreateMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCreateModeProperty =
            DependencyProperty.Register("IsCreateMode", typeof(bool), typeof(TunnelConfig), new PropertyMetadata(true));


    }
}
