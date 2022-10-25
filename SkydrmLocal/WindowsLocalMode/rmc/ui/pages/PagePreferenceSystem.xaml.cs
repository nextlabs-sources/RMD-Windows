using SkydrmLocal.rmc.featureProvider;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkydrmLocal.rmc.ui.pages
{
    /// <summary>
    /// Interaction logic for PagePreferenceSystem.xaml
    /// </summary>
    public partial class PagePreferenceSystem : Page
    {
        private Window parentWindow;
        public Window ParentWindow
        {
            get { return parentWindow; }
            set { parentWindow = value; }
        }


        public PagePreferenceSystem()
        {
            InitializeComponent();
            InitCheckStatus();
        }

        private void InitCheckStatus()
        {
            //SkydrmLocalApp.Singleton.Config.GetPreferences();
            //if (SkydrmLocalApp.Singleton.Config.StartLoginPre == "true")
            //{
            //    this.checkLogin.IsChecked = true;
            //}
            //else
            //{
            //    this.checkLogin.IsChecked = false;
            //}
            this.checkLogin.IsChecked = SkydrmLocalApp.Singleton.User.AutoStartApp;

            //"Leave a copy in local Folder"
            this.checkCopyFolder.IsChecked = SkydrmLocalApp.Singleton.User.LeaveCopy;

            //
            //for (int i = 0; i < this.comboxUpload.Items.Count; i++)
            //{
            //    if (this.comboxUpload.Items[i].ToString() == SkydrmLocalApp.Singleton.User.UploadFilePolicy.ToString())
            //    {
            //        this.comboxUpload.SelectedItem = this.comboxUpload.Items[i];
            //    }
            //}
            
        }

        private void SaveSystemPre()
        {
            //data to save database or register
            //"Start SkyDRM on Windows login"
            bool isLoginChecked = (bool)this.checkLogin.IsChecked;

            SkydrmLocalApp.Singleton.User.AutoStartApp = isLoginChecked;

            //"Leave a copy in local Folder"
            SkydrmLocalApp.Singleton.User.LeaveCopy = (bool)this.checkCopyFolder.IsChecked;

            //SkydrmLocalApp.Singleton.User.UploadFilePolicy = this.comboxUpload.SelectedItem;

            //SkydrmLocalApp.Singleton.ShowBalloonTip("Save succcessfully !");
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveSystemPre();

            if (parentWindow != null)
            {
                parentWindow.Close();
            }

        }

        private void Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveSystemPre();
            this.ApplyBtn.IsEnabled = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Name != null)
            {
                this.ApplyBtn.IsEnabled = true;
            }
        }

    }
}
