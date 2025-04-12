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
using Windows.System;
using CommunityToolkit.Mvvm.Messaging;



namespace OpenFrp.Launcher.ViewModels
{
    internal partial class LoginWindowViewModel : ObservableObject,IHrViewModel
    {
        public LoginWindowViewModel()
        {
            CallbackAction = (state) =>
            {
                if (window != null)
                {
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
                            App.LaunchRpcProcess();

                            if (event_FastLoginCommand.IsRunning || event_WebLogin2Command.IsRunning || event_WebLoginCommand.IsRunning)
                            {
                                if (!CanCancelLogin)
                                {
                                    App.Current.Dispatcher.Invoke(() => conve_RelayPrepareCommand.Execute(default));
                                }
                            }
                        }; break;
                }
            });
            try
            {
                var r1 = System.Text.Json.JsonSerializer.Deserialize<OpenFrp.Launcher.Properties.Settings.UserProperty[]>(App.Settings.Token);
                if (r1 is not null && r1.Select(x => new Model.PlatformUser { EmailAddress = x.Email, UserAuthorzation = x.Authorization, Username = x.User }) is { } col)
                {
                    PlatformUsers = new ObservableCollection<Model.PlatformUser>(col);

                    return;
                }
            }
            catch
            {

            }
            PlatformUsers = new ObservableCollection<Model.PlatformUser>()
            {

            };
            
        }

        internal const string LoadingState = "DisplayLoadingCtrl";
        internal const string QrCodeFVState = "DisplayQrCodeFvCtrl";
        internal const string SettingState = "DisplaySettingCtrl";
        internal const string LoginState = "DisplayLoginCtrl";

        internal readonly Action<string> CallbackAction;

        private LoginWindow? window;

        private OpenFrp.Service.Host.HttpServer? webLoginHttpServer;

        [ObservableProperty,NotifyCanExecuteChangedFor(nameof(event_CancelLoginCommand))]
        private bool canCancelLogin = true;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(event_LoginCommand))]
        private string? username;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(event_LoginCommand))]
        private string? password;

        [ObservableProperty,NotifyPropertyChangedFor(nameof(IsExceptionInfobarOpen),nameof(HttpResponseMessage))]
        private Model.ExecuteResult? executeResult;

        [ObservableProperty]
        private string? httpResponseMessage;

        [ObservableProperty]
        private ObservableCollection<Model.PlatformUser> platformUsers = new() { };

        public bool HasOwnerWindow
        {
            get => window is { Owner: not null };
        }

        public bool IsExceptionInfobarOpen
        {
            get => ExecuteResult is not null;
            set
            {
                if (!value) { ExecuteResult = null; }
            }
        }

        [RelayCommand]
        private async Task @event_MainWindowLoaded(LoginWindow wind)
        {
            window = wind;

            wind.Closing += Wind_Closing;

            await Task.Delay(700);

            

            wind.TaskbarItemInfo ??= new System.Windows.Shell.TaskbarItemInfo { };

            if (wind.FindName("acrylicPanel") is iNKORE.UI.WPF.Modern.Controls.AcrylicPanel panel &&
                wind.FindName("acrylicPanel2") is iNKORE.UI.WPF.Modern.Controls.AcrylicPanel panel2 &&
                wind.FindName("background") is FrameworkElement fe &&
                !window.UserInfoCallback.Task.IsCanceled)
            {
                try
                {
                    panel.Target = fe;
                    panel2.Target = fe;
                }
                catch
                {
                    window.UserInfoCallback.TrySetCanceled();
                    
                    return;
                }
            }

            if (window != null)
            {
                VisualStateManager.GoToElementState(window, LoginState, false);
            }
            

            UpdateImageOpacity(ThemeManager.GetActualTheme(wind));

            wind.AddHandler(ThemeManager.ActualThemeChangedEvent, new RoutedEventHandler(delegate 
            {
                UpdateImageOpacity(ThemeManager.GetActualTheme(wind));
            }));

            wind.IsHitTestVisible = true;
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_RelayPrepare(CancellationToken cancellationToken)
        {
            var userInfo = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

            if (!this.UpdateState(userInfo, () => userInfo.Data is not null)) return;

            await PrepareForApp(userInfo.Data!, cancellationToken);
        }

        private void Wind_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            App.ResetDaemonWaitHandle();

            if (window is { Owner: MainWindow mw })
            {
                window.UserInfoCallback.TrySetCanceled();
                mw.Activate();
                if (webLoginHttpServer is not null)
                {
                    webLoginHttpServer.StopListen();
                }
                event_CancelLogin();
            }
            else Application.Current.Shutdown();
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
        private void @event_DisplayLoadingControl(string xval)
        {
            if (window is null || !bool.TryParse(xval,out var flag)) return;

            VisualStateManager.GoToElementState(window, flag ? LoadingState : LoginState, false);
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_WebLogin(CancellationToken cancellationToken)
        {
            try
            {
                if (window is not null)
                {
                    window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                }

                

                string address = "";
                string local_Address = "";
                var task = new TaskCompletionSource<string>();

                webLoginHttpServer = new OpenFrp.Service.Host.HttpServer();
                webLoginHttpServer.ServiceAvailable += async (_, e) =>
                {
                    var oauthUrl = await OpenFrpApi.GetAuthorizeUrl(e, cancellationToken);

                    if (!this.UpdateState(oauthUrl))
                    {
                        webLoginHttpServer.StopListen();
                        return;
                    }
                    else
                    {
                        address = oauthUrl.Data!;
                    }
                    local_Address = $"http://localhost:{e}/oauth_callback";
                    //address = $"http://launcher.openfrp.net:{e}/oauth_callback";

                    try
                    {
                        _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = address,
                            UseShellExecute = true,
                            ErrorDialog = false
                        });
                    }
                    catch (Exception)
                    {

                    }
                };
                webLoginHttpServer.AcceptConnection += async (_, e) =>
                {
                    if (e.Request is { Url.LocalPath: "/oauth_callback", QueryString: var qr })
                    {
                        string? frCode = qr.GetValues("code")?.FirstOrDefault();
                        if (!string.IsNullOrEmpty(frCode))
                        {
                            task.TrySetResult(frCode!);

                            e.Response.Redirect("../app/index.html");
                        }
                        else if (!string.IsNullOrEmpty(address))
                        {
                            e.Response.Redirect(address);
                        }
                    }
                    else if (e.Request is { Url.Segments.Length: 3, Url.Segments: var seg } && seg[0] is "/" && seg[1] is "app/")
                    {
                        if (seg[2] is "close")
                        {
                            e.Response.Abort();
                            webLoginHttpServer.StopListen();

                            return;
                        }
                        try
                        {
                            var ctx = App.GetResourceStream(new Uri($"pack://application:,,,/Resources/OAuth/{seg[2]}"));

                            if (ctx is null or { Stream: null })
                            {
                                e.Response.StatusCode = 404;
                            }
                            else
                            {
                                e.Response.ContentType = Service.Host.HttpServer.GetContentType(seg[2]);
                                e.Response.ContentLength64 = ctx.Stream.Length;

                                await ctx.Stream.CopyToAsync(e.Response.OutputStream);
                                await ctx.Stream.FlushAsync();
                            }
                        }
                        catch
                        {
                            e.Response.StatusCode = 404;
                        }
                    }
                    e.Response.Close();
                };
                webLoginHttpServer.HandledException += (_, e) =>
                {
                    Exception ex = e.GetException();
                    // todo
                };
                webLoginHttpServer.ServiceCloseed += delegate
                {
                    if (!task.Task.IsCompleted) return;
                    task.TrySetCanceled();
                };

                webLoginHttpServer.StartListen(cancellationToken);

                string? code = await task.WaitTaskCompletionSource(cancellationToken);

                if (string.IsNullOrEmpty(code)) return;

                var oauthTp = await OpenFrp.Service.Net.OpenFrpApi.Login(code!, local_Address, cancellationToken);

                if (!this.UpdateState(oauthTp)) return;

                var userInfo = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

                if (!this.UpdateState(userInfo, () => userInfo.Data is not null)) return;

                await PrepareForApp(userInfo.Data!, cancellationToken);
            }
            finally
            {
                if (window is not null)
                {
                    window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
            }
        }

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
                    br.Child = qrCodeFV = new Views.LoginWindow.QrCodeFV((state) =>
                    {
                        if (br.Child is Views.LoginWindow.QrCodeFV { DataContext: ViewModels.QrCodeFVViewModel qrFvvm })
                        {
                            qrFvvm.event_RefreshLinkCommand.Cancel();
                            qrFvvm.event_RequestUpdateCommand.Cancel();
                            qrFvvm.conve_WaitForPollLoginCommand.Cancel();
                        }
                        event_WebLogin2Command.Cancel();

                        CallbackAction.Invoke(state);
                    });
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

        [RelayCommand(CanExecute = nameof(CanExecuteLogin),IncludeCancelCommand = true)]
        private async Task @event_Login(CancellationToken cancellationToken)
        {
            //try
            //{
            //    if (window is not null)
            //    {
            //        window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            //    }

            //    if (Username is null || Password is null) return;

            //    IsExceptionInfobarOpen = false;

            //    var oauthLogin = await OpenFrp.Service.Net.NatayarkAuth.Login(Username, Password, cancellationToken);

            //    if (!this.UpdateState(oauthLogin)) return;

            //    var oaoLogin = await OpenFrp.Service.Net.OpenFrpApi.AuthorizeOnce(cancellationToken);

            //    _ = await OpenFrp.Service.Net.NatayarkAuth.Logout(cancellationToken);

            //    if (!this.UpdateState(oaoLogin)) return;

            //    // 绕过 Web OAuth 界面，直接调用 API
            //    var userInfo = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

            //    if (!this.UpdateState(userInfo, () => userInfo.Data is not null)) return;

            //    await PrepareForApp(userInfo.Data!, cancellationToken);
            //}
            //finally
            //{
            //    if (window is not null)
            //    {
            //        window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            //    }
            //}
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

                Service.Net.OpenFrpApi.SetAuthorization(usr.UserAuthorzation);

                // 绕过 Web OAuth 界面，直接调用 API
                var userInfo = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

                if (!this.UpdateState(userInfo, () => userInfo.Data is not null)) return;

                await PrepareForApp(userInfo.Data!, cancellationToken);
            }
            finally
            {
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
            event_LoginCommand.Cancel();
            event_WebLogin2Command.Cancel();
        }

        [RelayCommand]
        private void @event_GotoMainWindow()
        {
            event_WebLoginCommand.Cancel();
        }

        [RelayCommand]
        private async Task @event_DisplayException()
        {
            if (ExecuteResult is { HasException: true,Exception: not null and Exception ex })
            {
                var dialog = new Controls.ErrorContentDialog
                {
                    Exception = ex
                };
                await dialog.ShowAsync();
            }
        }

        private async Task PrepareForApp(Yue3.Model.OpenFrp.Response.Data.UserInfoData userInfo,CancellationToken cancellationToken = default)
        {
            CanCancelLogin = false;

            App.LaunchRpcProcess(out var manager);

            await App.WaitForProcessLaunch(cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            OpenFrp.Service.Proto.RpcResponse<SyncResponse>? r1_resp = default;

            for (int i = 0; i < 5; i++)
            {
                r1_resp = await manager.Sync(cancellationToken);

                if (r1_resp.Flag)
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

            var r2_resp = await manager.Login(new Service.Proto.Request.LoginRequest { UserInfomationJson = Google.Protobuf.ByteString.CopyFrom(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userInfo)) }, cancellationToken);
            if (!this.UpdateState(r2_resp)) { CanCancelLogin = true; return; }

            if (Service.Net.OpenFrpApi.GetAuthorization() is { Length: > 0} ap)
            {
                bool hadAccount = false;
                for (global::System.Int32 i = 0; i < PlatformUsers.Count; i++)
                {
                    if (userInfo.Email.Equals(PlatformUsers[i].EmailAddress))
                    {
                        hadAccount = true;
                        PlatformUsers[i].UserAuthorzation = ap;
                        PlatformUsers[i].Username = userInfo.UserName;
                    }
                }
                if (!hadAccount)
                {
                    PlatformUsers.Add(new Model.PlatformUser
                    {
                        EmailAddress = userInfo.Email,
                        Username = userInfo.UserName,
                        UserAuthorzation = ap
                    });
                }
            }
            App.Settings.Token = System.Text.Json.JsonSerializer.Serialize(PlatformUsers.Select(x => new Properties.Settings.UserProperty { Authorization = x.UserAuthorzation, Email = x.EmailAddress, User = x.Username }));


            if (window is not null)
            {
                window.Closing -= Wind_Closing;
                window.Close();

                GC.Collect();

                App.Settings.Save();

                if (window.Owner is MainWindow mw)
                {
                    Model.RouteMessage<ViewModels.MainWindowViewModel>.Send("processOnline");

                    window.UserInfoCallback.TrySetResult(userInfo);
                    mw.Activate();
                }
                else
                {
                    var mainWindow = new MainWindow(userInfo);

                    mainWindow.Show();
                }
            }
        }

        /// <summary>
        /// Note: 在主题改变的时候，同时改变图片的透明度以适应 Mica / Acrylic 主题背景
        /// </summary>
        /// <param name="theme"></param>
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

        private bool CanExecuteLogin() => Username is not null && Password is not null;
        private bool CanExecuteCancelLogin() => CanCancelLogin;

        private void UpdateInfobarPlace()
        {
            if (window is null) return;
            if (window.FindName("gl_erBar") is iNKORE.UI.WPF.Modern.Controls.InfoBar infoBar)
            {
                infoBar.UpdateLayout();
            }
        }
    }
}
