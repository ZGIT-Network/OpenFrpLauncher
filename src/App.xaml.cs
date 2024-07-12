using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Awe.Model;
using Awe.Model.OpenFrp.Response.Data;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Grpc.Core;
using H.NotifyIcon;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Web.WebView2.Core;
using OpenFrp.Launcher.Model;
using OpenFrp.Launcher.ViewModels;
using OpenFrp.Service.Call;
using OpenFrp.Service.Net;
using static Google.Protobuf.WellKnownTypes.Field.Types;


namespace OpenFrp.Launcher
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private enum AppStartEnum
        {
            None = 0,
            Minimize = 1,
            Update,
            UpdateFinish,
            Uninstall
        }

        public App()
        {
            
        }

        internal static CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

        public static string? WebViewTemplatePath { get; set; }

        public static string VersionString { get; } = "OpenFrpLauncher.v100.VupiKelbel";

        public static string FrpcVersionString { get; private set; } = "Unknown";

        public static CancellationToken CancellationToken { get => TokenSource.Token; }

        internal static Properties.Settings Settings 
        { 
            get
            {
                try
                {
                    return OpenFrp.Launcher.Properties.Settings.Default;
                }
                catch
                {
                    throw;
                }
            } 
        }

#pragma warning disable CS8618
        public static H.NotifyIcon.TaskbarIcon TaskBarIcon { get; set; }

        public static Process ServiceProcess { get; set; }

#pragma warning restore CS8618

        protected override async void OnStartup(StartupEventArgs e)
        {
            var ase = GetAppType(e.Args);

            ExcepitonHandler = (ex) =>
            {
                try { Clipboard.SetText(ex.ToString()); } catch { }

                MessageBox.Show($"错误内容已复制，按下Ctrl+V | 粘贴 来显示内容。" +
                $"\n{ex.Message}", "OpenFrp Launcher Throw Out!!", MessageBoxButton.OK, MessageBoxImage.Error);

                Environment.Exit(ex.HResult);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    ExcepitonHandler?.Invoke(ex);
                }
            };

            switch (ase)
            {
                case AppStartEnum.Uninstall:
                    {
                        try
                        {
                            var fe = FileDictionary.GetAutoStartupFile();
                            try
                            {
                                File.Delete(fe);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                            }
                            if (Environment.OSVersion.Version.Major is 10)
                            {
                                Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
                                Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.Uninstall();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }

                        Settings.Reset();

                        try
                        {
                            
                            File.Delete(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
                        }
                        catch { }
                        
                        break;
                    }
                case AppStartEnum.Update:
                    {
                        if (IsAdministrator())
                        {
                            FrpcVersionString = await GetFrpcVersionAsync();

                            KillServiceProcess();
                            SetSecureApp();

                            _ = ConfigureVersionCheck(FrpcVersionString);
                            ConfigureUpdateWindow();
                        }
                        else
                        {
                            if (ase is AppStartEnum.UpdateFinish or AppStartEnum.None)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = Assembly.GetExecutingAssembly().Location,
                                        Verb = "runas",
                                        Arguments = "--update",
                                        ErrorDialog = false
                                    });
                                } catch { }
                            }
                        }
                        break;
                    }
                case AppStartEnum.Minimize: goto case AppStartEnum.None;
                case AppStartEnum.UpdateFinish:
                    {
                        FrpcVersionString = await GetFrpcVersionAsync();
                        if ("non-frp".Equals(FrpcVersionString))
                        {
                            if (MessageBox.Show("看起来下载的 FRPC 文件无效，是否重新下载？", "OpenFrp Launcher", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) is MessageBoxResult.Cancel)
                            {
                                break;
                            }
                            goto case AppStartEnum.Update;
                        }
                        goto case AppStartEnum.None;
                    }
                case AppStartEnum.None:
                    {
                        try
                        {
                            Settings.Reload();
                        }
                        catch
                        {
                            Settings.Reset();
                            try
                            {
                                File.Delete(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
                            }
                            catch { }
                        }
                        try
                        {
                            if (Environment.OSVersion.Version.Major is 10)
                            {
                                if (Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
                                {
                                    Environment.Exit(0);
                                    return;
                                }
                                if (Settings.NotifyMode is NotifyMode.NotifyIconDefault)
                                {
                                    Settings.NotifyMode = NotifyMode.Toast;
                                }
                            }
                        }
                        catch { }
                        try
                        {
                            if (!e.Args.Contains("--update") && e.Args.Contains("--finish") && Process.GetProcessesByName("OpenFrpLauncher") is { Length: > 1 } lt)
                            {
                                var self = Process.GetCurrentProcess();

                                foreach (var item in lt)
                                {
                                    if (item.Handle.Equals(self.Handle) || item.MainWindowTitle.Equals("OpenFrp 更新窗口")) break;

                                    if (item.MainModule.FileName == Assembly.GetEntryAssembly().Location)
                                    {
                                        if (item.MainWindowHandle == IntPtr.Zero)
                                        {
                                            MessageBox.Show(
                                                "OpenFrp 启动器已启动。若您想运行多个启动器实例，更推荐您使用 FRPC 进行进程管理。\n请单击\"托盘图标\"来显示主窗口。", "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);

                                            break;
                                        }
                                        try { item.Refresh(); } catch { }
                                        
                                        Awe.UI.Win32.User32.ShowWindow(item.MainWindowHandle, 9);
                                        Awe.UI.Win32.User32.SetForegroundWindow(item.MainWindowHandle);

                                        App.Current.Shutdown();
                                        return;
                                    }
                                    break;
                                }

                            }
                        }
                        catch { }

                        if (ase != AppStartEnum.UpdateFinish)
                        {
                            FrpcVersionString = await GetFrpcVersionAsync();
                            if ("non-frp".Equals(FrpcVersionString))
                            {
                                goto case AppStartEnum.Update;
                            }
                        }

                        SetSecureApp();

                      
                        if (WindowsServiceCall.IsInstalledService())
                        {
                            if (!WindowsServiceCall.IsServiceLaunched)
                            {
                                do
                                {
                                    await Task.Delay(50);
                                } while (!WindowsServiceCall.StartService());
                            }

                            ConfigureRPC();
                        }
                        else
                        {
                            ConfigureProcess();
                            ConfigureRPC();
                        }
                        ConfigureAppCenter();
                        ConfigureToast();
                        ConfigureTimer();

                        //TryAutoLogin();

                        ConfigureWindow(ase is AppStartEnum.Minimize);


                        return;
                    }
            }
            Environment.Exit(0);
        }

        private AppStartEnum GetAppType(params string[] arg)
        {
            if (arg.Contains("--uap"))
            {
                return AppStartEnum.Uninstall;
            }
            else if (arg.Contains("--update"))
            {
                return AppStartEnum.Update;
            }
            else if (arg.Contains("--minimize"))
            {
                return AppStartEnum.Minimize;
            }
            else if (arg.Contains("--finish"))
            {
                return AppStartEnum.UpdateFinish;
            }
            return AppStartEnum.None;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ClearNotifications();
        }

        /// <summary>
        /// 是否为管理员
        /// </summary>
        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// HTTP Network 配置
        /// </summary>
        private static void SetSecureApp()
        {
            if (Settings.UseProxy)
            {
                OpenFrp.Service.Net.HttpRequest.ProxyEditor(true);
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                if (errors is not System.Net.Security.SslPolicyErrors.None)
                {
                    return MessageBox.Show($"来自服务器的证书无效:::" +
                    $"\n Name:{certificate.Issuer}" +
                    $"\n Reason:{Enum.GetName(typeof(System.Net.Security.SslPolicyErrors), errors)} " +
                    $"\n是否允许访问?", "OpenFrp Launcher", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) is MessageBoxResult.OK ? true
                 : false;
                }
                return true;
            };

        }

        /// <summary>
        /// 清除通知
        /// </summary>
        internal static void ClearNotifications(string? tag = "")
        {
            if (Environment.OSVersion.Version.Major is not 10)
            {
                try
                {
                    App.TaskBarIcon.ClearNotifications();
                }
                catch
                {
                    
                }
            }
            else
            {
                try
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Remove(tag);

                        return;
                    }
                    Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
                }
                catch
                {
                    try
                    {
                        App.TaskBarIcon.ClearNotifications();
                    }
                    catch
                    {

                    }
                }
            }
        }

        /// <summary>
        /// 清除 WebView2 缓存
        /// </summary>
        internal static void ClearWebviewRuntimeCache()
        {
            if (!string.IsNullOrEmpty(App.WebViewTemplatePath) && Directory.Exists(App.WebViewTemplatePath))
            {
                try { Directory.Delete(App.WebViewTemplatePath, true); } catch { }
            }
        }

        internal static void KillServiceProcess(bool killFrpcService = false)
        {
            try
            {
                if (WindowsServiceCall.IsInstalledService())
                {
                    WindowsServiceCall.StopService(true);

                    return;
                }
                else if (!ServiceProcess.HasExited)
                {
                    ServiceProcess.EnableRaisingEvents = false;
                    ServiceProcess.Kill();
                }
            }
            catch
            {

            }
            if (killFrpcService)
            {
                KillFrpcProcess();
            }
            try
            {
                if (Process.GetProcessesByName("OpenFrpService") is { Length: > 0 } ck)
                {
                    var asm = Assembly.GetAssembly(typeof(OpenFrp.Service.Net.OpenFrp));

                    foreach (var item in ck)
                    {
                        try
                        {
                            if (item.MainModule.FileName.Equals(asm.Location))
                            {
                                item.Kill();
                            }
                        }
                        catch { }
                    }
                }
            }
            catch
            {

            }

        }

        internal static void KillFrpcProcess()
        {
            try
            {
                string platform = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "amd64",
                    Architecture.X86 => "386",
                    Architecture.Arm64 => "arm64",
                    _ => throw new NotSupportedException("本软件暂不支持 ARMv7 等其他平台。"),
                };
                if (Process.GetProcessesByName($"frpc_windows_{platform}") is { Length: > 0 } ck)
                {
                    foreach (var pro in ck)
                    {
                        try
                        {
                            if (pro.MainModule.FileName.Equals(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc", $"frpc_windows_{platform}.exe")) && !pro.HasExited)
                            {
                                pro.Kill();
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
            catch
            {

            }
        }

        #region Configure

        private static async void ConfigureAppCenter()
        {


            AppCenter.Start("84e8ed84-8a2d-4eeb-ae5a-cc073b745677", typeof(Analytics), typeof(Crashes));

            var id = await AppCenter.GetInstallIdAsync();

            AppCenter.SetUserId(id.ToString());
            AppCenter.SetCountryCode(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            ExcepitonHandler = (ex) =>
            {
                Crashes.TrackError(ex);
                try { Clipboard.SetText(ex.ToString()); } catch { }

                MessageBox.Show($"错误内容已复制，按下Ctrl+V | 粘贴 来显示内容。" +
                $"\n您也可以不反馈该问题，因为问题已上传到云端。\n会话 ID: {id}\n", "OpenFrp Launcher Throw Out!!", MessageBoxButton.OK, MessageBoxImage.Error);

                Environment.Exit(ex.HResult);
            };
            await AppCenter.SetEnabledAsync(true);
            //Analytics.EnableManualSessionTracker();

            /**
            AppCenter.Start("7655be1b-6ace-41f5-8b35-d3405ff31dda",
                typeof(Analytics), typeof(Crashes));
            var id = UserId = (await AppCenter.GetInstallIdAsync()).ToString();
            AppCenter.SetUserId(id.ToString());
            AppCenter.SetCountryCode(System.Globalization.CultureInfo.CurrentUICulture.EnglishName);
            await AppCenter.SetEnabledAsync(true);
             
             */

            

            //await AppCenter.SetEnabledAsync(true);




        }

        public static Action<Exception> ExcepitonHandler { get; set; } = delegate { };

        private static void ConfigureProcess()
        {
            if (Process.GetProcessesByName("OpenFrpService") is { Length: > 0 } ck)
            {
                var asm = Assembly.GetAssembly(typeof(OpenFrp.Service.RpcResponse));

                foreach (var item in ck)
                {
                    try
                    {
                        if (item.MainModule.FileName.Equals(asm.Location))
                        {
                            ServiceProcess = item;

                            return;
                        }
                    }
                    catch { }
                }
            }
            
            ServiceProcess = new Process()
            {
                StartInfo =
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenFrpService.exe"),
                    CreateNoWindow = true,
                    Arguments = $"deamon --pn openfrpLauncher.{@ev_AssemblyAc()}",
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            try
            {
                ServiceProcess.Start();
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                try
                {
                    var rtr = new Thread(() => Clipboard.SetText(ex.ToString()));

                    rtr.TrySetApartmentState(ApartmentState.STA);

                    rtr.Start();
                }
                catch
                {

                }

                MessageBox.Show($"\n!!!!!!!辅助进程启动失败!!!!!!\n{ex.Message}\n错误内容已复制，按下Ctrl+V | 粘贴 来显示内容。", "OpenFrp Launcher Throw Out!!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ServiceProcess.Exited += async delegate
            {
                if (CancellationToken.IsCancellationRequested) return;

                await Task.Delay(500);

                ConfigureProcess();
            };
        }

        private static string @ev_AssemblyAc(int v = 0)
        {
            try
            {
                var va = Assembly.GetAssembly(typeof(Service.HashCalculator));

                return Service.HashCalculator.CompushHash(va.Location);
            }
            catch
            {
                if (v is 5)
                {
                    throw;
                }
                v++;
                return ev_AssemblyAc(v);
            }
        }

        public static async Task<ApiResponse?> ConfigureVersionCheck(string frpVersion)
        {
            var versionData = await OpenFrp.Service.Net.OpenFrp.GetSoftwareVersionInfo();

            if (versionData.Exception is null && versionData.StatusCode is System.Net.HttpStatusCode.OK &&
                versionData.Data is { } data)
            {
                if (!VersionString.Equals(versionData.Data?.Launcher.Latest))
                {
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new Model.UpdateInfo
                    {
                        Type = Model.UpdateInfoType.Launcher,
                        Log = data.Launcher.Message,
                        Title = data.Launcher.Title,
                        SoftWareVersionData = data,
                    }));
                }   
                if (!frpVersion.Equals(versionData.Data?.Latest))
                {
                    if (Environment.OSVersion.Version.Major is not 10 && frpVersion.Equals("OpenFRP_0.54.0_835276e2_20240205"))
                    {
                        return default;
                    }
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new Model.UpdateInfo
                    {
                        Type = Model.UpdateInfoType.FrpClient,
                        SoftWareVersionData = versionData.Data,
                        Title = "FRPC 更新",
                        Log = 
                            (Environment.OSVersion.Version.Major is 10 ? "" : "Windows 7 已不受支持，将升级到 OpenFRP_0.54.0_835276e2_20240205。") +
                            data.FrpcUpdateLog +
                            (Environment.OSVersion.Version.Major is 10 ? $"\nUpdate: {App.FrpcVersionString} => {data.Latest}" : $"\nUpdate: {App.FrpcVersionString} => OpenFRP_0.54.0_835276e2_20240205。") +
                            $"\n请注意: 若您在使用 FRPC 映射远程服务，请备用远程方式，否则请不要更新！"
                    }));
                }
                return default;
            }
            return versionData;
        }
        
        private static void ConfigureToast()
        {
            if (Environment.OSVersion.Version.Major is not 10) return;
            try
            {
                Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.OnActivated += (e) =>
                {
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            if (e.Argument.Contains("copy"))
                            {
                                Clipboard.SetText(e.Argument.Split(' ').Last());
                            }
                        }
                        catch
                        {

                        }
                    });

                    thread.TrySetApartmentState(ApartmentState.STA);
                    thread.Start();
                };
            }
            catch { }
        }

        private static async Task<RenLink.Assets.Minecraft.MinecraftStatus?> @ef_sikeeriSiskeTuwuwee(int port)
        {
            TcpClient client = new TcpClient();

            return await await client.ConnectAsync("127.0.0.1", port).ContinueWith<Task<RenLink.Assets.Minecraft.MinecraftStatus?>>(async delegate (Task task)
            {
                if (!task.IsFaulted && task.IsCompleted && client.Connected)
                {
                    try
                    {
                        using (RenLink.Network.Minecraft.MinecraftStream stream = new RenLink.Network.Minecraft.MinecraftStream(client))
                        {
                            var handshake = new RenLink.Assets.Minecraft.Packet.Handshake.Handshake
                            {
                                GameVersion = 47,
                                NextState = RenLink.Assets.Minecraft.ClientState.Status,
                                ServerAddress = "127.0.0.1\0FML",
                                ServerPort = 0
                            };

                            await handshake.SerializeAsync(stream);

                            await stream.WriteVarIntAsync(1);
                            await stream.WriteVarIntAsync(0);

                            var len = await stream.ReadVarIntAsync();
                            if (len > 0)
                            {
                                _ = await stream.ReadVarIntAsync();

                                var json = await stream.ReadStringAsync();

                                return System.Text.Json.JsonSerializer.Deserialize<RenLink.Assets.Minecraft.MinecraftStatus>(json);
                            }
                            client.Close();
                            return default(RenLink.Assets.Minecraft.MinecraftStatus);
                            //var status = RenLink.Assets.Minecraft.Packet.JsonResponseBody
                        }
                    }
                    catch
                    {

                    }
                }
                return default(RenLink.Assets.Minecraft.MinecraftStatus);
            });
        }

        private static async void ConfigureRPC()
        {
            RpcManager.Configre($"openfrpLauncher.{@ev_AssemblyAc()}");
            Regex regexForMotdPort = new Regex(@"\[MOTD\](\D{1,999})?\[/MOTD\]\[AD\]([0-9]{1,999})\[\/AD\]");

            while (!CancellationToken.IsCancellationRequested)
            {
                var resp = await RpcManager.SyncAsync(new Service.Proto.Request.SyncRequest(),cancellationToken: CancellationToken);

                if (resp.IsSuccess && resp.Data != null)
                {
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(resp.Data.TunnelId.ToArray()));
                    try
                    {
                        WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create("onService"));


                        if (string.IsNullOrEmpty(RpcManager.UserSecureCode))
                        {
                            await Task.Delay(1500);
                            continue;
                        }

                        var exception = await RpcManager.NotifiyStream(async (response) =>
                        {
                            try
                            {
                                switch (response.State)
                                {
                                    case Service.Proto.Response.NotiflyStreamState.NoticeUdpClientFallback:
                                        {
                                            if (response.Data.Is(OpenFrp.Service.Proto.Response.UdpBroadcastReceived.Descriptor) && response.Data.TryUnpack<OpenFrp.Service.Proto.Response.UdpBroadcastReceived>(out var packet))
                                            {
                                                if (WeakReferenceMessenger.Default.IsRegistered<RouteMessage<Controls.TunnelEditor, int>>(nameof(Controls.TunnelEditor)))
                                                {
                                                    string str = packet.Data.ToStringUtf8();
                                                    if (regexForMotdPort.Match(str) is { Success: true, Groups.Count: 3 } match)
                                                    {
                                                        if (int.TryParse(match.Groups[2].ToString(),out var port))
                                                        {
                                                            WeakReferenceMessenger.Default.Send(RouteMessage<Controls.TunnelEditor>.Create(port));
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    case Service.Proto.Response.NotiflyStreamState.UpdateUserTunnelList:
                                        {
                                            if (response.Data is { } && response.Data.Is(Google.Protobuf.WellKnownTypes.ListValue.Descriptor) &&
                                                response.Data.TryUnpack<Google.Protobuf.WellKnownTypes.ListValue>(out var packet))
                                            {
                                                WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(
                                                     packet.Values
                                                    .Where(x => x.HasNumberValue)
                                                    .Select(x => (int)x.NumberValue)
                                                    .ToArray()
                                                ));
                                            }
                                            break;
                                        }
                                    default:
                                        {
                                            Awe.Model.OpenFrp.Response.Data.UserTunnel? tunnel = default;
                                            try
                                            {
                                                tunnel = JsonSerializer.Deserialize<Awe.Model.OpenFrp.Response.Data.UserTunnel>(response.TunnelJson);
                                            }
                                            catch
                                            {
                                                break;
                                            }
                                            if (tunnel != null)
                                            {
                                                switch (response.State)
                                                {
                                                    case Service.Proto.Response.NotiflyStreamState.LaunchSuccess:
                                                        {
                                                            if (Settings.NotifyMode is Model.NotifyMode.Disable) break;

                                                            var sb = new StringBuilder();

                                                            if ("HTTP".Contains(tunnel.Type))
                                                            {
                                                                foreach (var item in tunnel.Domains)
                                                                {
                                                                    sb.Append(item + ",");
                                                                }
                                                            }
                                                            if (Environment.OSVersion.Version.Major is 10 && Settings.NotifyMode is Model.NotifyMode.Toast)
                                                            {
                                                                
                                                                try
                                                                {
                                                                    if (!string.IsNullOrEmpty(tunnel.TunnelCustomConfig) && Regex.IsMatch(tunnel.TunnelCustomConfig, "launcher.isMinecraftService( )?=( )?(T)?(t)?(rue)?(RUE)?"))
                                                                    {
                                                                        var img = Path.GetTempFileName();
                                                                        using (FileStream fs = File.Open(img, FileMode.OpenOrCreate))
                                                                        {
                                                                            var source = App.GetResourceStream(new Uri($"pack://application:,,,/Resources/Images/home-bar.png"));

                                                                            if (source.Stream is { Length: > 0 } f)
                                                                            {
                                                                                await f.CopyToAsync(fs);
                                                                                await fs.FlushAsync();
                                                                            }
                                                                        }

                                                                        var status = await ef_sikeeriSiskeTuwuwee(tunnel.Port).WithTimeout(5000);

                                                                        new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                                                            .AddHeroImage(new Uri(img,UriKind.RelativeOrAbsolute))
                                                                            .AddText($"Minecraft 服务映射成功!", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                                                            .AddText($"游戏版本: {status?.Version.AppName ?? "未知"} ({status?.Version.ProtocolVersion ?? -1})", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Subtitle)
                                                                            .AddText($"点击\"复制按钮\"复制链接地址,开始你的映射之旅吧。\n可用地址: {tunnel.ConnectAddress}")
                                                                            .AddAttributionText($"{tunnel.Type!.ToUpper()} {tunnel.Host}:{tunnel.Port}\nMotd: {status?.Description.ToString()}")
                                                                            
                                                                            .AddButton("复制链接", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, $"copy {("HTTP".Contains(tunnel.Type) ? tunnel.Domains.First() : tunnel.ConnectAddress)}")
                                                                            .AddButton("确定", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, "none")
                                                                            .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                                                            .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                                                            .Show(toast =>
                                                                            {
                                                                                toast.Tag = tunnel.Name;
                                                                                try { toast.ExpiresOnReboot = true; }
                                                                                catch
                                                                                {

                                                                                }
                                                                                toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                                                            });
                                                                    }
                                                                    else
                                                                    {
                                                                        new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                                                        .AddText($"隧道 {tunnel.Name} 启动成功!", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                                                        .AddText($"点击\"复制按钮\"复制链接地址,开始你的映射之旅吧。")
                                                                        .AddText($"可用地址: {("HTTP".Contains(tunnel.Type) ? sb.ToString().Remove(sb.Length - 1) : tunnel.ConnectAddress)}")
                                                                        .AddAttributionText($"{tunnel.Type!.ToUpper()} {tunnel.Host}:{tunnel.Port}")
                                                                        .AddButton("复制链接", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, $"copy {("HTTP".Contains(tunnel.Type) ? tunnel.Domains.First() : tunnel.ConnectAddress)}")
                                                                        .AddButton("确定", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, "none")
                                                                        .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                                                        .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                                                        .Show(toast =>
                                                                        {
                                                                            toast.Tag = tunnel.Name;
                                                                            try { toast.ExpiresOnReboot = true; }
                                                                            catch
                                                                            {

                                                                            }
                                                                            toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                                                        });
                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                    if (TaskBarIcon is not null)
                                                                    {
                                                                        try
                                                                        {
                                                                            TaskBarIcon.ShowNotification($"隧道 {tunnel.Name} 启动成功!", $"可用地址: {("HTTP".Contains(tunnel.Type) ? sb.ToString().Remove(sb.Length - 1) : tunnel.ConnectAddress)}",
                                                                                icon: H.NotifyIcon.Core.NotificationIcon.Info, timeout: TimeSpan.FromSeconds(10));
                                                                        }
                                                                        catch { }
                                                                    }
                                                                }
                                                            }
                                                            else if (TaskBarIcon is not null && Settings.NotifyMode is Model.NotifyMode.NotifyIcon)
                                                            {
                                                                try
                                                                {
                                                                    TaskBarIcon.ShowNotification($"隧道 {tunnel.Name} 启动成功!", $"可用地址: {("HTTP".Contains(tunnel.Type) ? sb.ToString().Remove(sb.Length - 1) : tunnel.ConnectAddress)}",
                                                                        icon: H.NotifyIcon.Core.NotificationIcon.Info, timeout: TimeSpan.FromSeconds(10));
                                                                }
                                                                catch { }
                                                            }
                                                            break;
                                                        };
                                                    case Service.Proto.Response.NotiflyStreamState.NoticeForLauncher when response.Message.Contains("被 Console 要求下线"):
                                                        {
                                                            var resp = await RpcManager.SyncAsync(new Service.Proto.Request.SyncRequest { });

                                                            if (resp.IsSuccess && resp.Data != null)
                                                            {
                                                                WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(resp.Data.TunnelId.ToArray()));

                                                                await Task.Delay(500);

                                                                WeakReferenceMessenger.Default.Send(new Tuple<string, object?>("refresh", default));
                                                            }

                                                            ; break;
                                                        }
                                                    case Service.Proto.Response.NotiflyStreamState.LaunchFailed:
                                                        {
                                                            if (response.TunnelJson.Equals("app.unhandleException"))
                                                            {
                                                                break;
                                                            }
                                                            if (Environment.OSVersion.Version.Major is 10 &&
                                                                Settings.NotifyMode is Model.NotifyMode.Toast)
                                                            {
                                                                try
                                                                {
                                                                    new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                                                     .AddText($"隧道 {tunnel.Name} 启动失败", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                                                     .AddText(response.Message)
                                                                     .AddAttributionText($"{tunnel.Type!.ToUpper()} {tunnel.Host}:{tunnel.Port}")
                                                                     .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                                                     .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                                                     .Show(toast =>
                                                                     {
                                                                         toast.Tag = tunnel.Name;
                                                                         try { toast.ExpiresOnReboot = true; } catch { }
                                                                         toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                                                     });
                                                                }
                                                                catch
                                                                {
                                                                    if (TaskBarIcon is not null)
                                                                    {
                                                                        try
                                                                        {
                                                                            TaskBarIcon.ShowNotification($"隧道 {tunnel.Name} 启动失败", response.Message,
                                                                                icon: H.NotifyIcon.Core.NotificationIcon.Error, timeout: TimeSpan.FromSeconds(10));
                                                                        }
                                                                        catch { }
                                                                    }
                                                                }
                                                            }
                                                            else if (TaskBarIcon is not null && Settings.NotifyMode is Model.NotifyMode.NotifyIcon)
                                                            {
                                                                try
                                                                {
                                                                    TaskBarIcon.ShowNotification($"隧道 {tunnel.Name} 启动失败", response.Message,
                                                                        icon: H.NotifyIcon.Core.NotificationIcon.Error, timeout: TimeSpan.FromSeconds(10));
                                                                }
                                                                catch { }
                                                            }
                                                            break;
                                                        }
                                                    case Service.Proto.Response.NotiflyStreamState.NoticeForTunnelClosed:
                                                        {
                                                            WeakReferenceMessenger.Default.Send(RouteMessage<TunnelsViewModel>.Create(response));

                                                            break;
                                                        }
                                                }
                                            }
                                            break;
                                        }
                                }
                               
                            }
                            catch
                            {

                            }
                        });
                        if (exception is not null)
                        {
                            Service.Net.OpenFrp.Logout();
                            
                            WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new UserInfo
                            {
                                UserName = "not-allow-display"
                            }));
                        }
                        else
                        {
                            await Task.Delay(1500);
                            continue;
                        }
                    }
                    catch { }
                }
                WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create("offService"));
                await Task.Delay(1000);
                {
                    if (CancellationToken.IsCancellationRequested) return;

                    if (WindowsServiceCall.IsInstalledService())
                    {
                        if (!WindowsServiceCall.IsServiceLaunched)
                        {
                            do
                            {
                                await Task.Delay(50);
                            } while (!WindowsServiceCall.StartService());
                        }
                    }
                    else 
                    {
                        try
                        {
                            if (ServiceProcess.HasExited)
                            {
                                ConfigureProcess();
                            }
                        }
                        catch
                        {
                            ConfigureProcess();
                        }
                    }
                }
            }
        }

        private static async void ConfigureTimer()
        {
            while (true)
            {
                var va = await ConfigureVersionCheck(FrpcVersionString);

                if (va is { Exception: null, StatusCode: HttpStatusCode.OK })
                {
                    await Task.Delay(TimeSpan.FromHours(2));
                }
                else await Task.Delay(TimeSpan.FromMinutes(10));
            }
        }

        private static void ConfigureWindow(bool minimize = false)
        {
            if (App.Current is { MainWindow: var wind})
            {
                wind = new MainWindow();

                if (minimize)
                {
                    wind.WindowState = WindowState.Minimized;
                }
                else
                {
                    wind.Show();
                }

                var inp = new WindowInteropHelper(wind);

                try
                {
                    if (App.Current is null) { throw null!; }

                    var ctx = (ContextMenu)App.Current.TryFindResource("OpenFrp.Launcher.App.ContextMenu");

                    ctx.SetBinding(ModernWpf.ThemeManager.RequestedThemeProperty, new Binding
                    {
                        Source = wind,
                        Mode = BindingMode.OneWay,
                        Path = new PropertyPath(ModernWpf.ThemeManager.RequestedThemeProperty)
                    });
                    TaskBarIcon = new H.NotifyIcon.TaskbarIcon()
                    {
                        NoLeftClickDelay = true,
                        LeftClickCommand = ShowWindowCommand,
                        ToolTipText = "OpenFrp 桌面启动器",
                        IconSource = new BitmapImage(new Uri("pack://application:,,,/Resources/desktop.ico")),
                        ContextMenu = ctx
                    };
                    TaskBarIcon.ForceCreate(false);
                }
                catch (Exception?) { }

                if (Awe.UI.Win32.UserUxtheme.IsSupportDarkMode)
                {
                    Awe.UI.Win32.UserUxtheme.AllowDarkModeForApp(true);
                    Awe.UI.Win32.UserUxtheme.ShouldAppsUseDarkMode();
                    Awe.UI.Win32.UserUxtheme.ShouldSystemUseDarkMode();
                }

                if (Settings.FontFamily is null)
                {
                    Settings.FontFamily = new FontFamily("Microsoft YaHei UI");
                }

                wind.SetValue(ModernWpf.ThemeManager.RequestedThemeProperty, Settings.ApplicationTheme);
                if (Settings.ApplicationBackdrop is { } backdrop && backdrop != ModernWpf.Controls.Primitives.BackdropType.None)
                {
                    wind.SetValue(ModernWpf.Controls.Primitives.WindowHelper.SystemBackdropTypeProperty, backdrop);
                }

                var handle = new WindowInteropHelper(wind).EnsureHandle();
                __mainWindHandle = handle;
                Awe.UI.Win32.UserUxtheme.SetWindowLong(handle, -16, Awe.UI.Win32.UserUxtheme.GetWindowLong(handle, -16) & ~0x80000);
            }
            Microsoft.Win32.SystemEvents.SessionEnding += (_, e) =>
            {
                
                //try { ServiceProcess.EnableRaisingEvents = false; } catch { }
                TokenSource.Cancel(false);

                e.Cancel = true;

                if (WindowsServiceCall.IsInstalledService())
                {
                    Settings.Account = new Service.UsrLogin
                    {
                        UserAuthroization = HttpRequest.GetUserAuthroization("of-dev-api.bfsea.xyz")
                    };
                    Environment.Exit(0);

                    return;
                }

                if (App.Current is { MainWindow: { DataContext: ViewModels.MainViewModel vm } window })
                {
                    window.Visibility = Visibility.Hidden;
                    Settings.AutoStartupTunnelId = vm.OnlineTunnels.ToArray();
                }
                Settings.Account = new Service.UsrLogin
                {
                    UserAuthroization = HttpRequest.GetUserAuthroization("of-dev-api.bfsea.xyz")
                };

                Settings.Save();

                ClearNotifications();
                ClearWebviewRuntimeCache();

                KillServiceProcess(true);
                Environment.Exit(0);

            };
        }

        private static void ConfigureUpdateWindow()
        {
            var wnd = new UpdateWindow();

            if (Awe.UI.Win32.UserUxtheme.IsSupportDarkMode)
            {
                Awe.UI.Win32.UserUxtheme.AllowDarkModeForApp(true);
                Awe.UI.Win32.UserUxtheme.ShouldAppsUseDarkMode();
                Awe.UI.Win32.UserUxtheme.ShouldSystemUseDarkMode();
            }

            _ = ConfigureVersionCheck(FrpcVersionString);

            wnd.ShowDialog();
        }

        #endregion

        internal static async Task<string> GetFrpcVersionAsync()
        {
            string platform = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "amd64",
                Architecture.X86 => "386",
                Architecture.Arm64 => "arm64",
                _ => throw new NotSupportedException("本软件暂不支持 ARMv7 等其他平台。"),
            };
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc");
            var pathForFile = Path.Combine(path, $"frpc_windows_{platform}.exe");

            if (!File.Exists(pathForFile))
            {
                return "non-frp";
            }
            try
            {
                var vk = Process.Start(new ProcessStartInfo
                {
                    WorkingDirectory = path,
                    RedirectStandardOutput = true,
                    FileName = pathForFile,
                    Arguments = "-v",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                await Task.Run(vk.WaitForExit);

                while (!vk.StandardOutput.EndOfStream)
                {
                    string str = await vk.StandardOutput.ReadLineAsync();
                    if (str.Contains("OpenFRP_"))
                    {
                        return str;
                    }
                }


                Analytics.TrackEvent("User's FRPC detect failed!", new Dictionary<string, string>(){
                    { "File",$"{pathForFile}" }
                 });
            }
            catch (System.ComponentModel.Win32Exception)
            {

            }

            return string.Empty;
        }

        internal static async void TryAutoLogin()
        {
            try
            {
                if (Settings.Account is not { } account || Settings.Account.IsEmpty())
                {
                    return;
                }
                try
                {
                    HttpRequest.CreateAuthorization("of-dev-api.bfsea.xyz", account.UserAuthroization!);

                    var openfrpUserinfo = await OpenFrp.Service.Net.OpenFrp.GetUserInfo();

                    if (openfrpUserinfo is { Exception: null, StatusCode: HttpStatusCode.OK, Data: UserInfo userInfo } &&
                        userInfo != null)
                    {
                        // finish
                        var rrpc = await RpcManager.LoginAsync(new Service.Proto.Request.LoginRequest
                        {
                            UserToken = userInfo.UserToken,
                            UserTag = $"@!{userInfo.UserID}+{userInfo.UserName}",
                                        
                        }, TimeSpan.FromSeconds(10));

                        if (rrpc.IsSuccess)
                        {
                            WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(userInfo));
                            RpcManager.UserSecureCode = rrpc.Data;
                            @appLaunchTunnel_auto();

                            return;
                        }
                        Service.Net.OpenFrp.Logout();
                    }
                }
                catch
                {

                }
            }
            catch { }
        }

        private static void @appLaunchTunnel_auto()
        {
            if (WindowsServiceCall.IsInstalledService()) return;

            ThreadPool.QueueUserWorkItem(async delegate
            {
                try
                {
                    if (Launcher.Properties.Settings.Default.AutoStartupTunnelId is { Length: > 0 } tb)
                    {
                        var userTunnels = await Service.Net.OpenFrp.GetUserTunnels();
                        if (userTunnels.StatusCode is HttpStatusCode.OK && userTunnels.Data is { Total: > 0 } && userTunnels.Data.List is { } list)
                        {
                            var resp = await RpcManager.SyncAsync(new Service.Proto.Request.SyncRequest
                            {
                                SecureCode = RpcManager.UserSecureCode
                            });
                            if (resp.IsSuccess && resp.Data is { })
                            {
                                
                                var online = new HashSet<int>(resp.Data.TunnelId);

                                foreach (var item in userTunnels.Data!.List!)
                                {
                                    if (tb.Contains(item.Id))
                                    {
                                        if ((!string.IsNullOrEmpty(item.TunnelCustomConfig) && Regex.IsMatch(item.TunnelCustomConfig, "launcher.isMinecraftService( )?=( )?(T)?(t)?(rue)?(RUE)?"))
                                        || online.Contains(item.Id))
                                        {
                                            break;
                                        }
                                        if (await RpcManager.LaunchTunnel(item) is { IsSuccess: true})
                                        {
                                            online.Add(item.Id);
                                        }
                                    }
                                }
                 
                                WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(online.ToArray()));
                            }
                        }
                    }

                }
                catch { }
            });
        }

        public static RelayCommand DestoryAppCommand { get; } = new RelayCommand(async delegate
        {
            App.Current.MainWindow.Visibility = Visibility.Hidden;

            if (!WindowsServiceCall.IsInstalledService())
            {
                var resp = await RpcManager.SyncAsync(TimeSpan.FromSeconds(5));
                if (resp.IsSuccess && resp.Data is { UserLogon: true })
                {
                    Settings.AutoStartupTunnelId = resp.Data.TunnelId.ToArray();
                }
            }
            else
            {
                Settings.AutoStartupTunnelId = Array.Empty<int>();
            }

            Settings.Account = new Service.UsrLogin
            {
                UserAuthroization = HttpRequest.GetUserAuthroization("of-dev-api.bfsea.xyz")
            };

            Settings.Save();
            ClearNotifications();
            ClearWebviewRuntimeCache();
           
            KillServiceProcess(true);
         
            App.Current.Shutdown();
        });

        public static RelayCommand DestoryLauncherCommand { get; } = new RelayCommand(async delegate
        {
            await Task.Delay(250);

            if (!WindowsServiceCall.IsInstalledService())
            {
                var resp = await RpcManager.SyncAsync(TimeSpan.FromSeconds(5));
                if (resp.IsSuccess && resp.Data is { UserLogon: true })
                {
                    Settings.AutoStartupTunnelId = resp.Data.TunnelId.ToArray();
                }
            }
            else
            {
                Settings.AutoStartupTunnelId = Array.Empty<int>();
            }

            Settings.Account = new Service.UsrLogin
            {
                UserAuthroization = HttpRequest.GetUserAuthroization("of-dev-api.bfsea.xyz")
            };

            Settings.Save();

            ClearNotifications();
            ClearWebviewRuntimeCache();
     
            App.Current.Shutdown();
        });

        public static RelayCommand ShowWindowCommand { get; } = new RelayCommand(() =>
        {
            if (App.Current is { MainWindow: var wind })
            {
                wind.ShowInTaskbar();
                wind.Show();

                if (App.Current.MainWindow.WindowState is WindowState.Minimized)
                {
                    App.Current.MainWindow.WindowState = WindowState.Normal;
                }
                Awe.UI.Win32.User32.SetForegroundWindow(__mainWindHandle);

                wind.Activate();
            }
        });

        private static IntPtr __mainWindHandle = IntPtr.Zero;
    }

    internal static class ExtendMethod
    {
        public static bool IsNotNullOrEmpty(this string? str) => !string.IsNullOrEmpty(str);

        public static async Task<T?> WithTimeout<T>(this Task<T> task,TimeSpan timeout)
        {
            Task tk = await Task.WhenAny(Task.Delay(timeout),task);
            if (tk.Equals(task)) { return await task; }

            return default;
        }

        public static async Task<T?> WithTimeout<T>(this Task<T> task, int delay)
        {
            Task tk = await Task.WhenAny(Task.Delay(delay), task);

            if (tk.Equals(task)) { return await task; }

            return default;
        }
    }
}
