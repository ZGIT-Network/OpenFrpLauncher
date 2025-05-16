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
                    return;
                }
            }
        }

        internal static bool Notification_UseExpiredReboot = false;

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

        protected override void OnStartup(StartupEventArgs e)
        {
            // false is failed to delete config / read config
            if (!Settings_TryReadConfiguration())
            {
                Environment.Exit(-1);

                App.Current.Shutdown(-1);

                return;
            }

            if (e.Args.Contains("--no-effect"))
            {

            }



            string launcherFilePath = Path.Combine(AppContext.BaseDirectory, "OpenFrp.Launcher.exe");
            string appHash = Service.Helpers.HashAlgorithmHelper.ComputeHashString(AppContext.BaseDirectory);

            launcherMutex = new Mutex(false, $"openfrp.launcher.{appHash}", out var createdNewFlag);

#if DEBUG
            //MessageBox.Show("" +
            //    $"AppContext.BaseDirectory: {AppContext.BaseDirectory}" + 
            //    $"\nopenfrp.launcher.{appHash}" +
            //    $"\ncreatedNewFlag : {createdNewFlag}" +
            //    $"\nlauncherMutex.SafeWaitHandle.IsClosed: {launcherMutex.SafeWaitHandle.IsClosed}");
#endif
            if (!createdNewFlag && !launcherMutex.SafeWaitHandle.IsClosed)
            {
                launcherMutex.Close();

                var currentId = Process.GetCurrentProcess().Id;

                foreach (var proc in Process.GetProcessesByName("OpenFrp.Launcher"))
                {
                    try
                    {
                        if (proc.Id == currentId) continue;
                        if (proc.MainModule is { } && proc.MainModule.FileName == launcherFilePath)
                        {
                            if (proc.MainWindowHandle != IntPtr.Zero)
                            {
                                Win32.User32.ShowWindow(proc.MainWindowHandle, Win32.User32.SW_TYPE.SW_RESTORE);
                                Win32.User32.SetForegroundWindow(proc.MainWindowHandle);
                                break;
                            }
                        }
                    }
                    catch
                    {

                    }
                }

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
            #region Prepare for window
            if (OSVersionHelper.IsWindows10OrGreater)
            {
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

            if (!Uri.TryCreate("pack://application:,,,/Resources/Images/desktop.ico",UriKind.RelativeOrAbsolute,out var result)) return;

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
                                lw.ShowByHwndCC();
                            };break;
                        case MainWindow mw:
                            {
                                mw.ShowByHwndCC();  
                            };break;
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
            if (e.Args.Contains("--minimize"))
            {

            }
            #endregion
            base.OnStartup(e);
        }

        private Mutex? launcherMutex;

        internal static H.NotifyIcon.TaskbarIcon? TaskBarIcon;

        internal static Properties.Settings Settings { get => OpenFrp.Launcher.Properties.Settings.Default; }

        internal static Process? ServiceProcess { get; set; }

        internal static Rpc.RpcManager? RpcManager { get; set; }

        internal static Model.FrpcFeatrue FrpcFeature { get; } = new Model.FrpcFeatrue();

        public static string FrpcVersionString { get; set; } = "Unknown";

        public static string LauncherVersionString => "5.7.6 Preview";

        public static string UiLauncherVersionString => $"OpenFrp 启动器 - v{LauncherVersionString}";

        private static Task? prevListenDaemonTask;

        private static EventWaitHandle? _daemon_ProcessEventWaitHandle;

        internal static async Task WaitForProcessLaunch(CancellationToken cancellationToken = default)
        {
            if (_daemon_ProcessEventWaitHandle is not null)
            {
                await Task.Run(_daemon_ProcessEventWaitHandle.WaitOne, cancellationToken);

                ResetDaemonWaitHandle();
            }
        }

        internal static void ResetDaemonWaitHandle()
        {
            if (_daemon_ProcessEventWaitHandle is null) return;

            _daemon_ProcessEventWaitHandle.Close();

            _daemon_ProcessEventWaitHandle = null;
        }

        internal static void LaunchRpcProcess(out Rpc.RpcManager manager)
        {
            LaunchRpcProcess();

            if (RpcManager is null)
            {
                throw new NullReferenceException();
            }
            manager = RpcManager;
        }

        internal static void LaunchRpcProcess()
        {
            RpcManager ??= new RpcManager();

            if (ServiceProcess is { HasExited: false })
            {
                return;
            }
            ServiceProcess = default;

            var mutex = new Mutex(true, $"service.{RpcManager.PipeName}", out var createdNewFlag);


            if (!createdNewFlag && !mutex.SafeWaitHandle.IsClosed)
            {
                // 已经创建了一个相同命名的进程，在此监听，等到其结束。
                prevListenDaemonTask ??= Task.Run(() => ListenDaemonProcessExit(ref mutex));

                return;
            }
            else
            {
                mutex.Close();
            }

            var fileName = typeof(Daemon).Assembly.Location;
            try
            {
                var pro = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenFrp.Service.exe"),
                        Arguments = "--daemon",
                        CreateNoWindow = true,
                        ErrorDialog = false,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        StandardErrorEncoding = System.Text.Encoding.Default,
                        StandardOutputEncoding = System.Text.Encoding.Default,
                        //WindowStyle = ProcessWindowStyle.Hidden,
                    },
                    EnableRaisingEvents = true
                };
                pro.OutputDataReceived += DaemonProcessOutputDataReceived;
                pro.ErrorDataReceived += DaemonProcessOutputDataReceived;
                pro.Exited += DaemonProcessExited;

                if ((!pro.Start() && pro.HasExited) || pro.HasExited)
                {
                    var code = pro.ExitCode;
                }
                else
                {
                    pro.BeginOutputReadLine();
                    pro.BeginErrorReadLine();

                    _daemon_ProcessEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                    ServiceProcess = pro;
                }
            }
            catch (FileNotFoundException)
            {

            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        private static void DaemonProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is string { Length: > 0 } msg)
            {
                Debug.WriteLine($"[OF Daemon] {msg}");
                switch (msg)
                {
                    case "dbug: OpenFrp.Service.Daemon.Daemon[0] service launched!":
                        {
                            _daemon_ProcessEventWaitHandle?.Set();
                        }; break;
                    default:
                        {
                            if (msg.StartsWith("fail"))
                            {
                                if (App.ServiceProcess is not {  } || !App.ServiceProcess.WaitForExit(1000)) return;

                                App.ServiceProcess.Exited -= DaemonProcessExited;

                                DaemonProcessExited(process: App.ServiceProcess, msg);
                            }
                        }; break;
                }
            }
        }

        // 当 Daemon 并非子进程时，监听其退出。
        private static void ListenDaemonProcessExit(ref Mutex mutex)
        {
            try
            {
                var processes = Process.GetProcessesByName("OpenFrp.Service");

                string asm = typeof(Daemon).Assembly.Location;

                asm = asm.Remove(asm.Length - 4);

                if (processes.Length > 0)
                {
                    foreach (var proc in processes)
                    {
                        if (proc.MainModule is { FileName: string fv } && asm == fv.Remove(fv.Length - 4))
                        {
                            ServiceProcess = proc;
                            break;
                        }
                    }

                    mutex.Close();

                    ServiceProcess?.WaitForExit();

                    // TODO: notice here //
                    
                    prevListenDaemonTask = null;

                    DaemonProcessExited(ServiceProcess, EventArgs.Empty);
                    return;
                }

            }
            catch
            {

            }
            try
            {
                var flag = mutex.WaitOne(Timeout.Infinite);
                if (flag)
                {
                    mutex.Dispose();
                }
            }
            catch (AbandonedMutexException)
            {
                mutex.Close();
                // nothing happend;
            }
            catch (ObjectDisposedException)
            {
                // nothing happend;
            }
            catch (Exception ex)
            {
                _ = ex;

                return;
            }
            prevListenDaemonTask = null;

            DaemonProcessExited(mutex, default);
        }

        private static void DaemonProcessExited(object? sender, EventArgs? e)
        {
            if (App.TaskBarIcon is { IsDisposed: true }) return;
            if (sender is Process process)
            {
                if (e != EventArgs.Empty)
                {
                    string err = process.StandardError.ReadToEnd().Trim();
                    if (err.Length > 0)
                    {
                        DaemonProcessExited(process, err);
                    }
                }
                else
                {
                    DaemonProcessExited(process, string.Empty);
                }
            }
            else if (sender is Mutex)
            {
                Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processExit");

                Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
            }
        }

        private static void DaemonProcessExited(Process process,string stdErrData)
        {
            int exitCode = -1;
            Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processExit");
            Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processExit");

            try
            {
                exitCode = process.ExitCode;
            }
            catch
            {

            }

            if (exitCode is 0 or 768 && !stdErrData.StartsWith("fail"))
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
                    Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");
                });

                return;
            }
            string msg =
                "(聚焦该窗口，按下Ctrl+C 复制内容) Deamon 异常退出" +
                $"\nExitCode: {exitCode}";

            if (!string.IsNullOrWhiteSpace(stdErrData) && stdErrData.Length > 0)
            {
                msg += $"\n\n错误内容:\n{stdErrData}\n";
            }
            msg += "\n\"重试\" - 将尝试重新启动守护进程；\n\"取消\" - 退出启动器。";

            try
            {
                Current.Dispatcher.Invoke(() =>
                {
                    Current.MainWindow.WindowState = WindowState.Normal;
                    Current.MainWindow.Activate();
                });
            }
            catch { }

            var resp = Current.Dispatcher.Invoke(() => Extend.SendMessage(Current.MainWindow, "OpenFrp Launcher", msg, OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode.Error | OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode.RetryCancel));
            if (resp is OpenFrp.Service.Helpers.MessageBoxHelper.MessageResult.Cancel)
            {
                Current.Dispatcher.Invoke(Current.Shutdown);
            }
            else if (resp is OpenFrp.Service.Helpers.MessageBoxHelper.MessageResult.Retry)
            {
                Current.Dispatcher.Invoke(() =>
                {
                    if (App.Current.MainWindow is MainWindow)
                    {
                        Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
                    }
                    else
                    {
                        Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");
                    }
                });
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
                    response.Message = "发生了错误";
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
