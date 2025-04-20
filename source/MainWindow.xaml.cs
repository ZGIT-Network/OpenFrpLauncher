using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using iNKORE.UI.WPF.Modern.Controls;



namespace OpenFrp.Launcher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.DataContext = new ViewModels.MainWindowViewModel();

            InitializeComponent();

            this.SetBinding(iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty, new Binding
            {
                Source = App.Settings,
                Path = new PropertyPath(nameof(App.Settings.ApplicationTheme)),
                Mode = BindingMode.OneWay
            });
            iNKORE.UI.WPF.Modern.Controls.Helpers.WindowHelper.SetSystemBackdropType(this,App.Settings.BackdropType);

            hWnd = new WindowInteropHelper(this).EnsureHandle();
        }

        public MainWindow(Yue3.Model.OpenFrp.Response.Data.UserInfoData userInfo)
        {
            this.DataContext = new ViewModels.MainWindowViewModel(userInfo);

            InitializeComponent();

            this.SetBinding(iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty, new Binding
            {
                Source = App.Settings,
                Path = new PropertyPath(nameof(App.Settings.ApplicationTheme)),
                Mode = BindingMode.OneWay
            });
            iNKORE.UI.WPF.Modern.Controls.Helpers.WindowHelper.SetSystemBackdropType(this, App.Settings.BackdropType);

            hWnd = new WindowInteropHelper(this).EnsureHandle();
        }

        public MainWindow(bool daemonState)
        {
            this.DataContext = new ViewModels.MainWindowViewModel(daemonState);

            InitializeComponent();

            this.SetBinding(iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty, new Binding
            {
                Source = App.Settings,
                Path = new PropertyPath(nameof(App.Settings.ApplicationTheme)),
                Mode = BindingMode.OneWay
            });
            iNKORE.UI.WPF.Modern.Controls.Helpers.WindowHelper.SetSystemBackdropType(this, App.Settings.BackdropType);

            hWnd = new WindowInteropHelper(this).EnsureHandle();
        }

        private readonly IntPtr hWnd;

        public void ShowByHwndCC()
        {
            if (hWnd != IntPtr.Zero)
            {
                Win32.User32.ShowWindow(hWnd, Win32.User32.SW_TYPE.SW_SHOW);

                if (WindowState is WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                if (Win32.User32.GetForegroundWindow() != hWnd)
                {
                    Win32.User32.SetForegroundWindow(hWnd);
                }
            }
        }

        public void HideByHwndCC()
        {
            if (hWnd != IntPtr.Zero)
            {
                Win32.User32.ShowWindow(hWnd, Win32.User32.SW_TYPE.SW_HIDE);
            }
        }

        public void SetCCWindowState(bool flag)
        {
            if (hWnd != IntPtr.Zero)
            {
                Win32.User32.EnableWindow(hWnd, flag);
            }
        }

        //protected override void OnClosing(CancelEventArgs e)
        //{


        //    Application.Current.Shutdown();
        //}

        internal async void ShowAlert(string title,string message,InfoBarSeverity severity)
        {
            if (FindName("uiAlert") is iNKORE.UI.WPF.Controls.SimpleStackPanel { Children: var cr })
            {
                // he
                var storyboard = new Storyboard();

                var doubleAnimation = new DoubleAnimation()
                {
                    From = 0,
                    To = 20
                };
                var opacityDoubleAnimation = new DoubleAnimation()
                {
                    From = 1,
                    To = 0
                };
                // 展示
                var hitAnimation = new BooleanAnimationUsingKeyFrames();
                {
                    hitAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame()
                    {
                        KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                        Value = false,
                    });
                    hitAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame()
                    {
                        KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)),
                        Value = true,
                    });
                }
                // 关闭
                var toffHitAnimation = new BooleanAnimationUsingKeyFrames();
                {
                    toffHitAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame()
                    {
                        KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                        Value = true,
                    });
                    toffHitAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame()
                    {
                        KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)),
                        Value = false,
                    });
                }

                Storyboard.SetTargetProperty(opacityDoubleAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTargetProperty(hitAnimation, new PropertyPath(UIElement.IsHitTestVisibleProperty));
                Storyboard.SetTargetProperty(toffHitAnimation, new PropertyPath(UIElement.IsHitTestVisibleProperty));
                Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

                opacityDoubleAnimation.Duration = doubleAnimation.Duration = hitAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(250));

                opacityDoubleAnimation.EasingFunction = doubleAnimation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };

                if (cr.Count >= 4 && cr[0] is InfoBar ifce)
                {
                    doubleAnimation.From = 0;
                    doubleAnimation.To = 0;

                    storyboard.Children.Add(opacityDoubleAnimation);
                    storyboard.Children.Add(doubleAnimation);
                    storyboard.Children.Add(toffHitAnimation);
                    Storyboard.SetTarget(opacityDoubleAnimation, ifce);
                    Storyboard.SetTarget(doubleAnimation, ifce);
                    Storyboard.SetTarget(toffHitAnimation, ifce);

                    ifce.BeginStoryboard(storyboard);

                    await Task.Delay(150);

                    if (cr.Count >= 1 && cr.Contains(ifce))
                    {
                        cr.Remove(ifce);
                        storyboard.Children.Clear();
                    }
                }

                storyboard.Children.Add(opacityDoubleAnimation);
                storyboard.Children.Add(doubleAnimation);
                storyboard.Children.Add(hitAnimation);

                var infoBar = new InfoBar
                {
                    IsOpen = true,
                    Title = title,
                    Message = message,
                    Severity = severity,
                    RenderTransform = new TranslateTransform { Y = 0 }
                };
                cr.Add(infoBar);

                Storyboard.SetTarget(opacityDoubleAnimation, infoBar);
                Storyboard.SetTarget(doubleAnimation, infoBar);
                Storyboard.SetTarget(hitAnimation, infoBar);
                opacityDoubleAnimation.From = 0;
                opacityDoubleAnimation.To = 1;

                doubleAnimation.From = 20;
                doubleAnimation.To = 0;

                infoBar.BeginStoryboard(storyboard);
                infoBar.Closed += delegate
                {
                    if (cr.Count >= 1 && cr.Contains(infoBar))
                    {
                        opacityDoubleAnimation.Freeze();
                        doubleAnimation.Freeze();
                        hitAnimation.Freeze();
                        toffHitAnimation.Freeze();

                        cr.Remove(infoBar);
                    }
                };

                await Task.Delay(5000);

                if (cr.Contains(infoBar))
                {
                    opacityDoubleAnimation.From = 1;
                    opacityDoubleAnimation.To = 0;

                    doubleAnimation.From = 0;
                    doubleAnimation.To = 10;

                    infoBar.BeginStoryboard(storyboard);
                    await Task.Delay(350);

                    cr.Remove(infoBar);

                    opacityDoubleAnimation.Freeze();
                    doubleAnimation.Freeze();
                    hitAnimation.Freeze();
                    toffHitAnimation.Freeze();
                }
            
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!e.Cancel)
            {
                BindingOperations.ClearBinding(this, iNKORE.UI.WPF.Modern.ThemeManager.RequestedThemeProperty);
            }
        }

        internal CommunityToolkit.Mvvm.ComponentModel.ObservableObject? FrameContentViewModel
        {
            get
            {
                if (this.frame.Content is FrameworkElement { DataContext: not null and CommunityToolkit.Mvvm.ComponentModel.ObservableObject vm } fe)
                {
                    return vm;
                }
                return default;
            }
        }
    }
}
