using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    public partial class UserTunnel : ObservableObject,ICloneable
    {
        public UserTunnel(OpenFrp.Service.Proto.Response.TunnelStreamResponse.Types.AnonymousTunnelResponse.Types.AnonymousTunnelData an) : this(tunnel: new Yue3.Model.OpenFrp.Response.Data.UserTunnel
        {
            Name = an.Name,
            Id = an.TunnelId,
            ConnectAddress = an.ConnectAddresses,
            IsEnabled = true
        })
        {
            IsFastLaunch = true;
            LastLaunchTime = DateTimeOffset.Now;


            searchPatten ??= $"{an.Name},{an.TunnelId}";

        }


        public UserTunnel(Yue3.Model.OpenFrp.Response.Data.UserTunnel tunnel) 
        {
            Tunnel = tunnel;

#if NET
            searchPatten ??= $"{tunnel.Port},{tunnel.Name},{tunnel.Id},{tunnel.Type},{tunnel.NodeId},{(IsHasRemotePort ? tunnel.RemotePort : string.Join(',',tunnel.Domains))}";
#else
            searchPatten ??= $"{tunnel.Port},{tunnel.Name},{tunnel.Id},{tunnel.Type},{tunnel.NodeId},{(IsHasRemotePort ? tunnel.RemotePort : string.Join(",", tunnel.Domains))}";
#endif
        }


        public byte[] GetTunnelJsonBuffer()
        {
            if (Tunnel is null) return Array.Empty<byte>();

            return _buffer ??= JsonSerializer.SerializeToUtf8Bytes(Tunnel);
        }

        public string GetSearchPatten()
        {
            return searchPatten ?? "";
        }
        public void Update() => OnPropertyChanged(nameof(Tunnel));

        public object Clone()
        {
            return MemberwiseClone();
        }

        public UserTunnel ModelClone()
        {
            return (UserTunnel)Clone();
        }



        private string? searchPatten;
        private byte[]? _buffer;

        [ObservableProperty,NotifyPropertyChangedFor(nameof(SortStatusLevel),nameof(LastUpdateTimestamp),nameof(Name), nameof(Type), nameof(Id), nameof(Host), nameof(Port), nameof(RemotePort), nameof(NodeName), nameof(NodeId), nameof(IsEnable), nameof(UseEncryption), nameof(UseCompression), nameof(ConnectAddress), nameof(ExtraConnectAddress), nameof(Domains),nameof(IsHasRemotePort),nameof(IsHttpService),nameof(HasExtraConnectAddress))]
        private Yue3.Model.OpenFrp.Response.Data.UserTunnel? tunnel;

        [ObservableProperty,NotifyPropertyChangedFor(nameof(SortStatusLevel))]
        private bool firstState;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(SortStatusLevel))]
        private bool isFastLaunch;


        public int SortStatusLevel { get => IsEnable ? (FirstState ? 2 : 0) : -1; }

        public string Name { get => Tunnel?.Name ?? throw new NullReferenceException(nameof(Tunnel)); }
        public string Type { get => IsFastLaunch ? "" : Tunnel?.Type ?? throw new NullReferenceException(nameof(Tunnel)); }
        public int Id { get => Tunnel?.Id ?? throw new NullReferenceException(nameof(Tunnel)); }
        public string Host { get => IsFastLaunch ? "" : Tunnel?.Host ?? throw new NullReferenceException(nameof(Tunnel)); }
        public int Port { get => IsFastLaunch ? -1 : Tunnel?.Port ?? throw new NullReferenceException(nameof(Tunnel)); }
        public int RemotePort { get => IsFastLaunch ? -1 : Tunnel?.RemotePort ?? throw new NullReferenceException(nameof(Tunnel)); }

        public bool IsHasRemotePort { get => IsFastLaunch ? false : (Tunnel?.RemotePort ?? throw new NullReferenceException(nameof(Tunnel))) is not 0; }
        public bool IsHttpService { get => IsFastLaunch ? false : (Tunnel?.Type ?? throw new NullReferenceException(nameof(Tunnel))).Contains("HTTP") || !IsHasRemotePort; }

        public string NodeName { get => IsFastLaunch ? "" : Tunnel?.NodeName ?? throw new NullReferenceException(nameof(Tunnel)); }
        public int NodeId { get => IsFastLaunch ? -1 : Tunnel?.NodeId ?? throw new NullReferenceException(nameof(Tunnel)); }

        public ulong LastUpdateTimestamp { get => IsFastLaunch ? 0 : Tunnel?.LastUpdate ?? throw new NullReferenceException(nameof(Tunnel)); }


        public DateTimeOffset? LastLaunchTime { get; private set; } = null;


        public bool IsEnable { get => IsFastLaunch || (Tunnel?.IsEnabled ?? throw new NullReferenceException(nameof(Tunnel))); }
        public bool UseEncryption { get => !IsFastLaunch && (Tunnel?.UseEncryption ?? throw new NullReferenceException(nameof(Tunnel))); }
        public bool UseCompression { get => !IsFastLaunch && (Tunnel?.UseCompression ?? throw new NullReferenceException(nameof(Tunnel))); }
        public bool ProxyProtocolVersion2 { get => !IsFastLaunch && (Tunnel?.ProxyProtocolVersion2 ?? throw new NullReferenceException(nameof(Tunnel))); }

        public string ConnectAddress
        {
            get
            {
                if (IsFastLaunch && Tunnel is { ConnectAddress: not null })
                {
                    return Tunnel.ConnectAddress;
                }
                if (Tunnel is not { ConnectAddress: not null,Type: not null })
                {
                    throw new NullReferenceException(nameof(Tunnel));
                }
                if ((Tunnel.Type.Contains("HTTP") || Tunnel.Type.Contains("http")) && !IsFastLaunch)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var dom in Tunnel.Domains)
                    {
                        sb.Append(dom);
                        sb.Append(',');
                    }
                    sb.Remove(sb.Length - 1, 1);

                    return sb.ToString();
                }
                else
                {
                    return Tunnel.ConnectAddress;
                }
            }
        }

        public string[] ExtraConnectAddress { get => IsFastLaunch ? Array.Empty<string>() : Tunnel?.ExtraConnectAddress ?? throw new NullReferenceException(nameof(Tunnel)); }
        public string[] Domains { get => IsFastLaunch ? Array.Empty<string>() : Tunnel?.Domains.ToArray() ?? throw new NullReferenceException(nameof(Tunnel)); }

        public bool HasExtraConnectAddress { get => Tunnel?.ExtraConnectAddress.Length > 0; }

        public override string ToString()
        {
            return "隧道 #" + Id + " " + Name;
        }
    }
}
