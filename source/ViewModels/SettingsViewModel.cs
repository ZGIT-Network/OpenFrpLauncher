using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

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
            if (Application.Current.MainWindow is { DataContext: MainWindowViewModel mv })
            {
                _mainWindowViewModel = mv;

                mv.PropertyChanged += (_, e) =>
                {
                    OnPropertyChanged(e.PropertyName);
                };
            }
        }

        public SettingsViewModel(LoginWindow _) : this()
        {
            IsAtLoginWindow = true;
        } 

        [ObservableProperty]
        private bool isAtLoginWindow = false;

        private readonly MainWindowViewModel? _mainWindowViewModel;

        public int ApplicationTheme
        {
            get => (int)App.Settings.ApplicationTheme;
            set
            {
                App.Settings.ApplicationTheme = (iNKORE.UI.WPF.Modern.ElementTheme)value;
                OnPropertyChanged(nameof(ApplicationTheme));
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

        [RelayCommand]
        private void @event_MessageTest()
        {

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
