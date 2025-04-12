using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter
{
    public class RegexReplaceConverter : IValueConverter
    {
        private Regex? regex;

        public string RegexPatten
        {
            set => regex = new Regex(value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (regex is null)
            {
                throw new NullReferenceException(nameof(regex));
            }
            if (value is string s1 && parameter is string v2)
            {
                return regex.Replace(s1,v2);
            }
            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
