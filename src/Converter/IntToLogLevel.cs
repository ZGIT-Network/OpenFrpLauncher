using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Windows.Devices.PointOfService;

namespace OpenFrp.Launcher.Converter
{
    internal class IntToLogLevel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            if (value is int)
            {
                switch (value)
                {
                    case 1:
                        {
                            return "[E]";
                        }
                    case 2:
                        {
                            return "[W]";
                        }
                    case 3:
                        {
                            return "[I]";
                        }
                    case 4:
                        {
                            return "[D]";
                        }
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
