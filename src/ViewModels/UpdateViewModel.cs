using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Awe.Model.OpenFrp.Response.Data;
using Awe.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenFrp.Service.Net;
using System.Net;
using System.Collections.ObjectModel;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class UpdateViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> log = new ObservableCollection<string>();

        [RelayCommand]
        private async Task event_UpdateRequest()
        {
            ApiResponse<SoftWareVersionData> app = await OpenFrp.Service.Net.OpenFrp.GetSoftwareVersionInfo();
            Log.Add($"{app.Message},{app.StatusCode},{app.Data?.Latest}");
            if (app.Exception != null || app.StatusCode != HttpStatusCode.OK || !App.VersionString.Equals(app.Data?.Launcher.Latest))
            {
                return;
            }
            try
            {
                if ((await App.GetFrpcVersionAsync()).Equals(app.Data?.Latest))
                {
                    return;
                }
                string platform = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "amd64",
                    Architecture.X86 => "386",
                    Architecture.Arm64 => "arm64",
                    _ => throw new NotSupportedException("本软件暂不支持 ARMv7 等其他平台。"),
                };
                SoftWareVersionData.DownloadSource[] downloadSources = app.Data!.DownloadSources!;
                foreach (SoftWareVersionData.DownloadSource item in downloadSources)
                {
                    ApiResponse<Stream> resp = await HttpRequest.GetStream(item.BaseUrl + "/" + app.Data!.Latest + "/frpc_windows_" + platform + ".zip");
                    if (resp.Exception == null && resp.StatusCode == HttpStatusCode.OK &&
                        resp.Data is { } buffer)
                    {
                        if (buffer != null && buffer.Length > 0)
                        {
                            ZipArchive source = new ZipArchive(buffer, ZipArchiveMode.Read);
                            string path = Path.Combine(Path.GetTempPath(), $"{buffer.Length}_{app.Data!.Latest}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
                            string frpcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc");
                            Log.Add(path);
                            source.ExtractToDirectory(path);
                            Directory.CreateDirectory(frpcPath);
                            File.Delete(Path.Combine(frpcPath, $"frpc_windows_{platform}.exe"));
                            File.Move(Path.Combine(path, $"frpc_windows_{platform}.exe"), Path.Combine(frpcPath, "frpc_windows_" + platform + ".exe"));
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "RunAs",
                                Arguments = $"/machine:{platform} /trustlevel:0x20000 {Assembly.GetExecutingAssembly().Location}",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            Environment.Exit(0);
                            continue;
                        }
                    }
                    Log.Add($"{resp.Exception}{resp.Message}{resp.StatusCode}");
                }
                Log.Add("下载失败 <_> ");
            }
            catch (Exception ex)
            {
                Log.Add(ex.ToString());
            }
        }
    }
}
