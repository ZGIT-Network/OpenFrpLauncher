using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter
{
    internal partial class LongToByteDanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long trf)
            {
                double d = System.Convert.ToDouble(trf);
                double d1 = d / 1024d;
                if (d1 < 1)
                {
                    return $"{Math.Round(d,2)} Mib";
                }
                double d2 = d1 / 1024d;
                if (d2 < 1)
                {
                    return $"{d1} Gib";
                }
                return $"{Math.Round(d2 / 1024d, 2)} Tib";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
