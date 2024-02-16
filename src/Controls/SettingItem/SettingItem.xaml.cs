using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.Input;
using ModernWpf;
using ModernWpf.Controls;

namespace OpenFrp.Launcher.Controls
{
    /// <summary>
    /// OpenFrp Launcher - Custom Control
    /// </summary>
    internal partial class SettingItem : ButtonBase
    {
        public SettingItem()
        {
        }
        
        // 一些东西已经迁移到统一的 Animation Resizer了。
        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("description") is TextBlock tb)
            {
                if (this.ExtendUI != null)
                {
                    tb.MaxWidth = tb.ActualWidth;
                    tb.SetBinding(WidthProperty, new Binding()
                    {
                        RelativeSource = new RelativeSource
                        {
                            AncestorType = typeof(WrapPanel)
                        },
                        Mode = BindingMode.OneWay,
                        Path = new PropertyPath(WrapPanel.ActualWidthProperty)
                    });
                }
                tb.TextWrapping = TextWrapping.Wrap;
            }
            SetValue(ContentTypeStringProperty, Content?.GetType().ToString() ?? "");
            base.OnApplyTemplate();
        }

        #region Property Icon
        public IconElement Icon
        {
            get { return (IconElement)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(IconElement), typeof(SettingItem), new PropertyMetadata());
        #endregion
        #region Property Title
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SettingItem), new PropertyMetadata("Defualt Title"));
        #endregion
        #region Property Description
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(SettingItem), new PropertyMetadata(""));
        #endregion
        #region Property IsClickable
        public bool IsClickable
        {
            get { return (bool)GetValue(IsClickableProperty); }
            set { SetValue(IsClickableProperty, value); }
        }
        public static readonly DependencyProperty IsClickableProperty =
            DependencyProperty.Register("IsClickable", typeof(bool), typeof(SettingItem), new PropertyMetadata());
        #endregion
        #region Property Extends
        public UIElement ExtendUI
        {
            get { return (UIElement)GetValue(ExtendUIProperty); }
            set { SetValue(ExtendUIProperty, value); }
        }
        public static readonly DependencyProperty ExtendUIProperty =
            DependencyProperty.Register("ExtendUI", typeof(UIElement), typeof(SettingItem), new PropertyMetadata());
        #endregion
        #region Property ContentTypeString
        public string ContentTypeString
        {
            get { return (string)GetValue(ContentTypeStringProperty.DependencyProperty); }
        }

        // Using a DependencyProperty as the backing store for ContentTypeString.  This enables animation, styling, binding, etc...
        public static readonly DependencyPropertyKey ContentTypeStringProperty =
            DependencyProperty.RegisterReadOnly("ContentTypeString", typeof(string), typeof(SettingItem), new PropertyMetadata(""));
        #endregion


        protected override AutomationPeer OnCreateAutomationPeer() => new SettingItemAutomationPeer(this);
    }

    internal class SettingItemAutomationPeer : FrameworkElementAutomationPeer
    {
        // From https://blog.walterlv.com/post/wpf-app-supports-ui-automation-better

        public SettingItemAutomationPeer(FrameworkElement owner) : base(owner)
        {
            
        }

        // 在 AutomationControlType 里找一个最能反应你所写控件交互类型的类型，
        // 准确返回类型可以让 UI 自动化软件针对性地做一些自动化操作（例如按钮的点击），
        // 如果找不到类似的就说明是全新种类的控件，应返回 Custom。
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        // 针对上面返回的类型，这里给一个本地化的控件类型名。
        protected override string GetLocalizedControlTypeCore() => "选项卡跳转按钮";

        protected override string GetHelpTextCore() => ((SettingItem)Owner).Description;

        // AutomationProperties.Name
        // 这里的文字就类似于按钮的 Content 属性一样，是给用户“看”的，可被读屏软件读出。
        // 你可以考虑返回你某个自定义属性的值或某些自定义属性组合的值，而这个值最能向用户反映此控件当前的状态。
        protected override string GetNameCore() => ((SettingItem)Owner).Title;
    }
}
