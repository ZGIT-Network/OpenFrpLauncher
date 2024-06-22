using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using ModernWpf;
using ModernWpf.Controls;


namespace OpenFrp.Launcher.ViewModels
{
    /// <summary>
    /// 起源于一次想法,开始了这次牛逼的测试 
    /// WebView 签到测试
    /// </summary>
    internal partial class SignInWebViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _tabIndex = 0;

        [ObservableProperty]
        private string? message;

        private Dialog.SignWebDialog? _dialog;

        [RelayCommand]
        private void @event_DialogLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is Dialog.SignWebDialog sw)
            {
                _dialog = sw;

                //_dialog.Closed += delegate
                //{
                //    if (_dialog.FindName("webViewCore") is WebView2 wv)
                //    {
                //        wv.Dispose();
                //    }
                //};
            }
            
            
        }

        [RelayCommand]
        private void @event_DialogRefreshRequest(ContentDialogButtonClickEventArgs arg)
        {
            arg.Cancel = true;

            if (_dialog?.FindName("webViewCore") is WebView2 wvw)
            {
                _dialog.IsPrimaryButtonEnabled = false;
                _dialog.PrimaryButtonText = string.Empty;
                TabIndex = 0;
                try { wvw.Reload(); } catch { }
            }
        }

        [RelayCommand]
        private void @event_DialogCancel(ContentDialogButtonClickEventArgs arg)
        {
            arg.Cancel = true;

            if (_dialog?.FindName("webViewCore") is WebView2 wv)
            {
                wv.Dispose();
            }

            _dialog?.Hide();
        }

        [RelayCommand]
        private async Task @event_WebViewHostCalled(RoutedEventArgs arg)
        {
            if (arg.Source is WebView2 {  } wv)
            {
                App.WebViewTemplatePath = OpenFrp.Service.Commands.FileDictionary.GetTemplateFolder();
                var ev = await CoreWebView2Environment.CreateAsync(userDataFolder: App.WebViewTemplatePath).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        try { Clipboard.SetText(task.Exception.ToString()); } catch { }
                        MessageBox.Show("请检查您是否已安装 Microsoft Edge WebView2 Runtime，若您已安装，请将错误信息打包后向我们反馈。\n\n错误内容:(已复制到剪切板) " + task.Exception.Message, "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                        

                        _dialog.RunOnUIThread(() => 
                        {
                            try { wv.Dispose(); } catch { }
                            _dialog?.Hide();
                        });

                        return default;
                    }
                    return task.Result;
                });
                if (ev is null) return;
                try
                {
                    var bol = await wv.EnsureCoreWebView2Async(ev).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            try { Clipboard.SetText(task.Exception.ToString()); } catch { }
                            MessageBox.Show("请检查您是否已安装 Microsoft Edge WebView2 Runtime，若您已安装，请将错误信息打包后向我们反馈。\n\n错误内容:(已复制到剪切板) " + task.Exception.Message, "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Error);


                            _dialog.RunOnUIThread(() =>
                            {
                                try { wv.Dispose(); } catch { }
                                _dialog?.Hide();
                            });
                        }
                        return !task.IsFaulted;
                    });
                    if (!bol) return;
                }
                catch { }
                // 设置内置过滤器
                wv.CoreWebView2.AddWebResourceRequestedFilter("*://openfrp.signinapppack/*", CoreWebView2WebResourceContext.All);
                wv.CoreWebView2.WebResourceRequested += event_CoreWebViewRequestFillter; ;
                wv.CoreWebView2.WebMessageReceived += event_CoreWebViewMessageReceived;

                wv.CoreWebView2.Settings.AreDevToolsEnabled = System.Diagnostics.Debugger.IsAttached;

                wv.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                wv.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                wv.CoreWebView2.Settings.IsPinchZoomEnabled = false;
                wv.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                wv.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
                wv.CoreWebView2.Settings.IsZoomControlEnabled = false;

                wv.CoreWebView2.Settings.IsStatusBarEnabled = false;
                
                wv.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                wv.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                wv.CoreWebView2.Settings.AreDefaultContextMenusEnabled = System.Diagnostics.Debugger.IsAttached;

                wv.CoreWebView2.Navigate("http://openfrp.signInAppPack/index.html");
            }
        }

        private async void @event_CoreWebViewMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (sender is not Microsoft.Web.WebView2.Core.CoreWebView2 { } wv || _dialog is null) return;
            switch (e.WebMessageAsJson)
            {
                case "\"themeRequest\"":
                    {
                        _ = wv.AddScriptToExecuteOnDocumentCreatedAsync("window.print = function () {}");
                        _dialog.PrimaryButtonText = "刷新";
                        _dialog.IsPrimaryButtonEnabled = true;
                        TabIndex = 1;
                        wv.PostWebMessageAsString(App.Current?.MainWindow?.GetValue(ModernWpf.ThemeManager.ActualThemeProperty) is ModernWpf.ElementTheme.Dark
                            ? "darkValueSet" : "lightValueSet");
                        break;
                    }
                default:
                    {
                        _dialog.IsPrimaryButtonEnabled = false;
                        _dialog.PrimaryButtonText = string.Empty;
                        _dialog.CloseButtonText = string.Empty;
                        _dialog.SecondaryButtonText = "确定";
                        _dialog.DefaultButton = ModernWpf.Controls.ContentDialogButton.Secondary;

                        TabIndex = 0;

 
                        string ls = e.TryGetWebMessageAsString();
                        var resp = await OpenFrp.Service.Net.OpenFrp.UserSignIn(ls.Substring(0, ls.Length));

                        if (resp.StatusCode is System.Net.HttpStatusCode.OK)
                        {
                            Message = resp.Data;
                        }
                        else
                        {
                            Message = resp.Data ?? resp.Message ?? resp.Exception?.Message;
                        }
                        if (_dialog.FindName("webViewCore") is WebView2 wvw)
                        {
                            wvw.Dispose();
                        }
                        

                        TabIndex = 2;
                        return;
                    }
            }
        }

        private void @event_CoreWebViewRequestFillter(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {

            if (sender is not CoreWebView2 { } wv) return;

            try
            {
                var file = e.Request.Uri.Replace("http://openfrp.signinapppack/", "");
                var source = App.GetResourceStream(new Uri($"pack://application:,,,/Resources/SignWebView/{file}"));

                if (source is { Stream.Length: > 0 })
                {
                    e.Response = wv.Environment.CreateWebResourceResponse(source.Stream, 200, "OK", $"Content-Type: {source.ContentType}");
                }
                else
                {
                    e.Response = wv.Environment.CreateWebResourceResponse(Stream.Null, 404, "Not Found", $"Content-Type: {source.ContentType}");
                }
            }
            catch
            {
                e.Response = wv.Environment.CreateWebResourceResponse(Stream.Null, 502, "Service Unavaliable", $"Content-Type: text/plain;");
            }
        }
    }
}
