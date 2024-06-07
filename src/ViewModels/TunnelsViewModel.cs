using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ModernWpf.Controls;
using OpenFrp.Launcher.Model;
using AppNetwork = OpenFrp.Service.Net;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class TunnelsViewModel : ObservableObject
    {
        public TunnelsViewModel ()
        {
            WeakReferenceMessenger.Default.UnregisterAll(nameof(TunnelsViewModel));

            WeakReferenceMessenger.Default.Register<RouteMessage<TunnelsViewModel, OpenFrp.Service.Proto.Response.NotiflyStreamResponse>>(nameof(TunnelsViewModel), (_, data) =>
            {
                if (data.Data is { State: Service.Proto.Response.NotiflyStreamState.NoticeForTunnelClosed } dac)
                {
                    try
                    {
                        var tunnel = JsonSerializer.Deserialize<Awe.Model.OpenFrp.Response.Data.UserTunnel>(dac.TunnelJson);

                        if (tunnel is null) return;

                        if (Tunnels is { Count: > 0 } && itemsControl != null)
                        {
                            OnlineTunnels.Remove(tunnel.Id);

                            foreach (var tal in Tunnels)
                            {
                                if (tal.Id.Equals(tunnel.Id))
                                {
                                    var vi = itemsControl.ItemContainerGenerator.ContainerFromItem(tal);
                                    if (vi is ContentPresenter cp && cp.ContentTemplate?.FindName("switcher", cp) is ToggleSwitch tg)
                                    {
                                        tg.IsEnabled = false;
                                        tg.Tag = "ccv";
                                        if (Environment.OSVersion.Version.Major is 10)
                                        {
                                            Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Remove(tunnel.Name);
                                        }
                                        tg.IsOn = false;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                    _ = event_RefreshTunnelsCollectionCommand.ExecuteAsync(null);
                }
            });
        }

        public HashSet<int> OnlineTunnels
        {
            get
            {
                if (App.Current is { MainWindow: { DataContext: ViewModels.MainViewModel mv} })
                {
                    return mv.OnlineTunnels;
                }
                global::System.Diagnostics.Debugger.Break();
                throw new KeyNotFoundException();
            }
        }

        [RelayCommand]
        private void @event_CopyConnectUrl(ModernWpf.Controls.HyperlinkButton hlb)
        {
            if (hlb.DataContext is Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
            {
                try
                {
                    Clipboard.SetText(tunnel.ConnectAddress);

                    hlb.Dispatcher.Invoke(async () =>
                    {
                        hlb.Tag = "success";
                        await Task.Delay(1000);
                        hlb.ClearValue(FrameworkElement.TagProperty);
                    });
                }
                catch (Exception ex)
                {
                    _ = ex;
                }
            }
        }

        [RelayCommand]
        private async Task @event_RefreshTunnelsCollection()
        {
            Response = await AppNetwork.OpenFrp.GetUserTunnels();

            if (Response.StatusCode is System.Net.HttpStatusCode.OK && Response.Exception is null &&
                Response.Data is { List: var list } && list is not null)
            {
                Tunnels = new ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel>();

                if (App.Current is { Dispatcher: var dispatcher })
                {
                    await Task.Delay(200);

                    _ = dispatcher.Invoke(async () =>
                    {
                        foreach (var tunnel in list)
                        {
                            Tunnels.Add(tunnel);

                            await Task.Delay(50);
                        }
                    }, priority: System.Windows.Threading.DispatcherPriority.Background);

                    _ = dispatcher.Invoke(async () =>
                    {
                        var resp = await RpcManager.SyncAsync();

                        if (resp.IsSuccess)
                        {
                            OnlineTunnels.Clear();
                            OnPropertyChanged(nameof(OnlineTunnels));
                            foreach (var item in resp.Data?.TunnelId ?? new Google.Protobuf.Collections.RepeatedField<int>())
                            {
                                OnlineTunnels.Add(item);
                            }
                            OnPropertyChanged(nameof(OnlineTunnels));
                        }
                    }, priority: System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        [RelayCommand]
        private async Task @event_EditTunnel(Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
        {
            try
            {
                var dialog = new Dialog.EditTunnelDialog
                {

                };
                dialog.SetValue(Controls.TunnelEditor.TunnelProperty, tunnel.CloneUserTunnel());
                dialog.SetValue(Controls.TunnelEditor.IsCreateModeProperty, false);

                if (await dialog.WaitForFinishAsync())
                {
                    _ = event_RefreshTunnelsCollectionCommand.ExecuteAsync(null);
                }
            }
            catch
            {

            }
        }

        [RelayCommand]
        private void @event_DeleteTunnel(Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
        {
            if (App.Current is { Dispatcher: var dispatcher } && dispatcher is not null &&
                itemsControl != null)
            {
                itemsControl.IsEnabled = false;
                dispatcher.Invoke(async () =>
                {
                    var response = await AppNetwork.OpenFrp.RemoveUserTunnel(tunnel.Id);
                    if (response.StatusCode is System.Net.HttpStatusCode.OK && "操作成功".Equals(response.Message))
                    {
                        Tunnels?.Remove(tunnel);
                    }
                    itemsControl.IsEnabled = true;
                    if (Tunnels?.Count is 0 && Response?.Data is not null)
                    {
                        Response.Data.Total = 0;
                        OnPropertyChanged(nameof(Response));
                    }
                });
            }
        }

        [RelayCommand]
        private void @event_ItemsContainerLoaed(RoutedEventArgs arg)
        {
            if (arg.Source is ItemsControl a)
            {
                itemsControl = a;
            }
        }

        [RelayCommand]
        private void @event_ToggleSwitchLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is ToggleSwitch switcher && switcher.DataContext is Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
            {
                switcher.Toggled += async delegate
                {
                    if (!switcher.IsEnabled)
                    {
                        if ("ccv".Equals(switcher.Tag)) switcher.IsEnabled = true;
                        switcher.ClearValue(FrameworkElement.TagProperty);
                        return;
                    }

                    switcher.IsEnabled = false;
                    if (switcher.IsOn)
                    {
                        var resp = await RpcManager.LaunchTunnel(tunnel); 
                        if (!resp.IsSuccess)
                        {
                            switcher.IsOn = false;
                            MessageBox.Show(resp.Message, "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            OnlineTunnels.Add(tunnel.Id);
                        }
                        switcher.IsEnabled = true;
                    }
                    else
                    {
                        var resp = await RpcManager.CloseTunnel(tunnel);
                        if (!resp.IsSuccess)
                        {
                            switcher.IsOn = false;
                            MessageBox.Show(resp.Message, "OpenFrp Launcher",MessageBoxButton.OK,MessageBoxImage.Warning);
                        }
                        else
                        {
                            try
                            {
                                if (Environment.OSVersion.Version.Major is 10 && Launcher.Properties.Settings.Default.NotifyMode is Model.NotifyMode.Toast)
                                {
                                    Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Remove(tunnel.Name);
                                }
                                else if (Launcher.Properties.Settings.Default.NotifyMode is Model.NotifyMode.NotifyIcon)
                                {
                                    App.TaskBarIcon.ClearNotifications();
                                }
                            }
                            catch { }

                            OnlineTunnels.Remove(tunnel.Id);
                        }
                        switcher.IsEnabled = true;
                    }
                };
            }
        }

        [RelayCommand]
        private void @event_GoToCreatePage()
        {
            WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(typeof(Views.CreateTunnel)));
            //if (App.Current is { MainWindow.DataContext: ViewModels.MainViewModel mv })
            //{
            //    mv.NavigationView.SelectedItem = mv.NavigationView.MenuItems[2];
            //}
        }

        private ItemsControl? itemsControl;

        [ObservableProperty]
        private Awe.Model.ApiResponse<Awe.Model.OpenFrp.Response.Data.UserTunnelData>? response;

        [ObservableProperty]
        private ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel>? tunnels;
    }
}
