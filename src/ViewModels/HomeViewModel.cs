using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using AppNetwork = OpenFrp.Service.Net;


namespace OpenFrp.Launcher.ViewModels
{
    internal partial class HomeViewModel : ObservableObject
    {
        public HomeViewModel()
        {
            if (App.Current?.MainWindow is { DataContext: MainViewModel dx } v)
            { 
                if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(v))
                {
                    return;
                } 
                UserInfo = dx.UserInfo;
                AvatorFilePath = dx.AvatorFilePath;

                dx.PropertyChanged += (_, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(UserInfo):
                            {
                                UserInfo = dx.UserInfo;
                                OnPropertyChanged(nameof(UserInfo));

                                event_RefreshSignStateCommand.Execute(null);
                            };break;
                        case nameof(AvatorFilePath):
                            {
                                AvatorFilePath = dx.AvatorFilePath;
                            };break;
                    }
                };
                
                if (dx.UserInfo.UserID != 0)
                {
                    event_AppRefreshCommand.Execute(null);
                }
            }
            event_RefreshBoardcastCommand.Execute(null);
            event_LoadAdSenceCommand.Execute(null);
            int h = DateTimeOffset.Now.Hour;

            if (h >= 0 && h <= 5) { HiString = "夜深了,早点睡觉哦,"; }
            if (h > 5 && h <= 8) { hiString = "早啊,"; }
            if (h > 9 && h < 12) { hiString = "上午好,"; }
            if (h >= 12 && h <= 18) { hiString = "下午好,"; }
            if (h > 18 && h < 24) { hiString = "晚上好,"; }
            //if (h > 21 && h <= 24) { hiString = "晚上好,"; }

            WeakReferenceMessenger.Default.UnregisterAll(nameof(HomeViewModel));

            WeakReferenceMessenger.Default.Register<RouteMessage<HomeViewModel, string>>(nameof(HomeViewModel), (_, data) =>
            {
                switch (data.Data)
                {
                    case "refresh":
                        {
                            if (!event_AppRefreshCommand.IsRunning && refreshFinish && UserInfo.UserID != 0)
                            {
                                _ = event_AppRefreshCommand.ExecuteAsync(null);
                            }
                            break;
                        }
                }
            });
        }

        [ObservableProperty]
        private string hiString = "";

        private bool refreshFinish;

        [ObservableProperty]
        private Awe.Model.OpenFrp.Response.Data.UserInfo userInfo = new Awe.Model.OpenFrp.Response.Data.UserInfo
        {
            UserName = "not-allow-display"
        };

        [ObservableProperty]
        private string avatorFilePath = "pack://application:,,,/Resources/Images/share_5bb469267f65d0c7171470739108cdae.png";

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
        private void @event_GotoSettingPage()
        {
            WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(typeof(Views.Settings)));
        }
        
        private bool CanExcuteCallUpSign() => !App.Current.Dispatcher.Invoke(() => IsSigned);

        [RelayCommand(CanExecute = nameof(CanExcuteCallUpSign))]
        private async Task @event_CallUpSign()
        {
            try
            {
                var dialog = new Dialog.SignWebDialog();

                if (await dialog.ShowAsync() is ModernWpf.Controls.ContentDialogResult.Secondary)
                {
                    event_AppRefreshCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "OpenFrp Launcher", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task @event_AppRefresh()
        {
            ResponseForBoardcast = default;

            await Task.WhenAll(
                event_RefreshSignStateCommand.ExecuteAsync(null), 
                event_RefreshBoardcastCommand.ExecuteAsync(null),
                event_LoadAdSenceCommand.ExecuteAsync(null),
                Task.Run(async delegate
                {
                    var openfrpUserinfo = await AppNetwork.OpenFrp.GetUserInfo();

                    if (openfrpUserinfo.Data is not null)
                    {
                        WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create<Awe.Model.OpenFrp.Response.Data.UserInfo>(openfrpUserinfo.Data));

                        refreshFinish = true;
                    }
                }));
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
                    App.Current.Dispatcher.Invoke(() => IsSigned = false);

                    OnPropertyChanged(nameof(IsSigned));
                }
            }
        }

        [RelayCommand]
        private async Task @event_RefreshBoardcast()
        {
            var resp = await OpenFrp.Service.Net.OpenFrp.GetSoftwareBoardcast();

            

            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { })
            {
                Papad = resp.Data.Papad;



                //FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ModernWpf.Controls.SimpleStackPanel));
                //factory.SetValue(ModernWpf.Controls.SimpleStackPanel.SpacingProperty, 8);
                //var stackpanel = new ModernWpf.Controls.SimpleStackPanel()
                //{
                //    Spacing = 8
                //};

                //foreach (var para in resp.Data.Paras)
                //{
                //    switch (para.Type)
                //    {
                //        case "System.String":
                //            {
                //                TextBlock textWrapper = new TextBlock()
                //                {
                //                    Text = para.Data.TryGetValue("value", out var v) ? v.ToString() : string.Empty,
                //                    HorizontalAlignment = HorizontalAlignment.Left,
                //                    TextWrapping = TextWrapping.Wrap
                //                };

                //                stackpanel.Children.Add(textWrapper);
                //                //factory.AppendChild(textWrapper);
                //            };break;
                //        default:
                //            {
                //                var a = typeof(System.Windows.Controls.Button).ToString();
                //                //System.Windows.Controls.Button
                //                //System.Windows.Controls.Button

                                
                //                var type = Type.GetType(para.Type,false);

                //                if (type is null) break;

                //                if (type.BaseType == typeof(FrameworkElement))
                //                {
                //                    var va = Activator.CreateInstance(type);

                //                    stackpanel.Children.Add((FrameworkElement)va);

                    
                //                }
                //            };break;
                //    }
                //}
                //Boards = stackpanel;
                
            }
            else if (string.IsNullOrEmpty(resp.Message))
            {
                resp.Message = resp.Exception?.ToString();
            }

            ResponseForBoardcast = resp;
        }

        [ObservableProperty]
        private FrameworkElement boards = new FrameworkElement();

        [ObservableProperty]
        private Awe.Model.ApiResponse? responseForBoardcast;

        [ObservableProperty]
        private Awe.Model.ApiResponse? responseForAdSence;

        [RelayCommand]
        private async Task @event_LoadAdSence()
        {
            var resp = await AppNetwork.HttpRequest.Get<Awe.Model.OpenFrp.Response.V2Response<AdSence[]>>("https://api.zyghit.cn/zg-adsense/openfrp-lanucher");


            try
            {
                if (resp.Data is { Data.Length: > 0 })
                {
                    resp.Message = "ok";

                    ResponseForAdSence = resp;

                    AdSences = new ObservableCollection<AdSence>();
                    //{
                    //    AdSences[0]
                    //};

                    foreach (var item in resp.Data.Data)
                    {
                        AdSences.Add(item);
                    }
                }
            }
            catch
            {

            }
        }

        [RelayCommand]
        private void @event_ToSettingsPage()
        {
            WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(typeof(Views.Settings)));
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(event_CallUpSignCommand))]
        private bool isSigned = true;

        [ObservableProperty]
        private int phIndex;

        [ObservableProperty]
        private ObservableCollection<AdSence> adSences = new ObservableCollection<AdSence>()
        {
            //new()
            //{
            //    Title = "OpenFRP Preview 启动器上新啦",
            //    Description = "参与使用，一起来填充这个大坑，一起走我们的路。你的赞助是前进的第一动力，欢迎赞助启动器作者。",
            //    Image = @"../Resources/Images/wallhaven-28ldd9_1920x1080.png",
            //    Url = "https://r.zyghit.cn/p2author"
            //},
        };

        [ObservableProperty]
        private string[] papad = Array.Empty<string>();
    }
}
