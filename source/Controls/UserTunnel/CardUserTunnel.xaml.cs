using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.ViewModels;

namespace OpenFrp.Launcher.Controls
{
    public partial class CardUserTunnel : UserTunnelBase
    {
        static CardUserTunnel()
        {
            var cubic = new CubicEase { EasingMode = EasingMode.EaseOut };

            cubic.Freeze();

            @opacityAnimationProc = new DoubleAnimation
            {
                To = 1,
                From = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = cubic
            };
        }
        public CardUserTunnel()
        {
            
        }

        private static DoubleAnimation? @opacityAnimationProc;


        public override void OnApplyTemplate()
        {
            if (BeginWithOpacityAnimation)
            {
                BeginAnimation(OpacityProperty, opacityAnimationProc);
            }

            base.OnApplyTemplate();
        }




        #region BeginWithOpacityAnimation
        public static readonly DependencyProperty BeginWithOpacityAnimationProperty =
            DependencyProperty.Register("BeginWithOpacityAnimation", typeof(bool), typeof(CardUserTunnel), new PropertyMetadata(false));

        public bool BeginWithOpacityAnimation
        {
            get { return (bool)GetValue(BeginWithOpacityAnimationProperty); }
            set { SetValue(BeginWithOpacityAnimationProperty, value); }
        }
        #endregion
        


        public void RemoveWithAnimate(Action callback)
        {
            if (GetTemplateChild("subfr") is not Border br) return;

            Storyboard stb = new Storyboard { };

            var toffHitAnimation = new BooleanAnimationUsingKeyFrames();
            {
                toffHitAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame()
                {
                    KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                    Value = false,
                });
            };
            var opacityDoubleAnimation = new DoubleAnimation()
            {
                From = 1,
                To = 0
            };
            var doubleXAnimation = new DoubleAnimation()
            {
                To = 0.95
            };
            var doubleYAnimation = new DoubleAnimation()
            {
                To = 0.95
            };

            Storyboard.SetTargetProperty(opacityDoubleAnimation, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTargetProperty(toffHitAnimation, new PropertyPath(UIElement.IsHitTestVisibleProperty));
            Storyboard.SetTargetProperty(doubleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTargetProperty(doubleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            opacityDoubleAnimation.Duration = doubleXAnimation.Duration = doubleYAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(250));

            opacityDoubleAnimation.EasingFunction = doubleXAnimation.EasingFunction = doubleYAnimation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };

            stb.Children.Add(opacityDoubleAnimation);
            stb.Children.Add(doubleXAnimation);
            stb.Children.Add(doubleYAnimation);
            stb.Children.Add(toffHitAnimation);

            stb.Completed += delegate { callback.Invoke(); };

            br.BeginStoryboard(stb);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new UserTunnelAutomationPeer(this);
        }
    }
    public partial class DeleteUserTunnelContentPresenterMenuItem : MenuItem
    {
        static DeleteUserTunnelContentPresenterMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DeleteUserTunnelContentPresenterMenuItem), new FrameworkPropertyMetadata(typeof(DeleteUserTunnelContentPresenterMenuItem)));
        }


        public DeleteUserTunnelContentPresenterMenuItem(UserTunnelBase userTunnelCtrl, MenuFlyout flyout)
        {
            this.flyout = flyout;
            this.userTunnelCtrl = userTunnelCtrl;
        }

        private readonly UserTunnelBase? userTunnelCtrl;
        private readonly MenuFlyout? flyout;

        public override void OnApplyTemplate()
        {
            if (userTunnelCtrl is null || flyout is null) throw new NotSupportedException();

            if (GetTemplateChild("delete_button") is Button button_delete)
            {
                button_delete.Command = new RelayCommand(delegate
                {
                    if (this.DataContext is Model.UserTunnel uix)
                    {
                        userTunnelCtrl.DeleteCommandBinding.Execute(uix);
                    }
                    flyout.Hide();
                });
            }
            if (GetTemplateChild("cancelAndCloseFlyout") is Button button_cancel)
            {
                button_cancel.Command = new RelayCommand(flyout.Hide);
            }
            base.OnApplyTemplate();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return null!;
        }
    }
}
