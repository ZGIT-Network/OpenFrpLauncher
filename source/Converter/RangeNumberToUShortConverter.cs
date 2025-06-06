using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter
{
    internal class RangeNumberToUShortConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return null!;
            }
            if (value is int ivl)
            {
                if (ivl < ushort.MinValue || ivl > ushort.MaxValue)
                {
                    throw new NotSupportedException(nameof(value));
                }
                return (ushort)value;
            }
            if (value is double dvl)
            {
                if (dvl < ushort.MinValue || dvl > ushort.MaxValue)
                {
                    throw new NotSupportedException(nameof(value));
                }
                return (ushort)Math.Round(dvl);
            }
            if (value is ushort v)
            {
                return v;
            }
            throw new NotImplementedException(value.GetType().ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ushort or int or double or null)
            {
                return value!;
            }
            throw new NotImplementedException(value.GetType().ToString());
        }
    }
}
