using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    public partial class Node : ObservableObject
    {
        // 仅作 Example
        public Node()
        {

        }

        public Node(string name,Yue3.Model.OpenFrp.Response.Data.NodeClassify classify)
        {
            NodeValue = new Yue3.Model.OpenFrp.Response.Data.Node
            {
                Id = -1,
                Name = name,
                Classify = classify,
                Group = string.Empty,
                ProtocolSupport = new Yue3.Model.OpenFrp.Response.Data.NodeProtocolSupport { }
            };
            IsDiplayLabel = true;
        }

        public Node(Yue3.Model.OpenFrp.Response.Data.Node node) => NodeValue = node;

        public bool IsDiplayLabel { get; private set; }

        [ObservableProperty,NotifyPropertyChangedFor(nameof(NotAvaliable),nameof(Status),nameof(IsHttpsSupportOnly),nameof(IsHttpSupportOnly),nameof(IsHttpOrHttpsSupport),nameof(NodeName), nameof(Comments), nameof(NodeId), nameof(NeedRealname), nameof(ProtocolSupport), nameof(Group), nameof(FriendlyGroupRequire), nameof(Bandwidth), nameof(BandWidthScale), nameof(NodeClassify))]
        private Yue3.Model.OpenFrp.Response.Data.Node? nodeValue;

        public string ApDisplayName { get => $"#{NodeId} {NodeName}"; }

        public string NodeName { get => NodeValue?.Name ?? throw new NullReferenceException(nameof(NodeValue)); }
        public string? Comments { get => NodeValue?.Comments; }
        public bool HasComments { get => !string.IsNullOrEmpty(Comments); }
        public bool IsFullyLoaded { get => NodeValue?.IsFullyLoaded ?? throw new NullReferenceException(nameof(NodeValue)); }


        public int NodeId { get => NodeValue?.Id ?? throw new NullReferenceException(nameof(NodeValue)); }
        public bool NeedRealname { get => NodeValue?.NeedRealname ?? throw new NullReferenceException(nameof(NodeValue)); }
        public Yue3.Model.OpenFrp.Response.Data.NodeProtocolSupport ProtocolSupport { get => NodeValue?.ProtocolSupport ?? throw new NullReferenceException(nameof(NodeValue)); }
        public string Group { get => NodeValue?.Group ?? throw new NullReferenceException(nameof(NodeValue)); }
        public string FriendlyGroupRequire
        {
            get
            {
                if (!string.IsNullOrEmpty(Group) && !Group!.Contains("normal"))
                {
                    return Group.Contains(";svip") ? "VIP" : "SVIP";
                }
                return string.Empty;
            }
        }
        public bool IsHttpSupportOnly { get => ProtocolSupport.HTTP && !ProtocolSupport.HTTPS; }
        public bool IsHttpsSupportOnly { get => !ProtocolSupport.HTTP && ProtocolSupport.HTTPS; }
        public bool IsHttpOrHttpsSupport { get => ProtocolSupport.HTTP && ProtocolSupport.HTTPS; }

        public double Bandwidth { get => NodeValue?.BandWidth ?? throw new NullReferenceException(nameof(NodeValue)); }
        public double BandWidthScale { get => NodeValue?.BandWidthScale ?? throw new NullReferenceException(nameof(NodeValue)); }

        public string BandwidthWithScaleString { get => $"{Bandwidth} Mbps" + (BandWidthScale > 1 ? $" × {BandWidthScale}" : string.Empty); }

        public Yue3.Model.OpenFrp.Response.Data.NodeClassify NodeClassify { get => NodeValue?.Classify ?? throw new NullReferenceException(nameof(NodeValue)); }

        public System.Net.HttpStatusCode Status { get => NodeValue?.Status ?? throw new NullReferenceException(nameof(NodeValue)); }

        public Yue3.Model.OpenFrp.Response.Data.NodePortRange PortRange { get => NodeValue?.AllowPortRange ?? throw new NullReferenceException(nameof(NodeValue)); }

        public bool NotAvaliable { get => Status is not System.Net.HttpStatusCode.OK; }
    }
}
