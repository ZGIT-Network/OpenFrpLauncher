using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace OpenFrp.Launcher.Controls;



internal class TextEditorWrapper
{
#pragma warning disable CS8604, CS8618, CS8602, CS8601, CS8600
    private static readonly Type TextEditorType = Type.GetType("System.Windows.Documents.TextEditor, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

	private static readonly PropertyInfo IsReadOnlyProp = TextEditorType.GetProperty("IsReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly PropertyInfo TextViewProp = TextEditorType.GetProperty("TextView", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo RegisterMethod = TextEditorType.GetMethod("RegisterCommandHandlers", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[4]
	{
		typeof(Type),
		typeof(bool),
		typeof(bool),
		typeof(bool)
	}, null);

	private static readonly Type TextContainerType = Type.GetType("System.Windows.Documents.ITextContainer, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

	private static readonly PropertyInfo TextContainerTextViewProp = TextContainerType.GetProperty("TextView");

	private static readonly PropertyInfo TextContainerProp = typeof(TextBlock).GetProperty("TextContainer", BindingFlags.Instance | BindingFlags.NonPublic);

	private readonly object _editor;

	public static void RegisterCommandHandlers(Type controlType, bool acceptsRichContent, bool readOnly, bool registerEventListeners)
	{
		RegisterMethod.Invoke(null, new object[4] { controlType, acceptsRichContent, readOnly, registerEventListeners });
	}

	public static TextEditorWrapper CreateFor(TextBlock tb)
	{
		object textContainer = TextContainerProp.GetValue(tb);
		TextEditorWrapper editor = new TextEditorWrapper(textContainer, tb, isUndoEnabled: false);
		IsReadOnlyProp.SetValue(editor._editor, true);
		TextViewProp.SetValue(editor._editor, TextContainerTextViewProp.GetValue(textContainer));
		return editor;
	}

	public TextEditorWrapper(object textContainer, FrameworkElement uiScope, bool isUndoEnabled)
	{
		_editor = Activator.CreateInstance(TextEditorType, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new object[3] { textContainer, uiScope, isUndoEnabled }, null);
	}
}
