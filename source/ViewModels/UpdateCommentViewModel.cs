using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using OpenFrp.Launcher.Model;

namespace OpenFrp.Launcher.ViewModels
{
    internal partial class UpdateCommentViewModel : ObservableObject,IHrViewModel
    {
        public UpdateCommentViewModel()
        {
            if (Application.Current.MainWindow is { DataContext: MainWindowViewModel mv })
            {
                this.mv = mv;

                mv.PropertyChanged += (_, e) =>
                {
                    OnPropertyChanged(e.PropertyName);
                };

                return;
            }
            throw new NullReferenceException(nameof(App.Current.MainWindow.DataContext));
        }

        internal const string LoadingState = "DisplayLoadingCtrl";
        internal const string ErrorState = "DisplayErrorCtrl";
        internal const string EmptyState = "DisplayEmptyCtrl";
        internal const string NormalState = "DisplayContainerCtrl";

        private FrameworkElement? container;

        [ObservableProperty,NotifyCanExecuteChangedFor(nameof(event_InstallUpdateCommand))]
        private UpdateType updateType = UpdateType.None;

        private readonly MainWindowViewModel mv;

        [RelayCommand]
        private async Task @conve_CheckUpdate()
        {
            UpdateType = UpdateType.None;
            VisualStateManager.GoToElementState(container, LoadingState, false);
            this.ClearExecuteResult();

            await Task.Delay(1500);

            var resp = await OpenFrp.Service.Net.OpenFrpApi.GetSoftwareConfig();

            if (!this.UpdateState(resp))
            {
                VisualStateManager.GoToElementState(container, ErrorState, false);
                // error
            }
            else if (resp.Data is { DownloadSources.Length: > 0 } software)
            {
                mv.SoftwareConfig = new SoftwareConfig(software);

                if (software.Launcher.Latest != App.LauncherVersionString)
                {
                    if (software.Launcher is { Message: not null, Title: not null })
                    {
                        Title = software.Launcher.Title;
                        Message = software.Launcher.Message;


                        mv.HasUpdate = true;
                        UpdateType = UpdateType.Launcher;
                        VisualStateManager.GoToElementState(container, NormalState, false);
                        return;
                    }
                }
                else if (App.FrpcVersionString != "Unknown")
                {
                    if (software.Latest != App.FrpcVersionString)
                    {
                        if (OSVersionHelper.IsWindows7OrGreater && !OSVersionHelper.IsWindows10OrGreater)
                        {
                            if (App.FrpcVersionString.Equals("OpenFRP_0.54.0_835276e2_20240205"))
                            {
                                VisualStateManager.GoToElementState(container, EmptyState, false);
                                mv.HasUpdate = false;
                                return;
                            }
                        }
                        Title = "FRPC 更新";
                        Message =
                                (OSVersionHelper.IsWindows10OrGreater ? "" : "Windows 10 以下版本 已不受支持，将升级到 OpenFRP_0.54.0_835276e2_20240205。") +
                                software.FrpcUpdateLog +
                                (OSVersionHelper.IsWindows10OrGreater ? $"\nUpdate: {App.FrpcVersionString} => {software.Latest}" : $"\nUpdate: {App.FrpcVersionString} => OpenFRP_0.54.0_835276e2_20240205。") +
                                $"\n请注意: 若您在使用 FRPC 映射远程服务，请备用远程方式，否则请不要更新！";
                        mv.HasUpdate = true;
                        UpdateType = UpdateType.Frpc;
                        VisualStateManager.GoToElementState(container, NormalState, false);
                        return;
                    }
                }
                VisualStateManager.GoToElementState(container, EmptyState, false);
                mv.HasUpdate = false;
            }
        }

        [RelayCommand]
        private async Task @event_DisplayException()
        {
            if (ExecuteResult is { HasException: true, Exception: not null and Exception ex })
            {
                if (App.Current is { MainWindow: var mw } && ContentDialog.GetOpenDialog(mw) is ContentDialog da)
                {
                    da?.Hide();
                }
                var dialog = new Dialogs.ErrorContentDialog
                {

                };
                dialog.SetValue(Controls.ErrorViewer.ExceptionProperty, ex);
                await dialog.ShowAsync();
            }
        }

        [RelayCommand]
        private void @event_OpenFrpcFolder()
        {
            _ = System.IO.Directory.CreateDirectory(Service.Helpers.FileHelper.FrpcDirectory);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Service.Helpers.FileHelper.FrpcDirectory,
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
                    Arguments = Service.Helpers.FileHelper.FrpcDirectory,
                });
                return;
            }
            catch
            {

            }
        }



        [RelayCommand]
        private void @event_PageLoaded(RoutedEventArgs e)
        {
            if (e.Source is iNKORE.UI.WPF.Modern.Controls.Page page)
            {
                container = page;

                if (mv.HasUpdate && SoftwareConfig is { SoftwareConfigValue: not null, SoftwareConfigValue: var software })
                {
                    if (software.Launcher.Latest != App.LauncherVersionString)
                    {
                        if (software.Launcher is { Message: not null, Title: not null })
                        {
                            Title = software.Launcher.Title;
                            Message = software.Launcher.Message;

                            UpdateType = UpdateType.Launcher;
                            //mv.HasUpdate = true;
                        }
                    }
                    else if (App.FrpcVersionString != "Unknown")
                    {
                        if (software.Latest != App.LauncherVersionString)
                        {
                            if (!OSVersionHelper.IsWindows8OrGreater)
                            {
                                if (App.LauncherVersionString.Equals("OpenFRP_0.54.0_835276e2_20240205"))
                                {
                                    return;
                                }
                            }
                            Title = "FRPC 更新";
                            Message =
                                    (OSVersionHelper.IsWindows8OrGreater ? "" : "Windows 7 已不受支持，将升级到 OpenFRP_0.54.0_835276e2_20240205。") +
                                    software.FrpcUpdateLog +
                                    (OSVersionHelper.IsWindows8OrGreater ? $"\nUpdate: {App.FrpcVersionString} => {software.Latest}" : $"\nUpdate: {App.FrpcVersionString} => OpenFRP_0.54.0_835276e2_20240205。") +
                                    $"\n请注意: 若您在使用 FRPC 映射远程服务，请备用远程方式，否则请不要更新！";

                            UpdateType = UpdateType.Frpc;
                        }
                    }
                    VisualStateManager.GoToElementState(container, NormalState, false);
                }
                else if (SoftwareConfig is null)
                {
                    conve_CheckUpdateCommand.Execute(default);
                }
                else
                {
                    UpdateType = UpdateType.None;
                    VisualStateManager.GoToElementState(container, EmptyState, false);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteInstallUpdate))]
        private void @event_InstallUpdate()
        {
            conve_InstallUpdateCommand.Execute(UpdateType); 
            //mv.conve_InstallUpdateCommand.exe
            //switch (UpdateType)
            //{
            //    case UpdateType.Frpc:
            //        {
            //            string targetVersion = software.Latest!;
            //            if (!OSVersionHelper.IsWindows10OrGreater)
            //            {
            //                targetVersion = "OpenFRP_0.54.0_835276e2_20240205";
            //            }
            //            foreach (var source in software.DownloadSources)
            //            {
            //                string url = $"{source.BaseUrl}/{targetVersion}/frpc_windows_{Service.Helpers.FileHelper.UserPlatform}.zip";

            //                //Service.Net.HttpClient.DefualtInstance.GetAsync(url);
            //            }

            //        }
            //        ;break;
            //}
        }

        private bool CanExecuteInstallUpdate() => UpdateType is UpdateType.Frpc or UpdateType.Launcher && SoftwareConfig is not null;

        public bool BypassProxy
        {
            get => !App.Settings.UseProxy;
            set
            {
                App.Settings.UseProxy = !value;
                OnPropertyChanged(nameof(BypassProxy));
            }
        }

        public bool HasUpdate { get => mv.HasUpdate;  }
        public Model.SoftwareConfig? SoftwareConfig { get => mv.SoftwareConfig; }

        public IRelayCommand conve_InstallUpdateCommand { get => mv.conve_InstallUpdateCommand; }

        public bool IsFailed { get => ExecuteResult is not null; }

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string message = string.Empty;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(IsFailed))]
        private Model.ExecuteResult? executeResult;
    }
}
