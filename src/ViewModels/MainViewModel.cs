using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using H.NotifyIcon;
using ModernWpf;
using ModernWpf.Controls;
using ModernWpf.Navigation;
using OpenFrp.Launcher.Model;
using static Google.Protobuf.WellKnownTypes.Field.Types;


namespace OpenFrp.Launcher.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            if (!WeakReferenceMessenger.Default.IsRegistered<Model.RouteMessage<MainViewModel, Awe.Model.OpenFrp.Response.Data.UserInfo>>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<Model.RouteMessage<MainViewModel, Awe.Model.OpenFrp.Response.Data.UserInfo>>(nameof(MainViewModel), (_, info) => UserInfo = info.Data ?? throw new NullReferenceException());
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<Model.RouteMessage<MainViewModel, Model.UpdateInfo>>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<Model.RouteMessage<MainViewModel, Model.UpdateInfo>>(nameof(MainViewModel), (_, info) =>
                {
                    if (info.Data is not null)
                    {
                        UpdateInfo = info.Data;
                        if (_navigationView?.FooterMenuItems is { Count: 1 } footer &&
                            footer[0] is NavigationViewItem nvi)
                        {
                            nvi.GetBindingExpression(FrameworkElement.VisibilityProperty)?.UpdateTarget();
                        }
                    }
                });
                
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<Model.RouteMessage<MainViewModel, Awe.Model.OpenFrp.Response.Data.UserTunnel>>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<RouteMessage<MainViewModel, Awe.Model.OpenFrp.Response.Data.UserTunnel>>(nameof(MainViewModel), (_, info) =>
                {
                    OnlineTunnels.Add(info.Data?.Id ?? throw new NullReferenceException());
                });
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<RouteMessage<MainViewModel, Type>>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<RouteMessage<MainViewModel, Type>>(nameof(MainViewModel), (_, data) =>
                {
                    if (_navigationView != null) 
                    {
                        //foreach (var item in _navigationView.MenuItems)
                        //{
                        //    if (item is NavigationViewItem vv && vv.Tag.Equals(data.Data))
                        //    {
                        //        _navigationView.SelectedItem = item;
                        //        break;
                        //    }
                        //}
                        _frame?.Navigate(data.Data);
                    }
                });
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<RouteMessage<MainViewModel, int[]>>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<RouteMessage<MainViewModel, int[]>>(nameof(MainViewModel), (_, data) =>
                {
                    OnlineTunnels.Clear();

                    foreach (var item in data.Data ?? new int[0])
                    {
                        OnlineTunnels.Add(item);
                    }
                });
            }
            if (!WeakReferenceMessenger.Default.IsRegistered<RouteMessage<MainViewModel, string>>(nameof(MainViewModel)))
            {
                WeakReferenceMessenger.Default.Register<RouteMessage<MainViewModel, string>>(nameof(MainViewModel), (_, data) =>
                {
                    switch (data.Data)
                    {
                        case "onService":
                            {
                                if ("not-allow-display".Equals(UserInfo.UserName))
                                {
                                    App.TryAutoLogin();
                                }

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
                _frame?.RunOnUIThread(() =>
                {
                    if (e.PropertyName is nameof(UserInfo))
                    {
                        event_OnUserInfoChanged();
                    }
                });
            };
        }

        public HashSet<int> OnlineTunnels { get; } = new HashSet<int>();

        private ModernWpf.Controls.Frame? _frame;
        private ModernWpf.Controls.NavigationView? _navigationView;

        public NavigationView NavigationView
        {
            get => _navigationView ?? throw new NullReferenceException(nameof(_navigationView));
        }

        [ObservableProperty]
        private Awe.Model.OpenFrp.Response.Data.UserInfo userInfo = new Awe.Model.OpenFrp.Response.Data.UserInfo
        {
            UserName = "not-allow-display"
        };

        [ObservableProperty]
        private Model.UpdateInfo updateInfo = new UpdateInfo();

        [ObservableProperty]
        private ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel> userTunnels = new ObservableCollection<Awe.Model.OpenFrp.Response.Data.UserTunnel>();

        [ObservableProperty]
        private bool stateOfService;


        [ObservableProperty]
        private string avatorFilePath = @"pack://application:,,,/Resources/Images/share_5bb469267f65d0c7171470739108cdae.png";

        [RelayCommand]
        private void @event_CopyCActive()
        {
            if (System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.ListViewItem { DataContext: OpenFrp.Service.Proto.Response.LogResponse.Types.LogData log })
            {
                try
                {
                    Clipboard.SetText($"{DateTimeOffset.FromUnixTimeMilliseconds(log.Date):yyyy/MM/dd HH:mm:ss} {log.Executor} {log.Content} (Level: {log.Level})");
                }
                catch { }
            }
        }

        private void @event_OnUserInfoChanged()
        {
            if (UserInfo.UserName.Equals("not-allow-display"))
            {
                AvatorFilePath = "pack://application:,,,/Resources/Images/share_5bb469267f65d0c7171470739108cdae.png";

                _frame?.RemoveBackEntry();

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

               
                ThreadPool.QueueUserWorkItem(async delegate
                {
                    var avator = await Service.Net.HttpRequest.Get($"https://api.zyghit.cn/avatar/?email={UserInfo.Email}&s=100");

                    if (avator.StatusCode is System.Net.HttpStatusCode.OK && avator.Data.Count() > 0)
                    {
                        string ab = System.IO.Path.GetTempFileName();

                        try
                        {
                            using var fs = System.IO.File.OpenWrite(ab);
                            await fs.WriteAsync(avator.Data.ToArray(), 0, avator.Data.Count());
                            await fs.FlushAsync();
                        }
                        catch
                        {
                            ab = "";

                            return;
                        }
                        AvatorFilePath = ab;
                    }
                });
            }
        }

        [RelayCommand]
        private void @event_NavigationViewLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is NavigationView nv)
            {
                _navigationView = nv;

                nv.BackRequested += delegate
                {
                    
                    if (_frame is not null)
                    {
                        if (_frame.CanGoBack)
                        {
                            _frame.Tag = "goback";
                            _frame.NavigationService.GoBack();
                        }
                    }
                };
            }
        }

        [RelayCommand]
        private void @event_RouterItemInvoked(NavigationViewItemInvokedEventArgs? value)
        { 
            if (_frame is not null)
            {
                Type? tp = default;
                if (value is { InvokedItemContainer.Tag: Type v })
                {
                    tp = v;
                }
                else if (value is { IsSettingsInvoked: true })
                {
                    tp = typeof(Views.Settings);
                }
                if (tp != null)
                {
                    if (_frame.SourcePageType == tp)
                    {
                        switch (_frame.SourcePageType.FullName)
                        {
                            case "OpenFrp.Launcher.Views.Tunnels":
                                {
                                    WeakReferenceMessenger.Default.Send(RouteMessage<TunnelsViewModel>.Create("refresh"));
                                };break;
                            case "OpenFrp.Launcher.Views.CreateTunnel":
                                {
                                    WeakReferenceMessenger.Default.Send(RouteMessage<CreateTunnelViewModel>.Create("refresh"));
                                }; break;
                            case "OpenFrp.Launcher.Views.Home":
                                {
                                    WeakReferenceMessenger.Default.Send(RouteMessage<HomeViewModel>.Create("refresh"));
                                }; break;
                        }
                        //object message;
                        
                        //WeakReferenceMessenger.Default.Send(message);
                        

                        return;
                    }
                    _frame.Navigate(tp);
                }
            }
        }


        [RelayCommand]
        private void @event_NavigateToSettings()
        {
            if (_frame?.SourcePageType?.Equals(typeof(Views.Settings)) is true)
            {
                if (UserInfo.UserName.Equals("not-allow-display") && _frame.Content is Views.Settings { DataContext: ViewModels.SettingsViewModel s })
                {
                    s.event_ShowLoginDialogCommand.Execute(null);
                }

                return;
            }

            _frame?.Navigate(typeof(Views.Settings), "request-login");
        }

        [RelayCommand]
        private void @event_FrameLoaded(RoutedEventArgs e)
        {
            if (_frame is null && e.Source is ModernWpf.Controls.Frame cc)
            {
                cc.Navigate(typeof(Views.Home),"");

                _frame = cc;

                _frame.Navigating += (_, e) =>
                {
                    if ("goback".Equals(_frame.Tag))
                    {
                        _frame.ClearValue(ModernWpf.Controls.Frame.TagProperty);

                        return;
                    }
                    if (e.NavigationMode is System.Windows.Navigation.NavigationMode.Forward or System.Windows.Navigation.NavigationMode.Back)
                    {
                        e.Cancel = true;
                    }
                };
                _frame.Navigated += (_, e) =>
                {
                    
                    if (e.ExtraData is not null && !e.ExtraData.Equals("request-login")) return;

                    var tgt = e.SourcePageType();

                    if (tgt == typeof(Views.Settings))
                    {
                        NavigationView.SelectedItem = NavigationView.SettingsItem;

                        if (e.Parameter()?.Equals("request-login") is true
                            && UserInfo.UserName.Equals("not-allow-display")
                            && e.Content is Views.Settings { DataContext: ViewModels.SettingsViewModel s})
                        {
                            s.event_ShowLoginDialogCommand.Execute(null);
                        }

                        return;
                    }

                    if (NavigationView is { MenuItems: var v1,FooterMenuItems: var v2 })
                    {
                        

                        foreach (NavigationViewItemBase nvi1 in v1)
                        {
                            if (tgt.Equals(nvi1.Tag))
                            {
                                if (NavigationView.SelectedItem != nvi1)
                                {
                                    NavigationView.SelectedItem = nvi1;
                                }

                                return;
                            }
                        }
                        foreach (NavigationViewItemBase nvi2 in v2)
                        {
                            if (tgt.Equals(nvi2.Tag))
                            {
                                if (NavigationView.SelectedItem != nvi2)
                                {
                                    NavigationView.SelectedItem = nvi2;
                                }

                                return;
                            }
                        }
                    }
                };
                
            }
        }

        [RelayCommand]
        private void @event_CloseingWindow(System.ComponentModel.CancelEventArgs c)
        {
            if (App.Current is { MainWindow: var mw})
            {
                

                Properties.Settings.Default.ApplicationTheme = (ModernWpf.ElementTheme)App.Current.MainWindow.GetValue(ModernWpf.ThemeManager.RequestedThemeProperty);
                Properties.Settings.Default.ApplicationBackdrop = (ModernWpf.Controls.Primitives.BackdropType)App.Current.MainWindow.GetValue(ModernWpf.Controls.Primitives.WindowHelper.SystemBackdropTypeProperty);
                try
                {
                    Properties.Settings.Default.Save();
                }
                catch { }

                if (Debugger.IsAttached) return;

                mw.HideInTaskbar();
                mw.Hide();

                c.Cancel = true;
            }
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
