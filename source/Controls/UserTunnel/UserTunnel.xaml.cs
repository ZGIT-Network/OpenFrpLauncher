using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class UserTunnel : ContentControl
    {
        static UserTunnel()
        {
            var cubic = new CubicEase { EasingMode = EasingMode.EaseOut };

            cubic.Freeze();

            @opacityAnimationProc = new DoubleAnimation
            {
                To = 1,
                From = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = cubic
            };
            opacityAnimationProc.Freeze();
        }
        public UserTunnel()
        {
            this.UserContextMenuCommandBinding ??= event_DefaultUserContextMenuActionCommand;
        }

        private static DoubleAnimation? @opacityAnimationProc;

        public override void OnApplyTemplate()
        {
            if (BeginWithOpacityAnimation)
            {
                BeginAnimation(OpacityProperty, opacityAnimationProc);
            }
            

            if (GetTemplateChild("displayContextMenu") is HyperlinkButton btn &&
                GetTemplateChild("subfr") is FrameworkElement { ContextMenu: var r_cm } &&
                r_cm.Items[r_cm.Items.Count - 1] is MenuItem { Name: "deleteUserTunnel" } mi)
            {
                r_cm.DataContext = r_cm.PlacementTarget = this;
                btn.Command = new RelayCommand(() => r_cm.IsOpen = true);
                mi.Command = new RelayCommand(() =>
                {
                    var f = new MenuFlyout();
                    f.Items.Add(new DeleteUserTunnelContentPresenterMenuItem(this, f) { });
                    f.ShowAt(btn);
                });
            }
            if (GetTemplateChild("switcher") is ToggleSwitch @switch)
            {
                @switch.IsOn = Tunnel.FirstState;

                @switch.Toggled += (_, e) =>
                {
                    if (!@switch.IsEnabled)
                    {
                        return;
                    }
                    @switch.IsEnabled = false;

                    ToggledCommandBinding.Execute(@switch);
                };
            }
            if (GetTemplateChild("copyLink") is HyperlinkButton btnCopy)
            {
                VisualStateManager.GoToElementState(btnCopy, "CopyNormal", false);

                btnCopy.Command = new RelayCommand(() =>
                {
                    try
                    {
                        Clipboard.SetText(Tunnel.ConnectAddress);

                        VisualStateManager.GoToElementState(btnCopy, "CopySuccess",false);

                        _ = btnCopy.Dispatcher.Invoke(async () => { await Task.Delay(1500); VisualStateManager.GoToElementState(btnCopy, "CopyNormal", false); });
                    }
                    catch(Exception ex)
                    {
                        Error.Invoke(this, ex);
                    }
                });
            }
        }

        [RelayCommand]
        private async Task @event_DefaultUserContextMenuAction(string? action)
        {
            switch (action)
            {
                case "view-info":
                    {
                        var viewDialog = new Controls.TunnelInfoContentDialog()
                        {
                            Tunnel = this.Tunnel,
                        };
                        _ = await viewDialog.ShowAsync();
                    }; break;
                case "copy-extra-address":
                    {
                        try
                        {
                            Clipboard.SetText(Tunnel.ExtraConnectAddress.FirstOrDefault());
                        }
                        catch(Exception ex)
                        {
                            Error.Invoke(this, ex);
                        }
                    };break;
            }
        }

        public void ToggleStateTo(bool flag)
        {
            if (GetTemplateChild("switcher") is ToggleSwitch @switch)
            {
                if (!@switch.IsEnabled)
                {
                    @switch.IsOn = flag;
                    @switch.IsEnabled = true;
                }
            }
        }

        public event EventHandler<Exception> Error = delegate { };

        #region Tunnel
        public Model.UserTunnel Tunnel
        {
            get { return (Model.UserTunnel)GetValue(TunnelProperty); }
            set { SetValue(TunnelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Tunnel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TunnelProperty =
            DependencyProperty.Register("Tunnel", typeof(Model.UserTunnel), typeof(UserTunnel), new PropertyMetadata(null));

        #endregion
        #region BeginWithOpacityAnimation
        // Using a DependencyProperty as the backing store for BeginWithOpacityAnimation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BeginWithOpacityAnimationProperty =
            DependencyProperty.Register("BeginWithOpacityAnimation", typeof(bool), typeof(UserTunnel), new PropertyMetadata(false));

        public bool BeginWithOpacityAnimation
        {
            get { return (bool)GetValue(BeginWithOpacityAnimationProperty); }
            set { SetValue(BeginWithOpacityAnimationProperty, value); }
        }
        #endregion
        #region DeleteCommandBinding
        public ICommand DeleteCommandBinding
        {
            get { return (ICommand)GetValue(DeleteCommandBindingProperty); }
            set { SetValue(DeleteCommandBindingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DeleteCommandBinding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DeleteCommandBindingProperty =
            DependencyProperty.Register("DeleteCommandBinding", typeof(ICommand), typeof(UserTunnel), new PropertyMetadata());
        #endregion
        #region UserContextMenuCommandBinding

        public ICommand UserContextMenuCommandBinding
        {
            get { return (ICommand)GetValue(UserContextMenuCommandBindingProperty); }
            set { SetValue(UserContextMenuCommandBindingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserContextMenuCommandBinding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserContextMenuCommandBindingProperty =
            DependencyProperty.Register("UserContextMenuCommandBinding", typeof(ICommand), typeof(UserTunnel), new PropertyMetadata());

        #endregion
        #region ToggledCommandBinding
        public ICommand ToggledCommandBinding
        {
            get { return (ICommand)GetValue(ToggledCommandBindingProperty); }
            set { SetValue(ToggledCommandBindingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ToggledCommandBinding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ToggledCommandBindingProperty =
            DependencyProperty.Register("ToggledCommandBinding", typeof(ICommand), typeof(UserTunnel), new PropertyMetadata());
        #endregion


        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new UserTunnelAutomationPeer(this);
        }
    }
    public partial class DeleteUserTunnelContentPresenterMenuItem : MenuItem
    {
        static DeleteUserTunnelContentPresenterMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DeleteUserTunnelContentPresenterMenuItem), new FrameworkPropertyMetadata(typeof(DeleteUserTunnelContentPresenterMenuItem)));
        }

        public DeleteUserTunnelContentPresenterMenuItem(UserTunnel userTunnelCtrl,MenuFlyout flyout)
        {
            this.flyout = flyout;
            this.userTunnelCtrl = userTunnelCtrl;
        }

        private readonly UserTunnel? userTunnelCtrl;
        private readonly MenuFlyout? flyout;

        public override void OnApplyTemplate()
        {
            if (userTunnelCtrl is null || flyout is null) throw new NotSupportedException();

            if (GetTemplateChild("delete_button") is Button button_delete)
            {
                button_delete.Command = new RelayCommand(delegate
                {
                    if (this.DataContext is Model.UserTunnel uix)
                    {
                        userTunnelCtrl.DeleteCommandBinding.Execute(uix);
                    }
                    flyout.Hide();
                });
            }
            if (GetTemplateChild("cancelAndCloseFlyout") is Button button_cancel)
            {
                button_cancel.Command = new RelayCommand(flyout.Hide);
            }
            base.OnApplyTemplate();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return null!;
        }
    }
}
