﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Awe.Model.OpenFrp.Response.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenFrp.Launcher.Model;
using AppNetwork = OpenFrp.Service.Net;
using NodeInfo = OpenFrp.Launcher.Model.NodeInfo;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class CreateTunnelViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<NodeInfo>? nodes;

        [ObservableProperty]
        private Awe.Model.ApiResponse<Awe.Model.OpenFrp.Response.Data.NodeInfoData>? response;

        [RelayCommand]
        private async Task @event_RefreshNodeCollection()
        {
            Response = await AppNetwork.OpenFrp.GetNodes();

            if (Response.StatusCode is System.Net.HttpStatusCode.OK && Response.Exception is null &&
                Response.Data is { List: var list } && list is not null)
            {
                var nodesStatusResp = await OpenFrp.Service.Net.OpenFrp.GetNodeStatus().WithTimeout(
                    delay: 5000);

                Nodes = new ObservableCollection<NodeInfo>();

                if (App.Current is { Dispatcher: var dispatcher })
                {
                    await Task.Delay(1500);

                    _ = dispatcher.Invoke(async () =>
                    {
                        Nodes.Add(new NodeInfo
                        {
                            Id = 0,
                            Name = "中国大陆",
                            Classify = NodeClassify.ChinaMainland,
                            Status = System.Net.HttpStatusCode.OK
                        });
                        Nodes.Add(new NodeInfo
                        {
                            Id = 0,
                            Name = "\n中国香港 | 中国台湾 | 中国澳门",
                            Classify = NodeClassify.ChinaHongKong,
                            Status = System.Net.HttpStatusCode.OK
                        });
                        Nodes.Add(new NodeInfo
                        {
                            Id = 0,
                            Name = "\n外国节点",
                            Classify = NodeClassify.Foreign,
                            Status = System.Net.HttpStatusCode.OK
                        });

                        if (nodesStatusResp is not null && nodesStatusResp.StatusCode is System.Net.HttpStatusCode.OK && nodesStatusResp.Data is { Count: > 0 } hs)
                        {
                            foreach (var tunnel in list)
                            {
                                if (tunnel.Id is 0) continue;

                                var va = nodesStatusResp.Data.Where(x => { return x.NodeId == tunnel.Id; }).FirstOrDefault();

                                if (va is null) continue;

                                Nodes.Add(new NodeInfo(tunnel)
                                {
                                    PressureLevel = (int)(Math.Round(va.ClientCount / va.Max, 2) * 100)
                                });
                                await Task.Delay(50);
                            }
                        }
                        else
                        {
                            foreach (var tunnel in list)
                            {
                                Nodes.Add(new NodeInfo(tunnel));

                                await Task.Delay(50);
                            }
                        }
                    }, priority: System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        [RelayCommand]
        private async Task @event_ShowEditConfigDialog(Awe.Model.OpenFrp.Response.Data.NodeInfo ni)
        {
           
            try
            { 
                var dialog = new Dialog.EditTunnelDialog
                {
                
                };
                dialog.SetValue(Controls.TunnelEditor.NodeInfoProperty, ni);
                dialog.SetValue(Controls.TunnelEditor.IsCreateModeProperty, true);
                dialog.ResetData();

                if (await dialog.WaitForFinishAsync() is true)
                {
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(typeof(Views.Tunnels)));
                    //_ = event_RefreshNodeCollectionCommand.ExecuteAsync(null);
                }
            }
            catch { }
            
        }

        [RelayCommand]
        private void @event_ConfigureSorter(RoutedEventArgs arg)
        {
            if (arg is { Source: ItemsControl c })
            {
                c.Items.SortDescriptions.Add(new SortDescription("Classify", ListSortDirection.Ascending));
                c.Items.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            }
        }
    }
}
