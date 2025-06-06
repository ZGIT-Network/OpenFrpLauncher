using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;

namespace OpenFrp.Launcher.Model
{
    internal partial class AdSenseItem : ObservableObject
    {
        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private string content = "";

        [ObservableProperty]
        private string? link = "";

        public string? ImageSource { get; set; }

        public ImageSource Source
        {
            get
            {
                if (ImageSource is null || !Uri.TryCreate(ImageSource,UriKind.RelativeOrAbsolute,out Uri? ur) || ur is null)
                { 
                    ur = new Uri("pack://application:,,,/Resources/Images/funx.png");
                }
                return new BitmapImage(ur);
            }
        }
    }
}
