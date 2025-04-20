using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    public partial class SoftwareConfig : ObservableObject
    {
        public SoftwareConfig(Yue3.Model.OpenFrp.Response.Data.SoftWareVersionData config)
        {
            this.SoftwareConfigValue = config;
        }

        [ObservableProperty,NotifyPropertyChangedFor(nameof(FrpcLatestVersion),nameof(LauncherConfig))]
        private Yue3.Model.OpenFrp.Response.Data.SoftWareVersionData? softwareConfigValue;

        public Yue3.Model.OpenFrp.Response.Data.SoftWareVersionData.LauncherProperty LauncherConfig { get => SoftwareConfigValue?.Launcher ?? throw new NullReferenceException(nameof(SoftwareConfig)); }

        public string FrpcLatestVersion { get => SoftwareConfigValue?.Latest ?? throw new NullReferenceException(nameof(SoftwareConfig)); }
    }
}
