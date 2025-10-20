using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class TunnelConfEditor : ContentControl
    {
        public bool IsServiceSelecting
        {
            get { return (bool)GetValue(IsServiceSelectingProperty); }
            set { SetValue(IsServiceSelectingPropertyKey, value); }
        }

        public void CancelServiceSelecting()
        {
            if (listView != null)
            {
                listView.ClearValue(ItemsControl.ItemsSourceProperty);
            }
            SetValue(IsServiceSelectingPropertyKey, false);
        }

        public static readonly DependencyPropertyKey IsServiceSelectingPropertyKey =
            DependencyProperty.RegisterReadOnly("IsServiceSelecting", typeof(bool), typeof(TunnelConfEditor), new PropertyMetadata(false, OnIsServiceSelectingPropertyChanged));

        protected static void OnIsServiceSelectingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TunnelConfEditor editor && e.NewValue is bool b)
            {
                editor.Conve_RefreshDetectedConnectionCommand.NotifyCanExecuteChanged();

                if (editor.GetTemplateChild("localServiceSelector") is Button btn)
                {
                    btn.IsEnabled = !b;
                }
                if (b)
                {
                    editor.Conve_RefreshDetectedConnectionCommand.Execute(default);
                }
                else
                {
                    editor.Conve_RefreshDetectedConnectionCommand.Cancel();
                }
            }
        }

        private bool CanExecuteRefreshDetectedConnection() => IsServiceSelecting;

        [RelayCommand(CanExecute = nameof(CanExecuteRefreshDetectedConnection),IncludeCancelCommand = true)]
        private async Task Conve_RefreshDetectedConnection(CancellationToken cancellationToken)
        {
            if (listView == null || suggestBox == null || connectionTypeSelector is null)
            {
                return;
            }

            listView.ItemsSource = null;
            connectionTypeSelector.SetValue(ComboBox.SelectedIndexProperty, 0);
            suggestBox.ClearValue(AutoSuggestBox.TextProperty);

            filterText = "";
            type = Service.Net.LocalConnectionSearch.LocalConnectonType.Unknown;

            var resp = await OpenFrp.Service.Net.LocalConnectionSearch.SearchConnection(cancellationToken);

            listView.Items.Filter = listView.Items.Filter;
            listView.ItemsSource = resp.Select(x => new Model.LocalConnection(x));
        }

        public static readonly DependencyProperty IsServiceSelectingProperty = IsServiceSelectingPropertyKey.DependencyProperty;

        private System.Windows.Controls.ListView? listView;
        private AutoSuggestBox? suggestBox;
        private ComboBox? connectionTypeSelector;

        public Model.Node Node
        {
            get { return (Model.Node)GetValue(NodeProperty); }
            set { SetValue(NodeProperty, value); }
        }
        public static readonly DependencyProperty NodeProperty =
            DependencyProperty.Register("Node", typeof(Model.Node), typeof(TunnelConfEditor), new PropertyMetadata(OnNodePropertyChanged));


        private static void OnNodePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not null && d is TunnelConfEditor fe)
            {
                if (fe.GetTemplateChild("typeSelector") is ComboBox cb)
                {
                    if (fe.GetValue(EditorTemplateProperty) is Model.TunnelEditorTemplate { SelectedTypeIndex: 0 })
                    {
                        cb.SetCurrentValue(ComboBox.SelectedIndexProperty, 0);
                    }
                }
            }
        }

        public Model.TunnelEditorTemplate EditorTemplate
        {
            get { return (Model.TunnelEditorTemplate)GetValue(EditorTemplateProperty); }
            set { SetValue(EditorTemplateProperty, value); }
        }

        
        public static readonly DependencyProperty EditorTemplateProperty =
            DependencyProperty.Register("EditorTemplate", typeof(Model.TunnelEditorTemplate), typeof(TunnelConfEditor), new PropertyMetadata(new Model.TunnelEditorTemplate { }));

        public Yue3.Model.OpenFrp.Request.ModifyTunnelRequest GetEditConfig()
        {
            FillEmptyBlank();
            RefreshInputPort();

            return EditorTemplate.GetEditConfig();
        }

        public Yue3.Model.OpenFrp.Request.ModifyTunnelRequest GetCreateConfig()
        {
            if (Node is null)
            {
                throw new NotSupportedException(nameof(GetCreateConfig));
            }

            FillEmptyBlank();
            RefreshInputPort();

            return EditorTemplate.GetCreateConfig(Node);
        }

        private void FillEmptyBlank()
        {
            if (EditorTemplate is not { } tp) return;

            if (string.IsNullOrEmpty(tp.Host))
            {
                tp.Host = "127.0.0.1";
            }
            if (string.IsNullOrEmpty(tp.Name))
            {
                EditorTemplate.SetRandomName();
            }
        }

        private void RefreshInputPort()
        {
            if (GetTemplateChild("localNumberBox") is NumberBox nb)
            {
                if (nb.IsFocused)
                {
                    Keyboard.ClearFocus();
                }
                if (string.IsNullOrEmpty(nb.Text))
                {
                    nb.Value = 25565;
                }
                if (nb.Value > ushort.MinValue && nb.Value < ushort.MaxValue)
                {
                    EditorTemplate.Port = (ushort)Math.Round(nb.Value);
                }
            }
            if (GetTemplateChild("remoteNumberBox") is NumberBox nt)
            {
                if (nt.IsFocused)
                {
                    Keyboard.ClearFocus();
                }
                if (string.IsNullOrEmpty(nt.Text) && Node is { PortRange: var ranger})
                {
                    nt.Value = ranger.GetRandomRemotePort();
                }
                if (nt.Value >= ushort.MinValue && nt.Value <= ushort.MaxValue)
                {
                    EditorTemplate.RemotePort = (ushort)Math.Round(nt.Value);
                }
            }
        }

        private OpenFrp.Service.Net.LocalConnectionSearch.LocalConnectonType type = Service.Net.LocalConnectionSearch.LocalConnectonType.Unknown;
        private string filterText = string.Empty;

        public override void OnApplyTemplate()
        {
            EditorTemplate.SelectedTypeIndex = 0;

            if (GetTemplateChild("localNumberBox") is NumberBox nb)
            {
                nb.NumberFormatter = new UshortFormatter();
                if (EditorTemplate.Port.HasValue && EditorTemplate.Port.Value > 0)
                {
                    nb.Text = EditorTemplate.Port.ToString();
                };
            }
            if (GetTemplateChild("remoteNumberBox") is NumberBox nt)
            {
                nt.NumberFormatter = new UshortFormatter();
                if (EditorTemplate.RemotePort.HasValue && EditorTemplate.RemotePort.Value > 0)
                {
                    nt.Text = EditorTemplate.RemotePort.ToString();
                }
                else if (Node is { PortRange: var ranger })
                {
                    nt.Value = ranger.GetRandomRemotePort();
                };
            }
            if (GetTemplateChild("randomNameBtn") is Button randomNameBtn)
            {
                randomNameBtn.Click += (_, e) =>
                {
                    EditorTemplate.SetRandomName();
                };
            }
            if (GetTemplateChild("randomRemotePortBtn") is Button randomRemortPortBtn)
            {
                randomRemortPortBtn.Click += delegate
                {
                    if (GetTemplateChild("remoteNumberBox") is NumberBox nt && Node != null)
                    {
                        var v = Node.PortRange.GetRandomRemotePort();

                        EditorTemplate.RemotePort = v;

                        nt.Value = (double)v;
                    }
                };
            }
            if (GetTemplateChild("typeSelector") is ComboBox cb)
            {
                EditorTemplate.SelectedTypeIndex = EditorTemplate.GetOrigianlType() switch
                {
                    "tcp" or "TCP" => 0,
                    "udp" or "UDP" => 1,
                    "http" or "HTTP" => 2,
                    "https" or "HTTPS" => 3,
                    _ => 0
                };

                cb.SetCurrentValue(ComboBox.SelectedIndexProperty, EditorTemplate.SelectedTypeIndex);
            }
            if (GetTemplateChild("localServiceSelector") is Button localServiceSelector)
            {
                localServiceSelector.Click += delegate
                {
                    IsServiceSelecting = true;
                };
            }
            if (GetTemplateChild("listView") is System.Windows.Controls.ListView lv)
            {
                listView = lv;

                lv.Items.Filter = item =>
                {
                    if (item is Model.LocalConnection ltc)
                    {
                        if (ltc.Type == Service.Net.LocalConnectionSearch.LocalConnectonType.Unknown || type == Service.Net.LocalConnectionSearch.LocalConnectonType.Unknown || ltc.Type == type)
                        {
                            if (string.IsNullOrEmpty(filterText))
                            {
                                return true;
                            }
#if NET
                            return ltc.ProcessName.Contains(filterText,StringComparison.OrdinalIgnoreCase);
#else
                            return ltc.ProcessName.IndexOf(filterText,StringComparison.OrdinalIgnoreCase) != -1;
#endif
                        }
                    }
                    return false;
                };
                lv.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, (RoutedEventHandler)((_, e) =>
                {
                    if (e.OriginalSource is not Button { DataContext: Model.LocalConnection localConnection }) { return; }

                    var endPoint = localConnection.GetIPEndPoint();


                    IPAddress mapped = endPoint.Address;

                    if (endPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        if (mapped.Equals(IPAddress.IPv6Any))
                        {
                            mapped = IPAddress.IPv6Loopback;
                        }
                    }
                    else if (mapped.Equals(IPAddress.Any))
                    {
                        mapped = IPAddress.Loopback;
                    }
                    EditorTemplate.Host = mapped.ToString();

                    if (GetTemplateChild("localNumberBox") is NumberBox nb)
                    {
                        nb.Text = (EditorTemplate.Port = (ushort)endPoint.Port).ToString();
                    }

                    CancelServiceSelecting();
                }));
            }
            if (GetTemplateChild("suggestBox") is AutoSuggestBox sb)
            {
                suggestBox = sb;

                sb.ClearValue(AutoSuggestBox.TextProperty);
                sb.TextChanged += (_, e) =>
                {
                    if (this.suggestBox is null || this.listView is null) return;
                    if (e.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                    {
                        return;
                    }
                    filterText = sb.Text;
                    listView.Items.Filter = listView.Items.Filter;
                };
            }
            if (GetTemplateChild("connectionTypeSelector") is ComboBox connectionTypeSelector)
            {
                this.connectionTypeSelector = connectionTypeSelector;

                connectionTypeSelector.SelectionChanged += (_, e) =>
                {
                    if (this.suggestBox is null || this.listView is null) return;
                    if (connectionTypeSelector.SelectedItem is not ComboBoxItem { Content: string type }) return;

                    this.type = type switch
                    {
                        "TCP" => Service.Net.LocalConnectionSearch.LocalConnectonType.TCP,
                        "UDP" => Service.Net.LocalConnectionSearch.LocalConnectonType.UDP,
                        _ => Service.Net.LocalConnectionSearch.LocalConnectonType.Unknown
                    };
                    listView.Items.Filter = listView.Items.Filter;
                };
            }

            base.OnApplyTemplate();
        }



        public class UshortFormatter : INumberBoxNumberFormatter
        {
            public string FormatDouble(double value)
            {
                if (double.IsNaN(value))
                {
                    return "";
                }
                else
                {
                    return value.ToString();
                }
            }

            public double? ParseDouble(string text)
            {
                if (ushort.TryParse(text,out var value))
                {
                    return value;
                }

                return null;
            }
        }
    }
}
