using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using OpenFrp.Launcher.Rpc;
using OpenFrp.Service.Daemon;
using iNKORE.UI.WPF.Helpers;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Diagnostics.Metrics;
using System.Runtime.ConstrainedExecution;
using static Google.Protobuf.WellKnownTypes.Field.Types;
using System.Text;
using System.Security.Principal;
using OpenFrp.Launcher.Win32;
using System.ComponentModel;


namespace OpenFrp.Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
            AppContext.SetSwitch("Switch.System.Windows.Media.EnableHardwareAccelerationInRdp", true);

            Dispatcher.UnhandledExceptionFilter += Dispatcher_UnhandledExceptionFilter;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.IsTerminating)
                    if (e.ExceptionObject is Exception ex)
                    {
                        try { Clipboard.SetText(ex.ToString()); }
                        catch
                        {
                            MessageBox.Show(ex.ToString(), "OpenFrp Launcher Throw Out!!", MessageBoxButton.OK, MessageBoxImage.Error);

                            Environment.Exit(ex.HResult);

                            return;
                        }

                        MessageBox.Show($"错误内容已复制，按下Ctrl+V | 粘贴 来显示内容。" +
                   $"\n{ex.Message}" , "OpenFrp Launcher Throw Out!!", MessageBoxButton.OK, MessageBoxImage.Error);

                        Environment.Exit(ex.HResult);
                    }
            };
#endif
        }

        internal static bool Notification_UseExpiredReboot { get; private set; } = false;

        internal static string LauncherMainModulePath { get; } = Path.Combine(AppContext.BaseDirectory, "OpenFrp.Launcher.exe");

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is TaskCanceledException or Grpc.Core.RpcException { StatusCode: Grpc.Core.StatusCode.Cancelled })
            {
                var flag = new StackTrace(e.Exception).GetFrames().Any(x =>
                {
                    if (x.GetMethod() is { DeclaringType.DeclaringType.Namespace: string @namespace } &&
                        @namespace.Contains("OpenFrp.Launcher.ViewModels"))
                    {
                        return true;
                    }
                    return false;
                });

                e.Handled = flag;
                return;
            }
        }

        private void Dispatcher_UnhandledExceptionFilter(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
        {
            // RequestCatch set true => avoid crash app!;

            if (e.Exception is TaskCanceledException or Grpc.Core.RpcException { StatusCode: Grpc.Core.StatusCode.Cancelled or Grpc.Core.StatusCode.Unavailable })
            {
                var flag = new StackTrace(e.Exception).GetFrames().Any(x =>
                {
                    if (x.GetMethod() is { DeclaringType.DeclaringType.Namespace: string @namespace } &&
                        @namespace.Contains("OpenFrp.Launcher.ViewModels"))
                    {
                        return true;
                    }
                    return false;
                });

                e.RequestCatch = flag;
                return;
            }
            if (e.Exception is System.Configuration.ConfigurationErrorsException)
            {
                e.RequestCatch = true;
            }
        }

        private static bool Settings_TryReadConfiguration()
        {
            // 防止错误的配置被读取
            // https://github.com/petergolde/PurplePen/blob/faa285b1a2a60aa8b1d4bf5d99bd8bf23e07cfe0/src/PurplePen/Program.cs#L62
            try
            {
                _ = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            }
            catch (ConfigurationErrorsException ex)
            {
                if (!DeleteFileAndResetConfig(ex.Filename))
                {
                    return false;
                }
            }
            catch (ConfigurationException exception)
            {
                if (exception.InnerException is ConfigurationErrorsException ce2)
                {
                    if (!DeleteFileAndResetConfig(ce2.Filename))
                    {
                        return false;
                    }
                }
            }

            return true;

            static bool DeleteFileAndResetConfig(string fileName)
            {
                try
                {
                    File.Delete(fileName);

                    App.Settings.Reload();

                    App.Settings.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "配置文件无法读取，且已尝试删除文件，但删除失败:" +
                        $"\n文件路径: {fileName}" +
                        $"\n提示内容: {ex.Message}", "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Error);

                    return false;
                }
                return true;
            }
        }

        internal static HashSet<string> StartupArguments { get; private set; } =
            /* Array.Empty<string>() "--minimize" */ 
            new HashSet<string> { };

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!Settings_TryReadConfiguration())
            {
                Environment.Exit(-1);

                App.Current.Shutdown(-1);

                return;
            }

            
            string appHash = Service.Helpers.HashAlgorithmHelper.ComputeHashString(AppContext.BaseDirectory);

            launcherMutex = new Mutex(false, $"openfrp.launcher.{appHash}", out var createdNewFlag);

            if (!Debugger.IsAttached)
            {
                StartupArguments = new HashSet<string>(e.Args);
            }


            if (!createdNewFlag && !launcherMutex.SafeWaitHandle.IsClosed)
            {
                launcherMutex.Close();

#if NET
                var currentId = Environment.ProcessId;
#else
                var currentId = Process.GetCurrentProcess().Id;
#endif

                foreach (var proc in Process.GetProcessesByName("OpenFrp.Launcher"))
                {
                    try
                    {
                        if (proc.Id == currentId) continue;

                        try
                        {
                            if (proc.MainModule is not { } || proc.MainModule.FileName != LauncherMainModulePath)
                            {
                                continue;
                            }
                        }
                        catch (Win32Exception)
                        {

                        }
                        if (proc.MainWindowHandle != IntPtr.Zero)
                        {
                            byte[] callFromLauncherPath = Encoding.UTF8.GetBytes(LauncherMainModulePath);
                            byte[] oappPath = Array.Empty<byte>();

                            if (StartupArguments.Count > 0 && StartupArguments.Where(x => x.StartsWith("openfrp://")) is { } c && c.Count() is 1)
                            {
                                oappPath = Encoding.UTF8.GetBytes(StartupArguments.First());
                            }

                            SendWindowCopyDataStruct(proc.MainWindowHandle,0x00);

                            SendWindowCopyDataStruct(proc.MainWindowHandle, 0x01, callFromLauncherPath);

                            if (oappPath is { Length: > 0 })
                            {
                                SendWindowCopyDataStruct(proc.MainWindowHandle, 0x02, oappPath);
                            }


                            static int SendWindowCopyDataStruct(IntPtr hWnd, int id, byte[]? buffer = null,bool waitForFinish = true)
                            {
                                var cd = new User32.COPYDATASTRUCT
                                {
                                    dwData = (IntPtr)id,
                                    cbData = (buffer != null) ? buffer.Length : 0
                                };
                                if (buffer != null)
                                {
                                    IntPtr pt2 = Marshal.AllocHGlobal(buffer.Length);

                                    try
                                    {
                                        cd.lpData = pt2;

                                        Marshal.Copy(buffer, 0, pt2, buffer.Length);
                                    }
                                    finally
                                    {
                                        Marshal.FreeHGlobal(pt2);
                                    }
                                }
                                try
                                {
                                    if (waitForFinish)
                                    {
                                        return User32.SendMessage(hWnd, 0x4A, IntPtr.Zero, cd);
                                    }
                                    else
                                    {
                                        _ = User32.SendMessage(hWnd, 0x4A, IntPtr.Zero, cd);
                                    }
                                }
                                catch
                                {
                                    return Marshal.GetHRForLastWin32Error();
                                }
                                return 0;
                            }

                            Environment.Exit(0);
                            return;
                        }
                    }
                    catch
                    {

                    }
                }

                Service.Helpers.MessageBoxHelper.MessageBox(IntPtr.Zero, "已有相同实例已开启。\n请在系统托盘中找到 OpenFrp 标志，单击或在菜单中点击显示窗口。", "OpenFrp Launcher Preview", (uint)(Service.Helpers.MessageBoxHelper.MessageMode.Confirm | Service.Helpers.MessageBoxHelper.MessageMode.Warning) );

                Environment.Exit(0);

                return;
            }

            Service.Net.HttpClient.DefualtInstance.SetUseProxy(App.Settings.UseProxy);

            App.Settings.SettingChanging += (_, e) =>
            {
                if (e.SettingClass is "OpenFrp.Launcher.Properties.Settings")
                {
                    switch (e.SettingName)
                    {
                        case "UseProxy" when e.NewValue is bool nv1:
                            {
                                Service.Net.HttpClient.DefualtInstance.SetUseProxy(nv1);
                            }
                            ; break;
                        default: return;
                    }
                }
            };

            ConfigureNotification();

            CreateNotifyIcon(appHash);

            base.OnStartup(e);
        }

        private Mutex? launcherMutex;

        internal static H.NotifyIcon.TaskbarIcon? TaskBarIcon;

        internal static Properties.Settings Settings { get => OpenFrp.Launcher.Properties.Settings.Default; }

        internal static Rpc.DaemonManager DaemonManager { get; set; } = new DaemonManager { };

        internal static Rpc.RpcManager? RpcManager { get; set; }

        internal static Model.FrpcFeatrue FrpcFeature { get; } = new Model.FrpcFeatrue();

        public static iNKORE.UI.WPF.Modern.ElementTheme ApplicationTheme { get => Settings.ApplicationTheme; }

        public static string FrpcVersionString { get; set; } = "Unknown";

        public static string LauncherVersionString => "5.8.2 Preview";

        public static string UiLauncherVersionString => $"OpenFrp 启动器 - v{LauncherVersionString}";



        internal static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        internal static void ConfigureNotification()
        {
            if (OSVersionHelper.IsWindows10OrGreater)
            {
            
                try
                {
                    Type? tp = typeof(Windows.UI.Notifications.ToastNotification);
                    if (tp is not null)
                    {
                        var putMethod = tp.GetMethod("put_ExpiresOnReboot", new Type[1] { typeof(bool) });
                        if (putMethod is not null)
                        {
                            Notification_UseExpiredReboot = true;
                        }
                    }
                }
                catch
                {
                  
                }
              
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
                                    Clipboard.SetText(e.Argument.Trim().Split(' ').Last());
                                }
                            }
                            catch { }
                        });

                        thread.TrySetApartmentState(ApartmentState.STA);
                        thread.Start();
                    };
                }
                catch { }
            }
            else if (App.Settings.NotificationMode is Model.NotificationMode.ToastNotification)
            {
                App.Settings.NotificationMode = Model.NotificationMode.TaskbarNotify;
            }
        }

        internal static void CreateNotifyIcon(string appHash)
        {
            if (!Uri.TryCreate("pack://application:,,,/Resources/Images/desktop.ico", UriKind.RelativeOrAbsolute, out var result)) return;

            TaskBarIcon = new H.NotifyIcon.TaskbarIcon()
            {
                IconSource = new BitmapImage(result),
                CustomName = $"OpenFrp Launcher (App {appHash})",
                ToolTipText = UiLauncherVersionString,
                PopupPlacement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint,
                TrayPopup = new Controls.AppPopup { },
                PopupActivation = H.NotifyIcon.Core.PopupActivationMode.RightClick,
                NoLeftClickDelay = true

            };
            if (TaskBarIcon.TrayPopupResolved is not null)
            {
                TaskBarIcon.LeftClickCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(delegate
                {
                    switch (App.Current.MainWindow)
                    {
                        case LoginWindow lw:
                            {
                                lw.ShowByHANDLE();
                            }
                            ; break;
                        case MainWindow mw:
                            {
                                mw.ShowByHANDLE();
                            }
                            ; break;
                    }
                });
                TaskBarIcon.TrayPopupResolved.PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade;
                TaskBarIcon.TrayPopupResolved.SetBinding(iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty, new Binding
                {
                    Source = App.Settings,
                    Path = new PropertyPath(nameof(App.Settings.ApplicationTheme)),
                    Mode = BindingMode.OneWay
                });
            }
            if (!TaskBarIcon.IsCreated)
            {
                TaskBarIcon.ForceCreate(false);
            }
        }
       



    }

    public static class Extend
    {
        internal static bool UpdateState(this ViewModels.IHrViewModel hr,Yue3.Model.Result.HttpResponse response, Func<bool>? predicate = default)
        {
            var tee = hr.GetType();
            
            var property = tee.GetProperty("ExecuteResult") ?? throw new NotSupportedException();

            if (response.StatusCode is not System.Net.HttpStatusCode.OK || response.Exception is not null)
            {
                if (response is { Message: null or "" })
                {
                    response.Message = "发生了错误";
                }
                property.SetValue(hr, new Model.ExecuteResult(response));
                
                return false;
            }
            if (predicate is not null)
            {
                if (predicate.Invoke())
                {
                    return true;
                }
                if (response is { Message: null or "" })
                {
                    response.Message = "发生了错误";
                }
                property.SetValue(hr, new Model.ExecuteResult(response));
                return false;
            }
            return true;
        }

        internal static bool UpdateState<TData>(this ViewModels.IHrViewModel hr,Yue3.Model.Result.HttpResponse<TData> response, Func<bool>? predicate = default)
        {
            var tee = hr.GetType();

            var property = tee.GetProperty("ExecuteResult") ?? throw new NotSupportedException();

            if (response.Data is null)
            {
                if (response is { Message: null or "" })
                {
                    response.Message = response.Exception?.InnerException?.Message ?? "发生了错误";
                }
                property.SetValue(hr, new Model.ExecuteResult(response));
                
                return false;
            }

            return hr.UpdateState((Yue3.Model.Result.HttpResponse)response, predicate);
        }

        internal static bool UpdateState(this ViewModels.IHrViewModel hr,OpenFrp.Service.Proto.RpcResponse? response, Func<bool>? predicate = default)
        {
            var tee = hr.GetType();

            var property = tee.GetProperty("ExecuteResult") ?? throw new NotSupportedException();

            if (response is null)
            {
                var self = new System.Diagnostics.StackTrace(true);

                property.SetValue(hr, new Model.ExecuteResult
                {
                    Exception = new NullReferenceException(self.ToString()),
                    StatusCode = -1
                });
                
                if (response is { Message: null or "" })
                {
                    response.Message = "发生了错误";
                }
                return false;
            }
            if (response.Flag)
            {
                return true;
            }
            if (response.StatusCode is not Grpc.Core.StatusCode.OK || response.Exception is not null)
            {
                if (response is { Message: null or "" })
                {
                    response.Message = "发生了错误";
                }
                property.SetValue(hr, new Model.ExecuteResult(response));
                
                return false;
            }
            if (predicate is not null)
            {
                if (predicate.Invoke())
                {
                    return true;
                }
                if (response is { Message: null or "" })
                {
                    response.Message = "发生了错误";
                }
                property.SetValue(hr, new Model.ExecuteResult(response));
                
          
                return false;
            }
            return true;
        }

        internal static bool UpdateState<TData>(this ViewModels.IHrViewModel hr,OpenFrp.Service.Proto.RpcResponse<TData>? response, Func<bool>? predicate = default)
        {
            var tee = hr.GetType();

            var property = tee.GetProperty("ExecuteResult") ?? throw new NotSupportedException();

            if (response is null)
            {
                var self = new System.Diagnostics.StackTrace(true);

                property.SetValue(hr, new Model.ExecuteResult
                {
                    Exception = new NullReferenceException(self.ToString()),
                    StatusCode = -1
                });
                
                if (response is { Message: null or "" })
                {
                    response.Message = "发生了错误";
                }

                return false;
            }
            if (response.Data is null)
            {
                if (response is { Message: null or "" })
                {
                    response.Message = "发生了错误";
                }
                property.SetValue(hr, new Model.ExecuteResult(response));
                
             
                return false;
            }

            return hr.UpdateState((OpenFrp.Service.Proto.RpcResponse)response, predicate);
        }

        internal static void ClearExecuteResult(this ViewModels.IHrViewModel hr)
        {
            var tee = hr.GetType();

            var property = tee.GetProperty("ExecuteResult") ?? throw new NotSupportedException();

            property.SetValue(hr, null);
        }

        public static OpenFrp.Service.Helpers.MessageBoxHelper.MessageResult SendMessage(Window wpfWnd, string title, string message, OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode mode)
        {
            System.IntPtr handleWnd = new WindowInteropHelper(wpfWnd).EnsureHandle();

            return (OpenFrp.Service.Helpers.MessageBoxHelper.MessageResult)OpenFrp.Service.Helpers.MessageBoxHelper.MessageBox(handleWnd, message, title, (uint)mode);
        }


        public static void CopyFromImage(this System.Windows.Media.Imaging.WriteableBitmap wb, System.Drawing.Image bitmap)
            => CopyFromBitmap(wb,(Bitmap)bitmap);

        // https://blog.walterlv.com/post/convert-bitmap-to-imagesource-using-unsafe-method.html
        public static void CopyFromBitmap(this System.Windows.Media.Imaging.WriteableBitmap wb, Bitmap bitmap)
        {
            if (wb == null) throw new ArgumentNullException(nameof(wb));
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            var ws = wb.PixelWidth;
            var hs = wb.PixelHeight;

            var wt = bitmap.Width;
            var ht = bitmap.Height;
            if (ws != wt || hs != ht) throw new ArgumentException("暂时只支持相同尺寸图片的复制。");

            var width = ws;
            var height = hs;
            var bytes = ws * hs * wb.Format.BitsPerPixel / 8;

            var rBitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);

            wb.Lock();

            unsafe
            {
                Buffer.MemoryCopy(rBitmapData.Scan0.ToPointer(), wb.BackBuffer.ToPointer(), bytes, bytes);
            }
            wb.AddDirtyRect(new Int32Rect(0, 0, width, height));
            wb.Unlock();

            bitmap.UnlockBits(rBitmapData);
        }
        
    }
}
