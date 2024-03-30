using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class LoginDialogVIewModel : ObservableObject
    {
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        [ObservableProperty]
        private string? username;

        [ObservableProperty]
        private string? password;

        [ObservableProperty]
        private string? reason = string.Empty;

        [ObservableProperty]
        private Exception? exception;

        public TaskCompletionSource<Awe.Model.OpenFrp.Response.Data.UserInfo>? taskCompletionSource;

        [RelayCommand]
        private void @event_DialogLoaded(RoutedEventArgs e)
        {
            if (e.Source is Dialog.LoginDialog dialog)
            {
                dialog.PrimaryButtonClick += @event_Dialog_PrimaryButtonClick;
                
                taskCompletionSource = dialog.TaskCompletionSource;
            }
        }

        private async void @event_Dialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            await event_LoginCommand.ExecuteAsync(default);

            
            if (string.IsNullOrEmpty(Reason) && taskCompletionSource != null)
            {
                if (await taskCompletionSource.Task is { } info)
                {
                    WeakReferenceMessenger.Default.Send(info);
                }
            }
        }

        [RelayCommand]
        private async Task @event_CloseDialog()
        {
            if (taskCompletionSource is { Task.IsCompleted: false})
            {
                CancellationTokenSource.Cancel();
                Service.Net.OpenFrp.Logout();
                if (Reason is "用户标识符不匹配" || Reason?.Contains("后台") is true)
                {
                    await Service.Net.OAuthApp.Logout().ConfigureAwait(false);
                }
                taskCompletionSource?.TrySetCanceled(CancellationTokenSource.Token);
            }
        }

        private Task @event_LoginDelay() => Task.Delay(200);

        [RelayCommand]
        private async Task @event_Login()
        {
            Reason = string.Empty;
            Exception = default;

            var oauthLogin = await OpenFrp.Service.Net.OAuthApp.Login(Username, Password, CancellationTokenSource.Token);
            if (!event_UploadState(oauthLogin)){ await @event_LoginDelay(); return; }

            var oauthAuthorize = await OpenFrp.Service.Net.OAuthApp.AuthorizeOpenFrp(CancellationTokenSource.Token);
            if (!event_UploadState(oauthAuthorize,() => oauthAuthorize.Data is not null && oauthAuthorize.Data.Code.IsNotNullOrEmpty())) { await @event_LoginDelay(); return; }

            var openfrpLogin = await OpenFrp.Service.Net.OpenFrp.Login(oauthAuthorize.Data!.Code!,CancellationTokenSource.Token);
            if (!event_UploadState(openfrpLogin)) { await @event_LoginDelay(); return; }

            var openfrpUserinfo = await OpenFrp.Service.Net.OpenFrp.GetUserInfo(CancellationTokenSource.Token);
            if (!event_UploadState(openfrpUserinfo, () => openfrpUserinfo.Data is not null)) { await @event_LoginDelay(); return; }
            else if (openfrpUserinfo.Data is { } userInfo)
            {
                var rrpc = await RpcManager.LoginAsync(new Service.Proto.Request.LoginRequest
                {
                    UserToken = userInfo.UserToken,
                    UserTag = $"@!{userInfo.UserID}+{userInfo.UserName}"
                }, TimeSpan.FromSeconds(10), cancellationToken: CancellationTokenSource.Token);

                if (rrpc.IsSuccess)
                {
                    Properties.Settings.Default.UserPwn = System.Text.Json.JsonSerializer.Serialize(new Awe.Model.OAuth.Request.LoginRequest
                    {
                        Username = Username,
                        Password = Launcher.PndCodec.EncryptString(Encoding.UTF8.GetBytes(Password))
                    });
                    RpcManager.UserSecureCode = rrpc.Data;

                    taskCompletionSource?.TrySetResult(userInfo);

                    //Service.Net.OpenFrp.Logout();
                    return;
                }
                else
                {
                    Reason = rrpc.Message ?? "发生了未知错误";
                    Exception = rrpc.Exception;
                }
                Service.Net.OpenFrp.Logout();
            }
        }

        private bool @event_UploadState(Awe.Model.ApiResponse resp,Func<bool>? func = default)
        {
            if (resp.Exception is null && resp.StatusCode is System.Net.HttpStatusCode.OK &&
                (func is null || func()))
            {
                return true;
            }
            else
            {
                Exception = resp.Exception;
                Reason = resp.Message?.Replace("bad request,",string.Empty) ?? resp.StatusCode.ToString();
            }
            return false;
        }

        [RelayCommand]
        private void @event_DisplayError()
        {
            Debug.WriteLine(Exception);
            MessageBox.Show(Exception?.ToString(), "OpenFRP Launcher", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
    }
}
