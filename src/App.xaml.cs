﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
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
using OpenFrp.Service.Net;


namespace OpenFrp.Launcher
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            
        }

        public static string? WebViewTemplatePath { get; set; }

        public static string VersionString { get; } = "OpenFrpLauncher.v100.RowcurGemmagu";

        public static string FrpcVersionString { get; private set; } = "Unknown";

        private static Properties.Settings Settings 
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
            if (e.Args.Contains("--uap"))
            {
                try
                {
                    var fe = OpenFrp.Service.Commands.FileDictionary.GetAutoStartupFile();
                    try
                    {
                        System.IO.File.Delete(fe);
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
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                Settings.Reset();
                try
                {
                    File.Delete(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
                }
                catch { }
                App.Current.Shutdown(0);
                Environment.Exit(0);
                return;
            }
            if (e.Args.Contains("--update"))
            {
                FrpcVersionString = await GetFrpcVersionAsync();

                if (Process.GetProcessesByName("OpenFrpService") is { Length: > 0 } ck)
                {
                    var asm = Assembly.GetAssembly(typeof(OpenFrp.Service.RpcResponse));

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


                var va = OpenFrp.Service.Commands.FileDictionary.GetFrpPlatform();
                var fe = OpenFrp.Service.Commands.FileDictionary.GetFrpFile();

                if (Process.GetProcessesByName($"frpc_windows_{va}.exe") is { Length: > 0 } cka)
                {
                    foreach (var item in cka)
                    {
                        try
                        {
                            if (item.MainModule.FileName.Equals(fe))
                            {
                                item.Kill();
                            }
                        }
                        catch { }
                    }
                }

                SetSecureApp();

                _ = ConfigureVersionCheck(FrpcVersionString);
                ConfigureUpdateWindow();

                App.Current.Shutdown();
                return;
            }
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
                                    "OpenFrp 启动器已启动。若您想运行多个启动器实例，更推荐您使用 FRPC 进行进程管理。\n请单击\"托盘图标\"来显示主窗口。","OpenFrp Launcher",MessageBoxButton.OK,MessageBoxImage.Warning);

                                break;
                            }
                            item.Refresh();

                            Awe.UI.Win32.User32.ShowWindow(item.MainWindowHandle,9);
                            Awe.UI.Win32.User32.SetForegroundWindow(item.MainWindowHandle);

                            App.Current.Shutdown();
                            return;
                        }
                        
                        break;
                    }
                    
                }
            }
            catch {  }
            FrpcVersionString = await GetFrpcVersionAsync();
            if ("non-frp".Equals(FrpcVersionString))
            {
                if (e.Args.Contains("--finish"))
                {
                    if (MessageBox.Show("看起来下载的 FRPC 文件无效，是否重新下载？", "OpenFrp Launcher", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) is MessageBoxResult.Cancel)
                    {
                        Environment.Exit(0);
                        Current.Shutdown();

                        return;
                    }
                }
                if (IsAdministrator())
                {
                    SetSecureApp();

                    _ = ConfigureVersionCheck(FrpcVersionString);
                    ConfigureUpdateWindow();
                }
                else
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
                    }
                    catch
                    {

                    }
                }

                App.Current.Shutdown();
                return;
            }

            SetSecureApp();
            ConfigureProcess();
            ConfigureAppCenter();
            TryAutoLogin();
            ConfigureRPC();
            ConfigureToast();
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var va = await ConfigureVersionCheck(FrpcVersionString);

                    if (va is { Exception: null,StatusCode: HttpStatusCode.OK })
                    {
                        await Task.Delay(TimeSpan.FromHours(2));
                    }
                    else await Task.Delay(TimeSpan.FromMinutes(10));
                }
            });
            ConfigureWindow(e.Args);


        }

        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void SetSecureApp()
        {
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

        protected override void OnExit(ExitEventArgs e)
        {
            ClearToast();

            Settings.UserAuthorization = HttpRequest.GetUserAuthroization("of-dev-api.bfsea.xyz");
        }

        private static async void ConfigureAppCenter()
        {
            var id = (await AppCenter.GetInstallIdAsync()).ToString();
            AppCenter.SetUserId(id.ToString());
            Analytics.EnableManualSessionTracker();
            //AppCenter.SetCountryCode(System.Globalization.CultureInfo.CurrentUICulture.EnglishName);

            AppCenter.Start("07ba1344-c34b-42ec-83f5-511448b065a1",typeof(Analytics), typeof(Crashes));

            await AppCenter.SetEnabledAsync(true);

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Crashes.TrackError(ex);
                    Clipboard.SetText(ex.ToString());

                    MessageBox.Show($"错误内容已复制，按下Ctrl+V | 粘贴 来显示内容。" +
                    $"\n您也可以不反馈该问题，因为问题已上传到云端。\n会话 ID: {id}\n", "OpenFrp Launcher Throw Out!!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private static void ClearToast()
        {
            if (Environment.OSVersion.Version.Major is not 10) return;

            Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
        }

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

        #region Configure

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
                Clipboard.SetText(ex.ToString());

                MessageBox.Show($"\n!!!!!!!辅助进程启动失败!!!!!!\n{ex.Message}\n错误内容已复制，按下Ctrl+V | 粘贴 来显示内容。", "OpenFrp Launcher Throw Out!!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ServiceProcess.Exited += async delegate
            {
                await Task.Delay(500);

                ConfigureProcess();
            };
        }

        private static string @ev_AssemblyAc(int v = 0)
        {
            try
            {
                var va = Assembly.GetExecutingAssembly();

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

        private static async void ConfigureRPC()
        {
            RpcManager.Configre($"openfrpLauncher.{@ev_AssemblyAc()}");

            while (true)
            {
                var resp = await RpcManager.SyncAsync(new OpenFrp.Service.Proto.Request.SyncRequest { }); 
                if (resp.IsSuccess && resp.Data != null)
                {
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(resp.Data.TunnelId.ToArray()));
                    try
                    {
                        WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create("onService"));
                        var exception = await RpcManager.NotifiyStream(async (response) =>
                        {
                            var tunnel = JsonSerializer.Deserialize<Awe.Model.OpenFrp.Response.Data.UserTunnel>(response.TunnelJson);
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
                                            else if (TaskBarIcon is not null &&
                                                Settings.NotifyMode is Model.NotifyMode.NotifyIcon)
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
                        });
                        if (exception is not null)
                        {
                            Service.Net.OpenFrp.Logout();
                            
                            WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new Awe.Model.OpenFrp.Response.Data.UserInfo
                            {
                                UserName = "not-allow-display"
                            }));

                            

                            throw exception;
                        }
                    }
                    catch { }
                }
                WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create("offService"));
                await Task.Delay(1000).ContinueWith(delegate
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
                    TryAutoLogin();
                });
            }
        }

        private static void ConfigureWindow(string[] args)
        {
            var wind = App.Current?.MainWindow;
            if (App.Current?.MainWindow is null)
            {
                wind = new MainWindow();

              
                
                if (args.Contains("--minimize"))
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

                    TaskBarIcon = new H.NotifyIcon.TaskbarIcon()
                    {
                        NoLeftClickDelay = true,
                        LeftClickCommand = new RelayCommand(() =>
                        {
                            wind.ShowInTaskbar();
                            wind.Show();
                            
                            if (App.Current.MainWindow.WindowState is WindowState.Minimized)
                            {
                                App.Current.MainWindow.WindowState = WindowState.Normal;
                            }
                            Awe.UI.Win32.User32.SetForegroundWindow(inp.Handle);
                            
                            wind.Activate();
                        }),
                        ToolTipText = "OpenFrp 桌面启动器",
                        IconSource = new BitmapImage(new Uri("pack://application:,,,/Resources/desktop.ico")),
                        ContextMenu = CreateObject<System.Windows.Controls.ContextMenu>(x =>
                        {
                            x.SetBinding(ModernWpf.ThemeManager.RequestedThemeProperty, new Binding
                            {
                                Source = wind,
                                Path = new PropertyPath(ModernWpf.ThemeManager.RequestedThemeProperty),
                                Mode = BindingMode.OneWay
                            });
                            x.Width = 175;
                            x.HasDropShadow = false;
                            x.Items.Add(CreateObject<MenuItem>((xa) =>
                            {
                                xa.Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\uE8A7" };
                                xa.Header = "显示窗口";
                                xa.Command = new RelayCommand(() =>
                                {
                                    wind.ShowInTaskbar();
                                    wind.Show();

                                    if (App.Current.MainWindow.WindowState is WindowState.Minimized)
                                    {
                                        App.Current.MainWindow.WindowState = WindowState.Normal;
                                    }
                                    Awe.UI.Win32.User32.SetForegroundWindow(inp.Handle);

                                    wind.Activate();
                                });
                            }));
                            x.Items.Add(new Separator());
                            x.Items.Add(CreateObject<MenuItem>((xa)=>
                            {
                                xa.Header = "退出";
                                xa.Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\ue89f" };
                                xa.Command = new AsyncRelayCommand(async () =>
                                {
                                    x.IsOpen = false;

                                    await Task.Delay(300);

                                    _ = wind.Dispatcher.Invoke(async delegate
                                    {
                                        var resp = await RpcManager.SyncAsync(TimeSpan.FromSeconds(5));
                                        if (resp.IsSuccess && resp.Data is { UserLogon: true })
                                        {
                                            Settings.AutoStartupTunnelId = JsonSerializer.Serialize(resp.Data.TunnelId);
                                        }
                                        if (Environment.OSVersion.Version.Major is 10)
                                        {
                                            Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
                                        }
                                        Settings.Save();
                                        if (!string.IsNullOrEmpty(App.WebViewTemplatePath) && Directory.Exists(App.WebViewTemplatePath))
                                        {
                                            try { Directory.Delete(App.WebViewTemplatePath, true); } catch { }
                                        }
                                        App.Current.Shutdown();
                                    });
                                    //Environment.Exit(0);
                                });
                            }));
                            x.Items.Add(CreateObject<MenuItem>((xa) =>
                            {
                                xa.Header = "彻底退出";
                                xa.Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\ue8bb" };
                                xa.Command = new AsyncRelayCommand(async () =>
                                {
                                    x.IsOpen = false;

                                    await Task.Delay(300);

                                    _ = wind.Dispatcher.Invoke(async delegate
                                    {
                                        App.Current!.MainWindow.Visibility = Visibility.Hidden;
                                        var resp = await RpcManager.SyncAsync(TimeSpan.FromSeconds(5));
                                        if (resp.IsSuccess && resp.Data is { UserLogon: true })
                                        {
                                            Settings.AutoStartupTunnelId = JsonSerializer.Serialize(resp.Data.TunnelId);
                                        }

                                        Settings.Save();


                                        if (Environment.OSVersion.Version.Major is 10)
                                        {
                                            Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
                                        }
                                        try
                                        {
                                            if (!ServiceProcess.HasExited)
                                            {
                                                ServiceProcess.EnableRaisingEvents = false;
                                                ServiceProcess.Kill();
                                            }
                                        }
                                        catch
                                        {

                                        }

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
                                        Environment.Exit(0);
                                    });

                                    
                                });
                            }));
                            //foreach (var va in x.Items)
                            //{
                            //    if (va is DependencyObject doc)
                            //    {
                            //        //doc.SetValue(Awe.UI.Helper.WindowsHelper.LightModeRebindProperty, true);
                            //    }
                            //}
                        })
                    };
                    TaskBarIcon.ForceCreate(false);
                }
                catch (Exception?)
                {

                }
            }

            if (Awe.UI.Win32.UserUxtheme.IsSupportDarkMode)
            {
                Awe.UI.Win32.UserUxtheme.AllowDarkModeForApp(true);
                Awe.UI.Win32.UserUxtheme.ShouldAppsUseDarkMode();
                Awe.UI.Win32.UserUxtheme.ShouldSystemUseDarkMode();
            }

            Microsoft.Win32.SystemEvents.SessionEnding += async (_, e) =>
            {
                e.Cancel = true;

                App.Current!.MainWindow.Visibility = Visibility.Hidden;

                var resp = await RpcManager.SyncAsync(TimeSpan.FromSeconds(5));
                if (resp.IsSuccess && resp.Data is { UserLogon: true })
                {
                    Settings.AutoStartupTunnelId = JsonSerializer.Serialize(resp.Data.TunnelId);
                }

                Settings.Save();


                if (Environment.OSVersion.Version.Major is 10)
                {
                    Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
                }
                try
                {
                    if (!ServiceProcess.HasExited)
                    {
                        ServiceProcess.EnableRaisingEvents = false;
                        ServiceProcess.Kill();
                    }
                }
                catch
                {

                }

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
                Environment.Exit(0);


                App.Current?.Shutdown();
            };

            if (wind is null) { App.Current?.Shutdown(); return; }

            //var useLightMode = Settings.ApplicationTheme;

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
            Awe.UI.Win32.UserUxtheme.SetWindowLong(handle, -16, Awe.UI.Win32.UserUxtheme.GetWindowLong(handle, -16) & ~0x80000);

           

            //if (Environment.OSVersion.Version.Major is 10)
            //{
            //    if (Settings.FollowSystemTheme)
            //    {
            //        try
            //        {
            //            var uiSettings = new Windows.UI.ViewManagement.UISettings();

            //            if (IsDarkBackground(uiSettings.GetColorValue(UIColorType.Background)))
            //            {
            //                RefreshApplicationTheme(wind, false);
            //                return;
            //            }
            //        }
            //        catch
            //        {
            //            // not support query
            //        }
            //    }
            //}
            //else Settings.FollowSystemTheme = false;
            //RefreshApplicationTheme(wind, useLightMode);

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

        private static async void TryAutoLogin()
        {
            try
            {
                //string da_sisseTaqeeveLesi = Encoding.UTF8.GetString(Launcher.PndCodec.Descrypt(Settings.UserPwnRedirectUrl));

                if (!string.IsNullOrEmpty(Settings.UserPwn))
                {
                    //await @event_NowterTrojeMoumeanu_AutoLoginCode(Settings.UserPwn, da_sisseTaqeeveLesi);
                    var ot = JsonSerializer.Deserialize<Awe.Model.OAuth.Request.LoginRequest>(Settings.UserPwn);
                    if (ot is { } c && !string.IsNullOrEmpty(c.Password))
                    {
                        var pwn = Encoding.UTF8.GetString(Launcher.PndCodec.Descrypt(c.Password!));

                        var fallbackTask = new TaskCompletionSource<Awe.Model.OpenFrp.Response.Data.UserInfo>();

                        var mwn = new ViewModels.LoginDialogVIewModel()
                        {
                            Username = c.Username,
                            Password = pwn,
                            taskCompletionSource = fallbackTask
                        };

                        await mwn.event_LoginCommand.ExecuteAsync(null);

                        if (string.IsNullOrEmpty(mwn.Reason))
                        {
                            if (await fallbackTask.Task is { } info)
                            {
                                WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(info));
                            }
                        }
                    }
                }
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(Settings.UserAuthorization))
                {
                    HttpRequest.CreateAuthorization("of-dev-api.bfsea.xyz", Settings.UserAuthorization);

                    var openfrpUserinfo = await OpenFrp.Service.Net.OpenFrp.GetUserInfo();

                    if (openfrpUserinfo is { Exception: null, StatusCode: HttpStatusCode.OK, Data: UserInfo userInfo } &&
                        userInfo != null)
                    {
                        // finish
                        var rrpc = await RpcManager.LoginAsync(new Service.Proto.Request.LoginRequest
                        {
                            UserToken = userInfo.UserToken,
                            UserTag = $"@!{userInfo.UserID}+{userInfo.UserName}"
                        }, TimeSpan.FromSeconds(10));

                        if (rrpc.IsSuccess)
                        {
                            WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(userInfo));
                            RpcManager.UserSecureCode = rrpc.Data;

                            return;
                        }
                        Service.Net.OpenFrp.Logout();
                    }
                }
            }
            catch { }
        }

        private static T CreateObject<T>(Action<T>? func = default, params object[] args)
        {
            var vc = Activator.CreateInstance(typeof(T), args);

            if (vc is null) throw new NullReferenceException();
            else if (vc is T tt)
            {
                if (func is not null) { func(tt); }
                return tt;
            }
            throw new TypeLoadException();
        }
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


        public static async Task<Exception?> RunWithTryCatch(Func<Task> task)
        {
            try
            {
                await Task.Run(task);

                return default;
            }
            catch (Grpc.Core.RpcException re)
            {
                if (re.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
                {
                    // 访问守护进程超时，重试。
                }
                else if (re.StatusCode == Grpc.Core.StatusCode.Unavailable && re.Status.Detail.Equals("failed to connect to all addresses"))
                {

                }
                //System.Diagnostics.Debug.WriteLine(re.ToString());
                return re;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                global::System.Diagnostics.Debugger.Break();

                return ex;
            }
            // return (default, default);
        }

    }
}
