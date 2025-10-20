using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter;

public class AweBooleanToVisibilityConverter : IValueConverter, IMultiValueConverter
{
    public bool IsCollapsed { get; set; } = true;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool bl)
        {
            bl = parameter is "Reflag" ? !bl : bl;
            
            if (bl)
            {
                return Visibility.Visible;
            }
            else
            {
                return IsCollapsed ? Visibility.Collapsed : Visibility.Hidden;
            }
        }
        return (value is null && parameter is not "Reflag") || (value is not null && parameter is "Reflag")
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is { Length: > 0 } && values.All(v => v is bool))
        {
            if (values.Length == 1)
            {
                return Convert(values[0], targetType, parameter, culture);
            }
            if (parameter is "Reflag")
            {
                if (values.All(v => v.Equals(true)))
                {
                    return IsCollapsed ? Visibility.Collapsed : Visibility.Hidden;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            else
            {
                if (values.All(v => v.Equals(true)))
                {
                    return Visibility.Visible;
                }
                else
                {
                    return IsCollapsed ? Visibility.Collapsed : Visibility.Hidden;
                }
            }
        }
            
        return (values is null && parameter is not "Reflag") || (values is not null && parameter is "Reflag")
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}