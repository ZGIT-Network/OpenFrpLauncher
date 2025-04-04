using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter;

public class EqualConverter : IValueConverter
{
    public object? TrueResult { get; set; }

    public object? FalseResult { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            object? obj;
            if (parameter != null)
            {
                obj = FalseResult;
                if (obj == null)
                {
                    return false;
                }
            }
            else
            {
                obj = TrueResult ?? true;
            }
            return obj;
        }
        if (!(value is bool flag))
        {
            if (!(value is int num))
            {
                if (!(value is double num2))
                {
                    if (value != null)
                    {
                        goto IL_0111;
                    }
                }
                else
                {
                    double doublVl1 = num2;
                    if (!double.TryParse(parameter.ToString(), out var compareDoublVl))
                    {
                        goto IL_0111;
                    }
                    if (!doublVl1.Equals(compareDoublVl))
                    {
                        return FalseResult ?? false;
                    }
                }
            }
            else
            {
                int intVl1 = num;
                if (!intVl1.ToString().Equals(parameter))
                {
                    return FalseResult ?? false;
                }
            }
            goto IL_01af;
        }
        bool booleanVl1 = flag;
        if (bool.TryParse(parameter?.ToString(), out var booleanPara))
        {
            object? obj2;
            if (!booleanVl1.Equals(booleanPara))
            {
                obj2 = FalseResult;
                if (obj2 == null)
                {
                    return false;
                }
            }
            else
            {
                obj2 = TrueResult ?? true;
            }
            return obj2;
        }
        goto IL_0111;
    IL_01af:
        return TrueResult ?? true;
    IL_0111:
        if (parameter is Array array)
        {
            object? obj3;
            if (!array.OfType<object>().Contains(value))
            {
                obj3 = FalseResult;
                if (obj3 == null)
                {
                    return false;
                }
            }
            else
            {
                obj3 = TrueResult ?? true;
            }
            return obj3;
        }
        if (!value.Equals(parameter))
        {
            //if (FalseResult is string FalseResultString && FalseResultString.Contains("[or_private_content]"))
            //{
            //    return string.Format(FalseResultString.Clone().ToString().Replace("[or_private_content]", string.Empty), value);
            //}
            return FalseResult ?? false;
        }
        goto IL_01af;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
