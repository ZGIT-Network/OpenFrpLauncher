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
using System.Windows.Navigation;
using System.Windows.Shapes;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Views.LoginWindow
{
    /// <summary>
    /// UpdateCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateCtrl : UserControl
    {
        public UpdateCtrl()
        {
            InitializeComponent();
        }

        public UpdateCtrl(Action<string> callbackAction)
        {
            this.CallbackAction = callbackAction;

            InitializeComponent();
        }
     

        private readonly Action<string> CallbackAction = delegate { };

        public override void OnApplyTemplate()
        {
            if (FindName("callback") is Button button)
            {
                button.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler((_, _) =>
                {
                    CallbackAction?.Invoke(ViewModels.LoginWindowViewModel.LoginState);
                }));
            }

            if (FindName("bk2") is HyperlinkButton button2)
            {
                button2.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler((_, _) =>
                {
                    CallbackAction?.Invoke(ViewModels.LoginWindowViewModel.LoginState);
                }));
            }

            base.OnApplyTemplate();
        }
    }
}
