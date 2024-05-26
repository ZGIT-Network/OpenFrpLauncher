using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ModernWpf.Controls;
using OpenFrp.Launcher.Model;
using FileManager = OpenFrp.Service.Commands.FileDictionary;



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


                dx.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(UserInfo))
                    {
                        UserInfo = dx.UserInfo;
                        OnPropertyChanged(nameof(UserInfo));
                    }
                };
                
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
                    Property = Run.TextProperty,
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
                    Property = Run.TextProperty,
                    Binding = mb
                }
            };
        }

        #endregion


        
        public int ApplicationTheme
        {
            get
            {
                if (App.Current is { MainWindow: var wind })
                {
                    return (int)wind.GetValue(ModernWpf.ThemeManager.RequestedThemeProperty);
                }
                throw new NullReferenceException();
            }
            set
            {
                if (App.Current is { MainWindow: var wind })
                {
                    wind.SetValue(ModernWpf.ThemeManager.RequestedThemeProperty, (ModernWpf.ElementTheme)value);
                }
            }
        }
        public int FontHittingMode
        {
            get
            {
                return (int)Properties.Settings.Default.EnableTextAnimatedHitting;
            }
            set
            {
                Properties.Settings.Default.EnableTextAnimatedHitting = (TextHintingMode)value;
            }
        }
        public int NotifyMode
        {
            get
            {
                if (Properties.Settings.Default.NotifyMode is Model.NotifyMode.NotifyIconDefault)
                {
                    return 1;
                }
                return (int)Properties.Settings.Default.NotifyMode;
            }
            set
            {
                Properties.Settings.Default.NotifyMode = (Model.NotifyMode)value;
            }
        }
        public int ApplicationBackdrop
        {
            get
            {
                if (App.Current is { MainWindow: var wind })
                {
                    int vl = (int)wind.GetValue(ModernWpf.Controls.Primitives.WindowHelper.SystemBackdropTypeProperty);
                    if (vl is 1 or 0) return 0;
                    return  vl- 1;
                }
                throw new NullReferenceException();
            }
            set
            {
                if (App.Current is { MainWindow: var wind })
                {
                    wind.SetValue(ModernWpf.Controls.Primitives.WindowHelper.SystemBackdropTypeProperty, (ModernWpf.Controls.Primitives.BackdropType)(value+1));
                }
            }
        }
        public bool ZoomErrorMessage
        {
            get
            {
                return Properties.Settings.Default.ZoomErrorMessage;
            }
            set
            {
                Properties.Settings.Default.ZoomErrorMessage = value;
            }
        }

        public double FontSize
        {
            get
            {
                return Properties.Settings.Default.FontSize;
            }
            set
            {
                Properties.Settings.Default.FontSize = value;
            }
        }


        public System.Windows.Media.FontFamily FontFamily
        {
            get
            {
                return Properties.Settings.Default.FontFamily;
            }
            set
            {
                Properties.Settings.Default.FontFamily = value;
            }
        }


        public bool AutoStartup
        {
            get
            {
                return System.IO.File.Exists(FileManager.GetAutoStartupFile());
            }
            set
            {
                var fe = FileManager.GetAutoStartupFile();
                if (value)
                {
                    var shortcut = (IWshRuntimeLibrary.IWshShortcut)new IWshRuntimeLibrary.WshShell().CreateShortcut(fe);
                    shortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
                    shortcut.Arguments = "--minimize";
                    shortcut.Description = "OpenFrp Launcher v4 开机自启动";
                    shortcut.Save();
                }
                else
                {
                    try
                    {
                        System.IO.File.Delete(fe);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
            }
        }
        /// <summary>
        /// 显示登录窗口
        /// </summary>
        [RelayCommand]
        private async Task @event_ShowLoginDialog()
        {
            var loginDialog = new Dialog.LoginDialog();

            var result = await loginDialog.ShowAsync();

            if (result is not null)
            {
                WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(UserInfo = result));

                OpenFrp.Launcher.Properties.Settings.Default.Save();
            }

            
        }

        [RelayCommand]
        private async Task @event_Logout()
        {
            var resp = await Service.Net.OAuthApp.Logout();

            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Exception is null)
            {
                var reso = await RpcManager.LogoutAsync(new Service.Proto.Request.LogoutRequest
                {
                    SecureCode = RpcManager.UserSecureCode,
                }, TimeSpan.FromSeconds(10));

                if (reso.IsSuccess)
                {
                    Service.Net.OpenFrp.Logout();
                    Properties.Settings.Default.UserPwn = string.Empty;
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(UserInfo = new Awe.Model.OpenFrp.Response.Data.UserInfo
                    {
                        UserName = "not-allow-display"
                    }));
                }
                else
                {
                    MessageBox.Show(reso.Message ?? reso.Exception?.ToString());
                }
            }
            else
            {
                MessageBox.Show(resp.Message);
            }
        }

        [RelayCommand]
        private void @event_OpenOpenFrpWebsite()
        {
            try { Process.Start("http://console.openfrp.net/"); return; } catch { }
            try { Process.Start("start","http://console.openfrp.net/"); } catch { }
        }

        [RelayCommand]
        public void @event_NumberBoxLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is ModernWpf.Controls.NumberBox nb)
            {
                nb.ValidationMode = NumberBoxValidationMode.Disabled;
                nb.ValueChanged += (_, e) =>
                {
                    if (e.NewValue is double.NaN)
                    {
                        nb.Value = e.OldValue;
                    }
                };
            }
        }
    }
}
