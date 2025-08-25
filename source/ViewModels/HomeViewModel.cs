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
                                continue;
                            }
                        }

                        string pathF = System.IO.Path.Combine(System.IO.Path.GetTempPath(), hash);
                        

                        try
                        {
                            if (!System.IO.File.Exists(pathF))
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
                        Description = "你的赞助是前进的第一动力，\n本项已从2023年开始成项，到2025年为开发的第四个版本，开发不易。\n欢迎赞助启动器作者 (越越)。",
                        Title = "日暮东风怨啼鸟，落花犹似坠楼人",
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
        }
    }
}
