using SkydrmLocal.rmc.sdk;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Viewer.overlay;
using Viewer.viewer;

namespace Viewer.render.preview
{
    /// <summary>
    /// Interaction logic for PreviewHandler.xaml
    /// </summary>
    public partial class PreviewHandlerPage : Page, IPrintable
    {
        [DllImport("user32.dll")]
        private static extern int MoveWindow(int hwnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        // Viewer window
        private ViewerWindow mViewerWindow;
        private OverlayWindow mOverlayWindow;
        private static string mStartPrintParamaters = string.Empty;
        private string mFilePath = string.Empty;
        private string mPrintFilePath = string.Empty;
        private bool mIsCanAccelerate = false;
        private double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private float DpiX = 96;
        private float DpiY = 96;
        private log4net.ILog mLog;
        private Session mSession;

        public OverlayWindow OverlayWindow
        {
            get { return mOverlayWindow;}
        }

        public PreviewHandlerPage(ViewerWindow window,
                                  string filePath,
                                  WatermarkInfo watermarkInfo,
                                  bool isCanAccelerate,
                                  Session session,
                                  log4net.ILog log
                                  )
        {
            InitializeComponent();
            mLog = log;
            mSession = session;
            mLog.Info("\t\t PreviewHandlerPage \r\n");
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                DpiX = graphics.DpiX;
                DpiY = graphics.DpiY;
            }
            mViewerWindow = window;
            mFilePath = filePath;
            mViewerWindow.Closing += new CancelEventHandler(Window_Closing);
            mViewerWindow.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
            mViewerWindow.LocationChanged += new EventHandler(Window_LocationChanged);
            mViewerWindow.StateChanged += new EventHandler(Window_StateChanged);
            mIsCanAccelerate = isCanAccelerate;
            InitOverlayWindow(mViewerWindow, watermarkInfo, out mOverlayWindow, log);
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mViewerWindow.Status == StatusOfView.NORMAL)
            {
                MoveOverlayWindow();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {

            if (mViewerWindow.Status == StatusOfView.NORMAL)
            {
                switch (mViewerWindow.WindowState)
                {
                    case WindowState.Maximized:
                        mOverlayWindow.Show();
                        break;
                    case WindowState.Minimized:

                        mOverlayWindow.Hide();
                        break;
                    case WindowState.Normal:
                        mOverlayWindow.Show();
                        break;
                }
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (mViewerWindow.Status == StatusOfView.NORMAL)
            {
                MoveOverlayWindow();
            }
        }

        public void HideOverlayWindow()
        {
            this.mOverlayWindow.Visibility = Visibility.Collapsed;
        }

        public void ShowCursorsWait()
        {
            this.mOverlayWindow.Cursor = Cursors.Wait;
        }

        public void HideCursorsWait()
        {
            this.mOverlayWindow.Cursor = Cursors.Arrow;
        }

        public void DisableOverlayWindowr()
        {
            this.mOverlayWindow.Host_Grid.IsEnabled = false;
        }

        public void EnableOverlayWindowr()
        {
            this.mOverlayWindow.Host_Grid.IsEnabled = true;
        }

        public void Window_Closing(object sender, CancelEventArgs e)
        {
              //  PreviewHandler.Instance().Unload();
        }

        public void MoveOverlayWindow()
        {
            if (null != mOverlayWindow)
            {
                System.Windows.Point position = mViewerWindow.Viewer_Content.TranslatePoint(new System.Windows.Point(0d, 0d), this);

                position = this.PointToScreen(position);

                IntPtr OverLay_hwnd = new WindowInteropHelper(mOverlayWindow).Handle;
                if (null != OverLay_hwnd)
                {
                    int offsets = 0;
                    if (mViewerWindow.CurrentFileType == EnumFileType.FILE_TYPE_PDF)
                        offsets = 35;

                    int width = Convert.ToInt32(Math.Round(mViewerWindow.Viewer_Content.ActualWidth * (DeviceIndependentUnit * DpiX)));

                    int height = Convert.ToInt32(Math.Round(mViewerWindow.Viewer_Content.ActualHeight * (DeviceIndependentUnit * DpiY))) - offsets;

                    MoveWindow((int)OverLay_hwnd, Convert.ToInt32(Math.Round(position.X)), Convert.ToInt32(Math.Round(position.Y)),
                    width, height, false);
                }
            }
        }

        private void InitOverlayWindow(ViewerWindow viewerWindow, WatermarkInfo watermarkInfo, out OverlayWindow overlayWindow, log4net.ILog log)
        {
            mLog.InfoFormat("\t\t Init Overlay Window \r\n");
            overlayWindow = new OverlayWindow(watermarkInfo, log);
            overlayWindow.Owner = viewerWindow;
        }

        public void RenderEventHandler(bool isSuccess, Exception ex)
        {
            if (isSuccess)
            {
                mLog.Info("\t\t Show OverlayWindow \r\n");
                mOverlayWindow.Show();
                mLog.Info("\t\t Show DismissLoadinBar \r\n");
                mViewerWindow.DismissLoadinBar();
                mLog.Info("\t\t Show MoveOverlayWindow \r\n");
                MoveOverlayWindow();
                mViewerWindow.Viewer_Content.Visibility = Visibility.Visible;
            }
            else
            {
                mViewerWindow.Viewer_Content.Visibility = Visibility.Visible;
                mViewerWindow.HandleOfficeFileRenderException();
                mLog.Error(ex);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PreviewHandlerHost.Render += new RenderEventHandler(RenderEventHandler);

            if (mIsCanAccelerate)
            {
                AcceleratedRenderingDevice.OpenFileAsync(PreviewHandlerHost, mFilePath, mLog);
            }
            else
            {
                PreviewHandlerHost.Open(mFilePath, PreviewHandler.Instance(), mSession, mLog);
            }

            //if (ViewerWin.CurrentFileType == EnumFileType.FILE_TYPE_PDF)
            //{
            //    try
            //    {
            //        PreviewHandlerHost.Open(new FileStream(NxlConverterResult.TmpPath,
            //                          FileMode.Open,
            //                          FileAccess.Read,
            //                          FileShare.None, 4096,
            //                          FileOptions.None), PreviewHandlerHost.GetPreviewHandlerGUID(NxlConverterResult.TmpPath));
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.ToString());
            //    }
            //}
            //else
            //{
            //    try
            //    {
            //        PreviewHandlerHost.Open(NxlConverterResult.TmpPath);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.ToString());
            //    }
            //}
        }


        #region Print

        public void Print()
        {
            ViewerApp viewerApp = (ViewerApp)Application.Current;
            IntPtr OverLay_hwnd = new WindowInteropHelper(OverlayWindow).Handle;

            Process mProc = new Process();
            mProc.StartInfo.FileName = "nxrmprint.exe";
            // Set Print.exe process dir
            mProc.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            mProc.StartInfo.CreateNoWindow = true;
            mProc.StartInfo.Arguments += "-print";
            mProc.StartInfo.Arguments += " ";
            mProc.StartInfo.Arguments += OverLay_hwnd.ToInt32();
            mProc.StartInfo.Arguments += " ";
            mProc.StartInfo.Arguments += "\"" + viewerApp.Intent.FilePath + "\"";
            mProc.Start();

            //if (string.IsNullOrEmpty(mStartPrintParamaters))
            //{
            //    try
            //    {
            //        StringBuilder wmText = new StringBuilder();
            //        CommonUtils.ConvertWatermark2DisplayStyle(mAdhocWaterMark, mUserEmail, ref wmText);
            //        PrintArgument printArgument = new PrintArgument();

            //        if (OverlayWindow == null)
            //        {
            //            return;
            //        }

            //        IntPtr OverLay_hwnd = new WindowInteropHelper(OverlayWindow).Handle;

            //        IntPtr Viewer_hwnd = new WindowInteropHelper(ViewerWin).Handle;

            //        if (null == OverLay_hwnd || null == Viewer_hwnd)
            //        {
            //            return; 
            //        }

            //        printArgument.IntPtrOfOverlayWindow = OverLay_hwnd.ToInt32();
            //        printArgument.IntPtrOfViewerWindow = Viewer_hwnd.ToInt32();
            //        printArgument.CopyedFilePath = mPrintFilePath;
            //        printArgument.AdhocWatermark = wmText.ToString();

            //        string json = JsonConvert.SerializeObject(printArgument);

            //        mStartPrintParamaters = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            //    }
            //    catch (Exception ex)
            //    {
            //        mStartPrintParamaters = string.Empty;
            //        MessageBox.Show(OverlayWindow, CultureStringInfo.VIEW_DLGBOX_DETAILS_SYSTEM_INTERNAL_ERROR, CultureStringInfo.View_DlgBox_Title);
            //    }
            //}

            //if (string.IsNullOrEmpty(mStartPrintParamaters))
            //{
            //    return;
            //}

            //InsertPrintTaskQueueResponse response = NamedPipesClient.StartOfficeFilePrint(mStartPrintParamaters);

            //switch (response)
            //{
            //    case InsertPrintTaskQueueResponse.Succeeded:
            //        break;

            //    case InsertPrintTaskQueueResponse.Failed:
            //        MessageBox.Show(OverlayWindow, CultureStringInfo.VIEW_DLGBOX_DETAILS_PRINT_SERVER_BUSY, CultureStringInfo.View_DlgBox_Title);
            //        break;

            //    case InsertPrintTaskQueueResponse.Connecting:
            //        MessageBox.Show(OverlayWindow, CultureStringInfo.VIEW_DLGBOX_DETAILS_PRINT_SERVER_BUSY, CultureStringInfo.View_DlgBox_Title);
            //        break;

            //    case InsertPrintTaskQueueResponse.NoResponse:
            //        try
            //        {
            //            Process mProc = new Process();
            //            mProc.StartInfo.FileName = "Print.exe";
            //            // Set Print.exe process dir
            //            mProc.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            //            mProc.StartInfo.CreateNoWindow = true;
            //            mProc.StartInfo.Arguments = mStartPrintParamaters;
            //            mProc.Start();
            //        }
            //        catch (Exception ex)
            //        {

            //        }
            //        break;
            //}
            #endregion Print
        }


    }
}
