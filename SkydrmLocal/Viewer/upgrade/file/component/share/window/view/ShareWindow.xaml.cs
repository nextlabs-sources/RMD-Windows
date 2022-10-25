using SkydrmLocal.rmc.sdk;
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
using Viewer.upgrade.file.component.share.page.sharePage.view;
using Viewer.upgrade.file.component.share.page.sharePage.viewModel;

namespace Viewer.upgrade.file.component.share.window.view
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
        private NxlFileFingerPrint mNxlFileFingerPrint;
        private SharePage mSharePage;

        public ShareWindow(string nxlFilePath)
        {
            InitializeComponent();
            this.mNxlFilePath = nxlFilePath;
            SharePageViewModel sharePageViewModel = new SharePageViewModel(mNxlFilePath, this);
            mSharePage = new SharePage(sharePageViewModel);
        }

        public ShareWindow(NxlFileFingerPrint nxlFileFingerPrint)
        {
            InitializeComponent();
            this.mNxlFileFingerPrint = nxlFileFingerPrint;
            SharePageViewModel sharePageViewModel = new SharePageViewModel(mNxlFileFingerPrint, this);
            mSharePage = new SharePage(sharePageViewModel);
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
            if (null != mSharePage)
            {
                this.main_frame.Content = mSharePage;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
