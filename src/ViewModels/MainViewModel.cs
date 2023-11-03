using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {

        private ContentControl? _frame;

        private bool CanFrameLoadExecute() => _frame is null;

        [RelayCommand(CanExecute = nameof(CanFrameLoadExecute))]
        private void @event_FrameLoaded(RoutedEventArgs e)
        {
            if (e.Source is ContentControl fr)
            {
                _frame = fr;

                event_FrameLoadedCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private async Task @event_SidebarItemInvoked(Type page)
        {

            VisualStateManager.GoToElementState(_frame, "_fr_Doing",false);

            await Task.Delay(125);

            if (_frame is not null && page is not null)
                _frame.Content = Activator.CreateInstance(page);

            VisualStateManager.GoToElementState(_frame, "_fr_Did", false);
        }
    }
}
