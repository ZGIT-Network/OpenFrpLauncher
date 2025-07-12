using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    internal partial class HomeUserInfoVex2 : ObservableObject
    {
        public HomeUserInfoVex2(string icon,string title,string description)
        {
            Icon = icon;
            Title = title;
            Description = description;
        }

        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private string description = "";

        [ObservableProperty]
        private string icon = "";
    }
}
