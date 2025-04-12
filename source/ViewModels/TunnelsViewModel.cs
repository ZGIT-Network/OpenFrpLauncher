using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Grpc.Core.Utils;
using iNKORE.UI.WPF.Modern.Controls;
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
            conve_CreateStreamCommand.Execute(null);
        }

        internal const string LoadingState = "DisplayLoadingCtrl";
        internal const string ErrorState = "DisplayErrorCtrl";
        internal const string EmptyState = "DisplayEmptyCtrl";
        internal const string NormalState = "DisplayContainerCtrl";



        private IClientStreamWriter<Service.Proto.Request.TunnelStreamRequest>? _writer;
        private FrameworkElement? container;
        private ItemsControl? itemsController;
        private readonly MainWindowViewModel? _mainWindowViewModel;
        private readonly Action<string, string, InfoBarSeverity> ShowAlertAction = delegate { };

        private static IDisposable? disposableStream;

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

        private TaskCompletionSource<int[]>? firstStateWaiter = new TaskCompletionSource<int[]>();

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
                CancellationTokenSource s = new CancellationTokenSource();

                s.CancelAfter(1000);

                disposableStream = await App.RpcManager.TunnelStream("testConnection", delegate { }, delegate { },s.Token);

                s.Dispose();

                disposableStream.Dispose();
            }
            else
            {
                await Task.Delay(500, cancellationToken);
            }
            disposableStream = await App.RpcManager.TunnelStream(UserInfo.UserToken, writer => _writer = writer, StreamReader, cancellationToken);
        }

        private void StreamReader(Service.Proto.Response.TunnelStreamResponse resp)
        {
            //System.Diagnostics.Debug.WriteLine($"[Tunnel Stream Reader] {resp.State} {resp.Data?.TypeUrl}");

            switch (resp.State)
            {
                case Service.Proto.Response.TunnelStreamResponse.Types.TunnelStreamResponseState.Messaging:
                    {
                        if (resp.Data.TryUnpack<Service.Proto.Response.BaseResponse>(out var bp))
                        {
                            ShowAlertAction("无法创建隧道流(请尝试重新加载页面)", bp.Message, InfoBarSeverity.Error);
                            //_ = bp.Message;
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
                        if (resp.Data.TryUnpack(out Int32Value _v) && _v is { Value: var tunnelId } && itemsController != null)
                        {
                            ToggleSwitchWithId(tunnelId);
                        }
                        else if (resp.Data.TryUnpack(out ListValue _l))
                        {
                            firstStateWaiter?.TrySetResult(_l.Values.Select(x => (int)x.NumberValue).ToArray());
                        }
                    };break;
            }
        }

        private void ToggleSwitchWithId(params int[] tunnelId)
        {
            if (itemsController is null || UserTunnels is null) return;

            itemsController.Dispatcher.Invoke(delegate
            {
                foreach (var tunnel in UserTunnels)
                {
                    bool? flag = default;
                    if (tunnelId.Contains(tunnel.Id)) flag = true;
                    if (tunnelId.Contains(-tunnel.Id)) flag = false;
                    if (flag is null) continue;
                    { 
                        var vi = itemsController.ItemContainerGenerator.ContainerFromItem(tunnel);
                        if (vi is ContentPresenter cp && cp.ContentTemplate?.FindName("userTunnel", cp) is Controls.UserTunnel userTunnel)
                        {
                            userTunnel.ToggleStateTo(flag.Value,force:true);



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

                VisualStateManager.GoToElementState(container, LoadingState, false);

                event_RefreshUserTunnelCommand.Execute(null);

                page.Unloaded += delegate
                {
                    event_RefreshUserTunnelCommand.Cancel();

                    _ = firstStateWaiter?.TrySetCanceled();

                    _ = _writer?.CompleteAsync();

                    conve_CreateStreamCommand.Cancel();

                    disposableStream?.Dispose();
                };
            }
        }

        [ObservableProperty]
        private Model.ExecuteResult? executeResult;

        [ObservableProperty]
        private ObservableCollection<object> preloadSkeletons = new ObservableCollection<object> { };

        [ObservableProperty]
        private ObservableCollection<Model.UserTunnel> userTunnels = new ObservableCollection<Model.UserTunnel> { };

        [RelayCommand]
        private async Task @event_DisplayException()
        {
            if (ExecuteResult is { HasException: true, Exception: not null and Exception ex })
            {
                var dialog = new Controls.ErrorContentDialog
                {
                    Exception = ex
                };
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

        }

        [RelayCommand]
        private async Task @event_DeleteUserTunnel(Model.UserTunnel tunnel)
        {
            var resp = await Service.Net.OpenFrpApi.RemoveTunnel(tunnel.Id);
            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { Flag: true })
            {
                UserTunnels.Remove(tunnel);
            }
            else
            {
                _mainWindowViewModel?.ShowAlert($"隧道 {tunnel.Name} 删除失败", 
                    message: resp.Data?.Message ?? resp.Message ?? resp.Exception?.Message ?? "请重试删除......", InfoBarSeverity.Error);
            }
        }

        //[RelayCommand]
        //private async Task @event_UserContextMenuInvoked(string action)
        //{
            
        //}

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task @event_TunnelSwitchInvoked(ToggleSwitch @switch)
        {
            if (@switch.DataContext is Model.UserTunnel tunnel && _writer != null)
            {
                await _writer.WriteAsync(new Service.Proto.Request.TunnelStreamRequest
                {
                    State = @switch.IsOn ?
                     Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.LaunchTunnel :
                     Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.CloseTunnel,
                    Data = Any.Pack(new BytesValue()
                    {
                        Value = Google.Protobuf.ByteString.CopyFrom(tunnel.GetTunnelJsonBuffer())
                    })
                });
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshUserTunnel(CancellationToken cancellationToken)
        {
            if (firstStateWaiter is null)
            {
                firstStateWaiter = new TaskCompletionSource<int[]>();
                if (_writer is not null)
                {
                    await _writer.WriteAsync(new Service.Proto.Request.TunnelStreamRequest
                    {
                        State = Service.Proto.Request.TunnelStreamRequest.Types.TunnelStreamRequestState.GetOnlineTunnel
                    });
                }
            }
            

            VisualStateManager.GoToElementState(container, LoadingState, false);

            CreatePreloadSkeleton();

            await Task.Delay(1000,cancellationToken);

            if (!true)
            {
                UserTunnels = new ObservableCollection<Model.UserTunnel>();

                var onlineTunnels = await firstStateWaiter.Task.WaitAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;

                for (global::System.Int32 i = 0; i < 4; i++)
                {
                    var tun = new Model.UserTunnel(1444+i) { };
                    if (onlineTunnels.Contains(tun.Id))
                    {
                        tun.FirstState = true;
                    }
                    UserTunnels.Add(tun);

                    await Task.Delay(75, cancellationToken);
                }

                VisualStateManager.GoToElementState(container, NormalState, false);

                firstStateWaiter = null;
                return;
            }

            var resp = await Service.Net.OpenFrpApi.GetUserTunnels(cancellationToken);

            if (!this.UpdateState(resp, () => resp.Data is { List: not null }))
            {
                VisualStateManager.GoToElementState(container, ErrorState, false);
            }
            else if (resp.Data!.Total is 0)
            {
                VisualStateManager.GoToElementState(container, EmptyState, false);
            }
            else
            {
                var onlineTunnels = await firstStateWaiter.Task.WaitAsync(cancellationToken);

                if (onlineTunnels is null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                UserTunnels = new ObservableCollection<Model.UserTunnel>();

                VisualStateManager.GoToElementState(container, NormalState, false);

                foreach (var tunnel in resp.Data.List!)
                {
                    var v = new Model.UserTunnel(tunnel);
                    if (onlineTunnels.Contains(tunnel.Id))
                    {
                        v.FirstState = true;
                    }
                    UserTunnels.Add(v);

                    await Task.Delay(75, cancellationToken);
                }
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
