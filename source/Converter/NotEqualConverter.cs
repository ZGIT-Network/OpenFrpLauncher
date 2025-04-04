using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter;

public class NotEqualConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return parameter != null;
        }
        if (!(value is bool flag))
        {
            if (!(value is string text))
            {
                if (!(value is int num))
                {
                    if (!(value is double num2))
                    {
                        if (!(value is Enum @enum))
                        {
                            if (value == null)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            Enum enumVl1 = @enum;
                            if (parameter is Enum enumParaVl1)
                            {
                                return !enumVl1.Equals(enumParaVl1);
                            }
                        }
                    }
                    else
                    {
                        double doublVl1 = num2;
                        if (double.TryParse(parameter.ToString(), out var compareDoublVl))
                        {
                            return !doublVl1.Equals(compareDoublVl);
                        }
                    }
                }
                else if (parameter != null)
                {
                    int in2 = num;
                    if (int.TryParse(parameter.ToString(), out var compateIn2))
                    {
                        return !in2.Equals(compateIn2);
                    }
                }
            }
            else
            {
                string str = text;
                if (parameter is string compareStr)
                {
                    return !str.Equals(compareStr);
                }
            }
        }
        else
        {
            bool boolva = flag;
            if (bool.TryParse(parameter.ToString(), out var compareBoolva))
            {
                return !boolva.Equals(compareBoolva);
            }
        }
        return !value.Equals(parameter) && !value.Equals(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
