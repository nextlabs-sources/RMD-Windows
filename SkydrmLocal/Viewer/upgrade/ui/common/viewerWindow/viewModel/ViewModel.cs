using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Viewer.upgrade.application;
using Viewer.upgrade.cookie;
using Viewer.upgrade.exception;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.errorPage.view;
using Viewer.upgrade.ui.common.loadingBar;
using Viewer.upgrade.ui.common.loadingBarPage.view;
using Viewer.upgrade.ui.common.overlayWindow.view;
using Viewer.upgrade.ui.common.viewerWindow.view;
using Viewer.upgrade.ui.nxl.page.toolbar.view;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.ui.common.viewerWindow.viewModel
{
    public class ViewModel : AbsViewModel
    {
        private ViewerApp mApplication;
        private log4net.ILog mLog;
        private Cookie mCookie;
        private IFileLoader mFileLoader;

        public ViewModel(Cookie cookie, ViewerWindow viewerWindow) : base(viewerWindow)
        {
            mApplication = (ViewerApp)ViewerApp.Current;
            mLog = mApplication.Log;
            mCookie = cookie;
            mViewerWindow = viewerWindow;
            InitializeLoadingBarPage();
        }

        public void InitializeLoadingBarPage()
        {
            LoadingBarPage loadingBarPage = new LoadingBarPage();

            Viewer = new Frame
            {
                Content = loadingBarPage
            };
        }

        public override void Window_Closed()
        {
            ToolKit.DeleteHwndFromRegistry(mCookie.FilePath);
            mFileLoader?.Close();
        }

        public override void Window_Loaded()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(mViewerWindow).Handle;
                ToolKit.SaveHwndToRegistry(mCookie.FilePath, hwnd);
            }
            catch (Exception ex)
            {

            }
        }

        public override void Window_ContentRendered()
        {
            try
            {
                _BaseFile _BaseFile = new _BaseFile(mCookie);
                if (_BaseFile.IsNxlFile)
                {
                    mFileLoader = new NxlFileLoader(this, _BaseFile);
                }
                else
                {
                    mFileLoader = new NativeFileLoader(this, _BaseFile);
                }
            }
            catch (RmSdkException ex)
            {
                LoadErrorPage(Path.GetFileName(mCookie.FilePath), mApplication.FindResource("Common_System_Internal_Error").ToString());
                SendViewLog(false);
            }
            catch (FileExpiredException ex)
            {
                LoadErrorPage(Path.GetFileName(mCookie.FilePath), mApplication.FindResource("Nxl_File_Has_Expired").ToString());
                SendViewLog(false);
            }
            catch (NotAuthorizedException ex)
            {
               LoadErrorPage(Path.GetFileName(mCookie.FilePath), mApplication.FindResource("VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED").ToString());
                SendViewLog(false);
            }
            catch (NxlFileException ex)
            {
                LoadErrorPage(Path.GetFileName(mCookie.FilePath), ex.Message);
                SendViewLog(false);
            }
            catch (Exception ex)
            {
                LoadErrorPage(Path.GetFileName(mCookie.FilePath), mApplication.FindResource("Common_System_Internal_Error").ToString());
                SendViewLog(false);
            }
        }

        public void SendViewLog(bool allow)
        {
            mApplication.SdkSession.User.AddLog(mCookie.FilePath, NxlOpLog.View, allow);
        }

        public void SendPrintLog(bool allow)
        {
            mApplication.SdkSession.User.AddLog(mCookie.FilePath, NxlOpLog.Print, allow);
        }

    }
}
