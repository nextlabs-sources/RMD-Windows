using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Newtonsoft.Json;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmDesktop.rmc.featureProvider.SharedWorkspace
{
    public sealed class SharedWorkSpace : ISharedWorkspace
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;
        private readonly log4net.ILog log;

        private IRmsRepo rmsRepo;

        private static List<string> mDirty_RecordingList = new List<string>();
        private static List<string> mDirty_ModifyList = new List<string>();

        public SharedWorkSpace(IRmsRepo repo)
        {
            this.log = app.Log;
            this.rmsRepo = repo;

            WorkingFolder = app.User.WorkingFolder + "\\SharedWorkspace\\" + repo.RepoId;
            if (!Directory.Exists(WorkingFolder))
            {
                Directory.CreateDirectory(WorkingFolder);
            }
        }

        public string WorkingFolder { get; }

        public ExternalRepoType Type => rmsRepo.Type;

        public string DisplayName { get => rmsRepo.DisplayName; set => rmsRepo.DisplayName = value; }

        public string RepoId => rmsRepo.RepoId;

        public ISharedWorkspaceFile[] Sync(string path)
        {
            // remote nodes
            var remote = app.Rmsdk.User.ListSharedWorkspaceAllFiles(RepoId, path);
            // local nodes
            var local = List(path);

            // Will do some merge as following 1 & 2.
            // 1. delete file that had been deleted on remote but still in local --> local also should delete them.

            // Note: here compare by fileid, the fileid still won't change when same file is overwrite.
            var diffset = from i in local
                          let rIds = from j in remote select j.fileId
                          where !rIds.Contains(i.File_Id)
                          select i;
            foreach (var i in diffset)
            {
                app.DBFunctionProvider.DeleteSharedWorkspaceFile(RepoId, i.File_Id);

                // if this file is a folder, remove all its sub fiels
                if (i.Is_Folder)
                {
                    app.DBFunctionProvider.DeleteSharedWorkspaceFolderAndAllSubFiles(RepoId, i.Path_Id);

                    // Note: if later workspace support mem cache, should also delete the folder's workspaceLocalFiles(waiting for upload files)
                    // in mem cache; Because even though local db will be deleted by cascading, but mem cache can't.
                }
            }

            // 2. remote added\modified some nodes but local don't ---> local also should added\modified them.
            var ff = new List<InsertSharedWorkspaceFile>();
            foreach(var f in FilterAddedOrModifiedInRemote(local, remote))
            {
                ff.Add(new InsertSharedWorkspaceFile()
                {
                    repoId = this.RepoId,
                    fileId = f.fileId,
                    path = f.path,
                    pathId = f.pathId,
                    name = f.fileName,
                    type = f.fileType,
                    modifiedTime = f.lastModified,
                    createTime = f.creationTime,
                    size = f.size,
                    isFolder = (int)f.isFolder,
                    isProtectedFile = (int)f.isProtectedFile

                });
            }
            // Insert\update
            app.DBFunctionProvider.UpsertSharedWorkspaceFileBatchEx(ff.ToArray());

            // Insert faked root node
            app.DBFunctionProvider.InsertSharedWorkspaceFakedRoot(RepoId);

            return List(path);
        }

        public ISharedWorkspaceFile[] List(string path)
        {
            try
            {
                var rt = new List<SharedWorkspaceFile>();
                var retDb = app.DBFunctionProvider.ListSharedWorkspaceFile(RepoId, path);
                foreach (var i in retDb)
                {
                    rt.Add(new SharedWorkspaceFile(this, i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public ISharedWorkspaceFile[] ListAll(bool toDisplay = false)
        {
            try
            {
                var rt = new List<SharedWorkspaceFile>();
                var retDb = app.DBFunctionProvider.ListSharedWorkspaceAllFile(RepoId);
                foreach (var i in retDb)
                {
                    rt.Add(new SharedWorkspaceFile(this, i, toDisplay));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public ISharedWorkspaceLocalFile[] ListLocalAdded(string path)
        {
            try
            {
                var rt = new List<SharedWorkspaceLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListSharedWorkspaceLocalFile(RepoId, path))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteSharedWorkspaceLocalFile(i.Id);
                        continue;
                    }

                    rt.Add(new SharedWorkspaceLocalFile(this,i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public ISharedWorkspaceLocalFile[] ListLocalAllAdded()
        {
            try
            {
                var rt = new List<SharedWorkspaceLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListSharedWorkspaceAllLocalFile(RepoId))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteSharedWorkspaceLocalFile(i.Id);
                        continue;
                    }

                    rt.Add(new SharedWorkspaceLocalFile(this,i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public ISharedWorkspaceLocalFile AddLocalAdded(string folderId, string folderDisplayPath, string filepath, 
            List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            string newAddedName = string.Empty;
            try
            {
                // Here use system bucket id, workSpace can look as the remote repository of system bucket.
                // So use system bucket's tokenGroup to encrypt.
                int id = app.SystemProject.Id;
                var outPath = app.Rmsdk.User.ProtectFileToSharedSpace(id, filepath, rights, waterMark, expiration, tags);

                // handle sdk nxl file
                string destFilePath = FileHelper.CreateNxlTempPath(WorkingFolder, folderId, outPath);
                outPath = FileHelper.HandleAddedFile(destFilePath, outPath, out bool isOverWriteUpload,
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isExistInLocal = false;
                        ISharedWorkspaceLocalFile[] localFiles = ListLocalAdded(folderDisplayPath);
                        isExistInLocal = localFiles.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        // think about search rms file exist:1. network connected---use api to search ?? 2. network outages---use db to search
                        // search rms file exist from db
                        bool isExistInRms = false;
                        ISharedWorkspaceFile[] rmsFiles = List(folderDisplayPath);
                        isExistInRms = rmsFiles.Any(f => f.Nxl_Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        return isExistInLocal || isExistInRms;
                    },
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isCan = true;
                        ISharedWorkspaceLocalFile[] localFiles = ListLocalAdded(folderDisplayPath);
                        ISharedWorkspaceLocalFile localFile = localFiles.FirstOrDefault(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                        if (localFile != null && localFile.Status == EnumNxlFileStatus.Uploading)
                        {
                            isCan = false;
                        }
                        return isCan;
                    });

                newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                // insert into db
                app.DBFunctionProvider.InertLocalFileToSharedWorkspace(RepoId, folderDisplayPath, newAddedName, outPath,  (int)newAddedFileSize, File.GetLastAccessTime(outPath), 
                    JsonConvert.SerializeObject(new SkydrmLocal.rmc.featureProvider.User.PendingUploadFileConfig() { overWriteUpload = isOverWriteUpload }));

                // tell service mgr
                app.MessageNotify.NotifyMsg(newAddedName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Succeed"),
                    EnumMsgNotifyType.LogMsg, MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.WaitingUpload);

                if (app.User.SelectedOption == 1)
                {
                    ISharedWorkspaceFile[] rmsFiles = List(folderDisplayPath);
                    ISharedWorkspaceFile rmsFile = rmsFiles.FirstOrDefault(f => f.Nxl_Name.Equals(newAddedName, StringComparison.OrdinalIgnoreCase));

                    if (rmsFile != null)
                    {
                        if (app.User.LeaveCopy)
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

                // Get from db and return.
                return ListLocalAdded(folderDisplayPath).First((i) => {
                    return i.Name.Equals(newAddedName, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception e)
            {
                app.Log.Error("Failed to Protect the file" + e.Message, e);
                throw;
            }
        }

        public IOfflineFile[] GetOfflines()
        {
            try
            {
                var rt = new List<IOfflineFile>();
                var retDb = app.DBFunctionProvider.ListSharedWorkspaceAllFile(RepoId);
                foreach (var i in retDb)
                {
                    if (!i.RmsIsFolder && i.IsOffline && FileHelper.Exist(i.LocalPath))
                    {
                        rt.Add(new SharedWorkspaceFile(this, i));
                    }
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            try
            {
                var rt = new List<IPendingUploadFile>();

                foreach (var i in app.DBFunctionProvider.ListSharedWorkspaceAllLocalFile(RepoId))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteSharedWorkspaceLocalFile(i.Id);
                        continue;
                    }

                    rt.Add(new SharedWorkspaceLocalFile(this,i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public void OnHeartBeat()
        {
            throw new NotImplementedException();
        }

        // Remote added\modified some nodes but local don't
        private SharedWorkspaceFileInfo[] FilterAddedOrModifiedInRemote(ISharedWorkspaceFile[] locals, SharedWorkspaceFileInfo[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }

            var rt = new List<SharedWorkspaceFileInfo>();
            foreach (var i in remotes)
            {
                try
                {
                    // If use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                    {
                        if (i.fileId != j.File_Id) // Note: the fileId still is the same when file is overwrite.
                        {
                            return false;
                        }
                        return true;
                    });

                    // If no matching element, will return null.
                    if (l == null)
                    {
                        app.Log.Info("SharedWorkSpace local list no matching element");
                        // remote added node, should add into local
                        rt.Add(i);
                        continue;
                    }

                    // The node has been updated(modified\overwrite) in remote, local node should also update.
                    if (i.fileName != l.Nxl_Name ||
                        i.size != l.Size ||
                        // Should compare via DateTime.toString() instead of DateTime override method "!=", which may impl by comparing Ticks inner,
                        // Actually, there are a slight difference between their ticks value, since 'Ticks' accuracy is too small.
                        SkydrmLocal.rmc.common.helper.JavaTimeConverter.ToCSDateTime(i.lastModified).ToString() != l.Last_Modified.ToString())
                    {
                        // Only intercept for file in offline status.
                        if (l.Is_Offline)
                        {
                            // Record the dirty item when detecting its "LastModified" changed.
                            // --- used for the file is edited\overwrite in local and remote.
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
                    app.Log.Error(e);
                    // local find error
                    rt.Add(i);
                }
            }

            return rt.ToArray();

        }

        #region Handle dirty data
        public static bool IsDataDirtyMasked(string path)
        {
            return mDirty_RecordingList != null && mDirty_RecordingList.Contains(path);
        }

        public static bool RemoveDirtyMask(string path)
        {
            bool ret = mDirty_RecordingList.Contains(path) && mDirty_RecordingList.Remove(path);
            if (ret && !mDirty_ModifyList.Contains(path))
            {
                mDirty_ModifyList.Add(path);
            }
            return ret;
        }

        public static bool IsDataModifyDirtyMask(string path)
        {
            return mDirty_ModifyList != null && mDirty_ModifyList.Contains(path);
        }

        public static bool RemoveModifyMaskRecord(string path)
        {
            return mDirty_ModifyList.Contains(path) && mDirty_ModifyList.Remove(path);
        }

        public bool CheckFileExists(string pathId)
        {
            app.Rmsdk.User.IsSharedWorkSpaceFileExist(RepoId, pathId, out bool rt);
            return rt;
        }
        #endregion // Handle dirty data

    }

    public sealed class SharedWorkspaceFile : IOfflineFile, ISharedWorkspaceFile
    {
        private SkydrmApp app = SkydrmApp.Singleton;
        private database.table.sharedworkspace.SharedWorkspaceFile raw;
        private string repoId;
        private SharedWorkSpace host;

        private string cacheFolder;
        private string partialLocalPath;
        private bool isDirty;

        public SharedWorkspaceFile(SharedWorkSpace host, database.table.sharedworkspace.SharedWorkspaceFile r, bool toDisplay=false)
        {
            this.host = host;
            this.raw = r;
            this.repoId = host.RepoId;

            if (toDisplay)
            {
                return;
            }

            this.cacheFolder = host.WorkingFolder;
            // Init cache folder
            // In SharedWorkSpace, If the RMS raw is a folder, we will use the FileId to create a cache folder under the working directory. 
            // If the raw is a file, the cache path will be assigned according to the display path to find the FileId of the parent folder.
            if (Is_Folder)
            {
                this.cacheFolder = this.cacheFolder + @"\" + File_Id;
                FileHelper.CreateDir_NoThrow(cacheFolder);
            }
            else
            {
                string parentFolderId = string.Empty;
                // if lastIndex is 0, the cache folder under the working directory. Or else under the parent folder.
                int lastIndex = Path_Display.LastIndexOf('/');
                if (lastIndex != 0)
                {
                    string parentFolderPath = Path_Display.Substring(0, Path_Display.LastIndexOf('/'));
                    parentFolderId = app.DBFunctionProvider.GetSharedWorkSpaceFileRmsParentFolderId(repoId, parentFolderPath);
                }

                if (!string.IsNullOrEmpty(parentFolderId))
                {
                    this.cacheFolder = this.cacheFolder + @"\" + parentFolderId;
                }
            }

            // auto fix
            AutoFixInConstruct();

            // leave a copy
            ImplLeaveCopy();
        }

        #region Impl for ISharedWorkspaceFile
        public string File_Id => raw.RmsFileid;

        public string Path_Display => raw.RmsPath;

        public string Path_Id => raw.RmsPathid;

        public string Nxl_Name => raw.RmsName;

        public string File_Type => raw.RmsType;

        public DateTime Last_Modified => raw.RmsLastModified;

        public DateTime Created_Time => raw.RmsCreatedTime;

        public int Size => raw.RmsSize;

        public bool Is_Folder => raw.RmsIsFolder;

        public bool Is_ProtectedFile => raw.RmsIsProtectedFile;

        public string Nxl_Local_Path => raw.LocalPath;

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
                    // fixed bug 55672
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

        public bool Is_Offline { get => raw.IsOffline; set => UpdateOffline(value); }

        public bool Is_Dirty
        {
            get
            {
                isDirty = SharedWorkSpace.IsDataDirtyMasked(Path_Id);
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
                    bool ret = SharedWorkSpace.RemoveDirtyMask(Path_Id);
                    if (ret)
                    {
                        Console.WriteLine("Remove target data with rmspathid = {0} from the dirty data list.", Path_Id);
                    }
                }
            }
        }   
        

        public bool Is_Edit
        {
            get => raw.Edit_Status != 0;
            set => UpdateEditStatus(value ? 1 : 0);
        }

        // Not supported now.
        public bool Is_ModifyRights { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.Status; set => UpdateStatus(value); }

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public void UpdateWhenOverwriteInLeaveCopy(EnumNxlFileStatus fStatus, long fSize, DateTime fLastModifed)
        {
            Status = fStatus;
            raw.RmsSize = (int)fSize;
            raw.RmsLastModified = fLastModifed;

            // udate into db
            app.DBFunctionProvider.UpdateSharedWorkspaceWhenOverwriteInLeaveCopy(raw.Id, (int)fStatus, fSize, fLastModifed);
        }

        public void Download(bool isViewOnly = false)
        {
            if (Is_Folder)
            {
                return;
            }

            string downloadFilePath = cacheFolder + "\\" + raw.RmsName;
            // update file status is: downloading
            UpdateStatus(EnumNxlFileStatus.Downloading);

            try
            {
                // delete previous file
                FileHelper.Delete_NoThrow(downloadFilePath, true);
                DownlaodWorkSpaceFileType type = isViewOnly ? DownlaodWorkSpaceFileType.ForVeiwer : DownlaodWorkSpaceFileType.ForOffline;
                // call api
                string targetPath = cacheFolder;
                app.Rmsdk.User.DownloadSharedWorkSpaceFile(repoId, raw.RmsPath, ref targetPath, (int)type, raw.RmsIsProtectedFile);
                
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

        public void Export(string destinationFolder)
        {
            if (Is_Folder)
            {
                return;
            }

            var app = SkydrmApp.Singleton;
            app.Log.Info(string.Format("SharedWorkSpace try to export file, path {0}.", destinationFolder));

            string currentUserTempPathOrDownloadFilePath = System.IO.Path.GetTempPath();
            try
            {
                app.Rmsdk.User.CopyNxlFile(Name, RMSRemotePath, NxlFileSpaceType.sharepoint_online, host.RepoId,
                   Path.GetFileName(destinationFolder), currentUserTempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                   true);

                string downloadFilePath = currentUserTempPathOrDownloadFilePath + Path.GetFileName(destinationFolder);
                File.Copy(downloadFilePath, destinationFolder, true);
            }
            catch (Exception e)
            {
                app.Log.Error(string.Format("SharedWorkSpace failed to export file {0}.", Nxl_Local_Path), e);
                throw;
            }
            finally
            {
                FileHelper.Delete_NoThrow(currentUserTempPathOrDownloadFilePath + Path.GetFileName(destinationFolder));
            }
        }

        public void GetNxlHeader()
        {
            if (Is_Folder)
            {
                return;
            }
            // File name is attached prefix "partial" returned by sdk.
            string partialPath = cacheFolder + "\\" + "partial_" + raw.RmsName;

            try
            {
                // delete it before downlaod
                FileHelper.Delete_NoThrow(partialPath);
                // call api
                partialLocalPath = app.Rmsdk.User.GetSharedWorkSpaceNxlFileHeader(repoId, raw.RmsPath, cacheFolder);

            }
            catch (Exception e)
            {
                app.Log.Error("failed in GetNxlHeader=" + partialPath, e);
                FileHelper.Delete_NoThrow(partialPath);
                throw e;
            }
        }

        // Partly regard as unmark.
        public void Remove()
        {
            try
            {
                if (Is_Folder)
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
                app.Log.Error(e);
                throw;
            }
        }

        public void UploadEditedFile()
        {
            try
            {

                // get parent folder
                var rms_display_path = app.DBFunctionProvider.GetSharedWorkSpaceLocalFileRmsParentFolder(repoId, raw.Id);
                if (string.IsNullOrEmpty(rms_display_path))
                {
                    throw new Exception("Ileagal parent folder path.");
                }
                // call api
                app.Rmsdk.User.UploadSharedWorkSpaceFile(repoId, GetParentFolderFromDisplayPath(rms_display_path), LocalDiskPath, 2, false);

                // Notofy msg
                app.MessageNotify.NotifyMsg(raw.RmsName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Upload_EditedFile_Succeed"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD_Edit, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Offline);
            }
            catch (Exception e)
            {
                // Notofy msg
                app.MessageNotify.NotifyMsg(raw.RmsName, e.Message, EnumMsgNotifyType.PopupBubble,
                    MsgNotifyOperation.UPLOAD_Edit, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Offline);

                throw;
            }
        }

        #endregion // Impl for ISharedWorkspaceFile

        #region Impl for IOfflineFile
        public string Name => Nxl_Name;

        public string LocalDiskPath => Nxl_Local_Path;

        public string RMSRemotePath => Path_Display;

        public long FileSize => Size;

        public DateTime LastModifiedTime => Last_Modified;

        public bool IsOfflineFileEdit => Is_Edit;

        public void RemoveFromLocal()
        {
            Remove();
        }

        #endregion // Impl for IOfflineFile

        #region Private methods

        private void UpdateEditStatus(int newStatus)
        {
            if (raw.Edit_Status == newStatus)
            {
                return;
            }
            // Update db.
            app.DBFunctionProvider.UpdateSharedWorkspaceFileEditsStatus(raw.Id, newStatus);
            // Update cache.
            raw.Edit_Status = newStatus;
        }

        private void UpdateOffline(bool offline)
        {
            // Sanity check.
            if (raw.IsOffline == offline)
            {
                return;
            }
            // Update offline marker in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateSharedWorkspaceFileOffline(raw.Id, offline);
            // Update obj raw's offline marker.
            raw.IsOffline = offline;
        }

        private string GetPartialLocalPath()
        {
            return cacheFolder + "\\" + "partial_" + raw.RmsName;
        }

        private void AutoFixInConstruct()
        {
            if (Is_Folder)
            {
                return;
            }
            if (raw.IsOffline)
            {
                // require the file must exist
                var fPath = cacheFolder + "\\" + raw.RmsName;
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

        private void OnFixResetLocalPathAndOffline()
        {
            // offline
            app.DBFunctionProvider.UpdateSharedWorkspaceFileOffline(raw.Id, false);
            raw.IsOffline = false;
            // local path
            app.DBFunctionProvider.UpdateSharedWorkspaceFileLocalPath(raw.Id, "");
            raw.LocalPath = "";
            // operation status Online = 4 which indicates file is in remote.
            app.DBFunctionProvider.UpdateSharedWorkspaceFileStatus(raw.Id, (int)EnumNxlFileStatus.Online);
            raw.Status = (int)EnumNxlFileStatus.Online;
        }

        private void ImplLeaveCopy()
        {
            var leaveACopy = SkydrmApp.Singleton.User.LeaveCopy_Feature;

            if (leaveACopy.Exist(raw.RmsName, cacheFolder))
            {
                // mark this file as local cached
                if (leaveACopy.MoveTo(cacheFolder, Nxl_Name))
                {
                    var newLocalPath = cacheFolder + "\\" + Nxl_Name;
                    // update this file status
                    OnChangeLocalPath(newLocalPath);
                    Is_Offline = true;
                    UpdateStatus(EnumNxlFileStatus.CachedFile);
                }

            }
        }

        private void OnChangeLocalPath(string newPath)
        {
            if (raw.LocalPath.Equals(newPath))
            {
                return;
            }
            // update db
            app.DBFunctionProvider.UpdateSharedWorkspaceFileLocalPath(raw.Id, newPath);
            // update cache
            raw.LocalPath = newPath;
        }

        private void UpdateStatus(EnumNxlFileStatus status)
        {
            // sanity check
            if (raw.Status == (int)status)
            {
                return;
            }
            // Update vaultfile status in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateSharedWorkspaceFileStatus(raw.Id, (int)status);
            raw.Status = (int)status;
            if (status == EnumNxlFileStatus.Online)
            {
                Is_Offline = false;
            }
            if (status == EnumNxlFileStatus.AvailableOffline)
            {
                Is_Offline = true;
            }
        }

        private string GetParentFolderFromDisplayPath(string rmsDisplayPath)
        {
            string folderId = "/";

            if (string.IsNullOrEmpty(rmsDisplayPath))
            {
                throw new Exception("Fatal error: invalid para is found.");
            }

            int lastIndex = rmsDisplayPath.LastIndexOf("/");
            if (lastIndex != 0)
            {
                folderId = rmsDisplayPath.Substring(0, lastIndex + 1);
            }

            return folderId.ToLower();
        }

        #endregion // Private methods

        #region Inner class: FileInfo
        private sealed class InternalFileInfo : FileInfoBaseImpl
        {
            private SharedWorkspaceFile outer;
            public InternalFileInfo(SharedWorkspaceFile file):base(file.Partial_Local_Path, !file.Is_ProtectedFile)
            {
                this.outer = file;
            }

            public override string Name => outer.Nxl_Name;

            public override long Size => outer.Size;

            public override DateTime LastModified => outer.Last_Modified;

            public override string RmsRemotePath => outer.Path_Display;

            public override bool IsCreatedLocal => false;

            public override string[] Emails
            {
                get
                {
                    // now do not support share feature.
                    return new string[0];
                }
            }

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_EXTERNAL_DRIVE;

            public override bool IsNormalFile => !outer.Is_ProtectedFile;

        }
        #endregion // Inner class: FileInfo

    }

    public sealed class SharedWorkspaceLocalFile : ISharedWorkspaceLocalFile
    {
        private database.table.sharedworkspace.SharedWorkspaceLocalFile raw;
        private string repoId;

        private SkydrmLocal.rmc.featureProvider.User.PendingUploadFileConfig pendingFileConfig;

        public SharedWorkspaceLocalFile(SharedWorkSpace host, database.table.sharedworkspace.SharedWorkspaceLocalFile r)
        {
            this.repoId = host.RepoId;
            this.raw = r;

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
        public string LocalDiskPath { get => raw.LocalPath; set => UpdateLocalPath(value); }

        public string DisplayPath => GetRmsRemotePath();

        public string PathId => "";

        public long FileSize => raw.Size;

        public string SharedEmails => "";

        public DateTime LastModifiedTime => raw.ModifiedTime;

        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.OperationStatus; set => UpdateStatus(value); }

        public EnumFileRepo FileRepo => EnumFileRepo.REPO_EXTERNAL_DRIVE;

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
            Remove();
        }

        public void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            InnerUpload(isOverWrite);
        }

        #region Private methods.

        private void Remove()
        {
            // delete in local disk
            var app = SkydrmApp.Singleton;
            if (FileHelper.Exist(raw.LocalPath))
            {
                FileHelper.Delete_NoThrow(raw.LocalPath);
            }
            else
            {
                app.Log.Warn("file to be del,but not in local, " + raw.LocalPath);
            }

            // delete in db
            app.DBFunctionProvider.DeleteSharedWorkspaceLocalFile(raw.Id);

            // delete in api
            app.Rmsdk.User.RemoveLocalGeneratedFiles(Name);

            // If LeaveCopy, should delete file in LeaveCopy Folder
            if (app.User.LeaveCopy)
            {
                var leaveACopy = SkydrmApp.Singleton.User.LeaveCopy_Feature;
                if (leaveACopy.Exist(Name, "", LocalDiskPath))
                {
                    leaveACopy.DeleteFile(LocalDiskPath);
                }
            }
        }

        private void InnerUpload(bool isOverWrite)
        {
            var app = SkydrmApp.Singleton;
            try
            {
                if (OverWriteUpload)
                {
                    isOverWrite = true;
                }

                // get parent folder
                var folder = app.DBFunctionProvider.GetSharedWorkSpaceLocalFileRmsParentFolder(repoId, raw.SharedWorkspaceFileTablePk);
                if (string.IsNullOrEmpty(folder))
                {
                    throw new Exception("Ileagal parent folder path.");
                }

                // The folder path returned by the server does not have a slash at the end, 
                // but it needs to be added when uploading files. 
                if (folder.Length > 1 && !folder.EndsWith("/"))
                {
                    folder += "/";
                }

                // call api
                app.Rmsdk.User.UploadSharedWorkSpaceFile(repoId, folder.ToLower(), LocalDiskPath, 4, isOverWrite);

                // delete from local db
                app.DBFunctionProvider.DeleteSharedWorkspaceLocalFile(raw.Id);

                // tell ServiceMgr -- Do this after Auto Remove (So invoking this in high level).

                // Every done, begin impl leave a copy featue
                if (app.User.LeaveCopy)
                {
                    app.User.LeaveCopy_Feature.AddFile(LocalDiskPath);

                    FileHelper.Delete_NoThrow(LocalDiskPath);
                }

            }
            catch (RmRestApiException ex)
            {
                // Handle workSpace upload file 4001(file exist) exception
                if (ex.MethodKind == RmSdkRestMethodKind.Upload
                    && ex.ErrorCode == 4001)
                {
                    IsExistInRemote = true;
                }

                // In SDK exception 404 message is a general message, for upload 404 need notify special message
                if (ex.ErrorCode == 404)
                {
                    app.MessageNotify.NotifyMsg(Name, CultureStringInfo.ApplicationFindResource("Common_Upload_Not_Found_DestFolder2"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);
                }
                else
                {
                    app.MessageNotify.NotifyMsg(Name, ex.Message, EnumMsgNotifyType.LogMsg,
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

        private string GetRmsRemotePath()
        {
            var folder = SkydrmApp.Singleton.DBFunctionProvider.GetSharedWorkSpaceLocalFileRmsParentFolder(repoId, raw.SharedWorkspaceFileTablePk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Name;
        }

        private void UpdateStatus(EnumNxlFileStatus status)
        {
            if (raw.OperationStatus == (int)status)
            {
                return;
            }

            raw.OperationStatus = (int)status;
            // update status into db
            SkydrmApp.Singleton.DBFunctionProvider.UpdateSharedWorkspaceLocalFileStatus(raw.Id, (int)status);
        }

        private void UpdateFileConfig()
        {
            SkydrmApp.Singleton.DBFunctionProvider.UpdateSharedWorkspaceLocalFileReserved1(raw.Id,
                JsonConvert.SerializeObject(pendingFileConfig));
        }

        private void UpdateName(string name)
        {
            //If no changes just return.
            if (raw.Name.Equals(name))
            {
                return;
            }
            //update name in db.
            SkydrmApp.Singleton.DBFunctionProvider.UpdateSharedWorkspaceLocalFileName(raw.Id, name);
            raw.Name = name;
        }

        private void UpdateLocalPath(string path)
        {
            //Sanity check
            //If no changes just return.
            if (raw.LocalPath.Equals(path))
            {
                return;
            }
            //update path in db.
            SkydrmApp.Singleton.DBFunctionProvider.UpdateSharedWorkspaceLocalFileLocalPath(raw.Id, path);
            raw.LocalPath = path;
        }

        #endregion // Private methods.

        #region Inner class: FileInfo
        private sealed class InternalFileInfo : FileInfoBaseImpl
        {
            private SharedWorkspaceLocalFile outer;
            public InternalFileInfo(SharedWorkspaceLocalFile outer) : base(outer.LocalDiskPath)
            {
                this.outer = outer;
            }

            public override string Name => outer.Name;

            public override long Size => outer.FileSize;

            public override DateTime LastModified => outer.LastModifiedTime;

            public override string RmsRemotePath => outer.DisplayPath;

            public override bool IsCreatedLocal => true;

            public override string[] Emails
            {
                get
                {
                    // now do not support share feature.
                    return new string[0];
                }
            }

            public override EnumFileRepo FileRepo => outer.FileRepo;

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }
        }
        #endregion // Inner class: FileInfo

    }
}
