﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Documents;
using Grpc.Core;
using static OpenFrp.Launcher.RpcManager;
using Proto = OpenFrp.Service.Proto;

namespace OpenFrp.Launcher
{
    internal class RpcManager
    {
        public static string? UserSecureCode { get; set; }

        public static void Configre(string pipeName)
        {
            var channel = new GrpcDotNetNamedPipes.NamedPipeChannel(".", pipeName, new GrpcDotNetNamedPipes.NamedPipeChannelOptions
            {
                ConnectionTimeout = 10
            });

            Client = new Service.Proto.Service.OpenFrp.OpenFrpClient(channel);
        }

        public static OpenFrp.Service.Proto.Service.OpenFrp.OpenFrpClient? Client { get; set; }

        /// <summary>
        /// 登录
        /// </summary>
        public static async Task<Service.RpcResponse<string>> LoginAsync(Proto.Request.LoginRequest request,TimeSpan timeOut = default,CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                var resp = await Task.Run(async () => await Client.LoginAsync(request, deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken));

                return new Service.RpcResponse<string>
                {
                    IsSuccess = resp.Flag,
                    Message = resp.Message,
                    Data = (resp.Data is not null && resp.Data.TryUnpack(out Google.Protobuf.WellKnownTypes.StringValue value)) ? value.Value : default                
                };
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <summary>
        /// 登出
        /// </summary>
        public static async Task<Service.RpcResponse> LogoutAsync(Proto.Request.LogoutRequest request, TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                return await Task.Run(async ()=> await Client.LogoutAsync(request, deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken));

            }catch(Exception ex) { return ex; }
        }

        public static async Task<Service.RpcResponse<Proto.Response.SyncResponse>> SyncAsync(TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                return new Service.RpcResponse<Proto.Response.SyncResponse>
                {
                    Data = await Task.Run(async () => await Client.SyncAsync(new Proto.Request.SyncRequest { SecureCode = UserSecureCode ?? string.Empty }, deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken)),
                    IsSuccess = true
                };
            }
            catch (Exception ex) { return ex; }
        }

        public static async Task<Service.RpcResponse<Proto.Response.SyncResponse>> SyncAsync(Proto.Request.SyncRequest request, TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));
                
                return new Service.RpcResponse<Proto.Response.SyncResponse>
                {
                    Data = await Task.Run(async () => await Client.SyncAsync(request,deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken)),
                    IsSuccess = true,
                };
            }
            catch (Exception ex) { return ex; }
        }

        public static async Task<Service.RpcResponse> LaunchTunnel(Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel,
            TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                return await Task.Run(async () => await Client.LaunchAsync(new Proto.Request.TunnelRequest
                {
                    SecureCode = UserSecureCode ?? string.Empty,
                    UseDebug = Properties.Settings.Default.UseDebugMode,
                    UseTlsEncrypt = Properties.Settings.Default.UseTlsEncrypt,
                    DisableConsoleColor = App.AllowDisableConsoleColor,
                    UserTunnelJson = JsonSerializer.Serialize(tunnel)
                }, deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken));
            }
            catch (Exception ex) { return ex; }
        }

        public static async Task<Service.RpcResponse> CloseTunnel(Awe.Model.OpenFrp.Response.Data.UserTunnel tunnel, TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                return await Task.Run(async () => await Client.CloseAsync(new Proto.Request.TunnelRequest
                {
                    SecureCode = UserSecureCode ?? string.Empty,
                    UserTunnelJson = JsonSerializer.Serialize(tunnel)
                }, deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken));
            }
            catch (Exception ex) { return ex; }
        }


        public static async Task<Service.RpcResponse<Proto.Response.ActiveProcessResponse>> GetActiveProcess(TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                var resp = await Task.Run(async () => await Client.GetActiveProcessAsync(new Proto.Request.SyncRequest
                {
                    SecureCode = UserSecureCode ?? string.Empty
                }, deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken));

                return new Service.RpcResponse<Proto.Response.ActiveProcessResponse>
                {
                    Data = resp.Data.Is(Proto.Response.ActiveProcessResponse.Descriptor)  ? resp.Data.Unpack<Proto.Response.ActiveProcessResponse>() : default,
                    IsSuccess = resp.Flag,
                    Message = resp.Message
                };
            }
            catch (Exception ex) { return ex; }
        }



        public static async Task<Service.RpcResponse> ClearLogStream(TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                var resp = await Task.Run(async () => await Client.ClearLogStreamAsync(new Proto.Request.SyncRequest
                {
                    SecureCode = UserSecureCode ?? string.Empty
                }, deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken));

                return resp;
            }
            catch (Exception ex) { return ex; }
        }

        public static async Task<Exception?> NotifiyStream(Action<Proto.Response.NotiflyStreamResponse> responseReceive,TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                var stream = Client.NotifiyStream(
                    new Proto.Request.BaseRequest
                    {
                        SecureCode = UserSecureCode ?? string.Empty
                    }, 
                    deadline: CreateDeadline(timeOut), 
                    cancellationToken: cancellationToken);

                while (await stream.ResponseStream.MoveNext(cancellationToken))
                {
                    responseReceive?.Invoke(stream.ResponseStream.Current);
                }

                return default;
            }
            catch (Exception ex){ return ex; }
        }

        public static async Task<Exception?> LogStream(
            Action<Proto.Response.LogResponse> responseReceive,
            Action<Proto.Response.LogResponse> errorResponseReceive,
            TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                var resp = Client.LogStream(new Proto.Request.SyncRequest { SecureCode = UserSecureCode ?? string.Empty },deadline: CreateDeadline(timeOut), cancellationToken:cancellationToken);

                while (await resp.ResponseStream.MoveNext(cancellationToken))
                {
                    if (resp.ResponseStream.Current is not { Data: not null } current) continue;

                    if (current.Data.Is(Proto.Response.LogResponse.Descriptor) && current.Data.TryUnpack<Proto.Response.LogResponse>(out var lp))
                    {
                        if (resp.ResponseStream.Current.Flag)
                        {
                            responseReceive?.Invoke(lp);
                        }
                        else
                        {
                            errorResponseReceive?.Invoke(lp);
                        }
                    }
                    else if (current.Data.Is(Google.Protobuf.WellKnownTypes.Empty.Descriptor))
                    {
                        errorResponseReceive?.Invoke(new Proto.Response.LogResponse
                        {
                            Logs =
                            {
                                 new Proto.Response.LogResponse.Types.LogData()
                                 {
                                     Date = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                                     Content = $"LogStream() => {current.Message}",
                                     Executor = "Launcher",
                                     Id = 0,
                                     Level = 2
                                 }
                            }
                        });
                    }
                }

                return default;
            }
            catch (Exception ex) { return ex; }
        }

        public static async Task<Exception?> UdpProcedureCall(TimeSpan timeOut = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Client is null) throw new NullReferenceException(nameof(Client));

                await Task.Run(async () => await Client.UdpProcedureCallAsync(new Google.Protobuf.WellKnownTypes.Empty { },deadline: CreateDeadline(timeOut), cancellationToken: cancellationToken));

                return default;
            }
            catch (Exception ex) { return ex; }
        }

        public static DateTime? CreateDeadline(TimeSpan timeSpan = default)
        {
            if (timeSpan.TotalMilliseconds is 0) { return default; }
            else { return DateTime.UtcNow.Add(timeSpan); }
        }

        public class AsyncDuplexCaller<T>
        {
            public Exception? Exception { get; set; }

            public IAsyncStreamWriter<T>? Writer { get; set; }

            public Task WriteAsync(T data)
            {
                if (Writer is null) throw new NullReferenceException(nameof(Writer));

                return Writer.WriteAsync(data);
            }
        }
    }
}
