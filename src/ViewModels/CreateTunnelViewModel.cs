using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Awe.Model.OpenFrp.Response.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class CreateTunnelViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Awe.Model.OpenFrp.Response.Data.NodeInfo> nodeList = new ObservableCollection<Awe.Model.OpenFrp.Response.Data.NodeInfo>();

        private Controls.TunnelConfig? _configWrapper;

        [RelayCommand]
        private void @event_ConfigWrapperLoaded(RoutedEventArgs e)
        {
            if (e is { Source: Controls.TunnelConfig tc })
            {
                _configWrapper = tc;

                tc.TunnelData = new UserTunnel();
            }
        }

        [RelayCommand]
        private async Task @event_PageLoaded(RoutedEventArgs e)
        {
            if (e is { Source: Views.CreateTunnel page })
            {
                var resp = await Service.Net.OpenFrp.GetNodes();

                if (resp.Exception is null && resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { List: var list })
                {
                    NodeList.Clear();

                    if (page.FindName("sortContainer") is System.Windows.Controls.ListBox lb)
                    {
                        lb.Items.SortDescriptions.Clear();
                        if (App.Current?.MainWindow is { Dispatcher: var dis })
                        {
                            _ = dis.Invoke(async () =>
                            {
                                NodeList.Clear();

                                NodeList.Add(new NodeInfo
                                {
                                    Id = 0,
                                    Name = "中国大陆",
                                    Classify = NodeClassify.ChinaMainland
                                });
                                NodeList.Add(new NodeInfo
                                {
                                    Id = 0,
                                    Name = "中国香港 | 中国台湾 | 中国澳门",
                                    Classify = NodeClassify.ChinaHongKong
                                });
                                NodeList.Add(new NodeInfo
                                {
                                    Id = 0,
                                    Name = "外国节点",
                                    Classify = NodeClassify.Foreign
                                });

                                lb.Items.SortDescriptions.Add(new SortDescription("Classify", ListSortDirection.Ascending));
                                lb.Items.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

                                foreach (var item in resp.Data!.List!)
                                {
                                    NodeList.Add(item);
                                    
                                    await Task.Delay(20);
                                }

                               


                                OnPropertyChanged(nameof(NodeList));
                            }, priority: System.Windows.Threading.DispatcherPriority.Background);
                        }
                    }
                }
            }
        }

        [RelayCommand]
        private async Task @event_CreateTunnel()
        {
            if (_configWrapper is null) return;

            var conf = _configWrapper.GetCreateConfig();

            var resp = await Service.Net.OpenFrp.CreateUserTunnel(conf);

            if (resp.Exception is null && resp.StatusCode is System.Net.HttpStatusCode.OK)
            {
                // success
                WeakReferenceMessenger.Default.Send(typeof(Views.Tunnels));
            }
            else
            {
                MessageBox.Show(resp.Message, "Create Tunnel", MessageBoxButton.OK, MessageBoxImage.Stop); ;
            }
        }
    }
}
