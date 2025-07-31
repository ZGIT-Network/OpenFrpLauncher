using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter;

public class AweBooleanToVisibilityConverter : IValueConverter
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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}