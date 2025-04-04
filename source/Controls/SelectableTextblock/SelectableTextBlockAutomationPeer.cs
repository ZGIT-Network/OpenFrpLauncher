using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace OpenFrp.Launcher.Controls;

internal class SelectableTextBlockAutomationPeer : TextBlockAutomationPeer
{
	public SelectableTextBlockAutomationPeer(TextBlock owner)
		: base(owner)
	{
	}

	protected override string GetNameCore()
	{
		return AutomationProperties.GetName(base.Owner);
	}

	protected override string GetHelpTextCore()
	{
		return AutomationProperties.GetHelpText(base.Owner);
	}
}
