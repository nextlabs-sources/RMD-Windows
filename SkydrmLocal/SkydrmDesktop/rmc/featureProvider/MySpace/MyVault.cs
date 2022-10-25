using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkydrmDesktop;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database.table.myvault;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using static Skydrmlocal.rmc.database2.FunctionProvider;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.featureProvider.SharedWithMe;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmLocal.rmc.featureProvider.MyVault
{
    public sealed class MyVault : IMyVault
    {
        private readonly SkydrmApp App;
        private readonly log4net.ILog Log;
        private string working_path;

        public string WorkingFolder { get => working_path;}

        private static List<string> mDirty_RecordingList = new List<string>();
        private static List<string> mDirty_ModifyList = new List<string>();

        public MyVault(SkydrmApp app)
        {
            this.App = app;
            this.Log = app.Log;
            working_path = App.User.WorkingFolder + "\\MyVault";
            if (!Directory.Exists(working_path))
            {
                Directory.CreateDirectory(working_path);
            }
        }

        public void OnHeartBeat()
        {
            Sync();
        }

        public static bool IsDataDirtyMasked(string pathId)
        {
            return mDirty_RecordingList != null && mDirty_RecordingList.Contains(pathId);
        }

        public static bool RemoveDirtyMask(string pathId)
        {
            bool ret = mDirty_RecordingList.Contains(pathId) && mDirty_RecordingList.Remove(pathId);
            if (ret && !mDirty_ModifyList.Contains(pathId))
            {
                mDirty_ModifyList.Add(pathId);
            }
            return ret;
        }

        public static bool IsDataModifyDirtyMask(string pathId)
        {
            return mDirty_ModifyList != null && mDirty_ModifyList.Contains(pathId);
        }

        public static bool RemoveModifyMaskRecord(string pathId)
        {
            return mDirty_ModifyList.Contains(pathId) && mDirty_ModifyList.Remove(pathId);
        }

        public IMyVaultFile[] List()
        {
            // history, in order to boost up speed for UI rendering, 
            // make it return as as soon as possible
            // we splite VaultFile new and init,

            // filter out deleted&revokded myvaultfile.
            return Impl_List(true, true, false);
        }

        public IMyVaultFile[] ListWithoutFilter()
        {
            return Impl_List(false, false, false);
        }

        public IMyVaultFile[] Sync()
        {
            lock (this)
            {
                // add cache
                var remotes = App.Rmsdk.User.ListMyVaultFiles();
                // for merge,we don't need care about marking sure local info fixed.
                var locals = Impl_List(false, false, false);

                // Fix bug 53562, find difference set by (Local - remote) , and delete it 
                var diffset = from i in locals
                              let rNames = from j in remotes select j.nxlName
                              where !rNames.Contains(i.Nxl_Name)
                              select i;
                foreach (var i in diffset)
                {
                    App.DBFunctionProvider.DeleteMyVaultFile(i.Nxl_Name);
                }

                var ff = new List<InsertMyVaultFile>();
                foreach (var i in FilterOutNotModified(locals, remotes))
                {
                    ff.Add(new InsertMyVaultFile()
                    {
                        path_id = i.pathId,
                        display_path = i.displayPath,
                        repo_id = i.repoId,
                        duid = i.duid,
                        nxl_name = i.nxlName,
                        last_modified_time = i.lastModifiedTime,
                        creation_time = i.creationTime,
                        shared_time = i.sharedTime,
                        shared_with_list = i.sharedWithList,
                        size = i.size,
                        is_deleted = i.isDeleted == 1,
                        is_revoked = i.isRevoked == 1,
                        is_shared = i.isShared == 1,
                        source_repo_type = i.sourceRepoType,
                        source_file_display_path = i.sourceFileDisplayPath,
                        source_file_path_id = i.sourceFilePathId,
                        source_repo_name = i.sourceRepoName,
                        source_repo_id = i.sourceRepoId
                    }
                        );
                }
                App.DBFunctionProvider.UpsertMyVaultFileBatch(ff.ToArray());
                return List();
            }
           
        }

        public IMyVaultLocalFile[] ListLocalAdded()
        {
            var rt = new List<MyVaultLocalAddedFile>();
            foreach (var i in App.DBFunctionProvider.ListMyVaultLocalFile())
            {
                if (!FileHelper.Exist(i.Nxl_Local_Path))
                {
                    App.DBFunctionProvider.DeleteMyVaultLocalFile(i.Nxl_Name);
                    continue;
                }
                rt.Add(new MyVaultLocalAddedFile(i));
            }
            return rt.ToArray();
        }

        public IOfflineFile[] GetOfflines()
        {
            IList<IOfflineFile> rt = new List<IOfflineFile>();
            // For myVault offline file
            foreach (var i in App.DBFunctionProvider.ListMyVaultFile())
            {
                // filter out, but IsRevoked may be not ,for future release
                if (i.RmsIsDeleted == true || i.RmsIsRevoked == true)
                {
                    continue;
                }

                if (i.Is_Offline == true && FileHelper.Exist(i.LocalPath))
                {
                    rt.Add(new MyVaultFile(this,i));
                }
            }

            //  For myVault sharedWithMe offline file
            string cacheFolder = App.User.WorkingFolder + "\\SharedWithMe";
            foreach (var i in App.DBFunctionProvider.ListSharedWithMeFile())
            {
                if(i.Is_offline && File.Exists(i.Local_path))
                {
                    rt.Add(new SharedWithMeFile(App, cacheFolder, i));
                }
            }

            return rt.ToArray();
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            IList<IPendingUploadFile> rt = new List<IPendingUploadFile>();
            foreach (var i in App.DBFunctionProvider.ListMyVaultLocalFile())
            {
                // if status is error, del it
                if (!FileHelper.Exist(i.Nxl_Local_Path))
                {
                    App.DBFunctionProvider.DeleteMyVaultLocalFile(i.Nxl_Name);
                    continue;
                }
                if (IsMatchPendingUpload((EnumNxlFileStatus)i.Status))
                {
                    rt.Add(new MyVaultLocalAddedFile(i));
                }
            }           
            return rt.ToArray(); ;
        }

        public bool IsMatchPendingUpload(EnumNxlFileStatus status)
        {
            if (status == EnumNxlFileStatus.WaitingUpload ||
                status == EnumNxlFileStatus.Uploading ||
                status == EnumNxlFileStatus.UploadFailed ||
                status == EnumNxlFileStatus.UploadSucceed
                )
            {
                return true;
            }
            return false;
        }

        // Protect or Protect & Share normal file, recipients and comments will added when upload file
        public IMyVaultLocalFile AddLocalAdded(string path, List<FileRights> rights, 
            WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags, List<string> recipients, string comments)
        {
            string newAddedName = string.Empty;
            try
            {
                // tell api to convert to nxl by protect
                App.Log.Info("try to protect file to myVault: "+ path);
                var outPath = App.Rmsdk.User.ProtectFile(path, rights, waterMark, expiration, tags);

                // handle sdk nxl file
                string destFilePath = FileHelper.CreateNxlTempPath(WorkingFolder, "/", outPath);
                outPath = FileHelper.HandleAddedFile(destFilePath, outPath, out bool isOverWriteUpload,
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isExistInLocal = false;
                        IMyVaultLocalFile[] localFiles = ListLocalAdded();
                        isExistInLocal = localFiles.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        // think about search rms file exist:1. network connected---use api to search ?? 2. network outages---use db to search
                        // search rms file exist from db
                        bool isExistInRms = false;
                        IMyVaultFile[] rmsFiles = List();
                        isExistInRms = rmsFiles.Any(f => f.Nxl_Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        return isExistInLocal || isExistInRms;
                    },
                    (fileName) => {
                    // search local pendingUpload file exist from db
                    bool isCan = true;
                    IMyVaultLocalFile[] localFiles = ListLocalAdded();
                    IMyVaultLocalFile localFile = localFiles.FirstOrDefault(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                    if (localFile != null && localFile.Status == EnumNxlFileStatus.Uploading)
                    {
                        isCan = false;
                    }
                    return isCan;
                });

                // find this file in api
                //App.Log.Info("try to get the new protected file's fingerprint");
                //var fp = App.Rmsdk.User.GetNxlFileFingerPrint(outPath);
                // by osmond, feature changed, allow user to portect a file which has not any permissons for this user
                newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                App.Log.Info("store the new protected file into database");
                InsertLocalFile(newAddedName, outPath, File.GetLastAccessTime(outPath), recipients?.ToArray(), newAddedFileSize, EnumNxlFileStatus.WaitingUpload, comments, path,
                    JsonConvert.SerializeObject(new User.PendingUploadFileConfig() { overWriteUpload = isOverWriteUpload }));

                // Notify msg
                App.MessageNotify.NotifyMsg(newAddedName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Succeed"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.WaitingUpload);

                if (App.User.SelectedOption == 1)
                {
                    IMyVaultFile[] rmsFiles = List();
                    IMyVaultFile rmsFile = rmsFiles.FirstOrDefault(f => f.Nxl_Name.Equals(newAddedName, StringComparison.OrdinalIgnoreCase));

                    if (rmsFile != null)
                    {
                        if (App.User.LeaveCopy)
                        {
                            if (rmsFile.Is_Offline || rmsFile.Is_Edit || rmsFile.Status == EnumNxlFileStatus.Online /* fix bug 63618 */)
                            {
                                rmsFile.UpdateWhenOverwriteInLeaveCopy(EnumNxlFileStatus.Online, newAddedFileSize, File.GetLastWriteTime(outPath));
                            }
                        }
                        else
                        {
                            if (rmsFile.Is_Offline || rmsFile.Is_Edit)
                            {
                                rmsFile.Status = EnumNxlFileStatus.Online;
                            }
                        }

                        if (rmsFile.Is_Edit)
                        {
                            rmsFile.Is_Edit = false;
                        }
                    }

                }

                // return the new added
                return ListLocalAdded().First((i) =>
                {
                    return i.Name.Equals(newAddedName, StringComparison.OrdinalIgnoreCase);
                });
            }catch(Exception e)
            {
                App.Log.Error("Error occured when tring to protect file in myault" + e.Message, e);

                throw e;
            }
        }

        public void InsertLocalFile(string nxl_name, 
                                string nxl_local_path,
                                DateTime last_modified_time, 
                                string[] shared_with_list,
                                long size,
                                EnumNxlFileStatus status,
                                string comment,
                                string originalPath, string reserved1)
        {
            App.DBFunctionProvider.InsertMyVaultLocalFile(
                nxl_name, nxl_local_path, 
                last_modified_time,
                MyVaultLocalAddedFile.TranslateSharedWithArrToStr(shared_with_list),
                size, 
                (int)status,
                comment,
                originalPath, reserved1);
        }

        // called when merging files between local and remote, 
        // return an array contained the modified files: 
        //      a file that remote changed its some fields,but local not update it 
        //      a file that is new added at remote. 
        private MyVaultFileInfo[] FilterOutNotModified(IMyVaultFile[] locals, MyVaultFileInfo[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }
            var rt = new List<MyVaultFileInfo>();
            foreach (var i in remotes)
            {
                try
                {
                    // find in local
                    // if use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                    {
                        // When the same name file is overwrite, we should do update by comparing last modified time.
                        if (i.nxlName != j.Nxl_Name)
                        {
                            return false;
                        }
                        return true;
                    });

                    // if no matching element, will return null.
                    if (l == null)
                    {
                        App.Log.Info("MyVault local list no matching element");
                        // remote added node, should add into local.
                        rt.Add(i);
                        continue;
                    }


                    // Means the file has been overwritten from local in leave copy model, fix bug 63614.
                    if(l.Is_Offline && l.IsOverwriteFromLocal == 1) 
                    {
                        rt.Add(i);
                        // must reset after whole overwrite flow complete: when overwrite and upload complete, then do refresh\sync, will enter here.
                        l.IsOverwriteFromLocal = 0;
                        continue;
                    }

                    //
                    // For test ---> Note: java time(returned by rms) and c# time (local in rmd client) are different,
                    // and need to convert between them.
                    //

                    /*
                    var java_dt_utc = SkydrmLocal.rmc.common.helper.JavaTimeConverter.ToCSDateTime(i.sharedTime).ToUniversalTime();
                    var local_dt_utc = l.Shared_Time;

                    var java_dt = JavaTimeConverter.ToCSDateTime(i.sharedTime);
                    var java_dt_local = java_dt.ToLocalTime();
                    var local_dt_local = l.Shared_Time.ToLocalTime();

                    var mill_local_dt_utc = local_dt_utc.Millisecond;
                    var mill_java_dt = java_dt.Millisecond;

                    var s_l = local_dt_utc.ToString();
                    var s_i = java_dt.ToString();

                    var s_l_ = local_dt_utc.ToLongDateString();
                    var s_i_ = java_dt.ToLongDateString();

                    var s_l_1 = local_dt_utc.ToLongTimeString();
                    var s_i_1 = java_dt.ToLongTimeString();

                    var s_l_2 = local_dt_utc.ToShortDateString();
                    var s_i_2 = java_dt.ToShortDateString();

                    var s_l_3 = local_dt_utc.ToShortTimeString();
                    var s_i_3 = java_dt.ToShortTimeString(); 

                    */

                    // Judege whether modified, only care is_removed, is_deleted, is_shared
                    if ((i.isDeleted == 1) != l.Is_Deleted ||
                        (i.isRevoked) == 1 != l.Is_Revoked ||
                        (i.isShared == 1) != l.Is_Shared ||
                        !i.sharedWithList.Equals(l.Shared_With_List, StringComparison.CurrentCultureIgnoreCase) ||
                        // Note compare sharedTime for myVault (lastModified always is 0)
                        JavaTimeConverter.ToCSDateTime(i.sharedTime).ToString() != l.Shared_Time.ToString()
                        ) 
                    {

                        // Record the dirty item when detecting its "LastModified" changed.
                        // Used for the file is overwrite in remote.
                        if (l.Is_Offline
                            && JavaTimeConverter.ToCSDateTime(i.sharedTime).ToString() != l.Shared_Time.ToString())
                        {
                            if (!IsDataDirtyMasked(i.pathId) && !IsDataModifyDirtyMask(i.pathId))
                            {
                                mDirty_RecordingList.Add(i.pathId);
                            }

                            if (IsDataModifyDirtyMask(i.pathId))
                            {
                                rt.Add(i);
                                RemoveModifyMaskRecord(i.pathId);
                            }
                        }
                        else
                        {
                            rt.Add(i);
                        }
                    }
                }
                catch (Exception e)
                {
                    App.Log.Error(e);
                    // local find error
                    rt.Add(i);
                }
            }
            return rt.ToArray();
        }
        
        private IMyVaultFile[] Impl_List(bool filterOutDelted = false,
                                bool filterOutRevoked = false,
                                bool bInitFileObjAsync = false)
        {
            var rt = new List<MyVaultFile>();
            foreach (var i in App.DBFunctionProvider.ListMyVaultFile())
            {
                if (filterOutDelted && i.RmsIsDeleted)
                {
                    continue;
                }
                if (filterOutRevoked && i.RmsIsRevoked)
                {
                    continue;
                }
                // impl auto fix, we can not del this item, but we can change its status in constructor
                rt.Add(new MyVaultFile(this, i));             
            }
            // by osmond, I made a mistake by impled some logic code in MyVaultFile's contructor
            // which may in time-consuming
            // init the list at background
            if (bInitFileObjAsync)
            {
                ThreadPool.QueueUserWorkItem((theList) =>
                {
                    bool bTellUI = false;
                    foreach (var i in (List<MyVaultFile>)theList)
                    {
                        var modified=i.Initialize();
                        if (modified)
                        {
                            bTellUI = true;
                        }
                    }
                    if (bTellUI)
                    {
                        // Dangerous code, will result in infinite loops if not control well.
                        App.InvokeEvent_MyVaultOrSharedWithmeFileLowLevelUpdated();
                    }

                }, rt);
            }
            else
            {
                foreach (var j in rt)
                {
                    j.Initialize();
                }
            }
            return rt.ToArray();
        }

        public bool CheckFileExists(string pathId)
        {
            App.Rmsdk.User.MyVaultFileIsExist(pathId, out bool rt);
            return rt;
        }
    }

    public sealed class MyVaultFile : IMyVaultFile, IOfflineFile
    {
        private database2.table.myvault.MyVaultFile raw;
        private MyVault myVaultHost;
        private InternalFileInfo fileInfo;  // each get will generate a new-one
        private string partialLocalPath;
        private bool isDirty;

        public MyVaultFile(MyVault host,database2.table.myvault.MyVaultFile raw)
        {
            this.myVaultHost = host;
            this.raw = raw;          
        }
       
        /// <summary>
        /// Check file status if is modified, return true if some fields has been modified, or else return false.
        /// Check following two case:
        /// 1. File if has been removed for marked offline file, if yes, will do auto fix.
        /// 2. Handle Leave copy if "Leave a copy" switch is on.
        /// </summary>
        public bool Initialize()
        {
            bool modified = false;
            // auto fix
            if (Is_Offline && !FileHelper.Exist(raw.LocalPath))
            {
                Status = EnumNxlFileStatus.Online;
                Nxl_Local_Path = "";
                modified = true;
            }
            // imple leave a copy   
            bool modified2 = ImplLeaveCopy();
            return modified || modified2;
        }

        #region Impl IMyVaultFile
        public string Path_Id { get => raw.RmsPathId; }

        public string Display_Path { get => raw.RmsDisplayPath; }

        public string Repo_Id { get => raw.RmsRepoId; }

        public string Duid { get => raw.RmsDuid; }

        public string Nxl_Name { get => raw.RmsName; }

        public DateTime Last_Modified_Time { get => raw.RmsLastModifiedTime; }

        public DateTime Creation_Time { get => raw.RmsCreationTime; }

        public DateTime Shared_Time { get => raw.RmsSharedTime; }

        public string Shared_With_List { get => raw.RmsSharedWith; }

        public bool Is_Deleted { get => raw.RmsIsDeleted; }

        public bool Is_Revoked { get => raw.RmsIsRevoked; }

        public bool Is_Shared { get => raw.RmsIsShared; set => UpdateShareStatus(value); }

        public string Nxl_Local_Path { get => raw.LocalPath; set => UpdateLocalPath(value); }

        public string Partial_Local_Path
        {
            get
            {
                if (string.IsNullOrEmpty(partialLocalPath))
                {
                    partialLocalPath = GetPartialLocalPath();
                }

                if (!FileHelper.Exist(partialLocalPath))
                {
                    if (FileHelper.Exist(raw.LocalPath))
                    {
                        partialLocalPath = raw.LocalPath;
                    }
                    else
                    {
                        partialLocalPath = "";
                    }
                }

                return partialLocalPath;
            }
        }

        public bool Is_Offline { get => raw.Is_Offline; set => UpdateOffline(value); }

        public bool Is_Edit
        {
            get => raw.Edit_Status != 0;
            set => UpdateEditStatus(value ? 1 : 0);
        }

        public bool Is_ModifyRights
        {
            get => raw.Modify_Rights_Status != 0;
            set => UpdateModifyRightsStatus(value ? 1 : 0);
        }

        public bool Is_Dirty
        {
            get
            {
                isDirty = MyVault.IsDataDirtyMasked(Path_Id);
                if (isDirty)
                {
                    Console.WriteLine("Found target data with rmspathid = {0} is the dirty data list.", Path_Id);
                }
                return isDirty;
            }

            set
            {
                isDirty = value;
                if (!isDirty)
                {
                    bool ret = MyVault.RemoveDirtyMask(Path_Id);
                    if (ret)
                    {
                        Console.WriteLine("Remove target data with rmspathid = {0} from the dirty data list.", Path_Id);
                    }
                }
            }
        }

        public int IsOverwriteFromLocal
        {
            get
            {
                bool result = int.TryParse(raw.Reserved1, out int reserved);
                return reserved;
            }

            set
            {
                raw.Reserved1 = Convert.ToString(value);
                // update into db
                SkydrmApp.Singleton.DBFunctionProvider.UpdateMyVaultFile_IsOverwriteFromLocal(raw.Id, value);
            }
        }

        public void UpdateWhenOverwriteInLeaveCopy(EnumNxlFileStatus fStatus, long fSize, DateTime fLastModifed)
        {
            Status = fStatus;
            raw.RmsSize = (int)fSize;
            raw.RmsLastModifiedTime = fLastModifed;
            IsOverwriteFromLocal = 1;

            // update into db
            SkydrmApp.Singleton.DBFunctionProvider.UpdateMyVaultFileWhenOverwriteInLeaveCopy(raw.Id, (int)fStatus,fSize, fLastModifed);
        }

        public void Download(bool isViewOnly = false)
        {
            var App = SkydrmApp.Singleton;
            //feature check
            if (Is_Deleted)
            {
                // - is_Delete file can not be downloaded
                App.ShowBalloonTip("The file " + raw.RmsName + " has been removed from MyVault permanently.", false, raw.RmsName);
                throw new Exception("The file " + raw.RmsName + " in myVault has been deleted.");
            }

            string downloadPath = myVaultHost.WorkingFolder + "\\" + Nxl_Name;
            //before download delete exist
            FileHelper.Delete_NoThrow(downloadPath);
            UpdateStatus(EnumNxlFileStatus.Downloading);

            try
            {
                DownlaodMyVaultFileType type = isViewOnly ? DownlaodMyVaultFileType.ForVeiwer : DownlaodMyVaultFileType.ForOffline;
                // download 
                string targetPath = myVaultHost.WorkingFolder;
                App.Rmsdk.User.DownloadMyVaultFile(Path_Id, ref targetPath, type);
                
                // check out path, if file name exceed 128 characters, the server return name will be truncated.
                if (!downloadPath.Equals(targetPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    // use for delete file.
                    downloadPath = targetPath;
                    throw new SkydrmException(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_FileName128"),
                        ExceptionComponent.FEATURE_PROVIDER);
                }

                UpdateLocalPath(downloadPath);
                UpdateStatus(EnumNxlFileStatus.DownLoadedSucceed);
            }
            catch (Exception e)
            {
                App.Log.Error("failed downlond file=" + downloadPath, e);
                UpdateStatus(EnumNxlFileStatus.DownLoadedFailed);

                // del
                FileHelper.Delete_NoThrow(downloadPath);
                throw;
            }
        }

        public void DownloadPartial()
        {
            var app = SkydrmApp.Singleton;
            //feature check
            if (Is_Deleted)
            {
                // - is_Delete file can not be downloaded
                app.ShowBalloonTip("The file " + raw.RmsName + " has been removed from MyVault permanently.", false, raw.RmsName);
                throw new Exception("The file " + raw.RmsName + " in myVault has been deleted.");
            }

            // File name is attached prefix "partial" returned by sdk.
            var partialFPath = myVaultHost.WorkingFolder + "\\" + "partial_" + Nxl_Name;

            //before download delete exist
            FileHelper.Delete_NoThrow(partialFPath);

            app.Log.Info("partical downlaod path: " + partialFPath);
            try
            {
                // download 
                string targetPath = myVaultHost.WorkingFolder;
                app.Rmsdk.User.DownloadMyVaultPartialFile(Path_Id, ref targetPath, sdk.DownlaodMyVaultFileType.ForVeiwer);

                partialFPath = targetPath;

                partialLocalPath = partialFPath;
            }
            catch (Exception e)
            {
                app.Log.Error("failed partial downlond file=" + partialFPath, e);
                FileHelper.Delete_NoThrow(partialFPath);
                
                throw e;
            }
        }

        public void GetNxlHeader()
        {
            var app = SkydrmApp.Singleton;
            //feature check
            if (Is_Deleted)
            {
                // - is_Delete file can not be downloaded
                app.ShowBalloonTip("The file " + raw.RmsName + " has been removed from MyVault permanently.", false, raw.RmsName);
                throw new Exception("The file " + raw.RmsName + " in myVault has been deleted.");
            }

            // File name is attached prefix "partial" returned by sdk.
            var partialFPath = myVaultHost.WorkingFolder + "\\" + "partial_" + Nxl_Name;

            //before download delete exist
            FileHelper.Delete_NoThrow(partialFPath);

            app.Log.Info("partical downlaod path: " + partialFPath);
            try
            {
                // download 
                string workingfolder = myVaultHost.WorkingFolder;
                partialLocalPath = app.Rmsdk.User.MyVaultGetNxlFileHeader(Path_Id, workingfolder);
            }
            catch (Exception e)
            {
                app.Log.Error("failed in GetNxlHeader=" + partialFPath, e);
                FileHelper.Delete_NoThrow(partialFPath);
                throw e;
            }
        }

        // Now used to do unmark.
        public void Remove()
        {
            var app = SkydrmApp.Singleton;
            try
            {
                // Delete local downloaded file.
                string local_path = Nxl_Local_Path;
                if (local_path.Equals("") || local_path == null)
                {
                    return;
                }
                if (!File.Exists(local_path))
                {
                    return;
                }
                FileHelper.Delete_NoThrow(local_path);
                // update db
                UpdateStatus(EnumNxlFileStatus.RemovedFromLocal);
            }
            catch (Exception e)
            {
                app.Log.Error("remove file filed,path=" + Nxl_Local_Path, e);
                throw;
            }
        }

        public void ChangeSharedWithList(string[] emails)
        {
            var l = "";
            foreach (var e in emails)
            {
                l += e + ",";
            }
            if (l.EndsWith(","))
            {
                l = l.Substring(0, l.Length - 1);
            }

            // update in local
            if (raw.RmsSharedWith.Equals(l, StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }
            SkydrmApp.Singleton.DBFunctionProvider.UpdateMyVaultFileSharedWithList(raw.Id, l);
            raw.RmsSharedWith = l;
        }

        public void Export(string destinationFolder)
        {
            var App = SkydrmApp.Singleton;

            App.Log.Info("MyVault Export As file,path=" + destinationFolder);

            string currentUserTempPathOrDownloadFilePath = System.IO.Path.GetTempPath();

            //feature check
            if (Is_Deleted)
            {
                // - is_Delete file can not be downloaded
                App.ShowBalloonTip("The file " + raw.RmsName + " has been removed from MyVault permanently.", false, raw.RmsName);
                throw new Exception("The file " + raw.RmsName + " in myVault has been deleted.");
            }

            try
            {
                App.Rmsdk.User.CopyNxlFile(Name, Path_Id, NxlFileSpaceType.my_vault, "",
                   Path.GetFileName(destinationFolder), currentUserTempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                   true);

                // by commend, sdk will help us to record log: DownloadForOffline
                //App.User.AddNxlFileLog(raw.LocalPath, NxlOpLog.Download, true);
                // download 
                //App.Rmsdk.User.DownloadMyVaultFile(Path_Id, ref currentUserTempPathOrDownloadFilePath, sdk.DownlaodMyVaultFileType.Normal);
                string downloadFilePath = currentUserTempPathOrDownloadFilePath + Path.GetFileName(destinationFolder);
                File.Copy(downloadFilePath, destinationFolder, true);
            }
            catch (Exception e)
            {
                App.Log.Error("failed SaveAs file=" + Nxl_Local_Path, e);
                // del                
                throw;
            }
            finally
            {
                FileHelper.Delete_NoThrow(currentUserTempPathOrDownloadFilePath + Path.GetFileName(destinationFolder));
            }
        }

        public MyVaultMetaData GetMetaData()
        {
            var app = SkydrmApp.Singleton;

            string localPath = Partial_Local_Path;
            string pathId = Path_Id;

            //Sanity check.
            if (string.IsNullOrEmpty(localPath))
            {
                throw new Exception("The argument localPath is null.");
            }
            if (string.IsNullOrEmpty(pathId))
            {
                throw new Exception("Illegal argument found,maybe transfer issue.");
            }

            return app.Rmsdk.User.GetMyVaultFileMetaData(localPath, pathId);
        }

        public void ShareFile(string nxlLocalPath, string[] recipentAdds, string[] recipentRmoves, string comments)
        {
            // if file is protected upload should invoke share repository api.
            // through isShared field we can find out whether this file is shared upload or protect upload or not.
            if (Is_Shared)
            {
                //Update recipents will be fine.
                UpdateRecipents(nxlLocalPath, recipentAdds, recipentRmoves, comments);
            }
            else
            {
                // Share repository.
                ShareRepository(nxlLocalPath, recipentAdds, comments);
                Is_Shared = true;
            }
        }
        #endregion // Impl IMyVaultFile

        #region Impl IOffline 
        public string Name => Nxl_Name;

        public string LocalDiskPath => Nxl_Local_Path;

        public string RMSRemotePath => Display_Path;

        public DateTime LastModifiedTime => Last_Modified_Time;

        public bool IsOfflineFileEdit => Is_Edit;

        public void RemoveFromLocal()
        {
            Remove();
        }
        #endregion // Impl IOffline

        #region Impl common method
        public long FileSize { get => raw.RmsSize; }

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.Status; set => UpdateStatus(value); }
        #endregion // Impl common method

        #region private methods
        private string GetPartialLocalPath()
        {
            return myVaultHost.WorkingFolder + "\\" + "partial_" + Nxl_Name;
        }
        
        private void UpdateOffline(bool offline)
        {
            //Sanity check.
            if (raw.Is_Offline == offline)
            {
                return;
            }
            //Update offline marker in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultFileOffline(raw.Id, offline);
            //Update obj raw's offline marker.
            raw.Is_Offline = offline;
        }

        private void UpdateShareStatus(bool newStatus)
        {
            if(raw.RmsIsShared == newStatus)
            {
                return;
            }
            //Update offline marker in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultFileShareStatus(raw.Id, newStatus);
            raw.RmsIsShared = newStatus;
        }

        private void UpdateStatus(EnumNxlFileStatus status)
        {    
            // sanity check
            if (raw.Status == (int)status)
            {
                return;
            }
            //Update vaultfile status in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultFileStatus(raw.Id, (int)status);
            raw.Status = (int)status;
            if (status == EnumNxlFileStatus.Online)
            {
                Is_Offline = false;
            }
            if(status == EnumNxlFileStatus.AvailableOffline)
            {
                Is_Offline = true;
            }
        }

        private void UpdateEditStatus(int newStatus)
        {
            // Check changable first.
            if (raw.Edit_Status == newStatus) {
                return;
            }
            var app = SkydrmApp.Singleton;
            // Update db.
            app.DBFunctionProvider.UpdateMyVaultFileEditStatus(raw.Id, newStatus);
            // Update cache.
            raw.Edit_Status = newStatus;
        }

        private void UpdateModifyRightsStatus(int newStatus)
        {
            // Check changable first.
            if (raw.Modify_Rights_Status == newStatus)
            {
                return;
            }
            var app = SkydrmApp.Singleton;
            // Update db.
            app.DBFunctionProvider.UpdateMyVaultFileModifyRightsStatus(raw.Id, newStatus);
            // Update cache.
            raw.Modify_Rights_Status = newStatus;
        }
       
        private void UpdateLocalPath(string local_path)
        {
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultFileLocalPath(raw.Id, local_path);
            raw.LocalPath = local_path;
        }

        private bool ImplLeaveCopy()
        {
            bool modified = false;
            var leaveACopy = SkydrmApp.Singleton.User.LeaveCopy_Feature;
            if (leaveACopy.Exist(raw.RmsName, myVaultHost.WorkingFolder))
            {
                // mark this file as local cached
                var newLocalPath = myVaultHost.WorkingFolder + "\\" + Nxl_Name;
                if (leaveACopy.MoveTo(myVaultHost.WorkingFolder, Nxl_Name))
                {
                    // update this file status
                    UpdateLocalPath(newLocalPath);
                    Is_Offline = true;
                    UpdateStatus(EnumNxlFileStatus.CachedFile);
                    modified = true;
                }

            }
            return modified;
        }

        private void ShareRepository(string nxlLocalPath, string[] recipents, string comments)
        {
            var app = SkydrmApp.Singleton;

            string repoId = Repo_Id;
            string fName = Nxl_Name;
            string fpId = Path_Id;
            string fp = Display_Path;

            app.Rmsdk.User.MyVaultShareFile(nxlLocalPath, recipents, repoId, fName, fpId, fp, comments);
        }

        private void UpdateRecipents(string nxlLocalPath, string[] addRecipents,string[] removedRecipents, string comment)
        {
            var app = SkydrmApp.Singleton;

            List<string> added = addRecipents.ToList();
            List<string> removed = removedRecipents.ToList();

            app.Rmsdk.User.UpdateRecipients(nxlLocalPath, added, removed, comment);
        }
        #endregion // private methods

        private class InternalFileInfo : FileInfoBaseImpl
        {
            private MyVaultFile Outer;

            public InternalFileInfo(MyVaultFile outer) : base(outer.Partial_Local_Path) //Use Partial_LocalPath get Rights, Change 'LocalPath' to 'Partial_LocalPath'
            {
                Outer = outer;
            }
            public override string Name => Outer.Name;

            public override long Size => Outer.FileSize;

            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string RmsRemotePath => Outer.RMSRemotePath;

            public override bool IsCreatedLocal => false;

            public override string[] Emails => GetEmails();

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_MYVAULT;
        
            private string[] GetEmails()
            {
                return Outer.Shared_With_List.Split(new char[] { ' ', ';',',' });
            }

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }

        }

    }

    // User add it in local, once it has been uploaded to RMS, it should be deleted in DB and local Disk
    public sealed class MyVaultLocalAddedFile : IMyVaultLocalFile
    {
        private database.table.myvault.MyVaultLocalFile raw;
        private InternalFileInfo fileInfo; // each get will generate a new-one

        private User.PendingUploadFileConfig pendingFileConfig;

        public MyVaultLocalAddedFile(database.table.myvault.MyVaultLocalFile raw)
        {
            this.raw = raw;

            if (string.IsNullOrEmpty(raw.Reserved1))
            {
                pendingFileConfig = new User.PendingUploadFileConfig();
            }
            else
            {
                pendingFileConfig = JsonConvert.DeserializeObject<User.PendingUploadFileConfig>(raw.Reserved1);
            }
        }

        #region IMyVaultLocalFile used
        public string[] Nxl_Shared_With_List { get => TranlateSharedWithFromStr(raw.Shared_With_List); set => UpdateSharedWith(value); }
        public void ChangeSharedWithList(string[] emails)
        {
            Nxl_Shared_With_List = emails;
        }
        #endregion

        #region For upload
        private string Comment => raw.Comment;
        /// <summary>
        /// File source path before encryption
        /// </summary>
        public string OriginalPath { get => raw.OriginalPath; set => UpdateOriginalPath(value); }
        #endregion

        #region IPendingUploadFile used
        public string Name { get => raw.Nxl_Name; set => UpdateName(value); }

        public string LocalDiskPath { get => raw.Nxl_Local_Path; set => UpdatePath(value); }

        // todo: wait for henry to modify Last_Modified_Time as DateTime type
        public DateTime LastModifiedTime => raw.Last_Modified_Time;

        public string DisplayPath => "/"+raw.Nxl_Name;

        public string PathId => "";

        public string SharedEmails => raw.Shared_With_List;

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public long FileSize { get => raw.Size; }

        public EnumFileRepo FileRepo { get => EnumFileRepo.REPO_MYVAULT; }

        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.Status; set => UpdateStatus(value); }

        public bool OverWriteUpload
        {
            get => pendingFileConfig.overWriteUpload;
            set
            {
                if (pendingFileConfig.overWriteUpload == value)
                {
                    return;
                };
                pendingFileConfig.overWriteUpload = value;
                UpdateFileConfig();
            }
        }

        public bool IsExistInRemote
        {
            get => pendingFileConfig.isExistInRemote;
            set
            {
                if (pendingFileConfig.isExistInRemote == value)
                {
                    return;
                };
                pendingFileConfig.isExistInRemote = value;
                UpdateFileConfig();
            }
        }

        public void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            try
            {
                if (OverWriteUpload)
                {
                    isOverWrite = true;
                }

                string locaPath = LocalDiskPath ?? "";
                //check file exists.
                if (!FileHelper.Exist(locaPath))
                {
                    throw new Exception("Ileagal local file path state.");
                }

                // Uplaod
                var App = SkydrmApp.Singleton;

                App.Log.InfoFormat("###Call Upload MyVault File api, NxlLocalPath:{0}, OriginalPath:{1}, SharedEmails:{2}, Comment:{3}", LocalDiskPath, OriginalPath, SharedEmails, Comment);
                // For protected and share file
                App.Rmsdk.User.UploadMyVaultFile(LocalDiskPath, Path.GetFileName(OriginalPath), SharedEmails, Comment, isOverWrite);

                // delete in db
                App.DBFunctionProvider.DeleteMyVaultLocalFile(Name);

                // tell ServiceMgr -- Do this after Auto Remove (So invoking this in high level).
                //App.MessageNotify.NotifyMsg(Nxl_Name, "Upload successfully", EnumMsgNotifyType.LogMsg,
                //    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);

                // Every done, begin impl leave a copy featue
                if (App.User.LeaveCopy)
                {
                    App.User.LeaveCopy_Feature.AddFile(locaPath);
                    FileHelper.Delete_NoThrow(LocalDiskPath);
                }
            }
            catch (RmRestApiException ex)
            {
                // Handle myVault upload file 4001(file exist) exception
                if (ex.MethodKind == RmSdkRestMethodKind.Upload
                    && ex.ErrorCode == 4001)
                {
                    IsExistInRemote = true;
                }

                SkydrmApp.Singleton.MessageNotify.NotifyMsg(Name, ex.Message, EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);

                throw;
            }
            catch (Exception)
            {
                SkydrmApp.Singleton.MessageNotify.NotifyMsg(Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Upload_Failed"), EnumMsgNotifyType.LogMsg,
                 MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);

                throw;
            }
        }

        public void RemoveFromLocal()
        {
            // Delele in local disk
            var App = SkydrmApp.Singleton;
            if (FileHelper.Exist(raw.Nxl_Local_Path))
            {
                FileHelper.Delete_NoThrow(raw.Nxl_Local_Path);
            }
            else
            {
                App.Log.Warn("file to be del,but not in local, " + raw.Nxl_Local_Path);
            }
            //Delete in db.
            App.DBFunctionProvider.DeleteMyVaultLocalFile(Name);
            //Delete in api.
            App.Rmsdk.User.RemoveLocalGeneratedFiles(Name);

            // Fix bug 51938, If LeaveCopy,should delete file in LeaveCopy Folder
            if (App.User.LeaveCopy)
            {
                var leaveACopy = SkydrmApp.Singleton.User.LeaveCopy_Feature;
                if (leaveACopy.Exist(Name, "", LocalDiskPath))
                {
                    leaveACopy.DeleteFile(LocalDiskPath);
                }
            }
        }
        #endregion

        public static string TranslateSharedWithArrToStr(string[] sharedWithList)
        {
            if (sharedWithList == null || sharedWithList.Length == 0)
            {
                return "";
            }
            StringBuilder rt = new StringBuilder();
            for (int i = 0; i < sharedWithList.Length; i++)
            {
                rt.Append(sharedWithList[i]);
                if (i != sharedWithList.Length - 1)
                {
                    rt.Append(";");
                }
            }
            return rt.ToString();
        }

        #region Private methods
        private void UpdateStatus(EnumNxlFileStatus status)
        {
            //Sanity check
            //If no changes just return.
            if (raw.Status == (int)status)
            {
                return;
            }
            //update status in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultLocalFileStatus(Name, (int)status);
            raw.Status = (int)status;

            //NotifyIRecentTouchedFile(status);
        }

        private void UpdateSharedWith(string[] sharedWithList)
        {
            string results = TranslateSharedWithArrToStr(sharedWithList);
            string local = raw.Shared_With_List;
            if (results.Equals(local))
            {
                return;
            }
            //update new resutls passed.
            //update sharedwithlist in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultLocalFileSharedWithList(Name, results);
            raw.Shared_With_List = results;
        }

        private string[] TranlateSharedWithFromStr(string shared_with_list)
        {
            string[] sharedWithArr = shared_with_list.Split(new char[] { ';' });
            return sharedWithArr;
        }

        private void UpdateOriginalPath(string path)
        {
            string local = raw.OriginalPath;
            if (path.Equals(local))
            {
                return;
            }
            //update new resutls passed.
            //update originalPath in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyVaultLocalFileOriginalPath(Name, path);
            raw.OriginalPath = path;
        }

        private void UpdateName(string name)
        {
            //Sanity check
            //If no changes just return.
            if (raw.Nxl_Name.Equals(name))
            {
                return;
            }
            //update name in db.
            SkydrmApp.Singleton.DBFunctionProvider.UpdateMyVaultLocalFileName(raw.Id, name);
            raw.Nxl_Name = name;
        }

        private void UpdatePath(string path)
        {
            //Sanity check
            //If no changes just return.
            if (raw.Nxl_Local_Path.Equals(path))
            {
                return;
            }
            //update path in db.
            SkydrmApp.Singleton.DBFunctionProvider.UpdateMyVaultLocalFilePath(raw.Id, path);
            raw.Nxl_Local_Path = path;
        }

        private void UpdateFileConfig()
        {
            SkydrmApp.Singleton.DBFunctionProvider.UpdateMyVaultLocalFileReserved1(raw.Id,
                JsonConvert.SerializeObject(pendingFileConfig));
        }
        #endregion

        private class InternalFileInfo : FileInfoBaseImpl
        {
            private MyVaultLocalAddedFile Outer;

            public InternalFileInfo(MyVaultLocalAddedFile outer): base(outer.LocalDiskPath)
            {
                Outer = outer;
            }

            public override long Size => Outer.FileSize;

            public override string Name => Outer.Name;

            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string RmsRemotePath => Outer.DisplayPath;

            public override bool IsCreatedLocal => true;

            public override string[] Emails => Outer.Nxl_Shared_With_List;

            public override EnumFileRepo FileRepo => Outer.FileRepo;

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }
        }

    }
}
