using System.Windows;
using System.Windows.Controls;

namespace OpenFrp.Launcher.Helpers;

public static class TextBoxHelper
{
    public static readonly DependencyProperty PasswordBindingProperty = DependencyProperty.RegisterAttached("PasswordBinding", typeof(string), typeof(TextBoxHelper), new PropertyMetadata("{3840C332-D5A1-411B-8DCA-6FE17EFF68DC}", OnPasswordBindingChanged));

    public static string GetPasswordBinding(PasswordBox obj)
    {
        return (string)obj.GetValue(PasswordBindingProperty);
    }

    public static void SetPasswordBinding(PasswordBox obj, string value)
    {
        obj.SetValue(PasswordBindingProperty, value);
    }

    public static void OnPasswordBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        
        if (d is PasswordBox pwd)
        {
            if (pwd.GetBindingExpression(PasswordBindingProperty) is null)
            {
                pwd.PasswordChanged -= OnPasswordChangedInvoked;
            }
            else
            {
                pwd.PasswordChanged += OnPasswordChangedInvoked;
            }

        }
    }

    private static void OnPasswordChangedInvoked(object sender,RoutedEventArgs arg)
    {
        if (sender is PasswordBox pb)
        {
            SetPasswordBinding(pb, pb.Password);
        }
    }
}
