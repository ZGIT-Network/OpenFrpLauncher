using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class UpdateViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> log = new ObservableCollection<string>();

        [RelayCommand]
        private async Task @event_UpdateRequest()
        {
            var app = await OpenFrp.Service.Net.OpenFrp.GetSoftwareVersionInfo();

            Log.Add($"{app.Message},{app.StatusCode},{app.Data?.Latest}");

            if (app.Exception is null && app.StatusCode is System.Net.HttpStatusCode.OK)
            {
                if (!App.VersionString.Equals(app.Data?.Launcher.Latest))
                {
                    
                    return;
                }
                try
                {
                    var frpcVersion = await App.GetFrpcVersionAsync();
                    if (!frpcVersion.Equals(app.Data?.Latest))
                    {
                        string platform = RuntimeInformation.ProcessArchitecture switch
                        {
                            Architecture.X64 => "amd64",
                            Architecture.X86 => "386",
                            Architecture.Arm64 => "arm64",
                            _ => throw new NotSupportedException("本软件暂不支持 ARMv7 等其他平台。"),
                        };
                        foreach (var item in app.Data!.DownloadSources!)
                        {
                            var resp = await OpenFrp.Service.Net.HttpRequest.GetStream($"{item.BaseUrl}/{app.Data!.Latest}/frpc_windows_{platform}.zip");

                            if (resp.Exception is null && resp.StatusCode is System.Net.HttpStatusCode.OK &&
                                resp.Data is { Length: > 0} buffer)
                            {
                                //var file = Path.GetTempFileName();

                                //await Task.Run(() => File.WriteAllBytes(file, buffer));

                                ZipArchive zip = new ZipArchive(buffer,ZipArchiveMode.Read);

                                var path = Path.Combine(Path.GetTempPath(),$"{buffer.Length}_{app.Data!.Latest}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
                                var frpcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc");

                                Log.Add(path);

                                zip.ExtractToDirectory(path);

                                Directory.CreateDirectory(frpcPath);

                                File.Delete(Path.Combine(frpcPath, $"frpc_windows_{platform}.exe"));
                                File.Move(Path.Combine(path, $"frpc_windows_{platform}.exe"), Path.Combine(frpcPath, $"frpc_windows_{platform}.exe"));

                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = "RunAs",
                                    Arguments = $"/machine:{platform} /trustlevel:0x20000 {Assembly.GetExecutingAssembly().Location}",
                                    CreateNoWindow = true,
                                    UseShellExecute = false
                                });
                                Environment.Exit(0);
                            }
                            else Log.Add($"{resp.Exception}{resp.Message}{resp.StatusCode}");
                        }
                        Log.Add("下载失败 <_> ");
                        // frpc has update
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(ex.ToString());
                    // frpc version check failed
                }
            }
        }
    }
}
