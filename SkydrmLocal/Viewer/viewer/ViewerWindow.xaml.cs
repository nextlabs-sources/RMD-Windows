using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Windows;
using System.Windows.Interop;
using Viewer.render;
using Viewer.render.av.AvViewer;
using Viewer.render.hoops.ThreeDView;
using Viewer.render.RichMedia;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Viewer.utils;
using Viewer.viewer;
using Viewer.overlay;
using System.ComponentModel;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using Viewer.viewer.model;
using System.Linq;
using System.Threading;
using Viewer.render.preview;
using System.Runtime.InteropServices;
using Viewer.hoops;
using System.Text;
using Viewer.utils.messagebox;
using Viewer.communication;
using Viewer.namedPipesClient;
using System.Drawing;
using static Viewer.namedPipesClient.NamedPipesClient;
using Viewer.extractContent;
using static Viewer.utils.NetworkStatus;
using System.Windows.Threading;
using Viewer.render.sap3dviewer;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
  
    public partial class ViewerWindow 
    {
        #region Data
        /* Private */
        private float DpiX = 96;
        private float DpiY = 96;
        private OverlayWindow OverlayWindow { get; set; }
        private IPCManager Ipc { get; set; }
        // Main window handle of SkydrmLocal process
        private IntPtr HMainWin { get; set; }
        //ViewerWindow handle
        private IntPtr Hwnd { get; set; } 
        // 3d converted output path
        private string ConvertedOutPath { get; set; }
        // current viewer.
        private object Viewer { get; set; }
        // received json string
        private string ResultJson { get; set; }
        private NxlConverterResult NxlConverterResult { get; set; }
        private StatusOfView mStatus;
        private double DeviceIndependentUnit = Convert.ToDouble(1) / Convert.ToDouble(96);
        private FileStream NxlFileStream { get; set; }
        private EventHandler CloseEventHandler { get; set; }
        private CancelEventHandler ClosingEventHandler { get; set; }
        private bool InterruptingCloseWindow = false;
        private WatermarkInfo WatermarkInfo { get; set; }
        // network status
        private bool isNetworkAvailable;
  
        /* Public */
        public event PropertyChangedEventHandler PropertyChanged;
        public EnumFileType CurrentFileType { get; set; }
        public bool IsNetworkAvailable
        {
            get { return isNetworkAvailable; }
            set
            {
                isNetworkAvailable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsNetworkAvailable"));
            }
        }
        #endregion Data

        #region Private

        [DllImport("user32.dll")]
        private static extern int MoveWindow(int hwnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //fix bug 55280
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            Hwnd = wndHelper.Handle;
            Win32Common.BringWindowToTop(Hwnd, Process.GetCurrentProcess());
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {

            //WatermarkInfo.Builder builder = new WatermarkInfo.Builder();
            //this.WatermarkInfo = builder.DefaultSet("test watermark", "Jack.Zhou@nextlabs.com").Build();
            //CurrentFileType = EnumFileType.FILE_TYPE_SAP_VDS;
            //this.Print.Visibility = Visibility.Collapsed;
            //Viewer = new SapThreeDViewer(this, @"C:\Users\jrzhou\Desktop\TestFiles\Rh\Cylinder_1_123_100_0_Parts.rh", WatermarkInfo);
            //this.Viewer_Content.Content = Viewer;
            //DismissLoadinBar();

            Init();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void InitWindowState()
        {
            this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if ((CurrentFileType == EnumFileType.FILE_TYPE_OFFICE || CurrentFileType == EnumFileType.FILE_TYPE_PDF) && mStatus == StatusOfView.NORMAL)
            {
                MoveOverlayWindow();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if ((CurrentFileType == EnumFileType.FILE_TYPE_OFFICE || CurrentFileType == EnumFileType.FILE_TYPE_PDF) && mStatus == StatusOfView.NORMAL)
            {
                switch (this.WindowState)
                {
                    case WindowState.Maximized:
                        OverlayWindow.Show();
                        break;
                    case WindowState.Minimized:

                        OverlayWindow.Hide();
                        break;
                    case WindowState.Normal:
                        OverlayWindow.Show();
                        break;
                }
            }
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

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if ((CurrentFileType == EnumFileType.FILE_TYPE_OFFICE
                || CurrentFileType == EnumFileType.FILE_TYPE_PDF)
                && mStatus == StatusOfView.NORMAL)
            {
                MoveOverlayWindow();
            }
        }

        private void InitOverlayWindow(ViewerWindow viewerWindow, WatermarkInfo watermarkInfo)
        {
            ViewerApp.Log.InfoFormat("\t\t Init Overlay Window \r\n");
            if (CurrentFileType == EnumFileType.FILE_TYPE_OFFICE

            || CurrentFileType == EnumFileType.FILE_TYPE_PDF
            )
            {
                OverlayWindow = new OverlayWindow(watermarkInfo);
                OverlayWindow.Owner = viewerWindow;
            }
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            SendLog(new Log(NxlConverterResult.LocalDiskPath, "Print", true));

            (Viewer as IPrintable)?.Print();
        }

        private void FileInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            CommunicationWithSkydrmLocal.FileInfo(NxlConverterResult, ResultJson, Ipc, HMainWin);
        }

        private void ShareBtn_Click(object sender, RoutedEventArgs e)
        {
            CommunicationWithSkydrmLocal.Share(NxlConverterResult, ResultJson, Ipc, HMainWin);
        }

        private void RotateAntiBtn_Click(object sender, RoutedEventArgs e)
        {
            (Viewer as ImageViewer)?.RotateLeft();
        }

        private void RotateBtn_Click(object sender, RoutedEventArgs e)
        {
            (Viewer as ImageViewer)?.RotateRight();
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            InterruptingCloseWindow = true;
            UnlockFile(NxlFileStream);
            CommunicationWithSkydrmLocal.Edit(NxlConverterResult);
            DisableTopmostContainer();
            ShowCursorsWait();
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            CommunicationWithSkydrmLocal.Export(NxlConverterResult, this);
        }

        private void HideViewer()
        {
            this.OverlayWindow.Visibility = Visibility.Collapsed;
            this.Visibility = Visibility.Collapsed;
            InterruptingCloseWindow = false;
        }

        private void ShowCursorsWait()
        {
            this.OverlayWindow.Cursor = Cursors.Wait;
            this.Cursor = Cursors.Wait;
        }

        private void DisableTopmostContainer()
        {
            this.OverlayWindow.Host_Grid.IsEnabled = false;
            this.Topmost_Container.IsEnabled = false;
        }

        private void DisableAllButton()
        {
            this.Print.IsEnabled = false;
            this.FileInfoBtn.IsEnabled = false;
            this.ShareFile.IsEnabled = false;
            this.EditBtn.IsEnabled = false;
            this.RotateStackPanel.IsEnabled = false;
            this.VerticalSeperateLine.IsEnabled = false;
            this.ExportBtn.IsEnabled = false;
            this.Extract_Content_Btn.IsEnabled = false;
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                IsNetworkAvailable = e.IsAvailable;

                if (mStatus == StatusOfView.NORMAL)
                {
                    if (e.IsAvailable)
                    {
                        if (!NxlConverterResult.IsByCentrolPolicy
                                                      &&
                            NxlConverterResult.Rights.Contains(FileRights.RIGHT_SHARE))
                        {

                            if (NxlConverterResult.EnumFileRepo == EnumFileRepo.EXTERN)
                            {
                                this.ShareFile.Visibility = Visibility.Visible;
                            }
                            else if (NxlConverterResult.EnumFileRepo == EnumFileRepo.REPO_MYVAULT)
                            {
                                this.ShareFile.Visibility = Visibility.Visible;
                            }
                        }

                        this.ExportBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (NxlConverterResult.EnumFileRepo == EnumFileRepo.REPO_MYVAULT)
                        {
                            this.ShareFile.Visibility = Visibility.Collapsed;
                        }
                        this.ExportBtn.Visibility = Visibility.Collapsed;
                    }
                }
            }));
        }

        private void SendLog(Log log)
        {
            try
            {
                CommunicationWithSkydrmLocal.SendLog(log);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void HandlePrintResult(string message)
        {
            MessageBox.Show(OverlayWindow, message, "SkyDRM DESKTOP");
        }

        private void ReceiveData(int msg, int wParam, string data)
        {
            if (msg == IPCManager.WM_COPYDATA)
            {
                if (string.IsNullOrEmpty(data))
                {
                    return;
                }

                switch (wParam)
                {
                    case IPCManager.WM_PRINT_RESULT:
                        HandlePrintResult(data);
                        break;

                    case IPCManager.WM_DECRYPTED_RESULT:
                        //cache json string 
                        ResultJson = data;
                        InitViewer();
                        break;

                    case IPCManager.WM_HAS_NO_RIGHTS:
                        mStatus = StatusOfView.ERROR_NOT_AUTHORIZED;
                        DispatchErrorUI(mStatus, string.Empty);
                        SetWindowTitle(data);
                        break;

                    case IPCManager.WM_DOWNLOAD_FAILED:
                        mStatus = StatusOfView.DOWNLOAD_FAILED;
                        DispatchErrorUI(mStatus, string.Empty);
                        SetWindowTitle(data);
                        break;

                    case IPCManager.WM_UNSUPPORTED_FILE_TYPE:
                        mStatus = StatusOfView.FILE_TYPE_NOT_SUPPORT;
                        DispatchErrorUI(mStatus, string.Empty);
                        SetWindowTitle(data);
                        break;

                    case IPCManager.WM_HIDE_VIEWER:
                        HideViewer();
                        break;
                }
            }
        }

        private void SetWindowTitle(string fileName)
        {
            this.Title = "SkyDRM Viewer -  " + fileName;
            this.fileName.Text = fileName;
            this.fileName.ToolTip = fileName;
        }

        // Parse the result json string.
        private NxlConverterResult ParseJson(string json)
        {
            ViewerApp.Log.Info("\t\t ParseJson \r\n");
            return JsonConvert.DeserializeObject<NxlConverterResult>(json);           
        }

        private void DynamicChangeUI(EnumFileType fileType)
        {
            switch (fileType)
            {
                case EnumFileType.FILE_TYPE_HOOPS_3D:
                    break;

                case EnumFileType.FILE_TYPE_OFFICE:
                    break;

                case EnumFileType.FILE_TYPE_PDF:

                    break;

                case EnumFileType.FILE_TYPE_3D_PDF:
                    break;

                case EnumFileType.FILE_TYPE_IMAGE:
                    this.RotateStackPanel.Visibility = Visibility.Visible;
                    this.VerticalSeperateLine.Visibility = Visibility.Visible;

                    break;

                case EnumFileType.FILE_TYPE_PLAIN_TEXT:
                    break;

                case EnumFileType.FILE_TYPE_VIDEO:
                    this.Print.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_AUDIO:
                    this.Print.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_NOT_SUPPORT:
                    mStatus = StatusOfView.FILE_TYPE_NOT_SUPPORT;
                    DispatchErrorUI(mStatus, string.Empty);
                    break;
            }
        }

        /// <summary>
        /// May need a load progress when load a complex view such as a big 3d file.
        /// </summary>
        private void LoadRenderViewer(EnumFileType fileType, NxlConverterResult nxlConverterResult)
        {
            DynamicChangeUI(fileType);
            ViewerApp.Log.InfoFormat("\t\t fileType: {0} \r\n", fileType);
            switch (fileType)
            {
                case EnumFileType.FILE_TYPE_HOOPS_3D:
                case EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D:
                
                    Viewer = new ThreeDViewer(this, nxlConverterResult.TmpPath, WatermarkInfo);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                case EnumFileType.FILE_TYPE_IMAGE:
                    Viewer = new ImageViewer(this, nxlConverterResult, WatermarkInfo);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                case EnumFileType.FILE_TYPE_PLAIN_TEXT:
                    Viewer = new RichTextBoxViewer(this, nxlConverterResult, WatermarkInfo);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                case EnumFileType.FILE_TYPE_AUDIO:
                case EnumFileType.FILE_TYPE_VIDEO:
                    Viewer = new AvViewer(this, nxlConverterResult, fileType, WatermarkInfo);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                case EnumFileType.FILE_TYPE_PDF:

                    // Judge pdf if is 2d or 3d
                    PdfAnalyzer pdfAnalyzer = new PdfAnalyzer(nxlConverterResult.TmpPath);
                    pdfAnalyzer.Analyzer((bool bIs3Dpdf) =>
                    {
                        if (bIs3Dpdf) // 3d pdf
                        {
                            ViewerApp.Log.Info("\t\t Is3Dpdf \r\n");

                            ///
                            /// Now can directly render 3d pdf using exchange.
                            ///
							CurrentFileType = EnumFileType.FILE_TYPE_3D_PDF;
                            Viewer = new ThreeDViewer(this, nxlConverterResult.TmpPath, WatermarkInfo);
                            this.Viewer_Content.Content = Viewer;
                            DismissLoadinBar();
                        }
                        else // 2d pdf
                        {
                            ViewerApp.Log.Info("\t\t Is 2D pdf \r\n");
                            CurrentFileType = EnumFileType.FILE_TYPE_PDF;
                            DynamicChangeUI(CurrentFileType);
                            Viewer = new PreviewHandlerPage(this, nxlConverterResult, OverlayWindow);
                            this.Viewer_Content.Content = Viewer;
                            DismissLoadinBar();
                        }
                    });
                    break;

                case EnumFileType.FILE_TYPE_OFFICE:
                    Viewer = new PreviewHandlerPage(this, nxlConverterResult, OverlayWindow);
                    this.Viewer_Content.Content = Viewer;
                    //fix file content render not yet done appears blank in short time  
                    //GDI conflict with Direct32
                    this.Viewer_Content.Visibility = Visibility.Hidden;
                    break;

                case EnumFileType.FILE_TYPE_SAP_VDS:
                    this.Print.Visibility = Visibility.Collapsed;
                    Viewer = new SapThreeDViewer(this, nxlConverterResult, WatermarkInfo);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                default:
                    break;
            }        
        }

        private void ErrorUI(string errorMsg)
        {
            LoadingBar.Visibility = Visibility.Collapsed;

            PromptInfo_Containe.Visibility = Visibility.Visible;

            TB_PromptInfo.Text = errorMsg;

            this.Print.Visibility = Visibility.Collapsed;
            this.FileInfoBtn.Visibility = Visibility.Collapsed;
            this.ShareFile.Visibility = Visibility.Collapsed;
            this.EditBtn.Visibility = Visibility.Collapsed;
            this.ExportBtn.Visibility = Visibility.Collapsed;
            this.Extract_Content_Btn.Visibility = Visibility.Collapsed;
        }

        private bool CanBeView(NxlConverterResult nxlConverterResult, out StatusOfView status)
        {
            bool result = false;
            if (nxlConverterResult.IsConverterSucceed)
            {
                if (NamedPipesClient.Register(new RegisterInfo(Process.GetCurrentProcess().Id, true)))
                {
                    ViewerApp.Log.Info("\t\t decrypted Succeed \r\n");
                    if (nxlConverterResult.IsByCentrolPolicy)
                    {
                        ViewerApp.Log.Info("\t\t Is CentrolPolicy File \r\n");
                        result = CheckRights(nxlConverterResult, out status);
                    }
                    else
                    {
                        ViewerApp.Log.Info("\t\t Is AdHoc File \r\n");
                        if (nxlConverterResult.IsOwner)
                        {
                            ViewerApp.Log.Info("\t\t Is owner: true \r\n");
                            status = StatusOfView.NORMAL;
                            result = true;
                        }
                        else
                        {
                            ViewerApp.Log.Info("\t\t Is owner: false \r\n");
                            result = CheckRights(nxlConverterResult, out status);
                        }
                    }

                }
                else
                {
                    status = StatusOfView.SYSTEM_INTERNAL_ERROR;
                }
            }
            else
            {
                ViewerApp.Log.Info("\t\t decrypt failed \r\n");
                status = StatusOfView.ERROR_DECRYPTFAILED;
            }
            return result;
        }

        private bool CheckRights(NxlConverterResult nxlConverterResult, out StatusOfView statusOfView)
        {
            bool result = false;

            if (null == nxlConverterResult.Rights
                               || nxlConverterResult.Rights.Length == 0
                               || !(nxlConverterResult.Rights.Contains(FileRights.RIGHT_VIEW)))
            {
                ViewerApp.Log.Info("\t\t Rights == null Or Rights.Length == 0 \r\n");
                statusOfView = StatusOfView.ERROR_NOT_AUTHORIZED;
            }
            else if (nxlConverterResult.Expiration.type != ExpiryType.NEVER_EXPIRE
                                                    &&
                CommonUtils.DateTimeToTimestamp(DateTime.Now) > nxlConverterResult.Expiration.End)
            {
                ViewerApp.Log.Info("\t\t File is expire \r\n");
                statusOfView = StatusOfView.ERROR_NOT_AUTHORIZED;
            }
            else
            {
                statusOfView = StatusOfView.NORMAL;
                result = true;
            }
            return result;
        }


        private void DispatchErrorUI(StatusOfView status, string message)
        {
            ViewerApp.Log.InfoFormat("\t\t Display UI for some Error,StatusOfView: {0}, message: {1} \r\n", status, message);
            switch (status)
            {
                case StatusOfView.ERROR_DECRYPTFAILED:
                    if (string.IsNullOrEmpty(message))
                    {
                        ErrorUI(CultureStringInfo.VIEW_DLGBOX_DETAILS_DECRYPTFAILED);
                    }
                    else
                    {
                        ErrorUI(message);
                    }
                    break;

                case StatusOfView.ERROR_NOT_AUTHORIZED:
                    ErrorUI(CultureStringInfo.VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED);
                    break;

                case StatusOfView.SYSTEM_INTERNAL_ERROR:
                    ErrorUI(CultureStringInfo.VIEW_DLGBOX_DETAILS_SYSTEM_INTERNAL_ERROR);
                    break;

                case StatusOfView.DOWNLOAD_FAILED:
                    ErrorUI(CultureStringInfo.VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED);
                    break;
                case StatusOfView.FILE_TYPE_NOT_SUPPORT:
                    ErrorUI(CultureStringInfo.VIEW_DLGBOX_DETAILS_NOTSUPPORT);
                    break;
            }
        }

        private void InitUI(NxlConverterResult nxlConverterResult)
        {
            ViewerApp.Log.Info("\t\t Init UI \r\n");
     
            this.fileName.Text = nxlConverterResult.FileName;
            this.fileName.ToolTip = nxlConverterResult.FileName;

            this.Title = "SkyDRM Viewer -  " + nxlConverterResult.FileName;

            this.FileInfoBtn.Visibility = Visibility.Visible;

            this.EditBtn.Visibility = nxlConverterResult.IsDisplayEditButton ? Visibility.Visible : Visibility.Collapsed;

            this.Print.Visibility = nxlConverterResult.IsDisplayPrintButton ? Visibility.Visible : Visibility.Collapsed;

            this.ShareFile.Visibility = nxlConverterResult.IsDisplayShareButton ? Visibility.Visible : Visibility.Collapsed;

            this.ExportBtn.Visibility = nxlConverterResult.IsDisplaySaveAsButton ? Visibility.Visible : Visibility.Collapsed;

            this.Extract_Content_Btn.Visibility = nxlConverterResult.IsDisplayExtractButton ? Visibility.Visible : Visibility.Collapsed;

        }

        private void LockFile(string nxlDiskPath)
        {
            ViewerApp.Log.InfoFormat("\t\t LockedFile Path :{0} \r\n", nxlDiskPath);
            try
            {
                if (!string.IsNullOrEmpty(nxlDiskPath))
                {
                    NxlFileStream = new FileStream(nxlDiskPath,
                      FileMode.Open,
                      FileAccess.Read,
                      FileShare.ReadWrite, 4096,
                      FileOptions.None);
                }
            }
            catch (Exception ex)
            {
                ViewerApp.Log.Error("\t\t Some error happend on lockedFile {0} \r\n", ex);
            }
        }

        private void UnlockFile(FileStream nxlFileStream)
        {
            try
            {
                if (null != nxlFileStream)
                {
                    ViewerApp.Log.InfoFormat("\t\t UnlockedFile Path :{0} \r\n", nxlFileStream.Name);
                    nxlFileStream.Close();
                }
            }
            catch (Exception ex)
            {
                ViewerApp.Log.InfoFormat("\t\t Some error happend UnlockedFile {0} \r\n", ex );
            }
        }

        private void InitViewer()
        {
            try
            {
                ViewerApp.Log.Info("\t\t InitViewer \r\n");

                NxlConverterResult = ParseJson(ResultJson);

                ViewerApp.Log.InfoFormat("\t\t Start render file:{0} \r\n", NxlConverterResult.TmpPath);

                LockFile(NxlConverterResult.LocalDiskPath);
  
                InitUI(NxlConverterResult);

                if (CanBeView(NxlConverterResult, out mStatus))
                {
                    WatermarkInfo.Builder builder = new WatermarkInfo.Builder();
                    this.WatermarkInfo = builder.DefaultSet(NxlConverterResult.AdhocWaterMark, NxlConverterResult.UserEmail).Build();

                    CurrentFileType = RenderHelper.GetFileTypeByExtension(NxlConverterResult.TmpPath);

                    InitOverlayWindow(this, this.WatermarkInfo);

                    LoadRenderViewer(CurrentFileType, NxlConverterResult);
                }
                else
                {
                    DispatchErrorUI(mStatus, NxlConverterResult.ErrorMsg);
                }
            }
            catch (Exception ex)
            {
                mStatus = StatusOfView.SYSTEM_INTERNAL_ERROR;
                DispatchErrorUI(mStatus, string.Empty);
            }
        }

        private void Init()
        {
            ViewerApp.Log.Info("\t\t Init \r\n");

            // fix after open office file from extern cannot hide viewer 
            // Register hook that use to receive info
            ViewerApp.Log.Info("\t\t AddHook Ipc.WndProc \r\n");
            (PresentationSource.FromVisual(this) as HwndSource).AddHook(new HwndSourceHook(Ipc.WndProc));

            if (!string.IsNullOrEmpty(ViewerApp.ViewerInstance.ReceiveData))
            {
                ViewerApp.Log.Info("\t\t Data get ready\r\n");
                ResultJson = ViewerApp.ViewerInstance.ReceiveData;

                InitViewer();
            }
            else
            {
                ViewerApp.Log.Info("\t\t Data not get ready \r\n");
          
                HMainWin = new IntPtr(ViewerApp.ViewerInstance.IntPtr);

                WindowInteropHelper wndHelper = new WindowInteropHelper(this);

                Hwnd = wndHelper.Handle;

                string jsonstr = JsonConvert.SerializeObject(ViewerApp.ViewerInstance.viewToAppParams);

                Ipc.SendData(HMainWin, IPCManager.WM_COPYDATA, IPCManager.WM_VIEWER_WINDOW_LOADED, jsonstr);
                ViewerApp.Log.Info("\t\t Tell SkydrmApp , start decrypt... \r\n");

                ViewerApp viewerApp = ViewerApp.ViewerInstance;
                bool isCanAccelerate = false;
                string extension = string.Empty;
                string nxlFilePath = viewerApp.viewToAppParams.SecretSignal;

                nxlFilePath = Path.GetFileNameWithoutExtension(nxlFilePath);

                EnumFileType enumFileType = RenderHelper.GetFileTypeByExtension(nxlFilePath);

                switch (enumFileType)
                {
                    case EnumFileType.FILE_TYPE_OFFICE:
                        isCanAccelerate = true;
                        break;

                    case EnumFileType.FILE_TYPE_PDF:
                        isCanAccelerate = true;
                        break;
                }

                if (isCanAccelerate)
                {
                    ViewerApp.Log.Info("\t\t This file can accelerate render\r\n");
                    extension = Path.GetExtension(nxlFilePath);

                    AcceleratePreview.PreInitializePreviewCaller(extension);
                }
            }
        }

      

        private void Dispose()
        {
            UnlockFile(NxlFileStream);

            SaveCurrentWindowState();

            if (OverlayWindow != null)
            {
                OverlayWindow.Close();
            }
            if ((null == Viewer) && (Viewer is PreviewHandlerPage))
            {
                AcceleratePreview.Dispose();
            }

            try
            {
                if (!string.IsNullOrEmpty(ConvertedOutPath) && File.Exists(ConvertedOutPath))
                {
                    File.Delete(ConvertedOutPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && mStatus != StatusOfView.NORMAL)
            {
                this.Close();
            }
        }

        private void Extract_Content_Btn_Click(object sender, RoutedEventArgs e)
        {
            CommunicationWithSkydrmLocal.ExtractContent(NxlConverterResult.LocalDiskPath, this);
        }

        #endregion Private

        #region Public

        public ViewerWindow()
        {
            InitializeComponent();

            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                DpiX = graphics.DpiX;
                DpiY = graphics.DpiY;
            }

            InitWindowState();

            Application.Current.MainWindow = this;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            Ipc = new IPCManager(new Action<int, int, string>(ReceiveData));

            this.Loaded += new RoutedEventHandler(Window_Loaded);

            CloseEventHandler = new EventHandler(Window_Closed);
            ClosingEventHandler = new CancelEventHandler(CancelEventHandler);

            this.Closed += CloseEventHandler;
            this.Closing += ClosingEventHandler;

            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);

            // init network status
            IsNetworkAvailable = NetworkStatus.IsAvailable;
        }

        public void MoveOverlayWindow()
        {
            if (null != OverlayWindow)
            {
                System.Windows.Point position = Viewer_Content.TranslatePoint(new System.Windows.Point(0d, 0d), this);

                position = this.PointToScreen(position);

                IntPtr OverLay_hwnd = new WindowInteropHelper(OverlayWindow).Handle;
                if (null != OverLay_hwnd)
                {
                    int offsets = 0;
                    if (CurrentFileType == EnumFileType.FILE_TYPE_PDF)
                        offsets = 35;

                    int width = Convert.ToInt32(Math.Round(Viewer_Content.ActualWidth * (DeviceIndependentUnit * DpiX)));

                    int height = Convert.ToInt32(Math.Round(Viewer_Content.ActualHeight * (DeviceIndependentUnit * DpiY))) - offsets;

                    MoveWindow((int)OverLay_hwnd, Convert.ToInt32(Math.Round(position.X)), Convert.ToInt32(Math.Round(position.Y)),
                    width, height, false);
                }
            }
        }

        public void HandleOfficeFileRenderException()
        {
            DismissLoadinBar();
            this.Print.Visibility = Visibility.Collapsed;
            this.FileInfoBtn.Visibility = Visibility.Collapsed;
            this.ShareFile.Visibility = Visibility.Collapsed;
            this.EditBtn.Visibility = Visibility.Collapsed;
            this.ExportBtn.Visibility = Visibility.Collapsed;
            this.Extract_Content_Btn.Visibility = Visibility.Collapsed;
            mStatus = StatusOfView.ERROR_DECRYPTFAILED;
        }

        public void HandlerException(StatusOfView status, string message)
        {
            PromptInfo_Containe.Visibility = Visibility.Visible;
            TB_PromptInfo.Text = message;
            this.Print.Visibility = Visibility.Collapsed;
            this.FileInfoBtn.Visibility = Visibility.Collapsed;
            this.ShareFile.Visibility = Visibility.Collapsed;
            this.EditBtn.Visibility = Visibility.Collapsed;
            this.RotateStackPanel.Visibility = Visibility.Hidden;
            this.VerticalSeperateLine.Visibility = Visibility.Hidden;
            this.ExportBtn.Visibility = Visibility.Collapsed;
            this.Extract_Content_Btn.Visibility = Visibility.Collapsed;
            this.mStatus = status;
        }

        public void CancelEventHandler(object sender, CancelEventArgs e)
        {
            e.Cancel = InterruptingCloseWindow;
        }

        public void DismissLoadinBar()
        {
            LoadingBar.Visibility = Visibility.Collapsed;
        }

        #endregion Public 
    }
}
