using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    internal partial class PlatformUser : ObservableObject
    {
        [ObservableProperty]
        private string? username;

        [ObservableProperty]
        private string? emailAddress;

        public string? UserAuthorzation { get; set; }
    }
}
