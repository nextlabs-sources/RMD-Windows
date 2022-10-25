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

namespace Viewer.share
{
    /// <summary>
    /// Interaction logic for ShareWindow.xaml
    /// </summary>
    public partial class ShareWindow : Window
    {
        // Flag that if the outlook address book window is closing or not, if yes and will cause other side effects,
        // now will forbid other side effects causing by this flag. --- fix bug 55194
        public bool IsClosingOutlookAddressBookWin { get; set; }
        private string mNxlFilePath;

        public ShareWindow(string nxlFilePath)
        {
            InitializeComponent();
            this.mNxlFilePath = nxlFilePath;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsClosingOutlookAddressBookWin)
            {
                e.Cancel = true;
                return;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            SharePageViewModel sharePageViewModel = new SharePageViewModel(mNxlFilePath, this);

            SharePage sharePage = new SharePage(sharePageViewModel);

            this.main_frame.Content = sharePage;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
