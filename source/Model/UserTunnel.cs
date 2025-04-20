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

        public string Name { get => Tunnel?.Name ?? throw new NullReferenceException(nameof(Tunnel)); }
        public string Type { get => Tunnel?.Type ?? throw new NullReferenceException(nameof(Tunnel)); }
        public int Id { get => Tunnel?.Id ?? throw new NullReferenceException(nameof(Tunnel)); }
        public string Host { get => Tunnel?.Host ?? throw new NullReferenceException(nameof(Tunnel)); }
        public int Port { get => Tunnel?.Port ?? throw new NullReferenceException(nameof(Tunnel)); }
        public int RemotePort { get => Tunnel?.RemotePort ?? throw new NullReferenceException(nameof(Tunnel)); }

        public bool IsHasRemotePort { get => (Tunnel?.RemotePort ?? throw new NullReferenceException(nameof(Tunnel))) is not 0; }
        public bool IsHttpService { get => (Tunnel?.Type ?? throw new NullReferenceException(nameof(Tunnel))).Contains("HTTP") || !IsHasRemotePort; }

        public string NodeName { get => Tunnel?.NodeName ?? throw new NullReferenceException(nameof(Tunnel)); }
        public int NodeId { get => Tunnel?.NodeId ?? throw new NullReferenceException(nameof(Tunnel)); }

        public bool IsEnable { get => Tunnel?.IsEnabled ?? throw new NullReferenceException(nameof(Tunnel)); }
        public bool UseEncryption { get => Tunnel?.UseEncryption ?? throw new NullReferenceException(nameof(Tunnel)); }
        public bool UseCompression { get => Tunnel?.UseCompression ?? throw new NullReferenceException(nameof(Tunnel)); }

        public string ConnectAddress { get => Tunnel?.ConnectAddress ?? throw new NullReferenceException(nameof(Tunnel)); }
        public string[] ExtraConnectAddress { get => Tunnel?.ExtraConnectAddress ?? throw new NullReferenceException(nameof(Tunnel)); }
        public string[] Domains { get => Tunnel?.Domains.ToArray() ?? throw new NullReferenceException(nameof(Tunnel)); }

        public bool HasExtraConnectAddress { get => Tunnel?.ExtraConnectAddress.Length > 0; }
    }
}
