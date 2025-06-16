using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.Model;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class HomeViewModel : ObservableObject
    {
        public HomeViewModel()
        {
            if (Application.Current.MainWindow is { DataContext: MainWindowViewModel mv })
            {
                _mainWindowViewModel = mv;
            }
        }

        private iNKORE.UI.WPF.Modern.Controls.Page? page;

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is Page page)
            {
                this.page = page;
                page.Unloaded += delegate
                {
                    event_RefreshAdSenseCommand.Cancel();
                };
                event_RefreshAdSenseCommand.Execute(null);
            }
        }

        private readonly MainWindowViewModel? _mainWindowViewModel;

        [ObservableProperty]
        private Model.AdSenseItem[] adSences = Array.Empty<Model.AdSenseItem>();

        public Model.UserInfo UserInfo
        {
            get
            {
                if (_mainWindowViewModel is not null)
                {
                    return _mainWindowViewModel.UserInfo;
                }
                return SettingsViewModel.__userInfo_Defualt;
            }
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshAdSense(CancellationToken cancellationToken)
        {
            await Task.Delay(1500,cancellationToken);

            var resp = await Service.Net.OpenFrpApi.GetLauncherAdSense(cancellationToken);

            if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { Length: > 0 })
            {
                var v = new AdSenseItem[resp.Data.Length];

                for (int i = 0; i < resp.Data.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    var ve = resp.Data[i];

                    if (!string.IsNullOrEmpty(ve.ImageUrl))
                    {
                        string hash = Service.Helpers.HashAlgorithmHelper.ComputeHashString(ve.ImageUrl!);

                        string pathF = System.IO.Path.Combine(System.IO.Path.GetTempPath(), hash);

                        _ = page?.Dispatcher.BeginInvoke(async (object c) =>
                        {
                            try
                            {
                                if (!System.IO.File.Exists(pathF))
                                {
                                    var resp = await Service.Net.HttpClient.DefualtInstance.GetStreamAsync(ve.ImageUrl!, cancellationToken);

                                    if (resp.StatusCode is System.Net.HttpStatusCode.OK && resp.Data is { Length: > 0 } st)
                                    {
                                        using var fs = System.IO.File.Open(pathF, FileMode.OpenOrCreate);

                                        st.Seek(0, SeekOrigin.Begin);

                                        await st.CopyToAsync(fs);
                                        await fs.FlushAsync(cancellationToken);

                                        fs.Close();
                                        st.Close();
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                                if (!Uri.TryCreate(pathF, UriKind.RelativeOrAbsolute, out var uri))
                                {
                                    return;
                                }
                                if (cancellationToken.IsCancellationRequested) return;
                                try
                                {
                                    var bm = new BitmapImage();

                                    bm.BeginInit();
                                    bm.UriSource = uri;
                                    bm.EndInit();
                                    bm.Freeze();

                                    v[(int)c].SetImageSource(bm);
                                }
                                catch
                                {

                                }
                            }
                            catch
                            {

                            }
                        }, priority: System.Windows.Threading.DispatcherPriority.Background, i);
                  
                    }
                    try
                    {
                        v[i] = new AdSenseItem(ve);
                    }
                    catch
                    {
                        break;
                    }
                }
                AdSences = v;
            }
            else
            {
                AdSences = new Model.AdSenseItem[1]
                {
                    new AdSenseItem
                    {
                        Description = "你的赞助是前进的第一动力，\n本项已从2023年开始成项，到2025年为开发的第四个版本，开发不易。\n欢迎赞助启动器作者 (越越)。",
                        Title = "东风袅袅泛崇光，香雾空蒙月转廊",
                        Url = "https://console.openfrp.net",
                        Company = "默认"
                    },
                };
            }

            //AdSences = new Model.AdSenseItem[]
            //{
            //    new AdSenseItem
            //    {
            //        Content = "hahahaapodwa154141414214214124124124oidawd",
            //        Title = "aduiawuiadhawiudha",
            //        Link = "https://console.openfrp.net"
            //    },
            //    new AdSenseItem
            //    {
            //        Content = "hahahaapodwaoidawdawdawdwdawdawdawd",
            //        Title = "aduiawuiadhawiudha",
            //        Link = "https://baidu.cn",
            //        ImageSource = @"E:\Desktop\Photo\5dc2031f4d77e71c84ee167c08e26c3ff7aeed0d.jpg"
            //    },
            //    new AdSenseItem
            //    {
            //        Content = "149aw4d98wa4d98aw4d98aw4d98aw4d9aw4d98waq4daw49daw489d4aw98d4aw9d4aw9d4aw9d489awd",
            //        Title = "中文中文中文中文中文中文中文中文",
            //        Link = "https://baidu2.cn",
            //        ImageSource = @"E:\Desktop\Photo\wallhaven_587f90e1-1583-42cc-af54-67fc365db17b.png"
            //    }
            //};
        }

    }
}
