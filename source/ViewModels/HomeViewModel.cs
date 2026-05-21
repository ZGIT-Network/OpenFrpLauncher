using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.Model;
using OpenFrp.Service.Net;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class HomeViewModel : ObservableObject
    {
        public HomeViewModel()
        {
            if (Application.Current.MainWindow is AppWindow { DataContext: MainWindowViewModel mv } t)
            {
                _mainWindow = t;
                _mainWindowViewModel = mv;
            }
        }

        private iNKORE.UI.WPF.Modern.Controls.Page? page;

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is Page page)
            {
                this.page = page;
                if (page.FindName("svo2") is ScrollViewerEx svo2)
                {
                    scroller = svo2;

                    if (page.Tag is "scrollToEnd")
                    {
                        svo2.ScrollToEnd();
                    }
                }
                if (page.FindName("carousel") is Controls.Carousel c)
                {
                    c.SetBinding(Controls.Carousel.IsActiveProperty, new System.Windows.Data.Binding("IsActive")
                    {
                        Source = _mainWindow,
                        Mode = System.Windows.Data.BindingMode.OneWay
                    });
                }

                page.Unloaded += delegate
                {
                    event_RefreshAdSenseCommand.Cancel();
                    event_RefreshUserInfoCommand.Cancel();
                    event_RefreshBroadCastCommand.Cancel();
                };
                event_RefreshAdSenseCommand.Execute(null);
                event_RefreshUserInfoCommand.Execute(null);
                event_RefreshBroadCastCommand.Execute(null);
            }
        }

        private readonly AppWindow? _mainWindow;
        private readonly MainWindowViewModel? _mainWindowViewModel;

        private ScrollViewerEx? scroller;

        internal void ScrollToEnd() => scroller?.ScrollToEnd();

        [ObservableProperty]
        private Model.AdSenseItem[]? adSences;

        [ObservableProperty]
        private Model.HomeUserInfoVex2[] userInfoVex2 = Array.Empty<Model.HomeUserInfoVex2>();

        [ObservableProperty]
        private Model.HomeAlertMessage[]? homeAlerts;

        public Model.UserInfo UserInfo
        {
            get
            {
                if (_mainWindowViewModel is not null)
                {
                    return _mainWindowViewModel.UserInfo;
                }
                return SettingsViewModel.__userInfo_Defualt;
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshAdSense(CancellationToken cancellationToken)
        {
            AdSences = null;

            var resp = await Service.Net.OpenFrpApi.GetLauncherAdSense(cancellationToken);

            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { Length: > 0 })
            {
                var v = resp.Data.Select(x => new AdSenseItem(x)).ToArray();

                Nito.AsyncEx.AsyncLock @lock = new Nito.AsyncEx.AsyncLock();
#if NET
                _ = Parallel.ForAsync(0, v.Length, cancellationToken,async (index, cancellationToken) =>
                {
#else
                _ = Parallel.For(0, v.Length,async index =>
                {
#endif
                    if (cancellationToken.IsCancellationRequested) return;

                    var ve = resp.Data[index];

                    if (!string.IsNullOrEmpty(ve.ImageUrl))
                    {
                        string? hash = default;
                        using (var loc = await @lock.LockAsync())
                        {
                            while (string.IsNullOrEmpty(hash))
                            {
                                try
                                {
#if NET
                                    hash = "ofapp_" + await Service.Helpers.HashAlgorithmHelper.ComputeHashStringAsync(ve.ImageUrl!);
#else
                                hash = "ofapp_" + Service.Helpers.HashAlgorithmHelper.ComputeHashString(ve.ImageUrl!);
#endif
                                }
                                catch
                                {
                                    await Task.Delay(500, cancellationToken);

                                    continue;
                                }
                            }
                        }

                        string pathF = System.IO.Path.Combine(System.IO.Path.GetTempPath(), hash);
                        

                        try
                        {
                            bool exist = System.IO.File.Exists(pathF);

                            if (exist) 
                            {
                                using var vf = File.OpenRead(pathF);

                                if (vf.Length <= 0)
                                {
                                    exist = false;
                                }
                            }
                            if (!exist)
                            {
                                var resp = await Service.Net.HttpClient.DefualtInstance.GetStreamAsync(ve.ImageUrl!, cancellationToken);

                                if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { Length: > 0 } st)
                                {
                                    using var fs = System.IO.File.Open(pathF, FileMode.OpenOrCreate);

                                    fs.Seek(0, SeekOrigin.Begin);
                                    st.Seek(0, SeekOrigin.Begin);

#if NET
                                    await st.CopyToAsync(fs,cancellationToken);
#else
                                    await st.CopyToAsync(fs);    
#endif
                                    await fs.FlushAsync(cancellationToken);

                                    st.Close();
                                }
                                else
                                {
                                    return;
                                }
                            }
                            if (!Uri.TryCreate(pathF, UriKind.RelativeOrAbsolute, out var uri))
                            {
                                return;
                            }
                            

                            page?.Dispatcher.BeginInvoke(() =>
                            {
                                var bm = new BitmapImage();
                                try
                                {
                                    bm.BeginInit();
                                    bm.CacheOption = BitmapCacheOption.OnLoad;
                                    bm.UriSource = uri;
                                    bm.EndInit();
                                    bm.Freeze();
                                }
                                catch
                                {
                                }
                            }, priority: System.Windows.Threading.DispatcherPriority.Background, null);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                        catch
                        {

                        }
                    }
                    try
                    {
                        v[index] = new AdSenseItem(ve);
                    }
                    catch
                    {
                        return;
                    }
                });
                AdSences = v;
            }
            else
            {
                AdSences = new Model.AdSenseItem[1]
                {
                    new AdSenseItem
                    {
                        Description = "",
                        Title = "雄跨洞庭野，楚望古湘州",
                        Url = "https://console.openfrp.net",
                        Company = "默认"
                    },
                };
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshUserInfo(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            @conve_ConfigureUserInfoVex2();
        }

        private void @conve_ConfigureUserInfoVex2()
        {
            if (UserInfo.Equals(SettingsViewModel.__userInfo_Defualt)) return;

            UserInfoVex2 = new Model.HomeUserInfoVex2[]
            {
                new HomeUserInfoVex2("\uE77B","用户组",UserInfo.GroupCName),
                new HomeUserInfoVex2("\uE779","实名状态",UserInfo.IsRealname ? "已实名" : "未实名"),
                new HomeUserInfoVex2("\uE7E3","隧道数",$"{UserInfo.UsedProxies} / {UserInfo.MaxProxies}"),
                new HomeUserInfoVex2("\uE88A","速率 (上/下)",$"{Math.Round(UserInfo.InputLimit / 128d,2)} Mbps / {Math.Round(UserInfo.OutputLimit / 128d,2)} Mbps"),
                new HomeUserInfoVex2("\uED2A","可用流量",TranlateTrafficString(UserInfo.Traffic)),
            };

            string TranlateTrafficString(long trf)
            {
                double d = System.Convert.ToDouble(trf);
                double d1 = d / 1024d;
                if (d1 < 1)
                {
                    return $"{Math.Round(d, 2)} Mib";
                }
                double d2 = d1 / 1024d;
                if (d2 < 1)
                {
                    return $"{Math.Round(d1,2)} Gib";
                }
                return $"{Math.Round(d2 / 1024d, 2)} Tib";
            }
        }

        [RelayCommand]
        private void @event_OpenOpenFrpNet()
        {
            string? auth = Service.Net.OpenFrpApi.GetAuthorization();

            if (string.IsNullOrEmpty(auth))
            {
                OpenFrp.Service.Helpers.ProcessHelper.OpenLink("https://console.openfrp.net");
            }
            else
            {
                OpenFrp.Service.Helpers.ProcessHelper.OpenLink($"https://console.openfrp.net/fastlogin?auth={auth}");
            }
        }

        [RelayCommand]
        private void @event_OpenPaymentApp()
        {
            Service.Helpers.ProcessHelper.OpenLink("https://yue3.pages.dev/#/donate");
        }

        [RelayCommand]
        private void @event_GotoSettingPage()
        {
            Model.RouteMessage<ViewModels.MainWindowViewModel>.Send(typeof(Views.Settings));
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshBroadCast(CancellationToken cancellationToken)
        {
            HomeAlerts = default;

            var resp = await OpenFrpApi.CommonQueryGet<Yue3.Model.OpenFrp.Response.Data.BroadcastData>("broadcast",cancellationToken);

            if (resp.Data is null || resp.StatusCode is not System.Net.HttpStatusCode.OK)
            {
                return;
            }
            HomeAlerts = resp.Data.Alerts.Where(static x =>
            {
                if (x is null)
                {
                    return false;
                }

                return (x.MaximumVersion == App.LauncherVersionNumber && x.MinimalVersion == App.LauncherVersionNumber) || (App.LauncherVersionNumber >= x.MinimalVersion && (App.LauncherVersionNumber <= x.MaximumVersion || x.MaximumVersion.Equals(-1)));
            }).Select(x => new Model.HomeAlertMessage(x.Title,x.Type,x.Data)).ToArray();

            if (App.StartupArguments.Contains("--skipDialog"))
                return;
            App.StartupArguments.Add("--skipDialog");


            foreach (var dialog in resp.Data.Dialogs)
            {
                if (!string.IsNullOrEmpty(dialog.Olid) && App.Settings.FeatureHashStr.Contains(dialog.Olid))
                {
                    continue;
                }

                if (!(dialog.MaximumVersion == App.LauncherVersionNumber && dialog.MinimalVersion == App.LauncherVersionNumber) || (App.LauncherVersionNumber >= dialog.MinimalVersion && !(App.LauncherVersionNumber > dialog.MaximumVersion && !dialog.MaximumVersion.Equals(-1))))
                {
                    continue;
                }
                var ct = new ContentDialog()
                {
                    Title = dialog.Title,
                    Content = new ScrollViewerEx()
                    {
                        VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
                    },
                    DefaultButton = ContentDialogButton.Primary,
                    PrimaryButtonText = "我已阅读以上内容，确定",
                    AllowCloseByEsc = dialog.Delay <= 0,
                    IsPrimaryButtonEnabled = true,
                };
                if (dialog.Olid != "")
                {
                    ct.SecondaryButtonText = "关闭并不再提醒";
                }
                var tb = new System.Windows.Controls.TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap
                };

                foreach (var item in dialog.Data)
                {
                    tb.Inlines.Add(item);
                }

                if (ct.Content is ScrollViewerEx ex)
                {
                    ex.Content = tb;
                }

                if (dialog.Delay > 0)
                {
                    ct.IsSecondaryButtonEnabled = false;
                    ct.Loaded += async delegate
                    {
                        for (global::System.Int32 i = (dialog.Delay) - 1; i >= 0; i-=1000)
                        {
                            if (i < 0) break;

                            ct.IsPrimaryButtonEnabled = false;
                            ct.PrimaryButtonText = $"请等待 {Math.Round(i / 1000d)} 秒";

                            await Task.Delay(1000, cancellationToken);
                        }
                        ct.IsPrimaryButtonEnabled = true;
                        ct.IsSecondaryButtonEnabled = true;
                        ct.PrimaryButtonText = "我已阅读以上内容，确定";
                    };
                }
                if (await ct.ShowAsync() is ContentDialogResult.Secondary)
                {
                    App.Settings.FeatureHashStr += dialog.Olid + '|';
                }
            }
        }
    }
}
