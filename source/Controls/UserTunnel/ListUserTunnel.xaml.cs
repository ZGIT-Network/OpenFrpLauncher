using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.ViewModels;

namespace OpenFrp.Launcher.Controls
{
    public partial class ListUserTunnel : UserTunnelBase
    {
        public ListUserTunnel()
        {

        }

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ListUserTunnel), new PropertyMetadata(new CornerRadius()));

        public int TotalInContainer
        {
            get { return (int)GetValue(TotalInContainerProperty); }
            set { SetValue(TotalInContainerProperty, value); }
        }

        public static readonly DependencyProperty TotalInContainerProperty =
            DependencyProperty.Register("TotalInContainer", typeof(int), typeof(ListUserTunnel), new PropertyMetadata(0));

        public int CurrentIndex
        {
            get { return (int)GetValue(CurrentIndexProperty); }
            set { SetValue(CurrentIndexProperty, value); }
        }

        public static readonly DependencyProperty CurrentIndexProperty =
            DependencyProperty.Register("CurrentIndex", typeof(int), typeof(ListUserTunnel), new PropertyMetadata(0));

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TotalInContainerProperty || e.Property == CurrentIndexProperty)
            {
                bool isLast = (CurrentIndex == TotalInContainer - 1) && TotalInContainer > 0;

                SetValue(IsLastItemPropertyKey, isLast);
            }

            base.OnPropertyChanged(e);
        }

        public bool IsLastItem
        {
            get => (bool)GetValue(IsLastItemProperty);
            protected set => SetValue(IsLastItemPropertyKey, value);
        }

        public static DependencyProperty IsLastItemProperty => IsLastItemPropertyKey.DependencyProperty;

        public static readonly DependencyPropertyKey IsLastItemPropertyKey = 
            DependencyProperty.RegisterReadOnly("IsLastItem", typeof(bool), typeof(ListUserTunnel), new PropertyMetadata(false));

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new UserTunnelAutomationPeer(this);
        }
    }
}
