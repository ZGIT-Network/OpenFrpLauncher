using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Awe.Model.OpenFrp.Response.Data;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Grpc.Core;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using OpenFrp.Launcher.Controls;
using Windows.UI.ViewManagement;
using static Google.Protobuf.WellKnownTypes.Field.Types;

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

        public static string BuildName { get; } = "v104";

        public static string VersionString { get; } = "Yue.OpenFRPLauncher.v10";

#pragma warning disable CS8618
        // 必须不为 Null
        public static OpenFrp.Service.Proto.Service.OpenFrp.OpenFrpClient RemoteClient { get; set; }

        public static H.NotifyIcon.TaskbarIcon TaskBarIcon { get; set; }

        public static Process ServiceProcess { get; set; }
#pragma warning restore CS8618

    

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (Environment.OSVersion.Version.Major is 10)
            {
                if (Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
                {
                    Environment.Exit(0);
                    return;
                }
            }
            
            if (e.Args.Contains("--update"))
            {
                if (Process.GetProcessesByName("OpenFrpService") is { Length: > 0 } ck)
                {
                    foreach (var item in ck)
                    {
                        item.Kill();
                    }

                    return;
                }
                ConfigureUpdateApp();
                Environment.Exit(0);
            }
            var frpVersion = await GetFrpcVersionAsync();

            if (!await ForceCheckVersionApp())
            {
                Environment.Exit(0);
                return;
            }
            if ("non-frp".Equals(frpVersion))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    Verb = "runas",
                    Arguments = "--update",
                    ErrorDialog = false
                });
                Environment.Exit(0);
                return;
            }

            ConfigureAppCenter();
            ConfigureWindow();
            TryAutoLogin();
            ConfigureRPC();
            ConfigureToast();
            ConfigureProcess();
            ConfigureVersionCheck(frpVersion);
        }

        protected override void OnExit(ExitEventArgs e) => ClearToast();


        private static async void ConfigureAppCenter()
        {
            var id = (await AppCenter.GetInstallIdAsync()).ToString();
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

            Analytics.EnableManualSessionTracker();
            AppCenter.Start("07ba1344-c34b-42ec-83f5-511448b065a1", typeof(Crashes), typeof(Analytics));
            AppCenter.SetUserId(id.ToString());
            AppCenter.SetCountryCode(System.Globalization.CultureInfo.CurrentUICulture.EnglishName);

        }

        private static void ClearToast()
        {
            if (Environment.OSVersion.Version.Major is not 10) return;
            Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
        }

        private static bool IsDarkBackground(Windows.UI.Color color)
        {
            return color.R + color.G + color.B < (255 * 3 - color.R - color.G - color.B);
        }

        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="NullReferenceException"></exception>
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
                //throw new FileNotFoundException("FRPC 文件丢失");
            }

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


            Analytics.TrackEvent("User's FRPC detect failed!",new Dictionary<string, string>(){
                { "File",$"{pathForFile}" }
            });

            return string.Empty;
            //throw new NullReferenceException();
        }

        #region Configure

        private static void ConfigureUpdateApp()
        {
            var dialog = new UpdateWindow()
            {

            };

            if (Awe.UI.Win32.UserUxtheme.IsSupportDarkMode)
            {
                Awe.UI.Win32.UserUxtheme.AllowDarkModeForApp(true);
                Awe.UI.Win32.UserUxtheme.ShouldAppsUseDarkMode();
                Awe.UI.Win32.UserUxtheme.ShouldSystemUseDarkMode();
            }
            var useLightMode = true;
            if (Environment.OSVersion.Version.Major is 10)
            {
                var uiSettings = new Windows.UI.ViewManagement.UISettings();

                if (IsDarkBackground(uiSettings.GetColorValue(UIColorType.Background)))
                {
                    useLightMode = false;
                }
            }
            RefreshApplicationTheme(dialog, useLightMode);

            dialog.ShowDialog();
        }

        private static void ConfigureProcess()
        {
            if (Process.GetProcessesByName("OpenFrpService") is { Length: > 0 } ck)
            {
                ServiceProcess = ck.First();

                return;
            }
            ServiceProcess = new Process()
            {
                StartInfo =
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenFrpService.exe"),
                    CreateNoWindow = true,
                    Arguments = "deamon --pn aweapp.test",
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            ServiceProcess.Start();
            ServiceProcess.Exited += async delegate
            {
                await Task.Delay(500);

                ConfigureProcess();
            };
        }

        private static async void ConfigureVersionCheck(string frpVersion)
        {
            var versionData = await OpenFrp.Service.Net.OpenFrp.GetSoftwareVersionInfo();

            if (versionData.Exception is null && versionData.StatusCode is System.Net.HttpStatusCode.OK)
            {
                if (!VersionString.Equals(versionData.Data?.Launcher.Latest))
                {
                    WeakReferenceMessenger.Default.Send(new Model.UpdateInfo
                    {
                        Type = Model.UpdateInfoType.Launcher,
                        SoftWareVersionData = versionData.Data,
                    });
                    return;
                }
                try
                {
                    if (!frpVersion.Equals(versionData.Data?.Latest))
                    {
                        WeakReferenceMessenger.Default.Send(new Model.UpdateInfo
                        {
                            Type = Model.UpdateInfoType.FrpClient,
                            SoftWareVersionData = versionData.Data,
                            Log = versionData.Data?.FrpcUpdateLog + 
                            $"\nUpdate: {frpVersion} => {versionData.Data?.Latest}" +
                            $"\n请注意: 若您在使用 FRPC 映射远程服务，请备用远程方式，否则请不要更新！"
                        });
                        // frpc has update
                    }
                }
                catch
                {
                    // frpc version check failed
                }
            }
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
            var channel = new GrpcDotNetNamedPipes.NamedPipeChannel(".", "aweapp.test",new GrpcDotNetNamedPipes.NamedPipeChannelOptions
            {
                ConnectionTimeout = 10
            });

            RemoteClient = new Service.Proto.Service.OpenFrp.OpenFrpClient(channel);

            while (true)
            {
                var va = await ExtendMethod.RunWithTryCatch(async () => await RemoteClient.SyncAsync(new Service.Proto.Request.SyncRequest()));

                if (va is (var data, _) && data is not null)
                {
                    WeakReferenceMessenger.Default.Send(data.TunnelId.ToArray());

                    try
                    {
                        var sc = RemoteClient.NotifiyStream(new Google.Protobuf.WellKnownTypes.Empty());

                        WeakReferenceMessenger.Default.Send("onService");
                        // message sender here
                        while (await sc.ResponseStream.MoveNext())
                        {
                            var bd = JsonSerializer.Deserialize<Awe.Model.OpenFrp.Response.Data.UserTunnel>(sc.ResponseStream.Current.TunnelJson);
                            if (bd is not null && sc.ResponseStream.Current is { } cr)
                            {
                                switch (sc.ResponseStream.Current.State)
                                {
                                    case Service.Proto.Response.NotiflyStreamState.LaunchSuccess:
                                        {
                                            var sb = new StringBuilder();

                                            if ("HTTP".Contains(bd.Type))
                                            {
                                                foreach (var item in bd.Domains)
                                                {
                                                    sb.Append(item + ",");
                                                }
                                            }
                                            if (Environment.OSVersion.Version.Major is 10)
                                            {
                                                new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                                .AddText($"隧道 {bd.Name} 启动成功!", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                                .AddText($"点击\"复制按钮\"复制链接地址,开始你的映射之旅吧。")
                                                .AddText($"可用地址: {("HTTP".Contains(bd.Type) ? sb.ToString().Remove(sb.Length - 1) : bd.ConnectAddress)}")
                                                .AddAttributionText($"{bd.Type!.ToUpper()} {bd.Host}:{bd.Port}")
                                                .AddButton("复制链接", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, $"copy {("HTTP".Contains(bd.Type) ? bd.Domains.First() : bd.ConnectAddress)}")
                                                .AddButton("确定", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, "none")
                                                .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                                .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                                .Show(toast =>
                                                {
                                                    toast.Tag = bd.Name;
                                                    toast.ExpiresOnReboot = true;
                                                    toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                                });
                                            }
                                            else if (TaskBarIcon is not null)
                                            {
                                                TaskBarIcon.ShowNotification($"隧道 {bd.Name} 启动成功!", $"可用地址: {("HTTP".Contains(bd.Type) ? sb.ToString().Remove(sb.Length - 1) : bd.ConnectAddress)}",
                                                    icon: H.NotifyIcon.Core.NotificationIcon.Info, timeout: TimeSpan.FromSeconds(10));
                                            }
                                            break;
                                        };
                                    case Service.Proto.Response.NotiflyStreamState.NoticeForLauncher when sc.ResponseStream.Current.Message.Contains("被 Console 要求下线"):
                                        {
                                            var vac = await ExtendMethod.RunWithTryCatch(async () => await RemoteClient.SyncAsync(new Service.Proto.Request.SyncRequest()));

                                            if (va is (var datac, _) && datac is not null)
                                            {
                                                WeakReferenceMessenger.Default.Send(datac.TunnelId.ToArray());

                                                await Task.Delay(500);

                                                WeakReferenceMessenger.Default.Send(new Tuple<string,object?>("refresh",default));
                                            }

                                            ;break;
                                        }
                                    case Service.Proto.Response.NotiflyStreamState.LaunchFailed:
                                        {
                                            if (Environment.OSVersion.Version.Major is 10)
                                            {
                                                new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                                 .AddText($"隧道 {bd.Name} 启动失败", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                                 .AddText(sc.ResponseStream.Current.Message)
                                                 .AddAttributionText($"{bd.Type!.ToUpper()} {bd.Host}:{bd.Port}")
                                                 .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                                 .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                                 .Show(toast =>
                                                 {
                                                     toast.Tag = bd.Name;
                                                     toast.ExpiresOnReboot = true;
                                                     toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                                 });
                                            }
                                            else if (TaskBarIcon is not null)
                                            {
                                                TaskBarIcon.ShowNotification($"隧道 {bd.Name} 启动失败", sc.ResponseStream.Current.Message,
                                                    icon: H.NotifyIcon.Core.NotificationIcon.Error, timeout: TimeSpan.FromSeconds(10));
                                            }
                                            break;
                                        }
                                    case Service.Proto.Response.NotiflyStreamState.NoticeForProxyUpdate:
                                        {
                                            if ("openfrp.app.closeProcessMainly".Equals(cr.Message))
                                            {
                                                WeakReferenceMessenger.Default.Send(new Tuple<string, object?>("openfrp.app.closeProcessMainly", bd));
                                            }
                                            
                                            break;
                                        }
                                }
                            }
                        }

                    }
                    catch
                    {
                        WeakReferenceMessenger.Default.Send("offService");
                    }
                }
                else
                {
                    WeakReferenceMessenger.Default.Send("offService");
                }
                await Task.Delay(1000);
            }
        }

        private static void ConfigureWindow()
        {
            var wind = App.Current?.MainWindow;
            if (App.Current?.MainWindow is null)
            {
                wind = new MainWindow();
                wind.Show();

                try
                {
                    if (App.Current is null) { throw null!; }

                    TaskBarIcon = new H.NotifyIcon.TaskbarIcon()
                    {
                        NoLeftClickDelay = true,
                        LeftClickCommand = new RelayCommand(() =>
                        {
                            App.Current!.MainWindow.Visibility = Visibility.Visible;
                            if (App.Current.MainWindow.WindowState is WindowState.Minimized)
                            {
                                App.Current.MainWindow.Topmost = true;
                                App.Current.MainWindow.Topmost = false;
                            }
                            App.Current.MainWindow.Activate();
                        }),
                        ToolTipText = "OpenFrp 桌面启动器",
                        IconSource = new BitmapImage(new Uri("pack://application:,,,/Resources/desktop.ico")),
                        ContextMenu = CreateObject<System.Windows.Controls.ContextMenu>(x =>
                        {
                            x.Width = 175;
                            x.SetBinding(Awe.UI.Helper.WindowsHelper.UseLightModeProperty, new Binding
                            {
                                Source = wind,
                                Path = new PropertyPath(Awe.UI.Helper.WindowsHelper.UseLightModeProperty),
                                Mode = BindingMode.OneWay
                            });
                            x.Items.Add(CreateObject<MenuItem>((xa) =>
                            {
                                xa.Header = "显示窗口";
                                xa.SetBinding(Awe.UI.Helper.WindowsHelper.UseLightModeProperty, new Binding
                                {
                                    Source = x,
                                    Path = new PropertyPath(Awe.UI.Helper.WindowsHelper.UseLightModeProperty),
                                    Mode = BindingMode.OneWay
                                });
                                xa.Command = new RelayCommand(() =>
                                {
                                    App.Current!.MainWindow.Visibility = Visibility.Visible;
                                    if (App.Current.MainWindow.WindowState is WindowState.Minimized)
                                    {
                                        App.Current.MainWindow.Topmost = true;
                                        App.Current.MainWindow.Topmost = false;
                                    }
                                    App.Current.MainWindow.Activate();
                                });
                            }));
                            x.Items.Add(new Separator() { Style = (Style)App.Current!.TryFindResource("RewriteSeparator") });
                            x.Items.Add(CreateObject<MenuItem>((xa)=>
                            {
                                xa.Header = "退出";

                                xa.SetBinding(Awe.UI.Helper.WindowsHelper.UseLightModeProperty, new Binding
                                {
                                    Source = x,
                                    Path = new PropertyPath(Awe.UI.Helper.WindowsHelper.UseLightModeProperty),
                                    Mode = BindingMode.OneWay
                                });
                                xa.Command = new RelayCommand(() =>
                                {
                                    OpenFrp.Launcher.Properties.Settings.Default.Save();
                                    App.Current.Shutdown();
                                    //Environment.Exit(0);
                                });
                            }));
                            x.Items.Add(CreateObject<MenuItem>((xa) =>
                            {
                                xa.Header = "彻底退出";
                                xa.SetBinding(Awe.UI.Helper.WindowsHelper.UseLightModeProperty, new Binding
                                {
                                    Source = x,
                                    Path = new PropertyPath(Awe.UI.Helper.WindowsHelper.UseLightModeProperty),
                                    Mode = BindingMode.OneWay
                                }); 
                                xa.Command = new RelayCommand(() =>
                                {
                                    App.Current!.MainWindow.Visibility = Visibility.Hidden;
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


                                    OpenFrp.Launcher.Properties.Settings.Default.Save();

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
                            }));
                            foreach (var va in x.Items)
                            {
                                if (va is DependencyObject doc)
                                {
                                    doc.SetValue(Awe.UI.Helper.WindowsHelper.LightModeRebindProperty, true);
                                }
                            }
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

            

            if (wind is null) { App.Current?.Shutdown(); return; }

            var useLightMode = OpenFrp.Launcher.Properties.Settings.Default.UseLightMode;


            if (Environment.OSVersion.Version.Major is 10)
            {
                try
                {
                    var uiSettings = new Windows.UI.ViewManagement.UISettings();

                    if (IsDarkBackground(uiSettings.GetColorValue(UIColorType.Background)))
                    {
                        RefreshApplicationTheme(wind, false);
                        return;
                    }
                }
                catch
                {
                    // not support query
                }
            }
            OpenFrp.Launcher.Properties.Settings.Default.FollowSystemTheme = false;
            RefreshApplicationTheme(wind, useLightMode);

        }

        #endregion

        private static async void TryAutoLogin()
        {
            try
            {
                var ot = JsonSerializer.Deserialize<Awe.Model.OAuth.Request.LoginRequest>(OpenFrp.Launcher.Properties.Settings.Default.UserPwn);
                if (ot is { } c && !string.IsNullOrEmpty(c.Password))
                {
                    var pwn = Encoding.UTF8.GetString(OpenFrp.Launcher.Properties.PndCodec.Descrypt(c.Password!));

                    var fallbackTask = new TaskCompletionSource<Awe.Model.OpenFrp.Response.Data.UserInfo>();
                    var mwn = new ViewModels.LoginDialogVIewModel()
                    {
                        Username = c.Username,
                        Password = pwn,
                        UserInfoFallback = fallbackTask
                    };
                    


                    await mwn.event_LoginCommand.ExecuteAsync(null);

                    if (string.IsNullOrEmpty(mwn.Reason))
                    {
                        if (await fallbackTask.Task is { } info)
                        {
                            WeakReferenceMessenger.Default.Send(info);
                        }
                        // success
                    }
                }
            }
            catch
            {

            }
        }
        private static async Task<bool> ForceCheckVersionApp()
        {
            if (Debugger.IsAttached) return true;
            try
            {
                var resp = await OpenFrp.Service.Net.HttpRequest.Get<JsonElement>("https://api.mclan.icu/api/news?query=openfrpLauncherPreview");

                if (resp.Exception is null && resp.StatusCode is System.Net.HttpStatusCode.OK &&
                    resp.Data.GetProperty("data") is { } data)
                {
                    if (BuildName.Equals(data.GetProperty("url").ToString()) &&
                        "测试中".Equals(data.GetProperty("title").ToString()))
                    {
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(data.GetProperty("subtitle").ToString(), "OpenFRP Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"{resp.Exception?.Message} {resp.Message}", "OpenFRP Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.ToString() + "\n点击\"确定\"将继续进入 App，老版本造成的问题一律不解决。", "OpenFRP Launcher", MessageBoxButton.OKCancel, MessageBoxImage.Warning) is MessageBoxResult.OK)
                {
                    return true;
                }
            }
            return false;
        }

        internal static void RefreshApplicationTheme(Window wind,bool useLightMode = false)
        {
            var handle = new WindowInteropHelper(wind).EnsureHandle();


            if (Environment.OSVersion.Version.Major >= 10)
            {
                int backdropPvAttribute = (int)Awe.UI.Win32.DwmApi.DWMSBT.DWMSBT_TABBEDWINDOW;
                Awe.UI.Win32.DwmApi.DwmSetWindowAttribute(handle, Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdropPvAttribute,
                    Marshal.SizeOf(typeof(int)));

                var pvAttribute = useLightMode ? (int)Awe.UI.Win32.DwmApi.PvAttribute.Disable : (int)Awe.UI.Win32.DwmApi.PvAttribute.Enable;

                var dwAttribute = Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

                if (Environment.OSVersion.Version < new Version(10, 0, 18985))
                {
                    dwAttribute = Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
                }

                var vk = Awe.UI.Win32.DwmApi.DwmSetWindowAttribute(handle, dwAttribute,
                    ref pvAttribute,
                    Marshal.SizeOf(typeof(int)));

                Awe.UI.WindowCommand.ChangeThemePerferences(pvAttribute is (int)Awe.UI.Win32.DwmApi.PvAttribute.Disable);

                if (vk is not 0)
                {
                    wind.Tag = "non-success-dark";
                }
                else
                {
                    wind.Width += 1;
                    wind.Width -= 1;
                }

                wind.SetCurrentValue(Window.ForegroundProperty, new System.Windows.Media.SolidColorBrush
                {
                    Color = pvAttribute is (int)Awe.UI.Win32.DwmApi.PvAttribute.Disable ? Colors.Black : Colors.White
                });
                wind.SetCurrentValue(Awe.UI.Helper.WindowsHelper.UseLightModeProperty, pvAttribute is (int)Awe.UI.Win32.DwmApi.PvAttribute.Disable);

            }

            Awe.UI.Win32.UserUxtheme.SetWindowLong(handle, -16, Awe.UI.Win32.UserUxtheme.GetWindowLong(handle, -16) & ~0x80000);
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
            var tk = Task.WhenAny(task, Task.Delay(timeout));
            if (tk.Equals(task)) { return await task; }

            return default;
        }

        public static async Task<(T? data,Exception? ex)> RunWithTryCatch<T>(Func<Task<T>> task)
        {
            try
            {
                return (await Task.Run(task),default);
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
                System.Diagnostics.Debug.WriteLine(re.ToString());
                return (default,re);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                global::System.Diagnostics.Debugger.Break();

                return (default, ex);
            }
            // return (default, default);
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
                System.Diagnostics.Debug.WriteLine(re.ToString());
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

        //public static async Task<T?> WithTryCatch<T>(this Grpc.Core.AsyncUnaryCall<T> task)
        //{
        //    try
        //    {
        //        return await Task.Run(async()=>await task);
        //    }
        //    catch (Grpc.Core.RpcException re)
        //    {
        //        if (re.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
        //        {
        //            // 访问守护进程超时，重试。
        //        }
        //        else if (re.StatusCode == Grpc.Core.StatusCode.Unavailable && re.Status.Detail.Equals("failed to connect to all addresses"))
        //        {

        //        }
        //    }
        //    return default;
        //}
    }
}
