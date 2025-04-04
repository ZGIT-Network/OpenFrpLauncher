using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace OpenFrp.Launcher.Controls;

public class SelectableTextBlock : TextBlock
{
	private readonly TextEditorWrapper _editor;

	static SelectableTextBlock()
	{
		UIElement.FocusableProperty.OverrideMetadata(typeof(SelectableTextBlock), new FrameworkPropertyMetadata(true));
		TextEditorWrapper.RegisterCommandHandlers(typeof(SelectableTextBlock), acceptsRichContent: true, readOnly: true, registerEventListeners: true);
		FrameworkElement.FocusVisualStyleProperty.OverrideMetadata(typeof(SelectableTextBlock), new FrameworkPropertyMetadata((object)null!));
	}

	public SelectableTextBlock()
	{
		_editor = TextEditorWrapper.CreateFor(this);
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new SelectableTextBlockAutomationPeer(this);
	}
}
