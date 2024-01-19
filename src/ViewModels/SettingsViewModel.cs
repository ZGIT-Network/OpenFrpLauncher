using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using static Google.Protobuf.WellKnownTypes.Field.Types;



namespace OpenFrp.Launcher.ViewModels
{
    internal partial class SettingsViewModel : ObservableObject
    {
        private static System.Windows.Markup.XmlLanguage userLang = System.Windows.Markup.XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

        public SettingsViewModel()
        {
            if (App.Current?.MainWindow is { DataContext: MainViewModel dx })
            {
                MainViewModel = dx;

                UserInfo = dx.UserInfo;
            }
        }

        [ObservableProperty]
        private ObservableCollection<Model.FontDisplay> fonts = new ObservableCollection<Model.FontDisplay>(System.Windows.Media.Fonts.SystemFontFamilies.Select(x => new Model.FontDisplay
        {
            FontFamily = x,
            FontName = x.FamilyNames.ContainsKey(userLang) ? x.FamilyNames[userLang] : x.FamilyNames.First().Value,
        }));


        #region UserInfo + Model # 附加后台绑定

        public MainViewModel? MainViewModel { get; set; }

        [ObservableProperty]
        private Awe.Model.OpenFrp.Response.Data.UserInfo userInfo = new Awe.Model.OpenFrp.Response.Data.UserInfo
        {
            UserName = "not-allow-display"
        };

        public Model.UserInfoItem[] UserInfoViewContainer { get; } = new Model.UserInfoItem[]
        {
            // 实际上创建了 Binding 属性，数据好更新。 

            __createUserInfoItem("用户组","GroupCName"),
            __createUserInfoItem("ID","UserID"),
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
        private static Model.UserInfoItem __createUserInfoItem(string title,string path,IValueConverter? converter = default,string? format = default)
        {
            return new Model.UserInfoItem
            {
                Title = title,
                Binding = new Awe.UI.Helper.TwiceBindingHelper
                {
                    Property = TextBlock.TextProperty,
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
        private static Model.UserInfoItem __createUserInfoItem(string title,string[] paths, IValueConverter? itemConverter = default,string? mformat = default)
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
                    Property = TextBlock.TextProperty,
                    Binding = mb
                }
            };
        }

        #endregion

        public int ApplicationTheme
        {
            get
            {
                if (App.Current?.MainWindow is Window wind)
                {
                    return (wind.GetValue(Awe.UI.Helper.WindowsHelper.UseLightModeProperty) is true && Properties.Settings.Default.UseLightMode) ? 0 : 1;
                }
                return 0;
            }
            set
            {
                if (App.Current?.MainWindow is Window wind)
                {
                    bool v2 = value is 0 ? true : false;

                    wind.SetValue(Awe.UI.Helper.WindowsHelper.UseLightModeProperty, v2);
                    App.RefreshApplicationTheme(wind, v2);

                    Properties.Settings.Default.UseLightMode = v2;
                    if (FollowBySystem is true)
                    {
                        Properties.Settings.Default.FollowSystemTheme = false;
                        OnPropertyChanged(nameof(FollowBySystem));
                    }
                }
            }
        }

        public bool FollowBySystem
        {
            get
            {
                return OpenFrp.Launcher.Properties.Settings.Default.FollowSystemTheme;
            }
            set
            {
                if (value is true && Environment.OSVersion.Version.Major is 10 &&
                    App.Current?.MainWindow is { } wind)
                {
                    try
                    {
                        var uiSettings = new Windows.UI.ViewManagement.UISettings();

                        if (IsDarkBackground(uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background)))
                        {
                            App.RefreshApplicationTheme(wind, false);
                            Properties.Settings.Default.FollowSystemTheme = value;
                            OnPropertyChanged(nameof(ApplicationTheme));
                            return;
                        }
                    }
                    catch
                    {
                        // not support query
                        return;
                    }
                }
                else
                {
                    Properties.Settings.Default.FollowSystemTheme = false;
                }
            }
        }

        private bool IsDarkBackground(Windows.UI.Color color)
        {
            return color.R + color.G + color.B < (255 * 3 - color.R - color.G - color.B);
        }

        /// <summary>
        /// 显示登录窗口
        /// </summary>
        [RelayCommand]
        private async Task @event_ShowLoginDialog()
        {
            if (App.Current?.MainWindow is Window wind)
            {
                var tcs = new TaskCompletionSource<Awe.Model.OpenFrp.Response.Data.UserInfo>();

                wind.SetCurrentValue(Awe.UI.Helper.WindowsHelper.DialogOpennedProperty, true);
                wind.SetCurrentValue(Awe.UI.Helper.WindowsHelper.DialogContentProperty, new Dialog.LoginDialog
                {
                    DialogFallback = tcs
                });

                try
                {
                    var rti = await tcs.Task;
                    if (rti is not null)
                    {
                        WeakReferenceMessenger.Default.Send(UserInfo = rti);
                    }
                }
                catch (TaskCanceledException)
                {

                }

            }
        }

        [RelayCommand]
        private async Task @event_Logout()
        {
            var resp = await Service.Net.OAuthApp.Logout();

            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Exception is null)
            {
                var rrpc = await ExtendMethod.RunWithTryCatch(async () =>
                {
                    return await App.RemoteClient.LogoutAsync(new Service.Proto.Request.LogoutRequest
                    {
                        UserTag = this.UserInfo.UserID,
                    }, deadline: DateTime.UtcNow.AddMinutes(10));
                });

                if (rrpc is (var data,var ex))
                {
                    if (ex is not null)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    else if (data is not null)
                    {
                        if (data.Flag)
                        {
                            Service.Net.OpenFrp.Logout();
                            Properties.Settings.Default.UserPwn = string.Empty;
                            WeakReferenceMessenger.Default.Send(UserInfo = new Awe.Model.OpenFrp.Response.Data.UserInfo
                            {
                                UserName = "not-allow-display"
                            });
                        }
                        else
                        {
                            MessageBox.Show(data.Message);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(resp.Message);
            }
        }
    }
}
