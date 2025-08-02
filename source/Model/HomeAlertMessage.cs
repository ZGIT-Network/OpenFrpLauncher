using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using iNKORE.UI.WPF.Modern.Controls;

namespace OpenFrp.Launcher.Model
{
    internal partial class HomeAlertMessage : ObservableValidator
    {
        public HomeAlertMessage() { }

        public HomeAlertMessage(string title, string type, string[] data)
        {
            Title = title;
            Type = type switch
            {
                "warn" => InfoBarSeverity.Warning,
                "error" => InfoBarSeverity.Error,
                "success" => InfoBarSeverity.Success,
                _ => InfoBarSeverity.Informational
            };
            Data = data;

            Message = string.Join("\n",data);
        }

        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private InfoBarSeverity type = InfoBarSeverity.Informational;

        [ObservableProperty]
        private string[] data  = Array.Empty<string>();

        [ObservableProperty]
        private string message = "";
    }
}
