using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Controls
{
    public partial class ExpandFeatureToggleHeader : ToggleButton
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ExpandFeatureToggleHeaderAutomationPeer(this);
        }

        #region Property Icon
        public IconElement Icon
        {
            get { return (IconElement)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(IconElement), typeof(ExpandFeatureToggleHeader), new PropertyMetadata());
        #endregion
        #region Property Title
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ExpandFeatureToggleHeader), new PropertyMetadata("Defualt Title"));
        #endregion
        #region Property Description
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(ExpandFeatureToggleHeader), new PropertyMetadata(""));
        #endregion
    }
}
