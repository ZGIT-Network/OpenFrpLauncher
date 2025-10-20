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
            if (e.NewValue is false)
            {
                if (GetTemplateChild("bg1") is Border bg1)
                {
                    bg1.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);
                }
                if (GetTemplateChild("bg2") is Border bg2)
                {
                    bg2.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);
                }
            }

            var easing = new CircleEase() { EasingMode = EasingMode.EaseOut };

            easing.Freeze();

            this.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = e.NewValue is true ? 1 : 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = easing
            });
        }

        public override void OnApplyTemplate()
        {
            var easing = new CircleEase() { EasingMode = EasingMode.EaseOut };

            easing.Freeze();

            this.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = IsEnabled ? 0 : null,
                To = IsEnabled ? 1 : 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = easing
            });

            base.OnApplyTemplate();
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

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(Skeleton), new PropertyMetadata(new CornerRadius()));






    }
}
