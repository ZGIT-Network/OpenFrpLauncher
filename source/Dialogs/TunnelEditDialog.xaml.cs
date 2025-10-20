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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.ViewModels;

namespace OpenFrp.Launcher.Dialogs
{
    /// <summary>
    /// TunnelEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class TunnelEditDialog : ContentDialog
    {
        public TunnelEditDialog()
        {
            InitializeComponent();
        }


        #region IsCreateMode
        public bool IsCreateMode
        {
            get { return (bool)GetValue(IsCreateModeProperty); }
            set { SetValue(IsCreateModeProperty, value); }
        }

        public static readonly DependencyProperty IsCreateModeProperty =
            DependencyProperty.Register("IsCreateMode", typeof(bool), typeof(TunnelEditDialog), new PropertyMetadata(false));
        #endregion




        private CancellationTokenSource? CancellationTokenSource { get; set; }

        private const string DisplayContainerState = "DisplayContainerCtrl";
        private const string DisplayLoadingState = "DisplayLoadingCtrl";
        private const string DisplayErrorState = "DisplayErrorCtrl";

#if NET
        private async void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (CancellationTokenSource != null)
            {
                await CancellationTokenSource.CancelAsync();
            }
#else
        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            editor.CancelServiceSelecting();
            

            CancellationTokenSource?.Cancel(true);
#endif
        }
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender,ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            if (editor.IsServiceSelecting)
            {
                editor.CancelServiceSelecting();
                return;
            }
#if NET
            CancellationTokenSource ??= new CancellationTokenSource();
            CancellationTokenSource.TryReset();
#else
            CancellationTokenSource = new CancellationTokenSource();
#endif

            if (VisualStateManager.GetVisualStateGroups(this)[0] is VisualStateGroup vsg)
            {
                if (vsg.CurrentState.Name.Equals(DisplayErrorState))
                {
                    UpdateTitle();
                    PrimaryButtonText = "确定";
                    VisualStateManager.GoToElementState(this, DisplayContainerState, false);

                    return;
                }
            }

            sender.IsPrimaryButtonEnabled = false;

            ClearReasonAndException();

            VisualStateManager.GoToElementState(this, DisplayLoadingState, false);

            var cancellationToken = CancellationTokenSource.Token;

            var config = IsCreateMode ? editor.GetCreateConfig() : editor.GetEditConfig();

            var resp = IsCreateMode ?
                await OpenFrp.Service.Net.OpenFrpApi.CreateTunnel(config, cancellationToken) :
                await OpenFrp.Service.Net.OpenFrpApi.EditTunnel(config, cancellationToken);

            if (resp.StatusCode is not System.Net.HttpStatusCode.OK || resp.Data is not { Flag: true })
            {
                SetReasonAndException(resp.Message ?? resp.Data?.Message ?? "未知原因", resp.Exception);
                VisualStateManager.GoToElementState(this, DisplayContainerState, false);
                sender.IsPrimaryButtonEnabled = true;
                return;
            }

            if (editor.EditorTemplate is not null)
            {
                editor.EditorTemplate.Name = null;
                editor.EditorTemplate.RemotePort = null;
            }
            Model.RouteMessage<MainWindowViewModel>.Send(typeof(Views.Tunnels));
            Hide();
        }

        private void SetReasonAndException(string message,Exception? ex)
        {
            infoBar.Dispatcher.Invoke(() =>
            {
                infoBar.Message = message;

                infoBar.DataContext = ex is not null;

                if (ex is not null)
                {
                    errViewer.Exception = ex;
                }
                infoBar.IsOpen = true;
            });
        }
        private void ClearReasonAndException()
        {
            infoBar.Dispatcher.Invoke(() =>
            {
                infoBar.IsOpen = false;

                infoBar.ClearValue(InfoBar.MessageProperty);
                infoBar.ClearValue(InfoBar.DataContextProperty);

                errViewer.ClearValue(Controls.ErrorViewer.ExceptionProperty);
            });
        }

        public override void OnApplyTemplate()
        {
            VisualStateManager.GoToElementState(this, DisplayContainerState, false);

            UpdateTitle();

            SetBinding(ContentDialog.FullSizeDesiredProperty, new Binding("IsServiceSelecting")
            {
                Source = editor,
                Mode = BindingMode.OneWay
            });

            base.OnApplyTemplate();
        }

        private void UpdateTitle(string suffix = "")
        {
            if (IsCreateMode)
            {
                Title = "新建隧道" + suffix;
            }
            else if (GetValue(Controls.TunnelConfEditor.EditorTemplateProperty) is Model.TunnelEditorTemplate { } template)
            {
                Title = $"编辑隧道 #{template.Id} {template.Name}" + suffix;
            }
        }

        private void DisplayExceptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement {  } fe)
            {
                UpdateTitle(" - 显示错误");
                PrimaryButtonText = "返回";
                VisualStateManager.GoToElementState(this, DisplayErrorState, false);
            }
        }
    }
}
