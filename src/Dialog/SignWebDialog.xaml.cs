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
using Windows.Media.Devices;
using Windows.UI.Xaml.Controls;

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

        //private async void WebView2_Loaded(object sender, RoutedEventArgs e)
        //{
        //    //if (sender is WebView2 wb)
        //    //{
        //    //    try
        //    //    {
        //    //        v2.IsDisplay = true;
                    
        //    //        await wb.EnsureCoreWebView2Async();
        //    //    }
        //    //    catch(Exception ex)
        //    //    {
        //    //        if (MessageBox.Show("无法加载 WebView 环境，请查看您是否已安装 Micrsoft Edge WebView 组件。\n点击\"确定\"复制整个错误内容\n\n" + ex.Message, "OpenFrp Launcher", MessageBoxButton.OKCancel, MessageBoxImage.Error, MessageBoxResult.OK) is MessageBoxResult.OK)
        //    //        {
        //    //            try
        //    //            {
        //    //                Clipboard.SetText(ex.ToString());
        //    //            }
        //    //            catch
        //    //            {

        //    //            }
        //    //        }

        //    //        Hide();
        //    //    }
        //    //}
        //}

        //private async void Wb_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        //{
        //    switch (e.WebMessageAsJson)
        //    {
        //        case "\"themeRequest\"":
        //            {
        //                PrimaryButtonText = "刷新";
        //                v1.IsDisplay = true;
        //                v2.IsDisplay = false;
        //                this.webView.CoreWebView2.PostWebMessageAsString(App.Current?.MainWindow?.GetValue(ModernWpf.ThemeManager.ActualThemeProperty) is ModernWpf.ElementTheme.Dark
        //                    ? "darkValueSet" : "lightValueSet");
        //                break;
        //            }
        //        default:
        //            {
        //                if (v2.IsDisplay is true) return;

        //                v1.IsDisplay = false;
        //                v2.IsDisplay = true;

        //                IsPrimaryButtonEnabled = false;
        //                PrimaryButtonText = string.Empty;
        //                CloseButtonText = "确定";
        //                webView.Dispose();


        //                string ls = e.TryGetWebMessageAsString();
        //                var resp = await OpenFrp.Service.Net.OpenFrp.UserSignIn(ls.Substring(0,ls.Length));

        //                v2.IsDisplay = false;
        //                v3.IsDisplay = true;
        //                if (resp.StatusCode is System.Net.HttpStatusCode.OK)
        //                {
        //                    v3_content.Text = resp.Data;
        //                }
        //                else
        //                {
        //                    v3_content.Text = resp.Data ?? resp.Message ?? resp.Exception?.Message;
        //                }

        //                return;
        //            }
        //    }
        //}

        //private void @event_CoreWebViewRequestFillter(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        //{
        //    try
        //    {
        //        if (e.Request.Uri.Contains("favicon.ico"))
        //        {
        //            return;
        //        }
        //        var va = e.Request.Uri.Replace("http://openfrp.signinapppack/", "");

        //        //System.Diagnostics.Debug.WriteLine($"请求: {e.Request.Uri} => {va}");

        //        var vaa = App.GetResourceStream(new Uri($"pack://application:,,,/Resources/SignWebView/{va}"));


        //        if (vaa is { Stream.Length: > 0 })
        //        {
        //            e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(vaa.Stream, 200, "OK", $"Content-Type: {vaa.ContentType}");
        //        }
        //    }
        //    catch
        //    {
            
        //    }
        //}

        //private void ContentDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        //{
        //    args.Cancel = true;
        //    this.webView.Reload();
        //}

        //private EventHandler<CoreWebView2DOMContentLoadedEventArgs>? @event;

        //private async void webView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        //{
        //    if (!e.IsSuccess) return;

        //    await Task.Delay(150);

        //    webView.CoreWebView2.AddWebResourceRequestedFilter("*://openfrp.signinapppack/*", CoreWebView2WebResourceContext.All);
        //    webView.CoreWebView2.WebResourceRequested += @event_CoreWebViewRequestFillter;

        //    @event = new(delegate
        //    {
        //        webView.Source = new Uri("http://openfrp.signInAppPack/index.html");
        //        webView.CoreWebView2.DOMContentLoaded -= @event;
        //    });
        //    webView.CoreWebView2.DOMContentLoaded += @event;
        //    webView.WebMessageReceived += Wb_WebMessageReceived;
        //    webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        //    webView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
        //    webView.CoreWebView2.Settings.IsPinchZoomEnabled = false;
        //    webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
        //    webView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
        //    webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        //    webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        //    webView.CoreWebView2.Settings.HiddenPdfToolbarItems = CoreWebView2PdfToolbarItems.None;
        //    webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
        //    webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
        //    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        //}
    }
}
