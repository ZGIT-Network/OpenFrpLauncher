using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class SettingExpander : System.Windows.Controls.Expander
    {
        public SettingExpander() { }

        #region Property Icon
        public IconElement Icon
        {
            get { return (IconElement)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(IconElement), typeof(SettingExpander), new PropertyMetadata());
        #endregion
        #region Property Title
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SettingExpander), new PropertyMetadata("Defualt Title"));
        #endregion
        #region Property Description
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(SettingExpander), new PropertyMetadata(""));
        #endregion

        protected override void OnCollapsed()
        {
            if (GetTemplateChild("RootPanel") is FrameworkElement panel && GetTemplateChild("ExpandSite") is ContentPresenter cp)
            {
                foreach (var group in VisualStateManager.GetVisualStateGroups(panel))
                {
                    if (group is VisualStateGroup vsg)
                    {
                        switch (vsg.Name)
                        {
                            case "ExpansionStates":
                                {
                                    foreach (var state in vsg.States)
                                    {
                                        if (state is VisualState { Name: "Collapsed" } vs)
                                        {
                                            if (vs.Storyboard.Children[1] is DoubleAnimation dAnimation)
                                            {
                                                dAnimation.To = -cp.ActualHeight + 20;
                                            }
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
            }

            base.OnCollapsed();
        }
    }
}
