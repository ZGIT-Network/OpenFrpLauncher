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
        }
        public UserTunnel()
        {
            
        }

        private static DoubleAnimation? @opacityAnimationProc;


        public override void OnApplyTemplate()
        {
            if (BeginWithOpacityAnimation)
            {
                BeginAnimation(OpacityProperty, opacityAnimationProc);
            }
            if (GetTemplateChild("displayContextMenu") is HyperlinkButton btn)
            {
                if (GetTemplateChild("deleteTunnel") is MenuItem mf1)
                {
                    mf1.Command = new RelayCommand(() =>
                    {
                        var mf = new MenuFlyout();
                        mf.Items.Add(new DeleteUserTunnelContentPresenterMenuItem(this, mf));
                        mf.ShowAt(btn);
                    });
                }
                if (GetTemplateChild("editTunnel") is MenuItem mf2)
                {
                    mf2.Command = new RelayCommand(() =>
                    {
                        if (App.Current is { MainWindow: var mw} && ContentDialog.GetOpenDialog(mw) is not null)
                        {
                            return;
                        }
                        var dialog = new Dialogs.TunnelEditDialog
                        {

                        };
                        dialog.SetValue(TunnelConfEditor.EditorTemplateProperty, new Model.TunnelEditorTemplate(this.Tunnel));
                        dialog.Dispatcher.Invoke(async () =>
                        {
                            await dialog.ShowAsync();
                        });
                    });
                }
                if (GetTemplateChild("viewInfo") is MenuItem mf3)
                {
                    mf3.Command = new RelayCommand(() =>
                    {
                        if (App.Current is { MainWindow: var mw } && ContentDialog.GetOpenDialog(mw) is not null)
                        {
                            return;
                        }
                        var dialog = new Dialogs.TunnelViewerDialog()
                        {

                        };
                        dialog.SetValue(TunnelConfViewer.TunnelProperty, this.Tunnel.ModelClone());
                        dialog.Dispatcher.Invoke(async () =>
                        {
                            await dialog.ShowAsync();
                        });
                    });
                }
                if (GetTemplateChild("openFolder") is MenuItem mf4)
                {
                    mf4.Command = new RelayCommand(() =>
                    {
                        var f = OpenFrp.Service.Helpers.FileHelper.GetFrpcWorkDictionary(Tunnel.Id.ToString());

                        try
                        {
                            System.Diagnostics.Process.Start("explorer", f);
                        }
                        catch
                        {

                        }
                    });
                }
            }
         
            if (GetTemplateChild("switcher") is ToggleSwitch @switch)
            {
                @switch.IsOn = Tunnel.FirstState;

                @switch.Toggled += (_, e) =>
                {
                    if (!@switch.IsEnabled || @switch.Tag is object)
                    {
                        @switch.ClearValue(TagProperty);

                        return;
                    }
                    @switch.IsEnabled = false;

                    ToggledCommandBinding.Execute(@switch);
                };
            }
            if (GetTemplateChild("copyLink") is HyperlinkButton btnCopy)
            {
                if (VisualStateManager.GetVisualStateGroups(btnCopy) is not { Count: > 0 } vsg || vsg[0] is not VisualStateGroup { Name: "CopyState" } cs) return;

                VisualStateManager.GoToElementState(btnCopy, "CopyNormal", false);

                btnCopy.Command = new RelayCommand(() =>
                {
                    if (cs.CurrentState.Name is "CopySuccess") return;

                    string address = "";

                    if (Tunnel.Type.Contains("http") || Tunnel.Type.Contains("HTTP"))
                    {
                        address = Tunnel.Domains.First() ?? "Unknown Domain";
                    }
                    else
                    {
                        address = Tunnel.ConnectAddress;
                    }

                    try
                    {
                        Clipboard.SetText(address);

                        VisualStateManager.GoToElementState(btnCopy, "CopySuccess", false);

                        _ = btnCopy.Dispatcher.Invoke(async () => { await Task.Delay(1500); VisualStateManager.GoToElementState(btnCopy, "CopyNormal", false); });
                    }
                    catch (Exception ex)
                    {
                        Error.Invoke(this, ex);
                    }
                });
            }
        }


        public void ToggleStateTo(bool flag, bool force = false)
        {
            if (GetTemplateChild("switcher") is ToggleSwitch @switch)
            {
                // !false = true;
                if (!@switch.IsEnabled)
                {
                    @switch.IsOn = flag;
                    @switch.IsEnabled = true;
                }
                else if (force) // true
                {
                    @switch.Tag = new object { };
                    @switch.IsOn = flag;
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
        public IAsyncRelayCommand DeleteCommandBinding
        {
            get { return (IAsyncRelayCommand)GetValue(DeleteCommandBindingProperty); }
            set { SetValue(DeleteCommandBindingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DeleteCommandBinding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DeleteCommandBindingProperty =
            DependencyProperty.Register("DeleteCommandBinding", typeof(IAsyncRelayCommand), typeof(UserTunnel), new PropertyMetadata());
        #endregion
        #region RefreshCommandBinding


        public ICommand RefreshCommandBinding
        {
            get { return (ICommand)GetValue(RefreshCommandBindingProperty); }
            set { SetValue(RefreshCommandBindingProperty, value); }
        }
        
        public static readonly DependencyProperty RefreshCommandBindingProperty =
            DependencyProperty.Register("RefreshCommandBinding", typeof(ICommand), typeof(UserTunnel), new PropertyMetadata());



        #endregion
        #region ToggledCommandBinding
        public IAsyncRelayCommand ToggledCommandBinding
        {
            get { return (IAsyncRelayCommand)GetValue(ToggledCommandBindingProperty); }
            set { SetValue(ToggledCommandBindingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ToggledCommandBinding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ToggledCommandBindingProperty =
            DependencyProperty.Register("ToggledCommandBinding", typeof(IAsyncRelayCommand), typeof(UserTunnel), new PropertyMetadata());
        #endregion

        public void RemoveWithAnimate(Action callback)
        {
            if (GetTemplateChild("subfr") is not Border br) return;

            Storyboard stb = new Storyboard { };

            var toffHitAnimation = new BooleanAnimationUsingKeyFrames();
            {
                toffHitAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame()
                {
                    KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                    Value = false,
                });
            };
            var opacityDoubleAnimation = new DoubleAnimation()
            {
                From = 1,
                To = 0
            };
            var doubleXAnimation = new DoubleAnimation()
            {
                To = 0.95
            };
            var doubleYAnimation = new DoubleAnimation()
            {
                To = 0.95
            };

            Storyboard.SetTargetProperty(opacityDoubleAnimation, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTargetProperty(toffHitAnimation, new PropertyPath(UIElement.IsHitTestVisibleProperty));
            Storyboard.SetTargetProperty(doubleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTargetProperty(doubleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            opacityDoubleAnimation.Duration = doubleXAnimation.Duration = doubleYAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(250));

            opacityDoubleAnimation.EasingFunction = doubleXAnimation.EasingFunction = doubleYAnimation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };

            stb.Children.Add(opacityDoubleAnimation);
            stb.Children.Add(doubleXAnimation);
            stb.Children.Add(doubleYAnimation);
            stb.Children.Add(toffHitAnimation);

            stb.Completed += delegate { callback.Invoke(); };

            br.BeginStoryboard(stb);
        }

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
