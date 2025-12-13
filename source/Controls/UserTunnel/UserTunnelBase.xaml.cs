using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.ViewModels;

namespace OpenFrp.Launcher.Controls
{
    public class UserTunnelBase : ContentControl
    {
        public override void OnApplyTemplate()
        {
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
                        if (App.Current is not { MainWindow: var mw } || ContentDialog.GetOpenDialog(mw) is not null)
                        {
                            return;
                        }
                        if (!App.Settings.UseWebView2Tools)
                        {
                            var dialog = new Dialogs.TunnelEditDialog
                            {

                            };
                            dialog.SetValue(TunnelConfEditor.EditorTemplateProperty, new Model.TunnelEditorTemplate(this.Tunnel));
                            dialog.Dispatcher.Invoke(async () =>
                            {
                                await dialog.ShowAsync();
                            });
                        }
                        else
                        {
                            var w = new WebView2Window
                            {
                                Title = $"OpenFRP 启动器 - 编辑隧道 #{this.Tunnel.Id} {this.Tunnel.Name} (WebView2)",
                                Source = $"https://console.openfrp.net/launcher/edit/" +
                                   this.Tunnel.Id +
                                   $"?use_backdrop={App.Settings.BackdropType is not iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType.None && OSVersionHelper.IsWindows11OrGreater}" +
                                   $"&theme_mode={(iNKORE.UI.WPF.Modern.ThemeManager.GetActualTheme(mw) is iNKORE.UI.WPF.Modern.ElementTheme.Dark ? "dark" : "light")}"
                            };

                            w.Owner = mw;

                            w.Loaded += delegate
                            {
                                w.Left = mw.Left + (mw.ActualWidth / 2) - (w.ActualWidth / 2);
                                w.Top = mw.Top + (mw.ActualHeight / 2) - (w.ActualHeight / 2);
                            };

                            if (w.ShowDialog() is true)
                            {
                                Model.RouteMessage<MainWindowViewModel>.Send(typeof(Views.Tunnels));
                            }
                        }
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
                @switch.IsOn = Tunnel.FirstState || Tunnel.Tunnel is { IsOnline: true };

                @switch.Toggled += (_, e) =>
                {
                    if (!@switch.IsEnabled || @switch.Tag is object)
                    {
                        @switch.ClearValue(TagProperty);

                        Tunnel.FirstState = @switch.IsOn;

                        if (!@switch.IsOn && Tunnel.Tunnel != null)
                        {
                            Tunnel.Tunnel.IsOnline = false;
                        }

                        return;
                    }
                    @switch.IsEnabled = false;

                    if (!@switch.IsOn && Tunnel.Tunnel is { IsOnline: true })
                    {
                        Tunnel.Tunnel.IsOnline = false;
                    }

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
            if (GetTemplateChild("usedByOtherClient") is TextBlock tb1)
            {
                if (Tunnel.IsEnable && !Tunnel.FirstState && Tunnel.Tunnel!.IsOnline)
                {
                    this.IsEnabled = false;
                    tb1.Visibility = Visibility.Visible;
                }
                else
                {
                    tb1.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void ToggleStateTo(bool flag, bool force = false)
        {
            if (GetTemplateChild("switcher") is ToggleSwitch @switch)
            {
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

        public Model.UserTunnel Tunnel
        {
            get { return (Model.UserTunnel)GetValue(TunnelProperty); }
            set { SetValue(TunnelProperty, value); }
        }

        public static readonly DependencyProperty TunnelProperty =
            DependencyProperty.Register("Tunnel", typeof(Model.UserTunnel), typeof(UserTunnelBase), new PropertyMetadata(null));

        #region ToggledCommandBinding
        public IAsyncRelayCommand ToggledCommandBinding
        {
            get { return (IAsyncRelayCommand)GetValue(ToggledCommandBindingProperty); }
            set { SetValue(ToggledCommandBindingProperty, value); }
        }

        public static readonly DependencyProperty ToggledCommandBindingProperty =
            DependencyProperty.Register("ToggledCommandBinding", typeof(IAsyncRelayCommand), typeof(UserTunnelBase), new PropertyMetadata());
        #endregion

        #region DeleteCommandBinding
        public IAsyncRelayCommand DeleteCommandBinding
        {
            get { return (IAsyncRelayCommand)GetValue(DeleteCommandBindingProperty); }
            set { SetValue(DeleteCommandBindingProperty, value); }
        }

        public static readonly DependencyProperty DeleteCommandBindingProperty =
            DependencyProperty.Register("DeleteCommandBinding", typeof(IAsyncRelayCommand), typeof(UserTunnelBase), new PropertyMetadata());
        #endregion
    }
}
