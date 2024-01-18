using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class LoginDialogVIewModel : ObservableObject
    {
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        [ObservableProperty]
        private string? username = "loliwa";

        [ObservableProperty]
        private string? password = "/#/#123456789Abc";



        [ObservableProperty]
        private string? reason;

        private TaskCompletionSource<Awe.Model.OpenFrp.Response.Data.UserInfo>? fallbackTask;

        [RelayCommand]
        private void @event_ContainerLoaded(RoutedEventArgs e)
        {
            if (e.Source is Dialog.LoginDialog ld)
            {
                fallbackTask = ld.DialogFallback;
            }
        }

        [RelayCommand]
        private async Task @event_CloseDialog()
        {
            if (App.Current?.MainWindow is Window wind)
            {
                if (fallbackTask?.Task.IsCompleted is false)
                {
                    CancellationTokenSource.Cancel();
                    fallbackTask?.TrySetCanceled();
                    Service.Net.OpenFrp.Logout();
                    if (Reason is "UserTag 不匹配" || Reason?.Contains("后台") is true)
                    {
                        await Service.Net.OAuthApp.Logout().ConfigureAwait(false);
                    }
                }

                wind.SetCurrentValue(Awe.UI.Helper.WindowsHelper.DialogOpennedProperty, false);
                await Task.Delay(250);
                wind.SetCurrentValue(Awe.UI.Helper.WindowsHelper.DialogContentProperty,DependencyProperty.UnsetValue);
            }
        }

        [RelayCommand]
        private async Task @event_Login()
        {
            Reason = default;

            var oauthLogin = await OpenFrp.Service.Net.OAuthApp.Login(Username,Password, CancellationTokenSource.Token);
            if (!event_UploadState(oauthLogin)) return;

            var oauthAuthorize = await OpenFrp.Service.Net.OAuthApp.AuthorizeOpenFrp(CancellationTokenSource.Token);
            if (!event_UploadState(oauthAuthorize,() => oauthAuthorize.Data is not null && oauthAuthorize.Data.Code.IsNotNullOrEmpty())) return;

            var openfrpLogin = await OpenFrp.Service.Net.OpenFrp.Login(oauthAuthorize.Data!.Code!,CancellationTokenSource.Token);
            if (!event_UploadState(openfrpLogin)) return;

            var openfrpUserinfo = await OpenFrp.Service.Net.OpenFrp.GetUserInfo(CancellationTokenSource.Token);
            if (!event_UploadState(openfrpUserinfo, () => openfrpUserinfo.Data is not null)) return;
            else if (openfrpUserinfo.Data is { } userInfo)
            {
                var rrpc = await ExtendMethod.RunWithTryCatch(async () =>
                {
                    return await App.RemoteClient.LoginAsync(new Service.Proto.Request.LoginRequest
                    {
                        UserToken = userInfo.UserToken,
                        UserTag = userInfo.UserID
                    }, deadline: DateTime.UtcNow.AddMinutes(10), cancellationToken: CancellationTokenSource.Token);
                });

                if (rrpc is (var data, var ex))
                {
                    if (ex is not null)
                    {
                        Reason = ex.Message;
                    }
                    else if (data is not null)
                    {
                        if (data.Flag)
                        {
                            fallbackTask?.SetResult(userInfo);
                            await event_CloseDialog();

                            return;
                        }
                        else { Reason = data.Message; }                        
                    }
                    Service.Net.OpenFrp.Logout();
                }
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
                Reason = resp.Message ?? resp.Exception?.Message ?? resp.StatusCode.ToString();
            }
            return false;
        }
    }
}
