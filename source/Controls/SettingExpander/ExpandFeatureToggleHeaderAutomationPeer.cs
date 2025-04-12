using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;

namespace OpenFrp.Launcher.Controls
{
    public class ExpandFeatureToggleHeaderAutomationPeer : ToggleButtonAutomationPeer
    {
        public ExpandFeatureToggleHeaderAutomationPeer(ExpandFeatureToggleHeader owner) : base(owner)
        {
        }

        protected override string GetNameCore()
        {
            if (Owner is ExpandFeatureToggleHeader { Title: string name })
            {
                return name;
            }

            return base.GetNameCore();
        }

        protected override string GetHelpTextCore()
        {
            if (Owner is ExpandFeatureToggleHeader { Description: string helpText })
            {
                return helpText;
            }

            return base.GetHelpTextCore();
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface is PatternInterface.Toggle)
            {
                return PatternInterface.Toggle;
            }

            return base.GetPattern(patternInterface);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }
    }
}
