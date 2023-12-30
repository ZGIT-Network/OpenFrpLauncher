using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace OpenFrp.Launcher.Controls
{
    /// <summary>
    /// TunnelConfig.xaml 的交互逻辑
    /// </summary>
    public partial class TunnelConfig : Control
    {
        public TunnelConfig()
        {
            InitializeComponent();
        }



        public Awe.Model.OpenFrp.Response.Data.NodeInfo NodeInfo
        {
            get { return (Awe.Model.OpenFrp.Response.Data.NodeInfo)GetValue(NodeInfoProperty); }
            set { SetValue(NodeInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NodeInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeInfoProperty =
            DependencyProperty.Register("NodeInfo", typeof(Awe.Model.OpenFrp.Response.Data.NodeInfo), typeof(TunnelConfig), new PropertyMetadata(OnNodeInfoChanged));

        private static void OnNodeInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TunnelConfig tf &&
                e.NewValue is Awe.Model.OpenFrp.Response.Data.NodeInfo { ProtocolSupport: var ps })
            {
                if (ps is null) return;

                ProtocolItems[0].IsEnabled = ps.TCP;
                ProtocolItems[1].IsEnabled = ps.UDP;
                ProtocolItems[2].IsEnabled = ps.HTTP;
                ProtocolItems[3].IsEnabled = ps.HTTPS;

                if (tf.GetTemplateChild("tunnelTypeSelector") is ComboBox tb)
                {
                    tb.SelectedIndex = 0;
                    if (tb.GetBindingExpression(Awe.UI.Controls.RwComboBox.ItemsSourceProperty) is { } isa)
                    {
                        isa.UpdateTarget();
                        tf.ProtocolItemsView.Refresh();
                    }
                }
            }
        }



        public Awe.Model.OpenFrp.Response.Data.UserTunnel TunnelData
        {
            get { return (Awe.Model.OpenFrp.Response.Data.UserTunnel)GetValue(TunnelDataProperty); }
            set { SetValue(TunnelDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TunnelData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TunnelDataProperty =
            DependencyProperty.Register("TunnelData", typeof(Awe.Model.OpenFrp.Response.Data.UserTunnel), typeof(TunnelConfig), new PropertyMetadata(new Awe.Model.OpenFrp.Response.Data.UserTunnel { }));



        public bool IsCreateMode
        {
            get { return (bool)GetValue(IsCreateModeProperty); }
            set { SetValue(IsCreateModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsCreateMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCreateModeProperty =
            DependencyProperty.Register("IsCreateMode", typeof(bool), typeof(TunnelConfig), new PropertyMetadata(true));




        public ListCollectionView ProtocolItemsView { get; } = new ListCollectionView(ProtocolItems);


        public static Model.ProtocolItem[] ProtocolItems { get;} = new Model.ProtocolItem[]
        {
            new()
            {
                Title = "TCP",
                Description = "适用于 Minecraft Java 版,大部分游戏联机,微软远程桌面,SSH",
            },
            new()
            {
                Title = "UDP",
                Description = "适用于 数据组播 (如Speakteam)"
            },
            new()
            {
                Title = "HTTP",
                Description = "适用于 搭建网站",
            },
            new()
            {
                Title = "HTTPS",
                Description = "适用于 搭建网站(加密链接)",
            },
        };

        public Model.ProtocolVersionItem[] ProtocolVersionItems { get; } = new Model.ProtocolVersionItem[]
        {
            new Model.ProtocolVersionItem
            {
                Title = "不启用",
                Value = "off"
            },
            new Model.ProtocolVersionItem
            {
                Title = "Version 1",
                Value = "v2"
            },
            new Model.ProtocolVersionItem
            {
                Title = "Version 2",
                Value = "v2"
            }
        };

        public override void OnApplyTemplate()
        {
            if (TunnelData.TunnelCustomConfig is not null)
            {
                if (GetTemplateChild("rwPpv") is Awe.UI.Controls.RwComboBox v)
                {
                    var reg = new Regex("proxy_protocol_version(\\s)?=(\\s)?(v[0-2])?(off)?(;)?(\\n)?");

                    if (reg.Match(TunnelData.TunnelCustomConfig) is { Success: true } vk)
                    {
                        var va = vk.Value.Replace("\n", "").Split('=').ElementAt(1).Replace(" ", "");

                        v.SelectedIndex = va switch
                        {
                            "v1" => 1,
                            "v2" => 2,
                            _ => 0
                        };

                        int indexOffset = 0;
                        int conOffset = 0;
                        if (vk.Index > 2)
                        {
                            var symbol = TunnelData.TunnelCustomConfig.Substring(vk.Index - 1, 1);

                            if (symbol is "\n") { indexOffset = -(conOffset = 1); }
                        }
                        TunnelData.TunnelCustomConfig = TunnelData.TunnelCustomConfig.Remove(vk.Index + indexOffset, vk.Length + conOffset);
                        
                        if (GetTemplateChild("customPara") is TextBox tb)
                        {
                            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                        }
                    }
                }
                
            }
            if (TunnelData.Domains.Count > 0)
            {
                if (GetTemplateChild("domainHost") is TextBox tb)
                {
                    tb.TextChanged += delegate
                    {
                        tb.GetBindingExpression(Awe.UI.Helper.ControlHelper.HeaderProperty)?.UpdateTarget();
                    };
                };
            }
            base.OnApplyTemplate();
        }

        public Awe.Model.OpenFrp.Response.Data.UserTunnel GetConfig()
        {
            if (GetTemplateChild("rwPpv") is Awe.UI.Controls.RwComboBox { SelectedValue: Model.ProtocolVersionItem pvi })
            {
                if (!"off".Contains(pvi.Value))
                {
                    TunnelData.TunnelCustomConfig += $"{(TunnelData.TunnelCustomConfig is not null and { Length: > 0 } ? '\n' : "")}proxy_protocol_version = {pvi.Value}";
                }
            }
            return TunnelData;
        }

        public Awe.Model.OpenFrp.Response.Data.UserTunnel GetCreateConfig()
        {
            if (NodeInfo is null)
            {
                throw new NullReferenceException("只有先选择 NodeInfo 后，才可获取创建配置内容。");
            }

            if (TunnelData.Name is null || TunnelData.Name.Length is 0)
            {
                TunnelData.Name = $"ofPr_{Guid.NewGuid()}".Substring(0, 12);

                UpdateTunnelName();
            }
            if (TunnelData.Port is 0)
            {
                TunnelData.Port = 25565;
            }

            

            if (TunnelData.RemotePort <= 0)
            {
                TunnelData.RemotePort = NodeInfo.AllowPortRange.GetRandomRemotePort();
            }
            UpdateTunnelRemortPort();

            if (TunnelData.Port <= 0)
            {
                TunnelData.Port = 25565;
            }
            UpdateTunnelLocalPort();

            if (string.IsNullOrEmpty(TunnelData.Host) || "localhost".Equals(TunnelData.Host))
            {
                TunnelData.Host = "127.0.0.1";
            }
            UpdateTunnelHost();

            if (string.IsNullOrEmpty(TunnelData.Type))
            {
                TunnelData.Type = "TCP";
            }
            UpdateTunnelType();

            if (GetTemplateChild("rwPpv") is Awe.UI.Controls.RwComboBox { SelectedValue: Model.ProtocolVersionItem pvi })
            {
                if (!"off".Contains(pvi.Value))
                {
                    TunnelData.TunnelCustomConfig += $"{(TunnelData.TunnelCustomConfig is not null and { Length: > 0 } ? '\n' : "")}proxy_protocol_version = {pvi.Value}";
                }
            }

            TunnelData.Id = -1;

            TunnelData.NodeId = NodeInfo.Id;

            return TunnelData;
        }

        public void UpdateTunnelName() => UpdateTunnelControl("tunnelNameHost", TextBox.TextProperty);
        public void UpdateTunnelHost() => UpdateTunnelControl("tunnelHost", TextBox.TextProperty);
        public void UpdateTunnelRemortPort() => UpdateTunnelControl("tunnelRemotePort", TextBox.TextProperty);
        public void UpdateTunnelLocalPort() => UpdateTunnelControl("tunnelLocalPort", TextBox.TextProperty);
        public void UpdateTunnelType() => UpdateTunnelControl("tunnelTypeSelector", Awe.UI.Controls.RwComboBox.SelectedValueProperty);

        public void UpdateTunnelControl(string name,DependencyProperty dp)
        {
            if (GetTemplateChild(name) is FrameworkElement tb)
            {
                if (tb.GetBindingExpression(dp) is { } vt)
                {
                    vt.UpdateTarget();
                }
            }
        }
    }
}
