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
using Microsoft.Xaml.Behaviors.Core;
using OpenFrp.Service;
using OpenFrp.Service.Helpers;

namespace OpenFrp.Launcher.Rpc
{
    class DaemonManager : OpenFrp.Service.Rpc.DaemonManager
    {
        public DaemonManager(Launcher.Rpc.RpcManager rpcManager, ILogger<Service.Rpc.DaemonManager> logger) : base(rpcManager, logger)
        {
        }

        public new async Task<Model.ExecuteResult> WaitForConfigureAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await base.WaitForConfigureAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return Model.ExecuteResult.Success();
        }

        public new async Task<Model.ExecuteResult> LaunchDaemonAsync()
        {
            try
            {
                await base.LaunchDaemonAsync();
            }
            catch (Exception ex)
            {
                return ex;
            }
            return Model.ExecuteResult.Success();
        }

        public new async Task<Model.ExecuteResult> KillDaemonAsync()
        {
            try
            {
                string str = await base.KillDaemonAsync();

                if (!string.IsNullOrEmpty(str))
                {
                    return new Model.ExecuteResult { StatusCode = 768, Message = str };
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return Model.ExecuteResult.Success();
        }


        protected override void DaemonProcessExited(object? sender, object? data)
        {
            if (sender is not Process process || (data is null && prevListenDaemonTask is null)) return;

            try { Semaphore_LaunchFinish?.Release(); } catch { }

            int exitCode = -1;

            rpcManager.Crack();

            try
            {
                exitCode = process.ExitCode;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DaemonProcessExited] 获取进程退出代码时发生了错误。");
            }




            StringBuilder @string = new StringBuilder();

            @string.AppendLine($"(聚焦该窗口，按下Ctrl+C 复制内容) Deamon 异常退出");
            @string.AppendLine($"ExitCode: {exitCode}");


            if (data is EventArgs)
            {
                DaemonProcess = default;
            }
            switch (exitCode)
            {
                case 0 or 768:
                    {
                        if (data is string { Length: > 0 } stdErr && string.IsNullOrWhiteSpace(stdErr))
                        {
                            if (stdErr.StartsWith("fail"))
                            {
                                @string.AppendLine($"\n\n错误内容:\n{stdErr}\n");
                            }
                            else
                            {
                                goto case -2;
                            }
                        }
                        else if (data is EventArgs)
                        {
                            goto case -2;
                        }
                    }
                    ; break;
                case 1 when data is EventArgs:
                case -2:
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processExit");
                            Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processLfec");

                            Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processExit");
                            Model.RouteMessage<ViewModels.LoginWindowViewModel>.Send("processLfec");
                        });
                        @string.Clear();
                        return;
                    }
                    ;
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

        protected override void DeamonProcessExited()
        {
            base.DeamonProcessExited();

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
    }
}
