using CustomControls.windows.fileInfo.view;
using Microsoft.Win32;
using Newtonsoft.Json;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.database;
using Viewer.upgrade.application;
using Viewer.upgrade.cookie;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.file.component.export;
using Viewer.upgrade.file.component.removeProtection;
using Viewer.upgrade.file.component.share.window.view;
using Viewer.upgrade.session;
using Viewer.upgrade.utils;
using static Viewer.upgrade.utils.NetworkStatus;
using Viewer.upgrade.communication.message;
using System.Windows.Interop;
using Viewer.upgrade.file.utils;
using static Viewer.upgrade.utils.ToolKit;
using Viewer.upgrade.exception;
using Viewer.upgrade.file.component.edit;
using static Viewer.upgrade.utils.RegisterProcessUtils;
using Viewer.upgrade.ui.common.fileInfoWindow;

namespace Viewer.upgrade.file.basic
{
    public class _StdNxlFile : INxlFile
    {
        public string Duid => mDuid;
        public bool Expired => mExpired;
        public WatermarkInfo WatermarkInfo => mWatermarkInfo;
        public NxlFileFingerPrint NxlFileFingerPrint => mNxlFileFingerPrint;
        public string FileName => mFileName;
        public string Extention => mExtention;
        public string FilePath => mFilePath;
        public EnumFileType FileType => mFileType;
        public Int32 Dirstatus => mDirstatus;

        protected ViewerApp mApplication;
        protected string mDuid;
        protected bool mExpired;
        protected WatermarkInfo mWatermarkInfo = null;
        protected NxlFileFingerPrint mNxlFileFingerPrint;
        protected string mFileName;
        protected string mExtention;
        protected string mFilePath;
        protected List<string> mRpmFilePaths = new List<string>();
        protected EnumFileType mFileType = EnumFileType.UNKNOWN;
        protected volatile EditProcess mEditProcess = null;
        protected Cookie mCookie;
        protected bool mIsNetworkAvailable;
        protected Int32 mDirstatus;

        public _StdNxlFile(Cookie cookie, Int32 dirstatus) : this(cookie)
        {
            this.mDirstatus = dirstatus;
        }

        private _StdNxlFile(Cookie cookie)
        {
            try
            {
                mCookie = cookie;
                mApplication = (ViewerApp)Application.Current;
                mFilePath = cookie.FilePath;
                mFileName = Path.GetFileName(mFilePath);
                // mExtention = Path.GetExtension(Path.GetFileNameWithoutExtension(mFilePath)).ToLower();
                mExtention = NxlFileUtils.GetFileExtention(mFilePath).ToLower();
                mNxlFileFingerPrint = mApplication.SdkSession.User.GetNxlFileFingerPrint(mFilePath);

                if (!mNxlFileFingerPrint.HasRight(FileRights.RIGHT_VIEW))
                {
                    mApplication.Log.Info("\t\t No view rights. \r\n");
                    throw new NotAuthorizedException(mApplication.FindResource("VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED").ToString());
                }

                mExpired = CheckExpired(mNxlFileFingerPrint);
                if (mExpired && !mNxlFileFingerPrint.isFromMyVault)
                {
                    throw new FileExpiredException(mApplication.FindResource("Nxl_File_Has_Expired").ToString());
                }
                mIsNetworkAvailable = NetworkStatus.IsAvailable;
                mFileType = NxlFileUtils.GetFileTypeByExtentionEx(mCookie.FilePath);
                mDuid = mNxlFileFingerPrint.duid;
                mWatermarkInfo = BuildWatermark(mNxlFileFingerPrint);
                AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
            }
            catch (RmSdkException ex)
            {
                // mStatusCode |= FileStatusCode.NOT_AUTHORIZED;
                mApplication.Log.Error(ex);
                throw new NotAuthorizedException(mApplication.FindResource("VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED").ToString());
            }
            catch (Exception ex)
            {
                // mStatusCode |= FileStatusCode.INTERNAL_ERROR;
                mApplication.Log.Error(ex);
                throw ex;
            }
        }


        ///**  
        // * ErrorCode
        // *      SUCCEEDED;
        // *      NOT_AUTHORIZED;
        // *      SYSTEM_INTERNAL_ERROR;
        // **/
        //public UInt64 Open()
        //{
        //    try
        //    {
        //        mFileName = Path.GetFileName(mFilePath);
        //        mExtention = Path.GetExtension(Path.GetFileNameWithoutExtension(mFilePath)).ToLower();
        //        if (ToolKit.GetFileTypeByExtentionEx(mCookie.FilePath, out mFileType) != ErrorCode.SUCCEEDED)
        //        {
        //            mStatusCode |= FileStatusCode.INTERNAL_ERROR;
        //            return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //        }

        //        mNxlFileFingerPrint = mApplication.SdkSession.User.GetNxlFileFingerPrint(mFilePath);
        //        mExpired = CheckExpired(mNxlFileFingerPrint);
        //        mWatermarkInfo = BuildWatermark(mNxlFileFingerPrint);
        //        mDuid = mNxlFileFingerPrint.duid;
        //        if (!mApplication.SdkSession.SDWL_RPM_GetFileStatus(mCookie.FilePath, out mDirstatus, out mIsNxlFile))
        //        {
        //            mStatusCode |= FileStatusCode.INTERNAL_ERROR;
        //            return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //        }
        //        mStatusCode |= FileStatusCode.OPENED;
        //        return ErrorCode.SUCCEEDED;
        //    }
        //    catch (RmSdkException e)
        //    {
        //        mStatusCode |= FileStatusCode.NOT_AUTHORIZED;
        //        return ErrorCode.NOT_AUTHORIZED;
        //    }
        //    catch (Exception ex)
        //    {
        //        mStatusCode |= FileStatusCode.INTERNAL_ERROR;
        //        return ErrorCode.SYSTEM_INTERNAL_ERROR;
        //    }
        //}


        //private void GeneralCheck()
        //{
        //    if ((mStatusCode & FileStatusCode.NOT_AUTHORIZED) == FileStatusCode.NOT_AUTHORIZED)
        //    {
        //        throw new NotAuthorizedException(mApplication.FindResource("VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED").ToString());
        //    }

        //    if ((mStatusCode & FileStatusCode.EXPIRED) == FileStatusCode.EXPIRED)
        //    {
        //        throw new NotAuthorizedException(mApplication.FindResource("Nxl_File_Has_Expired").ToString());
        //    }

        //    if ((mStatusCode & FileStatusCode.INTERNAL_ERROR) == FileStatusCode.INTERNAL_ERROR)
        //    {
        //        throw new UnknownException(mApplication.FindResource("Common_System_Internal_Error").ToString());
        //    }
        //}


        public string Decrypt(string outputFileName = "", bool removeTimestamp = true)
        {
            mApplication.Log.Info("\t\t Decrypt \r\n");
            string result = string.Empty;
            try
            {
                result = NxlFileUtils.Decrypt(FilePath, outputFileName, removeTimestamp);
                mRpmFilePaths.Add(result);
                return result;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                mApplication.Log.Error(ex.Message, ex);
                throw ex;
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex.Message, ex);
                throw ex;
            }
        }

        public void Delete()
        {
            return;
        }

        public void Edit(Action<bool> EditSaved, Action ProcessExited)
        {
            try
            {
                mApplication.Log.Info("\t\t Edit \r\n");
                if (null == mEditProcess)
                {

                    mEditProcess = new EditProcess(this);

                    if (null == mEditProcess)
                    {
                        throw new UnknownException();
                    }
                    mEditProcess.EditSaved += EditSaved;
                    mEditProcess.ProcessExited += ProcessExited;
                }
                else
                {
                    mEditProcess.ShowWindow();
                }
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex.Message, ex);
                throw ex;
            }

            //string rpmFilePath = string.Empty;
            //try
            //{
            //    if (null == mEditProcess)
            //    {
            //        lock (padlock)
            //        {
            //            if (null == mEditProcess)
            //            {
            //                mApplication.Log.Info("\t\t Edit \r\n");
            //                rpmFilePath = mApplication.SdkSession.RPM_EditFile(FilePath);
            //                OfficeRMXHelper.ChangeRegeditOfOfficeAddin(mApplication.SdkSession);
            //                IProcess editProcess = OpenNativeProcess(rpmFilePath);
            //                if (null == editProcess)
            //                {
            //                    throw new FileTypeNoSupportedException(mApplication.FindResource("Edit_File_Type_No_Supported").ToString());
            //                }
            //                editProcess.OfficeProcessExited += OfficeProcessExited;
            //                editProcess.EditSaved += EditSaved;
            //                ToolKit.SaveHwndToRegistry(mFilePath, editProcess.Process.MainWindowHandle);
            //                mEditProcess = editProcess;
            //            }
            //            else
            //            {
            //                Win32Common.BringWindowToTopEx(mEditProcess.Process.MainWindowHandle);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        Win32Common.BringWindowToTopEx(mEditProcess.Process.MainWindowHandle);
            //    }
            //}
            //catch (FileTypeNoSupportedException ex)
            //{
            //    if (!string.IsNullOrEmpty(rpmFilePath))
            //    {
            //        DeleteSubKeyValue(rpmFilePath);
            //    }
            //    mApplication.Log.Error(ex.Message, ex);
            //    throw ex;
            //}
            //catch (Exception ex)
            //{
            //    if (!string.IsNullOrEmpty(rpmFilePath))
            //    {
            //        DeleteSubKeyValue(rpmFilePath);
            //    }
            //    mApplication.Log.Error(ex.Message, ex);
            //    throw ex;
            //}
        }

        public void Export(System.Windows.Window Owner)
        {
            try
            {
                mApplication.Log.Info("Export");
                if (null == Owner)
                {
                    throw new ArgumentNullException();
                }

                if (string.Equals(mCookie.FileRepo, "REPO_EXTERNAL_DRIVE", StringComparison.CurrentCultureIgnoreCase))
                {
                    FileExport.GetInstance().Export(mCookie.RepoId,mCookie.DisplayPath,mNxlFileFingerPrint, Owner);
                }
                else
                {
                    FileExport.GetInstance().Export(mCookie.FileRepo, mNxlFileFingerPrint, Owner);
                }

            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public void Extract(System.Windows.Window Owner)
        {
            try
            {
                mApplication.Log.Info("Extract");
                if (null == Owner)
                {
                    throw new ArgumentNullException();
                }

                string destinationPath = string.Empty;
                if (ExtractContentHelper.ShowSaveFileDialog(Owner, out destinationPath, mFilePath))
                {
                    string decryptFilePath = Decrypt();
                    if (ExtractContentHelper.CopyFile(mApplication.SdkSession, mApplication.Def_RPM_Folder, decryptFilePath, destinationPath))
                    {

                        mApplication.SdkSession.User.AddLog(mFilePath, NxlOpLog.Decrypt, true);
                        MessageNotify.NotifyMsg(mApplication.SdkSession, mNxlFileFingerPrint.name, mApplication.FindResource("Exception_ExportFeature_Succeeded").ToString() + destinationPath + ".", EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.EXTRACT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);
                    }
                    else
                    {
                        mApplication.SdkSession.User.AddLog(mFilePath, NxlOpLog.Decrypt, false);
                        MessageNotify.NotifyMsg(mApplication.SdkSession, mNxlFileFingerPrint.name, mApplication.FindResource("Notify_RecordLog_Extract_Content_Failed").ToString(), EnumMsgNotifyType.PopupBubble, MsgNotifyOperation.EXTRACT, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Online);
                    }
                }

            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public void FileInfo(System.Windows.Window Owner)
        {
            try
            {
                mApplication.Log.Info("FileInfo");

                if (null == Owner)
                {
                    throw new ArgumentNullException();
                }

                FileInfoWin fileInfoWin = new FileInfoWin(this,mCookie);
                fileInfoWin.Owner = Owner;
                fileInfoWin.Show();

                #region Deprecated
                //FileInfoWindow fileInfoWindow = new FileInfoWindow();
                //CustomControls.windows.fileInfo.viewModel.FileInfoWindowViewModel ViewModel = new CustomControls.windows.fileInfo.viewModel.FileInfoWindowViewModel(fileInfoWindow);
                //ViewModel.Name = mNxlFileFingerPrint.name;
                //ViewModel.Path = mNxlFileFingerPrint.name;
                //ViewModel.Size = mNxlFileFingerPrint.size;
                //// string convertedLastModified = JavaTimeConverter.ToCSDateTime(mNxlFileFingerPrint.modified).ToLocalTime().ToString("MM/dd/yyyy h:mm:ss t\\M");
                //DateTime dateTime = JavaTimeConverter.ToCSDateTime(mNxlFileFingerPrint.modified).ToLocalTime();
                //string convertedLastModified = dateTime.ToLocalTime().ToString();

                //ViewModel.LastModified = convertedLastModified;

                //CustomControls.windows.fileInfo.helper.Expiration expiration = new CustomControls.windows.fileInfo.helper.Expiration();
                //expiration.type = (CustomControls.windows.fileInfo.helper.ExpiryType)((int)mNxlFileFingerPrint.expiration.type);
                //expiration.Start = mNxlFileFingerPrint.expiration.Start;
                //expiration.End = mNxlFileFingerPrint.expiration.End;

                //ViewModel.Expiration = expiration;
                //// ViewModel.WaterMark = mNxlFileFingerPrint.adhocWatermark;

                //string waterMarkText = null == mWatermarkInfo ? string.Empty : mWatermarkInfo.WaterMarkRaw;
                //ViewModel.WaterMark = waterMarkText;

                //ObservableCollection<string> Emails = new ObservableCollection<string>();
                //ViewModel.Emails = Emails;

                //if (mCookie.IsClickFromNxrmApp)
                //{
                //    if (mNxlFileFingerPrint.isFromMyVault)
                //    {
                //        if (mNxlFileFingerPrint.isOwner)
                //        {
                //            MyVaultFile myVaultFile = mApplication.FunctionProvider.QueryMyVaultFileByDuid(mNxlFileFingerPrint.duid);

                //            if (null != myVaultFile)
                //            {
                //                String[] tempEmails = myVaultFile.RmsSharedWith.Split(new char[] { ' ', ';', ',' });

                //                foreach (string one in tempEmails)
                //                {
                //                    if (!String.IsNullOrEmpty(one))
                //                    {
                //                        Emails.Add(one);
                //                    }
                //                }
                //            }
                //        }
                //        else
                //        {
                //            SharedWithMeFile sharedWithMeFile = mApplication.FunctionProvider.QuerySharedWithMeFileByDuid(mNxlFileFingerPrint.duid);
                //            Emails.Add(sharedWithMeFile.Shared_by);
                //        }
                //    }
                //}

                //SkydrmLocal.rmc.sdk.FileRights[] fileRights = mNxlFileFingerPrint.rights;
                //ObservableCollection<CustomControls.components.DigitalRights.model.FileRights> NxlFileRights = new ObservableCollection<CustomControls.components.DigitalRights.model.FileRights>();
                //for (int i = 0; i < fileRights.Length; i++)
                //{
                //    int value = (int)fileRights[i];

                //    if (Enum.IsDefined(typeof(CustomControls.components.DigitalRights.model.FileRights), value))
                //    {
                //        CustomControls.components.DigitalRights.model.FileRights temp = (CustomControls.components.DigitalRights.model.FileRights)value;
                //        NxlFileRights.Add(temp);
                //    }
                //}

                //if (!string.IsNullOrEmpty(waterMarkText))
                //{
                //    if (!NxlFileRights.Contains(CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK))
                //    {
                //        NxlFileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK);
                //    }
                //}

                //if (!mNxlFileFingerPrint.isByCentrolPolicy)
                //{
                //    NxlFileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_VALIDITY);
                //}

                //if (NxlFileRights.Contains(CustomControls.components.DigitalRights.model.FileRights.RIGHT_DOWNLOAD)
                //                                            &&
                //    NxlFileRights.Contains(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SAVEAS))
                //{
                //    NxlFileRights.Remove(CustomControls.components.DigitalRights.model.FileRights.RIGHT_DOWNLOAD);
                //}

                //ViewModel.FileRights = NxlFileRights;
                //ViewModel.IsByCentrolPolicy = mNxlFileFingerPrint.isByCentrolPolicy;
                //ViewModel.CentralTag = mNxlFileFingerPrint.tags;

                //if (mNxlFileFingerPrint.isFromMyVault)
                //{
                //    if (mNxlFileFingerPrint.isOwner)
                //    {
                //        ViewModel.FileMetadate = CustomControls.windows.fileInfo.helper.FileMetadate.isFromMyVault;
                //    }
                //    else
                //    {
                //        ViewModel.FileMetadate = CustomControls.windows.fileInfo.helper.FileMetadate.isFromShareWithMe;
                //    }
                //}
                //else if (mNxlFileFingerPrint.isFromPorject)
                //{
                //    ViewModel.FileMetadate = CustomControls.windows.fileInfo.helper.FileMetadate.isFromPorject;
                //}
                //else if (mNxlFileFingerPrint.isFromSystemBucket)
                //{
                //    ViewModel.FileMetadate = CustomControls.windows.fileInfo.helper.FileMetadate.isFromSystemBucket;
                //}

                //fileInfoWindow.ViewModel = ViewModel;
                //fileInfoWindow.Owner = Owner;
                //fileInfoWindow.Show();
                #endregion

            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public void Share(System.Windows.Window Owner)
        {
            try
            {
                mApplication.Log.Info("Share");
  
                if (null == Owner)
                {
                    throw new ArgumentNullException();
                }

                if (mNxlFileFingerPrint.isFromPorject && (!mNxlFileFingerPrint.isFromMyVault) && (!mNxlFileFingerPrint.isFromSystemBucket))
                {
                    Process rmd = new Process();
                    rmd.StartInfo.FileName = "nxrmdapp.exe";
                    rmd.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
                    rmd.StartInfo.Arguments += "-reShare";
                    rmd.StartInfo.Arguments += " ";
                    rmd.StartInfo.Arguments += "\"" + mFilePath + "\"";
                    rmd.Start();
                    return;
                }
                else
                {
                    Process rmd = new Process();
                    rmd.StartInfo.FileName = "nxrmdapp.exe";
                    rmd.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
                    rmd.StartInfo.Arguments += "-share";
                    rmd.StartInfo.Arguments += " ";
                    rmd.StartInfo.Arguments += "\"" + mFilePath + "\"";
                    rmd.Start();
                    return;
                }

                //ShareWindow win = new ShareWindow(mNxlFileFingerPrint);
                //win.Owner = Owner;
                //win.ShowDialog();
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public void ClearTempFiles()
        {
            mApplication.Log.Info("\t\t ClearTempFiles \r\n");
            try
            {
                DeleteRpmFile();
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public void Print(System.Windows.Window Owner)
        {
            //try
            //{
            //    if (null == Owner)
            //    {
            //        throw new ArgumentNullException();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    mApplication.Log.Error(ex);
            //    throw ex;
            //}
        }

        private void RPMDeleteDirectory(string directoryPath)
        {
            try
            {
                string[] allFilePath = Directory.GetFiles(directoryPath);

                foreach (string filePath in allFilePath)
                {
                    mApplication.SdkSession.RPM_DeleteFile(filePath);
                }

                string[] allSubdirectory = Directory.GetDirectories(directoryPath);

                foreach (string subDirectoryPath in allSubdirectory)
                {
                    RPMDeleteDirectory(subDirectoryPath);
                }

                mApplication.SdkSession.RPM_DeleteFolder(directoryPath);
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex.Message);
                throw ex;
            }
        }

        //private IProcess OpenNativeProcess(string filePath)
        //{
        //    if (ToolKit.WORD_EXTENSIONS.Contains(Extention))
        //    {
        //        return new WinWordProcess(filePath);
        //    }

        //    if (ToolKit.EXCEL_EXTENSIONS.Contains(Extention))
        //    {
        //        return new ExcelProcess(filePath);
        //    }

        //    if (ToolKit.POWERPOINT_EXTENSIONS.Contains(Extention))
        //    {
        //        return new PowerPntProcess(filePath);
        //    }

        //    //if (ToolKit.PDF_EXTENSIONS.Contains(mExtention))
        //    //{
        //    //    return new AdobeProcess();
        //    //}

        //    return null;
        //}

        private bool CheckExpired(NxlFileFingerPrint fp)
        {
            bool result = true;
            if (fp.expiration.type != SkydrmLocal.rmc.sdk.ExpiryType.NEVER_EXPIRE
                                    &&
            ToolKit.DateTimeToTimestamp(DateTime.Now) > fp.expiration.End)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        private WatermarkInfo BuildWatermark(NxlFileFingerPrint fp)
        {
            //string watermarkStr = string.Empty;
            if (fp.isByCentrolPolicy)
            {
                //Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;
                //try
                //{
                //    mApplication.SdkSession.User.EvaulateNxlFileRights(mFilePath, out rightsAndWatermarks);
                //    foreach (var v in rightsAndWatermarks)
                //    {
                //        List<WaterMarkInfo> waterMarkInfoList = v.Value;
                //        if (waterMarkInfoList == null)
                //        {
                //            continue;
                //        }
                //        foreach (var w in waterMarkInfoList)
                //        {
                //            watermarkStr = w.text;
                //            if (!string.IsNullOrEmpty(watermarkStr))
                //            {
                //                break;
                //            }
                //        }
                //        if (!string.IsNullOrEmpty(watermarkStr))
                //        {
                //            break;
                //        }
                //    }
                //}
                //catch (Exception ex)
                //{
                //    throw ex;
                //}

                string watermarkStr=  GetWatermarkFroCentrolPolicyFile(true);
                string watermarkStrRaw =  GetWatermarkFroCentrolPolicyFile(false);
                WatermarkInfo.Builder builder = new WatermarkInfo.Builder();
                WatermarkInfo watermarkInfo = builder.DefaultSet2(watermarkStrRaw, watermarkStr).Build();
                return watermarkInfo;
            }
            else
            {
                string watermarkStrRaw = fp.adhocWatermark;
                WatermarkInfo.Builder builder = new WatermarkInfo.Builder();
                WatermarkInfo watermarkInfo = builder.DefaultSet(watermarkStrRaw, mApplication.SdkSession.User.Email).Build();
                return watermarkInfo;
            }

            //WatermarkInfo.Builder builder = new WatermarkInfo.Builder();
            //WatermarkInfo watermarkInfo = builder.DefaultSet(watermarkStr, mApplication.SdkSession.User.Email).Build();
            //return watermarkInfo;
        }

        private string GetWatermarkFroCentrolPolicyFile(bool b)
        {
            string watermarkStr = string.Empty; 
            Dictionary <FileRights, List<WaterMarkInfo>> rightsAndWatermarks;
            try
            {
                mApplication.SdkSession.User.EvaulateNxlFileRights(mFilePath, out rightsAndWatermarks, b);
                foreach (var v in rightsAndWatermarks)
                {
                    List<WaterMarkInfo> waterMarkInfoList = v.Value;
                    if (waterMarkInfoList == null)
                    {
                        continue;
                    }
                    foreach (var w in waterMarkInfoList)
                    {
                        watermarkStr = w.text;
                        if (!string.IsNullOrEmpty(watermarkStr))
                        {
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(watermarkStr))
                    {
                        break;
                    }
                }

                return watermarkStr;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void DeleteRpmFile()
        {
            mApplication.Log.Info("\t\t DeleteRpmFile \r\n");
            foreach (string rpmFilePath in mRpmFilePaths)
            {
                if (string.IsNullOrWhiteSpace(rpmFilePath))
                {
                    continue;
                }

                if (!File.Exists(rpmFilePath))
                {
                    continue;
                }

                string directoryPath = Path.GetDirectoryName(rpmFilePath);

                if (!Directory.Exists(directoryPath))
                {
                    continue;
                }

                if (string.Equals(mApplication.Def_RPM_Folder, directoryPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                RPMDeleteDirectory(directoryPath);
            }
        }

        public bool CanShare()
        {
            try
            {
                if (!mIsNetworkAvailable)
                {
                    return false;
                }

                //if (!mNxlFileFingerPrint.isByAdHoc)
                //{
                //    mApplication.Log.Info("\t\t this file is not a adhoc file \r\n");
                //    return ErrorCode.ACCESS_DENY;
                //}

                if ((!mNxlFileFingerPrint.hasAdminRights) && (!mNxlFileFingerPrint.HasRight(FileRights.RIGHT_SHARE)))
                {
                    return false;
                }

                if (mCookie.IsClickFromNxrmApp)
                {
                    if (mCookie.AllowShare)
                    {
                        if (mNxlFileFingerPrint.isFromMyVault)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                        //else if (mNxlFileFingerPrint.isFromPorject && (!mNxlFileFingerPrint.isFromMyVault) && (!mNxlFileFingerPrint.isFromSystemBucket))
                        //{
                        //    mApplication.Log.Info("\t\t This file isFromPorject \r\n");
                        //    return ErrorCode.SUCCEEDED;
                        //}
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (mNxlFileFingerPrint.isFromMyVault)
                    {
                        if (mNxlFileFingerPrint.isOwner)
                        {
                            MyVaultFile myVaultFile = mApplication.FunctionProvider.QueryMyVaultFileByDuid(mDuid);
                            if (null != myVaultFile)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            SharedWithMeFile sharedWithMeFile = mApplication.FunctionProvider.QuerySharedWithMeFileByDuid(mDuid);
                            if (null != sharedWithMeFile)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                    //else if (mNxlFileFingerPrint.isFromPorject && (!mNxlFileFingerPrint.isFromMyVault) && (!mNxlFileFingerPrint.isFromSystemBucket))
                    //{
                    //    mApplication.Log.Info("\t\t This file isFromPorject \r\n");
                    //    return ErrorCode.SUCCEEDED;
                    //}
                }
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public bool CanPrint()
        {
            try
            {
                if (mFileType == EnumFileType.FILE_TYPE_VIDEO || mFileType == EnumFileType.FILE_TYPE_AUDIO)
                {
                    mApplication.Log.Info("\t\t Video and Audio type file currently not supported do print \r\n");
                    return false;
                }

                if (mFileType == EnumFileType.FILE_TYPE_SAP_VDS)
                {
                    mApplication.Log.Info("\t\t VDS type file currently not supported do print for security reason \r\n");
                    return false;
                }

                if (mFileType == EnumFileType.FILE_TYPE_HYPERTEXT_MARKUP)
                {
                    mApplication.Log.Info("\t\t HTML type file currently not supported do print for security reason \r\n");
                    return false;
                }

                if (mFileType == EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D)
                {
                    mApplication.Log.Info("\t\t HPS Exchange 3D type file need RIGHT_DECRYPT and RIGHT_PRINT to do print for security reason \r\n");
                    if (mNxlFileFingerPrint.HasRight(FileRights.RIGHT_DECRYPT) && mNxlFileFingerPrint.HasRight(FileRights.RIGHT_PRINT))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (mNxlFileFingerPrint.HasRight(FileRights.RIGHT_PRINT))
                {
                    string oriFileName = Path.GetFileNameWithoutExtension(mNxlFileFingerPrint.name);
                    if (!string.Equals(Path.GetExtension(oriFileName), ".gif", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }


                return false;
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public bool CanExport()
        {
            try
            {
                if (!mIsNetworkAvailable)
                {
                    return false;
                }

                if (!(mNxlFileFingerPrint.HasRight(FileRights.RIGHT_SAVEAS) || mNxlFileFingerPrint.HasRight(FileRights.RIGHT_DOWNLOAD)
                    || (mNxlFileFingerPrint.hasAdminRights && mNxlFileFingerPrint.HasRight(FileRights.RIGHT_VIEW))))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(mCookie.FileRepo))
                {
                    if (string.Equals(mCookie.FileRepo, "REPO_EXTERNAL_DRIVE",StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                if (mNxlFileFingerPrint.isFromSystemBucket)
                {
                    WorkSpaceFile workSpaceFile = mApplication.FunctionProvider.QueryWorkSpacetFileByDuid(mDuid);
                    if (null != workSpaceFile)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (mNxlFileFingerPrint.isFromPorject)
                {
                    int projectId;
                    ProjectFile projectFile = mApplication.FunctionProvider.QueryProjectFileByDuid(mDuid, out projectId);
                    if (null != projectFile)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (mNxlFileFingerPrint.isFromMyVault)
                {
                    if (mNxlFileFingerPrint.isOwner)
                    {
                        MyVaultFile myVaultFile = mApplication.FunctionProvider.QueryMyVaultFileByDuid(mDuid);
                        if (null != myVaultFile)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        SharedWithMeFile sharedWithMeFile = mApplication.FunctionProvider.QuerySharedWithMeFileByDuid(mDuid);
                        if (null != sharedWithMeFile)
                        {
                            mApplication.Log.Info("\t\t Has found this file in Share With Me tb \r\n");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public bool CanFileInfo()
        {
            return true;
        }

        public bool CanExtract()
        {
            try
            {
                if (mNxlFileFingerPrint.HasRight(FileRights.RIGHT_DECRYPT) && !mNxlFileFingerPrint.isFromMyVault)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        public bool CanEdit()
        {
            try
            {
                if (mFileType != EnumFileType.FILE_TYPE_OFFICE)
                {
                    return false;
                }

                if (   !ToolKit.DetectOffice2013(RegistryView.Registry64)
                    && !ToolKit.DetectOffice2013(RegistryView.Registry32)
                    && !ToolKit.DetectOffice2016(RegistryView.Registry64)
                    && !ToolKit.DetectOffice2016(RegistryView.Registry32)
                    && !ToolKit.DetectOffice2019(RegistryView.Registry64) 
                    && !ToolKit.DetectOffice2019(RegistryView.Registry32)
                    && !ToolKit.DetectOffice365(RegistryView.Registry64)
                    && !ToolKit.DetectOffice365(RegistryView.Registry32)) 
                {
                    return false;
                }

                if (!mNxlFileFingerPrint.HasRight(FileRights.RIGHT_EDIT))
                {
                    return false;
                }

                if (mCookie.IsClickFromNxrmApp)
                {
                    if (mCookie.AllowEdit)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                mApplication.Log.Error(ex);
                throw ex;
            }
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            mIsNetworkAvailable = e.IsAvailable;
        }
    }
}
