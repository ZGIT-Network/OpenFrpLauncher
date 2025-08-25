using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenFrp.Launcher
{
    /// <summary>
    /// WebView2Window.xaml 的交互逻辑
    /// </summary>
    [INotifyPropertyChanged]
    public partial class WebView2Window : Window
    {
        public WebView2Window()
        {
            InitializeComponent();

            logger = App.ServiceProvider.GetRequiredService<ILogger<WebView2Window>>();
        }

        private IntPtr hWnd; 
        private static readonly char[] PathSplit = new char[] { '/' };
        //private static readonly string Header = $"Access-Control-Allow-Origin: http://localhost:3201";
        private static readonly string[] OnlySupportedCommand = new string[]
        {
            "reload",
            "inspectElement"
        };
        private readonly ILogger<WebView2Window> logger;
        private readonly EventWaitHandle eventWaitHandle = new EventWaitHandle(false,EventResetMode.ManualReset) { };



        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(WebView2Window), new PropertyMetadata("about:blank"));



        [ObservableProperty,NotifyPropertyChangedFor(nameof(IsWebView2RuntimeException))]
        private Exception? exception;

        public bool IsWebView2RuntimeException { get => Exception is Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException; }

       

        protected override void OnClosed(EventArgs e)
        {
            hWnd = IntPtr.Zero;

            if (Owner is AppWindow ap)
            {
                iNKORE.UI.WPF.Modern.ThemeManager.RemoveActualThemeChangedHandler(ap, AppWindow_ActualThemeValueChanged);
            }

            eventWaitHandle.Set();
            eventWaitHandle.Close();
            eventWaitHandle.Dispose();

            wv.Dispose();

            base.OnClosed(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState is WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            base.OnStateChanged(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            if (hWnd != IntPtr.Zero)
            {
                int style = Win32.User32.GetWindowLong(hWnd, -16);

                style &= ~(131072);

                _ = Win32.User32.SetWindowLong(hWnd, -16, style);
            }
            base.OnActivated(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            hWnd = new WindowInteropHelper(this).EnsureHandle();

            SetBinding(iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty, new Binding
            {
                Source = App.Settings,
                Path = new PropertyPath(nameof(App.Settings.ApplicationTheme)),
                Mode = BindingMode.OneWay
            });

            if (Win32.UserUxtheme.IsSupportDarkMode)
            {
                if (hWnd != IntPtr.Zero)
                {
                    Win32.UserUxtheme.ShouldSystemUseDarkMode();
                    Win32.UserUxtheme.ShouldAppsUseDarkMode();
                    Win32.UserUxtheme.AllowDarkModeForApp(true);
                    Win32.UserUxtheme.AllowDarkModeForWindow(hWnd, true);
                }

                var so = (HwndSource)HwndSource.FromVisual(this);

                so.CompositionTarget.BackgroundColor = Colors.Transparent;

                if (OSVersionHelper.IsWindows11OrGreater && App.Settings.BackdropType != BackdropType.None)
                {
                    BackdropHelper.Apply(this, App.Settings.BackdropType, false);
                }

                SetDwmExtendFrameIntoClientArea(-1, -1, -1, -1);
            }

            if (Owner is AppWindow)
            {
                AppWindow_ActualThemeValueChanged(null, new RoutedEventArgs { });

                iNKORE.UI.WPF.Modern.ThemeManager.AddActualThemeChangedHandler(Owner, AppWindow_ActualThemeValueChanged);
            }

            _ = Task.Run(() => eventWaitHandle.WaitOne(5000)).ContinueWith(async t =>
            {
                if (eventWaitHandle.SafeWaitHandle.IsClosed || t.Result || hWnd == IntPtr.Zero)
                {
                    return;
                }

                await Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        //throw new NotSupportedException();
                        if (wv.CoreWebView2 is not null) return;
                        await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
                    }
                    catch(Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException ex)
                    {
                        if (!OSVersionHelper.IsWindows11OrGreater)
                        {
                            SetDwmExtendFrameIntoClientArea(1, 0, 0, 0);
                            SetResourceReference(BackgroundProperty, "ApplicationPageBackgroundThemeBrush");
                        }
                        Exception = new Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException("WebView2 环境未安装，请先安装后方可使用该功能。",ex.InnerException);
                    }
                    catch (Exception ex)
                    {
                        if (!OSVersionHelper.IsWindows11OrGreater)
                        {
                            SetDwmExtendFrameIntoClientArea(1, 0, 0, 0);
                            SetResourceReference(BackgroundProperty, "ApplicationPageBackgroundThemeBrush");
                        }
                        Exception = ex;
                    }
                    finally
                    {
                        refreshButton.Visibility = Visibility.Collapsed;
                    }
                });
            });

            base.OnSourceInitialized(e);
        }

        private void AppWindow_ActualThemeValueChanged(object? sender,RoutedEventArgs e)
        {
            if (Owner is AppWindow ap)
            {
                var actual = iNKORE.UI.WPF.Modern.ThemeManager.GetActualTheme(ap);

                if (actual == iNKORE.UI.WPF.Modern.ElementTheme.Dark)
                {
                    if (wv.CoreWebView2 is { } core)
                    {
                        core.PostWebMessageAsString("setDarkMode");
                    }
                    this.ApplyDarkMode();
                }
                else
                {
                    if (wv.CoreWebView2 is { } core)
                    {
                        core.PostWebMessageAsString("removeDarkMode");
                    }
                    this.RemoveDarkMode();
                }
            }
        }

        private void wv_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (wv.CoreWebView2 is not { } core || !e.IsSuccess) return;

            eventWaitHandle.Set();

            core.DownloadStarting += Core_DownloadStarting;
            core.NewWindowRequested += Core_NewWindowRequested;

            core.AddWebResourceRequestedFilter("*://openfrp.local/*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += Core_WebResourceRequested;

            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsPinchZoomEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.Settings.IsSwipeNavigationEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.HiddenPdfToolbarItems = Microsoft.Web.WebView2.Core.CoreWebView2PdfToolbarItems.None;
            core.Settings.IsGeneralAutofillEnabled = false;

            core.IsMuted = true;

            core.NavigationStarting += Core_NavigationStarting;
            core.ContextMenuRequested += Core_ContextMenuRequested;
            core.WebMessageReceived += Core_WebMessageReceived;
            core.NavigationCompleted += Core_NavigationCompleted;

            core.Environment.BrowserProcessExited += (s, ev) =>
            {
                if (hWnd == IntPtr.Zero) return;
                _ = Dispatcher.InvokeAsync(() =>
                {
                    if (this.IsActive)
                    {
                        Exception = new Exception("WebView2 进程意外退出，请重启应用。");
                    }
                });
            };
        }

        private void Core_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            wv.CoreWebView2.ExecuteScriptAsync("window.print = (function(){})");
        }

        private void Core_NavigationStarting(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.Equals(Source)) return;

            e.Cancel = true;

            _ = wv.CoreWebView2.ExecuteScriptAsync($"console.warn('操控台已禁止跳转到 URL: {e.Uri}，正在调起用户浏览器。')");

            if (!this.IsActive || !e.IsUserInitiated) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = e.Uri
                });
                return;
            }
            catch
            {

            }
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    Arguments = e.Uri,
                    FileName = "start"
                });
            }
            catch
            {

            }
        }

        private void Core_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string str = e.TryGetWebMessageAsString();

            switch (str)
            {
                case "requestUserTheme":
                    AppWindow_ActualThemeValueChanged(null, new RoutedEventArgs { });
                    return;
                case "exitProc":
                    DialogResult = true;
                    goto case "exitProcFail";
                case "exitProcFail":
                    this.Close();
                    break;
            }
        }

        private void Core_ContextMenuRequested(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuRequestedEventArgs e)
        {
            using var deferral = e.GetDeferral();

            for (int i = e.MenuItems.Count - 1; i >= 0; i--)
            {
                var v = e.MenuItems[i];
                if (OnlySupportedCommand.Contains(v.Name) || v.Kind is Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuItemKind.Separator)
                {
                    continue;
                }
                else
                {
                    e.MenuItems.RemoveAt(i);
                }
            }
        }

        private async void Core_WebResourceRequested(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        {
            

            try
            {
                if (e.Request.Uri is not { Length: > 21 } url || !url.StartsWith("https"))
                {
                    return;
                }
                using var deferral = e.GetDeferral();

                url = url.Substring(21);

                string[] path = url.Split(PathSplit, options: StringSplitOptions.RemoveEmptyEntries);

                if (path.Length < 2)
                {
                    return;
                }
                switch (path.First())
                {
                    case "api":
                        {
                            if (e.Request.Method is "OPTIONS")
                            {
                                string origin = e.Request.Headers.GetHeader("Referer");

                                e.Response = wv.CoreWebView2.Environment.CreateWebResourceResponse(Stream.Null, 200, "OK", null);
                                e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", origin.Remove(origin.Length - 1));
                                e.Response.Headers.AppendHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, Content-Length");

                                return;
                            }
                            
                            switch (path[1])
                            {
                                case "getUserInfo":
                                    {
                                        var u = await Service.Net.OpenFrpApi.GetUserInfo();

                                        if (u.Data is null) { break; }

                                        await WriteCoreWebView2WebResponse(e, u.Data);
                                    }
                                    ;return ;
                                case "getNodeList":
                                    {
                                        var t = await Service.Net.OpenFrpApi.GetNodes();

                                        if (t.Data is not { Total: >= 0 } l)
                                        {
                                            break;
                                        }

                                        await WriteCoreWebView2WebResponse(e, t.Data.List ?? Array.Empty<Yue3.Model.OpenFrp.Response.Data.Node>());
                                    }
                                    ;return;
                                case "getUserTunnels":
                                    {
                                        var ut = await Service.Net.OpenFrpApi.GetUserTunnels();

                                        if (ut.Data is not { Total: >= 0 } l)
                                        {
                                            break;
                                        }
                                        await WriteCoreWebView2WebResponse(e, ut.Data.List ?? Array.Empty<Yue3.Model.OpenFrp.Response.Data.UserTunnel>());
                                    }; return;
                                //case "editTun" when e.Request.Method is "POST" && e.Request.Content != Stream.Null: { 
                                //    }
                                case "newTun" or "editTun" when e.Request.Method is "POST" && e.Request.Content != Stream.Null:
                                    {
                                        using MemoryStream ms = new MemoryStream();

                                        await e.Request.Content.CopyToAsync(ms);
                                        ms.Seek(0, SeekOrigin.Begin);

                                        var resp = await System.Text.Json.JsonSerializer.DeserializeAsync<Yue3.Model.OpenFrp.Request.ModifyTunnelRequest>(ms);

                                        if (resp is null) { break; }

                                        Yue3.Model.Result.HttpResponse<Yue3.Model.OpenFrp.Response.BaseResponse> rs;
                                        if (path[1] is "editTun")
                                        {
                                            rs = await Service.Net.OpenFrpApi.EditTunnel(resp);
                                        }
                                        else
                                        {
                                            rs = await Service.Net.OpenFrpApi.CreateTunnel(resp);
                                        }

                                        if (rs.Data is null) { break; }

                                        await WriteCoreWebView2WebResponse(e, rs.Data);

                                    };return ;
                            }
                        };break;
                }
                // https://openfrp.app/ Length:20
                logger.LogInformation("[WebView2] WebResource Req: {url}", url);

            }
            catch (System.ObjectDisposedException)
            {
                return;
            }
            finally
            {
                if (e.Response is null)
                {
                    e.Response = wv.CoreWebView2.Environment.CreateWebResourceResponse(System.IO.Stream.Null, 500, "Not found route", "");

                    var or3 = e.Request.Headers.GetHeader("Referer");

                    e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", or3.Remove(or3.Length - 1));
                }
            }
        }

        private async Task WriteCoreWebView2WebResponse(Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e,object data)
        {
            string origin = e.Request.Headers.GetHeader("Referer");

            MemoryStream ms = new MemoryStream();

            await System.Text.Json.JsonSerializer.SerializeAsync(ms, data);

            ms.Seek(0, SeekOrigin.Begin);

            e.Response = wv.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "");
            e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", origin.Remove(origin.Length - 1));
            e.Response.Headers.AppendHeader("Content-Type", "application/json");
            e.Response.Headers.AppendHeader("Content-Length", ms.Length.ToString());
        }

        private void Core_NewWindowRequested(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs e)
        {
            using var defferal = e.GetDeferral();

            e.Handled = true;
        }

        private void Core_DownloadStarting(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs e)
        {
            e.Cancel = true;
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (wv.CoreWebView2 is not null)
            {
                refreshButton.Visibility = Visibility.Collapsed;
                wv.Visibility = Visibility.Visible;
                wv.CoreWebView2.Reload();
            }
        }

        private async void displayExceptionButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.ErrorContentDialog
            {
                Owner = this,
            };
            dialog.SetValue(Controls.ErrorViewer.ExceptionProperty, Exception);
            var resp = await dialog.ShowAsync();
        }

        private void SetDwmExtendFrameIntoClientArea(int th,int bh,int lw,int rw)
        {
            var margins = new Win32.DwmApi.Margins
            {
                BottomHeight = bh,
                LeftWidth = lw,
                RightWidth = rw,
                TopHeight = th
            };
            Win32.DwmApi.DwmExtendFrameIntoClientArea(hWnd, ref margins);
        }
    }
}
