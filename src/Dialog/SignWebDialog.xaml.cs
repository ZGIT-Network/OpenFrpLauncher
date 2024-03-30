using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using ModernWpf;
using Windows.Media.Capture.Core;

namespace OpenFrp.Launcher.Dialog
{
    /// <summary>
    /// SignWebDialog.xaml 的交互逻辑
    /// </summary>
 
    public partial class SignWebDialog : ModernWpf.Controls.ContentDialog
    {
        public SignWebDialog()
        {
            InitializeComponent();
        }
        [System.Runtime.InteropServices.ComVisible(true)]
        public class WebViewInjector
        {
            internal WebViewInjector(SignWebDialog dialog,WebView2 webView)
            {
                _dialog = dialog;
                _webView = webView;
            }

            public bool IsDarkTheme()
            {
                _dialog.PrimaryButtonText = "刷新";
                _dialog.v1.IsDisplay = true;
                _dialog.v2.IsDisplay = false;

                return App.Current?.MainWindow?.GetValue(ModernWpf.ThemeManager.ActualThemeProperty) is ModernWpf.ElementTheme.Dark;
            }

            //public void HideDialog()
            //{
            //    _dialog?.Hide();
            //    _webView?.Dispose();
            //}
            public async void CodeFallback(string code)
            {
                _dialog.v1.IsDisplay = false;
                _dialog.v2.IsDisplay = true;

                _dialog.IsPrimaryButtonEnabled = false;
                _dialog.PrimaryButtonText = string.Empty;
                _dialog.CloseButtonText = "确定";

                _webView.Dispose();


                var resp = await OpenFrp.Service.Net.OpenFrp.UserSignIn(code);

                _dialog.v2.IsDisplay = false;
                _dialog.v3.IsDisplay = true;
                if (resp.StatusCode is System.Net.HttpStatusCode.OK)
                {
                    _dialog.v3_content.Text = resp.Data;
                }
                else
                {
                    _dialog.v3_content.Text = resp.Data ?? resp.Message ?? resp.Exception?.Message;
                }
            }

            private SignWebDialog _dialog;
            private WebView2 _webView;
        }

        private async void WebView2_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is WebView2 wb)
            {
                try
                {
                    v2.IsDisplay = true;

                    await wb.EnsureCoreWebView2Async();
                    wb.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                    wb.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                    wb.CoreWebView2.AddHostObjectToScript("dialog", new WebViewInjector(this,wb));

                    wb.Source = new Uri("http://openfrp.signInAppPack/index.html");
                }
                catch(Exception ex)
                {
                    MessageBox.Show("无法加载 WebView 环境，请查看您是否已安装 Micrsoft Edge WebView 组件。\n\n" + ex.Message, "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Error);

                    Hide();
                }
            }
        }

        private void CoreWebView2_WebResourceRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        {
            try
            {
                if (!e.Request.Uri.Contains("http://openfrp.signinapppack/"))
                {
                    return;
                }
                var va = e.Request.Uri.Replace("http://openfrp.signinapppack/", "");

                //System.Diagnostics.Debug.WriteLine($"请求: {e.Request.Uri} => {va}");

                var vaa = App.GetResourceStream(new Uri($"pack://application:,,,/Resources/SignWebView/{va}"));


                if (vaa is { Stream.Length: > 0 })
                {
                    e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(vaa.Stream, 200, "OK", $"Content-Type: {vaa.ContentType}");
                }
            }
            catch
            {
            
            }
        }

        private void ContentDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            this.webView.Reload();
        }
    }
}
