using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    [INotifyPropertyChanged]
    internal partial class NodeInfo : Awe.Model.OpenFrp.Response.Data.NodeInfo
    {
        public NodeInfo() { }

        public NodeInfo(Awe.Model.OpenFrp.Response.Data.NodeInfo nt)
        {
            var tft = nt.GetType();
            var properties = typeof(Awe.Model.OpenFrp.Response.Data.NodeInfo).GetProperties();

            foreach (var item in properties)
            {
                if (item.CanWrite && item.CanRead)
                {
                    if (tft.GetProperty(item.Name) is { } pr)
                    {
                        item.SetValue(this, pr.GetValue(nt));
                    }
                }
            }
        }

        [ObservableProperty]
        [JsonIgnore]
        private int pressureLevel;
    }
}
