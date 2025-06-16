using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class AdaptScrollViewer : ScrollViewerEx
    {
        public AdaptScrollViewer()
        {
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
        }
        static AdaptScrollViewer()
        {
        }

        protected override void OnManipulationBoundaryFeedback(ManipulationBoundaryFeedbackEventArgs e)
        {
            // https://blog.lindexi.com/post/WPF-设置窗口不跟随触摸惯性拖动抖动.html
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (this.ComputedVerticalScrollBarVisibility is not Visibility.Visible)
            {
                e.Handled = false;
                return;
            }
            else if (e.Delta > 0 && this.VerticalOffset == 0)
            {
                e.Handled = false;
                return;
            }
            else if (e.Delta < 0 && VerticalOffset + ActualHeight >= this.ExtentHeight)
            {
                e.Handled = false;
                return;
            }
            base.OnMouseWheel(e);
        }
    }
}
