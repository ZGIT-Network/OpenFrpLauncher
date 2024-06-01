using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher
{
    internal class LoginWebService : Service.Net.HttpService
    {
        public override async void AcceptContext(HttpListenerContext context)
        {
            if (context.Request.Url.AbsolutePath.Equals("/oauth_callback"))
            {
                string code = context.Request.QueryString.Get("code");
                if (code.Length > 0)
                {
                    WaitForCode.TrySetResult(code);

                    byte[] dance = Encoding.UTF8.GetBytes($"<html><head><meta charset=\"utf-8\"/></head><body><h2>现在你可以关闭浏览器了。</h2></body></html>");
                    await context.Response.OutputStream.WriteAsync(dance, 0, dance.Length);

                    context.Response.Close();
                    Stop();
                }
            }
            context.Response.Close();
        }

        public TaskCompletionSource<string> WaitForCode { get; private set; } = new TaskCompletionSource<string>();

        public override void CorruptException(Exception exception)
        {
            System.Windows.MessageBox.Show(exception.Message,"OpenFrp Launcher",System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Error);
        }

        public string? OriginalRedirectUrl { get; private set; }

        public override void ServiceHosted(IPAddress address, int port)
        {
            OriginalRedirectUrl = $"http://launcher.openfrp.net:{port}/oauth_callback";

            try { Process.Start($"https://openid.17a.ink/oauth2/authorize?response_type=code&redirect_uri=http://launcher.openfrp.net:{port}/oauth_callback&client_id=openfrp"); return; } catch { }
            try { Process.Start("start",$"https://openid.17a.ink/oauth2/authorize?response_type=code&redirect_uri=http://launcher.openfrp.net:{port}/oauth_callback&client_id=openfrp"); return; } catch { }
        }
    }
}
