using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Newtonsoft.Json;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmDesktop.rmc.featureProvider.WorkSpace
{
    public sealed class WorkSpace : IWorkSpace
    {
        private readonly SkydrmApp App;
        private readonly log4net.ILog log;

        public string WorkingFolder { get; }

        private static List<string> mDirty_RecordingList = new List<string>();
        private static List<string> mDirty_ModifyList = new List<string>();

        public WorkSpace(SkydrmApp app)
        {
            this.App = app;
            this.log = app.Log;
            WorkingFolder = App.User.WorkingFolder + "\\WorkSpace";
            if (!Directory.Exists(WorkingFolder))
            {
                Directory.CreateDirectory(WorkingFolder);
            }
        }

        public void OnHeartBeat()
        {
            // Sync all nodes
            Stack<string> paths = new Stack<string>();
            paths.Push("/");
            while(paths.Count != 0)
            {
                (from f in Sync(paths.Pop())
                 where f.Is_Folder == true
                 select f.Path_Id)
                 .ToList()
                 .ForEach((j) => { paths.Push(j); });
            }

            // todo:
            // Maybe should notify ui treeview do auto refresh when some folder is deleted.
        }

        public IWorkSpaceFile[] List(string folderId)
        {
            try
            {
                var rt = new List<WorkSpaceFile>();
                var retDb = App.DBFunctionProvider.ListWorkSpaceFiles(folderId);
                foreach (var i in retDb)
                {
                    rt.Add(new WorkSpaceFile(this, i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public IWorkSpaceFile[] ListAll(bool toDisplay = false)
        {
            try
            {
                var rt = new List<WorkSpaceFile>();
                var retDb = App.DBFunctionProvider.ListAllWorkSpaceFiles();
                foreach(var i in retDb)
                {
                    rt.Add(new WorkSpaceFile(this, i, toDisplay));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public IWorkSpaceFile[] Sync(string folderId)
        {
            // remote nodes
            var remote = App.Rmsdk.User.ListWorkSpaceAllFiles(folderId);
            // local nodes
            var local = List(folderId);

            // Will do some merge as following 1 & 2.
            // 1. delete file that had been deleted on remote but still in local --> local also should delete them.

            // Note: here compare by fileid, the fileid still won't change when same file is overwrite.
            var diffset = from i in local
                          let rIds = from j in remote select j.fileId
                          where !rIds.Contains(i.File_Id)
                          select i;
            foreach(var i in diffset)
            {
                App.DBFunctionProvider.DeleteWorkSpaceFile(i.File_Id);

                // if this file is a folder, remove all its sub fiels
                if (i.Is_Folder)
                {
                    App.DBFunctionProvider.DeleteWorkSpaceFolderAndSubChildren(i.Path_Id);

                    // Note: if later workspace support mem cache, should also delete the folder's workspaceLocalFiles(waiting for upload files)
                    // in mem cache; Because even though local db will be deleted by cascading, but mem cache can't.
                }
            }

            // 2. remote added\modified some nodes but local don't ---> local also should added\modified them.
            var ff = new List<InsertWorkSpaceFile>();
            foreach(var f in FilterAddedOrModifiedInRemote(local, remote))
            {
                ff.Add(new InsertWorkSpaceFile()
                {
                    file_id = f.fileId,
                    duid = f.duid,
                    path_display = f.pathDisplay,
                    path_id = f.pathId,
                    nxl_name = f.nxlName,
                    file_type = f.fileType,
                    last_modified = f.lastModifed,
                    created_time = f.created,
                    size = f.size,
                    is_folser = f.isFolder != 0,
                    owner_id = f.ownerId,
                    owner_email = f.ownerEmail,
                    owner_display_name = f.ownerDisplayName,
                    modified_by = f.modifiedBy,
                    modified_by_email = f.modifedByEmail,
                    modified_by_name = f.modifiedByName
                });
            }
            // Insert\update
            App.DBFunctionProvider.UpsertWorkSpaceFileBatch(ff.ToArray());

            // Insert faked root node
            App.DBFunctionProvider.InsertWorkSpaceFakedRoot();

            return List(folderId);
        }

        public IWorkSpaceLocalFile[] ListLocalAdded(string folderId)
        {
            try
            {
                var rt = new List<WorkSpaceLocalAddedFile>();
                foreach( var i in App.DBFunctionProvider.ListWorkSpaceLocalFiles(folderId))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        App.DBFunctionProvider.DeleteWorkSpaceLocalFile(i.Id);
                        continue;
                    }

                    rt.Add(new WorkSpaceLocalAddedFile(i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public IWorkSpaceLocalFile[] ListLocalAllAdded()
        {
            try
            {
                var rt = new List<WorkSpaceLocalAddedFile>();
                foreach (var i in App.DBFunctionProvider.ListAllWorkSpaceLocalFiles())
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        App.DBFunctionProvider.DeleteWorkSpaceLocalFile(i.Id);
                        continue;
                    }

                    rt.Add(new WorkSpaceLocalAddedFile(i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public IWorkSpaceLocalFile AddLocalAdded(string parentFolder, 
            string filepath, List<FileRights> rights, WaterMarkInfo waterMark, 
            Expiration expiration, UserSelectTags tags)
        {
            string newAddedName = string.Empty;
            try
            {
                // Here use system bucket id, workSpace can look as the remote repository of system bucket.
                // So use system bucket's tokenGroup to encrypt.
                int id = App.SystemProject.Id;
                var outPath = App.Rmsdk.User.ProtectFileToWorkSpace(id, filepath, rights, waterMark, expiration, tags);

                // handle sdk nxl file
                string destFilePath = FileHelper.CreateNxlTempPath(WorkingFolder, parentFolder, outPath);
                outPath = FileHelper.HandleAddedFile(destFilePath, outPath, out bool isOverWriteUpload,
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isExistInLocal = false;
                        IWorkSpaceLocalFile[] localFiles = ListLocalAdded(parentFolder);
                        isExistInLocal = localFiles.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        // think about search rms file exist:1. network connected---use api to search ?? 2. network outages---use db to search
                        // search rms file exist from db
                        bool isExistInRms = false;
                        IWorkSpaceFile[] rmsFiles = List(parentFolder);
                        isExistInRms = rmsFiles.Any(f => f.Nxl_Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        return isExistInLocal || isExistInRms;
                    },
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isCan = true;
                        IWorkSpaceLocalFile[] localFiles = ListLocalAdded(parentFolder);
                        IWorkSpaceLocalFile localFile = localFiles.FirstOrDefault(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                        if (localFile != null && localFile.Status == EnumNxlFileStatus.Uploading)
                        {
                            isCan = false;
                        }
                        return isCan;
                    });

                newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                // insert into db
                App.DBFunctionProvider.InsertWorkSpaceLocalFile(newAddedName, outPath, parentFolder, File.GetLastAccessTime(outPath), newAddedFileSize,
                    JsonConvert.SerializeObject(new SkydrmLocal.rmc.featureProvider.User.PendingUploadFileConfig() { overWriteUpload = isOverWriteUpload }));

                // tell service mgr
                App.MessageNotify.NotifyMsg(newAddedName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Succeed"),
                    EnumMsgNotifyType.LogMsg, MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.WaitingUpload);

                if (App.User.SelectedOption == 1)
                {
                    IWorkSpaceFile[] rmsFiles = List(parentFolder);
                    IWorkSpaceFile rmsFile = rmsFiles.FirstOrDefault(f => f.Nxl_Name.Equals(newAddedName, StringComparison.OrdinalIgnoreCase));

                    if(rmsFile != null)
                    {
                        if(App.User.LeaveCopy)
                        {
                            if(rmsFile.Is_Offline || rmsFile.Is_Edit || rmsFile.Status == EnumNxlFileStatus.Online /* fix bug 63618 */)
                            {
                                var fp = App.Rmsdk.User.GetNxlFileFingerPrint(outPath);
                                rmsFile.UpdateWhenOverwriteInLeaveCopy(fp.duid, EnumNxlFileStatus.Online, newAddedFileSize, File.GetLastWriteTime(outPath));
                            }
                        }
                        else
                        {
                            if(rmsFile.Is_Offline || rmsFile.Is_Edit)
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
                return ListLocalAdded(parentFolder).First((i) => { 
                    return i.Name.Equals(newAddedName, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception e)
            {
                App.Log.Error("Failed to Protect the file" + e.Message, e);
                throw;
            }

        }

        public IOfflineFile[] GetOfflines()
        {
            try
            {
                var rt = new List<IOfflineFile>();
                var retDb = App.DBFunctionProvider.ListAllWorkSpaceFiles();
                foreach (var i in retDb)
                {
                    if (!i.RmsIsFolder && i.IsOffline && FileHelper.Exist(i.LocalPath))
                    {
                        rt.Add(new WorkSpaceFile(this, i));
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

        public IPendingUploadFile[] GetPendingUploads()
        {
            try
            {
                var rt = new List<IPendingUploadFile>();

                foreach (var i in App.DBFunctionProvider.ListAllWorkSpaceLocalFiles())
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        App.DBFunctionProvider.DeleteWorkSpaceLocalFile(i.Id);
                        continue;
                    }

                    rt.Add(new WorkSpaceLocalAddedFile(i));
                }

                return rt.ToArray();
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        // Remote added\modified some nodes but local don't
        private WorkSpaceFileInfo[] FilterAddedOrModifiedInRemote(IWorkSpaceFile[] locals, WorkSpaceFileInfo[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }

            var rt = new List<WorkSpaceFileInfo>();
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
                        App.Log.Info("WorkSpace local list no matching element");
                        // remote added node, should add into local
                        rt.Add(i);
                        continue;
                    }

                    // The node has been updated(modified\overwrite) in remote, local node should also update.
                    if (i.nxlName != l.Nxl_Name ||
                        i.size != l.Size ||
                        // Should compare via DateTime.toString() instead of DateTime override method "!=", which may impl by comparing Ticks inner,
                        // Actually, there are a slight difference between their ticks value, since 'Ticks' accuracy is too small.
                        SkydrmLocal.rmc.common.helper.JavaTimeConverter.ToCSDateTime(i.lastModifed).ToString() != l.Last_Modified.ToString())
                    {
                        // Only intercept for file in offline status.
                        if (l.Is_Offline)
                        {
                            // When nxl file rights is modified in Local, will set the flag "IsModifyRights" as true,
                            // then will update the "LastModified" to db.
                            if (l.Is_ModifyRights)
                            {
                                rt.Add(i);
                                l.Is_ModifyRights = false;
                            }

                            // Record the dirty item when detecting its "LastModified" changed.
                            // --- used for the file is edited\overwrite in local and remote, or the rights is modified in remote.
                            else
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

        public bool CheckFileExists(string pathId)
        {
            App.Rmsdk.User.WorkSpaceFileIsExist(pathId, out bool rt);
            return rt;
        }
    }

    public sealed class WorkSpaceFile : IOfflineFile, IWorkSpaceFile
    {
        private SkydrmApp app = SkydrmApp.Singleton;
        private database.table.workspace.WorkSpaceFile raw;
        private WorkSpace workspaceHost;
        private string cacheFolder;

        private bool isDirty;
        private InternalFileInfo fileInfo;
        private string partialLocalPath;

        // Constructor 
        public WorkSpaceFile(WorkSpace host, database.table.workspace.WorkSpaceFile raw, bool toDisplay = false)
        {
            this.workspaceHost = host;
            this.raw = raw;

            if (toDisplay)
            {
                return;
            }
            /*
               * WorkingFolder: C:\Users\aning\AppData\Local\SkyDRM\home\rms-centos7303.qapf1.qalab01.nextlabs.com\allen.ning@nextlabs.com\WorkSpace
               * 
               * Path_Display: /allen/sub_allen1/regNL.reg-2018-10-23-13-10-18.txt.nxl
              */
            cacheFolder = workspaceHost.WorkingFolder + Path_Display;
            if (Is_Folder)
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

            // leave a copy
            ImplLeaveCopy();
        }

        #region Impl for IWorkSpaceFile
        public string File_Id => raw.RmsFileId;

        public string Duid => raw.RmsDuid;

        public string Path_Display => raw.RmsPathDisplay;

        public string Path_Id => raw.RmsPathId;

        public string Nxl_Name => raw.RmsNxlName;

        public string File_Type => raw.RmsFileType;

        public DateTime Last_Modified => raw.RmsLastModified;

        public DateTime Created_Time => raw.RmsCreatedTime;

        public int Size => raw.RmsSize;

        public bool Is_Folder => raw.RmsIsFolder;

        public int Owner_Id => raw.RmsOwnerId;

        public string Owner_Display_Name => raw.RmsOwnerDisplayName;

        public string Owner_Email => raw.RmsOwnerEmail;

        public int Modified_By => raw.RmsModifiedBy;

        public string Modified_By_Name => raw.RmsModifedByName;

        public string Modified_By_Email => raw.RmsModifedByEmail;

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
                isDirty = WorkSpace.IsDataDirtyMasked(Path_Id);
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
                    bool ret = WorkSpace.RemoveDirtyMask(Path_Id);
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

        public bool Is_ModifyRights
        {
            get => raw.ModifyRightsStatus != 0;
            set => UpdateModifyRightsStatus(value ? 1 : 0);
        }

        public void Download(bool isViewOnly = false)
        {
            if (Is_Folder)
            {
                return;
            }

            string downloadFilePath = cacheFolder + "\\" + raw.RmsNxlName;
            // update file status is: downloading
            UpdateStatus(EnumNxlFileStatus.Downloading);

            try
            {
                DownlaodWorkSpaceFileType type = isViewOnly ? DownlaodWorkSpaceFileType.ForVeiwer : DownlaodWorkSpaceFileType.ForOffline;
                // delete previous file
                FileHelper.Delete_NoThrow(downloadFilePath, true);

                // call api
                string targetPath = cacheFolder;
                app.Rmsdk.User.DownloadWorkSpaceFile(raw.RmsPathId, ref targetPath, type);

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

                throw e;
            }
        }

        public void Export(string destinationFolder)
        {
            var app = SkydrmApp.Singleton;
            app.Log.Info(string.Format("Workspace try to export file, path {0}.", destinationFolder));

            string currentUserTempPathOrDownloadFilePath = System.IO.Path.GetTempPath();
            try
            {
                app.Rmsdk.User.CopyNxlFile(Name, Path_Id, NxlFileSpaceType.enterprise_workspace, "",
                   Path.GetFileName(destinationFolder), currentUserTempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                   true);

                string downloadFilePath = currentUserTempPathOrDownloadFilePath + Path.GetFileName(destinationFolder);
                File.Copy(downloadFilePath, destinationFolder, true);
            }
            catch (Exception e)
            {
                app.Log.Error(string.Format("Workspace failed to export file {0}.", Nxl_Local_Path), e);
                throw;
            }
            finally
            {
                FileHelper.Delete_NoThrow(currentUserTempPathOrDownloadFilePath + Path.GetFileName(destinationFolder));
            }
        }

        public void DownloadPartial()
        {
            if (Is_Folder)
            {
                return;
            }
            // File name is attached prefix "partial" returned by sdk.
            string partialPath = cacheFolder + "\\" + "partial_" + raw.RmsNxlName;

            try
            {
                // delete it before downlaod
                FileHelper.Delete_NoThrow(partialPath);
                // call api
                string targetPath = cacheFolder;
                app.Rmsdk.User.DownloadWorkSpacePartialFile(raw.RmsPathId, ref targetPath);

                partialPath = targetPath;
                
                // update partial local path in db
                partialLocalPath = partialPath;
            }
            catch (Exception e)
            {
                app.Log.Error("failed in partial downlaod file=" + partialPath, e);
                FileHelper.Delete_NoThrow(partialPath);
                throw e;
            }
        }

        public void GetNxlHeader()
        {
            if (Is_Folder)
            {
                return;
            }
            // File name is attached prefix "partial" returned by sdk.
            string partialPath = cacheFolder + "\\" + "partial_" + raw.RmsNxlName;

            try
            {
                // delete it before downlaod
                FileHelper.Delete_NoThrow(partialPath);
                // call api
                partialLocalPath = app.Rmsdk.User.WorkSpaceGetNxlFileHeader(raw.RmsPathId, cacheFolder);
            }
            catch (Exception e)
            {
                app.Log.Error("failed in GetNxlHeader=" + partialPath, e);
                FileHelper.Delete_NoThrow(partialPath);
                throw e;
            }
        }

        public void UploadEditedFile()
        {
            var app = SkydrmApp.Singleton;
            try
            {
                // get parent folder
                var rms_display_path = app.DBFunctionProvider.GetWorkSpaceLocalFileRmsParentFolder(raw.Id);
                if (string.IsNullOrEmpty(rms_display_path))
                {
                    throw new Exception("Ileagal parent folder path.");
                }
                // call api
                app.Rmsdk.User.UploadWorkSpaceEditedFile(GetParentFolderFromDisplayPath(rms_display_path), LocalDiskPath, true);

                // Notofy msg
                app.MessageNotify.NotifyMsg(raw.RmsNxlName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Upload_EditedFile_Succeed"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD_Edit, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Offline);
            }
            catch (Exception e)
            {
                // Notofy msg
                app.MessageNotify.NotifyMsg(raw.RmsNxlName, e.Message, EnumMsgNotifyType.PopupBubble,
                    MsgNotifyOperation.UPLOAD_Edit, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Offline);

                throw;
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

        public WorkspaceMetaData GetMetaData()
        {
            if(string.IsNullOrEmpty(Path_Id))
            {
                throw new Exception("Fatal error: invalid para is found.");
            }

            return app.Rmsdk.User.GetWorkSpaceFileMetadata(Path_Id);
        }

        public bool ModifyRights(List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            if (string.IsNullOrEmpty(LocalDiskPath))
            {
                return false;
            }

            string parentFolder = GetParentFolderFromDisplayPath(Path_Display);

            // call api.
            return app.Rmsdk.User.UpdateWorkSpaceNxlFileRights(Nxl_Local_Path, Name, parentFolder, rights, waterMark, expiration, tags);
        }

        #endregion // Impl for IWorkSpaceFile

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

        #region Impl common method
        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.Status; set => UpdateStatus(value); }

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public void UpdateWhenOverwriteInLeaveCopy(string duid, EnumNxlFileStatus fStatus, long fSize, DateTime fLastModifed)
        {
            Status = fStatus;
            raw.RmsSize = (int)fSize;
            raw.RmsLastModified = fLastModifed;

            // udate into db
            app.DBFunctionProvider.UpdateWhenOverwriteInLeaveCopy(raw.Id, duid, (int)fStatus, fSize, fLastModifed);
        }

        #endregion // Impl common method

        #region Private methods
        private string GetPartialLocalPath()
        {
            return cacheFolder + "\\" + "partial_" + raw.RmsNxlName;
        }

        private void OnChangeLocalPath(string newPath)
        {
            if (raw.LocalPath.Equals(newPath))
            {
                return;
            }
            // update db
            app.DBFunctionProvider.UpdateWorkSpaceFileLocalPath(raw.Id, newPath);
            // update cache
            raw.LocalPath = newPath;
        }

        private void UpdateEditStatus(int newStatus)
        {
            // Check changable first.
            if (raw.Edit_Status == newStatus)
            {
                return;
            }
            // Update db.
            app.DBFunctionProvider.UpdateWorkSpaceFileEditsStatus(raw.Id, newStatus); 
            // Update cache.
            raw.Edit_Status = newStatus;
        }

        private void UpdateModifyRightsStatus(int newStatus)
        {
            // Check changable first.
            if (raw.ModifyRightsStatus == newStatus)
            {
                return;
            }
            // Update db.
            app.DBFunctionProvider.UpdateWorkSpaceFileModifyRightsStatus(raw.Id, newStatus);
            // Update cache.
            raw.ModifyRightsStatus = newStatus;
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
            app.DBFunctionProvider.UpdateWorkSpaceFileOffline(raw.Id, offline);
            // Update obj raw's offline marker.
            raw.IsOffline = offline;
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
            app.DBFunctionProvider.UpdateWorkSpaceFileStatus(raw.Id, (int)status);
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

        private void AutoFixInConstruct()
        {
            if (Is_Folder)
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

        private void ImplLeaveCopy()
        {
            var leaveACopy = SkydrmApp.Singleton.User.LeaveCopy_Feature;

            if (leaveACopy.Exist(raw.RmsNxlName, cacheFolder))
            {
                // mark this file as local cached
                if (leaveACopy.MoveTo(cacheFolder, Name))
                {
                    var newLocalPath = cacheFolder + "\\" + Name;
                    // update this file status
                    OnChangeLocalPath(newLocalPath);
                    Is_Offline = true;
                    UpdateStatus(EnumNxlFileStatus.CachedFile);
                }

            }
        }

        private void OnFixResetLocalPathAndOffline()
        {
            // offline
            app.DBFunctionProvider.UpdateWorkSpaceFileOffline(raw.Id, false);
            raw.IsOffline = false;
            // local path
            app.DBFunctionProvider.UpdateWorkSpaceFileLocalPath(raw.Id, "");
            raw.LocalPath = "";
            // operation status Online = 4 which indicates file is in remote.
            app.DBFunctionProvider.UpdateWorkSpaceFileStatus(raw.Id, (int)EnumNxlFileStatus.Online);
            raw.Status = (int)EnumNxlFileStatus.Online;
        }

        private string GetParentFolderFromDisplayPath(string rmsDisplayPath)
        {
            string folderId = "/";

            if(string.IsNullOrEmpty(rmsDisplayPath))
            {
                throw new Exception("Fatal error: invalid para is found.");
            }

            int lastIndex = rmsDisplayPath.LastIndexOf("/");
            if(lastIndex != 0)
            {
                folderId = rmsDisplayPath.Substring(0, lastIndex + 1);
            }

            return folderId.ToLower();
        }

        #endregion // Private methods

        #region Inner class: FileInfo
        private sealed class InternalFileInfo : FileInfoBaseImpl
        {
            private WorkSpaceFile outer;
            public InternalFileInfo(WorkSpaceFile outer) : base(outer.Partial_Local_Path)
            {
                this.outer = outer;
            }

            public override string Name => outer.Name;

            public override long Size => outer.FileSize;

            public override DateTime LastModified => outer.LastModifiedTime;

            public override string RmsRemotePath => outer.RMSRemotePath;

            public override bool IsCreatedLocal => true;

            public override string[] Emails
            {                           
                get
                {
                    // now do not support share feature.
                    return new string[0];
                }           
            }

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_WORKSPACE;
        }
        #endregion // Inner class: FileInfo
    }

    public sealed class WorkSpaceLocalAddedFile : IWorkSpaceLocalFile
    {
        private database.table.workspace.WorkSpaceLocalFile raw;
        private InternalFileInfo fileInfo;

        private SkydrmLocal.rmc.featureProvider.User.PendingUploadFileConfig pendingFileConfig;

        // Constrctor
        public WorkSpaceLocalAddedFile(database.table.workspace.WorkSpaceLocalFile raw)
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

        #region For IPendingUploadFile
        public string Name { get => raw.Nxl_name; set => UpdateName(value); }

        public string LocalDiskPath { get => raw.LocalPath; set => UpdatePath(value); }

        public string DisplayPath =>GetRmsRemotePath();

        public string PathId =>"";

        public long FileSize => raw.Size;

        public EnumFileRepo FileRepo { get => EnumFileRepo.REPO_WORKSPACE; }

        public string SharedEmails => ""; // Now don't support currently

        public DateTime LastModifiedTime => raw.LastModified;

        public EnumNxlFileStatus Status { get => (EnumNxlFileStatus)raw.Status; set => UpdateStatus(value); }

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

        #endregion // For IPendingUploadFile

        // File source path before encryption.
        public string OriginalPath { get => raw.OriginalPath; set => UpdateOriginalPath(value); }

        private void Remove()
        {
            // delete in local disk
            var app = SkydrmApp.Singleton;
            if (FileHelper.Exist(raw.LocalPath))
            {
                FileHelper.Delete_NoThrow(raw.LocalPath);
            } else
            {
                app.Log.Warn("file to be del,but not in local, " + raw.LocalPath);
            }

            // delete in db
            app.DBFunctionProvider.DeleteWorkSpaceLocalFile(raw.Id);
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
                var folder = app.DBFunctionProvider.GetWorkSpaceLocalFileRmsParentFolder(raw.Workspacefile_table_pk);
                if (string.IsNullOrEmpty(folder))
                {
                    throw new Exception("Ileagal parent folder path.");
                }
                // call api
                app.Rmsdk.User.UploadWorkSpaceFile(folder.ToLower(), LocalDiskPath, isOverWrite);

                // delete from local db
                app.DBFunctionProvider.DeleteWorkSpaceLocalFile(raw.Id);

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
                app.MessageNotify.NotifyMsg(raw.Nxl_name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Upload_Failed"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);

                throw;
            }
        }

        #region Private methods.
        private string GetRmsRemotePath()
        {
            var folder = SkydrmApp.Singleton.DBFunctionProvider.GetWorkSpaceLocalFileRmsParentFolder(raw.Workspacefile_table_pk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Nxl_name;
        }

        private void UpdateStatus(EnumNxlFileStatus status)
        {
            if (raw.Status == (int)status)
            {
                return;
            }

            raw.Status = (int)status;
            // update status into db
            SkydrmApp.Singleton.DBFunctionProvider.UpdateWorkSpaceLocalFileStatus(raw.Id, (int)status);
        }

        private void UpdateOriginalPath(string path)
        {
            string local = raw.OriginalPath;
            if (path.Equals(local))
            {
                return;
            }
            //update originalPath in db.
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateWorkSpaceLocalFileOriginalPath(raw.Id, path);
            raw.OriginalPath = path;
        }

        private void UpdateName(string name)
        {
            //Sanity check
            //If no changes just return.
            if (raw.Nxl_name.Equals(name))
            {
                return;
            }
            //update name in db.
            SkydrmApp.Singleton.DBFunctionProvider.UpdateWorkSpaceLocalFileName(raw.Id, name);
            raw.Nxl_name = name;
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
            SkydrmApp.Singleton.DBFunctionProvider.UpdateWorkSpaceLocalFilePath(raw.Id, path);
            raw.LocalPath = path;
        }

        private void UpdateFileConfig()
        {
            SkydrmApp.Singleton.DBFunctionProvider.UpdateWorkSpaceLocalFileReserved1(raw.Id,
                JsonConvert.SerializeObject(pendingFileConfig));
        }
        #endregion // Private methods.

        #region Inner class: FileInfo
        private sealed class InternalFileInfo : FileInfoBaseImpl
        {
            private WorkSpaceLocalAddedFile outer;
            public InternalFileInfo(WorkSpaceLocalAddedFile outer) : base(outer.LocalDiskPath)
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
