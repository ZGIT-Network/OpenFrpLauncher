using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter
{
    public class TrafficConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long val)
            {
                if (parameter is "allTraffic")
                {
                    return Math.Round(val / 1024d, 2);
                }
            }
            else if (value is int vv)
            {
                if (parameter is "limit")
                {
                    return Math.Round((vv / 1024d) * 8, 2);
                }
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }
    }
}
