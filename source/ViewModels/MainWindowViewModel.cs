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
using System.Windows.Data;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.Model;

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
                                }
                                ;break;
                        }
                        return;
                    }
                    frame?.Navigate(tp);
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
                            //WeakReferenceMessenger.Default.Send(Model.RouteMessage<TunnelsViewModel>.Create("processOnline"));

                            IsDaemonConnected = true;
                        };break;
                    case "processOffline":
                        {
                            WeakReferenceMessenger.Default.Send(Model.RouteMessage<TunnelsViewModel>.Create("processOffline"));

                            IsDaemonConnected = false;
                        }; break;
                }
            });
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
            }
        }

        private Action<string,string,InfoBarSeverity> ShowAlertAction = delegate { };

        private iNKORE.UI.WPF.Modern.Controls.Frame? frame;
        private iNKORE.UI.WPF.Modern.Controls.NavigationView? navigationView;

        internal void OnViewModelPropertyChanged(string name) => OnPropertyChanged(name);

        [ObservableProperty]
        private Model.SoftwareConfig? softwareConfig;

        [ObservableProperty]
        private bool isDaemonConnected;

        [ObservableProperty]
        private bool hasUpdate;

        //[ObservableProperty]
        //private IProgress<Service.Net.HttpClient.HttpDownloadProgress>? downloadProgress;

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
                    
                    //if (frame.Content is Views.UpdateComment { DataContext: ViewModels.UpdateCommentViewModel ucvm })
                    //{
                        
                    //    if (ucvm.event_InstallUpdateCommand is IAsyncRelayCommand { IsRunning: true })
                    //    {
                            
                    //        return;
                    //    }
                    //}

                    if (e.IsSettingsInvoked || e.InvokedItemContainer is NavigationViewItem { Tag: "usrInfo" })
                    {
                        if (frame.Content is not Views.Settings) frame.Navigate(typeof(Views.Settings));

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
                        return;
                    }
                    //if (e.InvokedItemContainer is NavigationViewItem { Tag: "usrInfo" } )
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

                frame.Navigate(typeof(Views.Home));
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
            }
        }

        [RelayCommand]
        private void @event_WindowLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is MainWindow mw)
            {

                if (App.StartupArguments.Contains("--minimize") && mw.Tag is false)
                {
                    mw.HideByHwndCC();
                }
                ShowAlertAction = mw.ShowAlert;

                mw.Closing += (_, e) =>
                {
                    e.Cancel = true;
                    mw.HideByHwndCC();
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
                        string targetVersion = software.Latest!;
                        if (!OSVersionHelper.IsWindows10OrGreater)
                        {
                            targetVersion = "OpenFRP_0.54.0_835276e2_20240205";
                        }
                        StringBuilder sb = new StringBuilder();
                        foreach (var source in software.DownloadSources)
                        {
                            string url = $"{source.BaseUrl}/{targetVersion}/frpc_windows_{Service.Helpers.FileHelper.UserPlatform}.zip";

                            sb.Append(url);
                            sb.Append(';');
                        }
                        sb.Remove(sb.Length - 1, 1);

                        argument += " -urls " + sb.ToString();
                    }
                    ; break;
            }

            if (App.Current.MainWindow is not MainWindow mw) return;

            mw.IsEnabled = false;
            mw.SetCCWindowState(false);
            
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenFrp.Service.exe"),
                    Arguments = "--inst " + argument,
                    Verb = "runas",
                    ErrorDialog = false,
                    UseShellExecute = true,
                });
            }
            catch(System.ComponentModel.Win32Exception ex)
            {
                _ = ex;
                return;
            }
            catch (Exception ex2)
            {
                _ = ex2;
                return;
            }
            finally
            {
                mw.IsEnabled = true;
                mw.SetCCWindowState(true);
            }
            ShutdownApp();
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
            
            if (Service.Net.OpenFrpApi.GetAuthorization() is null) { return; }

            var n1_resp = await Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);
            if (n1_resp.StatusCode is not System.Net.HttpStatusCode.OK || n1_resp.Data is not { } data)
            {
                if (frame is { Content: Views.Tunnels })
                {
                    frame.Navigate(typeof(Views.Settings));
                }
                ShowAlert("重-登录失败", n1_resp?.Message ?? "未知原因", InfoBarSeverity.Error);
                return;
            }
            UserInfo = new Model.UserInfo(data);

            var r2_resp = await manager.Login(new Service.Proto.Request.LoginRequest { UserInfomationJson = Google.Protobuf.ByteString.CopyFrom(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data)) }, cancellationToken);

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
            App.LaunchRpcProcess(out var rpcManager);

            await rpcManager.NotificationStream(NotificationReader, cancellationToken);
        }
        private void NotificationReader(Service.Proto.Response.NotificationStreamResponse response)
        {
            switch (response.State)
            {
                case Service.Proto.Response.NotificationStreamResponse.Types.NotificationStreamResponseState.LaunchSuccess:
                    {
                        if (!response.Data.TryUnpack<Service.Proto.Response.NotificationStreamResponse.Types.LaunchSuccessMsg>(out var msg)) return;

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
                case Service.Proto.Response.NotificationStreamResponse.Types.NotificationStreamResponseState.Messaging:
                    {
                        if (response.Data.TryUnpack<Service.Proto.Response.NotificationStreamResponse.Types.UIWarningNotice>(out var warning))
                        {
                            ShowAlert(warning.Title, warning.Data, InfoBarSeverity.Warning);
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
                else if (App.FrpcVersionString != "Unknown")
                {
                    if (software.Latest != App.FrpcVersionString)
                    {
                        HasUpdate = true;
                    }
                }
                else
                {
                    HasUpdate = false;
                }
            }
        }

        internal static void ShutdownApp(Process? bypassProc = default)
        {
            App.TaskBarIcon?.CloseTrayPopup();

            switch (App.Current.MainWindow)
            {
                case MainWindow mw:
                    {
                        iNKORE.UI.WPF.Modern.Controls.ContentDialog.GetOpenDialog(mw)?.Hide();
                        mw.HideByHwndCC();
                    }
                    ; break;
                case LoginWindow lw:
                    {
                        iNKORE.UI.WPF.Modern.Controls.ContentDialog.GetOpenDialog(lw)?.Hide();
                        lw.HideByHwndCC();
                    }
                    ; break;
            }

            try
            {
                BindingOperations.ClearBinding(App.Current.MainWindow, iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty);
            }
            catch
            {

            }
            Helpers.UsrTokenService.WriteConfig();
            App.Settings.Save();

            App.TaskBarIcon?.Dispose();

            if (App.ServiceProcess is not null)
            {
                App.ServiceProcess.EnableRaisingEvents = false;
                try
                {
                    App.ServiceProcess.StandardInput.WriteLine("exitProc");

                    Application.Current.Shutdown();

                    return;
                }
                catch (InvalidOperationException)
                {

                }
            }
            var dBase = Path.Combine(Directory.GetCurrentDirectory(), "OpenFrp.Service.exe");
            foreach (var proc in Process.GetProcessesByName("OpenFrp.Service"))
            {
                try
                {
                    if (proc.Id.Equals(bypassProc?.Id)) continue;

                    if (proc.MainModule is { FileName: var f } && f.Equals(dBase))
                    {
                        proc.Kill();

                        break;
                    }
                }
                catch
                {

                }
            }

            if (OpenFrp.Service.Helpers.FileHelper.TryGetFRPClient(out string path))
            {
                foreach (var proc in Process.GetProcessesByName($"frpc_windows_{OpenFrp.Service.Helpers.FileHelper.UserPlatform}"))
                {
                    try
                    {
                        if (proc.MainModule is { FileName: var f } && f.Equals(path))
                        {
                            proc.Kill();

                            break;
                        }
                    }
                    catch
                    {

                    }
                }
            }
            Application.Current.Shutdown();
        }

        public void ShowAlert(string title, string message, InfoBarSeverity severity) => ShowAlertAction.Invoke(title,message,severity);
    }
}
