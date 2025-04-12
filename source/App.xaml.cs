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

namespace OpenFrp.Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();//显示控制台

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole(); //释放控制台、关闭控制台

        public App()
        {
            //AllocConsole();

            
            //RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
            AppContext.SetSwitch("Switch.System.Windows.Media.EnableHardwareAccelerationInRdp", true);

            Dispatcher.UnhandledException += Dispatcher_UnhandledException;


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
                            }; break;
                        default: return;
                    }
                }
            };
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

        }


        

        internal static Properties.Settings Settings { get => OpenFrp.Launcher.Properties.Settings.Default; }

        internal static Process? ServiceProcess { get; set; }

        internal static Rpc.RpcManager? RpcManager { get; set; }

        private static Task? prevListenDaemonTask;

        private static EventWaitHandle? _daemon_ProcessEventWaitHandle;

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is TaskCanceledException or Grpc.Core.RpcException { StatusCode: Grpc.Core.StatusCode.Cancelled })
            {
                var flag = new StackTrace(e.Exception).GetFrames().Any(x => x.GetMethod() is { DeclaringType.DeclaringType.Namespace: string @namespace } && @namespace.Contains("OpenFrp.Launcher.ViewModels"));

                e.Handled = flag;
            }
        }

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
                        CreateNoWindow = !true,
                        ErrorDialog = false,
                        UseShellExecute = false,
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
                Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
                Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");


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
                if (response is { Message: null })
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
                if (response is { Message: null })
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
                if (response is { Message: null })
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
                
                if (response is { Message: null })
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
                if (response is { Message: null })
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
                if (response is { Message: null })
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
                
                if (response is { Message: null })
                {
                    response.Message = "发生了错误";
                }

                return false;
            }
            if (response.Data is null)
            {
                if (response is { Message: null })
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
