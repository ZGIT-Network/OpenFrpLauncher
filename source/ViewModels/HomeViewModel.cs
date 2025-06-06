using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }

        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is Page page)
            {
                //event_RefreshAdSenseCommand.Execute(null);
            }
        }

        [ObservableProperty]
        private Model.AdSenseItem[] adSences = Array.Empty<Model.AdSenseItem>();

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task @event_RefreshAdSense(CancellationToken cancellationToken)
        {
            await Task.Delay(1500);

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
