using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class TunnelConfEditor : ContentControl
    {
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
