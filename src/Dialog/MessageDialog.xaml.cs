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
using CommunityToolkit.Mvvm.Input;
using static Google.Protobuf.WellKnownTypes.Field.Types;

namespace OpenFrp.Launcher.Dialog
{
    /// <summary>
    /// MessageDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MessageDialog : UserControl
    {
        public MessageDialog()
        {
            InitializeComponent();
        }



        public FrameworkElement Title
        {
            get { return (FrameworkElement)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(FrameworkElement), typeof(MessageDialog), new PropertyMetadata());



        public FrameworkElement Description
        {
            get { return (FrameworkElement)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Description.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(FrameworkElement), typeof(MessageDialog), new PropertyMetadata());



        public Window MainWindow
        {
            get { return (Window)GetValue(MainWindowProperty); }
            set { SetValue(MainWindowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MainWindow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MainWindowProperty =
            DependencyProperty.Register("MainWindow", typeof(Window), typeof(MessageDialog), new PropertyMetadata());



        public string PrimaryButtonText
        {
            get { return (string)GetValue(PrimaryButtonTextProperty); }
            set { SetValue(PrimaryButtonTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrimaryText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrimaryButtonTextProperty =
            DependencyProperty.Register("PrimaryButtonText", typeof(string), typeof(MessageDialog), new PropertyMetadata());



        public FrameworkElement PrimaryButtonIcon
        {
            get { return (FrameworkElement)GetValue(PrimaryButtonIconProperty); }
            set { SetValue(PrimaryButtonIconProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrimaryIcon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrimaryButtonIconProperty =
            DependencyProperty.Register("PrimaryButtonIcon", typeof(FrameworkElement), typeof(MessageDialog), new PropertyMetadata());



        public string CloseButtonText
        {
            get { return (string)GetValue(CloseButtonTextProperty); }
            set { SetValue(CloseButtonTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CloseButtonText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CloseButtonTextProperty =
            DependencyProperty.Register("CloseButtonText", typeof(string), typeof(MessageDialog), new PropertyMetadata());

        public TaskCompletionSource<MessageDialogResult> OutTaskCompletionSource { get; private set; } = new TaskCompletionSource<MessageDialogResult>();



        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(MessageDialog), new PropertyMetadata());

        public CancellationTokenSource CancellationTokenSource { get; private set; } = new CancellationTokenSource();

        public Func<MessageDialog,CancellationToken,Task<bool>> InvokeAction { get; set; } = new Func<MessageDialog,CancellationToken, Task<bool>>(async delegate
        {
            {
                return await Task.FromResult(false);
            }
        });

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("primary") is Button btn)
            {
                btn.Command = new AsyncRelayCommand(async () =>
                {
                    IsLoading = true;
                    try
                    {
                        if (await InvokeAction(this,CancellationTokenSource.Token))
                        {
                            //if (CancellationTokenSource.IsCancellationRequested)
                            //{
                            //    return;
                            //}
                            OutTaskCompletionSource.SetResult(MessageDialogResult.Primary);
                        }
                    }
                    catch { }
                    IsLoading = false;
                });
            }

            if (GetTemplateChild("close") is Button btn2)
            {
                btn2.Command = new RelayCommand(() =>
                {
                    OutTaskCompletionSource.SetResult(MessageDialogResult.Close);
                });
            }

            if (GetTemplateChild("closeWind") is Awe.UI.Controls.NavigationButton btn3)
            {
                btn3.Command = new RelayCommand(() =>
                {
                    CancellationTokenSource.Cancel();
                    OutTaskCompletionSource.SetResult(MessageDialogResult.CloseDialog);
                });
            }

            base.OnApplyTemplate();
        }

        public async Task<MessageDialogResult> ShowDialog()
        {
            if (App.Current?.MainWindow is Window wind)
            {
                MainWindow = wind;

                wind.SetCurrentValue(Awe.UI.Helper.WindowsHelper.DialogOpennedProperty, true);
                wind.SetCurrentValue(Awe.UI.Helper.WindowsHelper.DialogContentProperty, this);

                var vw = await OutTaskCompletionSource.Task;

                wind.Dispatcher.Invoke(async () =>
                {
                    wind.SetValue(Awe.UI.Helper.WindowsHelper.DialogOpennedProperty, false);
                    await Task.Delay(250);
                    wind.SetCurrentValue(Awe.UI.Helper.WindowsHelper.DialogContentProperty, DependencyProperty.UnsetValue);
                }).CreationOptions.Equals(null);

                return vw;
            }
            throw new NullReferenceException("Cannot found MainWindow");
        }
    }

    public enum MessageDialogResult
    {
        CloseDialog,
        Primary,
        Close
    }

    
}
