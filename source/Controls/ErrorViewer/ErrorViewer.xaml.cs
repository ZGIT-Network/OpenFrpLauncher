using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class ErrorViewer : ContentControl
    {
        public Exception Exception
        {
            get { return (Exception)GetValue(ExceptionProperty); }
            set { SetValue(ExceptionProperty, value); }
        }

        public static readonly DependencyProperty ExceptionProperty =
            DependencyProperty.Register("Exception", typeof(Exception), typeof(ErrorViewer), new PropertyMetadata(OnExceptionChanged));

        private static readonly string[] EnterChar = new string[] { "\r\n" };

        public static void OnExceptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Exception ex && !string.IsNullOrEmpty(ex.StackTrace)) 
            {
                string[] pattens = ex.StackTrace.Split(EnterChar, StringSplitOptions.None);

                d.SetValue(StackTracePropertyKey, string.Concat(pattens.Select(x => x.Trim() + "\r\n")));
            }
            else
            {
                d.ClearValue(StackTracePropertyKey);
            }
        }


        public string StackTrace
        {
            get { return (string)GetValue(StackTraceProperty); }
        }


        public static DependencyProperty StackTraceProperty { get => StackTracePropertyKey.DependencyProperty; }


        public static readonly DependencyPropertyKey StackTracePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("StackTrace", typeof(string), typeof(ErrorViewer), new PropertyMetadata());


    }
}
