using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
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
                App.ConfigureVersionCheck(App.FrpcVersionString).ContinueWith(async task =>
                {
                    if (task.Result is { } resp)
                    {
                        await Task.Delay(550);

                        this.ErrorMessage = resp.Exception?.ToString();
                    }
                    else
                    {
                        App.Current?.Dispatcher?.Invoke(() =>
                        {
                            _ = @event_EnterUpdate();
                        });
                    }
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

        public string Platfrom => Service.Commands.FileDictionary.GetFrpPlatform().ToUpper();

        [RelayCommand]
        private void @event_OpenFrpcFolder()
        {
            _ = Service.Commands.FileDictionary.CreateFrpFolder();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc"),
                    UseShellExecute = true,
                });
                return;
            }
            catch
            {

            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer",
                    Arguments = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc"),
                });
                return;
            }
            catch
            {

            }
        }

        [RelayCommand]
        private async Task @event_CheckUpdate()
        {
            this.ErrorMessage = default;

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
                    if (Environment.OSVersion.Version.Major is not 10 && App.FrpcVersionString.Equals("OpenFRP_0.54.0_835276e2_20240205"))
                    {
                        return;
                    }
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new Model.UpdateInfo
                    {
                        Type = Model.UpdateInfoType.FrpClient,
                        SoftWareVersionData = versionData.Data,
                        Title = "FRPC 更新",
                        Log =
                            (Environment.OSVersion.Version.Major is 10 ? "" : "Windows 7 已不受支持，将升级到 OpenFRP_0.54.0_835276e2_20240205。") +
                            versionData.Data?.FrpcUpdateLog +
                            (Environment.OSVersion.Version.Major is 10 ? $"\nUpdate: {App.FrpcVersionString} => {versionData.Data?.Latest}" : $"\nUpdate: {App.FrpcVersionString} => OpenFRP_0.54.0_835276e2_20240205。") +
                            $"\n请注意: 若您在使用 FRPC 映射远程服务，请备用远程方式，否则请不要更新！"
                    }));
                }
            }
            else
            {
                await Task.Delay(550);

                this.ErrorMessage = versionData.Exception?.ToString();
            }

            //OnPropertyChanged(nameof(UpdateInfo));
        }

        [ObservableProperty]
        private double progressValue;

        [ObservableProperty]
        private string? errorMessage;

        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        [RelayCommand]
        private async Task @event_EnterUpdate()
        {
            if (App.Current is { MainWindow: Window wnd })
            {
                if (UpdateInfo.SoftWareVersionData?.DownloadSources is { })
                {
                    string v2 = Service.Commands.FileDictionary.GetFrpPlatform();
                    if (v2 is "i386") v2 = "x86";

                    if (UpdateInfo.Type is Model.UpdateInfoType.FrpClient)
                    {
                        if (!IsAdministrator())
                        {
                            bool vl = true;
                            if (App.Current is { MainWindow: UpdateWindow })
                            {
                                if (MessageBox.Show("您是否已关闭 UAC 选项？\n若没有，本应用会再次请求 UAC。\n如果您调到最低级别，请点击继续来安装更新", "OpenFrp Launcher", MessageBoxButton.OKCancel, MessageBoxImage.Warning) is MessageBoxResult.Cancel)
                                {
                                    return;
                                }
                                vl = false;
                            }
                            if (vl)
                            {
                                wnd.IsEnabled = false;
                                try
                                {
                                    if (Process.Start(new ProcessStartInfo
                                    {
                                        FileName = Assembly.GetExecutingAssembly().Location,
                                        Arguments = "--update",
                                        Verb = "runas",
                                        UseShellExecute = true
                                    }) is Process)
                                    {
                                        App.ServiceProcess.Kill();
                                        App.Current?.Shutdown();
                                        Environment.Exit(0);
                                        return;
                                    }
                                }
                                catch
                                {
                                }
                                wnd.IsEnabled = true;
                                return;
                            }
                        }
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

                        foreach (var item in UpdateInfo.SoftWareVersionData.DownloadSources)
                        {
                            // win 7 version fall back
                            Awe.Model.ApiResponse<IEnumerable<byte>> resp;
                            if (Environment.OSVersion.Version.Major is not 10)
                            {
                                // 纯属擦屁股
                                resp = await AppNetwork.HttpRequest.Get($"{item.BaseUrl}/OpenFRP_0.54.0_835276e2_20240205/frpc_windows_{Service.Commands.FileDictionary.GetFrpPlatform()}.zip", progress);
                            }
                            else
                            {
                                resp = await AppNetwork.HttpRequest.Get($"{item.BaseUrl}/{UpdateInfo.SoftWareVersionData.Latest}/frpc_windows_{Service.Commands.FileDictionary.GetFrpPlatform()}.zip", progress);
                            }
                            if (resp.StatusCode is System.Net.HttpStatusCode.OK)
                            {
                                try
                                {
                                    if (File.Exists(Service.Commands.FileDictionary.GetFrpFile()))
                                    {
                                        File.Delete(Service.Commands.FileDictionary.GetFrpFile());
                                    }

                                    using (MemoryStream ms = new MemoryStream(resp.Data.ToArray()))
                                    using (ZipArchive ac = new ZipArchive(ms, ZipArchiveMode.Read, false))
                                    {
                                        ac.ExtractToDirectory(Service.Commands.FileDictionary.CreateFrpFolder());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ErrorMessage += "\n" + ex.ToString();
                                    return;
                                }



                                

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
                                    OpenFrp.Launcher.Properties.Settings.Default.Save();

                                    wnd.Close();

                                    App.ServiceProcess.Kill();

                                    App.Current.Shutdown();
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
                    else if (UpdateInfo.Type is UpdateInfoType.Launcher && !string.IsNullOrEmpty(UpdateInfo.SoftWareVersionData.Launcher.Download))
                    {
                        if (UpdateInfo.SoftWareVersionData.Launcher.Download is null) return;

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

                        if (string.IsNullOrEmpty(launcherDf))
                        {
                            var resp = await AppNetwork.HttpRequest.Get(UpdateInfo.SoftWareVersionData.Launcher.Download, progress);

                            if (resp.StatusCode is System.Net.HttpStatusCode.OK)
                            {
                                using FileStream fs = new FileStream(launcherDf = $"{Path.GetTempFileName()}.exe", FileMode.OpenOrCreate);
                                await fs.WriteAsync(resp.Data.ToArray(), 0, resp.Data.Count());

                                await fs.FlushAsync();

                                fs.Close();

                                await @event_EnterUpdate();

                                return;
                            }
                        }
                        else
                        {
                            var respa = await Task.Run(() =>
                            {
                                try
                                {
                                    return Process.Start(new ProcessStartInfo
                                    {
                                        FileName = launcherDf,
                                        Verb = "runas",
                                        UseShellExecute = true
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
                                OpenFrp.Launcher.Properties.Settings.Default.Save();

                                wnd.Close();

                                App.ServiceProcess.Kill();
                                App.Current.Shutdown();
                                Environment.Exit(0);
                            }
                        }
                    }
                }
            }
        }

        private string? launcherDf { get; set; }
    }
}
