using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Viewer.render;
using Viewer.render.av.AvViewer;
using Viewer.render.hoops.ThreeDView;
using Viewer.render.RichMedia;
using Viewer.utils;
using Viewer.viewer;
using Viewer.overlay;
using System.ComponentModel;
using System.Windows.Input;
using System.IO;
using Viewer.render.preview;
using static Viewer.utils.NetworkStatus;
using System.Windows.Threading;
using Viewer.render.sap3dviewer;
using static Viewer.ViewerApp.IntentParser;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.drive;
using Viewer.removeProtection;
using System.Collections.ObjectModel;
using CustomControls.windows.fileInfo.view;
using System.Runtime.InteropServices;
using Viewer.edit;
using Viewer.share;
using Viewer.database;
using Viewer.export;
using static Viewer.utils.IPC.NamedPipe;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Generic;
using static Viewer.utils.IPC.NamedPipe.NamedPipeClient;

namespace Viewer
{
    public partial class ViewerWindow
    {
        #region Private

        private ViewerApp mViewerInstance = null;

        private string mFilePath = null;

        private string mBackupFilePath = null;

        private NxlFileFingerPrint mNxlFileFingerPrint;

        private StatusOfView mStatus = StatusOfView.NORMAL;

        private EnumFileType mCurrentFileType = EnumFileType.FILE_TYPE_NOT_SUPPORT;

        private WatermarkInfo mWatermarkInfo = null;

        private bool mCanAccelerate = false;

        private IntPtr HwndViewerWindow;

        private object Viewer;

        private FileStream NxlFileStream;

        private bool isNetworkAvailable;

        private bool InterruptingCloseWindow = false;

        private IPCManager IPCManager;

        private log4net.ILog mLog;

        private string mWatermarkStr;

        #endregion Private

        #region Public

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsNetworkAvailable
        {
            get { return isNetworkAvailable; }
            set
            {
                isNetworkAvailable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsNetworkAvailable"));
            }
        }

        public StatusOfView Status
        {
            get
            {
                return mStatus;
            }
        }

        public EnumFileType CurrentFileType
        {
            get
            {
                return mCurrentFileType;
            }
        }

        #endregion Public

        #region Constructor
        public ViewerWindow()
        {
            InitializeComponent();
            InitWindowState();
            Application.Current.MainWindow = this;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            this.mViewerInstance = (ViewerApp)Application.Current;
            this.mLog = mViewerInstance.Log;
            IPCManager = new IPCManager(new Action<int, int, string>(ReceiveData));

            Loaded += new RoutedEventHandler(Window_Loaded);
            Closed += new EventHandler(Window_Closed);
            Closing += new CancelEventHandler(CancelEventHandler);

            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);

            // init network status
            IsNetworkAvailable = NetworkStatus.IsAvailable;
        }

        #endregion Constructor

        #region Private F

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

        private bool CheckExpired(NxlFileFingerPrint fp)
        {
            bool result = false;
            if (fp.expiration.type != SkydrmLocal.rmc.sdk.ExpiryType.NEVER_EXPIRE
                                    &&
            CommonUtils.DateTimeToTimestamp(DateTime.Now) > fp.expiration.End)
            {
                mViewerInstance.Log.Info("\t\t File is expire \r\n");
                result = true;
            }
            return result;
        }

        private bool CheckCanbeView(NxlFileFingerPrint fp, ref StatusOfView statusOfView)
        {
            bool result = true;

            if (fp.isByCentrolPolicy)
            {
                mViewerInstance.Log.Info("\t\t CentrolPolicy File \r\n");

                if (!fp.HasRight(FileRights.RIGHT_VIEW))
                {
                    statusOfView = StatusOfView.ERROR_NOT_AUTHORIZED;
                    result = false;
                }
                if (CheckExpired(fp))
                {
                    statusOfView = StatusOfView.FILE_HAS_EXPIRED;
                    result = false;
                }
            }
            else
            {
                mViewerInstance.Log.Info("\t\t AdHoc File \r\n");
                if (fp.isOwner)
                {
                    mViewerInstance.Log.Info("\t\t Is Owner: true \r\n");
                    statusOfView = StatusOfView.NORMAL;
                }
                else
                {
                    mViewerInstance.Log.Info("\t\t Is Owner: false \r\n");

                    if (!fp.HasRight(FileRights.RIGHT_VIEW))
                    {
                        statusOfView = StatusOfView.ERROR_NOT_AUTHORIZED;
                        result = false;
                    }
                    if (CheckExpired(fp))
                    {
                        statusOfView = StatusOfView.FILE_HAS_EXPIRED;
                        result = false;
                    }
                }
            }
            return result;
        }

        private bool CheckCanAccelerate(EnumFileType currentFileType)
        {
            bool result = false;
            switch (currentFileType)
            {
                case EnumFileType.FILE_TYPE_OFFICE:
                    result = true;
                    break;

                case EnumFileType.FILE_TYPE_PDF:
                    result = true;
                    break;
            }
            return result;
        }

        private void DumpFileRightsToLog(SkydrmLocal.rmc.sdk.FileRights[] fileRights, log4net.ILog log)
        {
            log.Info("\t\t Dump File Rights To Log \r\n");
            foreach (FileRights rights in fileRights)
            {
                log.Info("\t\t " + rights.ToString() + "\r\n");
            }
        }


        private void LoadFileInNormalDirectory(ref StatusOfView status, ref EnumFileType currentFileType, out WatermarkInfo watermarkInfo, out bool canAccelerate)
        {
            status = StatusOfView.NORMAL;
            canAccelerate = false;
            this.Btn_Print.Visibility = Visibility.Collapsed;
            this.Btn_FileInfo.Visibility = Visibility.Collapsed;
            this.Btn_ShareFile.Visibility = Visibility.Collapsed;
            this.Btn_Edit.Visibility = Visibility.Collapsed;
            this.Btn_Export.Visibility = Visibility.Collapsed;
            this.Btn_Extract_Content.Visibility = Visibility.Collapsed;
            this.RotateStackPanel.Visibility = Visibility.Collapsed;
            this.VerticalSeperateLine.Visibility = Visibility.Collapsed;
            currentFileType = RenderHelper.GetFileTypeByExtension(mFilePath, mViewerInstance.Log);
            WatermarkInfo.Builder builder = new WatermarkInfo.Builder();
            watermarkInfo = builder.DefaultSet(string.Empty, mViewerInstance.User.Email, mViewerInstance.Log).Build();
            LoadingRenderingDevice(currentFileType, mFilePath, watermarkInfo, canAccelerate, mViewerInstance.Log);
        }

        private void Init(out NxlFileFingerPrint nxlFileFingerPrint, ref StatusOfView status, ref EnumFileType currentFileType, out WatermarkInfo watermarkInfo, out bool canAccelerate)
        {
            nxlFileFingerPrint = new NxlFileFingerPrint();
            watermarkInfo = null;
            canAccelerate = false;

            mFilePath = mViewerInstance.Intent.FilePath;

            this.fileName.Text = Path.GetFileName(mFilePath);
            this.Title = "SkyDRM Viewer -  " + Path.GetFileName(mFilePath);

            switch (mViewerInstance.Intent.Intent)
            {
                case EnumIntent.View:

                    try
                    {
                        if (!mViewerInstance.IsNxlFile)
                        {
                            LoadFileInNormalDirectory(ref status, ref currentFileType,out watermarkInfo,out canAccelerate);
                        }
                        else
                        {
                            if (mViewerInstance.Dirstatus == ViewerApp.RPM_SAFEDIRRELATION_SAFE_DIR || mViewerInstance.Dirstatus == ViewerApp.RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR)
                            {
                                LoadFileInNormalDirectory(ref status, ref currentFileType, out watermarkInfo, out canAccelerate);
                            }
                            else
                            {
                                nxlFileFingerPrint = mViewerInstance.User.GetNxlFileFingerPrint(mFilePath);

                                DumpFileRightsToLog(nxlFileFingerPrint.rights, mViewerInstance.Log);

                                if (CheckCanbeView(mNxlFileFingerPrint, ref status))
                                {
                                    mWatermarkStr = string.Empty;
                                    if (nxlFileFingerPrint.isByCentrolPolicy)
                                    {
                                        Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;
                                        try
                                        {
                                             mViewerInstance.User.EvaulateNxlFileRights(mFilePath, out rightsAndWatermarks);
                                             foreach (var v in rightsAndWatermarks)
                                             {
                                                List <WaterMarkInfo> waterMarkInfoList = v.Value;
                                                if (waterMarkInfoList == null)
                                                {
                                                    continue;
                                                }
                                                foreach (var w in waterMarkInfoList)
                                                {
                                                    mWatermarkStr = w.text;
                                                    if (!string.IsNullOrEmpty(mWatermarkStr))
                                                    {
                                                        break;
                                                    }
                                                }
                                                if (!string.IsNullOrEmpty(mWatermarkStr))
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                    }
                                    else
                                    {
                                        mWatermarkStr = nxlFileFingerPrint.adhocWatermark;
                                    }

                                    WatermarkInfo.Builder builder = new WatermarkInfo.Builder();
                                    watermarkInfo = builder.DefaultSet(mWatermarkStr, mViewerInstance.User.Email, mViewerInstance.Log).Build();

                                    currentFileType = RenderHelper.GetFileTypeByExtension(Path.GetFileNameWithoutExtension(mFilePath), mViewerInstance.Log);
                                    canAccelerate = CheckCanAccelerate(currentFileType);
                                    mViewerInstance.RPMFilePath = RightsManagementService.GenerateDecryptFilePath(
                                                             mViewerInstance.Appconfig.RPM_FolderPath,
                                                             mFilePath,
                                                             true
                                                             );

                                    if (canAccelerate)
                                    {
                                        mViewerInstance.Log.Info("\t\t This file can accelerate render\r\n");
                                        AcceleratedRenderingDevice.Start(mFilePath, mViewerInstance.RPMFilePath , mViewerInstance.User, mViewerInstance.Session, mViewerInstance.Log);
                                    }
                                    else
                                    {
                                        RightsManagementService.DecryptNXLFile(mViewerInstance.User,
                                                                               mViewerInstance.Log,
                                                                               mFilePath,
                                                                               mViewerInstance.RPMFilePath
                                                                               );
                                    }

                                    LoadingRenderingDevice(currentFileType, mViewerInstance.RPMFilePath, watermarkInfo, canAccelerate, mViewerInstance.Log);
                                    if (EnumFileType.FILE_TYPE_NOT_SUPPORT != currentFileType && mStatus == StatusOfView.NORMAL)
                                    {
                                        UpdateLayout(mNxlFileFingerPrint);
                                    }
                                    SendLog(mFilePath, NxlOpLog.View, true);
                                }
                                else
                                {
                                    UpdateLayout(status);
                                }
                            }
                        }
                    }
                    catch (RmSdkException e)
                    {
                        status = StatusOfView.ERROR_NOT_AUTHORIZED;
                        UpdateLayout(status);
                        mViewerInstance.Log.Error(e);
                    }
                    catch (Exception e)
                    {
                        status = StatusOfView.SYSTEM_INTERNAL_ERROR;
                        UpdateLayout(status);
                        mViewerInstance.Log.Error(e);
                    }
            break;
                default:
                    break;
            }
        }

        private void SendLog(string nxlFilePath, NxlOpLog nxlOpLog, bool isAllow)
        {
            try
            {
                mLog.Info("\t\t SendLog \r\n");
                mViewerInstance.User.AddLog(nxlFilePath, nxlOpLog, isAllow);
            }
            catch (Exception ex)
            {
                mLog.Error(ex);
            }
        }


        private void LoadingRenderingDevice(EnumFileType fileType, string filePath, WatermarkInfo watermarkInfo, bool isCanAccelerate, log4net.ILog log)
        {
            mViewerInstance.Log.InfoFormat("\t\t fileType: {0} \r\n", fileType);
            UpdateLayout(fileType);
            switch (fileType)
            {
                case EnumFileType.FILE_TYPE_HOOPS_3D:
                    Viewer = new ThreeDViewer(this, filePath, watermarkInfo, log);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                case EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D:

                    string targetFilePath = filePath;
     
                        if (mViewerInstance.IsNxlFile)
                        {
                            if (mViewerInstance.Dirstatus == ViewerApp.RPM_SAFEDIRRELATION_SAFE_DIR || mViewerInstance.Dirstatus == ViewerApp.RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR)
                            {
                                if (targetFilePath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    targetFilePath = mFilePath.Remove(mFilePath.Length - 4, 4);
                                }
                                Viewer = new ThreeDViewer(this, targetFilePath, watermarkInfo, log);
                                this.Viewer_Content.Content = Viewer; 
                                mCurrentFileType = EnumFileType.FILE_TYPE_ASSEMBLY;
                                DismissLoadinBar();
                            }
                            else
                            {
                                string ext = Path.GetExtension(targetFilePath);
                                if ((string.Equals(ext, ".prt", StringComparison.CurrentCultureIgnoreCase)) ||
                                    (string.Equals(ext, ".asm", StringComparison.CurrentCultureIgnoreCase)) ||
                                    (string.Equals(ext, ".sldasm", StringComparison.CurrentCultureIgnoreCase)) ||
                                    (string.Equals(ext, ".asm", StringComparison.CurrentCultureIgnoreCase)) ||
                                    (string.Equals(ext, ".jt", StringComparison.CurrentCultureIgnoreCase)) ||
                                    (string.Equals(ext, ".sldprt", StringComparison.CurrentCultureIgnoreCase))||
                                    (string.Equals(ext, ".iam", StringComparison.CurrentCultureIgnoreCase))||
                                    (string.Equals(ext, ".catproduct", StringComparison.CurrentCultureIgnoreCase)))
                                {
                                        if (string.Equals(ext, ".iam", StringComparison.CurrentCultureIgnoreCase) || 
                                            string.Equals(ext, ".catproduct", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            string directory = Path.GetDirectoryName(mFilePath);
                                            if (ToolKits.IsSystemFolderPath(directory) || ToolKits.IsSpecialFolderPath(directory))
                                            {
                                                CommonUtils.ShowBalloonTip(string.Format(CultureStringInfo.Cannot_Set_RpmFolder_Under_System_Directory, directory), false);
                                                Viewer = new ThreeDViewer(this, targetFilePath, watermarkInfo, log);
                                                this.Viewer_Content.Content = Viewer;
                                                DismissLoadinBar();
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(mFilePath));
                                                    FileInfo file = new FileInfo(mFilePath);
                                                    file.CopyTo(tempFilePath, true);
                                                    mBackupFilePath = tempFilePath;
                                                    mViewerInstance.Session.RPM_AddDir(Path.GetDirectoryName(mFilePath));
                                                    mViewerInstance.TempRpmFolder = Path.GetDirectoryName(mFilePath);
                                                    // targetFilePath = Path.Combine(mViewerInstance.TempRpmFolder, Path.GetFileNameWithoutExtension(mFilePath)) + ".nxl";
                                                    targetFilePath = Path.Combine(mViewerInstance.TempRpmFolder, Path.GetFileNameWithoutExtension(mFilePath));
                                                    WIN32_FIND_DATA pNextInfo;
                                                    RightsManagementService.FindFirstFile(mViewerInstance.TempRpmFolder, out pNextInfo);

                                                    FileStream fileStream = File.Open(targetFilePath + ".nxl", FileMode.Open);
                                                    fileStream.Close();

                                                    Viewer = new ThreeDViewer(this, targetFilePath, watermarkInfo, log);
                                                    this.Viewer_Content.Content = Viewer;
                                                    mCurrentFileType = EnumFileType.FILE_TYPE_ASSEMBLY;
                                                    DismissLoadinBar();

                                                }
                                                catch (Exception ex)
                                                {
                                                    mStatus = StatusOfView.SYSTEM_INTERNAL_ERROR;
                                                    UpdateLayout(mStatus);
                                                    mViewerInstance.Log.Error(ex);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            List<string> paths = new List<string>();
                                            List<string> missingpaths = new List<string>();
                                            UInt32 missingCounts;
                                            mViewerInstance.User.GetAssmblyPathsFromModelFile(filePath, out paths, out missingpaths, out missingCounts);
                                            //string originalFilePath = mFilePath.Remove(mFilePath.Length - 4, 4);
                                            if (missingpaths.Count > 0 || paths.Count > 0 || missingCounts > 1)
                                            {
                                                string directory = Path.GetDirectoryName(mFilePath);
                                                if (ToolKits.IsSystemFolderPath(directory) || ToolKits.IsSpecialFolderPath(directory))
                                                {
                                                    CommonUtils.ShowBalloonTip(string.Format(CultureStringInfo.Cannot_Set_RpmFolder_Under_System_Directory, directory), false);
                                                    Viewer = new ThreeDViewer(this, targetFilePath, watermarkInfo, log);
                                                    this.Viewer_Content.Content = Viewer;
                                                    DismissLoadinBar();
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(mFilePath));
                                                        FileInfo file = new FileInfo(mFilePath);
                                                        file.CopyTo(tempFilePath, true);
                                                        mBackupFilePath = tempFilePath;
                                                        mViewerInstance.Session.RPM_AddDir(Path.GetDirectoryName(mFilePath));
                                                        mViewerInstance.TempRpmFolder = Path.GetDirectoryName(mFilePath);
                                                        // targetFilePath = Path.Combine(mViewerInstance.TempRpmFolder, Path.GetFileNameWithoutExtension(mFilePath)) + ".nxl";
                                                        targetFilePath = Path.Combine(mViewerInstance.TempRpmFolder, Path.GetFileNameWithoutExtension(mFilePath));
                                                        WIN32_FIND_DATA pNextInfo;
                                                        RightsManagementService.FindFirstFile(mViewerInstance.TempRpmFolder, out pNextInfo);

                                                        FileStream fileStream = File.Open(targetFilePath + ".nxl", FileMode.Open);
                                                        fileStream.Close();

                                                        Viewer = new ThreeDViewer(this, targetFilePath, watermarkInfo, log);
                                                        this.Viewer_Content.Content = Viewer;
                                                        mCurrentFileType = EnumFileType.FILE_TYPE_ASSEMBLY;
                                                        DismissLoadinBar();

                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        mStatus = StatusOfView.SYSTEM_INTERNAL_ERROR;
                                                        UpdateLayout(mStatus);
                                                        mViewerInstance.Log.Error(ex);
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                Viewer = new ThreeDViewer(this, targetFilePath, watermarkInfo, log);
                                                this.Viewer_Content.Content = Viewer;
                                                DismissLoadinBar();
                                            }
                                        }
                                }
                                else
                                {
                                    Viewer = new ThreeDViewer(this, targetFilePath, watermarkInfo, log);
                                    this.Viewer_Content.Content = Viewer;
                                    DismissLoadinBar();
                                }
                            }
                        }
                        else
                        {
                            Viewer = new ThreeDViewer(this, targetFilePath, watermarkInfo, log);
                            this.Viewer_Content.Content = Viewer;
                            mCurrentFileType = EnumFileType.FILE_TYPE_ASSEMBLY;
                            DismissLoadinBar();
                        }

                    break;

                case EnumFileType.FILE_TYPE_IMAGE:
                    Viewer = new ImageViewer(this, filePath, watermarkInfo, log);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                case EnumFileType.FILE_TYPE_PLAIN_TEXT:
                    Viewer = new RichTextBoxViewer(this, filePath, watermarkInfo, log)
                    {
                        Type = EnumFileType.FILE_TYPE_PLAIN_TEXT
                    };
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                case EnumFileType.FILE_TYPE_AUDIO:
                case EnumFileType.FILE_TYPE_VIDEO:
                    Viewer = new AvViewer(this, filePath, fileType, watermarkInfo, log);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;

                case EnumFileType.FILE_TYPE_PDF:

                    // Judge pdf if is 2d or 3d
                    PdfAnalyzer pdfAnalyzer = new PdfAnalyzer(filePath, log);
                    pdfAnalyzer.Analyzer((bool bIs3Dpdf) =>
                    {
                        if (bIs3Dpdf) // 3d pdf
                        {
                            mViewerInstance.Log.Info("\t\t Is3Dpdf \r\n");

                            ///
                            /// Now can directly render 3d pdf using exchange.
                            ///
							mCurrentFileType = EnumFileType.FILE_TYPE_3D_PDF;
                            Viewer = new ThreeDViewer(this, filePath, watermarkInfo, log);
                            this.Viewer_Content.Content = Viewer;
                            DismissLoadinBar();
                        }
                        else // 2d pdf
                        {
                            mViewerInstance.Log.Info("\t\t----- 2D PDF -----\r\n");
                            Console.WriteLine("----2d pdf----");
                            mCurrentFileType = EnumFileType.FILE_TYPE_PDF;
                            Viewer = new PreviewHandlerPage(this, filePath, watermarkInfo, isCanAccelerate, mViewerInstance.Session, mViewerInstance.Log);
                            this.Viewer_Content.Content = Viewer;
                            this.Viewer_Content.Visibility = Visibility.Hidden;
                        }
                    });
                    break;

                case EnumFileType.FILE_TYPE_OFFICE:
                    Viewer = new PreviewHandlerPage(this, filePath, watermarkInfo, isCanAccelerate, mViewerInstance.Session, mViewerInstance.Log);
                    this.Viewer_Content.Content = Viewer;
                    //fix file content render not yet done appears blank in short time  
                    //GDI conflict with Direct32
                    this.Viewer_Content.Visibility = Visibility.Hidden;
                    break;

                case EnumFileType.FILE_TYPE_SAP_VDS:
                    Viewer = new VdsViewer(this, filePath, watermarkInfo, mViewerInstance.Log);
                    this.Viewer_Content.Content = Viewer;
                    DismissLoadinBar();
                    break;
                default:
                    break;
            }
        }

        private void LockFile(string nxlDiskPath)
        {
            try
            {
                mViewerInstance.Log.InfoFormat("\t\t LockedFile Path :{0} \r\n", nxlDiskPath);
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
                mViewerInstance.Log.Error("\t\t Some error happend on lockedFile {0} \r\n", ex);
            }
        }

        private void UnlockFile(FileStream nxlFileStream)
        {
            try
            {
                mViewerInstance.Log.InfoFormat("\t\t UnlockedFile Path :{0} \r\n", nxlFileStream.Name);
                if (null != nxlFileStream)
                {
                    nxlFileStream.Close();
                }
            }
            catch (Exception ex)
            {
                mViewerInstance.Log.InfoFormat("\t\t Some error happend UnlockedFile {0} \r\n", ex);
            }
        }

        private void Dispose()
        {
            //  UnlockFile(NxlFileStream);

            SaveCurrentWindowState();

            if (mCanAccelerate)
            {
                PreviewHandler.Instance().Unload();
            }

            if (!string.IsNullOrEmpty(mBackupFilePath) && File.Exists(mBackupFilePath))
            {
                try
                {
                    File.Delete(mBackupFilePath);
                }
                catch (Exception)
                {

                }
            }

            //try
            //{
            //    if (!string.IsNullOrEmpty(ConvertedOutPath) && File.Exists(ConvertedOutPath))
            //    {
            //        File.Delete(ConvertedOutPath);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
        }

        #region Window Event

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //fix bug 55280
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            HwndViewerWindow = wndHelper.Handle;
            Win32Common.BringWindowToTopEx(HwndViewerWindow);

            // Register hook that use to receive info
            (PresentationSource.FromVisual(this) as HwndSource).AddHook(new HwndSourceHook(IPCManager.WndProc));
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Init(out mNxlFileFingerPrint, ref mStatus, ref mCurrentFileType, out mWatermarkInfo, out mCanAccelerate);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                IsNetworkAvailable = e.IsAvailable;

                if (mStatus == StatusOfView.NORMAL)
                {
                    if (IsNetworkAvailable)
                    {
                        if (!mViewerInstance.IsNxlFile)
                        {
                            return;
                        }
                        UpdateLayout(mNxlFileFingerPrint);
                    }
                }
            }));
        }

        #endregion Window Event

        #region Update UI
        private void UpdateLayout(string errorMsg)
        {
            SendLog(mFilePath, NxlOpLog.View,false);
            LoadingBar.Visibility = Visibility.Collapsed;
            PromptInfo_Containe.Visibility = Visibility.Visible;
            TB_PromptInfo.Text = errorMsg;
            this.Btn_Print.Visibility = Visibility.Collapsed;
            this.Btn_FileInfo.Visibility = Visibility.Collapsed;
            this.Btn_ShareFile.Visibility = Visibility.Collapsed;
            this.Btn_Edit.Visibility = Visibility.Collapsed;
            this.Btn_Export.Visibility = Visibility.Collapsed;
            this.Btn_Extract_Content.Visibility = Visibility.Collapsed;
            this.RotateStackPanel.Visibility = Visibility.Collapsed;
            this.VerticalSeperateLine.Visibility = Visibility.Collapsed;
        }

        private void UpdateLayout(StatusOfView status)
        {
            mViewerInstance.Log.InfoFormat("\t\t UpdateViewerLayout, StatusOfView: {0} \r\n", status);
            switch (status)
            {
                case StatusOfView.ERROR_DECRYPTFAILED:
                    UpdateLayout(CultureStringInfo.VIEW_DLGBOX_DETAILS_DECRYPTFAILED);
                    break;
                case StatusOfView.ERROR_NOT_AUTHORIZED:
                    UpdateLayout(CultureStringInfo.VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED);
                    break;
                case StatusOfView.SYSTEM_INTERNAL_ERROR:
                    UpdateLayout(CultureStringInfo.VIEW_DLGBOX_DETAILS_SYSTEM_INTERNAL_ERROR);
                    break;
                case StatusOfView.DOWNLOAD_FAILED:
                    UpdateLayout(CultureStringInfo.VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED);
                    break;
                case StatusOfView.FILE_TYPE_NOT_SUPPORT:
                    UpdateLayout(CultureStringInfo.VIEW_DLGBOX_DETAILS_NOTSUPPORT);
                    break;
                case StatusOfView.FILE_HAS_EXPIRED:
                    UpdateLayout(CultureStringInfo.VIEW_DLGBOX_DETAILS_FILE_HAS_EXPIRED);
                    break;
            }
        }

        private void UpdateLayout(EnumFileType fileType)
        {
            switch (fileType)
            {
                case EnumFileType.FILE_TYPE_HOOPS_3D:
                    this.Btn_Edit.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_OFFICE:
                    break;
                case EnumFileType.FILE_TYPE_PDF:
                    this.Btn_Edit.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_3D_PDF:
                    this.Btn_Edit.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_IMAGE:
                    this.RotateStackPanel.Visibility = Visibility.Visible;
                    this.VerticalSeperateLine.Visibility = Visibility.Visible;
                    this.Btn_Edit.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_PLAIN_TEXT:
                    this.Btn_Edit.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_VIDEO:
                    this.Btn_Print.Visibility = Visibility.Collapsed;
                    this.Btn_Edit.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_AUDIO:
                    this.Btn_Print.Visibility = Visibility.Collapsed;
                    this.Btn_Edit.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_SAP_VDS:
                    this.Btn_Print.Visibility = Visibility.Collapsed;
                    this.Btn_Edit.Visibility = Visibility.Collapsed;
                    break;

                case EnumFileType.FILE_TYPE_NOT_SUPPORT:
                    mStatus = StatusOfView.FILE_TYPE_NOT_SUPPORT;
                    UpdateLayout(CultureStringInfo.VIEW_DLGBOX_DETAILS_NOTSUPPORT);
                    break;
            }
        }

        private void UpdateLayout(NxlFileFingerPrint fp)
        {
            mViewerInstance.Log.Info("\t\t Update Viewer Layout \r\n");

 

            this.Btn_FileInfo.Visibility = Visibility.Visible;

            new Thread(new ThreadStart(() =>
            {
                if (IsDisplayEditButton(fp))
                {
                    mViewerInstance.Dispatcher.BeginInvoke(new Action(() =>
                    {
                         this.Btn_Edit.Visibility = Visibility.Visible;

                    }), DispatcherPriority.Normal);
                }
                else
                {
                    mViewerInstance.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.Btn_Edit.Visibility = Visibility.Collapsed;

                    }), DispatcherPriority.Normal);
                }
            }))
            {
                Name = "IsDisplayEditButton",
                IsBackground = true,
                Priority = ThreadPriority.Normal
            }.Start();

            this.Btn_Print.Visibility = IsDisplayPrintButton(fp) ? Visibility.Visible : Visibility.Collapsed;

            new Thread(new ThreadStart(() =>
            {
                if (IsDisplayShareButton(fp, IsNetworkAvailable))
                {
                    mViewerInstance.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.Btn_ShareFile.Visibility = Visibility.Visible;

                    }), DispatcherPriority.Normal);
                }
                else
                {
                    mViewerInstance.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.Btn_ShareFile.Visibility = Visibility.Collapsed;
                    }), DispatcherPriority.Normal);
                }
            }))
            {
                Name = "IsDisplayShareButton",
                IsBackground = true,
                Priority = ThreadPriority.Normal
            }.Start();


            new Thread(new ThreadStart(() =>
            {
                if (IsDisplayExportButton(fp, IsNetworkAvailable))
                {
                    mViewerInstance.Dispatcher.BeginInvoke(new Action(() =>
                    {
                            this.Btn_Export.Visibility = Visibility.Visible;

                    }), DispatcherPriority.Normal);
                }
                else
                {
                    mViewerInstance.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.Btn_Export.Visibility = Visibility.Collapsed;

                    }), DispatcherPriority.Normal);
                }
            }))
            {
                Name = "IsDisplayExportButton",
                IsBackground = true,
                Priority = ThreadPriority.Normal
            }.Start();

            this.Btn_Extract_Content.Visibility = IsDisplayExtractButton(fp) ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool IsDisplayShareButton(NxlFileFingerPrint fp, bool isNetworkAvailable)
        {
            bool result = false;
            if (!isNetworkAvailable)
            {
                mViewerInstance.Log.Info("\t\t Network uavailable \r\n");
                return result;
            }

            //if (!fp.isByAdHoc)
            //{
            //    mViewerInstance.Log.Info("\t\t This file is not a AdHoc file \r\n");
            //    return result;
            //}

            if (!fp.HasRight(FileRights.RIGHT_SHARE))
            {
                mViewerInstance.Log.Info("\t\t This file has not share rights \r\n");
                return result;
            }

            if (mViewerInstance.Intent.IsFromMainWindow)
            {
                mViewerInstance.Log.Info("\t\t This file click from nxrmApp \r\n");

                if (mViewerInstance.Intent.AllowShare)
                {
                    mViewerInstance.Log.Info("\t\t  Allow sharing \r\n");
                   
                    if (mNxlFileFingerPrint.isFromMyVault)
                    {
                        mViewerInstance.Log.Info("\t\t This file is FromMyVault \r\n");
                        result = true;
                    }
                    else if (mNxlFileFingerPrint.isFromSystemBucket)
                    {
                        mViewerInstance.Log.Info("\t\t This file isFromSystemBucket \r\n");
                        result = false;
                    }
                    else if (mNxlFileFingerPrint.isFromPorject)
                    {
                        mViewerInstance.Log.Info("\t\t This file isFromPorject \r\n");
                        result = true;
                    }
                }
            }
            else
            {
                mViewerInstance.Log.Info("\t\t This file click from OS folder \r\n");

                if (mNxlFileFingerPrint.isFromMyVault)
                {
                    mViewerInstance.Log.Info("\t\t This file is FromMyVault \r\n");

                    if (mNxlFileFingerPrint.isOwner)
                    {
                        mViewerInstance.Log.Info("\t\t Current user is file owner \r\n");
                        MyVaultFile myVaultFile = mViewerInstance.FunctionProvider.QueryMyVaultFileByDuid(mNxlFileFingerPrint.duid);
                        if (null != myVaultFile)
                        {
                            mViewerInstance.Log.Info("\t\t Has found this file in MyVault File tb \r\n");
                            result = true;
                        }
                        else
                        {
                            mViewerInstance.Log.InfoFormat("\t\t No found the MyVaultFile in DB \r\n", result);
                        }
                    }
                    else
                    {
                        mViewerInstance.Log.Info("\t\t Not file owner \r\n");
                        SharedWithMeFile sharedWithMeFile = mViewerInstance.FunctionProvider.QuerySharedWithMeFileByDuid(mNxlFileFingerPrint.duid);
                        if (null != sharedWithMeFile)
                        {
                            mViewerInstance.Log.Info("\t\t Has found this file in SharedWithMe File tb \r\n");
                            result = true;
                        }
                        else
                        {
                            mViewerInstance.Log.InfoFormat("\t\t No found the sharedWithMe File in DB \r\n", result);
                        }
                    }
                }
                else if (mNxlFileFingerPrint.isFromSystemBucket)
                {
                    mViewerInstance.Log.Info("\t\t This file isFromSystemBucket \r\n");
                    result = false;
                }
                else if (mNxlFileFingerPrint.isFromPorject)
                {
                    mViewerInstance.Log.Info("\t\t This file isFromPorject \r\n");
                    result = false;
                }
            }

            //if (mViewerInstance.Intent.IsFromMainWindow)
            //{
            //    if (mViewerInstance.Intent.AllowShare)
            //    {
            //        if (fp.isByAdHoc && fp.HasRight(FileRights.RIGHT_SHARE) && isNetworkAvailable)
            //        {
            //            if (mNxlFileFingerPrint.isFromMyVault)
            //            {               
            //                result = true;
            //            }
            //            else if (mNxlFileFingerPrint.isFromSystemBucket)
            //            {
            //                result = true;
            //            }
            //            else if (mNxlFileFingerPrint.isFromPorject)
            //            {
            //                result = true;
            //            }
            //        }
            //    }
            //}
            //else
            //{
            //    if (fp.isByAdHoc && fp.HasRight(FileRights.RIGHT_SHARE) && isNetworkAvailable)
            //    {
            //        if (mNxlFileFingerPrint.isFromMyVault)
            //        {
            //            if (mNxlFileFingerPrint.isOwner)
            //            {
            //                MyVaultFile myVaultFile = mViewerInstance.FunctionProvider.QueryMyVaultFileByDuid(mNxlFileFingerPrint.duid);
            //                if (null != myVaultFile) 
            //                {
            //                    result = true;
            //                }
            //                else
            //                {
            //                    mViewerInstance.Log.InfoFormat("\t\t no found the MyVaultFile in DB \r\n", result);
            //                }
            //            }
            //            else
            //            {
            //                SharedWithMeFile sharedWithMeFile = mViewerInstance.FunctionProvider.QuerySharedWithMeFileByDuid(mNxlFileFingerPrint.duid);
            //                if (null != sharedWithMeFile)
            //                {
            //                    result = true;
            //                }
            //                else
            //                {
            //                    mViewerInstance.Log.InfoFormat("\t\t no found the sharedWithMeFile in DB \r\n", result);
            //                }
            //            }
            //        }
            //        else if (mNxlFileFingerPrint.isFromSystemBucket)
            //        {
            //            result = true;
            //        }
            //        else if (mNxlFileFingerPrint.isFromPorject)
            //        {
            //            result = true;
            //        }
            //    }
            //}

            mViewerInstance.Log.InfoFormat("\t\t IsDisplayShareButton:{0} \r\n", result);
            return result;
        }

        public bool IsDisplayEditButton(NxlFileFingerPrint fp)
        {
            bool result = false;
            edit.Helper.EnumOfficeVer ver;

            if (!edit.Helper.IsOfficeFile(fp.name))
            {
                mViewerInstance.Log.Warn("\t\t This file is not a office file \r\n");
                return result;
            }

            if (!edit.Helper.IsOfficeInstalled(out ver))
            {
                mViewerInstance.Log.Warn("\t\t This machine is not install office \r\n");
                return result;
            }

            if (!(ver == edit.Helper.EnumOfficeVer.Office_2016 || ver == edit.Helper.EnumOfficeVer.Office_2013))
            {
                mViewerInstance.Log.Warn("\t\t This machine is not install office 2016 or 2013 \r\n");
                return result;
            }

            if (!fp.HasRight(FileRights.RIGHT_EDIT))
            {
                mViewerInstance.Log.Warn("\t\t This file or current user has not edit rights \r\n");
                return result;
            }

            if (mViewerInstance.Intent.IsFromMainWindow)
            {
                mViewerInstance.Log.Info("\t\t This file is click from nxrmApp \r\n");
                if (mViewerInstance.Intent.AllowEdit)
                {
                    mViewerInstance.Log.Info("\t\t Allow editing \r\n");
                    result = true;
                }
            }
            else
            {
                mViewerInstance.Log.Info("\t\t This file is click from OS folder \r\n");
                mViewerInstance.Log.Info("\t\t Allow editing \r\n");
                result = true;
            }

            //if (edit.Helper.IsOfficeFile(fp.name)
            //    && edit.Helper.IsOfficeInstalled(out ver) && (ver == edit.Helper.EnumOfficeVer.Office_2016 || ver == edit.Helper.EnumOfficeVer.Office_2013)
            //    && edit.Helper.IsExistOfficeAddin(fp.name, mViewerInstance.Session)
            //    && fp.HasRight(FileRights.RIGHT_EDIT))
            //{
              
            //    if (mViewerInstance.Intent.IsFromMainWindow)
            //    {
            //        if (mViewerInstance.Intent.AllowEdit)
            //        {
            //            result = true;
            //        }
            //        else
            //        {
            //            result = false;
            //        }
            //    }
            //    else
            //    {
            //        result = true;
            //    }
            //}
            //else
            //{
            //    result = false;
            //}

            return result;
        }

        private bool IsDisplayExtractButton(NxlFileFingerPrint fp)
        {
            bool result = false;
            if (fp.HasRight(FileRights.RIGHT_DECRYPT) && !fp.isFromMyVault)
            {
                result = true;
            }
            return result;
        }

        private bool IsDisplayPrintButton(NxlFileFingerPrint fp)
        {
            bool result = false;
            if (fp.HasRight(FileRights.RIGHT_PRINT))
            {
                string oriFileName = Path.GetFileNameWithoutExtension(fp.name);
                if (!string.Equals(Path.GetExtension(oriFileName), ".gif", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = true;
                }
            }
            return result;
        }

        private bool IsDisplayExportButton(NxlFileFingerPrint fp, bool isNetworkAvailable)
        {
            bool result = false;

            if (!isNetworkAvailable)
            {
                mViewerInstance.Log.Info("\t\t Network unavailable \r\n");
                return result;
            }

            if (!(fp.HasRight(FileRights.RIGHT_SAVEAS) || fp.HasRight(FileRights.RIGHT_DOWNLOAD)))
            {
                mViewerInstance.Log.Info("\t\t This file has not Save As Rights or Download rights \r\n");
                return result;
            }

            FunctionProvider functionProvider = mViewerInstance.FunctionProvider;

            if (fp.isFromSystemBucket)
            {
                mViewerInstance.Log.Info("\t\t This file is From SystemBucket \r\n");
                WorkSpaceFile workSpaceFile = functionProvider.QueryWorkSpacetFileByDuid(fp.duid);
                if (null != workSpaceFile)
                {
                    mViewerInstance.Log.Info("\t\t Has found this file in Work Space tb \r\n");
                    result = true;
                }
                else
                {
                    mViewerInstance.Log.Info("\t\t No found this file in Work Space tb \r\n");
                }
            }
            else if (fp.isFromPorject)
            {
                mViewerInstance.Log.Info("\t\t This file is From Porject \r\n");
                int projectId;
                ProjectFile projectFile = functionProvider.QueryProjectFileByDuid(fp.duid, out projectId);
                if (null != projectFile)
                {
                    mViewerInstance.Log.Info("\t\t Has found this file in Project tb \r\n");
                    result = true;
                }
                else
                {
                    mViewerInstance.Log.Info("\t\t No found this file in Project tb \r\n");
                }
            }
            else if (fp.isFromMyVault)
            {
                mViewerInstance.Log.Info("\t\t This file is From MyVault \r\n");
                if (fp.isOwner)
                {
                    mViewerInstance.Log.Info("\t\t Current user is file owner \r\n");
                    MyVaultFile myVaultFile = functionProvider.QueryMyVaultFileByDuid(fp.duid);
                    if (null != myVaultFile)
                    {
                        mViewerInstance.Log.Info("\t\t Has found this file in MyVault File tb \r\n");
                        result = true;
                    }
                    else
                    {
                        mViewerInstance.Log.Info("\t\t No found this file in MyVault File tb \r\n");
                    }
                }
                else
                {
                    mViewerInstance.Log.Info("\t\t Current user is not file owner \r\n");
                    SharedWithMeFile sharedWithMeFile = functionProvider.QuerySharedWithMeFileByDuid(fp.duid);
                    if (null != sharedWithMeFile)
                    {
                        mViewerInstance.Log.Info("\t\t Has found this file in Share With Me tb \r\n");
                        result = true;
                    }
                    else
                    {
                        mViewerInstance.Log.Info("\t\t No found this file in Share With Me tb \r\n");
                    }
                }
            }


            //if (isNetworkAvailable)
            //{
            //    if (fp.HasRight(FileRights.RIGHT_SAVEAS) || fp.HasRight(FileRights.RIGHT_DOWNLOAD))
            //    {
            //        FunctionProvider functionProvider = mViewerInstance.FunctionProvider;

            //        if (fp.isFromSystemBucket)
            //        {
            //            WorkSpaceFile workSpaceFile = functionProvider.QueryWorkSpacetFileByDuid(fp.duid);
            //            if (null != workSpaceFile)
            //            {
            //                result = true;
            //            }
            //        }
            //        else if (fp.isFromPorject)
            //        {
            //            int projectId;
            //            ProjectFile projectFile = functionProvider.QueryProjectFileByDuid(fp.duid, out projectId);
            //            if (null != projectFile)
            //            {
            //                result = true;
            //            }
            //        }
            //        else if (fp.isFromMyVault)
            //        {
            //            if (fp.isOwner)
            //            {
            //                MyVaultFile myVaultFile = functionProvider.QueryMyVaultFileByDuid(fp.duid);
            //                if (null != myVaultFile)
            //                {
            //                    result = true;
            //                }
            //            }
            //            else
            //            {
            //                SharedWithMeFile sharedWithMeFile = functionProvider.QuerySharedWithMeFileByDuid(fp.duid);
            //                if (null != sharedWithMeFile)
            //                {
            //                    result = true;
            //                }
            //            }
            //        }
            //        else if (fp.isFromPorject)
            //        {
            //            int projectId;
            //            ProjectFile projectFile = functionProvider.QueryProjectFileByDuid(fp.duid, out projectId);
            //            if (null != projectFile)
            //            {
            //                result = true;
            //            }
            //        }
            //    }
            //}
            mViewerInstance.Log.InfoFormat("\t\t IsDisplayExportButton:{0} \r\n", result);

            return result;
        }

        #endregion Update UI

        #region Click Event
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && mStatus != StatusOfView.NORMAL)
            {
                this.Close();
            }
        }

        private void Extract_Content_Btn_Click(object sender, RoutedEventArgs e)
        {
            string filePath = mFilePath;
            if (EnumFileType.FILE_TYPE_ASSEMBLY == CurrentFileType)
            {
                filePath = mBackupFilePath;
            }

            string destinationPath = string.Empty;
            if(ExtractContentHelper.ShowSaveFileDialog(mViewerInstance.Log, this, out destinationPath, filePath))
            {

                string decryptFilePath = RightsManagementService.GenerateDecryptFilePath(
                                                       mViewerInstance.Appconfig.RPM_FolderPath,
                                                       filePath,
                                                       true);

                RightsManagementService.DecryptNXLFile(mViewerInstance.User, mViewerInstance.Log, filePath, decryptFilePath);
                if (ExtractContentHelper.CopyFile(mViewerInstance.Log, mViewerInstance.Session, mViewerInstance.Appconfig.RPM_FolderPath, decryptFilePath, destinationPath))
                {
                    SendLog(filePath, NxlOpLog.Decrypt, true);
                    MessageNotify.NotifyMsg(mNxlFileFingerPrint.name, CultureStringInfo.Exception_ExportFeature_Succeeded + destinationPath + ".", EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.EXTRACT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);
                }
                else
                {
                    SendLog(filePath, NxlOpLog.Decrypt,false);
                    MessageNotify.NotifyMsg(mNxlFileFingerPrint.name, CultureStringInfo.Notify_RecordLog_Extract_Content_Failed, EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.EXTRACT, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Online);
                }
                RightsManagementService.RPMDeleteDirectory(mViewerInstance.Session, mViewerInstance.Log, mViewerInstance.Appconfig.RPM_FolderPath, Path.GetDirectoryName(decryptFilePath));
            }
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            SendLog(mFilePath,NxlOpLog.Print,true);
            (Viewer as IPrintable)?.Print();
        }

        private void FileInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            CustomControls.windows.fileInfo.viewModel.FileInfoWindowViewModel ViewModel = new CustomControls.windows.fileInfo.viewModel.FileInfoWindowViewModel();
            ViewModel.Name = mNxlFileFingerPrint.name;
            ViewModel.Path = mNxlFileFingerPrint.name;
            ViewModel.Size = mNxlFileFingerPrint.size;

            // string convertedLastModified = JavaTimeConverter.ToCSDateTime(mNxlFileFingerPrint.modified).ToLocalTime().ToString("MM/dd/yyyy h:mm:ss t\\M");

            DateTime dateTime = JavaTimeConverter.ToCSDateTime(mNxlFileFingerPrint.modified).ToLocalTime();
            string convertedLastModified = dateTime.ToLocalTime().ToString();

            ViewModel.LastModified = convertedLastModified;

            CustomControls.windows.fileInfo.helper.Expiration expiration = new CustomControls.windows.fileInfo.helper.Expiration();
            expiration.type = (CustomControls.windows.fileInfo.helper.ExpiryType)((int)mNxlFileFingerPrint.expiration.type);
            expiration.Start = mNxlFileFingerPrint.expiration.Start;
            expiration.End = mNxlFileFingerPrint.expiration.End;

            ViewModel.Expiration = expiration;
            // ViewModel.WaterMark = mNxlFileFingerPrint.adhocWatermark;
            ViewModel.WaterMark = mNxlFileFingerPrint.isByCentrolPolicy?mWatermarkStr: mNxlFileFingerPrint.adhocWatermark;

            ObservableCollection<string> Emails = new ObservableCollection<string>();
            ViewModel.Emails = Emails;

            if (mViewerInstance.Intent.IsFromMainWindow)
            {
                if (mNxlFileFingerPrint.isFromMyVault)
                {
                    if (mNxlFileFingerPrint.isOwner)
                    {
                        MyVaultFile myVaultFile = mViewerInstance.FunctionProvider.QueryMyVaultFileByDuid(mNxlFileFingerPrint.duid);

                        if (null != myVaultFile)
                        {
                            String[] tempEmails = myVaultFile.RmsSharedWith.Split(new char[] { ' ', ';', ',' });

                            foreach (string one in tempEmails)
                            {
                                if (!String.IsNullOrEmpty(one))
                                {
                                    Emails.Add(one);
                                }
                            }
                        }
                    }
                    else
                    {
                        SharedWithMeFile sharedWithMeFile = mViewerInstance.FunctionProvider.QuerySharedWithMeFileByDuid(mNxlFileFingerPrint.duid);
                        Emails.Add(sharedWithMeFile.Shared_by);
                    }
                }
            }

            SkydrmLocal.rmc.sdk.FileRights[] fileRights = mNxlFileFingerPrint.rights;
            ObservableCollection<CustomControls.pages.DigitalRights.model.FileRights> NxlFileRights = new ObservableCollection<CustomControls.pages.DigitalRights.model.FileRights>();
            for (int i = 0; i < fileRights.Length; i++)
            {
                int value = (int)fileRights[i];

                if (Enum.IsDefined(typeof(CustomControls.pages.DigitalRights.model.FileRights), value))
                {
                    CustomControls.pages.DigitalRights.model.FileRights temp = (CustomControls.pages.DigitalRights.model.FileRights)value;
                    NxlFileRights.Add(temp);
                }
            }

            if (!string.IsNullOrEmpty(ViewModel.WaterMark))
            {
                if (!NxlFileRights.Contains(CustomControls.pages.DigitalRights.model.FileRights.RIGHT_WATERMARK))
                {
                    NxlFileRights.Add(CustomControls.pages.DigitalRights.model.FileRights.RIGHT_WATERMARK);
                }
            }

            if (!mNxlFileFingerPrint.isByCentrolPolicy)
            {
                NxlFileRights.Add(CustomControls.pages.DigitalRights.model.FileRights.RIGHT_VALIDITY);
            }

            if (NxlFileRights.Contains(CustomControls.pages.DigitalRights.model.FileRights.RIGHT_DOWNLOAD)
                                                        &&
                NxlFileRights.Contains(CustomControls.pages.DigitalRights.model.FileRights.RIGHT_SAVEAS))
            {
                NxlFileRights.Remove(CustomControls.pages.DigitalRights.model.FileRights.RIGHT_DOWNLOAD);
            }

            ViewModel.FileRights = NxlFileRights;
            ViewModel.IsByCentrolPolicy = mNxlFileFingerPrint.isByCentrolPolicy;
            ViewModel.CentralTag = mNxlFileFingerPrint.tags;

            if (mNxlFileFingerPrint.isFromMyVault)
            {
                if (mNxlFileFingerPrint.isOwner)
                {
                    ViewModel.FileMetadate = CustomControls.windows.fileInfo.helper.FileMetadate.isFromMyVault;
                }
                else
                {
                    ViewModel.FileMetadate = CustomControls.windows.fileInfo.helper.FileMetadate.isFromShareWithMe;
                }
            }
            else if (mNxlFileFingerPrint.isFromPorject)
            {
                ViewModel.FileMetadate = CustomControls.windows.fileInfo.helper.FileMetadate.isFromPorject;
            }
            else if (mNxlFileFingerPrint.isFromSystemBucket)
            {
                ViewModel.FileMetadate = CustomControls.windows.fileInfo.helper.FileMetadate.isFromSystemBucket;
            }

            FileInfoWindow fileInfoWindow = new FileInfoWindow()
            {
                ViewModel = ViewModel
            };

            fileInfoWindow.Show();
        }

        private void ShareBtn_Click(object sender, RoutedEventArgs e)
        {
            // test add share transaction
            if (mNxlFileFingerPrint.isFromPorject)
            {
                Process rmd = new Process();
                rmd.StartInfo.FileName = "nxrmdapp.exe";
                rmd.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
                rmd.StartInfo.Arguments += "-reShare";
                rmd.StartInfo.Arguments += " ";
                rmd.StartInfo.Arguments += "\"" + mFilePath+ "\"";
                rmd.Start();
                
                return;
            }
            //

            string filePath = mFilePath;
            if (EnumFileType.FILE_TYPE_ASSEMBLY == CurrentFileType)
            {
                filePath = mBackupFilePath;
            }

            ShareWindow win = new ShareWindow(filePath);

            //if (CurrentFileType == EnumFileType.FILE_TYPE_OFFICE || CurrentFileType == EnumFileType.FILE_TYPE_PDF)
            //{
            //    if (Viewer is PreviewHandlerPage)
            //    {
            //        PreviewHandlerPage previewHandlerPage = Viewer as PreviewHandlerPage;
            //        OverlayWindow overlayWindow = previewHandlerPage.OverlayWindow;
            //        win.Owner = overlayWindow;
            //    }
            //}else
            //{
            //    win.Owner = this;
            //}

            win.Owner = this;

            win.ShowDialog();
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

        private void HideViewer()
        {
            (Viewer as PreviewHandlerPage)?.HideOverlayWindow();
            this.Visibility = Visibility.Collapsed;
            InterruptingCloseWindow = false;
        }

        private void ShowCursorsWait()
        {
            (Viewer as PreviewHandlerPage)?.ShowCursorsWait();
            this.Cursor = Cursors.Wait;
        }

        private void HideCursorsWait()
        {
            (Viewer as PreviewHandlerPage)?.HideCursorsWait();
            this.Cursor = Cursors.Arrow;
        }

        private void DisableTopmostContainer()
        {
            (Viewer as PreviewHandlerPage)?.DisableOverlayWindowr();
            this.Topmost_Container.IsEnabled = false;
        }

        private void EnableTopmostContainer()
        {
            (Viewer as PreviewHandlerPage)?.EnableOverlayWindowr();
            this.Topmost_Container.IsEnabled = true;
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            InterruptingCloseWindow = true;
            DisableTopmostContainer();
            ShowCursorsWait();

            log4net.ILog log = mViewerInstance.Log;
            OfficeFileEdit fileEditor = new OfficeFileEdit(log, mViewerInstance.Session);
            fileEditor.FileOpend += delegate (Process EditProcess)
            {
                HideViewer();
                // update the process id in registry for second open this file
                mViewerInstance.RecordTheFileInRegistry(mViewerInstance.Intent.FilePath, EditProcess.Id);
            };

            fileEditor.OnEditCompleteCallback += delegate (EditCallBack EditCallBack)
            {
                // Notify RMD to update file status and do sync.
                try
                {
                    Bundle<EditCallBack> bundle = new Bundle<EditCallBack>()
                    {
                        Intent = Intent.SyncFileAfterEdit,
                        obj = EditCallBack
                    };
                    string json = JsonConvert.SerializeObject(bundle);
                    NamedPipeClient.Start(json);
                }
                catch (Exception ex)
                {
                    log.Error(ex.ToString());
                }

                this.Close();

            };

            fileEditor.OnEditProcessExited += delegate ()
            {
                this.Close();
            };

            try
            {
                fileEditor.Edit(mFilePath);
            }
            catch (Exception ex)
            {
                InterruptingCloseWindow = false;
                EnableTopmostContainer();
                HideCursorsWait();
                CommonUtils.ShowBalloonTip(CultureStringInfo.VIEW_DLGBOX_DETAILS_SYSTEM_INTERNAL_ERROR, false);
            }
        }


        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileExport.GetInstance().Export(mNxlFileFingerPrint, this);
            }
            catch (Exception ex)
            {
            }
        }
        #endregion Click Event


        #endregion Private F

        #region Public F

        public void HandleOfficeFileRenderException()
        {
            //DismissLoadinBar();
            //this.Btn_Print.Visibility = Visibility.Collapsed;
            //this.Btn_FileInfo.Visibility = Visibility.Collapsed;
            //this.Btn_ShareFile.Visibility = Visibility.Collapsed;
            //this.Btn_Edit.Visibility = Visibility.Collapsed;
            //this.Btn_Export.Visibility = Visibility.Collapsed;
            //this.Btn_Extract_Content.Visibility = Visibility.Collapsed;
            mStatus = StatusOfView.ERROR_DECRYPTFAILED;
            UpdateLayout(mStatus);
        }

        public void HandlerException(StatusOfView status, string message)
        {
            //PromptInfo_Containe.Visibility = Visibility.Visible;
            //TB_PromptInfo.Text = message;
            //this.Btn_Print.Visibility = Visibility.Collapsed;
            //this.Btn_FileInfo.Visibility = Visibility.Collapsed;
            //this.Btn_ShareFile.Visibility = Visibility.Collapsed;
            //this.Btn_Edit.Visibility = Visibility.Collapsed;
            //this.Btn_Export.Visibility = Visibility.Collapsed;
            //this.Btn_Extract_Content.Visibility = Visibility.Collapsed;
            //this.RotateStackPanel.Visibility = Visibility.Hidden;
            //this.VerticalSeperateLine.Visibility = Visibility.Hidden;

            mStatus = status;
            UpdateLayout(message);
        }

        public void CancelEventHandler(object sender, CancelEventArgs e)
        {
            e.Cancel = InterruptingCloseWindow;
        }

        public void DismissLoadinBar()
        {
            LoadingBar.Visibility = Visibility.Collapsed;
        }

        #endregion Public F

        #region test Watermark

        [DllImport("CPPDLL.dll", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "InitWaterMark")]
        static private extern void InitWaterMark(IntPtr hwnd);

        [DllImport("CPPDLL.dll", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "UpdateWaterMark")]
        static private extern void UpdateWaterMark(IntPtr hwnd);

        [DllImport("CPPDLL.dll", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "DeleteWaterMark")]
        static private extern void DeleteWaterMark();

        #endregion test Watermark

    }
}
