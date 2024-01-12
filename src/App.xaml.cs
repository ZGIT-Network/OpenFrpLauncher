using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Grpc.Core;
using OpenFrp.Launcher.Controls;
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
#pragma warning disable CS8618
        // 必须不为 Null
        public static OpenFrp.Service.Proto.Service.OpenFrp.OpenFrpClient RemoteClient { get; set; }

        public static H.NotifyIcon.TaskbarIcon TaskBarIcon { get; set; }

        public static Process ServiceProcess { get; set; }
#pragma warning restore CS8618

        protected override void OnStartup(StartupEventArgs e)
        {
            if (Environment.OSVersion.Version.Major is 10)
            {
                if (Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
                {
                    Environment.Exit(0);
                    return;
                }
            }
            ConfigureWindow();
            ConfigureRPC();
            ConfigureToast();
            ConfigureProcess();
            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ClearToast();
        }

        private static void ClearToast()
        {
            if (Environment.OSVersion.Version.Major is not 10) return;
            Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
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
                            if (bd is not null)
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

                                                WeakReferenceMessenger.Default.Send("refresh");
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
                                            var selz = JsonSerializer.Deserialize<dynamic>(sc.ResponseStream.Current.Message);
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
                                    if (!ServiceProcess.HasExited)
                                    {
                                        ServiceProcess.EnableRaisingEvents = false;
                                        ServiceProcess.Kill();
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

            RefreshApplicationTheme(wind, false);

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
