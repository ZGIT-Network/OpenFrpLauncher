using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter
{
    internal class ProtobufTimestampToTimeString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Google.Protobuf.WellKnownTypes.Timestamp ts)
            {
                return ts.ToDateTimeOffset().ToString("yyyy/MM/dd HH:mm:ss");
            }
            throw new NotSupportedException(value.GetType().ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
