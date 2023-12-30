using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Grpc.Core;
using OpenFrp.Launcher.Controls;

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
#pragma warning restore CS8618

        protected override void OnStartup(StartupEventArgs e)
        {
            if (Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                Environment.Exit(0);
                return;
            }
            ConfigureWindow();
            ConfigureRPC();
            ConfigureToast();
            
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

                                            new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                                                .AddText($"隧道 {bd.Name} 启动成功!", Microsoft.Toolkit.Uwp.Notifications.AdaptiveTextStyle.Title)
                                                .AddText($"点击\"复制按钮\"复制链接地址,开始你的映射之旅吧。")
                                                .AddText($"可用地址: {("HTTP".Contains(bd.Type) ? sb.ToString().Remove(sb.Length - 1) : bd.ConnectAddress)}")
                                                .AddAttributionText($"{bd.Type!.ToUpper()} {bd.Host}:{bd.Port}")
                                                .AddButton("复制链接", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, $"copy {("HTTP".Contains(bd.Type) ? bd.Domains.First() : bd.ConnectAddress)}")
                                                .AddButton("确定", Microsoft.Toolkit.Uwp.Notifications.ToastActivationType.Foreground, "none")
                                                .SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short)
                                                .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default)
                                                .Show(toast => {
                                                                   toast.Tag = bd.Name;
                                                                   toast.ExpiresOnReboot = true;
                                                                   toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                                                               });
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
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                await Task.Delay(1000);
            }
        }
        private static bool _issfff;
        private static void ConfigureWindow()
        {
            var wind = App.Current?.MainWindow;
            if (App.Current?.MainWindow is null)
            {
                wind = new MainWindow();

                int cc = 0;
                wind.KeyDown += (e, s) =>
                {
                    if (s.Key is System.Windows.Input.Key.F12)
                    {
                        cc += 1;
                        if (cc is 2)
                        {
                            ConfigureWindow();

                            cc = 0;
                        }
                    }
                };
                wind.Show();
            }

            if (Awe.UI.Win32.UserUxtheme.IsSupportDarkMode)
            {
                Awe.UI.Win32.UserUxtheme.AllowDarkModeForApp(true);
                Awe.UI.Win32.UserUxtheme.ShouldAppsUseDarkMode();
                Awe.UI.Win32.UserUxtheme.ShouldSystemUseDarkMode();
            }

            if (wind is null) { App.Current?.Shutdown(); return; }

            var handle = new WindowInteropHelper(wind).EnsureHandle();

            if (Environment.OSVersion.Version.Major >= 10)
            {
                int backdropPvAttribute = (int)Awe.UI.Win32.DwmApi.DWMSBT.DWMSBT_TABBEDWINDOW;
                Awe.UI.Win32.DwmApi.DwmSetWindowAttribute(handle, Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdropPvAttribute,
                    Marshal.SizeOf(typeof(int)));

                var pvAttribute = _issfff ? (int)Awe.UI.Win32.DwmApi.PvAttribute.Disable : (int)Awe.UI.Win32.DwmApi.PvAttribute.Enable;

                _issfff = !_issfff;

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
