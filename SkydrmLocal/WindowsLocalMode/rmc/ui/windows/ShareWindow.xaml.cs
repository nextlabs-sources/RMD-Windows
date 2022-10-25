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
using SkydrmLocal.rmc.ui.pages;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.shareNxlFeature;
using SkydrmLocal.rmc.common.component;

namespace SkydrmLocal.rmc.ui.windows
{
    /// <summary>
    /// Interaction logic for ShareWindow.xaml
    /// History:
    ///     by osmond, avoid to use the class NxlFile
    /// </summary>
    public partial class ShareWindow : Window
    {
        private SkydrmLocalApp App = SkydrmLocalApp.Singleton;

        //for invoke SDK
        private bool isSuccess=true;
        private ProtectAndShareConfig tempConfig;

        public NxlFileFingerPrint FingerPrint { get; set; }

        // Flag that if the outlook address book window is closing or not, if yes and will cause other side effects,
        // now will forbid other side effects causing by this flag. --- fix bug 55194
        public bool IsClosingOutlookAddressBookWin { get; set; }

        // For online nxlFile UpdateRecipients and Share
        public ShareWindow()
        {
            InitializeComponent();

            InitEvent();

            this.GridProBar.Visibility = Visibility.Visible;
        }

        // For file do protect&share
        public ShareWindow(ProtectAndShareConfig config)
        {
            InitializeComponent();

            InitEvent();

            ParseConfigs(config);
        }

        private void InitEvent()
        {
            // Used to handle window display issue across processse(open the window from viewer process)
            this.Loaded += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                this.Topmost = false;
            });
            this.Activated += new EventHandler(delegate (object sender, EventArgs e)
            {
                this.Topmost = false;
                this.Focus();
            });
        }

        private void ParseConfigs(ProtectAndShareConfig config)
        {
            if (config == null)
            {
                Console.WriteLine("config is null in ShareWindow");
                return;
            }
            tempConfig = config;

            SharePage sharePage = new SharePage(config)
            {
                ParentWindow = this
            };
            SwitchPage(sharePage);
        }

        public void SwitchPage(Page page)
        {
            this.main_frame.Content = page;
        }

        /// <summary>
        /// For online nxlFile set Share config
        /// </summary>
        /// <param name="config"></param>
        public void InitShareConfig(ProtectAndShareConfig config)
        {
            this.GridProBar.Visibility = Visibility.Collapsed;
            // switch share page
            ParseConfigs(config);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            //Calculate half of the offset to move the form

            if (sizeInfo.HeightChanged)
                this.Top += (sizeInfo.PreviousSize.Height - sizeInfo.NewSize.Height) / 2;

            if (sizeInfo.WidthChanged)
                this.Left += (sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width) / 2;
        }


        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // during file-protecting and share , can't  be closed. 
            if (MenuDisableMgr.GetSingleton().IsSharing)
            {
                e.Cancel = true;
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.CreateFileWin_Notify_Wait_Protect);
                return;
            }

            App.Log.Info("----IsClosingOutlookAddressBookWin----> " + IsClosingOutlookAddressBookWin);
            if (IsClosingOutlookAddressBookWin)
            {
                e.Cancel = true;
                return;
            }

        }

        private void WIindow_Closed(object sender, EventArgs e)
        {
            // For online NxlFile do share,should delete downloaded nxlFile
            // If project's nxlFile be shared to mayVault,should delete decrypt file in RPM folder.
            tempConfig?.ShareNxlFeature?.DeleteNxlFile();
            tempConfig?.ShareNxlFeature?.DeleteRPM_File();
        }

    }
}
