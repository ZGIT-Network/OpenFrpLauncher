using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenFrp.Launcher.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class TunnelsViewModel : ObservableObject
    {
        public TunnelsViewModel()
        {
        }

        public Awe.Model.OpenFrp.Response.Data.UserInfo UserInfo
        {
            get
            {
                if (App.Current?.MainWindow is { DataContext: ViewModels.MainViewModel mv })
                {
                    return mv.UserInfo;
                }
                throw new NullReferenceException();
            }
        }

        public ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel> UserTunnels
        {
            get
            {
                if (App.Current?.MainWindow is { DataContext: ViewModels.MainViewModel mv })
                {
                    return mv.UserTunnels;
                }
                throw new NullReferenceException();
            }
            set
            {
                if (App.Current?.MainWindow is { DataContext: ViewModels.MainViewModel mv,Dispatcher: var dis })
                {
                    dis.Invoke(async () =>
                    {
                        mv.UserTunnels.Clear();

                        foreach (var item in value)
                        {
                            mv.UserTunnels.Add(item);

                            await Task.Delay(20);
                        }
                    }, priority: System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }


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


        [RelayCommand]
        private async Task @event_PageLoaded()
        {
            await Task.CompletedTask;
        }

        [RelayCommand]
        private void @event_SwitchLoaded(RoutedEventArgs e)
        {
            if (e is { Source : Awe.UI.Controls.ToggleSwitch sw } && sw.DataContext is Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
            {
                if (UserInfo.UserToken.IsNotNullOrEmpty()) 
                {
                    string token = UserInfo.UserToken!.ToString();
                    var bf = JsonSerializer.SerializeToUtf8Bytes(tunnel);

                    sw.Toggled += async delegate
                    {
                        sw.IsEnabled = false;

                        var rrpc = (default(Service.Proto.Response.TunnelResponse), default(Exception));
                        if (sw.IsChecked is true)
                        {
                            rrpc = await ExtendMethod.RunWithTryCatch(async () => await App.RemoteClient.LaunchAsync(new Service.Proto.Request.TunnelRequest
                            {
                                UserToken = token,
                                UserTunnelJson =
                                {
                                    Google.Protobuf.ByteString.CopyFrom(bf)
                                }
    
                            }));
                        }
                        else
                        {
                            rrpc = await ExtendMethod.RunWithTryCatch(async () => await App.RemoteClient.CloseAsync(new Service.Proto.Request.TunnelRequest
                            {
                                UserToken = token,
                                UserTunnelJson =
                                {
                                    Google.Protobuf.ByteString.CopyFrom(bf)
                                }
                            }));
                        }

                        if (rrpc is (var data,var ex))
                        {
                            if (ex is not null)
                            {
                                sw.IsChecked = !sw.IsChecked;
                            }
                            else if (data is not null && data.Flag)
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
                    if (dialog.Content is TunnelConfig { TunnelData: var wt })
                    {
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
                x.SetBinding(TextBlock.ForegroundProperty, new Binding
                {
                    Mode = BindingMode.OneWay,
                    Converter = new Awe.UI.Converter.EqualConverter()
                    {
                        TrueResult = Color.FromRgb(230, 0, 0),
                        FalseResult = Colors.Yellow
                    },

                    Path = new PropertyPath(Awe.UI.Helper.WindowsHelper.UseLightModeProperty),
                    Source = dialog
                });
            });

            var result = await dialog.ShowDialog();
        }

        [RelayCommand]
        private async Task @event_DeleteTunnel(Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel)
        {
            var dialog = new Dialog.MessageDialog
            {
                Title = new TextBlock()
                {
                    Inlines =
                    {
                        "删除隧道",
                        CreateObject<Run>((run) =>
                        {
                            run.SetResourceReference(Run.FontFamilyProperty, "Montserrat");
                            run.FontWeight = FontWeight.FromOpenTypeWeight(500);
                        }, $" #{tunnel.Id} {tunnel.Name} ")
                    },
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontSize = 24
                },
                PrimaryButtonText = "确定删除",
                PrimaryButtonIcon = CreateObject<Path>(x =>
                {
                    x.SetResourceReference(Path.DataProperty, "Awe.UI.Icons.Delete");
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
                InvokeAction = async (dialog,cancellationToken) =>
                {
                    var resp = await Service.Net.OpenFrp.RemoveUserTunnel(tunnel.Id, cancellationToken);

                    if ("提交的数据有误".Contains(resp.Message) || (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Exception is null))
                    {
                        return true;
                    }

                    // message :::
                    if (dialog.Description is TextBlock tb)
                    {
                        tb.Text = resp.Message;
                    }
                    return false;
                },
             };

            dialog.Description = CreateObject<TextBlock>((x) =>
            {
                x.SetBinding(TextBlock.ForegroundProperty, new Binding
                {
                    Mode = BindingMode.OneWay,
                    Converter = new Awe.UI.Converter.EqualConverter()
                    {
                        TrueResult = Color.FromRgb(230,0,0),
                        FalseResult = Colors.Yellow
                    },
                    
                    Path = new PropertyPath(Awe.UI.Helper.WindowsHelper.UseLightModeProperty),
                    Source = dialog
                });
            });
            dialog.Content = CreateObject<TextBlock>((tb) =>
            {
                tb.FontSize = 16;
                tb.TextWrapping = TextWrapping.Wrap;
                tb.Text = "确定要删除该隧道吗？会失去真的很久很久(是永久)哦。";
                tb.SetBinding(TextBlock.ForegroundProperty, new Binding
                {
                    Mode = BindingMode.OneWay,
                    Source = dialog,
                    Path = new PropertyPath(Dialog.MessageDialog.ForegroundProperty)
                });
            });

            var result = await dialog.ShowDialog();
            if (result is Dialog.MessageDialogResult.Primary)
            {
                UserTunnels.Remove(tunnel);
            }
        }

        [RelayCommand]
        private async Task @event_RefreshUserTunnel()
        {
            var resp = await Service.Net.OpenFrp.GetUserTunnels();

            if (resp.Exception is null && resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { List: var list})
            {
                UserTunnels = new(list);
                
                OnPropertyChanged(nameof(UserTunnels));
            }
            else
            {
                global::System.Diagnostics.Debugger.Break();
            }
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
