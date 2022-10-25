using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Newtonsoft.Json;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.database.table.myspace;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using static Skydrmlocal.rmc.database2.FunctionProvider;
using static SkydrmLocal.rmc.sdk.User;

namespace SkydrmDesktop.rmc.featureProvider.MySpace
{
    public sealed class MyDrive : IMyDrive
    {
        private SkydrmApp App;
        private log4net.ILog log;
        public string WorkingFolder { get; }

        private static List<string> mDirty_RecordingList = new List<string>();
        private static List<string> mDirty_ModifyList = new List<string>();

        public MyDrive(SkydrmApp app)
        {
            this.App = app;
            this.log = app.Log;
            WorkingFolder = App.User.WorkingFolder + "\\MyDrive";
            if(!Directory.Exists(WorkingFolder))
            {
                Directory.CreateDirectory(WorkingFolder);
            }
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

        public IMyDriveFile[] ListAll(bool toDisplay = false)
        {
            try
            {
                var rt = new List<MyDriveFile>();
                var retDb = App.DBFunctionProvider.ListAllMyDriveFiles();
                foreach (var i in retDb)
                {
                    rt.Add(new MyDriveFile(this, i, toDisplay));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public IMyDriveFile[] List(string folderId)
        {
            try
            {
                var rt = new List<MyDriveFile>();
                var retDb = App.DBFunctionProvider.ListMyDriveFiles(folderId);
                foreach (var i in retDb)
                {
                    rt.Add(new MyDriveFile(this, i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public IMyDriveFile[] Sync(string folderId)
        {
            // remote nodes
            var remote = App.Rmsdk.User.ListMyDriveFiles(folderId);
            // local nodes
            var local = List(folderId);

            // Will do some merge as following 1 & 2.
            // 1. delete file that had been deleted on remote but still in local --> local also should delete them.
            var diffset = from i in local
                          let rIds = from j in remote select j.pathId
                          where !rIds.Contains(i.PathId)
                          select i;

            foreach (var i in diffset)
            {
                App.DBFunctionProvider.DeleteMyDriveFile(i.PathId);

                // if this file is a folder, remove all its sub fiels
                if (i.IsFolder)
                {
                    App.DBFunctionProvider.DeleteMyDriveFolderAndSubChildren(i.PathId);

                    // Note: if later workspace support mem cache, should also delete the folder's workspaceLocalFiles(waiting for upload files)
                    // in mem cache; Because even though local db will be deleted by cascading, but mem cache can't.
                }
            }

            // 2. remote added\modified some nodes but local don't ---> local also should added\modified them.
            var ff = new List<InsertMyDriveFile>();
            foreach (var f in FilterAddedOrModifiedInRemote(local, remote))
            {
                ff.Add(new InsertMyDriveFile()
                {
                    pathId = f.pathId,
                    pathDisplay = f.pathDisplay,
                    name = f.name,
                    size = (long)f.size,
                    lastModified = (long)f.lastModified,
                    isFolder = (int)f.isFolder
                });
            }

            // Insert\update
            App.DBFunctionProvider.UpsertMyDriveFileBatch(ff.ToArray());

            // Insert faked root node
            App.DBFunctionProvider.InsertMyDriveFakedRoot();

            return List(folderId);
        }

        public void UploadFile(string fileLocalPath, string destFolder, bool overwrite = false)
        {
            try
            {
                App.Rmsdk.User.MyDriveUploadFile(fileLocalPath, destFolder, overwrite);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void CreateFolder(string name, string parantFolder)
        {
            var rt = App.Rmsdk.User.MyDriveCreateFolder(name, parantFolder);
            if (!rt)
            {
                throw new Exception("Create folder failed.");
            }
        }

        public IMyDriveLocalFile[] ListLocalAdded(string folderId)
        {
            try
            {
                var rt = new List<MyDriveLocalAddedFile>();
                foreach (var i in App.DBFunctionProvider.ListMyDriveLocalFiles(folderId))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        App.DBFunctionProvider.DeleteMyDriveLocalFile(i.Id);
                        continue;
                    }

                    rt.Add(new MyDriveLocalAddedFile(i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public IMyDriveLocalFile AddLocalAdded(string parentFolder, string filePath)
        {
            try
            {
                bool isFileInUse = FileHelper.IsFileInUse(filePath);
                if (isFileInUse)
                {
                    throw new Exception("File is occupied.");
                }
                // handle sdk nxl file
                string destFilePath = FileHelper.CreateNxlTempPath(WorkingFolder, parentFolder, filePath, false);
                string tempFilePath = FileHelper.HandleAddedFile(destFilePath, filePath, out bool isOverWriteUpload,
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isExistInLocal = false;
                        IMyDriveLocalFile[] localFiles = ListLocalAdded(parentFolder);
                        isExistInLocal = localFiles.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        // think about search rms file exist:1. network connected---use api to search ?? 2. network outages---use db to search
                        // search rms file exist from db
                        bool isExistInRms = false;
                        IMyDriveFile[] rmsFiles = List(parentFolder);
                        isExistInRms = rmsFiles.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        return isExistInLocal || isExistInRms;
                    },
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isCan = true;
                        IMyDriveLocalFile[] localFiles = ListLocalAdded(parentFolder);
                        IMyDriveLocalFile localFile = localFiles.FirstOrDefault(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                        if (localFile != null && localFile.Status == EnumNxlFileStatus.Uploading)
                        {
                            isCan = false;
                        }
                        return isCan;
                    }, false, false);

                string name = Path.GetFileName(tempFilePath);
                long fileSize = new FileInfo(tempFilePath).Length;

                // insert into db
                App.DBFunctionProvider.InsertMyDriveLocalFile(name, tempFilePath, parentFolder, File.GetLastAccessTime(tempFilePath), fileSize,
                    JsonConvert.SerializeObject(new SkydrmLocal.rmc.featureProvider.User.PendingUploadFileConfig() { overWriteUpload = isOverWriteUpload }));

                // tell service mgr
                App.MessageNotify.NotifyMsg(name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_AddFileToMyDrive_Succeed"),
                    EnumMsgNotifyType.LogMsg, "Add file", EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.WaitingUpload);

                if (App.User.SelectedOption == 1)
                {
                    IMyDriveFile[] rmsFiles = List(parentFolder);
                    IMyDriveFile rmsFile = rmsFiles.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (rmsFile != null && rmsFile.IsOffline)
                    {
                        rmsFile.Status = EnumNxlFileStatus.Online;
                    }
                }

                // Get from db and return.
                return ListLocalAdded(parentFolder).First((i) => {
                    return i.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception e)
            {
                App.Log.Error("Failed to Add the file" + e.Message, e);
                throw;
            }
        }

        public IOfflineFile[] GetOfflines()
        {
            try
            {
                var rt = new List<IOfflineFile>();
                var retDb = App.DBFunctionProvider.ListAllMyDriveFiles();
                foreach (var i in retDb)
                {
                    if (!i.RmsIsFolder && i.IsOffline && FileHelper.Exist(i.LocalPath))
                    {
                        rt.Add(new MyDriveFile(this, i));
                    }
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            try
            {
                var rt = new List<IPendingUploadFile>();

                foreach (var i in App.DBFunctionProvider.ListAllMyDriveLocalFiles())
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        App.DBFunctionProvider.DeleteMyDriveLocalFile(i.Id);
                        continue;
                    }

                    rt.Add(new MyDriveLocalAddedFile(i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public void OnHeartBeat()
        {
            // Sync all nodes
            Stack<string> paths = new Stack<string>();
            paths.Push("/");
            while (paths.Count != 0)
            {
                (from f in Sync(paths.Pop())
                 where f.IsFolder == true
                 select f.PathId)
                 .ToList()
                 .ForEach((j) => { paths.Push(j); });
            }

            // todo:
            // Maybe should notify ui treeview do auto refresh when some folder is deleted.
        }

        // Remote added\modified some nodes but local don't
        private MyDriveFileInfo[] FilterAddedOrModifiedInRemote(IMyDriveFile[] locals, MyDriveFileInfo[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }

            var rt = new List<MyDriveFileInfo>();
            foreach (var i in remotes)
            {
                try
                {
                    // If use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                    {
                        if (i.pathId != j.PathId)
                        {
                            return false;
                        }
                        return true;
                    });

                    // If no matching element, will return null.
                    if (l == null)
                    {
                        App.Log.Info("WorkSpace local list no matching element");
                        // remote added node, should add into local
                        rt.Add(i);
                        continue;
                    }

                    // For test, Note: we can compare directly DateTime, since DateTime inner '!=' overloaded method is compared by Ticks, 
                    // which is more precise time, even though display time between local and remote node is the same, but its Ticks value maybe
                    // tiny different, so we compare by its ToString().
                    // 

                    /*
                    DateTime remote_lastmodified = SkydrmLocal.rmc.common.helper.JavaTimeConverter.ToCSDateTime((long)i.lastModified);
                    DateTime local_lastmodified = l.LastModifiedTime;

                    var str_remote_lastmodified = SkydrmLocal.rmc.common.helper.JavaTimeConverter.ToCSDateTime((long)i.lastModified).ToString();
                    var sr_local_lastmodified = l.LastModifiedTime.ToString();
                    */

                    // Modified in remote, local node should also update.
                    if (i.name != l.Name ||
                        (long)i.size != l.FileSize ||
                        SkydrmLocal.rmc.common.helper.JavaTimeConverter.ToCSDateTime((long)i.lastModified).ToString() != l.LastModifiedTime.ToString())
                    {
                        if (l.IsOffline)
                        {
                            // Record the dirty item when detecting its "LastModified" changed.
                            // Used for the file is overwrite in remote.
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

    }

    public sealed class MyDriveFile : IMyDriveFile
    {
        private SkydrmApp app = SkydrmApp.Singleton;
        private database.table.myspace.MyDriveFile raw;
        private MyDrive mydriveHost;
        private string cacheFolder;
        private bool isDirty;

        public MyDriveFile(MyDrive host, database.table.myspace.MyDriveFile r, bool toDisplay = false)
        {
            this.mydriveHost = host;
            this.raw = r;

            if (toDisplay)
            {
                return;
            }

            cacheFolder = mydriveHost.WorkingFolder + PathDisplay;
            if (IsFolder)
            {
                FileHelper.CreateDir_NoThrow(cacheFolder);
            }
            else
            {
                // Cache Folder always save a folder path without trail '\'.
                cacheFolder = FileHelper.GetParentPathWithoutTrailSlash_WorkAround(cacheFolder);
            }

            // auto fix
            AutoFixInConstruct();
        }

        #region Impl for IMyDriveFile
        public string PathId => raw.RmsPathId;

        public string PathDisplay => raw.RmsPathDisplay;

        public bool IsFolder => raw.RmsIsFolder;

        public bool IsNormalFile => true;

        public bool IsOffline { get => raw.IsOffline; set => UpdateOffline(value); }

        public bool IsFavorite { get => raw.IsFavorite; set => UpdateFavorite(value); }

        public bool Is_Dirty
        {
            get
            {
                isDirty = MyDrive.IsDataDirtyMasked(PathId);
                if (isDirty)
                {
                    Console.WriteLine("Found target data with rmspathid = {0} is the dirty data list.", PathId);
                }
                return isDirty;
            }

            set
            {
                isDirty = value;
                if (!isDirty)
                {
                    bool ret = MyDrive.RemoveDirtyMask(PathId);
                    if (ret)
                    {
                        Console.WriteLine("Remove target data with rmspathid = {0} from the dirty data list.", PathId);
                    }
                }
            }
        }

        public void DeleteItem()
        {
            var rt = app.Rmsdk.User.MyDriveDeleteItem(PathId);
            if (!rt) // Failed
            {
                throw new Exception("Delete item failed.");
            }

            // If delete succeed from rms, local also should do some deleting,
            // or else should do refresh ??
            try
            {
                if (IsFolder)
                {
                    // delete from local
                    if(Directory.Exists(PathId)) {
                        Directory.Delete(PathId, true);
                    }
                    // remove db record
                    app.DBFunctionProvider.DeleteMyDriveFolderAndSubChildren(PathId);
                }
                else
                {
                    // delete local file
                    var path = cacheFolder + "\\" + Name;
                    if (!File.Exists(path))
                    {
                        return;
                    }
                    File.Delete(cacheFolder + "\\" + Name);

                    // delete the file from db
                    app.DBFunctionProvider.DeleteMyDriveFile(PathId);
                }
            }
            catch (Exception e)
            {
                app.Log.Error(e.ToString());
                throw e;
            }
        }

        public void Download()
        {
            if (IsFolder)
            {
                return;
            }

            string downloadFilePath = cacheFolder + "\\" + raw.RmsNxlName;
            // update file status is: downloading
            UpdateStatus(EnumNxlFileStatus.Downloading);

            try
            {
                // delete previous file
                FileHelper.Delete_NoThrow(downloadFilePath, true);

                // call api
                string targetPath = cacheFolder;
                app.Rmsdk.User.MyDriveDownloadFile(raw.RmsPathId, ref targetPath);

                // check out path, if file name exceed 128 characters, the server return name will be truncated.
                if (!downloadFilePath.Equals(targetPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    // use for delete file.
                    downloadFilePath = targetPath;
                    throw new SkydrmException(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_FileName128"), 
                        ExceptionComponent.FEATURE_PROVIDER);
                }

                // update localpath to db
                OnChangeLocalPath(downloadFilePath);
                UpdateStatus(EnumNxlFileStatus.DownLoadedSucceed);
            }
            catch (Exception e)
            {
                app.Log.Error("failed in downlaod file=" + downloadFilePath, e);
                UpdateStatus(EnumNxlFileStatus.DownLoadedFailed);

                // del 
                FileHelper.Delete_NoThrow(downloadFilePath);

                throw;
            }
        }
        #endregion // Impl for IMyDriveFile

        #region Impl for IOfflineFile
        public string Name => raw.RmsNxlName;

        public long FileSize => raw.RmsSize;

        public EnumNxlFileStatus Status
        {
            get => (EnumNxlFileStatus)raw.Status; set => UpdateStatus(value);
        }

        public string LocalDiskPath => raw.LocalPath;

        public string RMSRemotePath => raw.RmsPathDisplay;

        public DateTime LastModifiedTime => raw.RmsLastModified;

        public bool IsOfflineFileEdit => false;

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public void RemoveFromLocal()
        {
            try
            {
                if (IsFolder)
                {
                    return;
                }

                // delete local
                var path = cacheFolder + "\\" + Name;
                if (!File.Exists(path))
                {
                    return;
                }
                try
                {
                    File.Delete(cacheFolder + "\\" + Name);
                }
                catch (Exception e)
                {
                    app.Log.Error(e.ToString());
                }

                // update file status -- also will update db
                Status = EnumNxlFileStatus.RemovedFromLocal;
            }
            catch (Exception e)
            {
                app.Log.Error(e.ToString());
                throw;
            }
        }
        #endregion // Impl for IOfflineFile

        #region private methods
        private void AutoFixInConstruct()
        {
            if (IsFolder)
            {
                return;
            }
            if (raw.IsOffline)
            {
                // require the file must exist
                var fPath = cacheFolder + "\\" + raw.RmsNxlName;
                if (raw.LocalPath.Equals(fPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!FileHelper.Exist(fPath))
                    {
                        // corrupted, set localpath is "" and is_offline is false
                        OnFixResetLocalPathAndOffline();
                    }
                }
                else
                {
                    // local path corrupted, reset it as "" and  is_offline is false
                    OnFixResetLocalPathAndOffline();
                }
            }
        }

        private void UpdateOffline(bool isOffline)
        {
            if (raw.IsOffline == isOffline)
            {
                return;
            }
            // Update offline marker in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyDriveFileOffline(raw.Id, isOffline);
            // Update obj raw's offline marker.
            raw.IsOffline = isOffline;
        }

        private void UpdateFavorite(bool isFavorite)
        {
            // todo
        }

        private void UpdateStatus(EnumNxlFileStatus status)
        {
            if (raw.Status == (int)status)
            {
                return;
            }
            // Update vaultfile status in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyDriveFileStatus(raw.Id, (int)status);
            raw.Status = (int)status;
            if (status == EnumNxlFileStatus.Online)
            {
                IsOffline = false;
            }
            if (status == EnumNxlFileStatus.AvailableOffline)
            {
                IsOffline = true;
            }
        }

        private void OnChangeLocalPath(string newPath)
        {
            if (raw.LocalPath.Equals(newPath))
            {
                return;
            }
            // update db
            app.DBFunctionProvider.UpdateMyDriveFileLocalPath(raw.Id, newPath);
            // update cache
            raw.LocalPath = newPath;
        }

        private void OnFixResetLocalPathAndOffline()
        {
            // offline
            app.DBFunctionProvider.UpdateMyDriveFileOffline(raw.Id, false);
            raw.IsOffline = false;
            // local path
            app.DBFunctionProvider.UpdateMyDriveFileLocalPath(raw.Id, "");
            raw.LocalPath = "";
            // operation status Online = 4 which indicates file is in remote.
            app.DBFunctionProvider.UpdateMyDriveFileStatus(raw.Id, (int)EnumNxlFileStatus.Online);
            raw.Status = (int)EnumNxlFileStatus.Online;
        }
        #endregion // private methods

        #region Inner class FileInfo
        private sealed class InternalFileInfo : FileInfoBaseImpl
        {
            private MyDriveFile outer;
            public InternalFileInfo(MyDriveFile outer) : base(outer.LocalDiskPath)
            {
                this.outer = outer;
            }

            // --- start--- Impl the abstract methods
            public override DateTime LastModified => outer.LastModifiedTime;

            public override string RmsRemotePath => outer.RMSRemotePath;

            public override bool IsCreatedLocal => false;

            public override string[] Emails => new string[0];

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_MYDRIVE;
            // --- end--- Impl the abstract methods

            // ---start--- must overwrite the below fields
            public override string Name => outer.Name;
            public override long Size => outer.FileSize;
            public override bool IsNormalFile => outer.IsNormalFile;
            public override bool IsOwner
            {
                get
                {
                    // Actually, mydrive file always is normal file, so we can directly return false.
                    if (IsNormalFile) return false;
                    else return base.IsOwner;
                }
            }
            public override bool HasAdminRights
            {
                get
                {
                    if (IsNormalFile) return false;
                    else return base.HasAdminRights;
                }
            }

            public override bool IsByAdHoc => false;
            public override bool IsByCentrolPolicy => false;
            public override FileRights[] Rights => new FileRights[0];
            public override string WaterMark => "";
            public override Expiration Expiration => new Expiration();
            public override Dictionary<string, List<string>> Tags => null;
            public override string RawTags => "";

            // ---end--- must overwrite the below fields
        }
        #endregion // Inner class FileInfo
    }

    // use for add normal file to upload manager
    public sealed class MyDriveLocalAddedFile : IMyDriveLocalFile
    {
        private database.table.myspace.MyDriveLocalFile raw;

        private SkydrmLocal.rmc.featureProvider.User.PendingUploadFileConfig pendingFileConfig;

        public MyDriveLocalAddedFile(database.table.myspace.MyDriveLocalFile raw)
        {
            this.raw = raw;

            if (string.IsNullOrEmpty(raw.Reserved1))
            {
                pendingFileConfig = new SkydrmLocal.rmc.featureProvider.User.PendingUploadFileConfig();
            }
            else
            {
                pendingFileConfig = JsonConvert.DeserializeObject<SkydrmLocal.rmc.featureProvider.User.PendingUploadFileConfig>(raw.Reserved1);
            }
        }

        public string Name { get => raw.Name; set => UpdateName(value); }

        public string LocalDiskPath { get => raw.LocalPath; set => UpdatePath(value); }

        public string DisplayPath => GetDisplayPath();

        public string PathId => "";

        public long FileSize => raw.Size;

        public string SharedEmails => "";

        public DateTime LastModifiedTime => raw.Last_Modified_Time;

        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.OperationStatus; set => UpdateStatus(value); }

        public EnumFileRepo FileRepo => EnumFileRepo.REPO_MYDRIVE;

        public IFileInfo FileInfo => new InternalFileInfo(this);

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

        public void RemoveFromLocal()
        {
            // Delele in local disk
            var App = SkydrmApp.Singleton;
            if (FileHelper.Exist(raw.LocalPath))
            {
                FileHelper.Delete_NoThrow(raw.LocalPath);
            }
            else
            {
                App.Log.Warn("file to be del,but not in local, " + raw.LocalPath);
            }
            //Delete in db.
            App.DBFunctionProvider.DeleteMyDriveLocalFile(raw.Id);
            //Delete in api.
            App.Rmsdk.User.RemoveLocalGeneratedFiles(raw.LocalPath);
        }

        public void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            var app = SkydrmApp.Singleton;
            try
            {
                if (OverWriteUpload)
                {
                    isOverWrite = true;
                }

                // get parent folder
                var folderDisplayPath = app.DBFunctionProvider.GetMyDriveLocalFileParentFolderDisplayPath(raw.MyDriveFile_Table_Pk);
                if (string.IsNullOrEmpty(folderDisplayPath))
                {
                    throw new Exception("Ileagal parent folder path.");
                }
                // call api
                app.Rmsdk.User.MyDriveUploadFile(LocalDiskPath, folderDisplayPath.ToLower(), isOverWrite);

                // delete from local db
                app.DBFunctionProvider.DeleteMyDriveLocalFile(raw.Id);

                // tell ServiceMgr -- Do this after Auto Remove (So invoking this in high level).
            }
            catch (RmRestApiException ex)
            {
                // Handle myDrive upload file 4002(file exist) exception
                if (ex.MethodKind == RmSdkRestMethodKind.Upload
                    && ex.ErrorCode == 4002)
                {
                    IsExistInRemote = true;
                }

                // In SDK exception 404 message is a general message, for upload 404 need notify special message
                if (ex.ErrorCode == 404)
                {
                    app.MessageNotify.NotifyMsg(raw.Name, CultureStringInfo.ApplicationFindResource("Common_Upload_Not_Found_DestFolder2"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);
                }
                else
                {
                    app.MessageNotify.NotifyMsg(raw.Name, ex.Message, EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);
                }

                throw;
            }
            catch (Exception)
            {
                app.MessageNotify.NotifyMsg(raw.Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Upload_Failed"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);

                throw;
            }
        }

        #region Private methods
        private string GetDisplayPath()
        {
            var folder = SkydrmApp.Singleton.DBFunctionProvider.GetMyDriveLocalFileParentFolderDisplayPath(raw.MyDriveFile_Table_Pk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Name;
        }

        private void UpdateStatus(EnumNxlFileStatus status)
        {
            //Sanity check
            //If no changes just return.
            if (raw.OperationStatus == (int)status)
            {
                return;
            }
            //update status in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateMyDriveLocalFileStatus(raw.Id, (int)status);
            raw.OperationStatus = (int)status;
        }

        private void UpdateName(string name)
        {
            //Sanity check
            //If no changes just return.
            if (raw.Name.Equals(name))
            {
                return;
            }
            //update name in db.
            SkydrmApp.Singleton.DBFunctionProvider.UpdateMyDriveLocalFileName(raw.Id, name);
            raw.Name = name;
        }

        private void UpdatePath(string path)
        {
            //Sanity check
            //If no changes just return.
            if (raw.LocalPath.Equals(path))
            {
                return;
            }
            //update path in db.
            SkydrmApp.Singleton.DBFunctionProvider.UpdateMyDriveLocalFilePath(raw.Id, path);
            raw.LocalPath = path;
        }

        private void UpdateFileConfig()
        {
            SkydrmApp.Singleton.DBFunctionProvider.UpdateMyDriveLocalFileReserved1(raw.Id,
                JsonConvert.SerializeObject(pendingFileConfig));
        }
        #endregion

        #region Inner class FileInfo
        private sealed class InternalFileInfo : FileInfoBaseImpl
        {
            private MyDriveLocalAddedFile outer;
            public InternalFileInfo(MyDriveLocalAddedFile outer) : base(outer.LocalDiskPath)
            {
                this.outer = outer;
            }

            // --- start--- Impl the abstract methods
            public override DateTime LastModified => outer.LastModifiedTime;

            public override string RmsRemotePath => outer.DisplayPath;

            public override bool IsCreatedLocal => false;

            public override string[] Emails => new string[0];

            public override EnumFileRepo FileRepo => outer.FileRepo;
            // --- end--- Impl the abstract methods

            // ---start--- must overwrite the below fields
            public override string Name => outer.Name;
            public override long Size => outer.FileSize;
            public override bool IsNormalFile => true;
            public override bool IsOwner
            {
                get
                {
                    // Actually, mydrive file always is normal file, so we can directly return false.
                    if (IsNormalFile) return false;
                    else return base.IsOwner;
                }
            }
            public override bool HasAdminRights
            {
                get
                {
                    if (IsNormalFile) return false;
                    else return base.HasAdminRights;
                }
            }

            public override bool IsByAdHoc => false;
            public override bool IsByCentrolPolicy => false;
            public override FileRights[] Rights => new FileRights[0];
            public override string WaterMark => "";
            public override Expiration Expiration => new Expiration();
            public override Dictionary<string, List<string>> Tags => null;
            public override string RawTags => "";

            // ---end--- must overwrite the below fields
        }
        #endregion // Inner class FileInfo
    }

}
