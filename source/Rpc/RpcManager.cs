using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using OpenFrp.Service.Daemon;

namespace OpenFrp.Launcher.Rpc
{
    internal class RpcManager
    {
        internal readonly string? PipeName;

        internal readonly OpenFrp.Service.Proto.Service.OpenFrp.OpenFrpClient? OpenFrpDeamonRpcClient;

        internal readonly Metadata GlobalHeader = new Metadata { };

        //private static DateTime Deadline { get => DateTime.UtcNow.AddSeconds(10); }

        public RpcManager() : this(Daemon.GetPipename())
        {
            
        }

        public RpcManager(string pipeName) 
        {
            PipeName = pipeName;

            var channel = new GrpcDotNetNamedPipes.NamedPipeChannel(".", pipeName, new GrpcDotNetNamedPipes.NamedPipeChannelOptions
            {
                ConnectionTimeout = 10
            });
            
            OpenFrpDeamonRpcClient = new OpenFrp.Service.Proto.Service.OpenFrp.OpenFrpClient(channel);
        }

        public async Task<OpenFrp.Service.Proto.RpcResponse<Service.Proto.Response.SyncResponse>> Sync(CancellationToken cancellationToken = default)
        {
            if (OpenFrpDeamonRpcClient is null)
            {
                throw new ArgumentNullException(nameof(OpenFrpDeamonRpcClient));
            }
            try
            {
                var resp = await Task.Run(async () => await OpenFrpDeamonRpcClient.SyncAsync(
                    /* deadline: Deadline, */
                    request: new Google.Protobuf.WellKnownTypes.Empty { },
                    cancellationToken: cancellationToken));

                if (resp is not null)
                {
                    return resp;
                }
            }
            catch(RpcException rpcEx)
            {
                return rpcEx;
            }
            catch(Exception ex)
            {
                return ex;
            }
            return Service.Proto.RpcResponse<Service.Proto.Response.SyncResponse>.FailedResponse;
        }

        public async Task<OpenFrp.Service.Proto.RpcResponse> Login(Service.Proto.Request.LoginRequest request,CancellationToken cancellationToken = default)
        {
            if (OpenFrpDeamonRpcClient is null)
            {
                throw new ArgumentNullException(nameof(OpenFrpDeamonRpcClient));
            }
            try
            {
                GlobalHeader.Clear();

                if (Service.Net.OpenFrpApi.GetAuthorization() is not string { Length: > 0 } auth)
                {
                    throw new NullReferenceException(nameof(Service.Net.OpenFrpApi.GetAuthorization));
                }
                var resp = await Task.Run(() =>  OpenFrpDeamonRpcClient.LoginAsync(
                    /* deadline: Deadline, */
                    request: request,
                    headers: new Metadata
                    {
                        { "Authorization",auth }
                    },
                    cancellationToken: cancellationToken));

                if (await resp.ResponseHeadersAsync is not { Count: > 0 } headers || headers.Get("HashCode") is not { Value: string hashCode })
                {
                    throw new NullReferenceException(nameof(resp.ResponseHeadersAsync));
                }
                GlobalHeader.Add("HashCode", hashCode);

                return await resp.ResponseAsync;
            }
            catch (RpcException rpcEx)
            {
                return rpcEx;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async Task<OpenFrp.Service.Proto.RpcResponse> Logout(CancellationToken cancellationToken = default)
        {
            if (OpenFrpDeamonRpcClient is null)
            {
                throw new ArgumentNullException(nameof(OpenFrpDeamonRpcClient));
            }
            if (GlobalHeader is null || GlobalHeader.Count < 1)
            {
                throw new ArgumentNullException(nameof(GlobalHeader));
            }
            try
            {
                var resp = await Task.Run(async () => await OpenFrpDeamonRpcClient.LogoutAsync(
                    request: new Google.Protobuf.WellKnownTypes.Empty { },
                    /* deadline: Deadline, */
                    headers: GlobalHeader,
                    cancellationToken: cancellationToken));

                if (resp.Flag)
                {
                    GlobalHeader.Clear();
                }

                return Service.Proto.RpcResponse.SuccessResponse;
            }
            catch (RpcException rpcEx)
            {
                return rpcEx;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async Task<IDisposable> NotificationStream(
             Action<Service.Proto.Response.NotificationStreamResponse> readerCallback,
            CancellationToken cancellationToken = default)
        {
            if (OpenFrpDeamonRpcClient is null)
            {
                throw new ArgumentNullException(nameof(OpenFrpDeamonRpcClient));
            }

            var duplex = OpenFrpDeamonRpcClient.NotificationStream(
                request: new Empty { },
                headers: GlobalHeader,
                /* deadline: Deadline, */
                cancellationToken: cancellationToken);

            try
            {
                while (await duplex.ResponseStream.MoveNext(cancellationToken))
                {
                    readerCallback.Invoke(duplex.ResponseStream.Current);
                }
            }
            catch (Grpc.Core.RpcException e) when (e.StatusCode is StatusCode.Cancelled) { }
            //catch (Grpc.Core.RpcException e) when (e.StatusCode is StatusCode.) { }

            return duplex;
        }

        public async Task<IDisposable> TunnelStream(
            string userTokenAccess,
            Action<IClientStreamWriter<Service.Proto.Request.TunnelStreamRequest>> writerCallback,
            Action<Service.Proto.Response.TunnelStreamResponse> readerCallback,
            CancellationToken cancellationToken = default)
        {
            if (OpenFrpDeamonRpcClient is null)
            {
                throw new ArgumentNullException(nameof(OpenFrpDeamonRpcClient));
            }
            if (GlobalHeader is null || GlobalHeader.Count < 1)
            {
                throw new ArgumentNullException(nameof(GlobalHeader));
            }
            
            var duplex = OpenFrpDeamonRpcClient.TunnelStream(
                headers: GlobalHeader,
                /* deadline: Deadline, */
                cancellationToken: cancellationToken);

            writerCallback.Invoke(duplex.RequestStream);

#if NET
            await duplex.RequestStream.WriteAsync(new Service.Proto.Request.TunnelStreamRequest
            {
                State = Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.Prepare,
                Data = Any.Pack(new StringValue
                {
                    Value = userTokenAccess
                }) 
            },cancellationToken);
#elif NETFRAMEWORK
            await duplex.RequestStream.WriteAsync(new Service.Proto.Request.TunnelStreamRequest
            {
                State = Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.Prepare,
                Data = Any.Pack(new StringValue
                {
                    Value = userTokenAccess
                }) 
            });
#endif
            //var status = duplex.GetStatus();
            
            
            try
            {
                while (await duplex.ResponseStream.MoveNext(cancellationToken))
                {
                    readerCallback.Invoke(duplex.ResponseStream.Current);
                }
            }
            catch (Grpc.Core.RpcException e) when (e.StatusCode is StatusCode.Cancelled) {  }

            return duplex;
        }

        public async Task<IDisposable> LogStream(
            Google.Protobuf.Collections.MapField<int, int> KnownLogIndexMapping,
            Action<Service.Proto.Response.LogStreamResponse> readerCallback,
            CancellationToken cancellationToken)
        {
            if (OpenFrpDeamonRpcClient is null)
            {
                throw new ArgumentNullException(nameof(OpenFrpDeamonRpcClient));
            }
            var duplex = OpenFrpDeamonRpcClient.LogStream(
                request: new Service.Proto.Request.LogStreamRequest { IndexMaps = { KnownLogIndexMapping } },
                headers: GlobalHeader,
                /* deadline: Deadline, */
                cancellationToken: cancellationToken);

            try
            {
                while (await duplex.ResponseStream.MoveNext(cancellationToken))
                {
                    readerCallback.Invoke(duplex.ResponseStream.Current);
                }
            }
            catch (Grpc.Core.RpcException e) when (e.StatusCode is StatusCode.Cancelled) {  }

            return duplex;
        }

        public async Task<OpenFrp.Service.Proto.RpcResponse> ClearLog(int id,CancellationToken cancellationToken = default)
        {
            if (OpenFrpDeamonRpcClient is null)
            {
                throw new ArgumentNullException(nameof(OpenFrpDeamonRpcClient));
            }
            try
            {
                var resp = await Task.Run(async () => await OpenFrpDeamonRpcClient.ClearLogAsync(
                    request: new Service.Proto.Request.ClearLogRequest { LogId = id },
                    /* deadline: Deadline, */
                    headers: GlobalHeader,
                    cancellationToken: cancellationToken));

                return resp;

            }
            catch (RpcException rpcEx)
            {
                return rpcEx;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
