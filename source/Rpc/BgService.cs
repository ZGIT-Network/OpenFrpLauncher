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
using OpenFrp.Launcher.ViewModels;
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
        }

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

            var service = new BackgroundServiceInst(DownloadServiceFallback.Invoke);

            Service.Proto.BackgroundService.OpenFrpBackgroundService.BindService(currentPipeServer.ServiceBinder, service);

            currentPipeServer.Start();
        }

        public async Task LaunchProcessAndWait()
        {
            currentRpcProcess = await Task.Run(() =>
            {
                return Process.Start(new ProcessStartInfo()
                {
                    FileName = OpenFrp.Service.Helpers.FileHelper.GetServiceExecutableFile(),
                    Arguments = $"--inst frpc-update -pipe=\"{pipeName}\" -useProxy={App.Settings.UseProxy}",
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    UseShellExecute = true,
                    ErrorDialogParentHandle = IntPtr.Zero,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            });

            if (currentRpcProcess is null)
            {
                return;
            }
            await Task.Run(currentRpcProcess.WaitForExit);
        }

        private bool _isDisposed = false;



        public void Dispose()
        {
            if (_isDisposed) return;

            currentPipeServer?.Kill();


            currentRpcProcess?.Dispose();
            currentPipeServer?.Dispose();
            WaitHandle?.Dispose();

            _isDisposed = true;
        }

        public void KillServer() => currentPipeServer?.Kill();

        public event DownloadServiceFallbackHandler DownloadServiceFallback = delegate { };

        public delegate void DownloadServiceFallbackHandler(DownloadFallback.Types.DownloadFallbackType type,Any data);

        private class BackgroundServiceInst : Service.Proto.BackgroundService.OpenFrpBackgroundService.OpenFrpBackgroundServiceBase
        {
            public BackgroundServiceInst(Action<DownloadFallback.Types.DownloadFallbackType, Any> downloadServiceFallbackTo)
            {
                DownloadServiceFallbackTo = downloadServiceFallbackTo;
            }

            private Action<DownloadFallback.Types.DownloadFallbackType,Any>? DownloadServiceFallbackTo;

            public override Task<Empty> DownloadServiceFallback(DownloadFallback request, ServerCallContext context)
            {
                DownloadServiceFallbackTo?.Invoke(request.State, request.Data);

                return Task.FromResult(new Empty { });
            }
        }
    }
}
