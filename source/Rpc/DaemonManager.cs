using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using iNKORE.UI.WPF.Helpers;
using Microsoft.Extensions.Logging;
using OpenFrp.Service;
using OpenFrp.Service.Helpers;

namespace OpenFrp.Launcher.Rpc
{
    //class DaemonManager
    //{
    //    internal DaemonManager()
    //    {
    //        //CancellationTokenSource = new CancellationTokenSource { };

    //        //GetOpenFrpService();
    //    }

    //    internal Process? ServiceProcess { get; set; }

    //    //private CancellationTokenSource CancellationTokenSource { get; set; }

    //    //internal CancellationToken CancellationToken
    //    //{
    //    //    get
    //    //    {
    //    //        if (CancellationTokenSource is not null)
    //    //        {
    //    //            return CancellationTokenSource.Token;
    //    //        }
    //    //        return CancellationToken.None;
    //    //    }
    //    //}

    //    //internal void Cancal()
    //    //{
    //    //    if (CancellationTokenSource is not null)
    //    //    {
    //    //        CancellationTokenSource.Cancel();
    //    //        CancellationTokenSource.Dispose();
    //    //    }
    //    //}

    //    private TaskCompletionSource<string>? onlineInstanceWaiter { get; set; }

    //    private Task? prevListenDaemonTask;

    //    private EventWaitHandle? _daemon_ProcessEventWaitHandle;

    //    internal async Task<bool> WaitForProcessLaunch(CancellationToken cancellationToken = default)
    //    {
    //        if (_daemon_ProcessEventWaitHandle is not null)
    //        {
    //            await Task.Run(_daemon_ProcessEventWaitHandle.WaitOne).WhenAnyTime(cancellationToken);

    //            ResetDaemonWaitHandle();

                
    //        }
    //        else if (serviceController is not null)
    //        {
    //            serviceController.Refresh();

    //            try
    //            {
    //                await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Running)).WhenAnyTime(cancellationToken).WithTimeout(7500);

    //                return serviceController.Status == ServiceControllerStatus.Running;
    //            }
    //            catch (InvalidOperationException)
    //            {
    //                RefreshServiceActivation();

    //                return await WaitForProcessLaunch(cancellationToken);
    //            }
    //        }
    //        return !cancellationToken.IsCancellationRequested;
    //    }

    //    internal void ResetDaemonWaitHandle()
    //    {
    //        if (_daemon_ProcessEventWaitHandle is null) return;


    //        _daemon_ProcessEventWaitHandle.Close();

    //        _daemon_ProcessEventWaitHandle = null;
    //    }

    //    internal Model.ExecuteResult LaunchRpcProcess(out Rpc.RpcManager manager)
    //    {
    //        var r = LaunchRpcProcess();

    //        if (App.RpcManager is null)
    //        {
    //            throw new NullReferenceException();
    //        }
    //        manager = App.RpcManager;

    //        return r;
    //    }

    //    internal Model.ExecuteResult LaunchRpcProcess()
    //    {
    //        //App.RpcManager ??= new RpcManager();

    //        if (ServiceProcess is { HasExited: false })
    //        {
    //            return new Model.ExecuteResult { };
    //        }
    //        ServiceProcess = default;

    //        RefreshServiceActivation();

    //        if (IsServiceDaemon)
    //        {
                
    //            try
    //            {
    //                serviceController!.Refresh();

    //                if (serviceController!.Status is ServiceControllerStatus.Running)
    //                {
    //                    return new Model.ExecuteResult { };
    //                }
    //                serviceController.Start();

    //                return new Model.ExecuteResult { };
    //            }
    //            catch(Exception ivo)
    //            {
    //                if (ivo.InnerException is System.ComponentModel.Win32Exception { NativeErrorCode: not 5 } ex)
    //                {
    //                    if (ex.NativeErrorCode is 1060 or 1072)
    //                    {
    //                        RefreshServiceActivation();
    //                    }
    //                    return new Model.ExecuteResult { Exception = ex, Message = ex.Message };
    //                }
    //            }


    //            try
    //            {
    //                Process.Start(new ProcessStartInfo()
    //                {
    //                    FileName = OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile(),
    //                    Arguments = $"--service launch",
    //                    CreateNoWindow = true,
    //                    ErrorDialog = false,
    //                    UseShellExecute = true,
    //                    ErrorDialogParentHandle = IntPtr.Zero,
    //                    Verb = "runas",
    //                    WindowStyle = ProcessWindowStyle.Hidden
    //                });

    //                return new Model.ExecuteResult { };
    //            }
    //            catch (Exception ex)
    //            {
    //                return new Model.ExecuteResult { Exception = ex,Message = "启动服务时发生了错误" };
    //            }
    //        }


    //        var mutex = new Mutex(true, $"service.{App.RpcManager.PipeName}", out var createdNewFlag);

    //        // ofRpcDaemon.ZrzkE5Ew57JqCQQV1X3BgnOMrVUUjdUPcc82uZ8agH8=
    //        // ofRpcDaemon.ZrzkE5Ew57JqCQQV1X3BgnOMrVUUjdUPcc82uZ8agH8=

    //        if (!createdNewFlag && !mutex.SafeWaitHandle.IsClosed)
    //        {
    //            // 已经创建了一个相同命名的进程，在此监听，等到其结束。
    //            prevListenDaemonTask ??= Task.Run(() => ListenDaemonProcessExit(ref mutex));

    //            return new Model.ExecuteResult { };
    //        }
    //        else
    //        {
    //            mutex.Close();
    //        }


            
    //        try
    //        {
    //            var pro = new Process()
    //            {
    //                StartInfo = new ProcessStartInfo
    //                {
    //                    FileName = Service.Helpers.FileHelper.GetServiceExecutableFile(),
    //                    Arguments = "--daemon",
    //                    CreateNoWindow = true,
    //                    ErrorDialog = false,
    //                    UseShellExecute = false,
    //                    RedirectStandardInput = true,
    //                    RedirectStandardError = true,
    //                    RedirectStandardOutput = true,
    //                    StandardErrorEncoding = System.Text.Encoding.Default,
    //                    StandardOutputEncoding = System.Text.Encoding.Default,
    //                    //WindowStyle = ProcessWindowStyle.Hidden,
    //                },
    //                EnableRaisingEvents = true
    //            };
    //            pro.OutputDataReceived += DaemonProcessOutputDataReceived;
    //            pro.ErrorDataReceived += DaemonProcessOutputDataReceived;
    //            pro.Exited += DaemonProcessExited;

    //            if ((!pro.Start() && pro.HasExited) || pro.HasExited)
    //            {
    //                return new Model.ExecuteResult { StatusCode = pro.ExitCode,Exception = new InvalidOperationException(pro.ToString()) };
    //            }
    //            else
    //            {
    //                pro.BeginOutputReadLine();
    //                pro.BeginErrorReadLine();

    //                _daemon_ProcessEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

    //                ServiceProcess = pro;
    //                return new Model.ExecuteResult { };
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            return new Model.ExecuteResult { Exception = ex,Message = "启动 Daemon 时发生了错误" };
    //        }
    //    }

    //    private void DaemonProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
    //    {
    //        if (e.Data is string { Length: > 0 } msg)
    //        {
    //            Debug.WriteLine($"[OF Daemon] {msg}");
    //            switch (msg)
    //            {
    //                case "dbug: OpenFrp.Service.Daemon.Daemon[0] service launched!":
    //                    {
    //                        _daemon_ProcessEventWaitHandle?.Set();
    //                    }
    //                    ; break;
    //                case "dbug: OpenFrp.Service.Daemon.Daemon[0] Is Service Mode, Return.":
    //                    {
    //                        this.RefreshServiceActivation();
    //                    };
    //                    break;
    //                default:
    //                    {
    //                        if (msg.StartsWith("fail"))
    //                        {
    //                            if (ServiceProcess is not { } || !ServiceProcess.WaitForExit(1000)) return;

    //                            ServiceProcess.Exited -= DaemonProcessExited;

    //                            DaemonProcessExited(process: ServiceProcess, msg);
    //                        }
    //                        else if (msg.StartsWith("jsonValue!of+="))
    //                        {
    //                            onlineInstanceWaiter?.TrySetResult(msg.Substring("jsonValue!of+=".Length));
    //                        }
    //                    }
    //                    ; break;
    //            }
    //        }
    //    }

    //    public async Task<bool> InputToExitProc()
    //    {

    //        if (App.DaemonManager.ServiceProcess is not null)
    //        {
    //            App.DaemonManager.ServiceProcess.EnableRaisingEvents = false;

    //            onlineInstanceWaiter = new TaskCompletionSource<string> { };

    //            try
    //            {
    //                await App.DaemonManager.ServiceProcess.StandardInput.WriteLineAsync("exitProc");



    //                var delay = Task.Run(() => App.DaemonManager.ServiceProcess.WaitForExit(3000));

    //                if (await Task.WhenAny(onlineInstanceWaiter.Task, delay) != delay && onlineInstanceWaiter.Task.Status is TaskStatus.RanToCompletion)
    //                {
    //                    App.Settings.AutoLaunchTunnel = onlineInstanceWaiter.Task.Result;
    //                }


    //                App.Settings.Save();

    //                //Cancal();

    //                Application.Current.Shutdown();

    //                return true;
    //            }
    //            catch (InvalidOperationException)
    //            {

    //            }
    //            catch (System.IO.IOException)
    //            {

    //            }
    //        }
    //        return false;
    //    }

    //    // 当 Daemon 并非子进程时，监听其退出。
    //    private void ListenDaemonProcessExit(ref Mutex mutex)
    //    {
    //        try
    //        {
    //            var processes = Process.GetProcessesByName("OpenFrp.Service");

    //            string asm = typeof(OpenFrp.Service.Daemon.Daemon).Assembly.Location;

    //            asm = asm.Remove(asm.Length - 4);

    //            if (processes.Length > 0)
    //            {
    //                foreach (var proc in processes)
    //                {
    //                    if (proc.MainModule is { FileName: string fv } && asm == fv.Remove(fv.Length - 4))
    //                    {
    //                        ServiceProcess = proc;
    //                        break;
    //                    }
    //                }

    //                mutex.Close();

    //                ServiceProcess?.WaitForExit();

    //                // TODO: notice here //

    //                prevListenDaemonTask = null;

    //                DaemonProcessExited(ServiceProcess, EventArgs.Empty);
    //                return;
    //            }

    //        }
    //        catch
    //        {

    //        }
    //        try
    //        {
    //            var flag = mutex.WaitOne(Timeout.Infinite);
    //            if (flag)
    //            {
    //                mutex.Dispose();
    //            }
    //        }
    //        catch (AbandonedMutexException)
    //        {
    //            mutex.Close();
    //            // nothing happend;
    //        }
    //        catch (ObjectDisposedException)
    //        {
    //            // nothing happend;
    //        }
    //        catch (Exception ex)
    //        {
    //            _ = ex;

    //            return;
    //        }
    //        prevListenDaemonTask = null;

    //        DaemonProcessExited(mutex, default);
    //    }

    //    private void DaemonProcessExited(object? sender, EventArgs? e)
    //    {
    //        if (App.TaskBarIcon is { IsDisposed: true }) return;
    //        if (sender is Process process)
    //        {
    //            if (e != EventArgs.Empty)
    //            {
    //                string err = process.StandardError.ReadToEnd().Trim();
    //                if (err.Length > 0)
    //                {
    //                    DaemonProcessExited(process, err);
    //                }
    //            }
    //            else
    //            {
    //                DaemonProcessExited(process, string.Empty);
    //            }
    //        }
    //        else if (sender is Mutex)
    //        {
    //            Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processExit");

    //            Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
    //        }
    //    }

    //    private void DaemonProcessExited(Process process, string stdErrData)
    //    {
    //        try { ResetDaemonWaitHandle(); } catch { }

    //        int exitCode = -1;
    //        Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processExit");
    //        Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processExit");

    //        try
    //        {
    //            exitCode = process.ExitCode;
    //        }
    //        catch
    //        {

    //        }

    //        if (exitCode is 0 or 768 && !stdErrData.StartsWith("fail"))
    //        {
    //            App.Current.Dispatcher.Invoke(() =>
    //            {
    //                Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
    //                Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");
    //            });

    //            return;
    //        }

    //        string msg =
    //            "(聚焦该窗口，按下Ctrl+C 复制内容) Deamon 异常退出" +
    //            $"\nExitCode: {exitCode}";

    //        if (!string.IsNullOrWhiteSpace(stdErrData) && stdErrData.Length > 0)
    //        {
    //            msg += $"\n\n错误内容:\n{stdErrData}\n";
    //        }
    //        msg += "\n\"重试\" - 将尝试重新启动守护进程；\n\"取消\" - 退出启动器。";

    //        try
    //        {
    //            App.Current.Dispatcher.Invoke(() =>
    //            {
    //                App.Current.MainWindow.WindowState = WindowState.Normal;
    //                App.Current.MainWindow.Activate();
    //            });
    //        }
    //        catch { }

    //        var resp = App.Current.Dispatcher.Invoke(() => Extend.SendMessage(App.Current.MainWindow, "OpenFrp Launcher", msg, OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode.Error | OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode.RetryCancel));
    //        if (resp is OpenFrp.Service.Helpers.MessageBoxHelper.MessageResult.Cancel)
    //        {
    //            App.Current.Dispatcher.Invoke(App.Current.Shutdown);
    //        }
    //        else if (resp is OpenFrp.Service.Helpers.MessageBoxHelper.MessageResult.Retry)
    //        {
    //            App.Current.Dispatcher.Invoke(() =>
    //            {
    //                if (App.Current.MainWindow is MainWindow)
    //                {
    //                    Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
    //                }
    //                else
    //                {
    //                    Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");
    //                }
    //            });
    //        }
    //    }

    //    public bool IsServiceDaemon { get => serviceController is not null; }

    //    private ServiceController? serviceController;

    //    public void RefreshServiceActivation()
    //    {
    //        GetOpenFrpService();
    //    }

    //    private void GetOpenFrpService()
    //    {
    //        string serviceName = OpenFrp.Service.WinSrv.ServiceWorker.GetServiceName();

    //        var services = ServiceController.GetServices();

    //        foreach (var serve in services)
    //        {
    //            if (!serve.ServiceName.Equals(serviceName))
    //            {
    //                continue;
    //            }
    //            serviceController = serve;
    //            return;
    //        }
    //        serviceController = null;
    //    }

    //    internal ServiceControllerStatus GetServiceState()
    //    {
    //        if (serviceController is not null)
    //        {
    //            serviceController.Refresh();

    //            try
    //            {
    //                return serviceController.Status;
    //            }
    //            catch
    //            {

    //            }
    //        }
    //        return ServiceControllerStatus.Paused;
    //    }

    //    internal async Task KillService()
    //    {
    //        if (serviceController is null) return;

    //        serviceController.Refresh();

    //        try
    //        {
    //            if (serviceController!.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
    //            {
    //                return;
    //            }

    //            serviceController.Stop();


    //            return;
    //        }
    //        catch (Exception ivo)
    //        {
    //            if (ivo.InnerException is System.ComponentModel.Win32Exception { NativeErrorCode: not 5 } ex)
    //            {
    //                if (ex.NativeErrorCode is 1060 or 1072)
    //                {
    //                    return;
    //                    //RefreshServiceActivation();
    //                }
    //            }
    //        }


    //        try
    //        {
    //            await Task.Run(() =>
    //            {
    //                Process.Start(new ProcessStartInfo()
    //                {
    //                    FileName = OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile(),
    //                    Arguments = $"--service stop",
    //                    CreateNoWindow = true,
    //                    ErrorDialog = false,
    //                    UseShellExecute = true,
    //                    ErrorDialogParentHandle = IntPtr.Zero,
    //                    Verb = "runas",
    //                    WindowStyle = ProcessWindowStyle.Hidden
    //                });
    //            });
    //        }
    //        catch { }

    //        return;
    //    }
    //}


    class DaemonManager
    {
        public DaemonManager(RpcManager rpcManager, ILogger<DaemonManager> logger)
        {
            this.logger = logger;
            this.rpcManager = rpcManager;

            DaemonService = GetWindowsService();
        }

        private readonly RpcManager rpcManager;
        private readonly ILogger<DaemonManager> logger;

        private Task? prevListenDaemonTask;
        private TaskCompletionSource<string>? onlineTunnelsWaiter;
        private Process? daemon_Process3rd;

        public ServiceController? DaemonService
        {
            get; private set;
        }

        public Process? DaemonProcess
        {
            get; private set;
        }

        public SemaphoreSlim? Semaphore_LaunchFinish { get; private set; }

        public static ServiceController? GetWindowsService()
        {
            var installedServices = ServiceController.GetServices();
            var serviceName = OpenFrp.Service.WinSrv.ServiceWorker.GetServiceName();

            foreach (var serviceItem in installedServices)
            {
                if (!serviceItem.ServiceName.Equals(serviceName, StringComparison.Ordinal))
                {
                    continue;
                }
                return serviceItem;
            }
            return default;
        }
        public async Task<Model.ExecuteResult> WaitForConfigureAsync(CancellationToken cancellationToken = default)
        {
            if (prevListenDaemonTask is not null)
            {
                return new Model.ExecuteResult { };
            }
            if (DaemonService is not null)
            {
                await RefreshServiceStateAsync();

                if (DaemonService is not null)
                {
                    try
                    {
                        await Task.Run(() => DaemonService.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10))).WhenAnyTime(cancellationToken);

                        return new Model.ExecuteResult { };
                    }
                    catch (System.ServiceProcess.TimeoutException ex)
                    {
                        logger.LogWarning(ex, "[WaitForConfigureAsync] 等待 OpenFrp 服务启动时超时。");
                        return ex;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                }
            }
            if (DaemonProcess is null & Semaphore_LaunchFinish is null)
            {
                return new Model.ExecuteResult { Exception = new InvalidOperationException("请先启动后再进行操作。") };
            }
            if (Semaphore_LaunchFinish is null)
            {
                return new Model.ExecuteResult { Exception = new InvalidOperationException("不允许多实例同时等待。") };
            }

            if (!rpcManager.IsConfigured)
            {
                try
                {
                    await Semaphore_LaunchFinish.WaitAsync(cancellationToken);
                }
                catch (System.Threading.Tasks.TaskCanceledException ex)
                {
                    return ex;
                }
                catch
                {

                }
            }

            return new Model.ExecuteResult { };
        }

        /// <summary>
        /// 刷新服务状态
        /// (注：当服务被[标记]删除或者不存在时，会将服务字样标为默认值。)
        /// </summary>
        public async Task RefreshServiceStateAsync()
        {
            if (DaemonService is null)
            {
                if (GetWindowsService() is { } sv) 
                {
                    DaemonService = sv;
                }
                else return;
            }

            await Task.Run(DaemonService.Refresh);

            try
            {
                _ = DaemonService.ServiceHandle;
            }
            catch (System.InvalidOperationException ex)
            {
                // 1072 = ERROR_SERVICE_MARKED_FOR_DELETE ; 1060 = ERROR_SERVICE_DOES_NOT_EXIST;
                if (ex.InnerException is System.ComponentModel.Win32Exception { NativeErrorCode: 1060 or 1072 })
                {
                    DaemonService = null;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[RefreshServiceState] 刷新 OpenFrp 服务状态时发生了错误。");
            }
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        private async Task<Model.ExecuteResult> LaunchServiceAsync()
        {
            if (DaemonService is null) return new Model.ExecuteResult { Exception = new NullReferenceException(nameof(DaemonService)) };

            try
            {
                if (DaemonService.Status is not ServiceControllerStatus.Running and not ServiceControllerStatus.StartPending)
                {
                    DaemonService.Start();
                }

                return new Model.ExecuteResult { };
            }
            catch (InvalidOperationException ex)
            {
                if (ex.InnerException is System.ComponentModel.Win32Exception p)
                {
                    switch (p.NativeErrorCode)
                    {
                        case 1084:
                            logger.LogError(p, "[LaunchService] 在安全模式下暂时无法启动服务。");
                            break;
                        case 1058:
                            logger.LogError(p, "[LaunchService] OpenFrp 服务已被禁用，请启用后再试。");
                            break;
                        case 1072 or 1060:
                            DaemonService = null;
                            return p;
                        case 5:
                            logger.LogDebug("[LaunchService] 调用辅助进程启动 Service");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[LaunchService] 启动 OpenFrp 服务时发生了错误。");
                return ex;
            }

            try
            {
                var proc = await OpenFrp.Service.Helpers.ProcessHelper.StartAsync(new ProcessStartInfo()
                {
                    FileName = OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile(),
                    Arguments = $"--service launch",
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    UseShellExecute = true,
                    ErrorDialogParentHandle = IntPtr.Zero,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                if (proc is not null)
                {
                    await proc.WaitForExitAsync();

                    if (proc.HasExited && proc.ExitCode is not 0)
                    {
                        throw new System.ComponentModel.Win32Exception(proc.ExitCode);
                    }
                    else if (!proc.HasExited)
                    {
                        throw new System.InvalidOperationException("OpenFrp 服务启动失败，进程未退出。");
                    }
                    else
                    {
                        return new Model.ExecuteResult { };
                    }
                }
                throw new NullReferenceException($"无法启动 OpenFrp 服务，进程未创建。({OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile()})");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // 5 - ERROR_ACCESS_DENIED
                switch (ex.NativeErrorCode)
                {
                    case 1084:
                        logger.LogError(ex, "[LaunchService] 在安全模式下暂时无法启动服务。");
                        break;
                    case 1058:
                        logger.LogError(ex, "[LaunchService] OpenFrp 服务已被禁用，请启用后再试。");
                        break;
                    case 1072 or 1060:
                        DaemonService = null;
                        return ex;
                    case 1223:
                        logger.LogDebug("[LaunchService] 用户取消了服务启动请求。");
                        return new Model.ExecuteResult { Exception = ex, Message = "用户取消了服务启动请求。" };
                    case 5:
                        logger.LogDebug("[LaunchService] 操作被系统拒绝");
                        return new Model.ExecuteResult { Exception = ex, Message = "操作被系统拒绝，请检查杀软是否拦截了操作。" };
                }
                return ex;
            }
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        /// <returns></returns>
        private async Task<Model.ExecuteResult> KillServiceAsync()
        {
            if (DaemonService is null) return new Model.ExecuteResult { Exception = new NullReferenceException(nameof(DaemonService)) };

            try
            {
                if (DaemonService.Status is not ServiceControllerStatus.Stopped and not ServiceControllerStatus.StopPending)
                {
                    DaemonService.Stop();
                }

                return new Model.ExecuteResult { };
            }
            catch (InvalidOperationException ex)
            {
                if (ex.InnerException is System.ComponentModel.Win32Exception p)
                {
                    switch (p.NativeErrorCode)
                    {
                        case 1072 or 1060:
                            DaemonService = null;
                            return new Model.ExecuteResult { };
                        case 5:
                            logger.LogDebug("[KillService] 调用辅助进程关闭 Service");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[KillService] 关闭 OpenFrp 服务时发生了错误。");
                return ex;
            }

            try
            {
                var proc = await OpenFrp.Service.Helpers.ProcessHelper.StartAsync(new ProcessStartInfo()
                {
                    FileName = OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile(),
                    Arguments = $"--service stop",
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    UseShellExecute = true,
                    ErrorDialogParentHandle = IntPtr.Zero,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                if (proc is not null)
                {
                    await proc.WaitForExitAsync();

                    if (proc.HasExited && proc.ExitCode is not 0)
                    {
                        throw new System.ComponentModel.Win32Exception(proc.ExitCode);
                    }
                    else if (!proc.HasExited)
                    {
                        throw new System.InvalidOperationException("OpenFrp 服务进程关闭失败，进程未退出。");
                    }
                    else
                    {
                        return new Model.ExecuteResult { };
                    }
                }
                throw new NullReferenceException($"无法关闭 OpenFrp 服务，进程未创建。({OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile()})");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // 5 - ERROR_ACCESS_DENIED
                switch (ex.NativeErrorCode)
                {
                    case 1072 or 1060:
                        DaemonService = null;
                        return new Model.ExecuteResult { };
                    case 1223:
                        logger.LogDebug("[LaunchService] 用户取消了服务启动请求。");
                        return new Model.ExecuteResult { Exception = ex, Message = "用户取消了服务启动请求。" };
                    case 5:
                        logger.LogDebug("[LaunchService] 操作被系统拒绝");
                        return new Model.ExecuteResult { Exception = ex, Message = "操作被系统拒绝，请检查杀软是否拦截了操作。" };
                }
                return ex;
            }
        }

        /// <summary>
        /// 启动 Daemon (自动判断 Win 服务 / 子进程模式)
        /// </summary>
        public async Task<Model.ExecuteResult> LaunchDaemonAsync()
        {
            await RefreshServiceStateAsync();

            if (DaemonProcess is not null)
            {
                return Model.ExecuteResult.Success();
            }
            
            if (DaemonService is not null)
            {
                DaemonProcess = null;

                var result = await LaunchServiceAsync();

                if (!result.HasException)
                {
                    return result;
                }
                else
                {
                    if (result.Exception is not System.ComponentModel.Win32Exception { NativeErrorCode: 1060 or 1072 })
                    {
                        logger.LogError(result.Exception, "[LaunchDaemon] 启动 OpenFrp 服务时发生了错误。");

                        return result;
                    }
                    else logger.LogDebug("[LaunchDaemon] OpenFrp 服务不存在, 回退到 子进程 Daemon 模式。");
                }
            }

            var pipeName = OpenFrp.Service.Daemon.Daemon.GetPipename();

            var mutex = new Mutex(true, $"service.{pipeName}", out var createdNewFlag);

            if (!createdNewFlag && !mutex.SafeWaitHandle.IsClosed && prevListenDaemonTask is null)
            {
                // 已经创建了一个相同命名的进程，在此监听，等到其结束。

                var processes = Process.GetProcessesByName("OpenFrp.Service");

                if (processes.Length > 0)
                {
                    string asm = FileHelper.GetServiceExecutableFile();

                    foreach (var proc in processes)
                    {
                        try
                        {
                            if (proc.GetMainModuleFileName().Equals(asm))
                            {
                                mutex.Close();
                                prevListenDaemonTask = ListenProcessUntilExit(daemon_Process3rd = proc);

                                return new Model.ExecuteResult { };
                            }
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            logger.LogWarning(ex, "[LaunchDaemon] 获取进程 (PID: {pid})的主模块文件名时发生了错误。({msg})",proc.Id,ex.Message);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex,"[LaunchDaemon] 获取进程 (PID: {pid})的主模块文件名时发生了未知错误。", proc.Id);
                        }
                    }
                }

                prevListenDaemonTask = ListenMutexUntilRelease(mutex);

                return Model.ExecuteResult.Success();
            }
            else
            {
                mutex.Close();
            }
            
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile(),
                        Arguments = "--daemon",
                        CreateNoWindow = true,
                        ErrorDialog = false,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        StandardErrorEncoding = System.Text.Encoding.Default,
                        StandardOutputEncoding = System.Text.Encoding.Default,
                    },
                    EnableRaisingEvents = true
                };

                proc.OutputDataReceived += DaemonProcessOutputDataReceived;
                proc.ErrorDataReceived += DaemonProcessOutputDataReceived;

                proc.Exited += DaemonProcessExited;

                if (!await proc.StartAsync())
                {
                    try
                    {
                        if (proc.HasExited)
                        {
                            throw new System.ComponentModel.Win32Exception(proc.ExitCode);
                        }
                        else
                        {
                            throw new InvalidOperationException("[LaunchDaemon] 无法启动 OpenFrp 服务，进程未创建。");
                        }
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }

                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();

                Semaphore_LaunchFinish = new SemaphoreSlim(0, 1);

                DaemonProcess = proc;

                return Model.ExecuteResult.Success();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                switch (ex.NativeErrorCode)
                {
                    case 5:
                        logger.LogDebug("[LaunchDaemon] 操作被系统拒绝");
                        return new Model.ExecuteResult { Exception = ex, Message = "操作被系统拒绝，请检查杀软是否拦截了操作。" };
                    case 1223:
                        logger.LogDebug("[LaunchDaemon] 用户取消了服务启动请求。");
                        return new Model.ExecuteResult { Exception = ex, Message = "用户取消了服务启动请求。" };
                    default:
                        {
                            return ex;
                        }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <summary>
        /// 杀死 Daemon 进程 (当 StatusCode 为 768 时，Message 为 Daemon 杀死前所开启的隧道的等效文本表示内容)
        /// </summary>
        /// <returns></returns>
        public async Task<Model.ExecuteResult> KillDaemonAsync()
        {
            if (DaemonProcess is not null)
            {
                if (DaemonProcess.HasExited)
                {
                    DaemonProcess = default;

                    return Model.ExecuteResult.Success();
                }

                DaemonProcess.EnableRaisingEvents = false;

                onlineTunnelsWaiter = new TaskCompletionSource<string> { };

                try
                {
                    await DaemonProcess.StandardInput.WriteLineAsync("exitProc");

                    TryDisposeSemaphore();

                    var delay = DaemonProcess.WaitForExitAsync(3000);

                    if (await Task.WhenAny(onlineTunnelsWaiter.Task, delay) != delay && onlineTunnelsWaiter.Task.Status is TaskStatus.RanToCompletion)
                    {
                        logger.LogDebug("[KillDaemonAsync] 正在保存自启动隧道 (JSON Value): {result}", onlineTunnelsWaiter.Task.Result);

                        return new Model.ExecuteResult { StatusCode = 768, Message = await onlineTunnelsWaiter.Task };
                    }

                    return Model.ExecuteResult.Success();
                }
                catch (InvalidOperationException)
                {
                    // 可能方法被二次调用或者进程已经退出。
                }
                catch (System.IO.IOException)
                {

                }
                finally
                {
                    DaemonProcess = default;
                    onlineTunnelsWaiter?.TrySetCanceled();
                }
            }
            else if (rpcManager.IsConfigured)
            {
                var resp = await rpcManager.Sync();

                if (resp.Data is { IsLogon: true, Onlines: var onlinex,HasCurrentId: true })
                {
                    logger.LogDebug("[KillDaemonAsync] RPC 返回 | 用户 ID : #{id} , 在线隧道列表: {onlinex}", resp.Data.CurrentId ,string.Join(", ", onlinex));

                    try
                    {
                        daemon_Process3rd?.Kill();
                    }
                    catch (Exception)
                    {
                        daemon_Process3rd = default;
                    }
                    finally
                    {
                        if (daemon_Process3rd is null)
                        {
                            KillProcessByName();
                        }
                        daemon_Process3rd = default;
                    }
                    return new Model.ExecuteResult
                    {
                        StatusCode = 768,
                        Message = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, int[]>()
                        {
                            {resp.Data.CurrentId.ToString(), onlinex.ToArray() }
                        })
                    };
                }

                if (DaemonService is not null)
                {
                    return await KillServiceAsync();
                }
            }

            prevListenDaemonTask = default;

            KillProcessByName();

            return new Model.ExecuteResult { };

          
        }


        // 接下来的部分是在 子进程 模式在中对进程的监听和处理。

        // 只有主动创建进程才会有 Semaphore_LaunchFinish 
        private void TryDisposeSemaphore()
        {
            try
            {
                Semaphore_LaunchFinish?.Release();
                Semaphore_LaunchFinish?.Dispose();
            }
            catch (Exception) { }
            finally
            {
                Semaphore_LaunchFinish = default;
            }
        }

        private void KillProcessByName()
        {
            var processes = Process.GetProcessesByName("OpenFrp.Service");

            if (processes.Length > 0)
            {
                string asm = FileHelper.GetServiceExecutableFile();

                foreach (var proc in processes)
                {
                    try
                    {
                        if (proc.GetMainModuleFileName().Equals(asm))
                        {
                            proc.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[KillDaemon] 获取进程 (PID: {pid}) 的主模块文件名时发生了未知错误。", proc.Id);
                    }
                }
            }

            if (!FileHelper.TryGetFRPClient(out string path)) return;
            
            string prefix = "";
            if (OSVersionHelper.IsWindows7OrGreater && !OSVersionHelper.IsWindows8OrGreater)
            {
                prefix = "legacy_";
            }

            foreach (var proc in Process.GetProcessesByName($"{prefix}frpc_windows_{OpenFrp.Service.Helpers.FileHelper.UserPlatform}"))
            {
                try
                {
                    if (proc.GetMainModuleFileName().Equals(path))
                    {
                        proc.Kill();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("[KillDaemon] 获取 FRPC 进程 (PID: {pid}) 的主模块文件名时发生了错误。({msg})", proc.Id, ex.Message);
                }
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
                            try
                            {
                                Semaphore_LaunchFinish?.Release();
                            }
                            catch (System.ObjectDisposedException)
                            {
                                break;
                            }
                        }
                        ; break;
                    case "dbug: OpenFrp.Service.Daemon.Daemon[0] Is Service Mode, Return.":
                        _ = this.RefreshServiceStateAsync();
                        break;
                    default:
                        {
                            if (msg.StartsWith("fail"))
                            {
                                if (DaemonProcess is not { } || !DaemonProcess.WaitForExit(1000)) return;
                                DaemonProcess.Exited -= DaemonProcessExited;

                                DaemonProcessExited(DaemonProcess, msg);
                            }
                            else if (msg.StartsWith("jsonValue!of+="))
                            {
                                onlineTunnelsWaiter?.TrySetResult(msg.Substring("jsonValue!of+=".Length));
                            }
                        }
                        ; break;
                }
            }
        }

        private void DaemonProcessExited(object? sender, object? data)
        {
            if (sender is not Process process || (data is null && prevListenDaemonTask is null)) return;

            try { Semaphore_LaunchFinish?.Release(); } catch { }

            int exitCode = -1;

            try
            {
                exitCode = process.ExitCode;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "[DaemonProcessExited] 获取进程退出代码时发生了错误。");
            }


            StringBuilder @string = new StringBuilder();

            @string.AppendLine($"(聚焦该窗口，按下Ctrl+C 复制内容) Deamon 异常退出");
            @string.AppendLine($"ExitCode: {exitCode}");

            if (data is string { Length: > 0 } stdErr && string.IsNullOrWhiteSpace(stdErr))
            {
                if (!stdErr.StartsWith("fail") && exitCode is 0 or 768)
                {
                    @string.Clear();

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
                        Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");
                    });
                    return;
                }
                @string.AppendLine($"\n\n错误内容:\n{stdErr}\n");
            }

            @string.AppendLine("\n\"重试\" - 将尝试重新启动守护进程；\n\"取消\" - 退出启动器。");

            App.Current.Dispatcher.Invoke(() =>
            {
                if (App.Current.MainWindow is AppWindow ap)
                {
                    ap.ShowByHANDLE();

                    ap.WindowState = WindowState.Normal;
                    ap.Activate();
                }
                var resp = Extend.SendMessage(App.Current.MainWindow, "OpenFrp Launcher", @string.ToString(), OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode.Error | OpenFrp.Service.Helpers.MessageBoxHelper.MessageMode.RetryCancel);

                switch (resp)
                {
                    case MessageBoxHelper.MessageResult.Cancel:
                        ViewModels.MainWindowViewModel.ShutdownApp();
                        break;
                    case MessageBoxHelper.MessageResult.Retry:
                        if (App.Current.MainWindow is MainWindow)
                        {
                            Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
                        }
                        else if (App.Current.MainWindow is LoginWindow)
                        {
                            Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");
                        }
                        break;
                }
            });
        }

        // 无参 作为备用方案 不是 Mutex 时请勿用
        private void DeamonProcessExited()
        {
            if (App.Current.MainWindow is MainWindow)
            {
                Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processExit");
                Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");
            }
            else if (App.Current.MainWindow is LoginWindow)
            {
                Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processExit");
                Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");
            }
        }

        internal async Task ListenProcessUntilExit(Process process,CancellationToken cancellationToken = default)
        {
            try
            {
                if (process.HasExited)
                {
                    return;
                }
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                prevListenDaemonTask = default;

                DaemonProcessExited(process, null);
            }
        }

        internal async Task ListenMutexUntilRelease(Mutex mutex)
        {
            try
            {
                if (await Task.Run(mutex.WaitOne))
                {
                    mutex.Dispose();
                }
            }
            catch(AbandonedMutexException)
            {
                mutex.Close();
            }
            catch (ObjectDisposedException)
            {
                // nothing happend;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ListenMutexUntilRelease] 监听 Mutex 时发生了错误。");
                return;
            }
            finally
            {
                prevListenDaemonTask = default;

                DeamonProcessExited();
            }
        }
    }
}
