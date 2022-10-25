using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database2.table.project;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmLocal.rmc.featureProvider
{
    public sealed class MyProjects : IMyProjects
    {
        readonly private SkydrmLocalApp app;
        readonly private log4net.ILog log;
        public MyProjects(SkydrmLocalApp app)
        {
            this.app = app;
            this.log = app.Log;
        }

        public IMyProject[] List()
        {
            var ps = app.DBFunctionProvider.ListProject();
            if (ps == null || ps.Length == 0)
            {
                return new IMyProject[0];
            }
            IList<MyProject> rt = new List<MyProject>();
            int Id_SystemDefaultProejct = app.Rmsdk.User.GetSystemProjectId();
            for (int i = 0; i < ps.Length; i++)
            {
                // new feature, filter out system default project ,when returning uses's my projects
                if (ps[i].Rms_project_id == Id_SystemDefaultProejct)
                {
                    continue;
                }
                rt.Add(new MyProject(app, ps[i]));
            }
            return rt.ToArray();
        }

        public IMyProject[] Sync()
        {
            var remote = app.Rmsdk.User.UpdateProjectInfo();
            var local = List();
            // find difference set by (Local - remote) , and delete it 
            var difset = from i in local
                         let rIds = from j in remote select j.id
                         where !rIds.Contains(i.Id)
                         select i;

            foreach (var i in difset)
            {
                app.DBFunctionProvider.DeleteProject(i.Id);

                // Also need to delete the project all local files(waiting upload files) in mem cache.
                app.DBFunctionProvider.DeleteProjectAllLocalFiles(i.Id);
            }


            foreach (var i in remote)
            {
                // Upsert each i's meta info
                app.DBFunctionProvider.UpsertProject(
                        i.id, i.name,
                        i.displayName, i.description,
                        i.isOwner == 1, i.tenantId, () =>
                        {
                        // Upsert each i's classification config
                        app.DBFunctionProvider.UpdateProjectClassification(
                                i.id,
                                GetSerializableClassificationJStr(GetProjectClassification(i.tenantId)));
                        });
                // upsert is enabled adoc
                app.DBFunctionProvider.UpsertProjectIsEnabledAdhoc(i.id, i.isAdhocEnabled == 1 ? true : false);
            }
            return List();
        }

        public string GetSerializableClassificationJStr(ProjectClassification[] classifications)
        {
            if (classifications == null || classifications.Length == 0)
            {
                return "[]";
            }
            try
            {
                return JsonConvert.SerializeObject(classifications);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured when invoke GetSerializableClassificationJStr {0}", e);
            }
            return "[]";
        }

        public ProjectClassification[] GetProjectClassification(string tenantId)
        {
            try
            {
                return app.Rmsdk.User.GetProjectClassification(tenantId);
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception occured when invoke GetProjectClassification {0}", e);
            }
            return new ProjectClassification[0];
        }

        // Good point to sync user's all projects info with RMS
        public void OnHeartBeat()
        {
            // update each
            //foreach (var i in Sync())
            //{
            //    Stack<string> paths = new Stack<string>();
            //    paths.Push("/");
            //    while (paths.Count != 0)
            //    {
            //        (from f in i.SyncFiles(paths.Pop())
            //         where f.isFolder == true
            //         select f.RsmPathId)
            //         .ToList()
            //         .ForEach((j) => { paths.Push(j); });
            //    }
            //}

            // Fixed bug 54850, main window treeview can't update, because the hearbeat firstly trigger refresh to update db,
            // which result in merge failed(oldList is the same with newSyncList).
            SkydrmLocalApp.Singleton.Log.Info("Project HeartBeat");
            SkydrmLocalApp.Singleton.InvokeEvent_ProjectUpdate();
        }
    }

    public sealed class MyProject : IMyProject
    {
        readonly private SkydrmLocalApp app;
        readonly private log4net.ILog log;

        private database2.table.project.Project rawProject;

        private string working_path;

        private static List<string> mDirty_RecordingList= new List<string>();
        private static List<string> mDirty_ModifyList = new List<string>();

        public MyProject(SkydrmLocalApp app, database2.table.project.Project rawProject)
        {
            this.app = app;
            this.log = app.Log;
            this.rawProject = rawProject;

            // make sure working path exists  <= IUser
            working_path = app.User.WorkingFolder + "\\myProject\\" + Id;
            Directory.CreateDirectory(working_path);
        }

        public string Path => working_path;

        public int Id { get => rawProject.Rms_project_id; }

        public int RowId { get => rawProject.Id; }

        public string DisplayName { get => rawProject.Rms_display_name; }

        public string Description { get => rawProject.Rms_description; }

        public bool IsOwner { get => rawProject.Rms_is_owner; }

        public bool IsEnableAdHoc { get => rawProject.Rms_is_enable_adhoc; }

        public string MemberShipId { get => rawProject.Rms_tenant_id; }

        public IProjectFile[] ListFiles(string FolderId)
        {
            try
            {
                var rt = new List<ProjectFile>();
                foreach (var i in app.DBFunctionProvider.ListProjectFile(Id, FolderId))
                {
                    // required each new fill do auto-fix 
                    rt.Add(new ProjectFile(Id, Path, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IProjectFile[] ListAllProjectFile()
        {
            try
            {
                var rt = new List<ProjectFile>();
                var retCache = app.DBFunctionProvider.ListAllProjectFile(Id);
                foreach (var i in retCache)
                {
                    // required each new fill do auto-fix 
                    rt.Add(new ProjectFile(Id, Path, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }


        public IProjectLocalFile[] ListProjectLocalFiles()
        {
            try
            {
                var rt = new List<ProjectLocalAddedFile>();
                foreach (var i in app.DBFunctionProvider.ListProjectLocalFiles(Id))
                {             
                    rt.Add(new ProjectLocalAddedFile(Id, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IProjectLocalFile[] ListLocalAdded(string FolderId)
        {
            try
            {
                var rt = new List<ProjectLocalAddedFile>();
                foreach (var i in app.DBFunctionProvider.ListProjectLocalFiles(Id, FolderId))
                {
                    if (!FileHelper.Exist(i.Path))
                    {
                        app.DBFunctionProvider.DeleteProjectLocalFile(i.ProjectTablePk,i.Id);
                        continue;
                    }
                    rt.Add(new ProjectLocalAddedFile(Id, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }

        }

        public IOfflineFile[] GetOfflines()
        {
            try
            {
                var rt = new List<ProjectFile>();
                foreach (var i in app.DBFunctionProvider.ListProjectOfflineFile(Id))
                {
                    if (i.Is_offline && File.Exists(i.Local_path))
                    {
                        rt.Add(new ProjectFile(Id, Path, i));
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
                IList<IPendingUploadFile> rt = new List<IPendingUploadFile>();
                foreach (var i in app.DBFunctionProvider.ListProjectLocalFiles(Id))
                {
                    // auto fix
                    if (!FileHelper.Exist(i.Path))
                    {
                        app.DBFunctionProvider.DeleteProjectLocalFile(i.ProjectTablePk,i.Id);
                        continue;
                    }
                    if (IsMatchPendingUpload((EnumNxlFileStatus)i.Operation_status))
                    {
                        rt.Add(new ProjectLocalAddedFile(Id, i));
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

        private bool IsMatchPendingUpload(EnumNxlFileStatus status)
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



        public IProjectLocalFile AddLocalFile(string ParentFolder,
            string filePath,
            List<FileRights> rights,
            WaterMarkInfo waterMark,
            Expiration expiration,
            UserSelectTags tags)
        {
            try
            {
                // tell api to convert this this
                app.Log.Info("protect the file to project: "+ filePath);
                var outPath = app.Rmsdk.User.ProtectFileToProject(Id,
                    filePath, rights, waterMark, expiration, tags);
                // find this file in api
                //app.Log.Info("get the finger print of the new protected file");
                //var fp = app.Rmsdk.User.GetNxlFileFingerPrint(outPath);
                // by osmond, feature changed, allow user to portect a file which has not any permissons for this user
                string newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                // store this file into db;
                app.Log.Info("store the new protected file into database");
                app.DBFunctionProvider.AddLocalFileToProject(Id, ParentFolder,
                    newAddedName, outPath, (int)newAddedFileSize, File.GetLastAccessTime(outPath));
                // tell IRecentTouchedFiles
                app.UserRecentTouchedFile.UpdateOrInsert(EnumNxlFileStatus.WaitingUpload, newAddedName);
                // return this local file to caller
                return ListLocalAdded(ParentFolder).First((i) =>
                {
                    return i.LocalPath.Equals(outPath);
                });

            }
            catch (Exception e)
            {
                app.Log.Error("Failed to Protect the file"+e.Message, e);
                throw;
            }
        }

        public IProjectLocalFile CopyLocalFile(string CopyPath,
           string filePath,
           List<FileRights> rights,
           WaterMarkInfo waterMark,
           Expiration expiration,
           UserSelectTags tags)
        {
            try
            {
                // tell api to convert this this
                app.Log.Info("protect the file to project: "+ filePath);
                var outPath = app.Rmsdk.User.ProtectFileToProject(Id,
                    filePath, rights, waterMark, expiration, tags);

                // find this file in api
                //app.Log.Info("get the finger print of the new protected file");
                //var fp = app.Rmsdk.User.GetNxlFileFingerPrint(outPath);
                string newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                // if need delete plainFile, nxl file without time-stamp,
                // if not, nxl file has time-stamp
                bool IsNeedDeleteSourceFile =app.Rmsdk.User.GetIsDeleteSource();

                string destPath= FileHelper.DoAfterProtect(filePath, outPath, CopyPath, false);

                // return this local file to caller
                return new ProjectLocalAddedFile(Id,
                    new ProjectLocalFile()
                    {
                        Id = Id,
                        Name = newAddedName,
                        Path = destPath,
                        Last_modified_time = File.GetLastAccessTime(destPath),
                        Size = newAddedFileSize
                    }
                );

            }
            catch (Exception e)
            {
                app.Log.Error("Failed to Protect the file" + e.Message, e);
                throw;
            }
        }

        public ProjectClassification[] ListClassifications()
        {
            return JsonConvert.DeserializeObject<ProjectClassification[]>(
                app.DBFunctionProvider.GetProjectClassification(Id));
        }

        public IProjectMember[] ListMembers()
        {
            throw new NotImplementedException();
        }

        public ProjectClassification[] SyncClassifications()
        {
            try
            {
                app.DBFunctionProvider.UpdateProjectClassification(
                                        Id,
                                        JsonConvert.SerializeObject(
                                            app.Rmsdk.User.GetProjectClassification(MemberShipId)
                                            )
                                    );
                return ListClassifications();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IProjectFile[] SyncFiles(string FolderId)
        {
            // remote sets
            var remote = app.Rmsdk.User.ListProjectsFiles(Id, FolderId);
            var remote_fileIds = (from i in remote select i.id);
            var local = ListFiles(FolderId);
            var local_fileIds = (from i in local select i.RmsFileId);

            // routine: delete file that had been del on remote but still in local
            var diffset = from i in local
                          let rIds = from j in remote select j.id
                          where !rIds.Contains(i.RmsFileId)
                          select i;
            foreach (var i in diffset)
            {
                // requery 
                int projectfile_table_pk = app.DBFunctionProvider.QueryProjectFileId(RowId, i.RsmPathId);

                app.DBFunctionProvider.DeleteProjectFile(Id, i.RmsFileId);

                // if this file is a folder, remove all its sub fiels
                if (i.isFolder)
                {
                    app.DBFunctionProvider.DeleteProjectFolderAndAllSubFiles(Id, i.RsmPathId);

                    // Also need to delete the project folder's local files(waiting upload files) in mem cache. --- fix bug 55652
                    if (projectfile_table_pk != -1)
                    {
                        app.DBFunctionProvider.DeleteProjectFolderLocalFiles(RowId, projectfile_table_pk);
                    }
                }
            }
            var ff = new List<InstertProjectFile>();
            // filter the remotes that has been modified, reduce db insert/update
            foreach (var f in FilterOutNotModified(local, remote))
            {
                ff.Add(new InstertProjectFile()
                {
                    project_id = Id,
                    file_id = f.id,
                    file_duid = f.duid,
                    file_display_path = f.displayPath,
                    file_path_id = f.pathId,
                    file_nxl_name = f.nxlName,
                    file_lastModifiedTime = f.lastModifedTime,
                    file_creationTime = f.creationTime,
                    file_size = f.fileSize,
                    file_rms_ownerId = f.ownerId,
                    file_ownerDisplayName = f.ownerDisplayName,
                    file_ownerEmail = f.ownerEmail
                });
            }
            app.DBFunctionProvider.UpsertProjectFileBatch(ff.ToArray());
            return ListFiles(FolderId);
        }

        public IProjectMember[] SyncMembers()
        {
            throw new NotImplementedException();
        }


        private ProjectFileInfo[] FilterOutNotModified(IProjectFile[] locals, ProjectFileInfo[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }

            var rt = new List<ProjectFileInfo>();
            foreach (var i in remotes)
            {
                try
                {
                    // find in local
                    // if use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                      {
                          if (i.id != j.RmsFileId)
                          {
                              return false;
                          }
                          return true;
                      });

                    // if no matching element, will return null.
                    if (l == null)
                    {
                        app.Log.Info("Project local list no matching element");
                        rt.Add(i);
                        continue;
                    }

                    // judege whether modified

                    // by current all rms fileds won't changed
                    // for safe concerned, I just compare name and size
                    if (i.nxlName != l.Name ||
                        i.fileSize != l.FileSize ||
                        SkydrmLocal.rmc.common.helper.JavaTimeConverter.ToCSDateTime(i.lastModifedTime)!=l.LastModifiedTime
                        )
                    {
                        // Only intercept for file in offline status.
                        if (l.isOffline)
                        {
                            // When nxl file rights is modified in Local, will set the flag "IsModifyRights" as true,
                            // then will update the "LastModified" to db.
                            if (l.IsModifyRights)
                            {
                                rt.Add(i);
                                l.IsModifyRights = false;
                            }
                            // Record the dirty item when detecting its "LastModified" changed.
                            // --- used for the file is edited in local and remote, or the rights is modified in remote.
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
                    app.Log.Error(e);
                    // local find error
                    rt.Add(i);
                }

            }
            return rt.ToArray();
        }

    }

    public sealed class ProjectFile : IProjectFile, IOfflineFile
    {
        int ProjectId;
        private database2.table.project.ProjectFile raw;
        // folder: local cache path,
        private string cache_folder;
        SkydrmLocalApp app = SkydrmLocalApp.Singleton;
        private IFileInfo fileInfo; // each get will generate a new-one
        private bool isDirty;
        private string partialLocalPath;

        public string RmsFileId => raw.Rms_file_id;

        public string RmsDuId => raw.Rms_duid;

        public string Name { get => raw.Rms_name; }

        public string RMSDisplayPath => raw.Rms_display_path;

        public string RsmPathId => raw.Rms_path_id;

        public bool isFolder { get => raw.Rms_is_folder; }

        public bool isOffline { get => raw.Is_offline; set => OnOfflineMarkChanged(value); }

        public EnumNxlFileStatus Status
        {
            get => (EnumNxlFileStatus)raw.Operation_status;
            set => OnChangeOperationStaus(value);
        }

        public DateTime LastModifiedTime => raw.Rms_lastModifiedTime;

        public DateTime CreationTime => raw.Rms_creationTime;

        public long FileSize => raw.Rms_fileSize;

        public int RmsOwnerId => raw.Rms_OwnerId;

        public string OwnerDisplayName => raw.Rms_OwnerDisplayName;

        public string OwnerEmail => raw.Rms_OwnerEmail;

        public string LocalDiskPath { get => raw.Local_path; }

        public string RMSRemotePath => raw.Rms_display_path;

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public string PartialLocalPath
        {
            get
            {
                //Fix bug 53922
                // When modify rights of offline file in project,the partial file not update.
                // If the nxl file is offline,we use local path instead of partoal_localPath.

                /// 
                /// ---- Comment it, since now for offline file, also will re-download partial file, so using partialPath to get file info.
                ///
                //if (isOffline && FileHelper.Exist(LocalDiskPath))
                //{
                //    return LocalDiskPath;
                //}

                if (string.IsNullOrEmpty(partialLocalPath))
                {
                    partialLocalPath = GetPartialLocalPath();
                }

                if (!FileHelper.Exist(partialLocalPath))
                {
                    // fixed bug 55672
                    if (FileHelper.Exist(raw.Local_path))
                    {
                        partialLocalPath = raw.Local_path;
                    }
                    else
                    {
                        partialLocalPath = "";
                    }
                }

                return partialLocalPath;
            }
        }

        public bool IsDirty
        {
            get
            {
                isDirty = MyProject.IsDataDirtyMasked(RsmPathId);
                if (isDirty)
                {
                    Console.WriteLine("Found target data with rmspathid = {0} is the dirty data list.", RsmPathId);
                }
                return isDirty;
            }
            set
            {
                isDirty = value;
                if (!isDirty)
                {
                    bool ret = MyProject.RemoveDirtyMask(RsmPathId);
                    if (ret)
                    {
                        Console.WriteLine("Remove target data with rmspathid = {0} from the dirty data list.", RsmPathId);
                    }
                }
            }
        }

        public bool IsEdit
        {
            get => raw.Edit_Status != 0;
            set => UpdateEditStatus(value ? 1 : 0);
        }

        public bool IsOfflineFileEdit => IsEdit;

        public bool IsModifyRights
        {
            get => raw.Modify_Rights_Status != 0;
            set => UpdateModifyRightsStatus(value ? 1 : 0);
        }

        private string GetPartialLocalPath()
        {
            return cache_folder + "\\" + "partial_" + raw.Rms_name;
        }

        public ProjectFile(int ProjectId,
            string homePath,
            database2.table.project.ProjectFile raw)
        {
            this.ProjectId = ProjectId;
            this.raw = raw;
            /*
             * homePath: C:\Users\oye\AppData\Local\SkyDRM\home\rms-centos7303.qapf1.qalab01.nextlabs.com\osmond.ye@nextlabs.com\myProject\6
             * 
             * RMSDisplayPath: /allen/sub_allen1/regNL.reg-2018-10-23-13-10-18.txt.nxl
            */
            cache_folder = homePath + RMSDisplayPath;
            if (isFolder)
            {
                FileHelper.CreateDir_NoThrow(cache_folder);
            }
            else
            {
                // Cache Folder always save a folder path without trail \
                // like: C:\Users\oye\AppData\Local\SkyDRM\home\rms-centos7303.qapf1.qalab01.nextlabs.com\osmond.ye@nextlabs.com\myProject\6\allen\sub_allen1
                //cache_folder = Directory.GetParent(cache_folder).FullName;
                // witou trail \

                //by osmond, work around map_path 2555
                cache_folder = FileHelper.GetParentPathWithoutTrailSlash_WorkAround(cache_folder);
            }
            AutoFixInContructor();
            // imple Leave A Copy
            ImplLeaveCopy();

        }

        private void AutoFixInContructor()
        {
            if (isFolder)
            {
                return;
            }
            if (raw.Is_offline)
            {
                // require the file must exist
                var fPath = cache_folder + "\\" + raw.Rms_name;
                if (raw.Local_path.Equals(fPath, StringComparison.CurrentCultureIgnoreCase))
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
            // comments by Allen: avoid 52838, online file autofix is not improtant
            //else // Not offline
            //{
            //    // May file is downloading, -- fix bug 52838 about project mark offline issue.
            //    if (Status != EnumNxlFileStatus.Online && Status != EnumNxlFileStatus.Downloading)
            //    {
            //        // reset online status
            //        Status = EnumNxlFileStatus.Online;

            //        // file is online but exist in local, del local
            //        var fPath = cache_folder + "\\" + raw.Rms_name;
            //        if (FileHelper.Exist(fPath))
            //        {
            //            FileHelper.Delete_NoThrow(fPath);
            //        }
            //    }
            //}
        }

        private void ImplLeaveCopy()
        {
            var leaveACopy = SkydrmLocalApp.Singleton.User.LeaveCopy_Feature;
            if (leaveACopy.Exist(raw.Rms_name))
            {
                // mark this file as local cached
                var newLocalPath = cache_folder + "\\" + Name;
                if (leaveACopy.MoveTo(raw.Rms_name, newLocalPath))
                {
                    // update this file status
                    OnChangeLocalPath(newLocalPath);
                    isOffline = true;
                    OnChangeOperationStaus(EnumNxlFileStatus.CachedFile);
                }

            }
        }

        public void DownlaodFile(bool isForViewOnly=false)
        {

            // sanity check
            if (isFolder)
            {
                return;
            }
            // tell log
            var downloadFilePath = cache_folder + "\\" + raw.Rms_name;
            app.Log.Info("Project download file,path=" + downloadFilePath);
            // begin
            OnChangeOperationStaus(EnumNxlFileStatus.Downloading);
            try
            {
                ProjectFileDownloadType type = isForViewOnly ? ProjectFileDownloadType.ForViewer : ProjectFileDownloadType.ForOffline;
                // before download, del previous one
                FileHelper.Delete_NoThrow(downloadFilePath, true);
                // set isViewOnly as false, by design requried
                app.Rmsdk.User
                    .DownlaodProjectFile(ProjectId, raw.Rms_path_id, ref cache_folder, type);
                cache_folder = FileHelper.GetParentPathWithoutTrailSlash_WorkAround(cache_folder);
                // update loacal path in db
                OnChangeLocalPath(downloadFilePath);
                // tell IRecentTouchedFiels
                OnChangeOperationStaus(EnumNxlFileStatus.DownLoadedSucceed);
            }
            catch (Exception ex)
            {
                OnChangeOperationStaus(EnumNxlFileStatus.DownLoadedFailed);
                // del 
                FileHelper.Delete_NoThrow(downloadFilePath);
                throw;
            }
        }

        // Download partial file to check file rights.
        public void DownloadPartial()
        {
            if (isFolder)
            {
                return;
            }

            // File name is attached prefix "partial" returned by sdk.
            var partialFPath = cache_folder + "\\" + "partial_" + raw.Rms_name;
            app.Log.Info("partical downlaod path: " + partialFPath);

            try
            {
                // delete it before downlaod
                FileHelper.Delete_NoThrow(partialFPath);
                app.Rmsdk.User.DownlaodProjectPartialFile(ProjectId, RsmPathId, ref cache_folder);
                cache_folder = FileHelper.GetParentPathWithoutTrailSlash_WorkAround(cache_folder);
                // update partial local path in db
                partialLocalPath = partialFPath;
            }
            catch(Exception E)
            {
                FileHelper.Delete_NoThrow(partialFPath);
                throw E;
            }
        }

        public void Export(string destinationFolder)
        {
            // sanity check
            if (isFolder)
            {
                return;
            }
            // tell log
            string currentUserTempPathOrDownLoadFilePath = Path.GetTempPath();
            app.Log.Info("Project Export file,currentUserTempPathOrDownLoadFilePath =" + currentUserTempPathOrDownLoadFilePath);
       
            try
            {
                // by commend, sdk will help us to record log: DownloadForOffline
                //app.User.AddNxlFileLog(raw.Local_path, NxlOpLog.Download, true);
                // set isViewOnly as false, by design requried
                app.Rmsdk.User
                    .DownlaodProjectFile(ProjectId, raw.Rms_path_id, ref currentUserTempPathOrDownLoadFilePath, ProjectFileDownloadType.Normal);

                File.Copy(currentUserTempPathOrDownLoadFilePath, destinationFolder,true);

            }
            catch (Exception)
            {                         
                throw;
            }
            finally
            {
                // del 
                FileHelper.Delete_NoThrow(currentUserTempPathOrDownLoadFilePath);
            }
        }

        private void OnOfflineMarkChanged(bool newMark)
        {
            if (raw.Is_offline == newMark)
            {
                return;
            }
            // update database
            app.DBFunctionProvider.UpdateProjectFileOfflineMark(raw.ProjectTablePk,raw.Id, newMark);
            // change cache
            raw.Is_offline = newMark;
            // tell ServiceMgr
            if (newMark)
            {
                NotifyIRecentTouchedFile(EnumNxlFileStatus.AvailableOffline);
            }
            else
            {
                NotifyIRecentTouchedFile(EnumNxlFileStatus.Online);
            }
        }

        // reset local path is "" and offline is false
        private void OnFixResetLocalPathAndOffline()
        {
            // offline
            app.DBFunctionProvider.UpdateProjectFileOfflineMark(raw.ProjectTablePk,raw.Id, false);
            raw.Is_offline = false;
            // local path
            app.DBFunctionProvider.UpdateProjectFileLocalpath(raw.ProjectTablePk,raw.Id, "");
            raw.Local_path = "";
            // operation status Online = 4 which indicates file is in remote.
            app.DBFunctionProvider.UpdateProjectFileOperationStatus(raw.ProjectTablePk, raw.Id, (int)EnumNxlFileStatus.Online);
            raw.Operation_status = (int)EnumNxlFileStatus.Online;
        }

        private void OnChangeLocalPath(string newPath)
        {
            if (raw.Local_path.Equals(newPath))
            {
                return;
            }
            // update db
            app.DBFunctionProvider.UpdateProjectFileLocalpath(raw.ProjectTablePk,raw.Id, newPath);
            // update cache
            raw.Local_path = newPath;
        }

        private void OnChangeOperationStaus(EnumNxlFileStatus status)
        {

            if (raw.Operation_status == (int)status)
            {
                return;
            }
            // change db
            app.DBFunctionProvider.UpdateProjectFileOperationStatus(raw.ProjectTablePk,
                raw.Id, (int)status);
            //
            if (status == EnumNxlFileStatus.Online)
            {
                isOffline = false;
            }
            if (status == EnumNxlFileStatus.AvailableOffline)
            {
                isOffline = true;
            }
            // update cache;
            raw.Operation_status = (int)status;
            // tell ServiceMgr
            NotifyIRecentTouchedFile(status);
        }

        private void UpdateEditStatus(int newStatus)
        {
            // Check changable first.
            if (raw.Edit_Status == newStatus)
            {
                return;
            }
            // Update db.
            app.DBFunctionProvider.UpdateProjectFileEditStatus(raw.ProjectTablePk, raw.Id, newStatus);
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
            // Update db.
            app.DBFunctionProvider.UpdateProjectFileModifyRightsStatus(raw.ProjectTablePk, raw.Id, newStatus);
            // Update cache.
            raw.Modify_Rights_Status = newStatus;
        }

        public void Remove() // partly regard as unmark
        {
            try
            {
                if (isFolder)
                {
                    return;
                }
                // tell skd to remove it - sdk not imple
                // delete local copy
                var path = cache_folder + "\\" + Name;
                if (!File.Exists(path))
                {
                    return;
                }
                try
                {
                    File.Delete(cache_folder + "\\" + Name);
                }
                catch (Exception)
                {
                    //ignored
                }
                // update Db
                //app.DBFunctionProvider.UpdateProjectFileOperationStatus(raw.ProjectTablePk,
                //raw.Id, (int)EnumNxlFileStatus.RemovedFromLocal);
                Status = EnumNxlFileStatus.RemovedFromLocal;
                // tell ServiceMgr
                // NotifyIRecentTouchedFile(EnumNxlFileStatus.RemovedFromLocal);
            }
            catch (Exception e)
            {
                app.Log.Error(e);
                throw;
            }

        }

        private void NotifyIRecentTouchedFile(EnumNxlFileStatus newStatus)
        {
            try
            {
                SkydrmLocalApp.
                    Singleton.
                    UserRecentTouchedFile.
                    UpdateOrInsert(newStatus,
                    Name);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void RemoveFromLocal()
        {
            Remove();
        }
       
        public void DoEdit(Action<EditCallBack> OnEditCompleteCallback)
        {
            FileEditorHelper.DoEdit(this.LocalDiskPath,OnEditCompleteCallback);
        }

        public void UploadEditedFile()
        {
            var app = SkydrmLocalApp.Singleton;
            try
            {
                // tell ServiceMgr 
                // NotifyIRecentTouchedFile(EnumNxlFileStatus.Uploading);
                // call api 
                var rms_display_path = app.DBFunctionProvider.QueryProjectLocalFileRMSParentFolder(ProjectId, raw.Id);
                app.Rmsdk.User.UploadEditedProjectFile(ProjectId, SubFoldIdFromDisplayPath(rms_display_path), LocalDiskPath);
            }
            catch
            {
                //NotifyIRecentTouchedFile(EnumNxlFileStatus.UploadFailed);
                throw;
            }
        }

        private string SubFoldIdFromDisplayPath(string rms_display_path)
        {
            string FolderId = "/";

            if (string.IsNullOrEmpty(rms_display_path))
            {
                throw new Exception("Fatal error, illegal parameters found.");
            }
            
            int lastIndex = rms_display_path.LastIndexOf("/");
            if (lastIndex != 0)
            {
                FolderId = rms_display_path.Substring(0, lastIndex + 1);
            }

            return FolderId.ToLower();
        }

        public string GetNxlFileLocalPath()
        {
            return LocalDiskPath;
        }

        public bool ModifyRights(List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration,
            UserSelectTags tags)
        {
            string nxlLocalPath = LocalDiskPath;

            if(string.IsNullOrEmpty(nxlLocalPath))
            {
                return false;
            }
            UInt32 pId = (UInt32)ProjectId;
            string fName = Name;
            string parentPId = SubFoldIdFromDisplayPath(RMSDisplayPath);

            var app = SkydrmLocalApp.Singleton;

            return app.Rmsdk.User.UpdateProjectNxlFileRights(nxlLocalPath, pId, fName, parentPId, rights, waterMark, expiration, tags);
        }

        private class InternalFileInfo : FileInfoBaseImpl
        {
            private ProjectFile Outer;
     

            public InternalFileInfo(ProjectFile outer): base(outer.PartialLocalPath) //Use Partial_LocalPath get Rights, Change 'LocalPath' to 'Partial_LocalPath'
            {
                Outer = outer;
            }

            public override string Name => Outer.Name;

            public override long Size => Outer.FileSize;

            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string RmsRemotePath => Outer.RMSRemotePath;

            public override bool IsCreatedLocal => false;

            public override  EnumFileRepo FileRepo => EnumFileRepo.REPO_PROJECT;

            public override string[] Emails => GetEmails();

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }

            // project does not supprot share
            private string[] GetEmails()
            {
                SkydrmLocalApp.Singleton.Log.Info("project file does not supprot share_feature");
                return new string[0];
            }

        }

    }

    // User add it in local, once it has been uploaded to RMS, it should be deleted in DB and local Disk
    public sealed class ProjectLocalAddedFile : IProjectLocalFile
    {
        int ProjectId;
        private database2.table.project.ProjectLocalFile raw;
        private InternalFileInfo fileinfo; // each get will generate a new-one
    
        public ProjectLocalAddedFile(int projectId,
            database2.table.project.ProjectLocalFile raw)
        {
            ProjectId = projectId;
            this.raw = raw;
        }

        public string Name { get => raw.Name; }

        public DateTime LocalModifiedTime { get => raw.Last_modified_time; }

        public long FileSize { get => raw.Size; }

        public string LocalPath => raw.Path;

        public EnumNxlFileStatus Status
        {
            get => (EnumNxlFileStatus)raw.Operation_status; set => ChangeOperationStaus(value);
        }

        public string LocalDiskPath => LocalPath;

        public DateTime LastModifiedTime => raw.Last_modified_time;

        //
        // by osmond
        //
        public string RMSRemotePath => GetThisFileRemotePath();

        public string SharedEmails => ""; // MyProject does not support share curretly

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public string GetThisFileRemotePath()
        {
            var app = SkydrmLocalApp.Singleton;
            var folder = app.DBFunctionProvider.QueryProjectLocalFileRMSParentFolder(ProjectId, raw.ProjectFileTablePk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Name;
        }

        public void ChangeOperationStaus(EnumNxlFileStatus status)
        {
            if (raw.Operation_status == (int)status)
            {
                return;
            }
            // change db
            var app = SkydrmLocalApp.Singleton;
            app.DBFunctionProvider.UpdateProjectLocalFileOperationStatus(raw.ProjectTablePk,
                raw.Id, (int)status);
            // update cache
            raw.Operation_status = (int)status;
            // tell ServiceMgr
            NotifyIRecentTouchedFile(status);
        }
        public void Upload()
        {
            var app = SkydrmLocalApp.Singleton;
            try
            {
                // tell ServiceMgr 
                NotifyIRecentTouchedFile(EnumNxlFileStatus.Uploading);

                // call api 
                var folder = app.DBFunctionProvider.QueryProjectLocalFileRMSParentFolder(ProjectId, raw.ProjectFileTablePk);

                app.Log.InfoFormat("###Call Upload Project File api, projectId:{0}, rmsParentFolder:{1}, nxlFilePath:{2}", ProjectId, folder.ToLower(), LocalDiskPath);

                app.Rmsdk.User.UploadProjectFile(ProjectId, folder.ToLower(), LocalDiskPath);
                // delete in db
                app.DBFunctionProvider.DeleteProjectLocalFile(raw.ProjectTablePk, raw.Id);
                // tell ServiceMgr
                NotifyIRecentTouchedFile(EnumNxlFileStatus.UploadSucceed);
                // Every done, begin impl leave a copy featue
                if (app.User.LeaveCopy)
                {
                    app.User.LeaveCopy_Feature.AddFile(LocalDiskPath);

                    // Delete the sdk local file after the "Leave a copy" file move to repo folder.(fix bug 53692)
                    app.Rmsdk.User.RemoveLocalGeneratedFiles(LocalDiskPath);
                }
            }
            catch
            {
                NotifyIRecentTouchedFile(EnumNxlFileStatus.UploadFailed);
                throw;
            }
        }
        
        public void Remove()
        {
            var app = SkydrmLocalApp.Singleton;
            // delete at local disk
            if (FileHelper.Exist(raw.Path))
            {
                FileHelper.Delete_NoThrow(raw.Path);
            }
            else
            {
                app.Log.Warn("file to be del,but not in local, " + raw.Path);
            }

            // remove from database
            app.DBFunctionProvider.DeleteProjectLocalFile(raw.ProjectTablePk, raw.Id);
            // tell skd to remove it
            app.Rmsdk.User.RemoveLocalGeneratedFiles(Name);

            // Fix bug 51938, If LeaveCopy,should delete file in LeaveCopy Folder
            if (app.User.LeaveCopy)
            {
                var leaveACopy = SkydrmLocalApp.Singleton.User.LeaveCopy_Feature;
                if (leaveACopy.Exist(Name))
                {
                    leaveACopy.DeleteFile(Name);
                }
            }
            // Tell service mgr
            if (app.User.LeaveCopy)
            {
                if (raw.Operation_status == (int)EnumNxlFileStatus.CachedFile)
                {
                    NotifyIRecentTouchedFile(EnumNxlFileStatus.Online);
                }
                else
                {
                    NotifyIRecentTouchedFile(EnumNxlFileStatus.RemovedFromLocal);
                }
                
            }
            else
            {
                NotifyIRecentTouchedFile(EnumNxlFileStatus.RemovedFromLocal);
            }

        }

        private void NotifyIRecentTouchedFile(EnumNxlFileStatus newStatus)
        {
            try
            {
                SkydrmLocalApp.
                    Singleton.
                    UserRecentTouchedFile.
                    UpdateOrInsert(newStatus,
                    Name);
            }
            catch (Exception)
            {

            }
        }

        public void UploadToRms()
        {
            Upload();
        }

        public void RemoveFromLocal()
        {
            Remove();
        }

        public void DoEdit(Action<EditCallBack> OnEditCompleteCallback = null)
        {
            FileEditorHelper.DoEdit(this.LocalDiskPath, OnEditCompleteCallback);
        }

        public string GetNxlFileLocalPath()
        {
            return LocalDiskPath;
        }

        private class InternalFileInfo : FileInfoBaseImpl
        {
            private ProjectLocalAddedFile Outer;

            public InternalFileInfo(ProjectLocalAddedFile outer) : base(outer.LocalDiskPath)
            {
                Outer = outer;
            }

            public override string Name => Outer.Name;

            public override long Size => Outer.FileSize;

            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string RmsRemotePath => Outer.RMSRemotePath;

            public override bool IsCreatedLocal => true;

            public override string[] Emails => GetEmails();

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_PROJECT;

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }
           
            // project does not supprot share
            private string[] GetEmails()
            {
                SkydrmLocalApp.Singleton.Log.Info("project localadded file does not supprot share_feature");
                return new string[0];
            }
        }
    }
}
