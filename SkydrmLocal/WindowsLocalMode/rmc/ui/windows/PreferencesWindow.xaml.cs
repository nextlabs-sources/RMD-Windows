using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SkydrmLocal.rmc;

namespace SkydrmLocal
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        private SkydrmLocalApp App = (SkydrmLocalApp)SkydrmLocalApp.Current;
        public PreferencesWindow()
        {
            InitializeComponent();
            InitCheckStatus();
        }
        private void InitCheckStatus()
        {
            //App.Config.GetPreferences();
            if (App.User.AutoStartApp)
            {
                this.checkLogin.IsChecked = true;
            }
            else
            {
                this.checkLogin.IsChecked = false;
            }
        }
        //protected override void Click_close(object sender, RoutedEventArgs args)
        //{
        //    this.Close();
        //}

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            //"Start SkyDRM on Windows login"
            bool isChecked = (bool)this.checkLogin.IsChecked;
            App.User.AutoStartApp= isChecked;
            //"Leave a copy in local Folder"

            this.Close();
        }
 
        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
       
    }
}
