using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Model
{
    internal class PortWaitData
    {
        public TaskCompletionSource<int>? PortWait { get; set; }

        public OpenFrp.Launcher.Model.UserTunnel? Tunnel { get; set; }
    }
}
