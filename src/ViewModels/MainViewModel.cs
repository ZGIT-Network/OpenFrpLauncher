using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using CommunityToolkit.Mvvm.Messaging.Messages;
using OpenFrp.Launcher.Model;


namespace OpenFrp.Launcher.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            if (!WeakReferenceMessenger.Default.IsRegistered<Awe.Model.OpenFrp.Response.Data.UserInfo>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<Awe.Model.OpenFrp.Response.Data.UserInfo>(nameof(MainViewModel), (_, info) => UserInfo = info);
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<OpenFrp.Launcher.Model.UpdateInfo>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<OpenFrp.Launcher.Model.UpdateInfo>(nameof(MainViewModel), (_, data) => UpdateInfo = data);
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<Awe.Model.OpenFrp.Response.Data.UserTunnel>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<Awe.Model.OpenFrp.Response.Data.UserTunnel>(nameof(MainViewModel), (_, info) =>
                {
                    OnlineTunnels.Add(info.Id);
                });
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<Type>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<Type>(nameof(MainViewModel), (_, page) =>
                {
                    event_RouterItemInvoked(page);
                });
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<int[]>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<int[]>(nameof(MainViewModel), (_, data) =>
                {
                    OnlineTunnels.Clear();

                    OnlineTunnels.AddRange(data);
                });
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<string>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<string>(nameof(MainViewModel), (_, data) =>
                {
                    switch (data)
                    {
                        case "onService":
                            {
                                StateOfService = true;

                                break;
                            }
                        case "offService":
                            {
                                StateOfService = false;

                                break;
                            }
                    }
                });
            }
            this.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(UserInfo))
                {
                    event_OnUserInfoChanged();
                }
            };
        }

        public List<int> OnlineTunnels { get; } = new List<int>();

        private ContentControl? _frame;

        public RouterItem[] MainRouterItems { get; } = new RouterItem[]
        {
            new RouterItem
            {
                Icon = App.Current.TryFindResource("Awe.UI.Icons.Home") as Geometry,
                Title = "首页",
                Page = typeof(Views.Home)
            },
            new RouterItem
            {
                Icon = App.Current.TryFindResource("Awe.UI.Icons.WirelessBroadcast") as Geometry,
                Title = "隧道",
                Page = typeof(Views.Tunnels),
                IsEnableBinding = new Awe.UI.Helper.TwiceBindingHelper
                {
                    Property = RadioButton.IsEnabledProperty,
                    Binding = new Binding
                    {
                        Mode = BindingMode.OneWay,
                        Path = new PropertyPath("DataContext.UserInfo.UserName"),
                        Converter = new Awe.UI.Converter.NotEqualConverter(),
                        ConverterParameter = "not-allow-display",
                        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor,typeof(ItemsControl),1)
                    }
                }
            },
            new RouterItem
            {
                Icon = App.Current.TryFindResource("Awe.UI.Icons.Report") as Geometry,
                Title = "日志",
                Page = typeof(Views.Log),
            },
            new RouterItem
            {
                Icon = App.Current.TryFindResource("Awe.UI.Icons.Info") as Geometry,
                Title = "关于",
                Page = typeof(Views.About)
            },
        };

        public RouterItem[] SubRouterItems { get; } = new RouterItem[]
        {
            new RouterItem
            {
                Icon = App.Current.TryFindResource("Awe.UI.Icons.Setting") as Geometry,
                Title = "设置",
                Page = typeof(Views.Settings)
            },
        };

        [ObservableProperty]
        private Awe.Model.OpenFrp.Response.Data.UserInfo userInfo = new Awe.Model.OpenFrp.Response.Data.UserInfo
        {
            UserName = "not-allow-display"
        };

        [ObservableProperty]
        private ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel> userTunnels = new ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel>();

        [ObservableProperty]
        private OpenFrp.Launcher.Model.UpdateInfo updateInfo = new UpdateInfo
        {
            Type = UpdateInfoType.None
        };

        [ObservableProperty]
        private bool stateOfService;

        private void @event_OnUserInfoChanged()
        {
            if (UserInfo.UserName.Equals("not-allow-display"))
            {
                UserTunnels.Clear();
            }
            else
            {
                //var resp = await Service.Net.OpenFrp.GetUserTunnels();
                //if (resp.Exception is null && resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is not null)
                //{
                //    UserTunnels = new ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel>(resp.Data.List);
                //}
                //else
                //{
                //    global::System.Diagnostics.Debugger.Break();
                //}
            }
        }

        [RelayCommand]
        private async Task @event_RequesetForUpdate()
        {
            if (UpdateInfo.Type is UpdateInfoType.None) return;
            var dialog = new Dialog.MessageDialog
            {
                Title = new TextBlock()
                {
                    Text = UpdateInfo.Type switch {
                        UpdateInfoType.Launcher => "启动器更新",
                        UpdateInfoType.FrpClient => "FRPC 更新",
                        _ => "Unknown Update"
                    },
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontSize = 24
                },
                Content = new TextBlock
                {
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap,
                    Text = UpdateInfo.Log,
                }
            };
            await dialog.ShowDialog(); 
            
        }

        [RelayCommand]
        private void @event_RouterItemInvoked(Type? value)
        { 
            if (_frame is not null && value is not null)
            {
                _frame.Content = Activator.CreateInstance(value);
                if (App.Current.TryFindResource("FrameChangeAnimation") is Storyboard sb)
                {
                    _frame.BeginStoryboard(sb);
                }
            }
        }

        [RelayCommand]
        private void @event_FrameLoaded(RoutedEventArgs e)
        {
            if (_frame is null && e.Source is ContentControl cc) _frame = cc;
        }

        [RelayCommand]
        private void @event_CloseingWindow(System.ComponentModel.CancelEventArgs c)
        {
            App.Current.MainWindow.Visibility = Visibility.Hidden;
            c.Cancel = true;
        }

        private static T CreateObject<T>(Action<T>? func = default, params object[] args)
        {
            var vc = Activator.CreateInstance(typeof(T), args);

            if (vc is null) throw new NullReferenceException();
            else if (vc is T tt)
            {
                if (func is not null) { func(tt); }
                return tt;
            }
            throw new TypeLoadException();
        }
    }
}
