using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    internal partial class DownloadProcess : ObservableObject
    {
        public DownloadProcess()
        {
            
        }

        [ObservableProperty]
        private string downloadFileUrl = "";

        [ObservableProperty, NotifyPropertyChangedFor(nameof(IsIndeterminate))]
        private bool progressBarShowError;

        public bool IsIndeterminate { get => ProgressValue is 0; }

        [ObservableProperty, NotifyPropertyChangedFor(nameof(IsIndeterminate))]
        private double progressValue;
    }
}
