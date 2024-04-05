using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppNetwork = OpenFrp.Service.Net;

namespace OpenFrp.Launcher.Dialog
{
    /// <summary>
    /// EditTunnelDialog.xaml 的交互逻辑
    /// </summary>
    [INotifyPropertyChanged]
    public partial class EditTunnelDialog : ModernWpf.Controls.ContentDialog
    {
        public EditTunnelDialog()
        {
            InitializeComponent();
        }

        [ObservableProperty]
        private Awe.Model.ApiResponse? response;

        public bool IsFinished { get; private set; }

        public void ResetData()
        {
            Editor.Tunnel.Name = default;
            if (Editor.Template.FindName("TunnelNameInput", Editor) is FrameworkElement fe)
            {
                fe.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            }
            
        }

        [RelayCommand]
        private async Task @event_UploadData()
        {
            if (Editor.IsPortImportOpen)
            {
                Editor.IsPortImportOpen = false;
            }
            else
            {
                if (GetValue(Controls.TunnelEditor.IsCreateModeProperty) is true)
                {
                    Response = await AppNetwork.OpenFrp.CreateUserTunnel(Editor.GetCreateConfig());
                    if (Response.StatusCode is System.Net.HttpStatusCode.OK && "创建成功".Equals(Response.Message))
                    {
                        IsFinished = true;

                        Hide();
                    }
                }
                else
                {
                    Response = await AppNetwork.OpenFrp.EditUserTunnel(Editor.GetEditConfig());
                    if (Response.StatusCode is System.Net.HttpStatusCode.OK && "保存成功".Equals(Response.Message))
                    {
                        IsFinished = true;

                        Hide();
                    }
                }
            }
        }

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("CommandSpace") is Border br)
            {
                br.Margin = new Thickness(24, 24, 24, 24);
            }
            if (GetTemplateChild("BackgroundElement") is Border br2)
            {
                br2.Margin = new Thickness(0, 24, 0, 24);
            }



            PrimaryButtonClick += async (sender, e) => {e.Cancel = true; await event_UploadDataCommand.ExecuteAsync(null); };

            SetBinding(IsPrimaryButtonEnabledProperty, new Binding
            {
                Source = event_UploadDataCommand,
                Path = new PropertyPath("IsRunning"),
                Converter = new Awe.UI.Converter.RollbackConverter(),
                Mode = BindingMode.OneWay
            });

            
            base.OnApplyTemplate();
        }

        public async Task<bool> WaitForFinishAsync()
        {
            await base.ShowAsync();

            return IsFinished;
        }
    }
}
