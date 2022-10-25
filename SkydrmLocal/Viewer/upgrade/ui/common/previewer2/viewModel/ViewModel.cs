using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Viewer.upgrade.application;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.overlayWindow.view;
using Viewer.upgrade.ui.common.previewer2.view;
using Viewer.upgrade.utils;
using Viewer.upgrade.utils.overlay.utils;
using Viewer.upgrade.utils.overlay.windowOverlay;

namespace Viewer.upgrade.ui.common.previewer2.viewModel
{
    public class ViewModel : ISensor
    {
        public event Action<Exception> OnUnhandledExceptionOccurrence;
        public event Action<Window> OnOverlayWindowLoaded;
        public event Action OnLoadFileSucceed;

        private ViewerApp mApplication;
        private log4net.ILog mLog;
        private const int mOffset = 50;
        private WatermarkInfo mWatermarkInfo;
        private Window mParentWindow;
        private string mFilePath;
        private PreviewHandler mPreviewHandler;
        private PreviewerPage mPreviewerPage;
        private OverlayWindow mOverlayWindow;
        private int mOverlayOffsetsBottom = 0;
        private bool mWithOverlay = false;
        private INxlFile mNxlFile;

        public INxlFile NxlFile
        {
            get { return mNxlFile; }
            set { mNxlFile = value; }
        }

        public WatermarkInfo WatermarkInfo
        {
            get { return mWatermarkInfo; }
            set { mWatermarkInfo = value; }
        }

        public int OverlayOffsestBottom
        {
            get { return mOverlayOffsetsBottom; }
            set { mOverlayOffsetsBottom = value; }
        }

        public ViewModel(string filePath, PreviewHandler previewHandler, PreviewerPage previewerPage)
        {
            mApplication = (ViewerApp)ViewerApp.Current;
            mLog = mApplication.Log;
            mFilePath = filePath;
            mPreviewHandler = previewHandler;
            mPreviewerPage = previewerPage;
        }

        public void Print()
        {
            if (null == mOverlayWindow)
            {
                return;
            }
            IntPtr OverLay_hwnd = new WindowInteropHelper(mOverlayWindow).Handle;
            Process mProc = new Process();
            mProc.StartInfo.FileName = "nxrmprint.exe";
            mProc.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            mProc.StartInfo.CreateNoWindow = true;
            mProc.StartInfo.Arguments += "-print";
            mProc.StartInfo.Arguments += " ";
            mProc.StartInfo.Arguments += OverLay_hwnd.ToInt32();
            mProc.StartInfo.Arguments += " ";
            mProc.StartInfo.Arguments += "\"" + NxlFile.FilePath + "\"";
            mProc.Start();
        }

        public void Page_Loaded()
        {
            try
            {
                mParentWindow = Window.GetWindow(mPreviewerPage);
                if (IsAttachWatermark())
                {
                    AttachWatermark();
                }
                LoadFile();
                mParentWindow.Closed += Window_Closed;
                mParentWindow.SizeChanged += Window_SizeChanged;
                mParentWindow.LocationChanged += Window_LocationChanged;
                mParentWindow.StateChanged += Window_StateChanged;
                mParentWindow.IsVisibleChanged += VisibleChangedEventHandler;

                OnOverlayWindowLoaded?.Invoke(mOverlayWindow);
                OnLoadFileSucceed?.Invoke();
            }
            catch (Exception ex)
            {
                OnUnhandledExceptionOccurrence?.Invoke(ex);
            }
            
            //mParentWindow = Window.GetWindow(mPreviewerPage);
            //UInt64 res;
            //if (IsAttachWatermark())
            //{
            //    res = AttachWatermark();
            //    if (ErrorCode.SUCCEEDED == res)
            //    {
            //        mWithOverlay = true;
            //    }
            //    else
            //    {
            //        OnUnhandledExceptionOccurrence?.Invoke(res);
            //    }
            //}
            //res = LoadFile();
            //if (ErrorCode.SUCCEEDED == res)
            //{
            //    mParentWindow.Closed += Window_Closed;
            //    mParentWindow.SizeChanged += Window_SizeChanged;
            //    mParentWindow.LocationChanged += Window_LocationChanged;
            //    mParentWindow.StateChanged += Window_StateChanged;
            //    OnOverlayWindowLoaded?.Invoke(mOverlayWindow);
            //    OnLoadFileSucceed?.Invoke();
            //}
            //else
            //{
            //    OnUnhandledExceptionOccurrence?.Invoke(res);
            //}
        }

        public void VisibleChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (null == mOverlayWindow)
            {
                return;
            }
            bool flag = (bool)e.NewValue;
            if (flag)
            {
                mOverlayWindow.Show();
            }
            else
            {
                mOverlayWindow.Hide();
            }
        }

        private void LoadFile()
        {
            try
            {
                //IntPtr intPtr = new WindowInteropHelper(mParentWindow).Handle;
                // System.Windows.Point position = mPreviewerPage.TranslatePoint(new System.Windows.Point(), mParentWindow);
                //Rectangle rectangle = new Rectangle(new System.Drawing.Point((int)position.X, (int)position.Y),
                //                                    new System.Drawing.Size((int)mPreviewerPage.ActualWidth,
                //                                    (int)mPreviewerPage.ActualHeight + mOffset*2));
                IntPtr intPtr = mPreviewerPage.PreviewHandlerHost.Handle;
               // mPreviewerPage.PreviewHandlerHost.Update(); // try fix Bug 51404 - [doc] word file content not display to top 
                Rectangle rectangle = mPreviewerPage.PreviewHandlerHost.ClientRectangle;
                mPreviewHandler.DoPreview(mFilePath, intPtr, rectangle);
                mParentWindow.Activate();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private bool IsAttachWatermark()
        {
            if (null != mWatermarkInfo)
            {
                return true;
            }
            return false;
        }

        private void AttachWatermark()
        {
            try
            {
                mOverlayWindow = new OverlayWindow(mWatermarkInfo, mPreviewerPage.PointToScreen(new System.Windows.Point()),
                                                (int)mPreviewerPage.ActualWidth,
                                                (int)mPreviewerPage.ActualHeight - mOverlayOffsetsBottom);
                mOverlayWindow.Owner = mParentWindow;
                mOverlayWindow.Show();
                SetRectOverlayWindow();
                mParentWindow.Focus();
                mWithOverlay = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void SetRectOverlayWindow()
        {
 
            mOverlayWindow.SetRect(mPreviewerPage.PointToScreen(new System.Windows.Point()),
                                         (int)mPreviewerPage.ActualWidth,
                                         (int)mPreviewerPage.ActualHeight-mOverlayOffsetsBottom);
        }

        public void Page_Unloaded()
        {
            Release();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Release();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //System.Windows.Point position = mPreviewerPage.TranslatePoint(new System.Windows.Point(), mParentWindow);
            //Rectangle rectangle = new Rectangle(new System.Drawing.Point((int)position.X, (int)position.Y),
            //                                    new System.Drawing.Size((int)mPreviewerPage.ActualWidth,
            //                                    (int)mPreviewerPage.ActualHeight + mOffset));
            Rectangle rectangle = mPreviewerPage.PreviewHandlerHost.ClientRectangle;
            mPreviewHandler.OnResize(rectangle);
            mParentWindow.Focus();
            if (mWithOverlay)
            {
                SetRectOverlayWindow();
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (mWithOverlay)
            {
                SetRectOverlayWindow();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (mParentWindow.WindowState)
            {
                case WindowState.Maximized:
                    mOverlayWindow?.Show();
                    mParentWindow.Focus();
                    break;
                case WindowState.Minimized:
                    mOverlayWindow?.Hide();
                    break;
                case WindowState.Normal:
                    mOverlayWindow?.Show();
                    mParentWindow.Focus();
                    break;
            }
        }

        private void ReleaseComObj()
        {
            mPreviewHandler.Dispose();
        }

        private void Release()
        {
            mParentWindow.Closed -= Window_Closed;
            mParentWindow.SizeChanged -= Window_SizeChanged;
            mParentWindow.LocationChanged -= Window_LocationChanged;
            mParentWindow.StateChanged -= Window_StateChanged;
            mParentWindow.IsVisibleChanged -= VisibleChangedEventHandler;
            mOverlayWindow?.Close();
            ReleaseComObj();
        }
    }
}
