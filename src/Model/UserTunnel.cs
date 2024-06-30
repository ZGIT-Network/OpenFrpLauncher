using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    [INotifyPropertyChanged]
    internal partial class UserTunnel : Awe.Model.OpenFrp.Response.Data.UserTunnel
    {
        public bool IsMinecraftService
        {
            get
            {
                if (string.IsNullOrEmpty(TunnelCustomConfig)) return false;

                return Regex.IsMatch(TunnelCustomConfig, "launcher.isMinecraftService( )?=( )?(T)?(t)?(rue)?(RUE)?");
            }
        }

        [ObservableProperty]
        private bool isPortWaiting;

        public static UserTunnel FromOriginalUserTunnel(Awe.Model.OpenFrp.Response.Data.UserTunnel originalUserTunnel)
        {
            var @this = new UserTunnel();

            var tft = originalUserTunnel.GetType();
            var properties = typeof(Awe.Model.OpenFrp.Response.Data.UserTunnel).GetProperties();

            foreach (var item in properties)
            {
                if (item.CanWrite && item.CanRead)
                {
                    if (tft.GetProperty(item.Name) is { } pr)
                    {
                        item.SetValue(@this, pr.GetValue(originalUserTunnel));
                    }
                }
            }

            return @this;
        }
    }
}
