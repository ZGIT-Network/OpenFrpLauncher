using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NaCl;
using OpenFrp.Service.Net;


namespace OpenFrp.Launcher.ViewModels
{
    internal partial class QrCodeFVViewModel : ObservableObject,IHrViewModel
    {
        private bool requrieClearContainer = false;

        private Views.LoginWindow.QrCodeFV? page;

        private Controls.QRCode? appQrCodeWorker;

        private DateTimeOffset LastUpdateTime = DateTimeOffset.MinValue;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasAuthorizationUrl))]
        private string? user_AuthorizationUrl;

        [ObservableProperty]
        private bool needDisplayUrl;

        public bool HasAuthorizationUrl { get => !string.IsNullOrEmpty(User_AuthorizationUrl); }

        private string? user_lastWatingUuid;

        private byte[]? user_privateKey;

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is Views.LoginWindow.QrCodeFV page && page.FindName("appQrCodeWorker") is Controls.QRCode qrCode)
            {
                this.page = page;
                this.appQrCodeWorker = qrCode;

                LastUpdateTime = DateTimeOffset.Now;

                event_RefreshLinkCommand.Execute(null);
            }
        }

        [RelayCommand]
        private void @event_CallbackRequest()
        {
            event_RequestUpdateCommand.Cancel();
            event_RefreshLinkCommand.Cancel();
            conve_WaitForPollLoginCommand.Cancel();

            page?.CallbackAction.Invoke(ViewModels.LoginWindowViewModel.LoginState);
        }

        // 当 UI 重新调入时触发
        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RequestUpdate(CancellationToken cancellationToken)
        {
            // 4 * 60 = 240
            if ((DateTimeOffset.Now - LastUpdateTime).TotalSeconds > 200 || appQrCodeWorker is { IsCreateQRCode: false })
            {
                await event_RefreshLinkCommand.ExecuteAsync(cancellationToken);
            }
            if (user_privateKey is { Length: > 0 } && !string.IsNullOrEmpty(user_lastWatingUuid))
            {
                await conve_WaitForPollLoginCommand.ExecuteAsync(cancellationToken);
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshLink(CancellationToken cancellationToken)
        {
            if (appQrCodeWorker is null) return;

            NeedDisplayUrl = false;

            this.ClearExecuteResult();

            conve_WaitForPollLoginCommand.Cancel();

            User_AuthorizationUrl = user_lastWatingUuid = default;

            this.user_privateKey = null;

            LastUpdateTime = DateTimeOffset.Now;

            if (requrieClearContainer)
            {
                appQrCodeWorker.ClearQRCode();
            }
            else
            {
                requrieClearContainer = false;
            }

            var (privateKey, publicKey) = NaCl.Curve25519XSalsa20Poly1305.KeyPair();

            this.user_privateKey = privateKey;

            var base64_publicKey = Convert.ToBase64String(publicKey)
                .Trim().Replace('+', '-').Replace('/', '_');

            //https://access.openfrp.net/argoAccess/requestLogin
            // POST: { public_key: byte[] }

            var access = await OpenFrpApi.AccessRequestLogin(base64_publicKey, cancellationToken);

            
            if (!this.UpdateState(access, () => access.Data is { AuthorizeUrl: not null, GuidString: not null }))
            {
                _ = appQrCodeWorker?.ApplyQRCodeAsync(null, default);
                return;
            }


            // for example
            User_AuthorizationUrl = access.Data!.AuthorizeUrl!;
            user_lastWatingUuid = access.Data.GuidString!;


            await appQrCodeWorker.ApplyQRCodeAsync(User_AuthorizationUrl, cancellationToken);

            _ = conve_WaitForPollLoginCommand.ExecuteAsync(null);
        }

        // 前端不会用到这个 Command，只是为了方便取消 //
        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_WaitForPollLogin(CancellationToken cancellationToken)
        {
            // 300 / 5 = 60per
            for (int i = 0; i < 59; i++)
            {
                if (user_lastWatingUuid is null)
                {
                    throw new NullReferenceException(nameof(user_lastWatingUuid));
                }
                if (user_privateKey is null)
                {
                    throw new NullReferenceException(nameof(user_privateKey));
                }
                if (cancellationToken.IsCancellationRequested) return;

                if ((DateTimeOffset.Now - LastUpdateTime).TotalSeconds >= 250)
                {
                    break;
                }
                var resp = await OpenFrpApi.AccessPollLogin(user_lastWatingUuid, cancellationToken);

                if (resp.StatusCode is not System.Net.HttpStatusCode.NoContent && resp.StatusCode is not System.Net.HttpStatusCode.OK)
                {
                    if (resp.Exception is System.Threading.Tasks.TaskCanceledException)
                    {
                        return;
                    }
                    if (resp.StatusCode is System.Net.HttpStatusCode.NotFound)
                    {
                        break;
                    }
                    this.UpdateState(resp);
                    _ = appQrCodeWorker?.ApplyQRCodeAsync(null,default);
                    return;
                }
                else if (resp.Headers != null && resp.Data is { AuthorizationData: not null })
                {
                    string? publicKeyString = default;
                    if (resp.Headers.TryGetValues("x-request-public-key", out var @_request_pv1))
                    {
                        publicKeyString = @_request_pv1.FirstOrDefault();
                    }
                    else if (resp.Headers.TryGetValues("X-Request-Public-Key", out var @_request_pv2))
                    {
                        publicKeyString = @_request_pv2.FirstOrDefault();
                    }
                    if (publicKeyString is null)
                    {
                        this.ExecuteResult = new Model.ExecuteResult
                        {
                            Message = "服务器提供了无效的公钥。",
                            StatusCode = (int)System.Net.HttpStatusCode.BadRequest
                        };
                        return;
                    }
                    // https://github.com/cloudflare/cloudflared/blob/master/token/encrypt.go#L87
                    // original data (nonce + encrypted Data)
                    // .Replace('-', '+').Replace('_', '/')
                    byte[] authorizationData = Convert.FromBase64String(resp.Data!.AuthorizationData!.Trim());

                    publicKeyString = publicKeyString.Trim().Replace('-', '+').Replace('_', '/');
                    switch (publicKeyString.Length % 4)
                    {
                        case 2: publicKeyString += "=="; break;
                        case 3: publicKeyString += "="; break;
                    }

                    byte[] serverPublicKey = Convert.FromBase64String(publicKeyString);

                    byte[] nonce = new byte[XSalsa20Poly1305.NonceLength];
                    byte[] encryptedMessage = new byte[authorizationData.Length - XSalsa20Poly1305.NonceLength];

                    Buffer.BlockCopy(authorizationData, 0, nonce, 0, nonce.Length);
                    Buffer.BlockCopy(authorizationData, nonce.Length, encryptedMessage, 0, encryptedMessage.Length);

                    using var poly = new NaCl.Curve25519XSalsa20Poly1305(user_privateKey, serverPublicKey);

                    byte[] decrypetdMessage = new byte[encryptedMessage.Length - XSalsa20Poly1305.TagLength];

                    if (poly.TryDecrypt(decrypetdMessage, encryptedMessage, nonce))
                    {
                        string data = Encoding.UTF8.GetString(decrypetdMessage);
   
                        page?.AuthorizationCodeWaiter.TrySetResult(data);
                    }
                    else
                    {
                        ExecuteResult = new Model.ExecuteResult
                        {
                            Message = "数据解密失败！",
                            StatusCode = -1
                        };
                    }
                    return;

                }
                
                await Task.Delay(4500, cancellationToken);
            }
            requrieClearContainer = false;
            // 超时自动刷新
            _ = event_RefreshLinkCommand.ExecuteAsync(null);
        }

        [RelayCommand(CanExecute = nameof(CanOpenLinkInWeb))]
        private void @event_OpenLinkInWeb()
        {
            if (string.IsNullOrEmpty(User_AuthorizationUrl)) return;

            string egx = User_AuthorizationUrl!.Replace("&", "^&");

            try 
            { 
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    FileName = "cmd",
                    Arguments = $"/c start {egx}"
                }); 
                return; 
            } catch { }

            try 
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = egx
                });
                return; 
            } catch { NeedDisplayUrl = true; }
            
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

        private bool CanOpenLinkInWeb() => !IsFailed;
        
        public bool IsFailed { get => ExecuteResult is not null; }

        [ObservableProperty,NotifyPropertyChangedFor(nameof(IsFailed))]
        [NotifyCanExecuteChangedFor(nameof(event_OpenLinkInWebCommand))]
        private Model.ExecuteResult? executeResult;
    }
}
