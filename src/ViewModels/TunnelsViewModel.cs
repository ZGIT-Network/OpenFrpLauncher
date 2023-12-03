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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

                                    Debug.WriteLine("addd");
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
                    FontSize = 24
                },
                Content = CreateObject<TextBlock>((tb) =>
                {
                    tb.FontSize = 16;
                    tb.Text = "确定要删除该隧道吗？会失去真的很久很久(是永久)哦。";
                    tb.SetBinding(TextBlock.ForegroundProperty, new Binding
                    {
                        Mode = BindingMode.OneWay,
                        RelativeSource = RelativeSource.TemplatedParent,
                        Path = new PropertyPath(TextElement.ForegroundProperty)
                    });
                })
            };

            var result = await dialog.ShowDialog();
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
