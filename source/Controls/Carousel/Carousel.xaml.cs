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
using System.Windows.Shapes;



namespace OpenFrp.Launcher.Controls
{
    public partial class Carousel : ItemsControl
    {
        public Carousel()
        {

        }

        private static readonly EasingFunctionBase defaultEasingFunction = new CubicEase()
        {
            EasingMode = EasingMode.EaseOut
        };

        private int itemSourceCount;
        private Storyboard? prevInfitiy = default;

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            itemSourceCount = newValue is Array { Length: var length } ? length : 0;
            ToggleControllerVisibility();

            SelectedIndex = 0;

            base.OnItemsSourceChanged(oldValue, newValue);
        }

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("nextCav") is Button nextButton)
            {
                nextButton.Click += delegate
                {
                    NextPage();
                };
            }
            if (GetTemplateChild("prevCav") is Button prevButton)
            {
                prevButton.Click += delegate
                {
                    PreviousPage();
                };
            }
            if (GetTemplateChild("lFakeFill") is VisualBrush lFakeFill && GetTemplateChild("rFakeFill") is VisualBrush rFakeFill)
            {
                ItemContainerGenerator.StatusChanged += delegate
                {
                    if (ItemContainerGenerator.Status is System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                    {
                        if (ItemContainerGenerator.Items is { Count: > 0, Count: int count })
                        {
                            lFakeFill.Visual = (UIElement)ItemContainerGenerator.ContainerFromIndex(count - 1);
                            rFakeFill.Visual = (UIElement)ItemContainerGenerator.ContainerFromIndex(0);
                        }
                        else
                        {
                            lFakeFill.ClearValue(VisualBrush.VisualProperty);
                            rFakeFill.ClearValue(VisualBrush.VisualProperty);
                        }
                        ScrollToCarousel(0, TimeSpan.Zero);
                    }
                };
                base.OnApplyTemplate();
            }
        }

        #region Property



        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(Carousel), new PropertyMetadata(true));



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
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(Carousel), new PropertyMetadata(-1));

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
                    carousel.AutoPlayProc();
                }
                else
                {
                    carousel.SetRandomId();
                }
            }
        }

        #endregion

        private void NextPage()
        {
            if (SelectedIndex == itemSourceCount - 1)
            {
                SelectedIndex = 0;

                prevInfitiy = ScrollToCarousel(itemSourceCount, completed: () => { ScrollToCarousel(SelectedIndex, TimeSpan.Zero); prevInfitiy = null; });
            }
            else
            {
                if (prevInfitiy is not null)
                {
                    prevInfitiy.Stop();
                    prevInfitiy = null;
                }
                SelectedIndex += 1;
                ScrollToCarousel(SelectedIndex);
            }
        }

        private void PreviousPage()
        {
            if (SelectedIndex is 0)
            {
                SelectedIndex = itemSourceCount - 1;

                prevInfitiy = ScrollToCarousel(-1, completed: () => { ScrollToCarousel(SelectedIndex, TimeSpan.Zero); prevInfitiy = null; });
            }
            else
            {
                if (prevInfitiy is not null)
                {
                    prevInfitiy.Stop();
                    prevInfitiy = null;
                }
                SelectedIndex -= 1;
                ScrollToCarousel(SelectedIndex);
            }
        }

        private void ToggleControllerVisibility()
        {
            if (GetTemplateChild("controller") is FrameworkElement fe)
            {
                fe.Visibility = itemSourceCount > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public void StopCarouselAnimate()
        {
            if (GetTemplateChild("animateRoot") is FrameworkElement fe)
            {
                fe.BeginAnimation(UIElement.RenderTransformProperty, null);
                fe.BeginAnimation(UIElement.IsHitTestVisibleProperty, null);
            }
        }
        public Storyboard? ScrollToCarousel(int index, Duration duration = default, Action? completed = default)
        {
            if (duration.Equals(default))
            {
                duration = TimeSpan.FromMilliseconds(350);
            }

            if (GetTemplateChild("animateRoot") is FrameworkElement fe)
            {
                var doubleAnimation = new DoubleAnimation
                {
                    To = -(ActualWidth * index + ActualWidth),
                    Duration = duration,
                    EasingFunction = defaultEasingFunction,
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
                            KeyTime = KeyTime.FromTimeSpan(duration.TimeSpan),
                            Value = true,
                        }
                    }
                };
                Storyboard.SetTarget(doubleAnimation, fe);
                Storyboard.SetTarget(hitAnimation, fe);
                Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                Storyboard.SetTargetProperty(hitAnimation, new PropertyPath(UIElement.IsHitTestVisibleProperty));

                var storyboard = new Storyboard()
                {
                    Children =
                    {
                        doubleAnimation,
                        hitAnimation
                    },
                    Duration = duration
                };
                fe.BeginAnimation(UIElement.RenderTransformProperty, null);
                storyboard.Completed += delegate { completed?.Invoke(); };
                storyboard.Begin();

                return storyboard;
            }
            return default;
        }

        #region Touch Support
        private Rect touchFirstRect = Rect.Empty;
        private int lastPrimaryTouchId = -1;

        protected override void OnTouchDown(TouchEventArgs e)
        {
            if (lastPrimaryTouchId != -1) return;

            TouchPoint tp = e.GetTouchPoint(this);
            //System.Diagnostics.Debug.WriteLine("set new rect!!!!!!!!!!!!!!!!!!");
            touchFirstRect = tp.Bounds;
            lastPrimaryTouchId = tp.TouchDevice.Id;

            e.Handled = changedByTouch = true;

            tp.TouchDevice.Updated += OnTouchDeviceUpdated;

            base.OnTouchDown(e);
        }

        private void OnTouchDeviceUpdated(object? _o, EventArgs _e)
        {
            if (GetTemplateChild("animateRoot") is not FrameworkElement { RenderTransform: TranslateTransform ttf })
            {
                return;
            }

            if (_o is TouchDevice td)
            {
                TouchPoint tp = td.GetTouchPoint(this);
                switch (tp.Action)
                {
                    case TouchAction.Move when tp.TouchDevice.Id == lastPrimaryTouchId:
                        {
                            double offset = tp.Position.X - touchFirstRect.Left;
                            if (offset is 0)
                            {
                                return;
                            }
                            else
                            {
                                ttf.BeginAnimation(TranslateTransform.XProperty, null);
                            }
                            ttf.SetValue(TranslateTransform.XProperty, -(ActualWidth * SelectedIndex + ActualWidth) + offset);
                            System.Diagnostics.Debug.WriteLine($"Move Action => [X: {offset}] => [New X: {ttf.X}]");
                        }
                        ; break;
                    case TouchAction.Up:
                        {
                            ReTouchUp(tp);
                        }; break;
                }
            }
        }

        private void ReTouchUp(TouchPoint tp)
        {
            if (tp.TouchDevice.Id != lastPrimaryTouchId) return;

            tp.TouchDevice.Updated -= OnTouchDeviceUpdated;

            double touchOffset = tp.Bounds.Left - touchFirstRect.Left;
            touchFirstRect = Rect.Empty;
            lastPrimaryTouchId = -1;
            if (touchOffset is 0) return;

            if (Math.Truncate(Math.Abs(touchOffset)) > ActualWidth / 3)
            {
                System.Diagnostics.Debug.WriteLine($"Offset:{touchOffset},ActualWidth:{ActualWidth}"); ;
                if (touchOffset < 0)
                {
                    if (SelectedIndex == itemSourceCount - 1)
                    {
                        SelectedIndex = 0;

                        prevInfitiy = ScrollToCarousel(itemSourceCount, completed: () => { ScrollToCarousel(SelectedIndex, TimeSpan.Zero); prevInfitiy = null; });

                        return;
                    }
                    SelectedIndex += 1;
                }
                else // touchOffset > 0 即右滑
                {
                    if (SelectedIndex is 0)
                    {
                        SelectedIndex = itemSourceCount - 1;

                        prevInfitiy = ScrollToCarousel(-1, completed: () => { ScrollToCarousel(SelectedIndex, TimeSpan.Zero); prevInfitiy = null; });

                        return;
                    }
                    SelectedIndex -= 1;
                }
                if (prevInfitiy is not null)
                {
                    prevInfitiy.Stop();
                    prevInfitiy = null;
                }
                ScrollToCarousel(SelectedIndex);
            }
            else
            {
                ScrollToCarousel(SelectedIndex);
            }
        }

        protected override void OnTouchLeave(TouchEventArgs e)
        {
            if (touchFirstRect.IsEmpty) return;

            TouchPoint tp = e.GetTouchPoint(this);

            if (tp.Action is TouchAction.Up)
            {
                ReTouchUp(tp);
            }

            base.OnTouchLeave(e);
        }
        #endregion

        #region AutoPlay Serivce

        private bool changedByTouch;
        private int autoPlayRandomId;
        private void AutoPlayProc()
        {
            SetRandomId();
            int currentId = autoPlayRandomId;

            Dispatcher.BeginInvoke(async () =>
            {
                for (; ; )
                {
                    if (autoPlayRandomId != currentId) return;

                    await Task.Delay(3000);

                    if (itemSourceCount > 1 && IsActive)
                    {

                        var t = Mouse.GetPosition(this);
                        
                        if (t.X < 0 || t.Y < 0 || t.X > ActualWidth || t.Y > ActualHeight)
                        {

                        }
                        else
                        {
                            IInputElement? iie = default;
                            foreach (var touchDev in TouchesOver)
                            {
                                iie ??= touchDev.DirectlyOver;
                            }
                            iie ??= Mouse.DirectlyOver;
                            switch (iie)
                            {
                                case System.Windows.Controls.Border:
                                case System.Windows.Controls.TextBlock:
                                case System.Windows.Documents.Run:
                                    {
                                        continue;
                                    }
                            }
                        }
                        if (!touchFirstRect.IsEmpty) continue;
                        if (changedByTouch)
                        {
                            changedByTouch = false;
                            continue;
                        }

                        //SelectedIndex++;
                        NextPage();
                    }
                }
            }, priority: System.Windows.Threading.DispatcherPriority.Background, null);

        }
        private void SetRandomId()
        {
#if NET
            autoPlayRandomId = Random.Shared.Next(1, int.MaxValue);
#else
            Random r = new Random();

            autoPlayRandomId = r.Next(1, int.MaxValue);
#endif
        }

        #endregion

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged)
            {
                ScrollToCarousel(SelectedIndex, TimeSpan.Zero);
            }
            base.OnRenderSizeChanged(sizeInfo);
        }
    }
}
