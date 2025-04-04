using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter;

public class EitherConverter : IMultiValueConverter
{
    public bool ReflagValue { get; set; }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // true && true = true
        // true && false = false
        // true 
        return values.Any(static (x) => x is true) == (!ReflagValue);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
