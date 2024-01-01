using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Awe.Model.OpenFrp.Response.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Win32;
using OpenFrp.Service.Proto.Request;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class LogViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<OpenFrp.Service.Proto.Response.LogClientResponse.Types.LogClient>? logClients;

        [ObservableProperty]
        private ObservableCollection<OpenFrp.Service.Proto.Response.LogDataResponse.Types.LogData> logDatas = new ObservableCollection<Service.Proto.Response.LogDataResponse.Types.LogData>();

        [ObservableProperty]
        private OpenFrp.Service.Proto.Response.LogClientResponse.Types.LogClient? selectedClient;
        //private ObservableCollection<>

        private ItemsControl? itemsControl;

        private Grpc.Core.AsyncDuplexStreamingCall<LogDataRequest,Service.Proto.Response.LogDataResponse>? asyncDuplexStreaming;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedClient))
            {
                if (SelectedClient is not null && itemsControl is not null)
                {
                    if (SelectedClient.LogId is 0)
                    {
                        itemsControl.Items.Filter = null;

                        return;
                    }
                    itemsControl.Items.Filter = new Predicate<object>((x) =>
                    {
                        if (x is OpenFrp.Service.Proto.Response.LogDataResponse.Types.LogData ld)
                        {
                            if (ld.LogId.Equals(SelectedClient.LogId))
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                }
            }

            base.OnPropertyChanged(e);
        }

        [RelayCommand]
        private async Task @event_PageLoaded()
        {
            try
            {
                asyncDuplexStreaming = App.RemoteClient.GetLogStream();

                var va = await App.RemoteClient.GetLogClientAsync(new Service.Proto.Request.LogClientRequest { });

                if (App.Current is { MainWindow: var dis })
                {
                    Task.Run(async () =>
                    {
                        await asyncDuplexStreaming.RequestStream.WriteAsync(new LogDataRequest
                        {
                            LogId = 0
                        });
                        if (asyncDuplexStreaming is not null)
                        {
                            while (await asyncDuplexStreaming.ResponseStream.MoveNext(CancellationToken.None))
                            {
                                dis.Dispatcher.Invoke(() =>
                                {
                                    foreach (var item in asyncDuplexStreaming.ResponseStream.Current.LogData)
                                    {
                                        LogDatas.Add(item);
                                    }
                                    OnPropertyChanged(nameof(LogDatas));
                                }, priority: System.Windows.Threading.DispatcherPriority.Background);
                            }
                            await Task.Delay(200);
                        }
                    }).GetAwaiter();

                    if (LogClients is null)
                    {
                        LogClients = new ObservableCollection<Service.Proto.Response.LogClientResponse.Types.LogClient>(va.LogClient);

                        return;
                    }
                }
            }
            catch
            {
                await Task.Delay(1500);

                await event_PageLoadedCommand.ExecuteAsync(null);
            }
        }

        [RelayCommand]
        private void @event_ItemsContainerLoaded(RoutedEventArgs e)
        {
            if (e.Source is ItemsControl isc)
            {
                itemsControl = isc;

                // 在这做个筛选器是个聪明的选择
                isc.Items.IsLiveFiltering = true;
                isc.Items.Filter = null;
                isc.Items.SortDescriptions.Add(new SortDescription("TimeZone", ListSortDirection.Ascending));
            }
        }

        [RelayCommand]
        private async Task @event_RefreshRequest()
        {
            try
            {
                LogDatas.Clear();

                if (asyncDuplexStreaming is not null)
                {
                    await asyncDuplexStreaming.RequestStream.WriteAsync(new LogDataRequest
                    {
                        LogId = 0
                    });
                }
            }
            catch
            {

            }
        }

        [RelayCommand]
        private async Task @event_SaveFileRequest()
        {
            var dialog = new SaveFileDialog()
            {
                OverwritePrompt = true,
                AddExtension = true,
                
                ValidateNames = true,
                Filter = "日志文件(*.log)|*.log"
            };
            if (dialog.ShowDialog() is true)
            {
                var fs = File.Open(dialog.FileName, FileMode.OpenOrCreate);
                foreach (var item in LogDatas)
                {
                    var data = Encoding.UTF8.GetBytes($"{DateTimeOffset.FromUnixTimeMilliseconds(item.TimeZone).ToOffset(TimeSpan.FromHours(8)).ToString("yyyy/MM/dd HH:mm:ss")} [{IntToLevel(item.Level)}] {item.Data}\n");
                    await fs.WriteAsync(data,0,data.Length);
                }
                fs.Close();
            }

            await Task.CompletedTask;

            string IntToLevel(int val) => val switch
            {
                1 => "[E]",
                2 => "[W]",
                3 => "[I]",
                _ => "[D]"
            };
        }
    }
}
