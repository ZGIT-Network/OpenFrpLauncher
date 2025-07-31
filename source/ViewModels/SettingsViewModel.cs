using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Google.Protobuf.WellKnownTypes;
using OpenFrp.Launcher.Model;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class SettingsViewModel : ObservableObject
    {
        internal static readonly Model.UserInfo __userInfo_Defualt = new Model.UserInfo(new Yue3.Model.OpenFrp.Response.Data.UserInfo
        {
            UserName = "non-display-user"
        });


        public SettingsViewModel()
        {
            if (Application.Current.MainWindow is { DataContext: MainWindowViewModel mv } v)
            {
                mvW = v;
                _mainWindowViewModel = mv;


                mv.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is "IsUrlSchemeRegistered") return;

                    OnPropertyChanged(e.PropertyName);

                    if (e.PropertyName is "UserInfo")
                    {
                        UpdateCurrentAutoLogonId();
                    }
                };

                UpdateCurrentAutoLogonId();
            }
            else if (Application.Current.MainWindow is LoginWindow lWnd)
            {
                mvW = lWnd;
                IsAtLoginWindow = true;
            }
        }

        private string? currentUserAutoLogonId;
        private string astLaunchFile = Service.Helpers.FileHelper.GetAutoStartupFile();

        [ObservableProperty]
        private bool isAtLoginWindow = false;

  
        public Uri? UserAvatorSource
        {
            get => _mainWindowViewModel?.UserAvatorSource;
        }

        private readonly Window? mvW;
        private readonly MainWindowViewModel? _mainWindowViewModel;

        public int ApplicationTheme
        {
            get => (int)App.Settings.ApplicationTheme;
            set
            {
                App.Settings.ApplicationTheme = (iNKORE.UI.WPF.Modern.ElementTheme)value;

                App.Settings.Save();

                OnPropertyChanged(nameof(ApplicationTheme));
            }
        }
        public int BackdropType
        {
            get
            {
                if (mvW != null)
                {
                    switch (iNKORE.UI.WPF.Modern.Controls.Helpers.WindowHelper.GetSystemBackdropType(mvW))
                    {
                        case iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType.Acrylic10 or iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType.Acrylic11:
                            {
                                return 2;
                            };
                        case iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType other:
                            {
                                return (int)other - 1;
                            }
                    }
                }
                switch (App.Settings.BackdropType)
                {
                    case iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType.Acrylic10 or iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType.Acrylic11:
                        {
                            return 2;
                        };
                    case iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType other:
                        {
                            return (int)other - 1;
                        }
                }
            }
            set
            {
                if (mvW != null)
                {
                    App.Settings.BackdropType = (iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType)(value + 1);

                    iNKORE.UI.WPF.Modern.Controls.Helpers.WindowHelper.SetSystemBackdropType(mvW, App.Settings.BackdropType);

                    App.Settings.Save();

                    OnPropertyChanged(nameof(BackdropType));
                }
            }
        }
        public int NotificationMode
        {
            get
            {
                return (int)App.Settings.NotificationMode;
            }
            set
            {
                App.Settings.NotificationMode = (Model.NotificationMode)value;
                OnPropertyChanged(nameof(NotificationMode));
            }
        }

        public bool BypassProxy
        {
            get => !App.Settings.UseProxy;
            set
            {
                App.Settings.UseProxy = !value;
                OnPropertyChanged(nameof(BypassProxy));
            }
        }
        public bool UseForceTls
        {
            get => App.Settings.UseForceTls;
            set
            {
                App.Settings.UseForceTls = value;
                OnPropertyChanged(nameof(UseForceTls));
            }
        }
        public bool UseConfigLaunch
        {
            get => App.Settings.UseConfigLaunch;
            set
            {
                App.Settings.UseConfigLaunch = value;
                OnPropertyChanged(nameof(UseConfigLaunch));
            }
        }
        public bool ShowTitlebarBackground
        {
            get => App.Settings.ShowTitlebarBackground;
            set
            {
                App.Settings.ShowTitlebarBackground = value;
                OnPropertyChanged(nameof(ShowTitlebarBackground));
            }
        }
        public bool UseDebug
        {
            get => App.Settings.UseDebug;
            set
            {
                App.Settings.UseDebug = value;
                OnPropertyChanged(nameof(UseDebug));
            }
        }
        public bool DoNotNoticeErrorMsg
        {
            get => App.Settings.DoNotNoticeErrorMsg;
            set
            {
                App.Settings.DoNotNoticeErrorMsg = value;
                OnPropertyChanged(nameof(DoNotNoticeErrorMsg));
            }
        }
        public bool DoNotNoticeAutoLaunchTunnelMsg
        {
            get => App.Settings.DoNotNoticeAutoLaunchTunnelMsg;
            set
            {
                App.Settings.DoNotNoticeAutoLaunchTunnelMsg = value;
                OnPropertyChanged(nameof(DoNotNoticeAutoLaunchTunnelMsg));
            }
        }
        public bool AutoLaunchWhenLogon
        {
            get
            {
                //return false;
                return System.IO.File.Exists(astLaunchFile);
            }
            set
            {
                //return;

                if (!value)
                {
                    try
                    {
                        System.IO.File.Delete(astLaunchFile);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "无法删除开机自启动文件，请手动删除:" +
                            $"\n路径: {astLaunchFile}" +
                            $"\nReason: {ex}", "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }
                var shellType = System.Type.GetTypeFromProgID("WScript.Shell");
                if (shellType is null)
                {
                    return;
                }
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell is null || Assembly.GetEntryAssembly() is not { Location: string location })
                {
                    return;
                }
                var shortcut = shell.CreateShortcut(astLaunchFile);

                shortcut.TargetPath = location;
                shortcut.Arguments = "--minimize";
                shortcut.WorkingDirectory = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                shortcut.Save();
            }
        }
        public bool AutoLogonWithCurrentUser
        {
            get
            {
                if (string.IsNullOrEmpty(currentUserAutoLogonId) || string.IsNullOrEmpty(App.Settings.AutoLoginId))
                {
                    return false;
                }
                return App.Settings.AutoLoginId.Equals(currentUserAutoLogonId);
            }
            set
            {
                if (!value)
                {
                    App.Settings.AutoLoginId = "";
                    return;
                }
                if (string.IsNullOrEmpty(currentUserAutoLogonId))
                {
                    return;
                }
                App.Settings.AutoLoginId = currentUserAutoLogonId;
            }
        }

        public bool IsUrlSchemeRegistered
        {
            get
            {
                if (_mainWindowViewModel is null) return Microsoft.Win32.Registry.ClassesRoot.GetSubKeyNames().Contains("openfrp");

                try
                {
                    if (Microsoft.Win32.Registry.ClassesRoot.GetSubKeyNames().Contains("openfrp"))
                    {
                        return _mainWindowViewModel.IsUrlSchemeRegistered = true;
                    }
                }
                catch
                {

                }

                return _mainWindowViewModel.IsUrlSchemeRegistered = false;
            }
        }


        [ObservableProperty]
        private ObservableCollection<FontDisplay>? fontDisplays;

        [ObservableProperty]
        private FontDisplay? selectedFont;

        public Model.UserInfo UserInfo
        {
            get
            {
                if (_mainWindowViewModel is not null)
                {
                    return _mainWindowViewModel.UserInfo;
                }
                return __userInfo_Defualt;
            }
        }

        private void UpdateCurrentAutoLogonId()
        {
            if (UserInfo.Equals(__userInfo_Defualt))
            {
                currentUserAutoLogonId = null;
            }
            else
            {
                currentUserAutoLogonId = Helpers.UsrTokenService.GetCurrentUserAutoLogonId(UserInfo.UserName);
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_CallUpLoginWindow(CancellationToken cancellationToken)
        {
            var lf = new LoginWindow(Application.Current.MainWindow);

            lf.WindowState = WindowState.Normal;

            var @value = await lf.LoginWndProcAsync(cancellationToken);

            if (@value is not null)
            {
                Model.RouteMessage<MainWindowViewModel>.Send(new Model.UserInfo(value));
            }
            GC.Collect();
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshUserInfo(CancellationToken cancellationToken)
        {
            var ruxe = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

            if (ruxe.Data is { } userInfo)
            {
                Model.RouteMessage<MainWindowViewModel>.Send(new Model.UserInfo(userInfo));
            }
        }


        [RelayCommand]
        private void @event_OnPicpicControlLoaded(RoutedEventArgs e)
        {
            if (e.Source is iNKORE.UI.WPF.Modern.Controls.PersonPicture pipc)
            {
                pipc.Dispatcher.UnhandledException += (_, e) =>
                {
                    if (e.Exception is NotSupportedException or BadImageFormatException)
                    {
                        e.Handled = true;
                    }
                };
                
                pipc.SetBinding(iNKORE.UI.WPF.Modern.Controls.PersonPicture.ProfilePictureProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath("UserAvatorSource"),
                    FallbackValue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/weavatar.png")),
                    Mode=  BindingMode.OneWay
                });
            }
        }
        //[RelayCommand]
        //private void @event_MessageTest()
        //{

        //}

        [RelayCommand]
        private void @event_UrlSchemeToggleSwitchLoaded(RoutedEventArgs e)
        {
            if (e.Source is iNKORE.UI.WPF.Modern.Controls.ToggleSwitch tg)
            {
                tg.Toggled += async (sender, args) =>
                {
                    if (!tg.IsEnabled) return;

                    tg.IsEnabled = false;
                    //args.Handled = true;
                    

                    
                    try
                    {
                        

                        var cpc = new ProcessStartInfo
                        {
                            FileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenFrp.Service.exe"),
                            Arguments = "--inst -type=reg " + tg.IsOn,
                            ErrorDialog = false,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        };
                        if (!App.IsAdministrator())
                        {
                            cpc.Verb = "runas";
                        }

                        Process.Start(cpc);

                        await Task.Delay(500);

                        if (tg.IsOn)
                        {
                            App.Settings.DoNotAskMeForUrlSchemeTools = true;
                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        OnPropertyChanged(nameof(IsUrlSchemeRegistered));
                        tg.IsEnabled = true;
                    }
                };
            }
        }

        [RelayCommand]
        private void @event_FontNumberOrFamilySelector(RoutedEventArgs e)
        {
            if (e.Source is iNKORE.UI.WPF.Modern.Controls.NumberBox nb)
            {
                nb.SetCurrentValue(iNKORE.UI.WPF.Modern.Controls.NumberBox.ValueProperty, App.Settings.LogFontSize);

                nb.ValueChanged += (e2,e3) =>
                {
                    if (e3.NewValue.Equals(double.NaN))
                    {
                        if (e3.OldValue.Equals(double.NaN)) nb.SetCurrentValue(iNKORE.UI.WPF.Modern.Controls.NumberBox.ValueProperty, 12d);
                        else nb.SetCurrentValue(iNKORE.UI.WPF.Modern.Controls.NumberBox.ValueProperty, e3.OldValue);

                        return;
                    }
                    App.Settings.LogFontSize = e3.NewValue;
                };
            }
            else if (e.Source is ComboBox cmb)
            {
                System.Windows.Markup.XmlLanguage userLang = System.Windows.Markup.XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

                var ff2233 = string.IsNullOrEmpty(App.Settings.LogFontFamily) ? null : new FontFamily(App.Settings.LogFontFamily);

                if (FontDisplays is null)
                {
                    FontDisplays = new ObservableCollection<Model.FontDisplay>();

                    foreach (var ffc in System.Windows.Media.Fonts.SystemFontFamilies)
                    {
                        var c2 = new Model.FontDisplay(ffc, ffc.FamilyNames.ContainsKey(userLang) ? ffc.FamilyNames[userLang] : ffc.FamilyNames.First().Value);

                        FontDisplays.Add(c2);

                        if (ff2233 != null && ff2233.Equals(ffc))
                        {
                            cmb.SetCurrentValue(ComboBox.SelectedValueProperty, SelectedFont = c2);
                        }
                    }

                    if (App.Current.TryFindResource("SourceCodeProMixYahei") is FontFamily ff) 
                    {
                        var dfc = new FontDisplay(ff, "默认 (Sora Medium + 微软雅黑)") { };

                        FontDisplays.Add(dfc);

                        if (string.IsNullOrEmpty(App.Settings.LogFontFamily) || ff.Equals(ff2233))
                        {
                            cmb.SetCurrentValue(ComboBox.SelectedValueProperty, SelectedFont = dfc);
                        }
                    }
                }

                cmb.SelectionChanged += delegate
                {
                    if (SelectedFont is not null)
                    {
                        if (SelectedFont.Name.Equals("默认 (Sora Medium + 微软雅黑)"))
                        {
                            App.Settings.LogFontFamily = null;
                        }
                        else
                        {
                            App.Settings.LogFontFamily = SelectedFont.Font.ToString();
                        }
                    }
                    //SelectedFont
                };
            }
        }

        [RelayCommand]
        private async Task @event_Logout()
        {
            if (App.RpcManager is null)
            {
                // todo
                return;
            }

            event_RefreshUserInfoCommand.Cancel();

            var rux1 = await App.RpcManager.Logout();

            if (!rux1.Flag)
            {
                // todo
                return;
            }

            Service.Net.OpenFrpApi.Logout();

            App.Settings.AutoLoginId = "";

            //try
            //{
            //    Helpers.UsrTokenService.RemoveUser(UserInfo.UserName, true);
            //}
            //catch { }

            Model.RouteMessage<MainWindowViewModel>.Send(__userInfo_Defualt);
        }
    }
}
