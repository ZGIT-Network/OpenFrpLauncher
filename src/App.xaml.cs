using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

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
            ConfigureWindow();
            // ConfigureRPC();    
        }

        private static async void ConfigureRPC()
        {
            var channel = new GrpcDotNetNamedPipes.NamedPipeChannel(".", "aweapp.test");

            var rpc = RemoteClient = new Service.Proto.Service.OpenFrp.OpenFrpClient(channel);


            var va = await ExtendMethod.RunWithTryCatch(async() => await rpc.SyncAsync(new Google.Protobuf.WellKnownTypes.Empty())) ;

            if (va is (var data,_) && data is not null)
            {
                WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<bool>(rpc, "IsPipeConnected", false, true));
            }
        }
        private static void ConfigureWindow()
        {
            var wind = new MainWindow();

            wind.Show();

            if (Awe.UI.Win32.UserUxtheme.IsSupportDarkMode)
            {
                Awe.UI.Win32.UserUxtheme.AllowDarkModeForApp(true);
                Awe.UI.Win32.UserUxtheme.ShouldAppsUseDarkMode();
                Awe.UI.Win32.UserUxtheme.ShouldSystemUseDarkMode();
            }

            var handle = new WindowInteropHelper(wind).EnsureHandle();

            if (Environment.OSVersion.Version.Major >= 10)
            {
                int backdropPvAttribute = (int)Awe.UI.Win32.DwmApi.DWMSBT.DWMSBT_TABBEDWINDOW;
                Awe.UI.Win32.DwmApi.DwmSetWindowAttribute(handle, Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdropPvAttribute,
                    Marshal.SizeOf(typeof(int)));

                var pvAttribute = (int)Awe.UI.Win32.DwmApi.PvAttribute.Enable;
                var dwAttribute = Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

                if (Environment.OSVersion.Version < new Version(10, 0, 18985))
                {
                    dwAttribute = Awe.UI.Win32.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
                }

                var vk = Awe.UI.Win32.DwmApi.DwmSetWindowAttribute(handle, dwAttribute,
                    ref pvAttribute,
                    Marshal.SizeOf(typeof(int)));


                if (vk is not 0)
                {
                    wind.Tag = "non-success-dark";
                }
                else
                {
                    wind.Width += 1;
                    wind.Width -= 1;
                }

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
                return (default,re);
            }
            catch (Exception)
            {

            }
            return (default, default);
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
