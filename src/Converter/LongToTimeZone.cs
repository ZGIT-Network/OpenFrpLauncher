using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter
{
    internal class LongToTimeZone : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds((long)value).ToOffset(TimeSpan.FromHours(8)).ToString("yyyy/MM/dd HH:mm:ss");
            }
            return DateTimeOffset.FromUnixTimeMilliseconds(-1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
