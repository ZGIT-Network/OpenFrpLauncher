using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    internal partial class AdSence : ObservableObject
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        private string? _image { get; set; }

        [JsonPropertyName("img")]
        public string? Image
        {
            get => _image;
            set
            {
                _image = value;

                OnPropertyChanged(nameof(UU));
            }
        }

        public Uri UU
        {
            get
            {
                if(Uri.TryCreate(Image, UriKind.RelativeOrAbsolute, out var va))
                {
                    return va;
                }
                throw new NullReferenceException();
            }
        }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
