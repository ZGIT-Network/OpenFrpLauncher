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
        // 仅作 Example
        public UserTunnel(int ce)
        {
            /**
             * {
                "connectAddress": "kr-nc-bgp-1.ofalias.net:20495",
                "extAddress": [],
                "forceHttps": false,
                "friendlyNode": "韩国-春川",
                "id": 604966,
                "lastLogin": 1737985721000,
                "lastUpdate": 1719717293000,
                "localIp": "127.0.0.1",
                "localPort": 8006,
                "nid": 21,
                "nodeHostname": "kr-nc-bgp-1.ofalias.net",
                "online": false,
                "proxyName": "rJPZQlD",
                "proxyProtocolVersion": false,
                "proxyType": "tcp",
                "remotePort": 20495,
                "status": true,
                "uid": 1448,
                "useCompression": false,
                "useEncryption": false
            },
             */
            Tunnel = new Yue3.Model.OpenFrp.Response.Data.UserTunnel
            {
                Name = "dddd" + DateTimeOffset.Now.Millisecond,
                Type = "tcp",
                NodeId = 21,
                Host = "127.0.0.1",
                Port = 25552,
                Id = 14133 + ce,
                NodeName = "朝鲜-平壤",
                ConnectAddress = "kr-dadad.dd.dd"  + DateTimeOffset.Now.ToString(),
                RemotePort = 16661,
                IsEnabled = true
            };
        }

        public UserTunnel(OpenFrp.Service.Proto.Response.TunnelStreamResponse.Types.AnonymousTunnelResponse.Types.AnonymousTunnelData an) : this(tunnel: new Yue3.Model.OpenFrp.Response.Data.UserTunnel
        {
            Name = an.Name,
            Id = an.TunnelId,
            ConnectAddress = an.ConnectAddresses,
            IsEnabled = true
        })
        {
            IsFastLaunch = true;
        }


        public UserTunnel(Yue3.Model.OpenFrp.Response.Data.UserTunnel tunnel) => Tunnel = tunnel;

        public byte[] GetTunnelJsonBuffer()
        {
            if (Tunnel is null) return Array.Empty<byte>();

            return _buffer ??= JsonSerializer.SerializeToUtf8Bytes(Tunnel);
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

        private byte[]? _buffer;

        [ObservableProperty,NotifyPropertyChangedFor(nameof(Name), nameof(Type), nameof(Id), nameof(Host), nameof(Port), nameof(RemotePort), nameof(NodeName), nameof(NodeId), nameof(IsEnable), nameof(UseEncryption), nameof(UseCompression), nameof(ConnectAddress), nameof(ExtraConnectAddress), nameof(Domains),nameof(IsHasRemotePort),nameof(IsHttpService),nameof(HasExtraConnectAddress))]
        private Yue3.Model.OpenFrp.Response.Data.UserTunnel? tunnel;

        public bool FirstState { get; internal set; }

        [ObservableProperty,NotifyPropertyChangedFor(nameof(IsNotFastLaunch))]
        private bool isFastLaunch;

        public bool IsNotFastLaunch { get => !IsFastLaunch; }

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
