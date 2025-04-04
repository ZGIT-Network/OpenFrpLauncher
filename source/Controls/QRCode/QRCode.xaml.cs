using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using QRCoder;


namespace OpenFrp.Launcher.Controls
{
    public partial class QRCode : ContentControl
    {


        public bool IsCreateQRCode     
        {
            get { return (bool)GetValue(IsCreateQRCodeProperty); }
            set { SetValue(IsCreateQRCodePropertyKey, value); }
        }

        public static DependencyProperty IsCreateQRCodeProperty => IsCreateQRCodePropertyKey.DependencyProperty;
        
        public static readonly DependencyPropertyKey IsCreateQRCodePropertyKey =
            DependencyProperty.RegisterReadOnly("IsCreateQRCode", typeof(bool), typeof(QRCode), new PropertyMetadata(false));



        internal const string ImageState = "DefualtImageCtrl";
        internal const string LoadingState = "DefualtLoadingCtrl";

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return null!;
        }

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("layout") is Border br)
            {
                VisualStateManager.GoToElementState(br, LoadingState, false);
            }
            base.OnApplyTemplate();
        }

        public async Task ApplyQRCodeAsync(string? data,CancellationToken cancellationToken = default)
        {
            if (GetTemplateChild("layout") is Border br &&
                GetTemplateChild("defualtImageContainer") is System.Windows.Controls.Image image)
            {
                image.ClearValue(System.Windows.Controls.Image.SourceProperty);

                if (data is null)
                {
                    VisualStateManager.GoToElementState(br, ImageState, false);
                    IsCreateQRCode = true;

                    return;
                }

                var dpiCalc = VisualTreeHelper.GetDpi(image);

                int rendeWidth = (int)(br.ActualWidth * dpiCalc.DpiScaleX);
                int rendeHeight = (int)(br.ActualHeight * dpiCalc.DpiScaleY);

                //var foregroundFillColor = iNKORE.UI.WPF.Modern.ThemeManager.GetActualTheme(image) is iNKORE.UI.WPF.Modern.ElementTheme.Light ? 
                //    System.Drawing.Color.Black : System.Drawing.Color.White;

                var foregroundFillColor = System.Drawing.Color.Black;

                try
                {
                    QRCodeData qrCodeData = new QRCodeGenerator().CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);

                    Bitmap qrCodeImage = await Task.Run(() => new QRCoder.QRCode(qrCodeData).GetGraphic(100, foregroundFillColor, System.Drawing.Color.Transparent, default, 0, 0, true), cancellationToken);

                    var visualWBitmap = new System.Windows.Media.Imaging.WriteableBitmap(rendeWidth, rendeHeight, dpiCalc.DpiScaleX * 100d, dpiCalc.DpiScaleY * 100d, PixelFormats.Pbgra32, null);

                    visualWBitmap.CopyFromImage(qrCodeImage.GetThumbnailImage(rendeWidth, rendeHeight, null, IntPtr.Zero));

                    if (visualWBitmap.CanFreeze)
                    {
                        visualWBitmap.Freeze();
                    }

                    br.Dispatcher.Invoke(() =>
                    {
                        VisualStateManager.GoToElementState(br, ImageState, false);
                        image.Source = visualWBitmap;
                        IsCreateQRCode = true;
                    });
                }
                catch (System.Threading.Tasks.TaskCanceledException)
                {
                    return;
                }
            }
        }

        public void ClearQRCode()
        {
            if (GetTemplateChild("layout") is Border br)
            {
                IsCreateQRCode = false;
                VisualStateManager.GoToElementState(br, LoadingState, false);
            }
        }
    }
}
