using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
using FileManager = OpenFrp.Service.Call.FileDictionary;



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
                StateOfService = dx.StateOfService;

                dx.PropertyChanged += (_, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(UserInfo):
                            {
                                UserInfo = dx.UserInfo;

                                goto case "updateVa";
                            }
                        case nameof(StateOfService):
                            {
                                StateOfService = dx.StateOfService;

                                goto case "updateVa";
                            }
                        case "updateVa":
                            {
                                OnPropertyChanged(e.PropertyName);
                                break;
                            };
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

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(event_ShowLoginDialogCommand))]
        private bool stateOfService;


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

        #region Settings Properties

        public bool AllowInstallOrUninstallService
        {
            get => Service.Call.WindowsServiceCall.AllowInstallOrUninstall;
        }
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
        public bool AllowLogTextWrap
        {
            get
            {
                return Properties.Settings.Default.AllowLogTextWrap;
            }
            set
            {
                Properties.Settings.Default.AllowLogTextWrap = value;
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
        public bool UseDebug
        {
            get
            {
                return Properties.Settings.Default.UseDebugMode;
            }
            set
            {
                Properties.Settings.Default.UseDebugMode = value;
            }
        }
        public bool UseTlsEncrypt
        {
            get
            {
                return Properties.Settings.Default.UseTlsEncrypt;
            }
            set
            {
                Properties.Settings.Default.UseTlsEncrypt = value;
            }
        }
        public bool UseProxy
        {
            get
            {
                return Properties.Settings.Default.UseProxy;
            }
            set
            {
                OpenFrp.Service.Net.HttpRequest.ProxyEditor(value);
                Properties.Settings.Default.UseProxy = value;
            }
        }
        public bool IsServiceInstall
        {
            get
            {
                return Service.Call.WindowsServiceCall.IsInstalledService();
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

        #endregion

        private bool CanExecuteLogin() => StateOfService;

        private bool CanInstallOrUninstallService() => Service.Call.WindowsServiceCall.AllowInstallOrUninstall;

        /// <summary>
        /// 显示登录窗口
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteLogin))]
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

        private async Task<Service.RpcResponse<Service.Proto.Response.SyncResponse>> UploadSetting(PropertyInfo? changedValue)
        {
            var se = new Service.Proto.Request.SyncRequest.Types.SyncSetting
            {
                UseDebug = UseDebug,
                UseProxy = UseProxy,
                UseTlsEncrypt = UseTlsEncrypt
            };
            if (changedValue is { Name: string name })
            {
                se.GetType().GetProperty(name).SetValue(se, changedValue.GetValue(this) is false);
            }
            return await RpcManager.SyncAsync(new Service.Proto.Request.SyncRequest
            {
                SecureCode = RpcManager.UserSecureCode,
                Setting = se
            });
        }

        [RelayCommand]
        private void @event_UploadSettingToggleSwitchLoaded(RoutedEventArgs arg)
        {
            
            if (arg.Source is ToggleSwitch @switch)
            {
                if (Service.Call.WindowsServiceCall.IsInstalledService())
                {
                    BindingOperations.ClearBinding(@switch, ToggleSwitch.IsOnProperty);

                    var property = GetType().GetProperty(@switch.Tag.ToString());

                    @switch.IsOn = property?.GetValue(this) is true;
                    @switch.Toggled += async delegate
                    {
                        if (!@switch.IsEnabled)
                        {
                            @switch.IsEnabled = true;
                            return;
                        }
                        @switch.IsEnabled = false;

                        var resp = await UploadSetting(property);
                        if (resp.IsSuccess && (-41404).Equals(resp.Data?.TunnelId.FirstOrDefault()))
                        {
                            property?.SetValue(this, @switch.IsOn);
                        }
                        else
                        {
                            MessageBox.Show(resp.Message, "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Error);

                            @switch.IsOn = !@switch.IsOn;

                            return;
                        }
                        @switch.IsEnabled = true;
                    };
                }
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
                    RpcManager.UserSecureCode = null;
                    Service.Net.OpenFrp.Logout();
                    Properties.Settings.Default.Account = new Service.UsrLogin();

                    
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

        [RelayCommand(CanExecute = nameof(CanInstallOrUninstallService))]
        private async Task @event_ServiceControl()
        {
            if (!IsServiceInstall)
            {
                // goto install
                var dialog = new ContentDialog
                {
                    Title = "安装服务",
                    Content = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Inlines =
                        {
                            new Run("请注意，安装服务会使守护进程及其子 FRPC 进程被结束，若您在使用远程桌面类程序，请慎重考虑是否安装。"),
                            new LineBreak(),
                            new Run("您的账户也会被退出。在重新打开启动器后,您需要重新登录。"),
                            new LineBreak(),
                            new Run("建议您保留"),
                            new Run("至少一款以上"){ FontWeight = FontWeights.Bold },
                            new Run("远程软件以防止失链。"),
                            new LineBreak(),
                            new Run("建议您保留"),
                            new Run("至少一款以上"){ FontWeight = FontWeights.Bold },
                            new Run("远程软件以防止失链。"),
                            new LineBreak(),
                        }
                    },
                    CloseButtonText = "取消",
                    PrimaryButtonText = "安装",
                    DefaultButton = ContentDialogButton.Primary
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {

                    App.Settings.Account = new Service.UsrLogin();
                    App.Settings.AutoStartupTunnelId = Array.Empty<int>();

                    App.Settings.Save();

                    App.ClearNotifications();
                    App.ClearWebviewRuntimeCache();

                    App.TokenSource.Cancel(false);

                    App.KillServiceProcess(true);

                    try
                    {
                        _ = Process.Start(new ProcessStartInfo(Assembly.GetAssembly(typeof(Service.RpcResponse)).Location, "service install")
                        {
                            Verb = "runas",
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true
                        });
                    }
                    catch
                    {

                    }
                    Environment.Exit(0);
                }
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "卸载服务",
                    Content = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Inlines =
                        {
                            new Run("请注意，卸载服务会使守护进程及其子 FRPC 进程被结束，若您在使用远程桌面类程序，请慎重考虑是否安装。"),
                            new LineBreak(),
                            new Run("建议您保留"),
                            new Run("至少一款以上"){ FontWeight = FontWeights.Bold },
                            new Run("远程软件以防止失链。"),
                            new LineBreak(),
                            new Run("建议您保留"),
                            new Run("至少一款以上"){ FontWeight = FontWeights.Bold },
                            new Run("远程软件以防止失链。"),
                            new LineBreak(),
                            new Run("(其实就是安装的反向操作，"),
                            new Run("可别说我水"){TextDecorations = TextDecorations.Strikethrough},
                            new Run(")"),
                            new LineBreak(),
                        }
                    },
                    CloseButtonText = "取消",
                    PrimaryButtonText = "卸载",
                    DefaultButton = ContentDialogButton.Primary
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {

                    //App.Settings.Account = new Service.UsrLogin();
                    App.Settings.AutoStartupTunnelId = Array.Empty<int>();

                    App.Settings.Save();

                    App.ClearNotifications();
                    App.ClearWebviewRuntimeCache();

                    App.TokenSource.Cancel(false);

                    //App.KillServiceProcess(true);

                    try
                    {
                        _ = Process.Start(new ProcessStartInfo(Assembly.GetAssembly(typeof(Service.RpcResponse)).Location, "service uninstall")
                        {
                            Verb = "runAs",
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true
                        });
                    }
                    catch { }

                    Environment.Exit(0);
                }
            }
        }

        [RelayCommand]
        private void @event_CloseFrpc() => App.KillFrpcProcess();
    }
}
