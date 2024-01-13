using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class SocketMonitorViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> connections = new ObservableCollection<string>();

        [RelayCommand]
        private async Task @event_CreateStreaming()
        {
            await Task.CompletedTask;
        }
    }
}
