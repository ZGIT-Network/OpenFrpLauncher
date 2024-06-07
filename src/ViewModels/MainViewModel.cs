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
                        foreach (var item in _navigationView.MenuItems)
                        {
                            if (item is NavigationViewItem vv && vv.Tag.Equals(data.Data))
                            {
                                _navigationView.SelectedItem = item;
                                break;
                            }
                        }
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
                        if (_frame is { Content: var c } &&
                            c is Views.Home or Views.Settings)
                        {
                            //event_RouterItemInvoked(c.GetType());
                        }
                    }
                });
            };
           // AvatorFilePath = Properties.Settings.Default.UserAvator;
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

        private void @event_OnUserInfoChanged()
        {
            if (UserInfo.UserName.Equals("not-allow-display"))
            {
                AvatorFilePath = "";

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
                    try
                    {
                        if (System.Text.Json.JsonSerializer.Deserialize<HashSet<int>>(Launcher.Properties.Settings.Default.AutoStartupTunnelId) is { Count: > 0 } tb)
                        {
                            var userTunnels = await Service.Net.OpenFrp.GetUserTunnels();
                            if (userTunnels.StatusCode is System.Net.HttpStatusCode.OK &&
                                userTunnels.Data is { Total: > 0 })
                            {
                                StringBuilder ob = new StringBuilder("[");

                                foreach (var item in userTunnels.Data!.List!)
                                {
                                    if (tb.Contains(item.Id))
                                    {
                                        // start up;
                                        ob.Append($"\"{System.Text.Json.JsonSerializer.Serialize(item).Replace("\"","\\\"")}\",");
                                    }
                                }
                                ob.Remove(ob.Length - 1, 1);
                                ob.Append("]");

                                var resp = await RpcManager.SyncAsync(new Service.Proto.Request.SyncRequest
                                {
                                    SecureCode = RpcManager.UserSecureCode,
                                    TunnelIdJson = ob.ToString(),
                                    UseDebug = Properties.Settings.Default.UseDebugMode,
                                    UseTlsEncrypt = Properties.Settings.Default.UseTlsEncrypt,
                                });
                                if (resp.IsSuccess)
                                {
                                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(tb.ToArray()));
                                }
                            }
                        }
                    
                    }
                    catch { }
                });
                ThreadPool.QueueUserWorkItem(async delegate
                {
                    var avator = await Service.Net.HttpRequest.Get($"https://api.zyghit.cn/avatar/?email={UserInfo.Email}&s=100");

                    if (avator.StatusCode is System.Net.HttpStatusCode.OK && avator.Data.Count() > 0)
                    {
                        Properties.Settings.Default.UserAvator = System.IO.Path.GetTempFileName();

                        try
                        {
                            using var fs = System.IO.File.OpenWrite(Properties.Settings.Default.UserAvator);

                            await fs.WriteAsync(avator.Data.ToArray(), 0, avator.Data.Count());



                            await fs.FlushAsync();
                        }
                        catch
                        {
                            Properties.Settings.Default.UserAvator = "";

                            return;
                        }

                        AvatorFilePath = Properties.Settings.Default.UserAvator;
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
            }
        }

        [RelayCommand]
        private void @event_RouterItemInvoked(NavigationViewItemInvokedEventArgs? value)
        { 
            if (_frame is not null)
            {
                if (value is { InvokedItemContainer.Tag: Type v })
                {
                    _frame.Navigate(v);
                }
                else if (value is { IsSettingsInvoked: true })
                {
                    _frame.Navigate(typeof(Views.Settings));
                }
            }
        }

        [RelayCommand]
        private void @event_NavigateToSettings()
        {
            if (_navigationView is not null)
            {
                _navigationView.SelectedItem = _navigationView.SettingsItem;
            }
            

            if (_frame?.Navigate(typeof(Views.Settings)) is true && _frame?.Content is Views.Settings { DataContext: ViewModels.SettingsViewModel v })
            {
                if ("not-allow-display".Equals(UserInfo.UserName))
                {
                    _ = v.event_ShowLoginDialogCommand.ExecuteAsync(null);
                }
            }
        }

        [RelayCommand]
        private void @event_FrameLoaded(RoutedEventArgs e)
        {
            if (_frame is null && e.Source is ModernWpf.Controls.Frame cc)
            {
                _frame = cc;
                cc.Navigate(typeof(Views.Home));
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
