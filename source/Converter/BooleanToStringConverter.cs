using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter;

public class BooleanToStringConverter : IValueConverter
{
    public string DefaultTrueString { get; set; } = "DefaultTrueString";


    public string DefaultFalseString { get; set; } = "DefaultFalseString";


    public BooleanToStringConverter()
    {
    }

    public BooleanToStringConverter(string defaultTrueString, string defaultFalseString)
    {
        DefaultTrueString = defaultTrueString;
        DefaultFalseString = defaultFalseString;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool && (bool)value)
        {
            return DefaultTrueString;
        }
        return DefaultFalseString;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
