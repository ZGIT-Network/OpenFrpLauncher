using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using System.Threading;
using OpenFrp.Service;
using OpenFrp.Launcher.HostLite;

namespace OpenFrp.Launcher.Rpc
{
    class WebOAuthHost : Launcher.HostLite.HttpServer
    {
        public WebOAuthHost(Action<Exception>? exceptionHandler = default)
        {
            ExceptionHandler = exceptionHandler;
        }

        private TaskCompletionSource<string> authCodeCompletion = new TaskCompletionSource<string> { };

        private bool refuseConnection = false;
        private string messagePlaceholdHtml = "";
        private string messagePlaceholdHtmlCss = "";
        private string? redirectUrl;

        public override async Task AcceptContextAsync(HttpServerRequest request)
        {
            if (refuseConnection || request.Url is null)
            {
                request.Abort();
                return;
            }
            
            var querys = request.GetQuerys();
            string[] patten = request.Url.AbsolutePath.Substring(1).Split('/').Where(x => !string.IsNullOrEmpty(x)).ToArray();

            if (patten.Length >= 1)
            {
                switch (patten.FirstOrDefault())
                {
                    case "callUpOAuthService":
                        {
                            if (string.IsNullOrEmpty(redirectUrl))
                            {
                                await ResponseWithMessageViewAsync(request,"请尝试稍后刷新，或在启动器中刷新。");
                            }
                            else
                            {
                                request.RedirectTo(redirectUrl!);
                            }
                            return;
                        };
                    case "oauth_callback":
                        {
                            if (!querys.HasKeys() || querys.Get("code") is not { Length: > 0 } code)
                            {
                                await ResponseWithMessageViewAsync(request, "请尝试重新在启动器中再次授权...");
                            }
                            else
                            {
                                authCodeCompletion.TrySetResult(code);
                                await ResponseWithMessageViewAsync(request, $"现在你可以关掉此页面了。");

                                _ = Task.Delay(1500).ContinueWith(delegate { Dispose(); });
                            };
                            return;
                        }
                    case "MessageWithPlaceholder.css":
                        {
                            await request.RespondWithStringAsync(messagePlaceholdHtmlCss, "text/css;charset=utf-8");
                            return;
                        }
                }   
            }

            await request.RespondWithJsonBodyAsync(patten);
        }

        public async Task ResponseWithMessageViewAsync(HttpServerRequest request,string message)
        {
            if (string.IsNullOrEmpty(messagePlaceholdHtml) || string.IsNullOrEmpty(messagePlaceholdHtmlCss))
            {
                try
                {
                    var asm = typeof(OpenFrp.Launcher.App).Assembly;

                    using var h5t = asm.GetManifestResourceStream("OpenFrp.Launcher.Resources.OAuth.MessageWithPlaceholder.html");
                    using var css = asm.GetManifestResourceStream("OpenFrp.Launcher.Resources.OAuth.MessageWithPlaceholder.css");
                    if (h5t != null && css != null)
                    {
                        using MemoryStream mfCss = new MemoryStream();
                        using MemoryStream mfH5t = new MemoryStream();

                        await Task.WhenAll(h5t.CopyToAsync(mfH5t), css.CopyToAsync(mfCss));

                        messagePlaceholdHtml = Encoding.UTF8.GetString(mfH5t.ToArray());
                        messagePlaceholdHtmlCss = Encoding.UTF8.GetString(mfCss.ToArray());
                    }
                }
                catch
                {

                }
            }
            await request.RespondWithStringAsync(string.Format(messagePlaceholdHtml, message), "text/html;charset=utf-8");
        }

        public override void HandleException(Exception ex)
        {
            ExceptionHandler?.Invoke(ex);
        }

        public void SetEnable(bool value)
        {
            refuseConnection = !value;
        }

        public void SetOAuthRedirectUrl(string? value)
        {
            redirectUrl = value;
        }

        public string? GetOAuthRedirectUrl() => redirectUrl;

        public Action<Exception>? ExceptionHandler { get; set; }

        public async Task<string?> WaitForAuthCode(CancellationToken cancellationToken = default)
        {
            return await authCodeCompletion.Task.WhenAnyTime(cancellationToken);
        }

        
    }
}
