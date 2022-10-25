using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using Viewer.upgrade.application;
using Viewer.upgrade.communication.message;
using Viewer.upgrade.cookie;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.viewerWindow.viewModel;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.ui.common.viewerWindow.view
{
    /// <summary>
    /// Interaction logic for ViewerWindow.xaml
    /// </summary>
    public partial class ViewerWindow : Window
    {
        private IViewModel mViewModel;
        private IPCManager mIPCManager;

        public ViewerWindow(Cookie cookie)
        {
            EssentialInitialize();
            mViewModel = new ViewModel(cookie, this);
            this.DataContext = mViewModel;
        }

        public ViewerWindow(string errorMsg, string fileName)
        {
            EssentialInitialize();
            mViewModel = new ErrorViewModel(errorMsg, fileName, this);
            this.DataContext = mViewModel;
        }

        private void EssentialInitialize()
        {
            InitializeComponent();
            InitWindowState();
            mIPCManager = new IPCManager(new Action<int, int, string>(ReceiveData));
        }


        private void ReceiveData(int msg, int wParam, string data)
        {
            if (msg == IPCManager.WM_START_LOGOUT_ACTION)
            {
                this.Close();
            }
        }

        private void InitWindowState()
        {
            this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
        }

        private void SaveCurrentWindowState()
        {
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
            }
            Properties.Settings.Default.WindowState = (int)this.WindowState == 1 ? 0 : (int)this.WindowState;
            Properties.Settings.Default.Save();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveCurrentWindowState();
            mViewModel.Window_Closed();
           // ToolKit.DeleteHwndFromRegistry(mBaseFile.FilePath);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Register hook that use to receive info
            (PresentationSource.FromVisual(this) as HwndSource).AddHook(new HwndSourceHook(mIPCManager.WndProc));
            mViewModel.Window_Loaded();
            ViewerApp.Current.MainWindow = this;

            //  IntPtr hwnd = new WindowInteropHelper(this).Handle;
            //  ToolKit.SaveHwndToRegistry(mBaseFile.FilePath, hwnd);
            //if(mApplication.SystemApplication.ShutdownMode == ShutdownMode.OnMainWindowClose)
            //{
            //    ViewerApp.Current.MainWindow = this;
            //}

            //Int64 code = ToolKit.RunningMode();
            //if (code == 0)
            //{
            //    ViewerApp.Current.MainWindow = this;
            //}
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            mViewModel.Window_ContentRendered();
        }

        private void Frame_Viewer_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                e.Cancel = true;
            }
        }

        private void Frame_Toolbar_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                e.Cancel = true;
            }
        }
    }
}
