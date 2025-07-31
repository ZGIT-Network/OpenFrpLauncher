using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Grpc.Core.Utils;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.Model;
using OpenFrp.Service;



namespace OpenFrp.Launcher.ViewModels
{
    internal partial class TunnelsViewModel : ObservableObject,IHrViewModel
    {
        public TunnelsViewModel()
        {
            if (Application.Current.MainWindow is { DataContext: MainWindowViewModel mv })
            {
                _mainWindowViewModel = mv;

                ShowAlertAction = mv.ShowAlert;

                mv.PropertyChanged += (_, e) =>
                {
                    OnPropertyChanged(e.PropertyName);
                };
            }
            if (App.RpcManager is null)
            {

            }
            WeakReferenceMessenger.Default.UnregisterAll(nameof(TunnelsViewModel));
            WeakReferenceMessenger.Default.Register<Model.RouteMessage<TunnelsViewModel, string>>(nameof(TunnelsViewModel), (_, message) =>
            {
                switch (message)
                {
                    case "processOffline":
                        {
                            disposableStream = null;

                        };break;
                    case "processOnlineWithAccount":
                        {
                            conve_CreateStreamCommand.Execute(null);

                            if (container is not null)
                            {
                                VisualStateManager.GoToElementState(container, LoadingState, false);
                            }

                            event_RefreshUserTunnelCommand.Execute(null);
                        }
                        ;break;
                }
            });
            conve_CreateStreamCommand.Execute(null);
        }

        internal const string LoadingState = "DisplayLoadingCtrl";
        internal const string NormalState = "DisplayContainerCtrl";

        internal const string UserDisplayNoraml = "UserDisplayNoraml";
        internal const string UserDisplayError = "UserDisplayError";
        internal const string UserDisplayEmpty = "UserDisplayEmpty";



        private IClientStreamWriter<Service.Proto.Request.TunnelStreamRequest>? _writer;
        private FrameworkElement? container;
        private ItemsControl? itemsController;
        private ItemsControl? remoteItemsController;
        private readonly MainWindowViewModel? _mainWindowViewModel;
        private readonly Action<string, string, InfoBarSeverity> ShowAlertAction = delegate { };

        private static IDisposable? disposableStream;

        public bool IsDaemonConnected
        {
            get => _mainWindowViewModel?.IsDaemonConnected ?? false;
        }
        public Model.UserInfo UserInfo
        {
            get
            {
                if (_mainWindowViewModel is not null)
                {
                    return _mainWindowViewModel.UserInfo;
                }
                return SettingsViewModel.__userInfo_Defualt;
            }
        }

        public bool IsUrlSchemeMode
        {
            get => _mainWindowViewModel?.IsUrlSchemeRegistered ?? false;
        }
        public bool IsUserLogin
        {
            get => !UserInfo.Equals(SettingsViewModel.__userInfo_Defualt);
        }


        private TaskCompletionSource<Google.Protobuf.Collections.RepeatedField<OpenFrp.Service.Proto.Response.TunnelStreamResponse.Types.AnonymousTunnelResponse.Types.AnonymousTunnelData>>? remoteAppWaiter;

        private TaskCompletionSource<int[]>? firstStateWaiter;

        private readonly EventWaitHandle fastLaunchWaitHandle = new EventWaitHandle(false,mode: EventResetMode.ManualReset) { };
        

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_CreateStream(CancellationToken cancellationToken)
        {
            if (App.RpcManager is null)
            {
                // TODO: THROW EXCEPTION
                return;
            }
            if (disposableStream is null)
            {
                disposableStream = await App.RpcManager.TunnelStream("testConnection", delegate { }, delegate { },cancellationToken);

                disposableStream.Dispose();
            }
            else
            {
                await Task.Delay(250, cancellationToken);
            }
            disposableStream = await App.RpcManager.TunnelStream(UserInfo.UserToken, writer => _writer = writer, StreamReader, cancellationToken);
        }

        private void StreamReader(Service.Proto.Response.TunnelStreamResponse resp)
        {
            switch (resp.State)
            {
                case Service.Proto.Response.TunnelStreamResponse.Types.TunnelStreamResponseState.Messaging:
                    {
                        if (resp.Data.TryUnpack<Service.Proto.Response.BaseResponse>(out var bp))
                        {
                            if (bp.Flag)
                            {
                                switch(bp.Message)
                                {
                                    case "W&!AskForUrlScheme":
                                        {
                                            if (IsUrlSchemeMode && App.StartupArguments.LastOrDefault() is { Length: > 0} pt && pt.StartsWith("openfrp://"))
                                            {
                                                LaunchWithFastLink(pt);
                                            }
                                        };break;
                                }
                            }
                            else
                            {
                                ShowAlertAction("无法创建隧道流(请尝试重新加载页面)", bp.Message, InfoBarSeverity.Error);
                            }
                        }
                        else if (resp.Data.TryUnpack<Service.Proto.Response.TunnelStreamResponse.Types.TunnelControlFailed>(out var cfl))
                        {
                            if (cfl.DebugInfo.TryUnpack<Google.Rpc.DebugInfo>(out var dbg))
                            {

                            }
                        }
                        else if (resp.Data.TryUnpack<Google.Rpc.DebugInfo>(out var dbInfo))
                        {
                            StringBuilder sb = new StringBuilder();
                            {
                                if (dbInfo.StackEntries.Count > 0)
                                {
                                    sb.Append("栈列表:");
                                    foreach (var stack in dbInfo.StackEntries)
                                    {
                                        sb.Append('\n');
                                        sb.Append(stack);
                                    }
                                }
                                else
                                {
                                    sb.Append("额...好像有点意外");
                                }
                            }
                            ShowAlertAction($"发生了错误: \n{dbInfo.Detail}", sb.ToString(), InfoBarSeverity.Error);
                        }
                    };break;
                case Service.Proto.Response.TunnelStreamResponse.Types.TunnelStreamResponseState.UpdateTunnel:
                    {
                        if (IsUrlSchemeMode)
                        {
                           if (resp.Data.TryUnpack(out Service.Proto.Response.TunnelStreamResponse.Types.AnonymousTunnelResponse _atr) && _atr.Datas is { } dl)
                            {
                                if (_atr.IsNewCreate && dl.Count > 0)
                                {
                                    if (_atr.Datas.FirstOrDefault() is var t && t is not null)
                                    {
                                        if (!IsUserLogin && AppRemoteTunnels.Count is 0)
                                        {
                                            VisualStateManager.GoToElementState(container, UserDisplayNoraml, false);
                                        }
                                        AppRemoteTunnels.Add(new Model.UserTunnel(t) { FirstState = true });
                                        
                                    }
                                    break;
                                }
                                else if (dl.Count is 0)
                                {
                                    if (!IsUserLogin)
                                    {
                                        VisualStateManager.GoToElementState(container, NormalState, false);
                                        VisualStateManager.GoToElementState(container, UserDisplayEmpty, false);
                                    }
                                }
                                remoteAppWaiter?.TrySetResult(dl);
                            }
                        }

                        if (resp.Data.TryUnpack(out Int32Value _v) && _v is { Value: var tunnelId })
                        {
                            ToggleSwitchWithId(tunnelId);
                        }
                        else if (resp.Data.TryUnpack(out ListValue _l))
                        {
                            firstStateWaiter?.TrySetResult(_l.Values.Select(x => (int)x.NumberValue).ToArray());
                        }
                        
                    }
                    ;break;
            }
        }

        private void ToggleSwitchWithId(params int[] tunnelId)
        {
            remoteItemsController?.Dispatcher.Invoke(delegate
            {
                foreach (var rt in AppRemoteTunnels)
                {
                    var vi = remoteItemsController.ItemContainerGenerator.ContainerFromItem(rt);

                    if (vi is ContentPresenter cp && cp.ContentTemplate?.FindName("userTunnel", cp) is Controls.UserTunnel userTunnel)
                    {
                        if (tunnelId.Contains(-rt.Id))
                        {
                            if (!IsUserLogin && AppRemoteTunnels.Count is 1)
                            {
                                VisualStateManager.GoToElementState(container, NormalState, false);
                                VisualStateManager.GoToElementState(container, UserDisplayEmpty, false);
                            }
                            userTunnel.RemoveWithAnimate(() => AppRemoteTunnels.Remove(rt));
                        }
                        else
                        {
                            continue;
                        }

                        if (iNKORE.UI.WPF.Helpers.OSVersionHelper.IsWindows10OrGreater)
                        {
                            try
                            {
                                Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Remove(rt.Name);
                            }
                            catch { }
                        }
                        break;
                    }
                }
            });
            itemsController?.Dispatcher.Invoke(delegate
            {
                foreach (var tunnel in UserTunnels)
                {
                    var vi = itemsController.ItemContainerGenerator.ContainerFromItem(tunnel);

                    bool? flag = default;
                    if (tunnelId.Contains(tunnel.Id)) flag = true;
                    if (tunnelId.Contains(-tunnel.Id)) flag = false;

                    if (!flag.HasValue) continue;

                    if (vi is ContentPresenter cp && cp.ContentTemplate?.FindName("userTunnel", cp) is Controls.UserTunnel userTunnel)
                    {
                        userTunnel.ToggleStateTo(flag.Value, force: true);

                        if (iNKORE.UI.WPF.Helpers.OSVersionHelper.IsWindows10OrGreater)
                        {
                            try
                            {
                                Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Remove(tunnel.Name);
                            }
                            catch { }
                        }
                        break;
                    }
                    continue;
                }
            });
        }

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is iNKORE.UI.WPF.Modern.Controls.Page page)
            {
                container = page;

                if (page.FindName("itemsController") is ItemsControl ic) 
                {
                    itemsController = ic;
                }
                if (page.FindName("remoteItemsController") is ItemsControl ric)
                {
                    remoteItemsController = ric;
                }

                VisualStateManager.GoToElementState(container, LoadingState, false);

                event_RefreshUserTunnelCommand.Execute(null);

                page.Unloaded += delegate
                {
                    WeakReferenceMessenger.Default.UnregisterAll(nameof(TunnelsViewModel));

                    event_RefreshUserTunnelCommand.Cancel();
                    event_DisplayExceptionCommand.Cancel();

                    _ = firstStateWaiter?.TrySetCanceled();

                    _ = _writer?.CompleteAsync();

                    conve_CreateStreamCommand.Cancel();

                    disposableStream?.Dispose();

                    _writer = null;

                    fastLaunchWaitHandle.Dispose();
                    fastLaunchWaitHandle.Close();
                };
            }
        }

        [ObservableProperty]
        private Model.ExecuteResult? executeResult;

        [ObservableProperty]
        private ObservableCollection<object> preloadSkeletons = new ObservableCollection<object> { };

        [ObservableProperty]
        private ObservableCollection<Model.UserTunnel> userTunnels = new ObservableCollection<Model.UserTunnel> { };

        [ObservableProperty]
        private ObservableCollection<Model.UserTunnel> appRemoteTunnels = new ObservableCollection<Model.UserTunnel> { };

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_DisplayException(CancellationToken cancellationToken)
        {
            if (ExecuteResult is { HasException: true, Exception: not null and Exception ex })
            {
                if (App.Current is { MainWindow: var mw } && ContentDialog.GetOpenDialog(mw) is ContentDialog da)
                {
                    da?.Hide();
                }
                var dialog = new Dialogs.ErrorContentDialog
                {

                };
                cancellationToken.Register(dialog.Hide);

                dialog.SetValue(Controls.ErrorViewer.ExceptionProperty, ex);
                await dialog.ShowAsync();
            }
        }

        [RelayCommand]
        private void @event_HandleTunnelCtrlException(Exception ex)
        {
            _mainWindowViewModel?.ShowAlert($"无法进行操作: {ex.Message}",
                    message: ex.StackTrace ?? "未知原因...", InfoBarSeverity.Error);
        }

        [RelayCommand]
        private void @event_GotoCreateTunnelPage()
        {
            Model.RouteMessage<MainWindowViewModel>.Send(typeof(Views.CreateTunnel));
        }

        [RelayCommand]
        private async Task @event_DeleteUserTunnel(Model.UserTunnel tunnel)
        {
            var resp = await Service.Net.OpenFrpApi.RemoveTunnel(tunnel.Id);
            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { Flag: true })
            {
                if (UserTunnels.Count is 1)
                {
                    VisualStateManager.GoToElementState(container, NormalState, false);
                    VisualStateManager.GoToElementState(container, UserDisplayEmpty, false);
                }
                if (itemsController is not null)
                {
                    var vi = itemsController.ItemContainerGenerator.ContainerFromItem(tunnel);
                    if (vi is ContentPresenter cp && cp.ContentTemplate?.FindName("userTunnel", cp) is Controls.UserTunnel userTunnel)
                    {
                        userTunnel.RemoveWithAnimate(() => UserTunnels.Remove(tunnel));

                        return;
                    }
                }
                UserTunnels.Remove(tunnel);
            }
            else
            {
                _mainWindowViewModel?.ShowAlert($"隧道 {tunnel.Name} 删除失败", 
                    message: resp.Data?.Message ?? resp.Message ?? resp.Exception?.Message ?? "请重试删除......", InfoBarSeverity.Error);
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task @event_TunnelSwitchInvoked(ToggleSwitch @switch)
        {
            if (@switch.DataContext is Model.UserTunnel { Tunnel: not null } tunnel && _writer != null)
            {
                string? tomlConfStr = default; 

                if (App.Settings.UseConfigLaunch)
                {
                    try
                    {
                        tomlConfStr = await GetTomlConfigPre(tunnel.Tunnel);
                    }
                    catch (Exception ex)
                    {
                        ShowAlertAction($"隧道 #{tunnel.Id} {tunnel.Name}", ex.Message ?? "请检查网络状态后尝试重新启动隧道。", InfoBarSeverity.Error);
                        ToggleSwitchWithId(-tunnel.Id);

                        return;
                    }
                }

                await _writer.WriteAsync(new Service.Proto.Request.TunnelStreamRequest
                {
                    State = @switch.IsOn ?
                    Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.LaunchTunnel :
                    Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.CloseTunnel,
                    Data = Any.Pack(new Service.Proto.Request.TunnelStreamRequest.Types.TunnelLaunchReq
                    {
                        OriginalJsonBuffer = Google.Protobuf.ByteString.CopyFrom(tunnel.GetTunnelJsonBuffer()),
                        OriginalTomlConfig = tomlConfStr ?? "",
                        Config = @switch.IsOn ? new Service.Proto.Request.TunnelStreamRequest.Types.TunnelLaunchConfig
                        {
                            AllowDisableConsoleColor = App.FrpcFeature.AllowDisableConsoleColor,
                            UseForceTls = App.FrpcFeature.UseForceTls,
                            UseDebug = App.FrpcFeature.UseDebug
                        } : default
                    }),
                });
                  

            }
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        internal static async Task<string> GetTomlConfigPre(Yue3.Model.OpenFrp.Response.Data.UserTunnel tunnel)
        {
            string? tomlConfStr = "";
            var config = await OpenFrp.Service.Net.OpenFrpApi.GetNodeConfig(tunnel.NodeId);

            
            if (config.StatusCode is System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(config.Data))
            {
                if (Tomlyn.Toml.ToModel(config.Data!) is { Count: > 0 } table && table.TryGetValue("proxies", out var proxiesValue) && proxiesValue is Tomlyn.Model.TomlTableArray { Count: > 0 } proxies)
                {

                    for (int i = proxies.Count - 1; i >= 0; i--)
                    {
                        if (!proxies[i].TryGetValue("name", out var nameValue) || !nameValue.Equals(tunnel.Name))
                        {
                            proxies.RemoveAt(i);
                        }
                    }
                    if (Tomlyn.Toml.TryFromModel(table, out tomlConfStr, out var diagnostics))
                    {
                        if (string.IsNullOrEmpty(tomlConfStr))
                        {
                            throw new NullReferenceException("无法将对象转换成 Toml 等效字符串。");
                        }
                        return tomlConfStr;
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Join(",", diagnostics.Select(x => x.Message)));
                    }
                }
                else throw new InvalidOperationException($"未能在节点 #{tunnel.NodeId} {tunnel.NodeName} 获取到有效隧道配置。");
            }
            else if (config.Exception is not null)
            {
                throw config.Exception;
            }
            else
            {
                throw new InvalidOperationException(config.Message ?? "未能成功启动");
            }
           
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshUserTunnel(CancellationToken cancellationToken)
        {
            VisualStateManager.GoToElementState(container, LoadingState, false);

            if (!IsUserLogin)
            {

                if (IsUrlSchemeMode)
                {
                    await Task.Delay(1000, cancellationToken);

                    await RequestForOnlineTunnelAndRemoteTunnel();

                    VisualStateManager.GoToElementState(container, NormalState, false);

                    CreateLoadForAppRemote(cancellationToken);
                    
                   

                    return;
                }
            }

            CreatePreloadSkeleton();

            await Task.Delay(1000, cancellationToken);




            await RequestForOnlineTunnelAndRemoteTunnel();


            await Task.Delay(1000,cancellationToken);

            var resp = await Service.Net.OpenFrpApi.GetUserTunnels(cancellationToken);

            try
            {
                if (!this.UpdateState(resp, () => resp.Data is { List: not null }))
                {
                    VisualStateManager.GoToElementState(container, NormalState, false);
                    VisualStateManager.GoToElementState(container, UserDisplayError, false);
                }
                else if (resp.Data!.Total is 0)
                {
                    VisualStateManager.GoToElementState(container, NormalState, false);
                    VisualStateManager.GoToElementState(container, UserDisplayEmpty, false);
                }
                else
                {
                    var onlineTunnels = await firstStateWaiter!.Task.WaitAsync(cancellationToken);
                
                    if (onlineTunnels is null || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    UserTunnels = new ObservableCollection<Model.UserTunnel>();

                    VisualStateManager.GoToElementState(container, NormalState, false);
                    VisualStateManager.GoToElementState(container, UserDisplayNoraml, false);

                    _ = container?.Dispatcher.Invoke(async () =>
                    {
                        try
                        {
                            foreach (var tunnel in resp.Data.List!)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }
                                var v = new Model.UserTunnel(tunnel);
                                if (onlineTunnels.Contains(tunnel.Id))
                                {
                                    v.FirstState = true;
                                }
                                UserTunnels.Add(v);

                                await Task.Delay(75, cancellationToken);
                            }
                        }
                        finally
                        {
                            //firstStateWaiter = null;
                        }
                    });
                }
            }
            finally 
            {
                CreateLoadForAppRemote(cancellationToken);
            }
        }

        private async Task RequestForOnlineTunnelAndRemoteTunnel()
        {
            if (firstStateWaiter is null || firstStateWaiter.Task.IsCompleted || remoteAppWaiter is null || remoteAppWaiter.Task.IsCompleted)
            {
                if (firstStateWaiter is null || firstStateWaiter.Task.IsCompleted)
                {
                    firstStateWaiter = new TaskCompletionSource<int[]>();
                }

                if (remoteAppWaiter is null || remoteAppWaiter.Task.IsCompleted)
                {
                    remoteAppWaiter = new TaskCompletionSource<Google.Protobuf.Collections.RepeatedField<Service.Proto.Response.TunnelStreamResponse.Types.AnonymousTunnelResponse.Types.AnonymousTunnelData>> { };
                }

                if (_writer is not null)
                {
                    await _writer.WriteAsync(new Service.Proto.Request.TunnelStreamRequest
                    {
                        State = Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.GetOnlineTunnel
                    });
                }
            }
        }

        private void CreateLoadForAppRemote(CancellationToken cancellationToken = default)
        {
            if (IsUrlSchemeMode && remoteAppWaiter is not null)
            {
                _ = container?.Dispatcher.Invoke(async () =>
                {
                    try
                    {
                        var v2 = await remoteAppWaiter.Task.WaitAsync(cancellationToken);

                        if (v2 is not { Count: > 0 }) { return; }

                        AppRemoteTunnels ??= new ObservableCollection<Model.UserTunnel> { };

                        if (AppRemoteTunnels.Count > 0)
                        {
                            AppRemoteTunnels.Clear();
                        }

                        foreach (var d in v2)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            AppRemoteTunnels.Add(new Model.UserTunnel(d) { FirstState = true });

                            await Task.Delay(75, cancellationToken);
                        }
                    }
                    finally
                    {
                        fastLaunchWaitHandle.Set();

                        fastLaunchWaitHandle.Reset();
                    }
                });
            }
        }

        internal async void LaunchWithFastLink(string link)
        {
            try { App.StartupArguments.Remove(link); } catch { }

            
            await Task.Run(fastLaunchWaitHandle.WaitOne);

            if (firstStateWaiter is null || _writer is null) return;

            var solveLink = OpenFrp.Service.Net.HttpClient.ParseQueryString(link);

            if (!solveLink.TryGetValue("proxy", out string? tidString) || !int.TryParse(tidString, out int tid))
            {
                return;
            }

            if (App.Current is { MainWindow: var mw } && ContentDialog.GetOpenDialog(mw) is var di)
            {
                if (di is Dialogs.RequestForFastLaunchDialog dt)
                {
                    if (dt.TunnelId == tid) return;
                }
                di?.Hide();
            }
            var onlineTunnels = await firstStateWaiter.Task;

            if (onlineTunnels is null || onlineTunnels.Contains(tid)) return;

            var dialog = new Dialogs.RequestForFastLaunchDialog(solveLink) { };

            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                
                await _writer.WriteAsync(new Service.Proto.Request.TunnelStreamRequest
                {
                    State = Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.LaunchTunnel,
                    Data = Any.Pack(new Service.Proto.Request.TunnelStreamRequest.Types.TunnelLaunchReq
                    {
                        OriginalFastLaunchCall = link,
                        Config = new Service.Proto.Request.TunnelStreamRequest.Types.TunnelLaunchConfig
                        {
                            AllowDisableConsoleColor = App.FrpcFeature.AllowDisableConsoleColor,
                            UseForceTls = App.FrpcFeature.UseForceTls,
                            UseDebug = App.FrpcFeature.UseDebug
                        } 
                    }),
                });
            }
        }

        private void CreatePreloadSkeleton()
        {
            int count = -1;
            if (App.Current is { MainWindow.DataContext: ViewModels.MainWindowViewModel { IsUserLogon: true, UserInfo: var userInfo } mvm })
            {
                count = userInfo.UsedProxies;
            }
            else
            {
                count = 4;
            }
            PreloadSkeletons = new ObservableCollection<object>(Enumerable.Range(0, count).Select(_ => new object { }));
        }
    }
}
