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

        public Node(Yue3.Model.OpenFrp.Response.Data.Node node) => NodeValue = node;

        [ObservableProperty,NotifyPropertyChangedFor(nameof(NodeName), nameof(Comments), nameof(NodeId), nameof(NeedRealname), nameof(ProtocolSupport), nameof(Group), nameof(FriendlyGroupRequire), nameof(Bandwidth), nameof(BandWidthScale), nameof(NodeClassify))]
        private Yue3.Model.OpenFrp.Response.Data.Node? nodeValue;

        public string NodeName { get => NodeValue?.Name ?? throw new NullReferenceException(nameof(NodeValue)); }
        public string Comments { get => NodeValue?.Comments ?? throw new NullReferenceException(nameof(NodeValue)); }
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
        public double Bandwidth { get => NodeValue?.BandWidth ?? throw new NullReferenceException(nameof(NodeValue)); }
        public double BandWidthScale { get => NodeValue?.BandWidthScale ?? throw new NullReferenceException(nameof(NodeValue)); }
        public Yue3.Model.OpenFrp.Response.Data.NodeClassify NodeClassify { get => NodeValue?.Classify ?? throw new NullReferenceException(nameof(NodeValue)); }
    }
}
