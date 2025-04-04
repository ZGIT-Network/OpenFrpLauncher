using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter;

public class ObjectToTypeStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.GetType().ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
