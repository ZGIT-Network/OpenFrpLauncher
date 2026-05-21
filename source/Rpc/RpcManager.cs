using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Grpc.Core.Logging;
using OpenFrp.Service.Daemon;
using Nito.AsyncEx;

namespace OpenFrp.Launcher.Rpc
{
    internal class RpcManager : OpenFrp.Service.Rpc.RpcManager
    {
        public RpcManager(ILogger<Service.Rpc.RpcManager> logger) : base(logger)
        {
        }
    }
}
