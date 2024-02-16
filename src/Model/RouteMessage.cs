using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Model
{
    internal class RouteMessage<TViewModel,TData>
    {
        public TData? Data { get; set; }
    }

    internal class RouteMessage<TViewModel>
    {
        public static RouteMessage<TViewModel, TData> Create<TData>(TData data)
        {
            return new RouteMessage<TViewModel, TData>() { Data = data };
        }
    }
}
