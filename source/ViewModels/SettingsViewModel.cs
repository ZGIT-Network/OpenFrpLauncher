using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
                    OnPropertyChanged(e.PropertyName);
                };
            }
            else if (Application.Current.MainWindow is LoginWindow lWnd)
            {
                mvW = lWnd;
                IsAtLoginWindow = true;

            }
        }

        //public SettingsViewModel(LoginWindow v) : this()
        //{
        //    mvW = v;

        //    IsAtLoginWindow = true;
        //} 

        [ObservableProperty]
        private bool isAtLoginWindow = false;

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

        public bool BypassProxy
        {
            get => !App.Settings.UseProxy;
            set
            {
                App.Settings.UseProxy = !value;
                OnPropertyChanged(nameof(BypassProxy));
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
            set
            {
                Model.RouteMessage<MainWindowViewModel>.Send(value);
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_CallUpLoginWindow(CancellationToken cancellationToken)
        {
            var lf = new LoginWindow(Application.Current.MainWindow);

            var @value = await lf.LoginWndProcAsync(cancellationToken);

            if (@value is not null)
            {
                UserInfo = new Model.UserInfo(value);
            }
            GC.Collect();
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshUserInfo(CancellationToken cancellationToken)
        {
            var ruxe = await OpenFrp.Service.Net.OpenFrpApi.GetUserInfo(cancellationToken);

            if (ruxe.Data is { } userInfo)
            {
                UserInfo = new Model.UserInfo(userInfo);
            }
        }

        //[RelayCommand]
        //private void @event_MessageTest()
        //{

        //}


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

                        if (App.Settings.LogFontFamily is null || ff.Equals(ff2233))
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

            try
            {
                var v = System.Text.Json.JsonSerializer.Deserialize<HashSet<Properties.Settings.UserProperty>>(App.Settings.Token);

                if (v is { Count: > 0 })
                {
                    v.RemoveWhere((x) => x.Equals(UserInfo));

                    App.Settings.Token = System.Text.Json.JsonSerializer.Serialize(v);
                    App.Settings.Save();
                }
            }
            catch { }

            UserInfo = __userInfo_Defualt;
        }
    }
}
