using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;


namespace OpenFrp.Launcher.Controls
{
    public partial class Carousel : ItemsControl
    {
        public Carousel()
        {
            if (ItemsSource != null)
            {
                itemSourceCount = (ItemsSource as Array)?.Length ?? 0;
            }
        }

        private static EasingFunctionBase defualtEasingFunction = new CubicEase()
        {
            EasingMode = EasingMode.EaseOut
        };

        private int itemSourceCount;

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            itemSourceCount = (newValue as Array)?.Length ?? 0;

            SelectedIndex = 0;

            base.OnItemsSourceChanged(oldValue, newValue);
        }

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set
            {
                int newValue = value;
    
                if (value >= itemSourceCount)
                {
                    newValue = 0;
                }
                else if (newValue < 0)
                {
                    newValue = itemSourceCount - 1;
                }
                SetValue(SelectedIndexProperty, newValue);
            }
        }
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(Carousel), new PropertyMetadata(-1, OnSelectedIndexChanged));

        protected static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Carousel carousel)
            {
                if (e.NewValue is int newValue)
                {
                    PlayAnimate(carousel, newValue);
                }
            }
        }
        private static void PlayAnimate(Carousel carousel,int index,int duration = 250)
        {
            if (carousel.GetTemplateChild("animateRoot") is ItemsPresenter canvas && canvas.RenderTransform is TranslateTransform ttf)
            {
                double newTo = -(carousel.ActualWidth * carousel.SelectedIndex);
                //ttf.X = newTo; 
                var doubleAnimation = new DoubleAnimation
                {
                    To = newTo,
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = defualtEasingFunction,
                    FillBehavior = FillBehavior.HoldEnd
                };
                var hitAnimation = new BooleanAnimationUsingKeyFrames()
                {
                    KeyFrames =
                    {
                        new DiscreteBooleanKeyFrame()
                        {
                            KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                            Value = false,
                        },
                        new DiscreteBooleanKeyFrame()
                        {
                            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration)),
                            Value = true,
                        }
                    }
                };
                Storyboard.SetTarget(doubleAnimation, canvas);
                Storyboard.SetTarget(hitAnimation, canvas);
                Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                Storyboard.SetTargetProperty(hitAnimation, new PropertyPath(UIElement.IsHitTestVisibleProperty));

                var storyboard = new Storyboard()
                {
                    Children =
                    {
                        doubleAnimation,
                        hitAnimation
                    }
                };
                canvas.BeginAnimation(UIElement.RenderTransformProperty, null);
                storyboard.Begin();
            }
        }

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("nextCav") is Button nextButton)
            {
                nextButton.Click += delegate
                {
                    SelectedIndex += 1;
                };
            }
            if (GetTemplateChild("prevCav") is Button prevButton)
            {
                prevButton.Click += delegate
                {
                    SelectedIndex -= 1;
                };
            }

            base.OnApplyTemplate();
        }



        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(Carousel), new PropertyMetadata(new CornerRadius(4)));



        public bool AutoPlay
        {
            get { return (bool)GetValue(AutoPlayProperty); }
            set { SetValue(AutoPlayProperty, value); }
        }

        public static readonly DependencyProperty AutoPlayProperty =
            DependencyProperty.Register("AutoPlay", typeof(bool), typeof(Carousel), new PropertyMetadata(false, OnAutoPlayChanged));

        protected static void OnAutoPlayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Carousel carousel)
            {
                if (e.NewValue is true)
                {
                    _ = AutoPlayProc(carousel);
                }
                else
                {
                    SetRandomId(carousel);
                }
            }
        }

        private int appRandomId;
        private static void SetRandomId(Carousel carousel)
        {
#if NET
            carousel.appRandomId = Random.Shared.Next(1,int.MaxValue);
#else
            Random r = new Random();

            carousel.appRandomId = r.Next(1, int.MaxValue);
#endif
        }

        private static async Task AutoPlayProc(Carousel carousel)
        {
            SetRandomId(carousel);

            int randomId = carousel.appRandomId;
            for(; ; )
            {
                if (carousel.appRandomId != randomId) break;

                await Task.Delay(3000);

                var t = Mouse.GetPosition(carousel);
                if (t.X < 0 || t.Y < 0 || t.X > carousel.ActualWidth || t.Y > carousel.ActualHeight)
                {
                    
                }
                else
                {
                    switch (Mouse.DirectlyOver)
                    {
                        case System.Windows.Controls.Border:
                        case System.Windows.Controls.TextBlock:
                        case System.Windows.Documents.Run:
                            {
                                continue;
                            }
                    }
                }
                carousel.SelectedIndex++;
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged && SelectedIndex > 0)
            {
                PlayAnimate(this, SelectedIndex,duration: 0);
            }

            base.OnRenderSizeChanged(sizeInfo);
        }
    }
}
