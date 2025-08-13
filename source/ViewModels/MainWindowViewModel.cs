using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Google.Rpc.Context;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFrp.Launcher.Model;
using OpenFrp.Launcher.Rpc;


namespace OpenFrp.Launcher.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            Logger = App.ServiceProvider.GetRequiredService<ILogger<MainWindowViewModel>>();

            RpcManager = App.ServiceProvider.GetRequiredService<RpcManager>();
            DaemonManager = App.ServiceProvider.GetRequiredService<DaemonManager>();
            FrpcManager = App.ServiceProvider.GetRequiredService<Service.Manager.Frpc.FrpcManager>();

            var appLogContainer = App.ServiceProvider.GetRequiredService<AppLogContainer>();

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

                    if (!UserInfo.Equals(SettingsViewModel.__userInfo_Defualt))
                    {
                        if (UserAvatorSource is null && !event_LoadUserAvatorCommand.IsRunning)
                        {
                            event_LoadUserAvatorCommand.Execute(default);
                        }
                    }
                    else
                    {
                        UserAvatorSource = null;
                        event_LoadUserAvatorCommand.Cancel();
                        conve_TryAutoLaunchTunnelCommand.Cancel();
                    }
                }
            });
            WeakReferenceMessenger.Default.Register<Model.RouteMessage<MainWindowViewModel, Type>>(nameof(MainWindowViewModel), (_, message) =>
            {
                if (message.Data is { Namespace: not null } tp && tp.Namespace.StartsWith("OpenFrp.Launcher.Views"))
                {
                    if (frame is { HasContent: true,Content: var content } && content.GetType().Equals(tp))
                    {
                        switch (content)
                        {
                            case Views.Tunnels {DataContext: ViewModels.TunnelsViewModel vtv }:
                                {
                                    vtv.event_RefreshUserTunnelCommand.Execute(null);
                                    if (this.IsUrlSchemeRegistered && App.StartupArguments.LastOrDefault() is { Length: > 0 } pt && pt.StartsWith("openfrp://"))
                                    {
                                        vtv.LaunchWithFastLink(pt);
                                    }
                                }
                                ;break;
                        }
                        return;
                    }
                    if (tp.Equals(typeof(Views.Tunnels)))
                    {
                        if (frame is { Content : FrameworkElement { DataContext: ViewModels.SettingsViewModel svm} })
                        {
                            svm.event_CallUpLoginWindowCommand.Cancel();
                        }
                    }
                    frame?.Dispatcher.Invoke(() => frame?.Navigate(tp));
                }
            });
            WeakReferenceMessenger.Default.Register<Model.RouteMessage<MainWindowViewModel, string>>(nameof(MainWindowViewModel), (_, message) =>
            {
                switch (message)
                {
                    case "processExit":
                        {
                            IsUserLogon = false;

                            if (frame is { Content: not Views.Settings })
                            {
                                frame.Navigate(typeof(Views.Settings));
                            }

                            App.Current.Dispatcher.Invoke(() =>
                            {
                                if (App.Current.MainWindow is MainWindow { FrameContentViewModel: ViewModels.SettingsViewModel svm })
                                {
                                    svm.event_RefreshUserInfoCommand.Cancel();
                                    svm.event_CallUpLoginWindowCommand.Cancel();
                                }
                                appLogContainer.LogsCache?.Clear();
                            });

                            conve_CreateNotificationStreamCommand.Cancel();

                            conve_RetryConnectRpcCommand.Cancel();

                            goto case "processOffline";
                        };
                    case "processLfec":
                        {
                            conve_RetryConnectRpcCommand.Execute(default);
                        };break;
                    case "processOnline":
                        {
                            IsDaemonConnected = true;
                        };break;
                    case "processOffline":
                        {
                            WeakReferenceMessenger.Default.Send(Model.RouteMessage<TunnelsViewModel>.Create("processOffline"));

                            IsDaemonConnected = false;
                        }; break;
                }
            });

            ReadRegistryConfig();



        }

        internal MainWindowViewModel(bool daemonState) : this()
        {
            IsDaemonConnected = daemonState;


            conve_CreateNotificationStreamCommand.Execute(default);
            conve_CheckUpdateCommand.Execute(default);
        }

        internal MainWindowViewModel(Yue3.Model.OpenFrp.Response.Data.UserInfo userInfo) : this(true)
        {
            UserInfo = new Model.UserInfo(userInfo);

            if (!UserInfo.Equals(SettingsViewModel.__userInfo_Defualt))
            {
                if (UserAvatorSource is null && !event_LoadUserAvatorCommand.IsRunning)
                {
                    event_LoadUserAvatorCommand.Execute(default);
                }
                conve_TryAutoLaunchTunnelCommand.Execute(default);
            }

            
        }

        private Action<string,string,InfoBarSeverity> ShowAlertAction = delegate { };

        private void ReadRegistryConfig()
        {
            try
            {
                if (Microsoft.Win32.Registry.ClassesRoot.GetSubKeyNames().Contains("openfrp"))
                {
                    IsUrlSchemeRegistered = true;
                }
            }
            catch { }
        }

        private iNKORE.UI.WPF.Modern.Controls.Frame? frame;
        private iNKORE.UI.WPF.Modern.Controls.NavigationView? navigationView;

        private Rpc.DaemonManager DaemonManager { get; set; }
        private Rpc.RpcManager RpcManager { get; set; }
        private Service.Manager.Frpc.FrpcManager FrpcManager { get; set; }

        private ILogger<MainWindowViewModel> Logger { get; set; }

        internal void OnViewModelPropertyChanged(string name) => OnPropertyChanged(name);

        [ObservableProperty]
        private Model.SoftwareConfig? softwareConfig;

        [ObservableProperty,NotifyPropertyChangedFor(nameof(IsAllowToUseTunnel))]
        private bool isDaemonConnected;

        [ObservableProperty]
        private bool hasUpdate;

        [ObservableProperty]
        private Uri? userAvatorSource;

        public bool IsUserLogon
        {
            get => !UserInfo.Equals(SettingsViewModel.__userInfo_Defualt);
            private set
            {
                if (!value)
                {
                    UserInfo = SettingsViewModel.__userInfo_Defualt;
                    
                    OnPropertyChanged(nameof(IsAllowToUseTunnel));
                }
            }
        }

        public bool IsAllowToUseTunnel
        {
            get
            {
                return IsDaemonConnected && (IsUserLogon || IsUrlSchemeRegistered);
            }
        }

        private bool isUrlSchemeRegistered;
        public bool IsUrlSchemeRegistered
        {
            get => isUrlSchemeRegistered;
            set
            {
                isUrlSchemeRegistered = value;
                OnPropertyChanged(nameof(IsUrlSchemeRegistered));
                OnPropertyChanged(nameof(IsAllowToUseTunnel));
            }
        }

        [ObservableProperty,NotifyPropertyChangedFor(nameof(IsUserLogon),nameof(IsAllowToUseTunnel))]
        private Model.UserInfo userInfo = SettingsViewModel.__userInfo_Defualt;



        [RelayCommand]
        private void @event_NavigationViewLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is iNKORE.UI.WPF.Modern.Controls.NavigationView navigationView)
            {
                this.navigationView = navigationView;
                navigationView.ItemInvoked += (_, e) =>
                {
                    if (frame is null) return;
                    
                    if (e.IsSettingsInvoked || e.InvokedItemContainer is NavigationViewItem { Tag: "usrInfo" })
                    {
                        if (frame.Content is not Views.Settings)
                        {
                            Views.Settings pageObj = new Views.Settings { };
                            pageObj.Dispatcher.BeginInvoke(() =>
                            {
                                pageObj.BeginInit();
                                pageObj.EndInit();

                                frame.Navigate(pageObj);
                            }, priority: System.Windows.Threading.DispatcherPriority.Background, null);
                        }

                        return;
                    }
                    if (e.InvokedItemContainer is NavigationViewItem { Tag: Type { Namespace: not null or "" } page })
                    {
                        if (frame.Content?.GetType() == page)
                        {
                            switch (frame.Content)
                            {
                                case Views.Tunnels { DataContext: ViewModels.TunnelsViewModel tvm }:
                                    {
                                        tvm.event_RefreshUserTunnelCommand.Execute(default);
                                    };break;
                                case Views.Home { DataContext: ViewModels.HomeViewModel hvm }:
                                    {
                                        hvm.event_RefreshBroadCastCommand.Execute(default);
                                        hvm.event_RefreshUserInfoCommand.Execute(default);
                                        hvm.event_RefreshAdSenseCommand.Execute(default);
                                    };break;
                            }
                            
                            return;
                        }

                        if (page.Namespace.StartsWith("OpenFrp.Launcher.Views"))
                        {
                            System.Windows.Controls.Page? pageObj = Activator.CreateInstance(page) as System.Windows.Controls.Page;

                            if (pageObj != null) 
                            {
                                pageObj.Dispatcher.BeginInvoke(() =>
                                {
                                    pageObj.BeginInit();
                                    pageObj.EndInit();

                                    frame.Navigate(pageObj, "byInvoked");
                                }, priority: System.Windows.Threading.DispatcherPriority.Background, null);
                                
                            }
                        }
                        else
                        {
                            throw new UnauthorizedAccessException(page.ToString());
                        }
                        return;
                    }
                    //if (e.InvokedItemContainer is NavigationViewItem { Tag: "usrInfo" })
                    //{
                    //    if (UserInfo.Equals(SettingsViewModel.__userInfo_Defualt))
                    //    {
                    //        frame.Navigate(typeof(Views.Settings), "byInvoked");
                    //    }
                    //}
                };
                if (navigationView.FooterMenuItems is { Count: 1 } fot && fot[0] is NavigationViewItem nviFoot)
                {
                    nviFoot.Dispatcher.UnhandledException += (_, e) => { e.Handled = true; };
                }
            }
        }

        [RelayCommand]
        private void @event_FrameLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is iNKORE.UI.WPF.Modern.Controls.Frame frame)
            {
                this.frame = frame;


                frame.Navigating += (_, e) =>
                {
                    if (e.NavigationMode is System.Windows.Navigation.NavigationMode.Forward or System.Windows.Navigation.NavigationMode.Back)
                    {
                        e.Cancel = true;
                    }
                };
                frame.Navigated += (_, e) =>
                {
                    if (e.ExtraData is "byInvoked") return;

                    if (navigationView != null)
                    {
                        switch (e.Content)
                        {
                            case Views.Settings:
                                {
                                    navigationView.SelectedItem = navigationView.SettingsItem;
                                };break;
                            case Views.Home or Views.Settings or Views.Tunnels or Views.UpdateComment or Views.CreateTunnel:
                                {
                                    if (e.Content is Views.CreateTunnel && true)
                                    {
                                        // nothing
                                    }
                                    foreach (var mn in navigationView.MenuItems)
                                    {
                                        if (mn is NavigationViewItem tg && e.Content.GetType().Equals(tg.Tag))
                                        {
                                            navigationView.SelectedItem = tg;

                                            break;
                                        }
                                    }
                                    foreach (var mn in navigationView.FooterMenuItems)
                                    {
                                        if (mn is NavigationViewItem tg && e.Content.GetType().Equals(tg.Tag))
                                        {
                                            navigationView.SelectedItem = tg;

                                            break;
                                        }
                                    }
                                }
                                ;break;
                        }
                    }
                };
                if (IsUrlSchemeRegistered && App.StartupArguments.Count > 0 && App.StartupArguments.Any(x => x.StartsWith("openfrp://")))
                {
                    frame.Navigate(typeof(Views.Tunnels));
                }
                else
                {
                    frame.Navigate(typeof(Views.Home));
                }
            }
        }

        [RelayCommand]
        private void @event_WindowLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is MainWindow mw)
            {

                if (App.StartupArguments.Contains("--minimize") && mw.Tag is false)
                {
                    mw.HideByHANDLE();
                }
                ShowAlertAction = mw.ShowAlert;

                mw.Closing += (_, e) =>
                {
                    e.Cancel = true;
                    mw.HideByHANDLE();
                };
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_LoadUserAvator(CancellationToken cancellationToken)
        {
            // https://api.zyghit.cn/avatar/?email={email}&s=100

            int currentCount = -1;
            if (Helpers.UsrTokenService.PlatformUserCache is { Count: > 0 } r1)
            {
                for (global::System.Int32 i = 0; i < r1.Count; i++)
                {
                    if (UserInfo.Email.Equals(r1[i].EmailAddress))
                    {
                        currentCount = i;
                        if (!string.IsNullOrEmpty(r1[i].UserAvatorHash) && System.IO.File.Exists(r1[i].UserAvatorHash) && Uri.TryCreate(r1[i].UserAvatorHash, UriKind.RelativeOrAbsolute, out var neio))
                        {
                            UserAvatorSource = neio;
                        }
                        break;
                    }
                }
            }
            else
            {
                // exit here
                return;
                // ????
            }

            var resp = await OpenFrp.Service.Net.HttpClient.DefualtInstance.GetStreamAsync($"https://api.zyghit.cn/avatar/?email={UserInfo.Email}&s=100", cancellationToken);

            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { Length: > 0})
            {
                string fp = System.IO.Path.GetTempFileName();

                try
                {
                    using (var fs = System.IO.File.Create(fp))
                    {
                        fs.Position = 0L;
                        resp.Data.Position = 0L;

                        await resp.Data.CopyToAsync(fs, 81920, cancellationToken);

                        await fs.FlushAsync(cancellationToken);
                    }
                    if (Uri.TryCreate(fp,UriKind.RelativeOrAbsolute,out var uir))
                    {
                        UserAvatorSource = uir;

                        if (currentCount >= 0 && Helpers.UsrTokenService.PlatformUserCache is not null)
                        {
                            Helpers.UsrTokenService.PlatformUserCache[currentCount].UserAvatorHash = fp;
                        }

                        Helpers.UsrTokenService.SaveUser();
                    }
                }
                catch
                {

                }
            }
            else
            {
                // nothing
            }
        }

        [RelayCommand]
        private void @conve_InstallUpdate(Model.UpdateType type)
        {
            if (SoftwareConfig is not { SoftwareConfigValue: not null, SoftwareConfigValue: var software }) return;
            if (string.IsNullOrEmpty(software.Latest) || software.DownloadSources is null) return;

            string argument = $"-type {type.ToString().ToLower()}";

            switch (type)
            {
                case UpdateType.Launcher:
                    {
                        try
                        {
                            Process.Start("https://console.openfrp.net/download");
                            return;
                        }
                        catch
                        {

                        }
                        try
                        {
                            Process.Start("cmd", "/c start https://console.openfrp.net/download");
                        }
                        catch
                        {

                        }
                        return;
                    };
                case UpdateType.Frpc:
                    {
                        ShutdownApp();
#if NET
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Environment.ProcessPath,
                                Arguments = "--updateFrpClient"
                            });
                        }
                        catch
                        {
                            return;
                        }
#else
                        try
                        {
                            var self = Process.GetCurrentProcess().MainModule.FileName;

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = self,
                                Arguments = "--updateFrpClient"
                            });
                        }
                        catch
                        {
                            return;
                        }
#endif

                        
                    }
                    ; break;
            }

            //if (App.Current.MainWindow is not MainWindow mw) return;

            //mw.IsEnabled = false;
            //mw.SetWindowEnableState(false);
            
            
            //try
            //{
            //    var pcp = new ProcessStartInfo
            //    {
            //        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenFrp.Service.exe"),
            //        Arguments = "--inst " + argument,
            //        ErrorDialog = false,
            //        UseShellExecute = true,
            //    };
            //    if (!App.IsAdministrator())
            //    {
            //        pcp.Verb = "runas";
            //    }

            //    Process.Start(pcp);
            //}
            //catch(System.ComponentModel.Win32Exception ex)
            //{
            //    _ = ex;
            //    return;
            //}
            //catch (Exception ex2)
            //{
            //    _ = ex2;
            //    return;
            //}
            //finally
            //{
            //    mw.IsEnabled = true;
            //    mw.SetWindowEnableState(true);
            //}
            //ShutdownApp();
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_RetryConnectRpc(CancellationToken cancellationToken)
        {
            await DaemonManager.LaunchDaemonAsync();

            await DaemonManager.WaitForConfigureAsync(cancellationToken);

            RpcManager.Configure();

            await Task.Delay(1000, cancellationToken);

            OpenFrp.Service.Proto.RpcResponse<Service.Proto.Response.SyncResponse>? r1_resp = default;

            for (int i = 0; i < 5; i++)
            {
                r1_resp = await RpcManager.Sync(cancellationToken);

                if (r1_resp.Flag)
                {
                    IsDaemonConnected = true;
                    break;
                }
                await Task.Delay(500, cancellationToken);
            }
            if (r1_resp is null || !r1_resp.Flag)
            {
                ShowAlert("无法同步数据", r1_resp?.Message ?? "未知原因", InfoBarSeverity.Error);
            }
            if (r1_resp is not { Data: Service.Proto.Response.SyncResponse srp_sr })
            {
                return;
            }
            
            if (Service.Net.OpenFrpApi.GetAuthorization() is null) { return; }

            var n1_resp = await Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);
            if (n1_resp.StatusCode is not System.Net.HttpStatusCode.OK || n1_resp.Data is not { } data)
            {
                if (frame is { Content: Views.Tunnels })
                {
                    frame.Navigate(typeof(Views.Settings));
                }
                ShowAlert("重新登录失败", n1_resp?.Message ?? "未知原因", InfoBarSeverity.Error);
                return;
            }
            UserInfo = new Model.UserInfo(data);

            var r2_resp = await RpcManager.Login(new Service.Proto.Request.LoginRequest { UserInfomationJson = Google.Protobuf.ByteString.CopyFrom(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data)) }, cancellationToken);

            if (r2_resp is { })
            {
                WeakReferenceMessenger.Default.Send(Model.RouteMessage<TunnelsViewModel>.Create("processOnlineWithAccount"));
                conve_CreateNotificationStreamCommand.Execute(default);
                // success;
            }
            else
            {
                if(frame is { Content: Views.Tunnels })
                {
                    frame.Navigate(typeof(Views.Settings));
                }
                ShowAlert("登录失败", r2_resp?.Message ?? "未知原因", InfoBarSeverity.Error);
            }
            return;
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_CreateNotificationStream(CancellationToken cancellationToken)
        {
            if (!RpcManager.IsConfigured) return;

            await RpcManager.NotificationStream(NotificationReader, cancellationToken);
        }

        /// <summary>
        /// 应用 - 自启动隧道
        /// </summary>
        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_TryAutoLaunchTunnel(CancellationToken cancellationToken)
        {
            if (!RpcManager.IsConfigured) return;
            try
            {
                if (!string.IsNullOrEmpty(App.Settings.AutoLaunchTunnel) && System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int[]>>(App.Settings.AutoLaunchTunnel) is { Count: > 0 } ap)
                {
                    Logger.LogDebug("[@conve_TryAutoLaunchTunnel] 正在尝试自动隧道启动，Prompt: {prompt}", App.Settings.AutoLaunchTunnel);

                    if (!ap.TryGetValue(UserInfo.UserID.ToString(), out var l) || l is not int[] { Length: > 0 } attr)
                    {
                        return;
                    }

                    var sync1 = await RpcManager.Sync(cancellationToken);

                    if (sync1.Data is null || sync1.Data.Onlines is not { } online)
                    {
                        return;
                    }
                    if (attr.Except(online) is { } t && !t.Any())
                    {
                        return;
                    }

                    var conf = await OpenFrp.Service.Net.OpenFrpApi.GetUserTunnels(cancellationToken);

                    Logger.LogDebug("[@conve_TryAutoLaunchTunnel] Request UserTunnels: Status: {code} , Data： {data}", conf.StatusCode,conf.Data);

                    if (conf.StatusCode is not System.Net.HttpStatusCode.OK || conf.Data is null || conf.Data.Total is 0 || conf.Data.List is null)
                    {
                        ShowAlert("在自动启动隧道时发生了错误", conf.Message ?? conf.Exception?.Message ?? "未知错误", InfoBarSeverity.Warning);

                        return;
                    }

                    using var mem = new MemoryStream();

                    Service.Proto.RpcResponse? rpcResponse;
                    List<Yue3.Model.OpenFrp.Response.Data.UserTunnel> requestTunnels = new List<Yue3.Model.OpenFrp.Response.Data.UserTunnel> { };
                    Dictionary<string, string>? tomlConfigMapping = new Dictionary<string, string> { };

                    foreach (var tun in conf.Data.List)
                    {
                        if (attr.Contains(tun.Id) && !online.Contains(tun.Id))
                        {
                            //　バカラ　// かみ　しろ　るい　//　神代類
                            requestTunnels.Add(tun);
                        }
                    }

                    if (App.Settings.UseConfigLaunch || FrpcManager.Feature.ForceUseConfig)
                    {
                        foreach (var nid in requestTunnels.Select(x => x.NodeId).Distinct())
                        {
                            var nodeConf = await OpenFrp.Service.Net.OpenFrpApi.GetNodeConfig(nid, cancellationToken);

                            if (nodeConf.StatusCode is not System.Net.HttpStatusCode.OK || string.IsNullOrEmpty(nodeConf.Data))
                            {
                                continue;
                            }

                            try
                            {
                                var table = Tomlyn.Toml.ToModel(nodeConf.Data!);

                                Tomlyn.Model.TomlTable[] sourceArray;

                                if (table.TryGetValue("proxies", out var val) && val is Tomlyn.Model.TomlTableArray proxies)
                                {
                                    sourceArray = new Tomlyn.Model.TomlTable[proxies.Count];

                                    proxies.CopyTo(sourceArray, 0);
                                }
                                else
                                {
                                    continue;
                                }

                                if (sourceArray.Length is 0) continue;

                                IEnumerable<string> namePool = (IEnumerable<string>)requestTunnels.Select(x => x.Name).Where(x => !string.IsNullOrEmpty(x));

                                table.Remove("proxies");

                                foreach (var tun in sourceArray)
                                {
                                    if (tun.TryGetValue("name",out var name) && name is not null or "" && namePool.Contains(name))
                                    {
                                        table.Add("proxies", new Tomlyn.Model.TomlTable[1] {tun});

                                        tomlConfigMapping?.Add(name.ToString()!, Tomlyn.Toml.FromModel(table));
                                    }
                                    table.Remove("proxies");
                                }
                            }
                            catch
                            {
                                continue;
                            }

                            
                        }
                    }

                    await System.Text.Json.JsonSerializer.SerializeAsync(mem, requestTunnels, cancellationToken: cancellationToken);

                    mem.Seek(0, SeekOrigin.Begin);

                    var bfStr = await Google.Protobuf.ByteString.FromStreamAsync(mem, cancellationToken).ConfigureAwait(false);

                    await Task.Delay(500, cancellationToken);

                    rpcResponse = await RpcManager.SyncWithLaunch(new Service.Proto.Request.SyncWithLaunchRequest
                    {
                        Config = new Service.Proto.Request.TunnelStreamRequest.Types.TunnelLaunchConfig
                        {
                            AllowDisableConsoleColor = FrpcManager.Feature.AllowDisableConsoleColor,
                            UseForceTls = App.Settings.UseForceTls,
                            UseDebug = App.Settings.UseDebug,
                        },
                        UserToken = UserInfo.UserToken,
                        RequireUserTunnels = bfStr,
                        TomlConfigMap =
                        {
                            tomlConfigMapping
                        }
                    }, cancellationToken);




                    if (rpcResponse is { Flag: false })
                    {
                        ShowAlert("自启动失败", rpcResponse.Message ?? rpcResponse.Status?.Message ?? "未知原因", InfoBarSeverity.Warning);
                    }
#if NET
                    await mem.DisposeAsync();
#else
                    mem.Dispose();

#endif
                }
            }
            catch
            {

            }
        }

        private void NotificationReader(Service.Proto.Response.NotificationStreamResponse response)
        {
            switch (response.State)
            {
                case Service.Proto.Response.NotificationStreamResponse.Types.NotificationStreamResponseState.LaunchSuccess:
                    {
                        if (!response.Data.TryUnpack<Service.Proto.Response.NotificationStreamResponse.Types.LaunchSuccessMsg>(out var msg)) return;


                        if (msg.IsAutoLaunch && App.Settings.DoNotNoticeAutoLaunchTunnelMsg)
                        {
                            return;
                        }

                        string[] addresses = msg.ConnectAddresses.ToArray();

                        var sb = new StringBuilder();

                        if (msg.TunnelType.Contains("HTTP"))
                        {
                            foreach (var item in msg.ConnectAddresses)
                            {
                                sb.Append(item + ",");
                            }
                        }
                        switch (App.Settings.NotificationMode)
                        {
                            case NotificationMode.ToastNotification:
                                {
                                    if (OSVersionHelper.IsWindows10OrGreater)
                                    {
                                        try
                                        {
                                            string adres;

                                            if (msg.TunnelType.Contains("HTTP"))
                                            {
                                                adres = sb.ToString().Remove(sb.Length - 1);
                                            }
                                            else
                                            {
                                                adres = addresses.First();
                                            }
                                            if (msg.TunnelType.Contains("HTTP"))
                                            {
                                                adres += "\n注: 请先将该上列域名解析到对应节点的地址。";
                                            }
                                            string attributionText = msg.IsFastLaunch ? "" : $"{msg.TunnelType.ToUpperInvariant()} {msg.Host}:{msg.Port}";

                                            new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                                            .AddText($"{(msg.IsFastLaunch ? "快启动" : string.Empty)}隧道 {msg.TunnelName} 启动成功!", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                                            .AddText($"点击\"复制按钮\"复制链接地址,开始你的映射之旅吧。")
                                                            .AddText($"可用地址: {adres}" )
                                                            .AddAttributionText(attributionText)
                                                            .AddButton("复制链接", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, $"copy {(msg.HasExtraConnectAddress ? msg.ExtraConnectAddress : addresses.First())}")
                                                            .AddButton("确定", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, "none")
                                                            .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                                            .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                                            .Show(toast =>
                                                            {
                                                                toast.Tag = msg.TunnelName;
                                                                if (App.Notification_UseExpiredReboot)
                                                                {
                                                                    toast.ExpiresOnReboot = true;
                                                                }
                                                                toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                                            });
                                            return;
                                        }
                                        catch
                                        {
                                            goto case Model.NotificationMode.TaskbarNotify;
                                        }
                                    }
                                }
                                ;break;
                            case NotificationMode.TaskbarNotify:
                                {
                                    if (App.TaskBarIcon is null)
                                    {
                                        return;
                                    }
                                    try
                                    {
                                        App.TaskBarIcon.ShowNotification(
                                            title: $"隧道 {msg.TunnelName} 启动成功!",
                                            message: $"可用地址: {(msg.TunnelType.Contains("HTTP") ? sb.ToString().Remove(sb.Length - 1) : addresses.First())}" + ("HTTP".Contains(msg.TunnelType) ? "\n注: 请先将该上列域名解析到对应节点的地址。" : ""),
                                            icon: H.NotifyIcon.Core.NotificationIcon.Info);
                                    }
                                    catch
                                    {

                                    }
                                }
                                ;break;
                        }
                        
                    };break;
                case Service.Proto.Response.NotificationStreamResponse.Types.NotificationStreamResponseState.LaunchFailed when !App.Settings.DoNotNoticeErrorMsg:
                    {
                        if (!response.Data.TryUnpack<Service.Proto.Response.NotificationStreamResponse.Types.LaunchFailedMsg>(out var msg)) return;

                        switch (App.Settings.NotificationMode)
                        {
                            case NotificationMode.ToastNotification:
                                {
                                    if (OSVersionHelper.IsWindows10OrGreater)
                                    {
                                        try
                                        {
                                            new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                                          .AddText($"隧道 {msg.TunnelName} 启动失败", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                                          .AddText(msg.Content)
                                                          .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                                          .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                                          .Show(toast =>
                                                          {
                                                              toast.Tag = msg.TunnelName;
                                                              if (App.Notification_UseExpiredReboot)
                                                              {
                                                                  toast.ExpiresOnReboot = true;
                                                              }
                                                              toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                                          });
                                            return;
                                        }
                                        catch
                                        {
                                            goto case NotificationMode.TaskbarNotify;
                                        }
                                    }
                                };break;
                            case NotificationMode.TaskbarNotify:
                                {
                                    if (App.TaskBarIcon is null)
                                    {
                                        return;
                                    }
                                    try
                                    {
                                        App.TaskBarIcon.ShowNotification(
                                            title: $"隧道 {msg.TunnelName} 启动失败",
                                            message: msg.Content,
                                            icon: H.NotifyIcon.Core.NotificationIcon.Error);
                                    }
                                    catch
                                    {

                                    }
                                }
                                ;break;
                            
                        }
                    };break;
                case Service.Proto.Response.NotificationStreamResponse.Types.NotificationStreamResponseState.Messaging:
                    {
                        if (response.Data.TryUnpack<Service.Proto.Response.NotificationStreamResponse.Types.UIWarningNotice>(out var warning))
                        {
                            ShowAlert(warning.Title, warning.Data, InfoBarSeverity.Warning);
                        }
                        else if (response.Data.TryUnpack<Service.Proto.Response.TunnelStreamResponse.Types.TunnelControlFailed>(out var tcf))
                        {
                            string details = "未知原因....";

                            if (tcf.DebugInfo.TryUnpack<Google.Rpc.DebugInfo>(out var dbgInfo))
                            {
                                details = dbgInfo.Detail;
                            }

                            ShowAlert($"自启动隧道 #{tcf.TunnelId} {tcf.TunnelName} 时发生了错误", dbgInfo.Detail,InfoBarSeverity.Error);
                        }
                    };break;
            }
        }

        [RelayCommand]
        private async Task @conve_CheckUpdate()
        {
            var resp = await OpenFrp.Service.Net.OpenFrpApi.GetSoftwareConfig();
            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { DownloadSources.Length: > 0 } software)
            {
                SoftwareConfig = new Model.SoftwareConfig(software);

                if (software.Launcher.Latest != App.LauncherVersionString)
                {
                    HasUpdate = true;
                }
                else if (FrpcManager.FrpcVersionString != "Unknown")
                {
                    if (software.Latest != FrpcManager.FrpcVersionString)
                    {
                        if (!OSVersionHelper.IsWindows8OrGreater && FrpcManager.FrpcVersionString.Equals("OpenFRP_0.54.0_835276e2_20240205"))
                        {
                            return;
                        }
                        HasUpdate = true;
                    }
                }
                else
                {
                    HasUpdate = false;
                }
            }
        }

        internal static async void ShutdownApp()
        {
            App.TaskBarIcon?.CloseTrayPopup();

            if (App.Current.MainWindow is AppWindow ap)
            {
                iNKORE.UI.WPF.Modern.Controls.ContentDialog.GetOpenDialog(ap)?.Hide();

                ap.HideByHANDLE();
                ap.CancelControl();
            }

            try
            {
                BindingOperations.ClearBinding(App.Current.MainWindow, iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty);
            }
            catch { }

            Helpers.UsrTokenService.WriteConfig();

            if (OSVersionHelper.IsWindows10OrGreater)
            {
                try
                {
                    Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.Uninstall();
                }
                catch { }
            }

            App.TaskBarIcon?.Dispose();

            var v = App.ServiceProvider.GetService<DaemonManager>();

            if (v is not null)
            {
                var resp = await v.KillDaemonAsync();

                if (resp.StatusCode is 768 && !string.IsNullOrEmpty(resp.Message))
                {
                    if (!string.IsNullOrEmpty(App.Settings.AutoLaunchTunnel))
                    {
                        try
                        {
                            var v1 = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int[]>>(App.Settings.AutoLaunchTunnel);
                            var v2 = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int[]>>(resp.Message!);

                            if (v1 is not null && v2 is not null)
                            {
                                var tkv = v2.FirstOrDefault();

                                if (v1.ContainsKey(tkv.Key))
                                {
                                    v1[tkv.Key] = tkv.Value;
                                }

                                string s = System.Text.Json.JsonSerializer.Serialize(v1);

                                if (!string.IsNullOrEmpty(s))
                                {
                                    App.Settings.AutoLaunchTunnel = s;
                                }
                            }
                        }
                        catch
                        {

                        }
                    }
                    App.Settings.AutoLaunchTunnel = resp.Message;
                }
                else if (resp.StatusCode is not 0)
                {
                    App.ServiceProvider.GetService<ILogger>()?.LogWarning("[ShutdownApp] DaemonManager.KillDaemonAsync failed: {statusCode} - {message}", resp.StatusCode, resp.Message);
                }
            }

            App.Settings.Save();

            Application.Current.Shutdown();
        }

        public void ShowAlert(string title, string message, InfoBarSeverity severity) => ShowAlertAction.Invoke(title,message,severity);
    }
}
