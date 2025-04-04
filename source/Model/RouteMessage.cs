using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;

namespace OpenFrp.Launcher.Model
{
    internal class RouteMessage<TViewModel,TData>
    {
        public TData? Data { get; set; }

        public static implicit operator TData?(RouteMessage<TViewModel,TData> msg)
        {
            return msg.Data;
        }
    }

    internal class RouteMessage<TViewModel>
    {
        public static RouteMessage<TViewModel, TData> Create<TData>(TData data)
        {
            return new RouteMessage<TViewModel, TData>() { Data = data };
        }

        public static void Send<TData>(TData data)
        {
            WeakReferenceMessenger.Default.Send(Create<TData>(data));
        }
    }
}
