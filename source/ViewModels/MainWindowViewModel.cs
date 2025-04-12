using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {


            WeakReferenceMessenger.Default.UnregisterAll(nameof(MainWindowViewModel));

            WeakReferenceMessenger.Default.Register<Model.RouteMessage<MainWindowViewModel, Yue3.Model.OpenFrp.Response.Data.UserInfoData>>(nameof(MainWindowViewModel), (_, message) =>
            {
                if (message.Data is not null)
                {
                    UserInfo = new Model.UserInfo(message.Data);
                }
            });
            WeakReferenceMessenger.Default.Register<Model.RouteMessage<MainWindowViewModel, Model.UserInfo>>(nameof(MainWindowViewModel), (_, message) =>
            {
                if (message.Data is not null)
                {
                    UserInfo = message.Data;
                }
            });
            WeakReferenceMessenger.Default.Register<Model.RouteMessage<MainWindowViewModel, string>>(nameof(MainWindowViewModel), (_, message) =>
            {
                switch (message)
                {
                    case "processExit":
                        {
                            IsUserLogon = false;

                            App.Current.Dispatcher.Invoke(() =>
                            {
                                if (App.Current.MainWindow is MainWindow { FrameContentViewModel: ViewModels.SettingsViewModel svm })
                                {
                                    svm.event_RefreshUserInfoCommand.Cancel();
                                    svm.event_CallUpLoginWindowCommand.Cancel();
                                }
                                LogsCache?.Clear();
                            });

                            conve_CreateNotificationStreamCommand.Cancel();

                            conve_RetryConnectRpcCommand.Cancel();

                            goto case "processOffline";
                        };
                    case "processLfec":
                        {
                            //if (SettingsViewModel.__userInfo_Defualt.Equals(UserInfo))
                            //{
                            //    return;
                            //}
                            conve_RetryConnectRpcCommand.Execute(default);
                        };break;
                    case "processOnline":
                        {
                            IsDaemonConnected = true;
                        };break;
                    case "processOffline":
                        {
                            IsDaemonConnected = false;
                        }; break;
                }
            });


            if (App.Current is { MainWindow: MainWindow mainWind })
            {
                ShowAlertAction = mainWind.ShowAlert;
            }
            else
            {
                throw new NotSupportedException(App.Current.MainWindow.GetType().ToString());
            }
        }

        internal MainWindowViewModel(Yue3.Model.OpenFrp.Response.Data.UserInfo userInfo) : this()
        {
            UserInfo = new Model.UserInfo(userInfo);

            IsDaemonConnected = true;

            conve_CreateNotificationStreamCommand.Execute(default);
        }

        private readonly Action<string,string,InfoBarSeverity> ShowAlertAction;

        private iNKORE.UI.WPF.Modern.Controls.Frame? frame;
        private iNKORE.UI.WPF.Modern.Controls.NavigationView? navigationView;

        [ObservableProperty]
        private bool isDaemonConnected;

        public bool IsUserLogon
        {
            get => !UserInfo.Equals(SettingsViewModel.__userInfo_Defualt);
            private set
            {
                if (!value)
                {
                    UserInfo = SettingsViewModel.__userInfo_Defualt;
                }
            }
        }

        [ObservableProperty,NotifyPropertyChangedFor(nameof(IsUserLogon))]
        private Model.UserInfo userInfo = SettingsViewModel.__userInfo_Defualt;

        [ObservableProperty]
        private ObservableCollection<Service.Proto.Response.LogStreamResponse.Types.LogContainer>? logsCache;

        public Google.Protobuf.Collections.MapField<int, int> KnownLogIndexMapping { get; set; } = new Google.Protobuf.Collections.MapField<int, int> { };

        [RelayCommand]
        private void @event_NavigationViewLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is iNKORE.UI.WPF.Modern.Controls.NavigationView navigationView)
            {
                this.navigationView = navigationView;
                navigationView.ItemInvoked += (_, e) =>
                {
                    if (frame is null) return;
                    if (e.IsSettingsInvoked)
                    {
                        frame.Navigate(typeof(Views.Settings),"byInvoked");
                        return;
                    }
                    if (e.InvokedItemContainer is NavigationViewItem { Tag: Type { Namespace: not null or "" } page })
                    {

                        if (frame.Content?.GetType() == page) return;

                        if (page.Namespace.StartsWith("OpenFrp.Launcher.Views"))
                        {
                            frame.Navigate(page, "byInvoked");
                        }
                        else
                        {
                            throw new UnauthorizedAccessException(page.ToString());
                        }
                    }
                };
            }
        }

        [RelayCommand]
        private void @event_FrameLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is iNKORE.UI.WPF.Modern.Controls.Frame frame)
            {
                this.frame = frame;

                frame.Navigated += (_, e) =>
                {
                    if (e.ExtraData is "byInvoked") return;


                };
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_RetryConnectRpc(CancellationToken cancellationToken)
        {
            App.LaunchRpcProcess(out var manager);

            await App.WaitForProcessLaunch(cancellationToken);

            OpenFrp.Service.Proto.RpcResponse<Service.Proto.Response.SyncResponse>? r1_resp = default;

            for (int i = 0; i < 5; i++)
            {
                r1_resp = await manager.Sync(cancellationToken);

                if (r1_resp.Flag)
                {
                    IsDaemonConnected = true;
                    break;
                }
                await Task.Delay(500, cancellationToken);
            }
            if (r1_resp is null || !r1_resp.Flag)
            {
                ShowAlert("Failed to sync", r1_resp?.Message ?? "未知原因", InfoBarSeverity.Error);
            }
            if (r1_resp is not { Data: Service.Proto.Response.SyncResponse srp_sr })
            {
                return;
            }
            var n1_resp = await Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);
            if (n1_resp.StatusCode is not System.Net.HttpStatusCode.OK || n1_resp.Data is not { } data)
            {
                return;
            }
            UserInfo = new Model.UserInfo(data);

            var r2_resp = await manager.Login(new Service.Proto.Request.LoginRequest { UserInfomationJson = Google.Protobuf.ByteString.CopyFrom(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data)) }, cancellationToken);

            if (r2_resp is { })
            {
                conve_CreateNotificationStreamCommand.Execute(default);
                // success;
            }
            return;
        }


        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_CreateNotificationStream(CancellationToken cancellationToken)
        {
            App.LaunchRpcProcess(out var rpcManager);

            await rpcManager.NotificationStream(NotificationReader, cancellationToken);
        }
        private void NotificationReader(Service.Proto.Response.NotificationStreamResponse response)
        {
            switch (response.State)
            {
                case Service.Proto.Response.NotificationStreamResponse.Types.NotificationStreamResponseState.LaunchSuccess:
                    {
                        if (response.Data.TryUnpack<Service.Proto.Response.NotificationStreamResponse.Types.LaunchSuccessMsg>(out var msg))
                        {
                            string[] addresses = msg.ConnectAddresses.ToArray();

                            var sb = new StringBuilder();

                            if (msg.TunnelType.Contains("HTTP"))
                            {
                                foreach (var item in msg.ConnectAddresses)
                                {
                                    sb.Append(item + ",");
                                }
                            }

                            new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                           .AddText($"隧道 {msg.TunnelName} 启动成功!", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                           .AddText($"点击\"复制按钮\"复制链接地址,开始你的映射之旅吧。")
                                           .AddText($"可用地址: {(msg.TunnelType.Contains("HTTP") ? sb.ToString().Remove(sb.Length - 1) : addresses.First())}" + ("HTTP".Contains(msg.TunnelType) ? "\n注: 请先将该上列域名解析到对应节点的地址。" : ""))
                                           .AddAttributionText($"{msg.TunnelType.ToUpper()} {msg.Host}:{msg.Port}")
                                           .AddButton("复制链接", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, $"copy {(msg.HasExtraConnectAddress ? msg.ExtraConnectAddress : addresses.First())}")
                                           .AddButton("确定", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, "none")
                                           .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                           .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                           .Show(toast =>
                                           {
                                               toast.Tag = msg.TunnelName;
                                               try { toast.ExpiresOnReboot = true; }
                                               catch { }
                                               toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                           });
                        }
                    };break;
                case Service.Proto.Response.NotificationStreamResponse.Types.NotificationStreamResponseState.Messaging:
                    {
                        if (response.Data.TryUnpack<Service.Proto.Response.NotificationStreamResponse.Types.UIWarningNotice>(out var warning))
                        {
                            ShowAlert(warning.Title, warning.Data, InfoBarSeverity.Warning);
                        }
                    };break;
            }
        }

        public void ShowAlert(string title, string message, InfoBarSeverity severity) => ShowAlertAction.Invoke(title,message,severity);
    }
}
