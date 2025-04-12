using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.Logging;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class LogsViewModel : ObservableObject
    {
        public LogsViewModel()
        {
            if (Application.Current.MainWindow is { DataContext: MainWindowViewModel mv })
            {
                _mainWindowViewModel = mv;

                ShowAlertAction = mv.ShowAlert;

                mv.LogsCache ??= new ObservableCollection<Service.Proto.Response.LogStreamResponse.Types.LogContainer>();
                mv.PropertyChanged += (_, e) =>
                {
                    OnPropertyChanged(e.PropertyName);
                };
                
            }
            else
            {
                throw new NullReferenceException(nameof(_mainWindowViewModel));
            }

            conve_CreateStreamCommand.Execute(default);
        }

        private readonly Action<string, string, InfoBarSeverity> ShowAlertAction = delegate { };
        private readonly MainWindowViewModel _mainWindowViewModel;
        private IDisposable? disposableStream;

#if NET
        [GeneratedRegex(@"\[I?E?W?D?\] ")]
        private static partial Regex LogLevelRegexFallback();
#elif NETFRAMEWORK
        private static Regex LogLevelRegexFallback() => new Regex(@"\[I?E?W?D?\] ");
#endif


        private Regex LogLevelRegex = LogLevelRegexFallback();

        private System.Windows.Controls.ListView? listView;

        // please one time binding
        //public int DefualtIndex { get => 0; }

        [ObservableProperty]
        private ObservableCollection<Service.Proto.Response.LogStreamResponse.Types.LogLive> tagSelector = new ObservableCollection<Service.Proto.Response.LogStreamResponse.Types.LogLive>
        {
            new() 
            {
                Id = -1,
                Tag = "全局"
            }
        };

        [ObservableProperty]
        private Service.Proto.Response.LogStreamResponse.Types.LogLive? selectedTag;
        // TODO: 日志获取，清除已另置



        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @conve_CreateStream(CancellationToken cancellationToken)
        {
            if (App.RpcManager is null)
            {
                // TODO: THROW EXCEPTION
                return;
            }
            int startIndex = _mainWindowViewModel.LogsCache?.Count ?? 1 - 1;

            if (startIndex < 0) { startIndex = 0; }

            disposableStream = await App.RpcManager.LogStream(KnownLogIndexMapping, StreamReader, cancellationToken);
        }

        private void StreamReader(Service.Proto.Response.LogStreamResponse resp)
        {
            switch (resp.State)
            {
                case Service.Proto.Response.LogStreamResponse.Types.LogStreamResponseState.UpdateLinks:
                    {
                        if (resp.Data.TryUnpack<Service.Proto.Response.LogStreamResponse.Types.LogsLiveData>(out var lv) && lv.Lives.Count > 0)
                        {
                            foreach (var live in lv.Lives)
                            {
                                TagSelector.Add(live);
                            }
                        }
                    }; break;
                case Service.Proto.Response.LogStreamResponse.Types.LogStreamResponseState.UpdateLogs:
                    {
                        if (resp.Data.TryUnpack<Service.Proto.Response.LogStreamResponse.Types.LogsData>(out var logs))
                        {
                            if (!KnownLogIndexMapping.ContainsKey(logs.LogId))
                            {
                                KnownLogIndexMapping.Add(logs.LogId, logs.Logs.Count);
                            }
                            else KnownLogIndexMapping[logs.LogId] += logs.Logs.Count;

                            foreach (var log in logs.Logs)
                            {
                                Logs?.Add(log);
                            }
                        }
                        else if (resp.Data.TryUnpack<Service.Proto.Response.LogStreamResponse.Types.LogContainer>(out var cot))
                        {
                            if (!KnownLogIndexMapping.ContainsKey(cot.LogId))
                            {
                                KnownLogIndexMapping.Add(cot.LogId, 1);
                            }
                            else KnownLogIndexMapping[cot.LogId] += 1;

                            Logs?.Add(cot);
                        }
                    }; break;
            }
        }

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is Page page)
            {
                if (page.FindName("flv") is System.Windows.Controls.ListView ve)
                {
                    this.listView = ve;

                    ve.SetBinding(System.Windows.Documents.TextElement.FontSizeProperty,new Binding
                    {
                        Source = App.Settings,
                        Path = new PropertyPath("LogFontSize"),
                        Mode = BindingMode.OneWay
                    });
                    if (string.IsNullOrEmpty(App.Settings.LogFontFamily) && App.Current.TryFindResource("SourceCodeProMixYahei") is System.Windows.Media.FontFamily ff)
                    {
                        ve.FontFamily = ff;
                    }
                    else
                    {
                        ve.FontFamily = new System.Windows.Media.FontFamily(App.Settings.LogFontFamily);
                    }
                }
                page.Unloaded += delegate
                {
                    if (listView is not null)
                    {
                        listView.Items.Filter = null;

                        System.Windows.Data.BindingOperations.ClearBinding(listView, System.Windows.Controls.ListView.ItemsSourceProperty);
                    }

                    conve_CreateStreamCommand.Cancel();

                    disposableStream?.Dispose();
                };
            }
        }

        [RelayCommand]
        private void @event_LogFilterSeleted(System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.listView is null or { Items.CanFilter: false }) return;
            if (e.AddedItems is {Count: 1} lx && lx[0] is Service.Proto.Response.LogStreamResponse.Types.LogLive live)
            {
                
                if (live.Id >= 0)
                {
                    listView.Items.Filter = new Predicate<object>(x =>
                    {
                        if (x is Service.Proto.Response.LogStreamResponse.Types.LogContainer { LogId : var v2 })
                        {
                            return v2.Equals(live.Id);
                        }
                        return false;
                    });
                }
                else
                {
                    listView.Items.Filter = null;
                }
            }
        }

        [RelayCommand]
        private async Task @event_ClearLog()
        {
            if (App.RpcManager is null)
            {
                // TODO: THROW EXCEPTION
                return;
            }
            if (SelectedTag is null || SelectedTag.Id is -1)
            {
                var r = await App.RpcManager.ClearLog(-1);

                if (r.Flag)
                {
                    Logs?.Clear();
                    KnownLogIndexMapping?.Clear();
                }
                else
                {
                    // todo: notice
                }
            }
            else
            {
                var ti = SelectedTag.Id;
                var r = await App.RpcManager.ClearLog(ti);

                if (r.Flag)
                {
                    if (Logs != null)
                    {
                        for (int i = Logs.Count - 1; i >= 0; i--)
                        {
                            if (Logs[i].LogId == ti)
                            {
                                Logs.RemoveAt(i);
                            }
                        }
                    }
                    KnownLogIndexMapping.Remove(ti);
                }
                else
                {
                    // todo: notice
                }
            }
        }

        [RelayCommand]
        private async Task @event_SaveLog()
        {
            if (Logs is null || listView is null) return;

            var fDialog = new Microsoft.Win32.SaveFileDialog
            {
                OverwritePrompt = true,
                AddExtension = true,
                ValidateNames = true,
                Filter = "日志文件(*.log)|*.log",
            };
            if(SelectedTag != null)
            {
                fDialog.FileName = $"OFLauncher-{SelectedTag.Tag}.log";
            }
            if (fDialog.ShowDialog() is true)
            {
                using (var f = System.IO.File.Open(fDialog.FileName,System.IO.FileMode.OpenOrCreate))
                {
                    f.Position = 0;
                    f.SetLength(0);
                    {
                        string logf = $"OpenFrp Launcher : 可以在 {System.IO.Path.Combine(AppContext.BaseDirectory, "logs")} 文件夹下查看来自 Daemon 的日志\n\n";
#if NET
                        ReadOnlyMemory<byte> fs = Encoding.UTF8.GetBytes(logf);
                        await f.WriteAsync(fs,CancellationToken.None);
#elif NETFRAMEWORK
                        byte[] fs = Encoding.UTF8.GetBytes(logf);
                        await f.WriteAsync(fs, 0, fs.Length);
#endif
                    }
                    foreach (var v in listView.Items)
                    {
                        if (v is Service.Proto.Response.LogStreamResponse.Types.LogContainer vc)
                        {
#if NET
                            ReadOnlyMemory<byte> fs = Encoding.UTF8.GetBytes($"{vc.Date.ToDateTime():yyyy/MM/dd HH:mm:ss} [{vc.Level}] {vc.Tag} {LogLevelRegex.Replace(vc.Data,string.Empty)}\n");
                            await f.WriteAsync(fs,CancellationToken.None);
#elif NETFRAMEWORK
                            byte[] fs = Encoding.UTF8.GetBytes($"{vc.Date.ToDateTime():yyyy/MM/dd HH:mm:ss} [{vc.Level}] {vc.Tag} {LogLevelRegex.Replace(vc.Data,string.Empty)}\n");
                            await f.WriteAsync(fs,0,fs.Length);
#endif
                        }
                    }
                    await f.FlushAsync();

                    f.Close();
                }

                ShowAlertAction($"日志 \"{fDialog.SafeFileName}\" 保存成功!", $"已成功保存在: {fDialog.FileName}", InfoBarSeverity.Success);

                // log notice
            }
        }
       
        public ObservableCollection<Service.Proto.Response.LogStreamResponse.Types.LogContainer>? Logs
        {
            get => _mainWindowViewModel.LogsCache;
        }

        public Google.Protobuf.Collections.MapField<int, int> KnownLogIndexMapping
        {
            get => _mainWindowViewModel.KnownLogIndexMapping;
        }


    }
}
