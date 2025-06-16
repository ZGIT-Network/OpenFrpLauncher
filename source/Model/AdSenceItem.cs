using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;

namespace OpenFrp.Launcher.Model
{
    internal partial class AdSenseItem : ObservableObject
    {
        public AdSenseItem()
        {

        }

        public AdSenseItem(Yue3.Model.OpenFrp.Response.Data.AdSense adSenseSource)
        {
            Title = adSenseSource.Title ?? "";
            Description = adSenseSource.Description ?? "";
            Tag = adSenseSource.Tag ?? "未知";
            Company = adSenseSource.Company ?? "未知";
            ImageSource = adSenseSource.ImageUrl;
            Url = adSenseSource.Link;
        }

        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private string description = "";

        [ObservableProperty]
        private string? url = "";

        [ObservableProperty]
        private string tag = "";

        [ObservableProperty]
        private string company = "";

        public string? ImageSource { get; set; }

        private ImageSource? source;

        public void SetImageSource(ImageSource imageSource) => this.source = imageSource;

        public ImageSource Source
        {
            get
            {
                if (source is null)
                {
                    return new System.Windows.Media.Imaging.BitmapImage();
                }
                return source;
            }
        }
    }
}
