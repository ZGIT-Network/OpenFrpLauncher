using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenFrp.Launcher.Converter
{
    internal class DomainStringToListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HashSet<string> strs)
            {
                if (strs.Count == 0) return "";

                var sb = new StringBuilder();

                foreach (string str in strs)
                {
                    sb.Append(str + ",");
                }

                return sb.Remove(sb.Length - 1,1).ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && str.Length > 0)
            {
                if (str.Contains(','))
                {
                    var hs = new HashSet<string>();
                    foreach (var item in str.Split(','))
                    {
                        if ((item != null || string.Empty.Equals(item)) && !item.Equals(" "))
                        {
                            hs.Add(item);
                        }
                    }
                    return hs;
                }
                else
                {
                    return new HashSet<string>(new string[1] { str });
                }
            }
            return default!;
        }
    }
}
