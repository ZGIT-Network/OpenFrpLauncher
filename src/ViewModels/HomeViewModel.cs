using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        }

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

        [ObservableProperty]
        private bool isSigned = true;
    }
}
