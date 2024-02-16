using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenFrp.Launcher.Model;
using Windows.Foundation.Metadata;
using AppNetwork = OpenFrp.Service.Net;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class UpdateCommentViewModel : ObservableObject
    {
        public UpdateCommentViewModel()
        {
            if (App.Current is { MainWindow.DataContext: MainViewModel mv })
            {
                mv.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(UpdateInfo))
                    {
                        OnPropertyChanged(nameof(UpdateInfo));
                    }
                };
                
            }
            if (App.Current is { MainWindow: UpdateWindow })
            {
                App.ConfigureVersionCheck(App.FrpcVersionString).ContinueWith(delegate
                {
                    App.Current?.Dispatcher?.Invoke(() =>
                    {
                        _ = @event_EnterUpdate();
                    });
                });
            }
            
        }

        public Model.UpdateInfo UpdateInfo
        {
            get
            {
                if (App.Current is { MainWindow.DataContext: MainViewModel mv })
                {
                    return mv.UpdateInfo;
                }
                global::System.Diagnostics.Debugger.Break();
                throw new NullReferenceException();
            }
        }

        [RelayCommand]
        private async Task @event_CheckUpdate()
        {
            var versionData = await OpenFrp.Service.Net.OpenFrp.GetSoftwareVersionInfo();

            if (versionData.StatusCode is System.Net.HttpStatusCode.OK &&
                versionData.Data is { } data)
            {
                if (!App.VersionString.Equals(versionData.Data?.Launcher.Latest))
                {
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new Model.UpdateInfo
                    {
                        Type = Model.UpdateInfoType.Launcher,
                        Log = versionData.Data?.Launcher.Message,
                        Title = versionData.Data?.Launcher.Title,
                        SoftWareVersionData = versionData.Data,
                    }));
                }
                if (!App.FrpcVersionString.Equals(versionData.Data?.Latest))
                {
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new Model.UpdateInfo
                    {
                        Type = Model.UpdateInfoType.FrpClient,
                        SoftWareVersionData = versionData.Data,
                        Title = "FRPC 更新",
                        Log = versionData.Data?.FrpcUpdateLog +
                            $"\nUpdate: {App.FrpcVersionString} => {versionData.Data?.Latest}" +
                            $"\n请注意: 若您在使用 FRPC 映射远程服务，请备用远程方式，否则请不要更新！"
                    }));
                }
            }

            //OnPropertyChanged(nameof(UpdateInfo));
        }

        [ObservableProperty]
        private double progressValue;

        [ObservableProperty]
        private string? errorMessage;

        [RelayCommand]
        private async Task @event_EnterUpdate()
        {
            if (App.Current is { MainWindow: MainWindow wnd })
            {
                wnd.IsEnabled = false;
                var resp = await Task.Run(() =>
                {
                    try
                    {
                        return Process.Start(new ProcessStartInfo
                        {
                            FileName = Assembly.GetExecutingAssembly().Location,
                            Arguments = "--update",
                            Verb = "runas",
                            UseShellExecute = true
                        }) is Process;
                    }
                    catch
                    {
                        return false;
                    }
                });
                if (resp)
                {
                    App.Current?.Shutdown();
                    Environment.Exit(0);
                    return;
                }
                wnd.IsEnabled = true;
            }
            else if (UpdateInfo.SoftWareVersionData?.DownloadSources is { })
            {
                ErrorMessage = default;
                ProgressValue = 0;
                IProgress<AppNetwork.HttpDownloadProgress> progress = new Progress<AppNetwork.HttpDownloadProgress>((x) =>
                {
                    if (x.ReadLength is 0)
                    {
                        return;
                    }
                    ProgressValue = 101 - (x.TotalLength / x.ReadLength);
                });
                
                if (UpdateInfo.Type is Model.UpdateInfoType.FrpClient)
                {
                    foreach (var item in UpdateInfo.SoftWareVersionData.DownloadSources)
                    {
                        var resp = await AppNetwork.HttpRequest.Get($"{item.BaseUrl}/{UpdateInfo.SoftWareVersionData.Latest}/frpc_windows_{Service.Commands.FileDictionary.GetFrpPlatform()}.zip", progress);
                        if (resp.StatusCode is System.Net.HttpStatusCode.OK)
                        {
                            try
                            {
                                if (File.Exists(Service.Commands.FileDictionary.GetFrpFile()))
                                {
                                    File.Delete(Service.Commands.FileDictionary.GetFrpFile());
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorMessage += "\n" + ex.ToString();
                                return;
                            }

                            using (MemoryStream ms = new MemoryStream(resp.Data.ToArray()))
                            using (ZipArchive ac = new ZipArchive(ms, ZipArchiveMode.Read, false))
                            {
                                ac.ExtractToDirectory(Service.Commands.FileDictionary.CreateFrpFolder());
                            }

                            string v2 = Service.Commands.FileDictionary.GetFrpPlatform();
                            if (v2 is "i386") v2 = "x86";
                            var respa = await Task.Run(() =>
                            {
                                try
                                {
                                    return Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "RunAs",
                                        Arguments = "/machine:" + v2 + " /trustlevel:0x20000 \"" + Assembly.GetExecutingAssembly().Location + " --finish\"",
                                        CreateNoWindow = true,
                                        UseShellExecute = false
                                    }) is Process;
                                }
                                catch (Exception ex)
                                {
                                    ErrorMessage += "\n" + ex.ToString();
                                    return false;
                                }
                            });
                            if (respa)
                            {
                                Environment.Exit(0);
                            }
                            return;
                        }
                        else
                        {
                            ErrorMessage += "\n" + resp.Exception?.ToString() ?? resp.Message;
                        }
                    }
                    
                    
                }
            }
        }
    }
}
