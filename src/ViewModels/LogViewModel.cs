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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using Microsoft.Win32;
using static OpenFrp.Service.Proto.Response.LogResponse.Types;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class LogViewModel : ObservableObject
    {
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private ObservableCollection<Service.Proto.Response.LogResponse.Types.LogData>? logs;

        [ObservableProperty]
        private ObservableCollection<Service.Proto.Response.ActiveProcessResponse.Types.ActiveProcess>? processes;

        [ObservableProperty]
        private Service.Proto.Response.ActiveProcessResponse.Types.ActiveProcess? selectedProcess;

        [ObservableProperty]
        private int selectedIndex;

        private ItemsControl? itemsControl;

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs arg)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Logs = new ObservableCollection<Service.Proto.Response.LogResponse.Types.LogData>();
            Processes = new ObservableCollection<Service.Proto.Response.ActiveProcessResponse.Types.ActiveProcess>();
            if (arg.Source is FrameworkElement fe)
            {
                fe.Unloaded += delegate
                {
                    _cancellationTokenSource?.Cancel();
                };
                //@event_RefreshData();
                _ = fe.Dispatcher.Invoke(async () =>
                {
                    await RpcManager.LogStream((x) =>
                    {
                        foreach (var item in x.Logs)
                        {
                            Logs?.Add(item);
                        }
                    }, cancellationToken: _cancellationTokenSource.Token);
                });
                _ = fe.Dispatcher.Invoke(async () =>
                {
                    var resp = await RpcManager.GetActiveProcess();
                    if (resp.IsSuccess && resp.Data is { Processes.Count: > 0 } data)
                    {
                        foreach (var item in data.Processes)
                        {
                            Processes?.Add(item);
                        }
                    }
                });
            }
        }

        [RelayCommand]
        private void @event_ItemsControlLoaded(RoutedEventArgs arg)
        {
            if (arg.Source is ItemsControl ic)
            {
                ic.Items.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
                itemsControl = ic;
            }
        }

        [RelayCommand]
        private async Task @event_SaveLog()
        {
            if (Logs is null) return;

            var fDialog = new Microsoft.Win32.SaveFileDialog
            {
                OverwritePrompt = true,
                AddExtension = true,
                ValidateNames = true,
                Filter = "日志文件(*.log)|*.log"
            };
            if (fDialog.ShowDialog() is true)
            {
                FileStream fs = File.Open(fDialog.FileName, FileMode.OpenOrCreate);

                fs.SetLength(0);

                foreach (LogData logData in Logs)
                {
                    if (SelectedProcess?.Id is 0 || logData.Id != SelectedProcess?.Id) continue;

                    byte[] bytes = new byte[] { 10 };
                    if (logData.Content.Length > 0)
                    {
                        bytes = Encoding.UTF8.GetBytes($"{DateTimeOffset.FromUnixTimeMilliseconds(logData.Date).LocalDateTime.ToString("yyyy/MM/dd HH:mm:ss")} {logData.Executor} {logData.Content}\n");
                    }
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                }
                fs.Close();
            }
        }

        [RelayCommand]
        private async Task @event_ClearLog()
        {
            var resp = await RpcManager.ClearLogStream();
            if (resp.IsSuccess)
            {
                Logs?.Clear();
            }
            else
            {
                MessageBox.Show(resp.Message);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedProcess) && SelectedProcess != null && itemsControl != null)
            {
                if (SelectedProcess.Id == 0)
                {
                    itemsControl.Items.Filter = null;
                    return;
                }
                itemsControl.Items.Filter = delegate (object x)
                {
                    if (x is Service.Proto.Response.LogResponse.Types.LogData data && SelectedProcess is not null)
                    {
                        return data.Id.Equals(SelectedProcess.Id);
                    }
                    return false;
                };
            }

            base.OnPropertyChanged(e);
        }
    }
}
