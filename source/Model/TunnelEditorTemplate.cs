using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Yue3.Model.OpenFrp.Request;

namespace OpenFrp.Launcher.Model
{
    public partial class TunnelEditorTemplate : ObservableObject
    {
        private string originalType = "tcp";

        private const string Sytanx = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789";

        [ObservableProperty]
        private string? domainString;

        [ObservableProperty]
        private string? name;

        [ObservableProperty]
        private string? apDisplayName;

        [ObservableProperty]
        private string? host;

        [ObservableProperty]
        private ushort? port;

        [ObservableProperty]
        private ushort? remotePort;

        [ObservableProperty]
        [NotifyPropertyChangedFor("IsWebService","IsNormalService")]
        private int selectedTypeIndex = 0;

        [ObservableProperty]
        private bool dataEncrypt;

        [ObservableProperty]
        private bool dataCompress;

        [ObservableProperty]
        private bool proxyProtocolVersion2;


        private Yue3.Model.OpenFrp.Request.ModifyTunnelRequest ModifyConfig { get; set; }

        public int Id => ModifyConfig.Id.GetValueOrDefault(-1);

        public bool IsWebService => SelectedTypeIndex >= 2;

        public bool IsNormalService => SelectedTypeIndex < 2;

        public TunnelEditorTemplate()
        {
            ModifyConfig = new ModifyTunnelRequest();
        }

        public TunnelEditorTemplate(Yue3.Model.OpenFrp.Response.Data.UserTunnel userTunnel)
        {
            Name = userTunnel.Name;
          
            if (userTunnel.Type == "HTTP" || userTunnel.Type == "HTTPS")
            {
                Port = userTunnel.Port;
                if (userTunnel.RemotePort >= 0 && userTunnel.RemotePort <= 65535)
                {
                    RemotePort = (ushort)userTunnel.RemotePort;
                }
            }
            Host = userTunnel.Host;
            Port = userTunnel.Port;
            DataEncrypt = userTunnel.UseEncryption;
            DataCompress = userTunnel.UseCompression;
            ProxyProtocolVersion2 = userTunnel.ProxyProtocolVersion2;
            ApDisplayName = $"#{userTunnel.NodeId} {userTunnel.NodeName}";

            switch (originalType = userTunnel.Type ?? "tcp")
            {
                case "tcp":
                case "TCP":
                    SelectedTypeIndex = 0;
                    break;
                case "udp":
                case "UDP":
                    SelectedTypeIndex = 1;
                    break;
                case "http":
                case "HTTP":
                    SelectedTypeIndex = 2;
                    break;
                case "https":
                case "HTTPS":
                    SelectedTypeIndex = 3;
                    break;
                default:
                    SelectedTypeIndex = 0;
                    break;
            }
            
             
            userTunnel.NodeName = null;
            userTunnel.ConnectAddress = null;
            userTunnel.ExtraConnectAddress = Array.Empty<string>();
            ModifyConfig = new ModifyTunnelRequest(userTunnel);
        }

        public TunnelEditorTemplate(OpenFrp.Launcher.Model.UserTunnel userTunnel)
            : this(userTunnel.Tunnel?.CloneUserTunnel() ?? throw new NullReferenceException("Tunnel"))
        {
            if (userTunnel.Type.Contains("http") || userTunnel.Type.Contains("HTTP"))
            {
                StringBuilder sb = new StringBuilder();
                string[] domains = userTunnel.Domains;
                foreach (string dom in domains)
                {
                    sb.Append(dom);
                    sb.Append(',');
                }
                sb.Remove(sb.Length - 1, 1);
                DomainString = sb.ToString();
            }
        }

        public ModifyTunnelRequest GetEditConfig()
        {
            ProcKrosame();
            ProcDomain();
            return ModifyConfig;
        }

        public ModifyTunnelRequest GetCreateConfig(OpenFrp.Launcher.Model.Node node)
        {
            if (!Port.HasValue)
            {
                throw new NotSupportedException();
            }
            ProcKrosame();
            ModifyConfig.NodeId = node.NodeId;
            ModifyConfig.RemotePort = RemotePort ?? node.PortRange.GetRandomRemotePort();
            ModifyTunnelRequest modifyConfig = ModifyConfig;

            string type = SelectedTypeIndex switch
            {
                1 => "udp",
                2 => "http",
                3 => "https",
                _ => "tcp",
            };

            modifyConfig.Type = type;
            ProcDomain();
            return ModifyConfig;
        }

        private void ProcKrosame()
        {
            if (!Port.HasValue)
            {
                throw new NotSupportedException();
            }
            ModifyConfig.UseCompression = DataCompress;
            ModifyConfig.UseEncryption = DataEncrypt;
            ModifyConfig.Name = (Name ??= GetRandomName());
            ModifyConfig.Port = Port.Value;
            ModifyConfig.Host = Host;
            ModifyConfig.ProxyProtocolVersionV2 = ProxyProtocolVersion2;
        }

        private void ProcDomain()
        {
            if (DomainString != null && IsWebService)
            {
                string[] dsr = DomainString.Split(',');
                string jsd = "[]";
                if (dsr.Length != 0)
                {
                    jsd = JsonSerializer.Serialize(dsr);
                }
                ModifyConfig.DomainsJsonString = jsd;
            }
        }

        public void SetRandomName()
        {
            Name = GetRandomName();
        }

        public string GetOrigianlType()
        {
            return originalType;
        }

        private string GetRandomName()
        {
            StringBuilder sb = new StringBuilder();
            Random rdm = new Random();
            for (int i = 0; i < 7; i++)
            {
                sb.Append(Sytanx[rdm.Next(0, Sytanx.Length - 1)]);
            }
            return sb.ToString();
        }
    }
}
