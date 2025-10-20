using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcDotNetNamedPipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFrp.Launcher.ViewModels;
using OpenFrp.Service.Helpers;
using OpenFrp.Service.Proto.Request;

namespace OpenFrp.Launcher.Rpc
{
    internal class BgService : IDisposable
    {
        public BgService() : this(GetPipeName())
        {
           
        }
        public BgService(string pipeName)
        {
            this.pipeName = pipeName;

            WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset)
            {

            };

            logger = App.ServiceProvider.GetRequiredService<ILogger<BgService>>();
        }

        private readonly ILogger<BgService> logger;

        private static string GetPipeName()
        {

            byte[] buffer = new byte[24];
#if NET
            Random.Shared.NextBytes(buffer);
#else
            Random rand = new Random();
            rand.NextBytes(buffer);
#endif

            return $"ofapp.pipe.{Convert.ToBase64String(buffer)}";
        }
        private readonly string pipeName;

        private NamedPipeServer? currentPipeServer { get; set; }
        private Process? currentRpcProcess { get; set; }

        internal EventWaitHandle WaitHandle { get; set; }

        public void LaunchServer()
        {
            if (currentPipeServer != null)
            {
                return;
            }
            var cur = WindowsIdentity.GetCurrent();
            if (cur.User is null)
            {
                return;
            }


            var security = new PipeSecurity();

            security.AddAccessRule(new PipeAccessRule(cur.User, PipeAccessRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));

            currentPipeServer = new NamedPipeServer(pipeName, new NamedPipeServerOptions()
            {
                PipeSecurity = security
            });

            var service = new BackgroundServiceInst(this);

            Service.Proto.BackgroundService.OpenFrpBackgroundService.BindService(currentPipeServer.ServiceBinder, service);

            currentPipeServer.Start();
        }

        public async Task<Model.ExecuteResult> LaunchProcessAndWait()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile(),
                    Arguments = $"--inst frpc-update -pipe=\"{pipeName}\" -useProxy={App.Settings.UseProxy}",
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    UseShellExecute = true,
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    ErrorDialogParentHandle = IntPtr.Zero,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            try
            {
                if (await proc.StartAsync())
                {
                    //proc.OutputDataReceived += ProcHandle_StdOutOrStdErr;
                    //proc.ErrorDataReceived += ProcHandle_StdOutOrStdErr;

                    //proc.BeginErrorReadLine();
                    //proc.BeginOutputReadLine();
                    LastDebugInfo = null;
                    currentRpcProcess = proc;
                }
                else
                {
                    throw new System.ComponentModel.Win32Exception(proc.ExitCode, "进程启动失败。");
                }
            }
            catch (Exception ex)
            {
                System.ComponentModel.Win32Exception? winEx = ex as System.ComponentModel.Win32Exception ?? ex.InnerException as System.ComponentModel.Win32Exception;

                if (winEx != null)
                {
                    switch (winEx.NativeErrorCode)
                    {
                        case 1223:
                            logger.LogDebug("用户已取消操作。");
                            return Model.ExecuteResult.Fail("Cancelled",1223);
                        case 5:
                            logger.LogDebug("操作被系统拒绝，请检查杀软是否拦截了操作。");
                            return new Model.ExecuteResult { Exception = ex, Message = "操作被系统拒绝，请检查杀软是否拦截了操作。" };
                    }
                }
                return ex;
            }

            await Task.Run(currentRpcProcess.WaitForExit);

            try
            {
                if (currentRpcProcess.ExitCode != 0)
                {
                    return new Model.ExecuteResult
                    {
                        Exception = new Exception("后台服务进程异常退出，错误代码：" + currentRpcProcess.ExitCode),
                        Message = "后台服务进程异常退出，错误代码：" + currentRpcProcess.ExitCode,
                        StatusCode = currentRpcProcess.ExitCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new Model.ExecuteResult
                {
                    Exception = ex,
                    Message = "后台服务进程异常退出",
                    StatusCode = -1
                };
            }

            return Model.ExecuteResult.Success();
        }

        private bool _isDisposed = false;


        public void Dispose()
        {
            if (_isDisposed) return;

            if (currentRpcProcess is { HasExited: false })
            {
                currentRpcProcess.Kill();
            }

            currentPipeServer?.Kill();

            currentRpcProcess?.Dispose();
            currentPipeServer?.Dispose();
            WaitHandle?.Dispose();

            _isDisposed = true;
        }

        public void KillServer() => currentPipeServer?.Kill();

        public Google.Rpc.DebugInfo? LastDebugInfo { get; internal set; }

        public event DownloadServiceFallbackHandler DownloadServiceFallback = delegate { };

        public delegate void DownloadServiceFallbackHandler(DownloadFallback.Types.DownloadFallbackType type,Any data);

        private class BackgroundServiceInst : Service.Proto.BackgroundService.OpenFrpBackgroundService.OpenFrpBackgroundServiceBase
        {
            public BackgroundServiceInst(BgService service)
            {
                //,Action<DownloadFallback.Types.DownloadFallbackType, Any> downloadServiceFallbackTo
                this.bgService = service;
            }

            private BgService bgService;


            public override Task<Empty> DownloadServiceFallback(DownloadFallback request, ServerCallContext context)
            {
                switch (request.State)
                {
                    case DownloadFallback.Types.DownloadFallbackType.Messaging 
                    when request.Data.Is(Google.Rpc.DebugInfo.Descriptor):
                        bgService.logger.LogInformation($"[服务端消息] {request.Data.Unpack<Google.Rpc.DebugInfo>().Detail}");
                        bgService.LastDebugInfo = request.Data.Unpack<Google.Rpc.DebugInfo>();
                        break;
                    default: bgService.DownloadServiceFallback?.Invoke(request.State, request.Data); break;
                }
                return Task.FromResult(new Empty { });
            }
        }
    }
}
