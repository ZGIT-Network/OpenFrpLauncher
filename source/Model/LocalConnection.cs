using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    internal partial class LocalConnection : ObservableObject
    {
        public LocalConnection()
        {

        }
        public LocalConnection(OpenFrp.Service.Net.LocalConnectionSearch.LocalConnection lc)
        {
            EndPointString = (endPoint = lc.EndPoint).ToString();
            ProcessName = lc.ProcessName;
            Type = lc.Type;
        }

        private readonly IPEndPoint endPoint = new IPEndPoint(IPAddress.None,0);

        public IPEndPoint GetIPEndPoint() => endPoint;

        [ObservableProperty]
        private string processName = string.Empty;

        [ObservableProperty]
        private string endPointString = string.Empty;

        [ObservableProperty]
        private OpenFrp.Service.Net.LocalConnectionSearch.LocalConnectonType type = Service.Net.LocalConnectionSearch.LocalConnectonType.Unknown;
    }
}
