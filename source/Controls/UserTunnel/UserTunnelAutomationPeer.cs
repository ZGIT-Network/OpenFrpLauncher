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
        public UserTunnelAutomationPeer(CardUserTunnel owner) : base(owner)
        {
        }

        public UserTunnelAutomationPeer(ListUserTunnel owner) : base(owner)
        {
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.SelectionItem)
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
            switch (base.Owner)
            {
                case OpenFrp.Launcher.Controls.ListUserTunnel { Tunnel: Model.UserTunnel tunnel }:
                    return "用户隧道 " + tunnel.Name;
                case OpenFrp.Launcher.Controls.CardUserTunnel { Tunnel: Model.UserTunnel tunnel }:
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
