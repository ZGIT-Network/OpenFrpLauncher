using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class HyperlinkTp1 : HyperlinkButton
    {
        public Brush FocusBursh
        {
            get { return (Brush)GetValue(FocusBurshProperty); }
            set { SetValue(FocusBurshProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FocusBurshProperty =
            DependencyProperty.Register("FocusBursh", typeof(Brush), typeof(HyperlinkTp1), new PropertyMetadata());



        public Brush PressedBrush
        {
            get { return (Brush)GetValue(PressedBrushProperty); }
            set { SetValue(PressedBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PressedBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PressedBrushProperty =
            DependencyProperty.Register("PressedBrush", typeof(Brush), typeof(HyperlinkTp1), new PropertyMetadata());


    }
}
