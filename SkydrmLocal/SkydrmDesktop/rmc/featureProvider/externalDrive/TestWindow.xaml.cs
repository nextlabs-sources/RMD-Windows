using Microsoft.Identity.Client;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    /// <summary>
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {
        NxOneDrive nxOneDrive;
        public TestWindow()
        {
            InitializeComponent();
            nxOneDrive = new NxOneDrive();
        }

        private void DisplayBasicTokenInfo(AuthenticationResult authResult)
        {
            TokenInfoText.Text = "";
            if (authResult != null)
            {
                TokenInfoText.Text += $"Username: {authResult.Account.Username}" + Environment.NewLine;
                TokenInfoText.Text += $"Token Expires: {authResult.ExpiresOn.ToLocalTime()}" + Environment.NewLine;
                TokenInfoText.Text += $"AccessToken : {authResult.AccessToken}" + Environment.NewLine;
                TokenInfoText.Text += $"IdToken : {authResult.IdToken}" + Environment.NewLine;
                TokenInfoText.Text += $"Scopes : {authResult.Scopes.ToString()}" + Environment.NewLine;
                TokenInfoText.Text += $"TenantId : {authResult.TenantId}" + Environment.NewLine;
                TokenInfoText.Text += $"UniqueId : {authResult.UniqueId}" + Environment.NewLine;
                TokenInfoText.Text += $"CorrelationId : {authResult.CorrelationId}" + Environment.NewLine;
            }
        }

        private async void Authentication_Click(object sender, RoutedEventArgs e)
        {
            string var = await nxOneDrive.Login(this);
            ResultText.Text = JsonHelper.FormatJson(var);
            DisplayBasicTokenInfo(nxOneDrive.AuthenticationResult);
        }

        private async void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            await nxOneDrive.SignOut();
        }

        private async void Drive_Info_Click(object sender, RoutedEventArgs e)
        {
            string var = await nxOneDrive.Information();
            ResultText.Text = JsonHelper.FormatJson(var);
        }

        private async void List_Directory_Click(object sender, RoutedEventArgs e)
        {
            string var = await nxOneDrive.ListChildren("root");
            ResultText.Text = JsonHelper.FormatJson(var);
        }

        private async void Delete_File_Click(object sender, RoutedEventArgs e)
        {
            string var = await nxOneDrive.Delete("C9FA40E83525749E!128");
            ResultText.Text = JsonHelper.FormatJson(var);
        }

        private async void Upload_File_Click(object sender, RoutedEventArgs e)
        {
            string filePath = string.Empty;
            var dlg = new OpenFileDialog();
            {
                dlg.Filter = "All Files|*.*";
                if (dlg.ShowDialog(this) == true)
                {
                    filePath = dlg.FileName;
                }
            }
            string var = await nxOneDrive.Upload("C9FA40E83525749E!104", filePath);
            ResultText.Text = JsonHelper.FormatJson(var);
        }

        private async void Upload_Larg_File_Click(object sender, RoutedEventArgs e)
        {
            string filePath = string.Empty;
            var dlg = new OpenFileDialog();
            {
                dlg.Filter = "All Files|*.*";
                if (dlg.ShowDialog(this) == true)
                {
                    filePath = dlg.FileName;
                }
            }
            await nxOneDrive.UploadLargFile("C9FA40E83525749E!129", filePath);
        }

        private async void Download_File_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                     await nxOneDrive.DownloadFile(fbd.SelectedPath, "C9FA40E83525749E!123");
                }
            }
        }

        private async void Create_folder_Click(object sender, RoutedEventArgs e)
        {
            string json = @"{
                          'name': 'Test Create Folder',
                          'folder': { },
                          '@microsoft.graph.conflictBehavior': 'rename'
                         }";
            
            string var = await nxOneDrive.CreateFolder("C9FA40E83525749E!104", json);
            ResultText.Text = JsonHelper.FormatJson(var);
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Download_Larg_File_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    await nxOneDrive.DownloadLargFile(fbd.SelectedPath, "C9FA40E83525749E!123");
                }
            }
        }
    }
}
