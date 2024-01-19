using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenFrp.Launcher.Controls;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class TunnelsViewModel : ObservableObject
    {
        public TunnelsViewModel()
        {
            WeakReferenceMessenger.Default.UnregisterAll(nameof(TunnelsViewModel));

            WeakReferenceMessenger.Default.Register<Tuple<string,object?>>(nameof(TunnelsViewModel), async (_, data) =>
            {
                switch (data.Item1)
                {
                    case "refresh":
                        {
                            await event_RefreshUserTunnelCommand.ExecuteAsync(null);
                            break;
                        }
                    case "openfrp.app.closeProcessMainly" when (data.Item2 is Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel):
                        {
                            OnlineTunnels.Remove(tunnel.Id);
                            if (itemsControl is { } itemsCont)
                            {
                                foreach (Awe.Model.OpenFrp.Response.Data.UserTunnel item in itemsCont.ItemContainerGenerator.Items)
                                {
                                    if (item.Id.Equals(tunnel.Id))
                                    {
                                        var app = itemsCont.ItemContainerGenerator.ContainerFromItem(item);
                                        if (app is ContentPresenter cp && cp?.ContentTemplate?.FindName("tg", cp) is Awe.UI.Controls.ToggleSwitch ts)
                                        {
                                            ts.IsChecked = false;
                                        }
                                        break;
                                    }
                                }
                            }
                            ;break;
                        }
                }
            });
        }

        public Awe.Model.OpenFrp.Response.Data.UserInfo UserInfo
        {
            get
            {
                if (App.Current?.MainWindow is { DataContext: ViewModels.MainViewModel mv })
                {
                    return mv.UserInfo;
                }
                global::System.Diagnostics.Debugger.Break();

                throw new NullReferenceException();
            }
        }

        [ObservableProperty]
        private ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel> userTunnels = new ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel>();

        public List<int> OnlineTunnels
        {
            get
            {
                if (App.Current?.MainWindow is { DataContext: ViewModels.MainViewModel mv })
                {
                    return mv.OnlineTunnels;
                }
                throw new NullReferenceException();
            }
        }

        [ObservableProperty]
        private Awe.Model.ApiResponse? tunnelResponse;

        [ObservableProperty]
        private bool requireDisplayData = false;

        [ObservableProperty]
        private bool displayError;

        private ItemsControl? itemsControl;

        [RelayCommand]
        private async Task @event_PageLoaded()
        {
            await event_RefreshUserTunnelCommand.ExecuteAsync(null);
            //var resp = await OpenFrp.Service.Net.OpenFrp.GetUserTunnels();
            //if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Exception is null &&
            //    resp.Data is not null)
            //{

            //}
            //await Task.CompletedTask;
        }

        [RelayCommand]
        private void @event_SwitchLoaded(RoutedEventArgs e)
        {
            if (e is { Source : Awe.UI.Controls.ToggleSwitch sw } && sw.DataContext is Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
            {
                if (UserInfo.UserToken.IsNotNullOrEmpty()) 
                {
                    string token = UserInfo.UserToken!.ToString();
                    var bf = JsonSerializer.Serialize(tunnel);
                    var st = false;

                    sw.Toggled += async delegate
                    {
                        if (st is true) { st = false; return; }
                        sw.IsEnabled = false;

                        var rrpc = (default(Service.Proto.Response.TunnelResponse), default(Exception));
                        if (sw.IsChecked is true)
                        {
                            rrpc = await ExtendMethod.RunWithTryCatch(async () => await App.RemoteClient.LaunchAsync(new Service.Proto.Request.TunnelRequest
                            {
                                UserToken = token,
                                UserTunnelJson = bf
                            }));
                        }
                        else
                        {
                            rrpc = await ExtendMethod.RunWithTryCatch(async () => await App.RemoteClient.CloseAsync(new Service.Proto.Request.TunnelRequest
                            {
                                UserToken = token,
                                UserTunnelJson = bf
                            }));
                        }

                        if (rrpc is (var data,var ex))
                        {
                            if (data is not null && data.Flag)
                            {
                                if (sw.IsChecked is true)
                                {
                                    WeakReferenceMessenger.Default.Send(tunnel);
                                }
                                else
                                {
                                    OnlineTunnels.Remove(tunnel.Id);
                                }
                            }
                            else
                            {
                                st = true;
                                sw.IsChecked = !sw.IsChecked;
                            }
                        }
                        await Task.Delay(250);
                        sw.IsEnabled = true;
                    };
                    return;
                }
                global::System.Diagnostics.Debugger.Break();
            }
        }

        [RelayCommand]
        private async Task @event_EditTunnel(Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
        {
            var dialog = new Dialog.MessageDialog
            {
                Title = new TextBlock()
                {
                    Inlines =
                    {
                        "编辑隧道",
                        CreateObject<Run>((run) =>
                        {
                            run.SetResourceReference(Run.FontFamilyProperty, "Montserrat");
                            run.FontWeight = FontWeight.FromOpenTypeWeight(500);
                        }, $" #{tunnel.Id} {tunnel.Name} ")
                    },
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontSize = 24
                },
                PrimaryButtonText = "编辑数据",
                PrimaryButtonIcon = CreateObject<Path>(x =>
                {
                    x.SetResourceReference(Path.DataProperty, "Awe.UI.Icons.Edit");
                    x.SetBinding(Path.FillProperty, new Binding
                    {
                        Mode = BindingMode.OneWay,
                        RelativeSource = RelativeSource.Self,
                        Path = new PropertyPath(TextElement.ForegroundProperty)
                    });
                    //x.Fill = new SolidColorBrush { Color = Colors.Red };
                    x.Stretch = Stretch.Uniform;
                    x.Margin = new Thickness(0, 0, 4, 0);
                    x.Width = x.Height = 16;
                }),
                CloseButtonText = "取消",
                Content = new Controls.TunnelConfig()
                {
                    TunnelData = tunnel.CloneUserTunnel(),
                    IsCreateMode = false,
                },
                InvokeAction = async (dialog, cancellationToken) =>
                {
                    if (dialog.Content is TunnelConfig { } tConfig && tConfig.GetConfig() is { } wt)
                    {
                        if (wt.Name is null || wt.Name.Length is 0)
                        {
                            wt.Name = $"ofPr_{Guid.NewGuid()}".Substring(0, 12);

                            tConfig.UpdateTunnelName();
                        }
                        
                        var resp = await Service.Net.OpenFrp.EditUserTunnel(wt);

                        if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Exception is null)
                        {
                            await this.event_RefreshUserTunnelCommand.ExecuteAsync(null);

                            return true;
                        }
                        // message :::
                        if (dialog.Description is TextBlock tb)
                        {
                            tb.Text = resp.Message;
                        }
                    }
                    return false;
                },
            };
            dialog.Description = CreateObject<TextBlock>((x) =>
            {
                x.SetResourceReference(TextBlock.ForegroundProperty, "WarningOrErrorBrush");
            });

            var result = await dialog.ShowDialog();
        }

        [RelayCommand]
        private void @event_DeleteTunnel(Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
        {
            if (App.Current?.MainWindow is { } wind)
            {
                var flyoutContent = new Controls.DeleteFlyoutContent
                {
                    UserTunnel = tunnel,
                    Description = CreateObject<TextBlock>((x) =>
                    {
                        x.FontSize = 16;
                        x.SetResourceReference(TextBlock.ForegroundProperty, "WarningOrErrorBrush");
                    }),
                };
                var flyout = new Awe.UI.Controls.Flyout
                {
                    Padding = new Thickness(16, 16, 16, 16),
                    Content = flyoutContent
                };

                Keyboard.ClearFocus();

                flyoutContent.InvokeAction = async () =>
                {
                    var resp = await Service.Net.OpenFrp.RemoveUserTunnel(tunnel.Id);

                    if ("未找到指定的隧道".Contains(resp.Message) || (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Exception is null))
                    {
                        UserTunnels.Remove(tunnel);
                        Awe.UI.Helper.FlyoutHelper.RemoveAllMask();
                    }

                    // message :::
                    if (flyoutContent.Description is TextBlock tb)
                    {
                        tb.Text = resp.Message;
                    }
                };
                Awe.UI.Helper.FlyoutHelper.CreateMask(
                    flyout,
                    () =>
                    {
                        return new Point(
                            (wind.ActualWidth / 2) - (flyout.ActualWidth / 2),
                            (wind.ActualHeight - flyout.ActualHeight) - 16
                        );
                    },
                    (container) =>
                    {
                        container.BeginAnimation(ContentControl.MarginProperty, new ThicknessAnimation
                        {
                            Duration = TimeSpan.FromMilliseconds(300),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                            To = new Thickness(0),
                            From = new Thickness(0,16,0,0)
                        });
                    }
                );
            }
            //var dialog = new Dialog.MessageDialog
            //{
            //    Title = new TextBlock()
            //    {
            //        Inlines =
            //        {
            //            "删除隧道",
            //            CreateObject<Run>((run) =>
            //            {
            //                run.SetResourceReference(Run.FontFamilyProperty, "Montserrat");
            //                run.FontWeight = FontWeight.FromOpenTypeWeight(500);
            //            }, $" #{tunnel.Id} {tunnel.Name} ")
            //        },
            //        TextTrimming = TextTrimming.CharacterEllipsis,
            //        FontSize = 24
            //    },
            //    PrimaryButtonText = "确定删除",
            //    PrimaryButtonIcon = CreateObject<Path>(x =>
            //    {
            //        x.SetResourceReference(Path.DataProperty, "Awe.UI.Icons.Delete");
            //        x.SetBinding(Path.FillProperty, new Binding
            //        {
            //            Mode = BindingMode.OneWay,
            //            RelativeSource = RelativeSource.Self,
            //            Path = new PropertyPath(TextElement.ForegroundProperty)
            //        });
            //        //x.Fill = new SolidColorBrush { Color = Colors.Red };
            //        x.Stretch = Stretch.Uniform;
            //        x.Margin = new Thickness(0, 0, 4, 0);
            //        x.Width = x.Height = 16;
            //    }),
            //    Content = new TextBlock()
            //    {
            //        FontSize = 16,
            //        TextWrapping = TextWrapping.Wrap,
            //        Text = "确定要删除该隧道吗？会失去真的很久很久(是永久)哦。"
            //    },
            //    CloseButtonText = "取消",
            //    InvokeAction = async (dialog,cancellationToken) =>
            //    {
            //        var resp = await Service.Net.OpenFrp.RemoveUserTunnel(tunnel.Id, cancellationToken);

            //        if ("提交的数据有误".Contains(resp.Message) || (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Exception is null))
            //        {
            //            return true;
            //        }

            //        // message :::
            //        if (dialog.Description is TextBlock tb)
            //        {
            //            tb.Text = resp.Message;
            //        }
            //        return false;
            //    },
            // };

            //dialog.Description = CreateObject<TextBlock>((x) =>
            //{
            //    x.SetResourceReference(TextBlock.ForegroundProperty, "WarningOrErrorBrush");
            //});

            //var result = await dialog.ShowDialog();
            //if (result is Dialog.MessageDialogResult.Primary)
            //{
            //    UserTunnels.Remove(tunnel);
            //}
        }




        [RelayCommand]
        private async Task @event_RefreshUserTunnel()
        {
            TunnelResponse = null;
            RequireDisplayData = false;

            var resp = await Service.Net.OpenFrp.GetUserTunnels();

            if (resp.Exception is null && resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { List: var list})
            {
                UserTunnels.Clear();

                if (App.Current?.MainWindow is { Dispatcher: var dis })
                {
                    _ = dis.Invoke(async () =>
                    {
                        UserTunnels.Clear();
                        RequireDisplayData = true;
                        foreach (var item in resp.Data!.List!)
                        {
                            UserTunnels.Add(item);

                            //OnPropertyChanged(nameof(UserTunnels));

                            await Task.Delay(20);
                        }
                        OnPropertyChanged(nameof(UserTunnels));
                    }, priority: System.Windows.Threading.DispatcherPriority.Background);
                }

                
            }
            else
            {
                await Task.Delay(100);
                TunnelResponse = resp;
                //global::System.Diagnostics.Debugger.Break();
            }
        }

        [RelayCommand]
        private void @event_ChangeVisibilityForException() => DisplayError = !DisplayError;

        [RelayCommand]
        private void @event_ToCreateTunnelPage() => WeakReferenceMessenger.Default.Send(typeof(Views.CreateTunnel));

        [RelayCommand]
        private void @event_ItemsControlLoaded(RoutedEventArgs e)
        {
            if (e.Source is ItemsControl ic) { itemsControl = ic; }
        }

        private T CreateObject<T>(Action<T>? func = default, params object[] args)
        {
            var vc = Activator.CreateInstance(typeof(T),args);

            if (vc is null) throw new NullReferenceException();
            else if (vc is T tt)
            {
                if(func is not null) { func(tt); }
                return tt;
            }
            throw new TypeLoadException();
        }
    }
}
