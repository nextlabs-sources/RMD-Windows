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
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using SkydrmLocal.rmc;
using SkydrmLocal.rmc.ui.windows.mainWindow.viewModel;
using System.Timers;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.windows.serviceManagerWindow.viewModel;
using System.Threading;
using static SkydrmLocal.rmc.helper.NetworkStatus;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System.Collections;
using System.ComponentModel;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmLocal
{
    public partial class ServiceManager : Window
    {
       // MainWindow mainWindow;
       // FeedBackWindow feedBackWindow;
       // PreferencesWindow preferencesWindow;

        public ViewModelServiceManagerWindow viewModel;
        private SkydrmLocalApp App = (SkydrmLocalApp)SkydrmLocalApp.Current;
     

        public ServiceManager()
        { 
            InitializeComponent();

            viewModel = new ViewModelServiceManagerWindow(this);

            this.DataContext = viewModel;

            // Regsiter network status event listener
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);

            // init network status
            viewModel.IsNetworkAvailable = NetworkStatus.IsAvailable;

            // Disable\Enable service manager Logout.
            MenuDisableMgr.GetSingleton().MenuItemDisabled += (string name, bool isDisabled) =>
            {
                if (name == "Log out")
                {
                    if (isDisabled)
                        this.munu_logout.IsEnabled = false;
                    else
                        this.munu_logout.IsEnabled = true;
                }
            };
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            viewModel.IsNetworkAvailable = e.IsAvailable;
            if (e.IsAvailable)
            {
                var app = SkydrmLocalApp.Singleton;
                app.User.UploadNxlFileLog_Async();
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            
            if (SkydrmLocalApp.clickEventfromNotifyIcon)
                return;
            try
            {
                if (SkydrmLocalApp.clickEventfromNotifyIcon)
                    return;
                Monitor.Enter(SkydrmLocalApp.IsOpenSM);

                if (SkydrmLocalApp.clickEventfromNotifyIcon)
                    return;
                base.OnDeactivated(e);

                if (SkydrmLocalApp.clickEventfromNotifyIcon)
                    return;
                if (SkydrmLocalApp.IsOpenSM)
                {

                    if (SkydrmLocalApp.clickEventfromNotifyIcon)
                        return;
                    if (this.Visibility == Visibility.Visible)
                  {
                        if (SkydrmLocalApp.clickEventfromNotifyIcon)
                            return;
                        SkydrmLocalApp.IsOpenSM = false;
                        this.Hide();
                  }

                }
            }
            catch(Exception ex)
            { 

            }
            finally
            {
                try
                {
                    Monitor.Enter(SkydrmLocalApp.IsOpenSM);
                }catch(Exception ex)
                {

                }
            }

            SkydrmLocalApp.Singleton.UserRecentTouchedFile.Notification -=OnFileStatusChanged;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            SkydrmLocalApp app = SkydrmLocalApp.Singleton;
            //
            // Move wnd's positon to right-bottom corner
            //
            this.Left = SystemParameters.WorkArea.Right - this.Width - 10;
            this.Top = SystemParameters.WorkArea.Height - this.Height;
       
            this.Topmost = true;

            // Mainly used to refresh date time when popup service manager window.
            viewModel.InitData();
            ListSort();
            app.UserRecentTouchedFile.Notification += OnFileStatusChanged;
        }

        public void ListSort()
        {
            smList.Items.SortDescriptions.Clear();
            smList.Items.SortDescriptions.Add(new SortDescription("DateTime", ListSortDirection.Descending));
        } 

        public void OnFileStatusChanged(EnumNxlFileStatus status, string fileName)
        {
            Action<EnumNxlFileStatus, string> updateAction = new Action<EnumNxlFileStatus, string>(viewModel.OnFileStatusChanged);
            Action sortAction = new Action(ListSort);

            smList.Dispatcher.Invoke(updateAction, status, fileName);
            smList.Dispatcher.Invoke(sortAction);  
        }

        private void btnMenu_Initialized(object sender, EventArgs e)
        {
            this.btnMenu.ContextMenu = null;
        }

        private void munu_about_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;
            app.Log.Info("show about the project");
            app.Mediator.OnShowAboutTheProject(this);
        }

        private void munu_help_Click(object sender, RoutedEventArgs e) 
        {
            SkydrmLocal.rmc.SkydrmLocalApp.Singleton.OnShowAppHelpInformation();                    
        }

        private void munu_feedback_Click(object sender, RoutedEventArgs e)
        {
             var app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;
             app.Log.Info("ServiceManager window to FeedBack window");
             app.Mediator.OnShowFeedBack(this);
        }

        private void munu_logout_Click(object sender, RoutedEventArgs e)
        {        
            App.Logout(this);
        }

        private void munu_preferences_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;
            app.Log.Info("ServiceManager window to Preference window");
            app.Mediator.OnShowPreference(this);        
        } 
        private void MenuToMain_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;
            app.Log.Info("ServiceManager window to Main window");
            app.Mediator.OnShowMain(this);
        }

        private void WebPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommonUtils.OpenSkyDrmWeb();
            }
            catch (Exception ignore)
            {
                App.Log.Warn("Error occured when do open skydrm web page in service manger[WebPage_Click]\n", ignore);
            }
        }

        private void PreferencesWindow_Click(object sender, RoutedEventArgs e)
        {

            var app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;
            app.Log.Info("ServiceManager window to Preference window");
            app.Mediator.OnShowPreference(this);
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            this.contextMenu.PlacementTarget = this.btnMenu;

            this.contextMenu.Placement = PlacementMode.MousePoint;

            this.contextMenu.IsOpen = true;
        }

        private void smList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
       
            //if (uploading_info.Visibility!=Visibility.Visible)
            //{
            //    uploading_info.Visibility = Visibility.Visible;
            //}

        }

        private void OpenSkyDrmLoacl_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as SkydrmLocal.rmc.SkydrmLocalApp;
            app.Log.Info("ServiceManager window to Main window");
            app.Mediator.OnShowMain(this);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Hide();
            }
        }
    }

}
