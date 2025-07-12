using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Rpc
{
    class DaemonManager
    {
        internal Process? ServiceProcess { get; set; }

        private Task? prevListenDaemonTask;

        private EventWaitHandle? _daemon_ProcessEventWaitHandle;

        internal async Task WaitForProcessLaunch(CancellationToken cancellationToken = default)
        {
            if (_daemon_ProcessEventWaitHandle is not null)
            {
                await Task.Run(_daemon_ProcessEventWaitHandle.WaitOne, cancellationToken);

                ResetDaemonWaitHandle();
            }
        }

        internal void ResetDaemonWaitHandle()
        {
            if (_daemon_ProcessEventWaitHandle is null) return;


            _daemon_ProcessEventWaitHandle.Close();

            _daemon_ProcessEventWaitHandle = null;
        }

        internal void LaunchRpcProcess(out Rpc.RpcManager manager)
        {
            LaunchRpcProcess();

            if (App.RpcManager is null)
            {
                throw new NullReferenceException();
            }
            manager = App.RpcManager;
        }

        internal void LaunchRpcProcess()
        {
            App.RpcManager ??= new RpcManager();

            if (ServiceProcess is { HasExited: false })
            {
                return;
            }
            ServiceProcess = default;

            var mutex = new Mutex(true, $"service.{App.RpcManager.PipeName}", out var createdNewFlag);


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

            var fileName = typeof(OpenFrp.Service.Daemon.Daemon).Assembly.Location;
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

        private void DaemonProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is string { Length: > 0 } msg)
            {
                Debug.WriteLine($"[OF Daemon] {msg}");
                switch (msg)
                {
                    case "dbug: OpenFrp.Service.Daemon.Daemon[0] service launched!":
                        {
                            _daemon_ProcessEventWaitHandle?.Set();
                        }
                        ; break;
                    default:
                        {
                            if (msg.StartsWith("fail"))
                            {
                                if (ServiceProcess is not { } || !ServiceProcess.WaitForExit(1000)) return;

                                ServiceProcess.Exited -= DaemonProcessExited;

                                DaemonProcessExited(process: ServiceProcess, msg);
                            }
                        }
                        ; break;
                }
            }
        }

        // 当 Daemon 并非子进程时，监听其退出。
        private void ListenDaemonProcessExit(ref Mutex mutex)
        {
            try
            {
                var processes = Process.GetProcessesByName("OpenFrp.Service");

                string asm = typeof(OpenFrp.Service.Daemon.Daemon).Assembly.Location;

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

        private void DaemonProcessExited(object? sender, EventArgs? e)
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

        private void DaemonProcessExited(Process process, string stdErrData)
        {
            try { ResetDaemonWaitHandle(); } catch { }

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
                App.Current.Dispatcher.Invoke(() =>
                {
                    App.Current.MainWindow.WindowState = WindowState.Normal;
                    App.Current.MainWindow.Activate();
                });
            }
            catch { }

            var resp = App.Current.Dispatcher.Invoke(() => Extend.SendMessage(App.Current.MainWindow, "OpenFrp Launcher", msg, OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode.Error | OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode.RetryCancel));
            if (resp is OpenFrp.Service.Helpers.MessageBoxHelper.MessageResult.Cancel)
            {
                App.Current.Dispatcher.Invoke(App.Current.Shutdown);
            }
            else if (resp is OpenFrp.Service.Helpers.MessageBoxHelper.MessageResult.Retry)
            {
                App.Current.Dispatcher.Invoke(() =>
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
}
