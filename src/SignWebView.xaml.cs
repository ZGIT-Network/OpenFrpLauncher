using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenFrp.Launcher
{
    /// <summary>
    /// SignWebView.xaml 的交互逻辑
    /// </summary>
    public partial class SignWebView : Window
    {
        public SignWebView()
        {
            InitializeComponent();
        }

        public override async void OnApplyTemplate()
        {
            //await webView.EnsureCoreWebView2Async();

            //var ms = App.GetResourceStream(new Uri("pack://application:,,,/Resources/SignWebView/index.html"));

            //if (ms?.Stream is { Length: > 0 } stream)
            //{
            //    byte[] buffer = new byte[stream.Length];

            //    await ms.Stream.ReadAsync(buffer,0,(int)stream.Length);

            //    webView.CoreWebView2.NavigateToString(Encoding.UTF8.GetString(buffer));
            //}
            base.OnApplyTemplate();
        }
    }
}
