using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;


namespace OpenFrp.Launcher.Converter
{
    public class ThicknessFilterConverter : IValueConverter
    {
        [Flags]
        public enum ThicknessFilterKind
        {
            None = 0,
            Top = 1,
            Right = 2,
            Bottom = 4,
            Left = 8,

            TopAndLeft = Top | Left,
            BottomAndRight = Bottom | Right,
            LeftAndRight = Left | Right,
            TopAndBottom = Top | Bottom,

            ExcludeTop = BottomAndRight | Left,
            ExcludeLeft = BottomAndRight | Top,
            ExcludeRight = TopAndBottom | Right,
            ExcludeBottom = LeftAndRight | Top
        }

        public ThicknessFilterKind Filter { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness thickness = (Thickness)value;

            if (!Filter.HasFlag(ThicknessFilterKind.Top))
            {
                thickness.Top = 0.0;
            }
            if (!Filter.HasFlag(ThicknessFilterKind.Left))
            {
                thickness.Left = 0.0;
            }
            if (!Filter.HasFlag(ThicknessFilterKind.Right))
            {
                thickness.Right = 0.0;
            }
            if (!Filter.HasFlag(ThicknessFilterKind.Bottom))
            {
                thickness.Bottom = 0.0;
            }

            return thickness;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
