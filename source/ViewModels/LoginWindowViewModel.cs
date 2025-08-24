using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OpenFrp.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern;
using System.Security.Policy;
using System.Threading;
using OpenFrp.Service.Proto.Response;
using OpenFrp.Service.Net;
using System.Net;
using CommunityToolkit.Mvvm.Messaging;
using OpenFrp.Launcher.Controls;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Media;
using iNKORE.UI.WPF.Helpers;
using System.IO;
using Google.Protobuf.WellKnownTypes;
using OpenFrp.Service.Proto.Request;
using Grpc.Core;
using System.IO.Pipes;
using System.Runtime.ConstrainedExecution;
using System.Security.AccessControl;
using System.Security.Principal;
using GrpcDotNetNamedPipes;
using OpenFrp.Launcher.Model;
using Microsoft.Extensions.Logging;
using static Google.Rpc.Context.AttributeContext.Types;
using OpenFrp.Launcher.Rpc;
using Google.Api;
using static Google.Protobuf.WellKnownTypes.Field.Types;
using Microsoft.Extensions.DependencyInjection;



namespace OpenFrp.Launcher.ViewModels
{
    internal partial class LoginWindowViewModel : ObservableObject, IHrViewModel
    {
        // 1. 构造函数
        public LoginWindowViewModel()
        {
            CallbackAction = (state) =>
            {
                if (window != null)
                {
                    if (event_WebLogin2Command.IsRunning && state is LoginState)
                    {
                        event_WebLogin2Command.Cancel();
                    }
                    if (event_WebLoginCommand.IsRunning && state is LoginState)
                    {
                        event_WebLoginCommand.Cancel();
                    }
                    if (event_FastLoginCommand.IsRunning && state is LoginState)
                    {
                        event_FastLoginCommand.Cancel();
                    }
                    if (window.FindName("defaultUpdateCtrl") is Border { IsVisible: true })
                    {
                        if (conve_TryDetectFrpcCommand.IsRunning && state is LoginState)
                        {
                            conve_TryDetectFrpcCommand.Cancel();


                            App.StartupArguments.Remove("--updateFrpClient");

                        }
                        window.Title = "OpenFRP 启动器 - 登录";
                    }

                    VisualStateManager.GoToElementState(window, state, false);
                }
            };
            WeakReferenceMessenger.Default.UnregisterAll(nameof(LoginWindowViewModel));

            WeakReferenceMessenger.Default.Register<Model.RouteMessage<LoginWindowViewModel, string>>(nameof(LoginWindowViewModel), (_, message) =>
            {
                switch (message)
                {
                    case "processExit":
                        {
                            if (!CanCancelLogin)
                            {
                                conve_RelayPrepareCommand.Cancel();
                            }
                        };break;
                    case "processLfec":
                        {
                            if (event_GotoMainWindowCommand.IsRunning ||event_FastLoginCommand.IsRunning || event_WebLogin2Command.IsRunning || event_WebLoginCommand.IsRunning)
                            {
                                if (!CanCancelLogin)
                                {
                                    App.Current.Dispatcher.Invoke(() => conve_RelayPrepareCommand.Execute(default));
                                }
                            }
                        }; break;
                }
            });
            WeakReferenceMessenger.Default.Register<Model.RouteMessage<LoginWindowViewModel, Yue3.Model.Result.HttpResponse>>(nameof(LoginWindowViewModel), (_, message) =>
            {
                if(message.Data is { } result)
                {
                    this.UpdateState(result);
                }
            });

            Helpers.UsrTokenService.RefreshPlatformUsers();
            OnPropertyChanged(nameof(PlatformUsers));

            Logger = App.ServiceProvider.GetRequiredService<ILogger<LoginWindowViewModel>>();

            RpcManager = App.ServiceProvider.GetRequiredService<RpcManager>();
            DaemonManager = App.ServiceProvider.GetRequiredService<DaemonManager>();
            FrpcManager = App.ServiceProvider.GetRequiredService<Service.Manager.Frpc.FrpcManager>();

        }

        // 2. 常量、字段、属性（依次为 internal const, private, public, [ObservableProperty]）
        internal const string LoadingState = "DisplayLoadingCtrl";
        internal const string QrCodeFVState = "DisplayQrCodeFvCtrl";
        internal const string WebOAuthState = "DisplayWebOAuthCtrl";
        internal const string SettingState = "DisplaySettingCtrl";
        internal const string UpdateCtrl = "DisplayUpdateCtrl";
        internal const string LoginState = "DisplayLoginCtrl";
        internal const string CaptchaWebViewDisplayState = "DisplayCaptchaWebView";
        internal const string CaptchaWebViewHiddenState = "HiddenCaptchaWebView";
        internal const string DisplayDownloadingInfobar = "DisplayDownloadingInfobar";
        internal const string DisplayNewUpdateInfobar = "DisplayNewUpdateInfobar";
        internal const string DisplayErrorInfobar = "DisplayErrorInfobar";
        internal const string HiddenFrpcCtrl = "HiddenFrpcCtrl";
        internal readonly Action<string> CallbackAction;

        private LoginWindow? window;

        private DaemonManager DaemonManager { get; set; }
        private RpcManager RpcManager { get; set; }
        private Service.Manager.Frpc.FrpcManager FrpcManager { get; set; }
        private ILogger<LoginWindowViewModel> Logger { get; set; } 

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(event_CancelLoginCommand))]
        private bool canCancelLogin = true;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(IsExceptionInfobarOpen), nameof(HttpResponseMessage))]
        private Model.ExecuteResult? executeResult;

        [ObservableProperty]
        private string? updateLog = "新版本 FRPC 具有更好的性能以及安全特性，请及时更新。";

        [ObservableProperty]
        private string? httpResponseMessage;

        public ObservableCollection<Model.PlatformUser> PlatformUsers => Helpers.UsrTokenService.PlatformUserCache;
        public bool HasOwnerWindow => window is { Owner: not null };
        public bool IsExceptionInfobarOpen
        {
            get => ExecuteResult is not null;
            set { if (!value) { ExecuteResult = null; } }
        }
        public bool ShowTitlebarBackground => App.Settings.ShowTitlebarBackground;
        public void OnShowTitlebarBackgroundChanged() => OnPropertyChanged(nameof(ShowTitlebarBackground));
        private bool _loaded = false;

        // 3. 生命周期/初始化相关
        [RelayCommand]
        private async Task @event_MainWindowLoaded(LoginWindow wind)
        {
            if (_loaded) { return; }
            else { _loaded = true; }
            window = wind;

            if (App.StartupArguments.Contains("--minimize") && window.Owner is not MainWindow)
            {
                wind.HideByHANDLE();
            }


            wind.Closing += Wind_Closing;

            await Task.Delay(700);

            wind.TaskbarItemInfo ??= new System.Windows.Shell.TaskbarItemInfo { };

            

            if (wind.FindName("titelRender") is TextBlock tb)
            {

            }
            if (!window.UserInfoCallback.Task.IsCanceled)
            {

                if (wind.FindName("acrylicPanel") is iNKORE.UI.WPF.Modern.Controls.AcrylicPanel panel &&
                    wind.FindName("acrylicPanel2") is iNKORE.UI.WPF.Modern.Controls.AcrylicPanel panel2 &&
                    wind.FindName("acrylicPanel3") is iNKORE.UI.WPF.Modern.Controls.AcrylicPanel panel3 &&
                    wind.FindName("acrylicPanel4") is iNKORE.UI.WPF.Modern.Controls.AcrylicPanel panel4 &&
                    wind.FindName("background") is FrameworkElement fe)
                {
                    try
                    {
                        panel.Target = fe;
                        panel2.Target = fe;
                        panel3.Target = fe;
                        panel4.Target = fe;
                    }
                    catch
                    {
                        window.UserInfoCallback.TrySetCanceled();

                        return;
                    }
                }
            }

            
            if (string.IsNullOrEmpty(App.Settings.AutoLoginId))
            {
                if (window != null)
                {
                    if (App.StartupArguments.Contains("--updateFrpClient"))
                    {
                        ToggleToUpdateCtrl();
                    }
                    else
                    {
                        VisualStateManager.GoToElementState(window, LoginState, false);
                        wind.Title = "OpenFRP 启动器 - 登录";
                    }
                    VisualStateManager.GoToElementState(window, HiddenFrpcCtrl, false);
                }
                if (wind.FindName("selfor") is ItemsControl isc)
                {
                    for (global::System.Int32 i = 0; i < isc.ItemContainerGenerator.Items.Count; i++)
                    {
                        if (isc.ItemContainerGenerator.Items[i] is not Model.PlatformUser { UserAvatorHash: not null or "" } platUser) continue;
                        if (isc.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter cp && cp.ContentTemplate.FindName("picpic", cp) is iNKORE.UI.WPF.Modern.Controls.PersonPicture picpic)
                        {
                            //btn.FindName("picpic") is iNKORE.UI.WPF.Modern.Controls.PersonPicture picpic
                            if (System.IO.File.Exists(platUser.UserAvatorHash) && Uri.TryCreate(platUser.UserAvatorHash, UriKind.RelativeOrAbsolute, out var iiccv))
                            {
                                var bmp = new BitmapImage();

                                picpic.Dispatcher.UnhandledException += (_, e) =>
                                {
                                    if (e.Exception is BadImageFormatException or NotSupportedException)
                                    {
                                        e.Handled = true;
                                    }
                                };
                                picpic.Dispatcher.Invoke(() =>
                                {
                                    bmp.BeginInit();
                                    bmp.UriSource = iiccv;
                                    bmp.EndInit();
                                    bmp.Freeze();

                                    picpic.ProfilePicture = bmp;
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                var usr = Helpers.UsrTokenService.GetUserFromAutoLoginId(App.Settings.AutoLoginId);
                if (usr is not null)
                {
                    event_FastLoginCommand.Execute(usr);
                }
            }

            UpdateImageOpacity(ThemeManager.GetActualTheme(wind));

            //_ = event_DisplayCaptchaWebViewControlCommand.ExecuteAsync("False");

            wind.AddHandler(ThemeManager.ActualThemeChangedEvent, new RoutedEventHandler(delegate 
            {
                UpdateImageOpacity(ThemeManager.GetActualTheme(wind));
            }));

            wind.IsHitTestVisible = true;

            conve_TryDetectFrpcCommand.Execute(default);
        }

        // 4. 事件/命令（RelayCommand 标记的方法，按界面交互流程排序）
        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_RelayPrepare(CancellationToken cancellationToken)
        {
            if (OpenFrpApi.GetAuthorization() is not null)
            {
                var userInfo = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

                if (!this.UpdateState(userInfo, () => userInfo.Data is not null)) return;

                await PrepareForApp(userInfo.Data!, cancellationToken);
            }
            else
            {
                await event_GotoMainWindowCommand.ExecuteAsync(cancellationToken);
            }
        }

        private void Wind_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            

            if (window is { Owner: MainWindow mw })
            {
                window.UserInfoCallback.TrySetCanceled();
                mw.Activate();
                event_CancelLogin();
            }
            else
            {
                e.Cancel = true;

                window?.HideByHANDLE();
            }
        }

        [RelayCommand]
        private void @event_DisplaySettingControl()
        {
            if (window is null) return;
            if (window.FindName("defaultSettingCtrl") is Border br)
            {
                VisualStateManager.GoToElementState(window, SettingState, false);

                if (br.Child is null)
                {
                    if (window.Owner is MainWindow { FrameContentViewModel: ViewModels.SettingsViewModel svm })
                    {
                        br.Child = new Views.LoginWindow.Settings(svm, CallbackAction);
                    }
                    else
                    {
                        br.Child = new Views.LoginWindow.Settings(CallbackAction);
                    }
                }
            }
        }

        [RelayCommand]
        private void @event_DisplayLoadingControl(string booleanStr)
        {
            if (window is null || !bool.TryParse(booleanStr, out var flag)) return;

            VisualStateManager.GoToElementState(window, flag ? LoadingState : LoginState, false);
        }

        /// <summary>
        /// OAuth 网页授权登录
        /// </summary>
        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_WebLogin(CancellationToken cancellationToken)
        {
            bool wentLoading = false;
            if (window is null) return;
            try
            {
                this.ClearExecuteResult();

                window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;

                if (window.FindName("defaultWebOAuthCtrl") is not Border br) return;

                VisualStateManager.GoToElementState(window, WebOAuthState, false);

                Views.LoginWindow.OAuthLoginView webOAuth;

                if (br.Child is null)
                {
                    br.Child = webOAuth = new Views.LoginWindow.OAuthLoginView(CallbackAction);
                }
                else if (br.Child is Views.LoginWindow.OAuthLoginView { DataContext: ViewModels.OAuthLoginViewModel lvw } otk)
                {
                    webOAuth = otk;
                    lvw.event_RefreshLinkCommand.Execute(null);
                }
                else
                {
                    return;
                }
    
                string? code = await webOAuth.WaitForFinish(cancellationToken);
                if (string.IsNullOrEmpty(code)) return;

                VisualStateManager.GoToElementState(window, LoadingState, false);

                wentLoading = true;

                VisualStateManager.GoToElementState(window, HiddenFrpcCtrl, false);

                string[] sp = code!.Split('^');

                if (sp.Length != 2) return;

                var oauthTp = await OpenFrp.Service.Net.OpenFrpApi.Login(sp[0], sp[1], cancellationToken);

                if (!this.UpdateState(oauthTp)) return;

                var userInfo = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

                if (!this.UpdateState(userInfo, () => userInfo.Data is not null)) return;

                await PrepareForApp(userInfo.Data!, cancellationToken);
            }
            finally
            {
                if (wentLoading)
                {
                    VisualStateManager.GoToElementState(window, LoginState, false);
                }
                if (window is not null)
                {
                    window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
            }
        }

        /// <summary>
        /// 网页面板快速登录
        /// </summary>
        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_WebLogin2(CancellationToken cancellationToken)
        {
            if (window is null) return;

            bool wentLoading = false;

            try
            {
                window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;

                if (window.FindName("defaultQrCodeFvCtrl") is not Border br) return;
                
                VisualStateManager.GoToElementState(window, QrCodeFVState, false);

                Views.LoginWindow.QrCodeFV qrCodeFV;
                if (br.Child is null)
                {
                    br.Child = qrCodeFV = new Views.LoginWindow.QrCodeFV(CallbackAction);
                }
                else if (br.Child is Views.LoginWindow.QrCodeFV { DataContext: ViewModels.QrCodeFVViewModel qrFvvm } _c)
                {
                    qrFvvm.event_RequestUpdateCommand.Execute(null);

                    qrCodeFV = _c;
                }
                else
                {
                    throw new NullReferenceException();
                }
                string? code = await qrCodeFV.WaitForFinish(cancellationToken);

                if (string.IsNullOrEmpty(code)) return;

                VisualStateManager.GoToElementState(window, LoadingState, false);

                wentLoading = true;

                VisualStateManager.GoToElementState(window, HiddenFrpcCtrl, false);

                await Task.Delay(1500,cancellationToken);

                Service.Net.OpenFrpApi.SetAuthorization(code);

                var userInfo = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

                if (!this.UpdateState(userInfo, () => userInfo.Data is not null)) return;

                await PrepareForApp(userInfo.Data!, cancellationToken);
            }
            finally
            {
                if (wentLoading)
                {
                    VisualStateManager.GoToElementState(window, LoginState, false);
                }

                if (window is not null)
                {
                    window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_FastLogin(Model.PlatformUser usr,CancellationToken cancellationToken)
        {
            try
            {
             
                if (string.IsNullOrEmpty(usr.UserAuthorzation)) return;

                if (window is not null)
                {
                    window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                }

                IsExceptionInfobarOpen = false;

                VisualStateManager.GoToElementState(window, HiddenFrpcCtrl, false);

                Service.Net.OpenFrpApi.SetAuthorization(usr.UserAuthorzation);

                // 绕过 Web OAuth 界面，直接调用 API
                var userInfo = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

                if (!this.UpdateState(userInfo, () => userInfo.Data is not null))
                {
                    await Task.Delay(500, cancellationToken);
                    return;
                }

                await PrepareForApp(userInfo.Data!, cancellationToken);
            }
            finally
            {
                VisualStateManager.GoToElementState(window, LoginState, false);

                if (window is not null)
                {
                    window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCancelLogin))]
        private void @event_CancelLogin()
        {

            event_WebLoginCommand.Cancel();
            event_FastLoginCommand.Cancel();
            event_WebLogin2Command.Cancel();
            
        }

        private void ToggleToUpdateCtrl()
        {
            if (window is null) return;

            if (window.FindName("defaultUpdateCtrl") is Border defaultUpdateCtrl)
            {
                if (defaultUpdateCtrl.Child is Views.LoginWindow.UpdateCtrl)
                {

                }
                else
                {
                    defaultUpdateCtrl.Child ??= new Views.LoginWindow.UpdateCtrl(CallbackAction) { };
                }
            }
            VisualStateManager.GoToElementState(window, UpdateCtrl, false);
            window.Title = "OpenFRP 启动器 - 更新";
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_GotoMainWindow(CancellationToken cancellationToken)
        {
            if (window is null) return;
            
            this.ClearExecuteResult();

            if (window is not null)
            {
                window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            }

            VisualStateManager.GoToElementState(window, LoadingState, false);

            try
            {
                await NOCGotoMainWindow(cancellationToken);
            }
            finally
            {
                VisualStateManager.GoToElementState(window, LoginState, false);
                if (window is not null)
                {
                    window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
            }
        }

        private async Task NOCGotoMainWindow(CancellationToken cancellationToken)
        {
            if (!await RetryCheckFrpcFile(cancellationToken))
            {
                VisualStateManager.GoToElementState(window, LoginState, false);
                return;
            }

            CanCancelLogin = false;

            var rrfe = await TryGetFrpcVersionString();

            if (rrfe.HasException)
            {
                CanCancelLogin = true;
                this.ExecuteResult = rrfe;

                VisualStateManager.GoToElementState(window, LoginState, false);
                return;
            }

            if (await DaemonManager.LaunchDaemonAsync() is { HasException: true } flagV1)
            {
                CanCancelLogin = false;
                this.ExecuteResult = flagV1;
                return;
            }
            else CanCancelLogin = DaemonManager.DaemonService is not null;

            if (await DaemonManager.WaitForConfigureAsync(cancellationToken) is { HasException: true } flagV2)
            {
                CanCancelLogin = true;
                this.ExecuteResult = flagV2;
                return;
            }


            CanCancelLogin = false;

            if (cancellationToken.IsCancellationRequested) return;

            OpenFrp.Service.Proto.RpcResponse<SyncResponse>? r1_resp = default;

            for (int i = 0; i < 5; i++)
            {
                if (!RpcManager.IsConfigured)
                {
                    RpcManager.Configure();
                }
                r1_resp = await RpcManager.Sync(cancellationToken);
                if (r1_resp is { Flag: true})
                {
                    break;
                }
                await Task.Delay(250, cancellationToken);
            }

            if (!this.UpdateState(r1_resp)) { CanCancelLogin = true; return; }

            if (r1_resp is not { Data: SyncResponse srp_sr })
            {
                throw new NullReferenceException(nameof(r1_resp));
            }

            if (window is not null)
            {
                window.Closing -= Wind_Closing;


                GC.Collect();

                App.Settings.Save();

                if (window.Owner is MainWindow mw)
                {
                    window.Close();

                    Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processOnline");

                    window.UserInfoCallback.TrySetCanceled(cancellationToken);

                    TryDisposeWebHost();
                    mw.Activate();

                    event_CancelLogin();
                }
                else if (App.Current.MainWindow is not MainWindow)
                {
                    await AskForUrlSchemeToolsAsync(cancellationToken);

                    window.Close();
                    // MainWindow mainWindow = string.IsNullOrEmpty(launchArg) ? new MainWindow(true) : new MainWindow(launchArg);
                    MainWindow mainWindow = new MainWindow(true);

                    mainWindow.Show();
                }
            }
        }

        [RelayCommand]
        private async Task @event_DisplayException()
        {
            if (ExecuteResult is { HasException: true,Exception: not null and Exception ex })
            {
                if (App.Current is { MainWindow: var mw } && ContentDialog.GetOpenDialog(mw) is ContentDialog da)
                {
                    da?.Hide();
                }
                var dialog = new Dialogs.ErrorContentDialog
                {

                };

                dialog.SetValue(Controls.ErrorViewer.ExceptionProperty, ex);
                await dialog.ShowAsync();
            }
        }

        private async Task AskForUrlSchemeToolsAsync(CancellationToken cancellationToken = default)
        {
            if (OSVersionHelper.IsWindows7OrGreater && !OSVersionHelper.IsWindows8OrGreater)
            {
                return;
            }
            if (string.IsNullOrEmpty(App.Settings.AutoLoginId) && !App.Settings.DoNotAskMeForUrlSchemeTools)
            {
                if (Microsoft.Win32.Registry.ClassesRoot.GetSubKeyNames().Contains("openfrp"))
                {
                    return;
                }
                var dialog = new Dialogs.AskForUrlScehmeToolsDialog
                {

                };
                if (await dialog.ShowAsync().WhenAnyTime(cancellationToken) is ContentDialogResult.Primary)
                {
                    var cpc = new ProcessStartInfo
                    {
                        FileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenFrp.Service.exe"),
                        Arguments = "--inst -type=reg True",
                        ErrorDialog = false,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                    };
                    if (!App.IsAdministrator())
                    {
                        cpc.Verb = "runas";
                    }

                    try
                    {
                        await Task.Run(() => Process.Start(cpc));
                    }
                    catch
                    {

                    }

                    await Task.Delay(500);
                }
                else
                {
                    App.Settings.DoNotAskMeForUrlSchemeTools = true;
                }
                
            }
        }

        [RelayCommand]
        private void @event_RemoveUserRecord(Model.PlatformUser usr)
        {
            Helpers.UsrTokenService.RemoveUser(usr, true);
        }

        private Regex FrpcVersionRegex = FrpcVersionRegexFun();

        private async Task<Model.ExecuteResult> TryGetFrpcVersionString(string? fp = default)
        {
            if (string.IsNullOrEmpty(fp))
            {
                if (!OpenFrp.Service.Helpers.FileHelper.TryGetFRPClient(out fp))
                {
                    return new Model.ExecuteResult()
                    {
                        Exception = new System.IO.FileNotFoundException(fp),
                        Message = "FRPC 文件丢失，是否进行下载操作？"
                    };
                }
            }
            Exception? exc = default;
            if (await FrpcManager.DetectFrpcVersionAndFeatrue(fp,ex => exc = ex))
            {
                VisualStateManager.GoToElementState(window, HiddenFrpcCtrl, false);
                return Model.ExecuteResult.Success();
            }
            
            return exc ?? new InvalidOperationException("操作失败，未知启动原因。");
        }

        [RelayCommand]
        private void @event_OpenHelpLinkInWeb()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    FileName = "cmd",
                    Arguments = $"/c start https://docs.openfrp.net/use/desktop-launcher"
                });
                return;
            }
            catch { }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = "https://docs.openfrp.net/use/desktop-launcher"
                });
                return;
            }
            catch { }
        }

        private async Task PrepareForApp(Yue3.Model.OpenFrp.Response.Data.UserInfoData userInfo,CancellationToken cancellationToken = default)
        {
            var flag = await RetryCheckFrpcFile(cancellationToken);

            if (!flag)
            {
                VisualStateManager.GoToElementState(window, LoginState, false);
                return;
            }

            CanCancelLogin = false;
            
            if (await TryGetFrpcVersionString() is { HasException: true } rrfe)
            {
                CanCancelLogin = true;
                this.ExecuteResult = rrfe;
                return;
            }

            if (await DaemonManager.LaunchDaemonAsync() is { HasException: true} flagV1)
            {
                CanCancelLogin = false;
                this.ExecuteResult = flagV1;
                return;
            }
            else CanCancelLogin = DaemonManager.DaemonService is not null;

            if (await DaemonManager.WaitForConfigureAsync(cancellationToken) is { HasException: true } flagV2)
            {
                CanCancelLogin = true;
                this.ExecuteResult = flagV2;
                return;
            }

//            RpcManager.Configure();

            CanCancelLogin = false;

            if (cancellationToken.IsCancellationRequested) return;

            OpenFrp.Service.Proto.RpcResponse<SyncResponse>? r1_resp = default;

            for (int i = 0; i < 5; i++)
            {
                if (!RpcManager.IsConfigured)
                {
                    RpcManager.Configure();
                }
                r1_resp = await RpcManager.Sync(cancellationToken);
                if (r1_resp is { Flag: true })
                {
                    break;
                }
                await Task.Delay(250, cancellationToken);
            }

           
            if (!this.UpdateState(r1_resp)) { CanCancelLogin = true; return; }

            if (r1_resp is not { Data: SyncResponse srp_sr })
            {
                throw new NullReferenceException(nameof(r1_resp));
            }
            if (srp_sr.IsLogon)
            {
                // todol
            }
            // UserId + RegTime + Email

            var r2_resp = await RpcManager.Login(new Service.Proto.Request.LoginRequest { UserInfomationJson = Google.Protobuf.ByteString.CopyFrom(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userInfo)) }, cancellationToken);
            if (!this.UpdateState(r2_resp)) { CanCancelLogin = true; return; }

            if (Service.Net.OpenFrpApi.GetAuthorization() is { Length: > 0} ap)
            {
                Helpers.UsrTokenService.AddUser(userInfo.UserName, userInfo.Email, ap, true);
            }
           

            if (window is not null)
            {
                bool foc = window.WindowState is WindowState.Normal;

                if (!foc && !App.StartupArguments.Contains("--minimize"))
                {
                    foc = true;
                }

                window.Closing -= Wind_Closing;
                

                GC.Collect();

                App.Settings.Save();

                if (window.Owner is MainWindow mw)
                {
                    window.Close();

                    Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processOnline");
                    TryDisposeWebHost();
                    window.UserInfoCallback.TrySetResult(userInfo);
                    mw.Activate();
                }
                else if (App.Current.MainWindow is not MainWindow)
                {
                    if (foc)
                    {
                        await AskForUrlSchemeToolsAsync(cancellationToken);
                    }

                    window.Close();

                    var mainWindow = new MainWindow(userInfo);

                    mainWindow.WindowState = foc ? WindowState.Normal : WindowState.Minimized;
                    mainWindow.Tag = foc;

                    mainWindow.Show();
                }
            }
        }

        // 6. 工具/私有方法
        private void UpdateImageOpacity(ElementTheme theme)
        {
            if (window is null) return;
            switch (theme)
            {
                case ElementTheme.Default: { /* ????? */ return; }
                case ElementTheme.Light:
                    {
                        VisualStateManager.GoToElementState(window, "DisplayLight", false);
                    }; break;
                case ElementTheme.Dark:
                    {
                        VisualStateManager.GoToElementState(window, "DisplayDark", false);
                    }; break;

            }
        }

        private void TryDisposeWebHost()
        {
            if (window is null) return;
            if (window.FindName("defaultWebOAuthCtrl") is not Border br) return;
            if (br.Child is Views.LoginWindow.OAuthLoginView { DataContext: ViewModels.OAuthLoginViewModel oavm })
            {
                oavm.DisposeWebHost();
            }
        }

        private Rpc.BgService? bgService;

        [ObservableProperty,NotifyCanExecuteChangedFor(nameof(event_TryLaunchBackgroundServiceCommand))]
        private Model.DownloadProcess downloadProcess = new Model.DownloadProcess { };

        private async Task<bool> RetryCheckFrpcFile(CancellationToken cancellationToken = default)
        {
            if (bgService is not null)
            {
                if (!event_TryLaunchBackgroundServiceCommand.IsRunning)
                {
                    if (cancellationToken != CancellationToken.None)
                    {
                        await event_TryLaunchBackgroundServiceCommand.ExecuteAsync(cancellationToken);
                    }
                    else
                    {
                        event_TryLaunchBackgroundServiceCommand.Execute(default);

                        if (event_TryLaunchBackgroundServiceCommand.ExecutionTask != null)
                        {
                            await event_TryLaunchBackgroundServiceCommand.ExecutionTask;
                        }
                    }
                }
                if (bgService?.WaitHandle != null)
                {
                    await Task.Run(() => bgService?.WaitHandle?.WaitOne()).WhenAnyTime(cancellationToken);

                    // false && false == true
                    // true && false == false;
                    return bgService is not null && !cancellationToken.IsCancellationRequested;
                }
            }
            else if (cancellationToken != CancellationToken.None)
            {
                await conve_TryDetectFrpcCommand.ExecuteAsync(cancellationToken);
            }
            else 
            { 
                conve_TryDetectFrpcCommand.Execute(default);

                if (conve_TryDetectFrpcCommand.ExecutionTask != null)
                {
                    await conve_TryDetectFrpcCommand.ExecutionTask;
                }
            }
            return cancellationToken.Equals(CancellationToken.None) || !cancellationToken.IsCancellationRequested ;
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_TryDetectFrpc(CancellationToken cancellationToken)
        {
            if (window is null) return;

            bool hasUpdateFrpClientArgument = App.StartupArguments.Contains("--updateFrpClient");

            if (OpenFrp.Service.Helpers.FileHelper.TryGetFRPClient(out string pf))
            {
                var result = await TryGetFrpcVersionString(pf);

                if (result.Exception != null)
                {
                    VisualStateManager.GoToElementState(window, DisplayErrorInfobar, false);
                    VisualStateManager.GoToElementState(window, LoginState, false);
                    return;
                }

                DownloadProcess = new Model.DownloadProcess { ProgressValue = 0, DownloadFileUrl = "寻找下载源..." };

                if (event_WebLoginCommand.IsRunning || event_WebLogin2Command.IsRunning || event_FastLoginCommand.IsRunning)
                {
                    return;
                }

                var config = await OpenFrpApi.GetSoftwareConfig(cancellationToken);

                if (config.StatusCode is not HttpStatusCode.OK || config.Data is not Yue3.Model.OpenFrp.Response.Data.SoftWareVersionData { Latest: not null, Launcher: not null } sv)
                {
                    DownloadProcess.DownloadFileUrl = "";
                    if (hasUpdateFrpClientArgument)
                    {
                        VisualStateManager.GoToElementState(window, LoginState, false);
                    }
                    // TODO
                    return;
                }

                if (FrpcManager.FrpcVersionString.Equals(sv.Latest, StringComparison.Ordinal))
                {
                    return;
                }
                else
                {
                    if (OSVersionHelper.IsWindows7OrGreater && !OSVersionHelper.IsWindows8OrGreater && FrpcManager.FrpcVersionString.Equals("OpenFRP_0.54.0_835276e2_20240205"))
                    {
                        DownloadProcess.DownloadFileUrl = "";

                        if (hasUpdateFrpClientArgument)
                        {
                            VisualStateManager.GoToElementState(window, LoginState, false);
                        }
                        return;
                    }

                    if (!hasUpdateFrpClientArgument)
                    {
                        UpdateLog = sv.CommonUpdateLog;
                        VisualStateManager.GoToElementState(window, DisplayNewUpdateInfobar, false);

                        DownloadProcess.DownloadFileUrl = "";

                        return;
                    }
                }
            }
  
            try
            {

                if (!hasUpdateFrpClientArgument)
                {
                    VisualStateManager.GoToElementState(window, DisplayDownloadingInfobar, false);
                }
                // non-detect

                bgService = new Rpc.BgService();

                bgService.DownloadServiceFallback += (type, data) =>
                {
                    switch (type)
                    {
                        case DownloadFallback.Types.DownloadFallbackType.Messaging:
                            {
                                if (data is null)
                                {
                                    return;
                                }
                                else if (data.TryUnpack<Google.Rpc.DebugInfo>(out var dbgInfo))
                                {
                                    Logger.LogError("Message: {msg}", dbgInfo.Detail);
                                }
                                else if (data.TryUnpack<StringValue>(out var sv) && !string.IsNullOrEmpty(sv.Value))
                                {
                                    switch (sv.Value)
                                    {
                                        case "finishDownload":
                                            {
                                                bgService?.WaitHandle.Set();
                                            }
                                            ; break;
                                        default:
                                            {
                                                if (sv.Value.StartsWith("err:"))
                                                {
                                                    DownloadProcess.DownloadFileUrl = sv.Value;

                                                    bgService?.WaitHandle.Set();

                                                    window.Dispatcher.Invoke(() =>
                                                    {
                                                        VisualStateManager.GoToElementState(window, DisplayErrorInfobar, false);
                                                    });
                                                }
                                                Logger.LogError("Message: {msg}", sv.Value);
                                            }
                                            ; break;
                                    }
                                }
                            }
                            ; break;
                        case DownloadFallback.Types.DownloadFallbackType.SwitchSource
                        when data.TryUnpack<StringValue>(out var stv) && !string.IsNullOrEmpty(stv.Value):
                            {
                                DownloadProcess.ProgressValue = 0;
                                DownloadProcess.DownloadFileUrl = stv.Value;
                            }
                            ; break;
                        case DownloadFallback.Types.DownloadFallbackType.ProgressValue
                        when data.TryUnpack<Value>(out var v) && v.HasNumberValue:
                            {
                                DownloadProcess.ProgressValue = v.NumberValue;
                            }
                            ; break;
                    }
                };

                DownloadProcess = new Model.DownloadProcess { ProgressValue = 0, DownloadFileUrl = "寻找下载源..." };

                bgService.LaunchServer();

                event_TryLaunchBackgroundServiceCommand.Execute(default);

                await Task.Run(() => bgService?.WaitHandle?.WaitOne()).WhenAnyTime(cancellationToken);

                bgService?.Dispose();

                if (cancellationToken.IsCancellationRequested)
                {
                    event_CancelLoginCommand.Execute(default);
                    event_GotoMainWindowCommand.Cancel();
                }
            }
            finally
            {
                bgService = default;
                if (!DownloadProcess.DownloadFileUrl.StartsWith("err:"))
                {
                    if (!hasUpdateFrpClientArgument)
                    {
                        VisualStateManager.GoToElementState(window, HiddenFrpcCtrl, false);
                    }
                    else
                    {
                        CallbackAction(LoginState);
                    }
                }
                DownloadProcess = new DownloadProcess { };
            }
            
        }



        private bool CanExecuteLaunchBackgroundService() => DownloadProcess.ProgressValue is 0 && bgService is not null;
            
        [RelayCommand(CanExecute = nameof(CanExecuteLaunchBackgroundService))]
        private async Task @event_TryLaunchBackgroundService()
        {
            if (bgService is null) return;

            try
            {
                DownloadProcess.DownloadFileUrl = "进程启动中...";
                DownloadProcess.ProgressBarShowError = false;

                await bgService.LaunchProcessAndWait();

                if (bgService is not null)
                {
                    DownloadProcess.ProgressBarShowError = false;
                    DownloadProcess.ProgressValue = 0;
                    DownloadProcess.DownloadFileUrl = "过程暂未完成，请点击重试继续操作。";
                }
            }
            catch(System.ComponentModel.Win32Exception ex)
            {
                DownloadProcess.ProgressBarShowError = true;
                DownloadProcess.DownloadFileUrl = ex.Message;
            }
            catch
            {

            }
        }

        [RelayCommand]
        private void @event_InstallFrpcUpdate()
        {
            VisualStateManager.GoToElementState(window, HiddenFrpcCtrl, false);

            if (event_WebLogin2Command.IsRunning || event_WebLoginCommand.IsRunning || event_FastLoginCommand.IsRunning || !CanCancelLogin)
            {
                return;
            }

            ToggleToUpdateCtrl();

            App.StartupArguments.Add("--updateFrpClient");

            conve_TryDetectFrpcCommand.Execute(default);
        }


        [RelayCommand]
        private void @event_GotoDesktopLauncherHelpPage()
        {
            try
            {
                Process.Start("https://docs.openfrp.net/use/desktop-launcher#%E5%8A%A0%E5%85%A5%E7%B3%BB%E7%BB%9F%E7%99%BD%E5%90%8D%E5%8D%95");
                return;
            }
            catch { }

            try
            {
                Process.Start("start","https://docs.openfrp.net/use/desktop-launcher#%E5%8A%A0%E5%85%A5%E7%B3%BB%E7%BB%9F%E7%99%BD%E5%90%8D%E5%8D%95");
            }
            catch { }
        }

        private bool CanExecuteCancelLogin() => CanCancelLogin;

#if NET
        [GeneratedRegex("O(\\D*?)F(\\D*?)_(\\d{0,2}).(\\d{0,3}).(\\d{0,2})")]
        private static partial Regex FrpcVersionRegexFun();
#else
        private static Regex FrpcVersionRegexFun() => new Regex("O(\\D*?)F(\\D*?)_(\\d{0,2}).(\\d{0,3}).(\\d{0,2})");
#endif
    }
}
