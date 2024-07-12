using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenFrp.Launcher.Model;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        public OpenSourceRefs[] refs = new OpenSourceRefs[]
        {
            new OpenSourceRefs()
            {
                Name = "ModernWpf (@where-where)",
                Descirption = "0.10.1"
            },
            new OpenSourceRefs()
            {
                Name = "H.NotifyIcon",
                Descirption = "2.1.0"
            },
            new OpenSourceRefs()
            {
                Name = "CommunityToolkit.Mvvm",
                Descirption = "8.2.2",
            },
            new()
            {
                Name = "Microsoft.AppCenter",
                Descirption = "5.0.5"
            },
            new OpenSourceRefs
            {
                Name = "Microsoft.Web.WebView2",
                Descirption = "1.0.2592.51"
            },
            new OpenSourceRefs
            {
                Name = "Microsoft.Xaml.Behaviors.Wpf",
                Descirption = "1.1.122",
                    
            }
        };
    }
}
