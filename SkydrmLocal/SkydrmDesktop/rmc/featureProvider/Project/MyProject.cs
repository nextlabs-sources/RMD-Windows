using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database2.table.project;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.featureProvider.FileInfo;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using static Skydrmlocal.rmc.database2.FunctionProvider;
using SkydrmDesktop.rmc.database.table.project;
using SkydrmDesktop.rmc.featureProvider.externalDrive;

namespace SkydrmLocal.rmc.featureProvider
{
    public sealed class MyProjects : IMyProjects
    {
        readonly private SkydrmApp app;
        readonly private log4net.ILog log;
        public MyProjects(SkydrmApp app)
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
            SkydrmApp.Singleton.Log.Info("Project HeartBeat");
            SkydrmApp.Singleton.InvokeEvent_ProjectUpdate();
        }
    }

    public sealed class MyProject : IMyProject
    {
        readonly private SkydrmApp app;
        readonly private log4net.ILog log;

        private database2.table.project.Project rawProject;

        private string working_path;

        private static List<string> mDirty_RecordingList= new List<string>();
        private static List<string> mDirty_ModifyList = new List<string>();

        public MyProject(SkydrmApp app, database2.table.project.Project rawProject)
        {
            this.app = app;
            this.log = app.Log;
            this.rawProject = rawProject;

            // make sure working path exists  <= IUser
            working_path = app.User.WorkingFolder + "\\myProject\\" + Id;
            Directory.CreateDirectory(working_path);
        }

        public string WorkingPath => working_path;

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
                    rt.Add(new ProjectFile(Id, WorkingPath, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IProjectFile[] ListAllProjectFile(bool toDisplay = false)
        {
            try
            {
                var rt = new List<ProjectFile>();
                var retCache = app.DBFunctionProvider.ListAllProjectFile(Id);
                foreach (var i in retCache)
                {
                    // required each new fill do auto-fix 
                    rt.Add(new ProjectFile(Id, WorkingPath, i, toDisplay));
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
                var rt = new List<IOfflineFile>();
                foreach (var i in app.DBFunctionProvider.ListProjectOfflineFile(Id))
                {
                    if (i.Is_offline && File.Exists(i.Local_path))
                    {
                        rt.Add(new ProjectFile(Id, WorkingPath, i));
                    }
                }

                // Shared with the project offline file
                foreach(var i in app.DBFunctionProvider.ListSharedWithProjectOfflineFile(Id))
                {
                    if(i.IsOffline && File.Exists(i.LocalPath))
                    {
                        rt.Add(new ProjectSharedWithMeFile(Id, WorkingPath, i));
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

        public IProjectLocalFile AddLocalFile(string parentFolder,
            string filePath,
            List<FileRights> rights,
            WaterMarkInfo waterMark,
            Expiration expiration,
            UserSelectTags tags)
        {
            string newAddedName = string.Empty;
            try
            {
                // tell api to convert this this
                app.Log.Info("protect the file to project: "+ filePath);
                var outPath = app.Rmsdk.User.ProtectFileToProject(Id,
                    filePath, rights, waterMark, expiration, tags);

                // handle sdk nxl file
                string destFilePath = FileHelper.CreateNxlTempPath(WorkingPath, parentFolder, outPath);
                outPath = FileHelper.HandleAddedFile(destFilePath, outPath, out bool isOverWriteUpload, 
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isExistInLocal = false;
                        IProjectLocalFile[] localFiles = ListLocalAdded(parentFolder);
                        isExistInLocal = localFiles.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        // think about search rms file exist:1. network connected---use api to search ?? 2. network outages---use db to search
                        // search rms file exist from db
                        bool isExistInRms = false;
                        IProjectFile[] rmsFiles = ListFiles(parentFolder);
                        isExistInRms = rmsFiles.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                        return isExistInLocal || isExistInRms;
                    },
                    (fileName) => {
                        // search local pendingUpload file exist from db
                        bool isCan = true;
                        IProjectLocalFile[] localFiles = ListLocalAdded(parentFolder);
                        IProjectLocalFile localFile = localFiles.FirstOrDefault(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                        if (localFile != null && localFile.Status == EnumNxlFileStatus.Uploading)
                        {
                            isCan = false;
                        }
                        return isCan;
                    });

                // find this file in api
                //app.Log.Info("get the finger print of the new protected file");
                //var fp = app.Rmsdk.User.GetNxlFileFingerPrint(outPath);
                // by osmond, feature changed, allow user to portect a file which has not any permissons for this user
                newAddedName = Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                // store this file into db;
                app.Log.Info("store the new protected file into database");
                app.DBFunctionProvider.AddLocalFileToProject(Id, parentFolder,
                    newAddedName, outPath, (int)newAddedFileSize, File.GetLastAccessTime(outPath),
                    JsonConvert.SerializeObject(new User.PendingUploadFileConfig() { overWriteUpload= isOverWriteUpload }));

                // tell service mgr
                app.MessageNotify.NotifyMsg(newAddedName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Succeed"), 
                    EnumMsgNotifyType.LogMsg, MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.WaitingUpload);

                if (app.User.SelectedOption == 1)
                {
                    IProjectFile[] rmsFiles = ListFiles(parentFolder);
                    IProjectFile rmsFile = rmsFiles.FirstOrDefault(f => f.Name.Equals(newAddedName, StringComparison.OrdinalIgnoreCase));

                    if (rmsFile != null)
                    {
                        if (app.User.LeaveCopy)
                        {
                            if (rmsFile.isOffline || rmsFile.IsEdit || rmsFile.Status == EnumNxlFileStatus.Online /* fix bug 63618 */)
                            {
                                var fp = app.Rmsdk.User.GetNxlFileFingerPrint(outPath);
                                rmsFile.UpdateWhenOverwriteInLeaveCopy(fp.duid, EnumNxlFileStatus.Online, newAddedFileSize, File.GetLastWriteTime(outPath));
                            }
                        }
                        else
                        {
                            if (rmsFile.isOffline || rmsFile.IsEdit)
                            {
                                rmsFile.Status = EnumNxlFileStatus.Online;
                            }
                        }

                        if (rmsFile.IsEdit)
                        {
                            rmsFile.IsEdit = false;
                        }
                    }

                }

                // return this local file to caller
                return ListLocalAdded(parentFolder).First((i) =>
                {
                    return i.Name.Equals(newAddedName, StringComparison.OrdinalIgnoreCase);
                });

            }
            catch (Exception e)
            {
                app.Log.Error("Failed to Protect the file"+e.Message, e);

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
                    // Even though local db will be deleted by cascading, but mem cache can't.
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

        // Support sharing transaction.
        public IProjectFile[] SyncFilesEx(string FolderId, FilterType type)
        {
            // remote
            var remote = app.Rmsdk.User.ProjectListTotalFiles(uint.Parse(Id.ToString()), FolderId, type);
            var local = ListFiles(FolderId);

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
                    // Even though local db will be deleted by cascading, but mem cache can't.
                    if (projectfile_table_pk != -1)
                    {
                        app.DBFunctionProvider.DeleteProjectFolderLocalFiles(RowId, projectfile_table_pk);
                    }
                }
            }

            var ff = new List<InstertProjectFileEx>();
            foreach (var f in FilterOutNotModified(local, remote))
            {
                ff.Add(new InstertProjectFileEx()
                {
                    project_id = Id,
                    file_id = f.id,
                    file_duid = f.duid,
                    file_display_path = f.pathDisplay,
                    file_path_id = f.pathId,
                    file_nxl_name = f.name,
                    file_lastModifiedTime = (long)f.lastModified,
                    file_creationTime = (long)f.createTime,
                    file_size = (long)f.size,
                    file_rms_ownerId = (int)f.owner.userId,
                    file_ownerDisplayName = f.owner.displayName,
                    file_ownerEmail = f.owner.email,
                    // extend.
                    isShared = f.isShared? 1: 0,
                    isRevoked = f.isRevoked? 1: 0,
                    sharedWithProject = f.sharedWithProject
                });
            }

            app.DBFunctionProvider.UpsertProjectFileBatchEx(ff.ToArray()); 

            return ListFiles(FolderId);
        }
        public IProjectSharedWithMeFile[] ListSharedWithMeFiles()
        {
            try
            {
                var rt = new List<ProjectSharedWithMeFile>();
                var retCache = app.DBFunctionProvider.ListSharedWithProjectFile(Id);
                foreach (var i in retCache)
                {
                    // required each new fill do auto-fix 
                    rt.Add(new ProjectSharedWithMeFile(Id, WorkingPath, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }
        public IProjectSharedWithMeFile[] SyncSharedWithMeFiles()
        {
            var remotes = app.Rmsdk.User.ProjectListTotalSharedWithMeFiles((uint)Id);
            var locals = ListSharedWithMeFiles();

            var diffset = from i in locals
                          let rNames = from j in remotes select j.name
                          where !rNames.Contains(i.Name)
                          select i;

            // delete file that had been deleted on remote but still in local --> local also should delete them.
            foreach (var i in diffset)
            {
                app.DBFunctionProvider.DeleteSharedWithProjectFile(RowId, i.Duid);
            }

            var ff = new List<InstertSharedWithProjectFile>();
            foreach(var f in FilterOutNotModified(locals, remotes))
            {
                ff.Add(new InstertSharedWithProjectFile()
                {
                    project_id = Id,
                    file_duid = f.duid,
                    file_name = f.name,
                    file_type = f.fileType,
                    file_size = (long)f.size,
                    shared_date = (long)f.sharedDate,
                    shared_by = f.sharedBy,
                    transaction_id = f.transactionId,
                    transaction_code = f.transactionCode,
                    shared_url = "", // Can't get
                    rights_json = "", // Todo: need to convert 'f.rights',
                    is_owner = f.isOwner ? 1:0,
                    protection_type = (int)f.protectType,
                    shared_by_project = f.sharedByProject
                });
            }
            // insert to db.
            app.DBFunctionProvider.UpsertSharedWithProjectFileBatch(ff.ToArray());
            return ListSharedWithMeFiles();
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
                        JavaTimeConverter.ToCSDateTime(i.lastModifedTime).ToString()!=l.LastModifiedTime.ToString()
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

        // Extend for supporting sharing transaction.
        private ProjectFileInfoEx[] FilterOutNotModified(IProjectFile[] locals, ProjectFileInfoEx[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }

            var rt = new List<ProjectFileInfoEx>();
            foreach(var  i in remotes)
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
                    if (i.name != l.Name ||
                        (long)i.size != l.FileSize ||
                        JavaTimeConverter.ToCSDateTime((long)i.lastModified).ToString() != l.LastModifiedTime.ToString()
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

        private sdk.ProjectSharedWithMeFile[] FilterOutNotModified(IProjectSharedWithMeFile[] locals,
            sdk.ProjectSharedWithMeFile[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }

            var rt = new List<sdk.ProjectSharedWithMeFile>();
            foreach (var i in remotes)
            {
                // find in local
                try
                {
                    // find in local
                    // if use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                    {
                        if (i.name != j.Name)
                        {
                            return false;
                        }
                        return true;
                    });

                    // if no matching element, will return null.
                    if (l == null)
                    {
                        app.Log.Info("ProjectSharedWithMeFile local list no matching element");
                        rt.Add(i);
                        continue;
                    }

                    // judege whether modified

                    // only care file size
                    if ((long)i.size != l.FileSize)
                    {
                        rt.Add(i);
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

        public bool CheckFileExists(string pathId)
        {
            app.Rmsdk.User.ProjectFileIsExist(Id, pathId, out bool rt);
            return rt;
        }
    }

    public sealed class ProjectFile : IProjectFile, IOfflineFile
    {
        int ProjectId;
        private database2.table.project.ProjectFile raw;
        // folder: local cache path,
        private string cache_folder;
        SkydrmApp app = SkydrmApp.Singleton;
        private IFileInfo fileInfo; // each get will generate a new-one
        private bool isDirty;
        private string partialLocalPath;

        public ProjectFile(int ProjectId,
            string homePath,
            database2.table.project.ProjectFile raw, bool toDisplay = false)
        {
            this.ProjectId = ProjectId;
            this.raw = raw;

            if (toDisplay)
            {
                return;
            }
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

        #region Impl IProjectFile
        public bool isFolder { get => raw.Rms_is_folder; }

        public bool isOffline { get => raw.Is_offline; set => OnOfflineMarkChanged(value); }

        public string RsmPathId => raw.Rms_path_id;

        public string RMSDisplayPath => raw.Rms_display_path;

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

        public DateTime CreationTime => raw.Rms_creationTime;

        public int RmsOwnerId => raw.Rms_OwnerId;

        public string OwnerDisplayName => raw.Rms_OwnerDisplayName;

        public string OwnerEmail => raw.Rms_OwnerEmail;

        public string RmsFileId => raw.Rms_file_id;

        public string RmsDuId => raw.Rms_duid;

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

        public bool IsModifyRights
        {
            get => raw.Modify_Rights_Status != 0;
            set => UpdateModifyRightsStatus(value ? 1 : 0);
        }

        // Extend for sharing transaction
        public bool IsShared
        {
            get => raw.IsShared;
            set => UpdateIsShared(value);
        }

        public bool IsRevoked
        {
            get => raw.IsRevoked;
            set => UpdateIsRevoked(value);
        }

        public List<uint> SharedToProjects
        {
            get => raw.SharedWithProjects;
            set => UpdateSharedWithProjects(value);
        }

        public void UpdateRecipients(List<uint> addRecipients, List<uint> removedRecipients, string comment)
        {
            try
            {
                app.Rmsdk.User.ProjectUpdateSharedFileRecipients(RmsDuId, addRecipients, removedRecipients, comment);
            }
            catch (Exception e)
            {
                app.Log.Error(e.ToString());
                throw;
            }
        }

        public void UpdateWhenOverwriteInLeaveCopy(string duid, EnumNxlFileStatus fStatus, long fSize, DateTime fLastModifed)
        {
            Status = fStatus;
            raw.Rms_fileSize = (int)fSize;
            raw.Rms_lastModifiedTime = fLastModifed;

            app.DBFunctionProvider.UpdateProjectFileWhenOverwriteInLeaveCopy(raw.ProjectTablePk,
                raw.Id, duid, (int)fStatus, fSize, fLastModifed);
        }

        public void ShareFile(List<uint> recipients, string comment)
        {
            try
            {
                app.Rmsdk.User.ProjectShareFile((uint)ProjectId, recipients, raw.Rms_name, raw.Rms_path_id, raw.Local_path, comment);
            }
            catch (Exception e)
            {
                app.Log.Error(e.ToString());
                throw;
            }
        }
        public bool RevokeFile()
        {
           return app.Rmsdk.User.ProjectRevokeShareFile(RmsDuId);
        }

        public void DownlaodFile(bool isForViewOnly = false)
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
                string targetPath = cache_folder;
                app.Rmsdk.User.DownlaodProjectFile(ProjectId, raw.Rms_path_id, ref targetPath, type);

                // check out path, if file name exceed 128 characters, the server return name will be truncated.
                if (!downloadFilePath.Equals(targetPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    // use for delete file.
                    downloadFilePath = targetPath;
                    throw new SkydrmException(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_FileName128"),
                        ExceptionComponent.FEATURE_PROVIDER);
                }

                // update loacal path in db
                OnChangeLocalPath(downloadFilePath);

                OnChangeOperationStaus(EnumNxlFileStatus.DownLoadedSucceed);

            }
            catch (Exception ex)
            {
                app.Log.Error("failed in downlaod file=" + downloadFilePath, ex);
                OnChangeOperationStaus(EnumNxlFileStatus.DownLoadedFailed);

                // del 
                FileHelper.Delete_NoThrow(downloadFilePath);
                throw;
            }
        }

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

                string targetPath = cache_folder;
                app.Rmsdk.User.DownlaodProjectPartialFile(ProjectId, RsmPathId, ref targetPath);

                partialFPath = targetPath;
                // update partial local path in db
                partialLocalPath = partialFPath;
            }
            catch (Exception e)
            {
                app.Log.Error("failed partial downlaod file=" + partialFPath, e);
                FileHelper.Delete_NoThrow(partialFPath);
                throw e;
            }
        }

        public void GetNxlHeader()
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
                partialLocalPath = app.Rmsdk.User.ProjectGetNxlFileHeader(ProjectId, RsmPathId, cache_folder);
            }
            catch (Exception e)
            {
                app.Log.Error("failed in GetNxlHeader=" + partialFPath, e);
                FileHelper.Delete_NoThrow(partialFPath);
                throw e;
            }
        }

        public void Export(string destinationFolder)
        {
            // Sanity check
            if (isFolder)
            {
                return;
            }
            var app = SkydrmApp.Singleton;
            app.Log.Info(string.Format("Project try to export file, path {0}", destinationFolder));

            string currentUserTempPathOrDownloadFilePath = Path.GetTempPath();
            try
            {
                app.Rmsdk.User.CopyNxlFile(Name, RMSRemotePath, NxlFileSpaceType.project, ProjectId.ToString(),
                   Path.GetFileName(destinationFolder), currentUserTempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                   true);

                string downloadFilePath = currentUserTempPathOrDownloadFilePath + Path.GetFileName(destinationFolder);
                File.Copy(downloadFilePath, destinationFolder, true);
            }
            catch (Exception e)
            {
                app.Log.Error(string.Format("Project failed to export file {0}.", destinationFolder), e);
                throw;
            }
            finally
            {
                FileHelper.Delete_NoThrow(currentUserTempPathOrDownloadFilePath);
            }
        }

        public void DoEdit(Action<EditCallBack> OnEditCompleteCallback)
        {
            FileEditorHelper.DoEdit(this.LocalDiskPath, OnEditCompleteCallback);
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
            }
            catch (Exception e)
            {
                app.Log.Error(e);
                throw;
            }

        }

        public void UploadEditedFile()
        {
            var app = SkydrmApp.Singleton;
            try
            {
                // tell ServiceMgr 
                // NotifyIRecentTouchedFile(EnumNxlFileStatus.Uploading);

                // call api 
                var rms_display_path = app.DBFunctionProvider.QueryProjectLocalFileRMSParentFolder(ProjectId, raw.Id);
                app.Rmsdk.User.UploadEditedProjectFile(ProjectId, SubFoldIdFromDisplayPath(rms_display_path), LocalDiskPath);

                // Notofy msg
                app.MessageNotify.NotifyMsg(raw.Rms_name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Upload_EditedFile_Succeed"), EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD_Edit, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Offline);
            }
            catch (Exception e)
            {
                // Notofy msg
                app.MessageNotify.NotifyMsg(raw.Rms_name, e.Message, EnumMsgNotifyType.PopupBubble,
                    MsgNotifyOperation.UPLOAD_Edit, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Offline);

                throw;
            }
        }

        public bool ModifyRights(List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration,
            UserSelectTags tags)
        {
            string nxlLocalPath = LocalDiskPath;

            if (string.IsNullOrEmpty(nxlLocalPath))
            {
                return false;
            }
            UInt32 pId = (UInt32)ProjectId;
            string fName = Name;
            string parentPId = SubFoldIdFromDisplayPath(RMSDisplayPath);

            var app = SkydrmApp.Singleton;

            return app.Rmsdk.User.UpdateProjectNxlFileRights(nxlLocalPath, pId, fName, parentPId, rights, waterMark, expiration, tags);
        }
        #endregion // Impl IProjectFile

        #region Impl IOffline 
        public string RMSRemotePath => raw.Rms_display_path;

        public bool IsOfflineFileEdit => IsEdit;

        public void RemoveFromLocal()
        {
            Remove();
        }
        #endregion // Impl IOffline

        #region Impl common method
        public string Name { get => raw.Rms_name; }

        public long FileSize => raw.Rms_fileSize;

        public IFileInfo FileInfo => new InternalFileInfo(this);

        public EnumNxlFileStatus Status
        {
            get => (EnumNxlFileStatus)raw.Operation_status;
            set => OnChangeOperationStaus(value);
        }

        public string LocalDiskPath { get => raw.Local_path; }

        public DateTime LastModifiedTime => raw.Rms_lastModifiedTime;
        #endregion // Impl common method

        #region private method
        private string GetPartialLocalPath()
        {
            return cache_folder + "\\" + "partial_" + raw.Rms_name;
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
            var leaveACopy = SkydrmApp.Singleton.User.LeaveCopy_Feature;
            if (leaveACopy.Exist(raw.Rms_name, cache_folder))
            {
                // mark this file as local cached
                if (leaveACopy.MoveTo(cache_folder, Name))
                {
                    var newLocalPath = cache_folder + "\\" + Name;
                    // update this file status
                    OnChangeLocalPath(newLocalPath);
                    isOffline = true;
                    OnChangeOperationStaus(EnumNxlFileStatus.CachedFile);
                }

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

        // For sharing transaction.
        private void UpdateIsShared(bool isShared)
        {
            if(raw.IsShared == isShared)
            {
                return;
            }
            // update
            app.DBFunctionProvider.UpdateProjectFileIsShared(raw.ProjectTablePk, raw.Id, isShared ? 1 : 0);
            // Actually, this has been updated in above operation, since before update into db, will firstly update into memory.
            raw.IsShared = isShared;
        }
        private void UpdateIsRevoked(bool isRevoked)
        {
            if (raw.IsRevoked == isRevoked)
            {
                return;
            }
            // update
            app.DBFunctionProvider.UpdateProjectFileIsRevoked(raw.ProjectTablePk, raw.Id, isRevoked ? 1 : 0);
            raw.IsRevoked = isRevoked;
        }
        private void UpdateSharedWithProjects(List<uint> list)
        {
            if (raw.SharedWithProjects == list)
            {
                return;
            }
            // update
            app.DBFunctionProvider.UpdateProjectFileSharedWith(raw.ProjectTablePk, raw.Id, list);
            raw.SharedWithProjects = list;
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
        #endregion // private method

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

            // project does not supprot share to person
            private string[] GetEmails()
            {
                SkydrmApp.Singleton.Log.Info("project file does not supprot share_feature");
                return new string[0];
            }

        }

    }

    public sealed class ProjectSharedWithMeFile : IProjectSharedWithMeFile, IOfflineFile
    {
        private int projectId;
        private SharedWithProjectFile raw;
        private string cacheFolder;
        private SkydrmApp app = SkydrmApp.Singleton;
        private IFileInfo fileInfo; // each get will generate a new-one
        private string partialLocalPath;

        public ProjectSharedWithMeFile(int pid, string homePath, SharedWithProjectFile rawFile)
        {
            this.projectId = pid;
            this.raw = rawFile;

            this.cacheFolder = homePath + @"\SharedWithMeFiles";
            if (!Directory.Exists(cacheFolder))
            {
                FileHelper.CreateDir_NoThrow(cacheFolder);
            }

            // Auto fix ?
        }

        #region Impl IProjectSharedWithMeFile
        public string Duid => raw.RmsDuid;

        public string Type => raw.RmsFileType;

        public DateTime SharedDate => raw.RmsSharedDate;

        public string SharedBy => raw.RmsSharedBy;

        public string sharedByProject => raw.RmsSharedByProject;

        public string SharedLinkeUrl => raw.RmsSharedUrl;

        public uint ProtectType => (uint)raw.RmsProtectionType;

        public FileRights[] Rights => NxlHelper.FromRightStrings(JsonConvert.DeserializeObject<string[]>(raw.RmsRightsJson));

        public string Comments => "";

        public bool IsOwner => raw.RmsIsOwner;

        public bool IsOffline { get => raw.IsOffline; set => OnChangedIsOfflineMark(value); }

        public string PartialLocalPath
        {
            get
            {
                if (string.IsNullOrEmpty(partialLocalPath))
                {
                    partialLocalPath = GetPartialLocalPath();
                }

                if (!FileHelper.Exist(partialLocalPath))
                {
                    partialLocalPath = "";
                }

                return partialLocalPath;
            }
        }

        public bool IsEdit // Is supported?
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }
        public bool IsModifyRights
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public void Download(bool isForViewOnly = true)
        {
            // tell log
            var downloadFilePath = cacheFolder + "\\" + raw.RmsName;
            app.Log.Info("ProjectSharedWithMeFile download, path=" + downloadFilePath);
            // begin
            OnChangeOperationStaus(EnumNxlFileStatus.Downloading);

            try
            {
                // before download, del previous one
                FileHelper.Delete_NoThrow(downloadFilePath, true);

                var tmpFolder = cacheFolder;
                //
                app.Rmsdk.User.ProjectDownloadSharedWithMeFile((uint)projectId, raw.RmsTransactionId, raw.RmsTransactionCode,
                    ref tmpFolder, isForViewOnly);

                // update loacal path in db
                OnChangeLocalPath(downloadFilePath);
                OnChangeOperationStaus(EnumNxlFileStatus.DownLoadedSucceed);
            }
            catch (Exception e)
            {
                OnChangeOperationStaus(EnumNxlFileStatus.DownLoadedFailed);
                // del 
                FileHelper.Delete_NoThrow(downloadFilePath);

                throw;
            }
        }

        public void DownloadPartial()
        {
            // File name is attached prefix "partial" returned by sdk.
            var partialPath = cacheFolder + "\\" + "partial_" + raw.RmsName;
            app.Log.Info("ProjectSharedWithMeFile Partial download, path=" + partialPath);

            try
            {
                // before download, del previous one
                FileHelper.Delete_NoThrow(partialPath, true);

                var tmpFolder = cacheFolder;
                app.Rmsdk.User.ProjectPartialDownloadSharedWithMeFile((uint)projectId, raw.RmsTransactionId, raw.RmsTransactionCode,
                    ref tmpFolder, true);

                // set
                partialLocalPath = partialPath;
            }
            catch (Exception e)
            {
                // del 
                FileHelper.Delete_NoThrow(partialPath);
                throw;
            }
        }

        public void Export(string destinationFolder)
        {
            var app = SkydrmApp.Singleton;
            app.Log.Info(string.Format("Project try to export file, path {0}", destinationFolder));

            string currentUserTempPathOrDownloadFilePath = Path.GetTempPath();
            try
            {
                app.Rmsdk.User.CopyNxlFile(Name, RMSRemotePath, NxlFileSpaceType.project, projectId.ToString(),
                   Path.GetFileName(destinationFolder), currentUserTempPathOrDownloadFilePath, NxlFileSpaceType.local_drive, "",
                   true, raw.RmsTransactionCode, raw.RmsTransactionId);

                string downloadFilePath = currentUserTempPathOrDownloadFilePath + Path.GetFileName(destinationFolder);
                File.Copy(downloadFilePath, destinationFolder, true);
            }
            catch (Exception e)
            {
                app.Log.Error(string.Format("Project failed to export file {0}.", LocalDiskPath), e);
                throw;
            }
            finally
            {
                FileHelper.Delete_NoThrow(currentUserTempPathOrDownloadFilePath);
            }
        }

        public void Remove()
        {
            var path = cacheFolder + "\\" + Name;
            if (FileHelper.Exist(path))
            {
                FileHelper.Delete_NoThrow(path);
            } else
            {
                FileHelper.Delete_NoThrow(LocalDiskPath);
            }
            // update db
            OnChangeLocalPath("");
            OnChangeOperationStaus(EnumNxlFileStatus.Online);
        }

        public bool ReShare(List<uint> recipients, string emailLsit = "")
        {
            try
            {
               var rt = app.Rmsdk.User.ProjectReshareSharedWithMeFile((uint)projectId, raw.RmsTransactionCode,
                    raw.RmsTransactionId, recipients, emailLsit);
               return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion // Impl IProjectSharedWithMeFile

        #region Impl IOfflineFile
        public string RMSRemotePath  => "/" + raw.RmsName;
        public DateTime LastModifiedTime => SharedDate;
        public bool IsOfflineFileEdit => IsOffline;
        public void RemoveFromLocal()
        {
            Remove();
        }

        #endregion // Impl IOfflineFile

        #region Impl common method
        public string Name => raw.RmsName;
        public long FileSize => raw.RmsFileSize;
        public string LocalDiskPath => raw.LocalPath;
        public IFileInfo FileInfo => new InternalFileInfo(this);
        public EnumNxlFileStatus Status
        {
            get => (EnumNxlFileStatus)raw.OperationStatus;
            set => OnChangeOperationStaus(value);
        }

        #endregion // Impl common method
  
        #region Private method
        private void OnChangedIsOfflineMark(bool newIsOffline)
        {
            if (raw.IsOffline == newIsOffline)
            {
                return;
            }
            // update database
            app.DBFunctionProvider.UpdateSharedWithProjectFileOfflineMark(raw.ProjectTablePK, raw.Id, newIsOffline);
            // change cache
            raw.IsOffline = newIsOffline;
        }
        private string GetPartialLocalPath()
        {
            return cacheFolder + "\\" + "partial_" + raw.RmsName;
        }
        private void OnChangeLocalPath(string newPath)
        {
            if (raw.LocalPath.Equals(newPath))
            {
                return;
            }
            // update db
            app.DBFunctionProvider.UpdateSharedWithProjectFileLocalpath(raw.ProjectTablePK, raw.Id, newPath);
            // update cache
            raw.LocalPath = newPath;
        }
        private void OnChangeOperationStaus(EnumNxlFileStatus status)
        {

            if (raw.OperationStatus == (int)status)
            {
                return;
            }
            // change db
            app.DBFunctionProvider.UpdateSharedWithProjectFileStatus(raw.ProjectTablePK,
                raw.Id, (int)status);
            //
            if (status == EnumNxlFileStatus.Online)
            {
                IsOffline = false;
            }
            if (status == EnumNxlFileStatus.AvailableOffline)
            {
                IsOffline = true;
            }
            // update cache;
            raw.OperationStatus = (int)status;
        }

        #endregion // Private method

        private class InternalFileInfo : FileInfoBaseImpl
        {
            private ProjectSharedWithMeFile Outer;
            public InternalFileInfo(ProjectSharedWithMeFile outer) : base(outer.PartialLocalPath)
            {
                this.Outer = outer;
            }

            public override DateTime LastModified => Outer.LastModifiedTime;

            public override string Name => Outer.Name;

            public override long Size => Outer.FileSize;

            public override string RmsRemotePath => Outer.RMSRemotePath;

            public override bool IsCreatedLocal => false;

            public override string[] Emails => new string[0]; // Used for share to person

            public override EnumFileRepo FileRepo => EnumFileRepo.REPO_PROJECT;
        }
    }

    // User add it in local, once it has been uploaded to RMS, it should be deleted in DB and local Disk
    public sealed class ProjectLocalAddedFile : IProjectLocalFile
    {
        int ProjectId;
        private database2.table.project.ProjectLocalFile raw;
        //private InternalFileInfo fileinfo; // each get will generate a new-one

        private User.PendingUploadFileConfig pendingFileConfig;

        public ProjectLocalAddedFile(int projectId,
            database2.table.project.ProjectLocalFile raw)
        {
            ProjectId = projectId;
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

        public string Name { get => raw.Name; set => UpdateName(value); }

        public string LocalDiskPath { get => raw.Path; set => UpdatePath(value); }

        public long FileSize { get => raw.Size; }

        public EnumNxlFileStatus Status
        {
            get => (EnumNxlFileStatus)raw.Operation_status; set => ChangeOperationStaus(value);
        }

        public EnumFileRepo FileRepo { get => EnumFileRepo.REPO_PROJECT; }

        public DateTime LastModifiedTime => raw.Last_modified_time;

        //
        // by osmond
        //
        public string DisplayPath => GetThisFileRemotePath();

        public string PathId => "";

        public string SharedEmails => ""; // MyProject does not support share curretly

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

        public void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            var app = SkydrmApp.Singleton;
            try
            {
                if (OverWriteUpload)
                {
                    isOverWrite = true;
                }

                // tell ServiceMgr 
                //NotifyIRecentTouchedFile(EnumNxlFileStatus.Uploading);
                
                // call api 
                var folder = app.DBFunctionProvider.QueryProjectLocalFileRMSParentFolder(ProjectId, raw.ProjectFileTablePk);
                if (string.IsNullOrEmpty(folder))
                {
                    throw new Exception("Ileagal parent folder path.");
                }

                app.Log.InfoFormat("###Call Upload Project File api, projectId:{0}, rmsParentFolder:{1}, nxlFilePath:{2}", ProjectId, folder.ToLower(), LocalDiskPath);

                app.Rmsdk.User.UploadProjectFile(ProjectId, folder.ToLower(), LocalDiskPath, isOverWrite);
                // delete in db
                app.DBFunctionProvider.DeleteProjectLocalFile(raw.ProjectTablePk, raw.Id);

                // tell ServiceMgr -- Do this after Auto Remove (So invoking this in high level).
                //app.MessageNotify.NotifyMsg(raw.Name, "Upload successfully", EnumMsgNotifyType.LogMsg,
                //    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);

                // Every done, begin impl leave a copy featue
                if (app.User.LeaveCopy)
                {
                    app.User.LeaveCopy_Feature.AddFile(LocalDiskPath);

                    FileHelper.Delete_NoThrow(LocalDiskPath);
                }
            }
            catch (RmRestApiException ex)
            {
                // Handle project upload file 4001(file exist) exception
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

        public void RemoveFromLocal()
        {
            var app = SkydrmApp.Singleton;
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
                var leaveACopy = SkydrmApp.Singleton.User.LeaveCopy_Feature;
                if (leaveACopy.Exist(Name, "", LocalDiskPath))
                {
                    leaveACopy.DeleteFile(LocalDiskPath);
                }
            }
        }

        #region Private methods
        private string GetThisFileRemotePath()
        {
            var app = SkydrmApp.Singleton;
            var folder = app.DBFunctionProvider.QueryProjectLocalFileRMSParentFolder(ProjectId, raw.ProjectFileTablePk);
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + raw.Name;
        }

        private void ChangeOperationStaus(EnumNxlFileStatus status)
        {
            if (raw.Operation_status == (int)status)
            {
                return;
            }
            // change db
            var app = SkydrmApp.Singleton;
            app.DBFunctionProvider.UpdateProjectLocalFileOperationStatus(raw.ProjectTablePk,
                raw.Id, (int)status);
            // update cache
            raw.Operation_status = (int)status;
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
            SkydrmApp.Singleton.DBFunctionProvider.UpdateProjectLocalFileName(raw.ProjectTablePk, raw.Id, name);
            raw.Name = name;
        }

        private void UpdatePath(string path)
        {
            //Sanity check
            //If no changes just return.
            if (raw.Path.Equals(path))
            {
                return;
            }
            //update path in db.
            SkydrmApp.Singleton.DBFunctionProvider.UpdateProjectLocalFileLocalPath(raw.ProjectTablePk, raw.Id, path);
            raw.Path = path;
        }

        private void UpdateFileConfig()
        {
            SkydrmApp.Singleton.DBFunctionProvider.UpdateProjectLocalFileReserved1(raw.ProjectTablePk, raw.Id, 
                JsonConvert.SerializeObject(pendingFileConfig));
        }
        #endregion

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

            public override string RmsRemotePath => Outer.DisplayPath;

            public override bool IsCreatedLocal => true;

            public override string[] Emails => GetEmails();

            public override EnumFileRepo FileRepo => Outer.FileRepo;

            public override IFileInfo Update()
            {
                base.Update();
                return this;
            }
           
            // project does not supprot share
            private string[] GetEmails()
            {
                SkydrmApp.Singleton.Log.Info("project localadded file does not supprot share_feature");
                return new string[0];
            }
        }
    }
}
