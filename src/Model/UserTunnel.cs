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

        public void CopyExtraAddress()
        {
            if (ExtraConnectAddress.Length is 0) return;

            try
            {
                System.Windows.Clipboard.SetText(ExtraConnectAddress[0]);

                System.Windows.MessageBox.Show(
                    "你刚刚右键了\"复制按钮\"，复制了该节点的扩展链接。\n该链接有应于该节点物理服务器的各种限制，为了保证你的链接，需要使用该链接进行链接。\n\n节点: #" + NodeId + " " + NodeName, "OpenFrp Launcher", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch{

            }
        }
    }
}
