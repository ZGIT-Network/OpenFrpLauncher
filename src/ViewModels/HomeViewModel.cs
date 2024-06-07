using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Web.WebView2.Wpf;
using OpenFrp.Launcher.Model;


namespace OpenFrp.Launcher.ViewModels
{
    internal partial class HomeViewModel : ObservableObject
    {
        public HomeViewModel()
        {
            if (App.Current?.MainWindow is { DataContext: MainViewModel dx })
            {
                UserInfo = dx.UserInfo;

                dx.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(UserInfo))
                    {
                        UserInfo = dx.UserInfo;
                        OnPropertyChanged(nameof(UserInfo));

                        event_RefreshSignStateCommand.Execute(null);
                    }
                };
                
                if (dx.UserInfo.UserID != 0)
                {
                    event_AppRefreshCommand.Execute(null);
                }
            }
            event_LoadAdSenceCommand.Execute(null);

            //cancellationToken = new CancellationTokenSource();

            //_ = Task.Run(async () =>
            //{
            //    while (!cancellationToken.Token.IsCancellationRequested)
            //    {
            //        await Task.Delay(2500);

            //        if (!flag) event_ActiveNextPage();

            //        flag = false;
            //    }
            //}, cancellationToken.Token);
        }

        //private readonly CancellationTokenSource cancellationToken;

        [ObservableProperty]
        private Awe.Model.OpenFrp.Response.Data.UserInfo userInfo = new Awe.Model.OpenFrp.Response.Data.UserInfo
        {
            UserName = "not-allow-display"
        };

        public Model.UserInfoItem[] UserInfoViewContainer { get; } = new Model.UserInfoItem[]
        {
            // 实际上创建了 Binding 属性，数据好更新。 

            __createUserInfoItem("用户组","GroupCName"),
            __createUserInfoItem("实名状态","IsRealname",new Awe.UI.Converter.BooleanToStringConverter("已实名","未实名")),
            __createUserInfoItem("隧道数",new string[]{ "UsedProxies", "MaxProxies" }),
            __createUserInfoItem("速率 (上/下)",new string[]{ "InputLimit", "OutputLimit" },new Awe.UI.Converter.CustomMathConverter(__limitSpeedCalc),"{0} Mbps / {1} Mbps"),
            __createUserInfoItem("可用流量","Traffic",new Awe.UI.Converter.DivisionConverter(1024,2),"{0} Gib")
        };
        private static object __limitSpeedCalc(object value)
        {
            if (value is int it)
            {
                return ((double)it) / 1024 * 8;
            }
            return -1;
        }
        private static Model.UserInfoItem __createUserInfoItem(string title, string path, IValueConverter? converter = default, string? format = default)
        {
            return new Model.UserInfoItem
            {
                Title = title,
                Binding = new Awe.UI.Helper.TwiceBindingHelper
                {
                    Property = System.Windows.Documents.Run.TextProperty,
                    Binding = new Binding()
                    {
                        Mode = BindingMode.OneWay,
                        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ItemsControl), 1),
                        Path = new PropertyPath($"DataContext.UserInfo.{path}"),
                        Converter = converter,
                        StringFormat = format
                    }
                }
            };
        }
        private static Model.UserInfoItem __createUserInfoItem(string title, string[] paths, IValueConverter? itemConverter = default, string? mformat = default)
        {
            var mb = new MultiBinding
            {
                Mode = BindingMode.OneWay,
                StringFormat = mformat ?? "{0} / {1}"
            };

            foreach (var path in paths)
            {
                mb.Bindings.Add(new Binding()
                {
                    Mode = BindingMode.OneWay,
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ItemsControl), 1),
                    Path = new PropertyPath($"DataContext.UserInfo.{path}"),
                    Converter = itemConverter
                });
            }

            return new Model.UserInfoItem
            {
                Title = title,
                Binding = new Awe.UI.Helper.TwiceBindingHelper
                {
                    Property = System.Windows.Documents.Run.TextProperty,
                    Binding = mb
                }
            };
        }

        [RelayCommand]
        private void @event_OpenOpenFrpWebsite()
        {
            try { Process.Start("http://console.openfrp.net/"); return; } catch { }
            try { Process.Start("start", "http://console.openfrp.net/"); } catch { }
        }

        [RelayCommand]
        private async Task @event_CallUpSign()
        {
            var dialog = new Dialog.SignWebDialog();

            if (await dialog.ShowAsync() is ModernWpf.Controls.ContentDialogResult.Secondary)
            {
                event_AppRefreshCommand.Execute(null);
            }
        }

        [RelayCommand]
        private async Task @event_AppRefresh()
        {
            await event_RefreshSignStateCommand.ExecuteAsync(null);
            var openfrpUserinfo = await OpenFrp.Service.Net.OpenFrp.GetUserInfo();

            if (openfrpUserinfo.Data is not null)
            {
                WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(openfrpUserinfo.Data));
            }
        }

        [RelayCommand]
        private async Task @event_RefreshSignState()
        {
            var resp = await OpenFrp.Service.Net.OpenFrp.GetUserSignInfo();

            if (resp.StatusCode is System.Net.HttpStatusCode.OK &&
                resp.Data is { SignDate: long date})
            {
                var va = DateTimeOffset.FromUnixTimeMilliseconds(date).LocalDateTime;
                if ((va - DateTimeOffset.Now.Date).TotalDays < 0)
                {
                    IsSigned = false;
                }
            }
        }

        [RelayCommand]
        private async Task @event_LoadAdSence()
        {
            var sence = await OpenFrp.Service.Net.HttpRequest.Get<Awe.Model.OpenFrp.Response.V2Response<OpenFrp.Launcher.Model.AdSence[]>>("https://api.zyghit.cn/zg-adsense/openfrp-lanucher");

            if (sence.Data is { } bv && bv.Data?.Length > 0)
            {
                foreach (var item in bv.Data)
                {
                    //item.Url = @"E:\Desktop\Photo\wallhaven-76w8x3_1920x1080.png";

                    //ThreadPool.QueueUserWorkItem(async delegate
                    //{
                    //    if (string.IsNullOrEmpty(item.Image)) return;
                    //    if (item.Image is null) return;

                    //    var vaa = OpenFrp.Service.HashCalculator.CompushHash($"{item.Url}.vacc.jpg");

                    //    var va = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{vaa}.jpg");

                    //    if (System.IO.File.Exists(va)) 
                    //    {
                    //        item.Image = va;
                    //        return; 
                    //    }

                    //    var avator = await Service.Net.HttpRequest.Get(item.Image);

                    //    if (avator.StatusCode is System.Net.HttpStatusCode.OK && avator.Data.Count() > 0)
                    //    {
                            

                            

                    //        try
                    //        {

                    //            var vca = Encoding.UTF8.GetString(avator.Data.ToArray());

                    //            using var fs = System.IO.File.OpenWrite(va);

                    //            await fs.WriteAsync(avator.Data.ToArray(), 0, avator.Data.Count());



                    //            await fs.FlushAsync();

                    //            item.Image = va;
                    //        }
                    //        catch
                    //        {
                                

                    //            return;
                    //        }

                            
                    //    }
                    //});

                    AdSences.Add(item);
                }
            }
        }

        [ObservableProperty]
        private bool isSigned = true;

        [ObservableProperty]
        private int phIndex;

        [ObservableProperty]
        private ObservableCollection<AdSence> adSences = new ObservableCollection<AdSence>()
        {
            new()
            {
                Title = "OpenFRP Preview 启动器上新啦",
                Description = "参与使用，一起来填充这个大坑，一起走我们的路。你的赞助是前进的第一动力，往下面看，赞助启动器作者。",
                Image = @"../Resources/Images/wallhaven-28ldd9_1920x1080.png",
            },
        };

        //[RelayCommand]
        //private void @event_PageUnloaded() => cancellationToken.Cancel();

        //[RelayCommand]
        //private void @event_ActiveNextPage()
        //{
        //    if (AdSences.Count <= PhIndex + 1)
        //    {
        //        PhIndex = 0;
        //    }
        //    else PhIndex += 1;

        //    flag = true;
        //}

        //private bool flag;

        //[RelayCommand]
        //private void @event_ActiveAbovePage()
        //{
            
        //    if (PhIndex is 0)
        //    {
        //        PhIndex = AdSences.Count;
        //    }
        //    PhIndex -= 1;

        //    flag = true;
        //}

        [ObservableProperty]
        private string[] papad = new string[]
        {
            "正@微信",
            "活着不玩不如死@微信",
            "lck@微信",
            "SP@微信",
            "此用户不想起昵称@微信",
            "十一[秋叶]@微信",
            "洪平[西瓜]@微信",
            "慢热@微信",
            "胡俊杰@微信",
            "[鲨鱼]@微信",
            "Anoxia.@微信",
            "三更居@微信",
            "佳[鼠]@微信",
            "星光@微信"
        };
    }
}
