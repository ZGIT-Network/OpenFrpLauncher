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
    internal partial class CreateTunnelViewModel : ObservableObject, IHrViewModel
    {
        internal const string LoadingState = "DisplayLoadingCtrl";
        internal const string ErrorState = "DisplayErrorCtrl";
        internal const string NormalState = "DisplayContainerCtrl";

        private FrameworkElement? container;

        [ObservableProperty]
        private ObservableCollection<Model.Node> nodes = new ObservableCollection<Model.Node> { };

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is iNKORE.UI.WPF.Modern.Controls.Page page)
            {
                container = page;

                VisualStateManager.GoToElementState(container, LoadingState, false);
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshNodeList(CancellationToken cancellationToken)
        {
            var resp = await Service.Net.OpenFrpApi.GetNodes(cancellationToken);

            if (!this.UpdateState(resp, () => resp.Data is { List: not null }))
            {
                VisualStateManager.GoToElementState(container, ErrorState, false);
            }
            else
            {
                VisualStateManager.GoToElementState(container, NormalState, false);

                foreach (var node in resp.Data!.List!)
                {
                    var v = new Model.Node(node);
                    
                    Nodes.Add(v);

                    await Task.Delay(75, cancellationToken);
                }
            }
        }

        [ObservableProperty]
        private Model.ExecuteResult? executeResult;
    }
}
