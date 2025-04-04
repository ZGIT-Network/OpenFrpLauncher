using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenFrp.Launcher.Controls
{
    public class Skeleton : UserControl
    {
        public Skeleton()
        {
            this.IsEnabledChanged += Skeleton_IsEnabledChanged;
        }

        private void Skeleton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = e.NewValue is true ? 1 : 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CircleEase() { EasingMode = EasingMode.EaseOut }
            });
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return null!;
            
        }

        

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CornerRadius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(Skeleton), new PropertyMetadata(new CornerRadius()));






    }
}
