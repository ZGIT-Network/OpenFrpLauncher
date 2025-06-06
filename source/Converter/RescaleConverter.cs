using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter
{
    public class RescaleConverter : IValueConverter
    {
        // width -> calc(height)
        public bool IsWidthToHeightScale { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleWidth)
            {
                if (IsWidthToHeightScale)
                {
                    return doubleWidth / 16 * 9;
                }
                else
                {
                    return doubleWidth / 9 * 16;
                }
            }
            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
