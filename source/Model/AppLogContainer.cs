using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    internal partial class AppLogContainer : ObservableObject
    {
        public AppLogContainer()
        {
            
        }

        [ObservableProperty]
        private ObservableCollection<Service.Proto.Response.LogStreamResponse.Types.LogContainer>? logsCache = new ObservableCollection<Service.Proto.Response.LogStreamResponse.Types.LogContainer> { };

        public void AddLogContainer(Service.Proto.Response.LogStreamResponse.Types.LogContainer logContainer)
        {
            if (LogsCache is null)
            {
                LogsCache = new ObservableCollection<Service.Proto.Response.LogStreamResponse.Types.LogContainer> { };
            }
            LogsCache.Add(logContainer);
        }

        public Google.Protobuf.Collections.MapField<int, int> KnownLogIndexMapping { get; set; } = new Google.Protobuf.Collections.MapField<int, int> { };
    }
}
