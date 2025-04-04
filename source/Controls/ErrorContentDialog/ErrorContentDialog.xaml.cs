using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class ErrorContentDialog : ContentDialog
    {
        public Exception Exception
        {
            get { return (Exception)GetValue(ExceptionProperty); }
            set { SetValue(ExceptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Exception.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExceptionProperty =
            DependencyProperty.Register("Exception", typeof(Exception), typeof(ErrorContentDialog), new PropertyMetadata());

        public override void OnApplyTemplate()
        {
            this.PrimaryButtonClick += (_, e) =>
            {
                try
                {
                    Clipboard.SetText(Exception.ToString());
                    Clipboard.Flush();
                }
                catch
                {

                }
            };

            base.OnApplyTemplate();
        }
    }
}
