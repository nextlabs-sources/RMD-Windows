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
using SkydrmLocal.rmc.ui.pages;

namespace SkydrmLocal
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow2 : Window
    {
        private SkydrmLocalApp App = (SkydrmLocalApp)SkydrmLocalApp.Current;

        private SolidColorBrush colorGray = new SolidColorBrush(Color.FromRgb(211, 206, 206));
        private SolidColorBrush colorTransparent = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        public PreferencesWindow2()
        {
            InitializeComponent();

            InitializeContentPages();
        }

        private void InitializeContentPages()
        {
            PagePreferenceSystem pagePreferenceSystem = new PagePreferenceSystem()
            {
                ParentWindow = this
            };
            this.SelectSystem.Content = pagePreferenceSystem;

            PagePreferenceDocument pageSelectDocument = new PagePreferenceDocument()
            {
                ParentWindow = this
            };
            this.SelectDocument.Content = pageSelectDocument;

            this.SelectSystem.Visibility = Visibility.Visible;
            this.BtnSystem.Background = colorGray;
        }

        private void BtnSystem_Click(object sender, RoutedEventArgs e)
        {
            this.SelectDocument.Visibility = Visibility.Collapsed;
            this.SelectSystem.Visibility = Visibility.Visible;
           
            this.BtnSystem.Background = colorGray;
            this.BtnDocument.Background = colorTransparent;
        }

        private void BtnDocument_Click(object sender, RoutedEventArgs e)
        {
            this.SelectSystem.Visibility = Visibility.Collapsed;
            this.SelectDocument.Visibility = Visibility.Visible;

            this.BtnSystem.Background = colorTransparent;          
            this.BtnDocument.Background = colorGray;
        }


        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
