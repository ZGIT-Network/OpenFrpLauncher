using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using ModernWpf.Controls;

namespace OpenFrp.Launcher.Controls
{
    /// <summary>
    /// 隧道编辑器
    /// </summary>
    internal class TunnelEditor : UserControl
    {



        public bool IsCreateMode
        {
            get { return (bool)GetValue(IsCreateModeProperty); }
            set { SetValue(IsCreateModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsCreateMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCreateModeProperty =
            DependencyProperty.Register("IsCreateMode", typeof(bool), typeof(TunnelEditor), new PropertyMetadata(false));



        #region Event PortImportChanged
        public static readonly RoutedEvent PortImportChangedEvent = EventManager.RegisterRoutedEvent(
               "PortImportChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TunnelEditor));

        public event RoutedEventHandler PortImportChanged
        {
            add { AddHandler(PortImportChangedEvent, value); }
            remove { RemoveHandler(PortImportChangedEvent, value); }
        }
#endregion
        #region Property NodeInfo
        public Awe.Model.OpenFrp.Response.Data.NodeInfo NodeInfo
        {
            get { return (Awe.Model.OpenFrp.Response.Data.NodeInfo)GetValue(NodeInfoProperty); }
            set { SetValue(NodeInfoProperty, value); }
        }
        public static readonly DependencyProperty NodeInfoProperty =
            DependencyProperty.Register("NodeInfo", typeof(Awe.Model.OpenFrp.Response.Data.NodeInfo), typeof(TunnelEditor), new PropertyMetadata());
#endregion
        #region Property Tunnel
        public Awe.Model.OpenFrp.Response.Data.UserTunnel Tunnel
        {
            get { return (Awe.Model.OpenFrp.Response.Data.UserTunnel)GetValue(TunnelProperty); }
            set { SetValue(TunnelProperty, value); }
        }
        public static readonly DependencyProperty TunnelProperty =
            DependencyProperty.Register("Tunnel", typeof(Awe.Model.OpenFrp.Response.Data.UserTunnel), typeof(TunnelEditor), new FrameworkPropertyMetadata()
            {
                DefaultValue = new Awe.Model.OpenFrp.Response.Data.UserTunnel
                {
                    Type = "tcp",
                    Host = "127.0.0.1",
                    Port = 25565
                },
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
        #endregion
        #region Property IsPortImportOpen
        public bool IsPortImportOpen
        {
            get { return (bool)GetValue(IsPortImportOpenProperty); }
            set { SetValue(IsPortImportOpenProperty, value); }
        }

        public static readonly DependencyProperty IsPortImportOpenProperty =
            DependencyProperty.Register("IsPortImportOpen", typeof(bool), typeof(TunnelEditor), new PropertyMetadata(false, OnIsPortImportOpenChanged));

        private CancellationTokenSource cancellationTokenSource { get; set; } = new CancellationTokenSource();

        public static void OnIsPortImportOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TunnelEditor dt)
            {
                dt.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = PortImportChangedEvent
                });
                if (e.NewValue is true)
                {
                    dt.RefreshPortImport();
                }
                else
                {
                    if (dt.GetTemplateChild("FillterPortImport") is AutoSuggestBox asb)
                    {
                        asb.ClearValue(AutoSuggestBox.TextProperty);
                    }
                }
            }
        }
        #endregion


        private void RefreshHost()
        {
            if (GetTemplateChild("LocalhostInput") is TextBox tb)
            {
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            }
        }
        private void RefreshPort()
        {
            if (GetTemplateChild("LocalPortInput") is NumberBox nb)
            {
                nb.GetBindingExpression(NumberBox.ValueProperty)?.UpdateTarget();
            }
        }
        private void RefreshTunnelName()
        {
            if (GetTemplateChild("TunnelNameInput") is TextBox tb)
            {
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            }
        }
        private void RefreshRemotePort()
        {
            if (GetTemplateChild("RemotePortInput") is NumberBox nb)
            {
                nb.GetBindingExpression(NumberBox.ValueProperty)?.UpdateTarget();
            }
        }
        private void RefreshPortImport()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var dictionary = new Dictionary<int, string>();
            foreach (var item in Process.GetProcesses())
            {
                dictionary.Add(item.Id, item.ProcessName);
            }
            if (GetTemplateChild("listContainer") is ItemsControl isc)
            {
                isc.Items.Clear();

                isc.Dispatcher.Invoke(async () =>
                {
                    isc.AddHandler(Button.ClickEvent, new RoutedEventHandler((_,e) =>
                    {
                        if (e.OriginalSource is Button { Tag: LanLink.Proxy.Connection connection } btn)
                        {
                            Tunnel.Port = connection.Port;
                            Tunnel.Host = "127.0.0.1";
                            IsPortImportOpen = false;

                            RefreshPort();
                            RefreshHost();
                        }
                    }));
                    if (GetTemplateChild("FillterPortImport") is AutoSuggestBox asb)
                    {
                        asb.ClearValue(AutoSuggestBox.TextProperty);
                    }
                    var rs = await LanLink.Proxy.ConnectionSearch.GetConnectionAsync(Tunnel.Type is "UDP" ? LanLink.Proxy.ConnectionType.UDP : LanLink.Proxy.ConnectionType.TCP, cancellationTokenSource.Token);
                    foreach (var item in rs)
                    {
                        try { item.ProcessName = dictionary.TryGetValue(item.ProcessId, out string val) ? val : "[拒绝访问]"; }
                        catch { }
                        isc.Items.Add(item);

                        await Task.Delay(20);
                    } 
                });
            }
        }

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("OpenPortImport") is Button btn)
            {
                btn.Click += delegate
                {
                    IsPortImportOpen = true;
                };
            }
            if (GetTemplateChild("RefreshPortImport") is Button button2)
            {
                button2.Click += delegate { RefreshPortImport(); };
            }
            if (GetTemplateChild("FillterPortImport") is AutoSuggestBox asb)
            {
                asb.ClearValue(AutoSuggestBox.TextProperty);
                asb.TextChanged += (_, e) =>
                {
                    if (e.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
                    {
                        if (GetTemplateChild("listContainer") is ItemsControl isc)
                        {
                            if (string.IsNullOrEmpty(_.Text))
                            {
                                isc.Items.Filter = null;
                            }
                            else isc.Items.Filter = new Predicate<object>((x) =>
                            {
                                if (x is LanLink.Proxy.Connection { ProcessName: var n }) return n?.ToLower().Contains(_.Text.ToLower()) ?? false;
                                return false;
                            });
                        }
                    }
                };
            }
            if (GetTemplateChild("RandomTunnelName") is Button button)
            {
                button.Click += delegate
                {
                    Tunnel.Name = GetRandomName();
                    RefreshTunnelName();
                };
            }
            if (GetTemplateChild("LocalPortInput") is NumberBox nb)
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
            if (IsCreateMode)
            {
                if (GetTemplateChild("RandomRemotePort") is Button bb)
                {
                    Tunnel.RemotePort = NodeInfo.AllowPortRange.GetRandomRemotePort();
                    RefreshRemotePort();
                    bb.Click += delegate
                    {
                        Tunnel.RemotePort = NodeInfo.AllowPortRange.GetRandomRemotePort();
                        RefreshRemotePort();
                    };
                }
                if (GetTemplateChild("TunnelTypeSelector") is ComboBox cb)
                {
                    cb.SelectedIndex = 0;
                    Tunnel.Type = "tcp";
                }
                if (NodeInfo is { ProtocolSupport: var support } && support is not null)
                {
                    ProtocolMethod[0].IsEnabled = support.TCP;
                    ProtocolMethod[1].IsEnabled = support.UDP;
                    ProtocolMethod[2].IsEnabled = support.HTTP;
                    ProtocolMethod[3].IsEnabled = support.HTTPS;
                }
            }
            else
            {
                if (GetTemplateChild("TunnelTypeSelector") is ComboBox cb)
                {
                    cb.SelectedIndex = Tunnel.Type?.ToLower() switch
                    {
                        "tcp" => 0,
                        "udp" => 1,
                        "http" => 2,
                        "https" => 3,
                        _ => throw new NotSupportedException("不支持的隧道类型")
                    };
                }
            }
            base.OnApplyTemplate();
        }

        private const string Sytanx = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";

        public ObservableCollection<Model.ProtocolItem> ProtocolMethod { get; } = new ObservableCollection<Model.ProtocolItem>(new Model.ProtocolItem[]
        {
            new(){Title = "TCP"},
            new(){Title = "UDP"},
            new(){Title = "HTTP"},
            new(){Title = "HTTPS"}
        });

        public static string GetRandomName()
        {
            StringBuilder sb = new StringBuilder();
            Random rdm = new Random();

            for (int i = 0; i < 7; i++)
            {
                sb.Append(Sytanx[rdm.Next(0,Sytanx.Length - 1)]);
            }
            return "laC" + sb.ToString();
        }

        public Awe.Model.OpenFrp.Response.Data.UserTunnel GetCreateConfig()
        {
            var ob = Tunnel.CloneUserTunnel();

            if (string.IsNullOrEmpty(ob.Name))
            {
                ob.Name = Tunnel.Name = GetRandomName();
                RefreshTunnelName();
            }

            ob.NodeId = NodeInfo.Id;

            return ob;
        }

        public Awe.Model.OpenFrp.Response.Data.UserTunnel GetEditConfig()
        {
            var ob = Tunnel.CloneUserTunnel();

            if (string.IsNullOrEmpty(ob.Name))
            {
                ob.Name = Tunnel.Name = GetRandomName();
                RefreshTunnelName();
            }

            ob.IsEnabled = true;

            return ob;
        }
    }
}
