using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
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

                page.Unloaded += delegate
                {
                    event_RefreshNodeListCommand.Cancel();
                    event_DisplayExceptionCommand.Cancel();
                };

                VisualStateManager.GoToElementState(container, LoadingState, false);

                event_RefreshNodeListCommand.Execute(default);

                if (page.FindName("itemsController") is ItemsControl itemsController)
                {
                    itemsController.Items.SortDescriptions.Add(new SortDescription("NodeClassify", ListSortDirection.Ascending));
                    itemsController.Items.SortDescriptions.Add(new SortDescription("NodeId", ListSortDirection.Ascending));
                }
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_DisplayException(CancellationToken cancellationToken)
        {
            if (ExecuteResult is { HasException: true, Exception: not null and Exception ex })
            {
                var dialog = new Dialogs.ErrorContentDialog
                {
                    
                };

                cancellationToken.Register(dialog.Hide);

                dialog.SetValue(Controls.ErrorViewer.ExceptionProperty, ex);
                await dialog.ShowAsync();
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshNodeList(CancellationToken cancellationToken)
        {
            Nodes.Clear();

            VisualStateManager.GoToElementState(container, LoadingState, false);

            var resp = await Service.Net.OpenFrpApi.GetNodes(cancellationToken);

            await Task.Delay(1000, cancellationToken);

            if (!this.UpdateState(resp, () => resp.Data is { List: not null }))
            {
                VisualStateManager.GoToElementState(container, ErrorState, false);
            }
            else
            {
                VisualStateManager.GoToElementState(container, NormalState, false);

                Nodes.Add(new Model.Node("中国大陆",Yue3.Model.OpenFrp.Response.Data.NodeClassify.ChinaMainland));
                Nodes.Add(new Model.Node("中国香港 | 中国台湾 | 中国澳门",Yue3.Model.OpenFrp.Response.Data.NodeClassify.ChinaHTM));
                Nodes.Add(new Model.Node("外国节点",Yue3.Model.OpenFrp.Response.Data.NodeClassify.Foreign));

                foreach (var node in resp.Data!.List!)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var v = new Model.Node(node);
                    
                    Nodes.Add(v);

                    await Task.Delay(50, cancellationToken);
                }
            }
        }

        [RelayCommand]
        private void @event_SelectNodeAndCrete(Model.Node node)
        {
            var editDialog = new Dialogs.TunnelEditDialog
            {
                IsCreateMode = true
            };
            editDialog.SetValue(Controls.TunnelConfEditor.NodeProperty, node);
            editDialog.Dispatcher.Invoke(async () =>
            {
                await editDialog.ShowAsync();
            });
        }

        [ObservableProperty]
        private Model.ExecuteResult? executeResult;
    }
}
