using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class TunnelConfViewer : ContentControl
    {
        #region Tunnel
        public Model.UserTunnel Tunnel
        {
            get { return (Model.UserTunnel)GetValue(TunnelProperty); }
            set { SetValue(TunnelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Tunnel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TunnelProperty =
            DependencyProperty.Register("Tunnel", typeof(Model.UserTunnel), typeof(TunnelConfViewer), new PropertyMetadata(null));

        #endregion
    }
}
