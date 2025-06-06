using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class OAuthLoginViewModel : ObservableObject, IHrViewModel
    {
        private Views.LoginWindow.OAuthLoginView? page;

        private Rpc.WebOAuthHost? webHost;

        [ObservableProperty]
        private bool needDisplayUrl;

        [ObservableProperty,NotifyCanExecuteChangedFor(nameof(event_OpenLinkInWebCommand))]
        private ushort port;

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is Views.LoginWindow.OAuthLoginView page)
            {
                this.page = page;


                //webHost = new Rpc.WebOAuthHost();

                event_RefreshLinkCommand.Execute(null);
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshLink(CancellationToken cancellationToken)
        {
            if (webHost is null)
            {
                webHost = new Rpc.WebOAuthHost();
            }
            webHost.SetEnable(true);
            webHost.SetOAuthRedirectUrl(default);
            await webHost.StartAsync(CancellationToken.None);

            var resp = await Service.Net.OpenFrpApi.GetAuthorizeUrl(webHost.Port, cancellationToken);

            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { Length: > 0 } url)
            {
                webHost.SetOAuthRedirectUrl(resp.Data);
                Port = webHost.Port;

                conve_WaitForAuthCodeCommand.Execute(null);
            }
            else
            {
                await Task.Delay(1000, cancellationToken);
                Model.RouteMessage<LoginWindowViewModel>.Send<Yue3.Model.Result.HttpResponse>(resp);
                @event_CallbackRequestCommand.Execute(null);
                // callback to homepage
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_WaitForAuthCode(CancellationToken cancellationToken)
        {
            if (webHost is { } && page is { })
            {
                
                //page.ResetAuthorizationCodeWaiter();

                string? code = await webHost.WaitForAuthCode(cancellationToken);

                if (code is null)
                {
                    return;
                }

                page.AuthorizationCodeWaiter.TrySetResult($"{code}^http://localhost:{Port}/oauth_callback");
                Port = 0;

                webHost = null;
            }
        }

        [RelayCommand(CanExecute = nameof(CanOpenLinkInWeb))]
        private void @event_OpenLinkInWeb()
        {
            string egx = $"http://localhost:{Port}/callUpOAuthService";

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
            }
            catch { }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = egx
                });
                return;
            }
            catch { NeedDisplayUrl = true; }
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
            catch {  }
        }

        [RelayCommand]
        private void @event_CallbackRequest()
        {
            conve_WaitForAuthCodeCommand.Cancel();
            webHost?.SetEnable(false);
            page?.CallbackAction.Invoke(ViewModels.LoginWindowViewModel.LoginState);
        }

        public void DisposeWebHost()
        {
            webHost?.Dispose();
        }

        private bool CanOpenLinkInWeb() => Port > 0;
    }
}