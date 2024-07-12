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
using OpenFrp.Service.Call;
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

        



        public string Platfrom => $"{FileDictionary.GetFrpPlatform().ToUpper()}    .NET Framework {GetDotNetVersion()}";

        private static string GetDotNetVersion()
        {
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        }
        
        [RelayCommand]
        private void @event_OpenFrpcFolder()
        {

            _ = FileDictionary.CreateFrpFolder();

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
                if (!App.VersionString.Equals(data.Launcher.Latest))
                {
#if DEBUG
                    if (MessageBox.Show("require to update launcher?","openfrpLauncher",MessageBoxButton.OKCancel) is MessageBoxResult.Cancel)
                    {
                        if (!App.FrpcVersionString.Equals(data.Latest))
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
                                    data.FrpcUpdateLog +
                                    (Environment.OSVersion.Version.Major is 10 ? $"\nUpdate: {App.FrpcVersionString} => {data.Latest}" : $"\nUpdate: {App.FrpcVersionString} => OpenFRP_0.54.0_835276e2_20240205。") +
                                    $"\n请注意: 若您在使用 FRPC 映射远程服务，请备用远程方式，否则请不要更新！"
                            }));
                        }
                    }
#endif
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new Model.UpdateInfo
                    {
                        Type = Model.UpdateInfoType.Launcher,
                        Log = data.Launcher.Message,
                        Title = data.Launcher.Title,
                        SoftWareVersionData = data,
                    }));
                }
                else if (!App.FrpcVersionString.Equals(data.Latest))
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
                            data.FrpcUpdateLog +
                            (Environment.OSVersion.Version.Major is 10 ? $"\nUpdate: {App.FrpcVersionString} => {data.Latest}" : $"\nUpdate: {App.FrpcVersionString} => OpenFRP_0.54.0_835276e2_20240205。") +
                            $"\n请注意: 若您在使用 FRPC 映射远程服务，请备用远程方式，否则请不要更新！"
                    }));
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(RouteMessage<MainViewModel>.Create(new Model.UpdateInfo
                    {
                        Type = UpdateInfoType.None
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


                    string v2 = FileDictionary.GetFrpPlatform();
                    if (v2 is "i386") v2 = "x86";

                    if (UpdateInfo.Type is Model.UpdateInfoType.FrpClient)
                    {
                        if (App.Current is { MainWindow: MainWindow wnda })
                        {
                            wnda.IsEnabled = false;
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
                            wnda.IsEnabled = true;
                            return;
                        }
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


                                resp = await AppNetwork.HttpRequest.Get($"{item.BaseUrl}/OpenFRP_0.54.0_835276e2_20240205/frpc_windows_{FileDictionary.GetFrpPlatform()}.zip", progress);
                            }
                            else
                            {


                                resp = await AppNetwork.HttpRequest.Get($"{item.BaseUrl}/{UpdateInfo.SoftWareVersionData.Latest}/frpc_windows_{FileDictionary.GetFrpPlatform()}.zip", progress);
                            }
                            if (resp.StatusCode is System.Net.HttpStatusCode.OK)
                            {
                                try
                                {

                                    if (File.Exists(FileDictionary.GetFrpFile()))
                                    {


                                        File.Delete(FileDictionary.GetFrpFile());
                                    }

                                    using (MemoryStream ms = new MemoryStream(resp.Data.ToArray()))
                                    using (ZipArchive ac = new ZipArchive(ms, ZipArchiveMode.Read, false))
                                    {

                                        ac.ExtractToDirectory(FileDictionary.CreateFrpFolder());
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

                                }
                                else
                                {
                                    try
                                    {
                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = Assembly.GetExecutingAssembly().Location,
                                            Arguments = "--finish",
                                            CreateNoWindow = true,
                                            UseShellExecute = false
                                        });
                                    }
                                    catch
                                    {

                                    }
                                }
                                App.Current.Shutdown();
                                Environment.Exit(0);
                                return;
                            }
                            else
                            {
                                ErrorMessage += "\n" + resp.Exception?.ToString() ?? resp.Message;
                            }
                        }
                    }
                    else if (UpdateInfo.Type is UpdateInfoType.Launcher && !string.IsNullOrEmpty(UpdateInfo.SoftWareVersionData.Launcher.Argument))
                    {
                        if (UpdateInfo.SoftWareVersionData.Launcher.Argument is null) return;

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
                            string vf;
#if NET481
                            vf = "dotNET481";
#elif NET462
                            vf = "dotNET462";
#endif

                            string ur = string.Format(UpdateInfo.SoftWareVersionData.Launcher.Argument, vf);

                            var resp = await AppNetwork.HttpRequest.Get(ur, progress);

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
                                try
                                {
                                    App.KillServiceProcess(true);
                                }
                                catch
                                {
                                    MessageBox.Show("无法他退出守护进程，请尝试以\"管理员权限\"运行本程序后重新尝试更新。", "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                                }


                                var va = FileDictionary.GetFrpPlatform();
                                var fe = FileDictionary.GetFrpFile();

                                if (Process.GetProcessesByName($"frpc_windows_{va}.exe") is { Length: > 0 } cka)
                                {
                                    foreach (var item in cka)
                                    {
                                        try
                                        {
                                            if (item.MainModule.FileName.Equals(fe))
                                            {
                                                item.Kill();
                                            }
                                        }
                                        catch { }
                                    }
                                }

                                App.Current.Shutdown();
                                Environment.Exit(0);
                            }
                        }
                    }
                }
            }
        }

        public bool UseProxy
        {
            get
            {
                return Properties.Settings.Default.UseProxy;
            }
            set
            {
                OpenFrp.Service.Net.HttpRequest.ProxyEditor(value);
                Properties.Settings.Default.UseProxy = value;
            }
        }

        private string? launcherDf { get; set; }
    }
}
