using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Viewer.utils;

namespace Viewer.share
{
    /// <summary>
    /// Interaction logic for SharePage.xaml
    /// </summary>
    public partial class SharePage : Page
    {
        private SharePageViewModel mSharePageViewModel;

        public SharePage(SharePageViewModel sharePageViewModel)
        {
            InitializeComponent();
            this.mSharePageViewModel = sharePageViewModel;
            this.DataContext = mSharePageViewModel;
        }

        private void EmailInputTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            mSharePageViewModel.EmailInputTB_TextChanged(sender,e);
        }
 
        private void EmailInput_KeyDown(object sender, KeyEventArgs e)
        {
            mSharePageViewModel.EmailInput_KeyDown(sender, e);
        }

        private void On_GetOutlookEmail_Btn(object sender, MouseButtonEventArgs e)
        {
            mSharePageViewModel.On_GetOutlookEmail_Btn(sender, e);
        }

        private void DeleteEmailItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mSharePageViewModel.DeleteEmailItem_MouseLeftButtonUp(sender, e);
        }

        private void CommentTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            mSharePageViewModel.CommentTB_TextChanged(sender, e);
        }

        private void Button_Ok(object sender, RoutedEventArgs e)
        {
            mSharePageViewModel.Button_Ok(sender,e);
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            mSharePageViewModel.Button_Cancel(sender,e);
        }

    }
}
