using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace OpenFrp.Launcher.Controls
{
    public class UserTunnelAutomationPeer : FrameworkElementAutomationPeer
    {
        public UserTunnelAutomationPeer(UserTunnel owner) : base(owner)
        {
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ItemContainer)
            {
                return patternInterface;
            }
            return base.GetPattern(patternInterface);
        }
        protected override string GetLocalizedControlTypeCore()
        {
            return string.Empty;
        }

        protected override string GetNameCore()
        {
            if (base.Owner is OpenFrp.Launcher.Controls.UserTunnel { Tunnel: Model.UserTunnel tunnel})
            {
                return "用户隧道 " + tunnel.Name;
            }
            return AutomationProperties.GetName(base.Owner);
        }

        protected override string GetHelpTextCore()
        {
            return AutomationProperties.GetHelpText(base.Owner);
        }

    }
}
