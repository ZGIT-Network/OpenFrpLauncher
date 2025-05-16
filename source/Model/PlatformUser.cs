using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.System;

namespace OpenFrp.Launcher.Model
{
    internal partial class PlatformUser : ObservableObject
    {
        public PlatformUser()
        {

        }
        public PlatformUser(OpenFrp.Launcher.Properties.Settings.UserProperty up)
        {
            EmailAddress = up.Email;
            Username = up.User;
            UserAuthorzation = up.Authorization;
            UserAvatorHash = up.UserAvator;
        }

        [ObservableProperty]
        private string? username;

        [ObservableProperty]
        private string? emailAddress;

        [ObservableProperty]
        private string? userAvatorHash;

        public string? UserAuthorzation { get; set; }

        public string? AutoLoginId { get => OpenFrp.Service.Helpers.HashAlgorithmHelper.ComputeHashString($"{Username ?? throw new NotSupportedException("User is null;")} 201 ER"); }
    }
}
