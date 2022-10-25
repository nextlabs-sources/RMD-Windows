using SkydrmLocal.rmc.database2.table.project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmLocal.rmc.database.memcache.project
{
    class ProjectCacheProxy : IProjectCache
    {
        private ProjectCache mProjectCache;
        private IProjectLocal mProjectLocal;

        private bool isProjectFileDataDirty;

        public ProjectCacheProxy(string dataBaseConnectionString, int user_Primary_Key)
        {
            mProjectCache = new ProjectCache(user_Primary_Key);
            mProjectLocal = new ProjectLocal(dataBaseConnectionString, user_Primary_Key);
        }

        #region Project cache lifecyle.
        public void OnInitialize()
        {
            //Prepare cache container & lock first
            mProjectCache.OnInitialize();
            OnPreload();
            
            //Pre-load project file table into cache.

            //Pre-load project local file table into cache.

        }

        public void OnPreload()
        {
            //Pre-load project table into cache.
            ProjectLocalLoader.GetInstance().Load(new Config(ListLocalProject, null, (data) =>
            {
                //If data exists in local then inject into cache.
                List<Project> localProjects = (List<Project>)data;
                bool cached = mProjectCache.PaddingProjectLists(localProjects);
                if (cached)
                {
                    Console.WriteLine("Pre-load project lists with size {0} finished", localProjects.Count);
                    var projects = mProjectCache.ListProject();
                    if (projects != null && projects.Count != 0)
                    {
                        foreach (var p in projects)
                        {
                            //Try get all project files belong to current login usr.
                            Console.WriteLine("Try pre-load file & local file with project primary key {0} started", p.Id);
                            ProjectLocalLoader.GetInstance().Load(new Config(ListAllProjectLocalFile, p.Id, (files) =>
                            {
                                //If local has data then padding them into caches.
                                KeyValuePair<int, List<ProjectFile>> pair = (KeyValuePair<int, List<ProjectFile>>)files;
                                int ptk = pair.Key;
                                List<ProjectFile> localFiles = pair.Value;
                                bool ready = mProjectCache.PaddingProjectFile(ptk, localFiles);
                                Console.WriteLine("Pre-load project file finished with project table pk {0} with size {1}", ptk, localFiles.Count);
                            }));
                            //Try get all project local files belong to current login usr.
                            ProjectLocalLoader.GetInstance().Load(new Config(ListProjectLocalFilesFromDB, p.Id, (localFiles) =>
                            {
                                var local = (KeyValuePair<int, List<ProjectLocalFile>>)localFiles;
                                //Padding local data into cache.
                                bool ready = mProjectCache.PaddingProjectLocalFile(local.Key, local.Value);
                                Console.WriteLine("Pre-load project local file finished with project table pk {0} with size {1}", local.Key, local.Value.Count);
                            }));
                        }
                    }
                }
            }));
        }

        public void OnDestroy()
        {
            mProjectCache.OnDestroy();
        }
        #endregion

        #region Project table.
        public List<Project> ListProject()
        {
            var cacheRet = mProjectCache.ListProject();
            //If data prepared in cache.
            if (cacheRet != null && cacheRet.Count != 0)
            {
                return cacheRet;
            }
            //Query data from db.
            //ProjectLocalLoader.GetInstance().Load(new Config(ListLocalProject, null, OnListProjectResult));
            //return new List<Project>();
            return mProjectLocal.ListProject();
        }
        public object ListLocalProject(object bundle)
        {
            return mProjectLocal.ListProject();
        }
        public void OnListProjectResult(object data)
        {
            //If data exists in local then inject into cache.
            List<Project> locals = (List<Project>)data;
            bool notify = mProjectCache.PaddingProjectLists(locals);
            if (notify)
            {
                //Notify with caller project cache data changed.
                Console.WriteLine("OnListProjectResult -.- Notify with caller project cache data changed.");
            }
        }

        public bool DeleteProject(int rms_project_id, OnDeleteSuccess callback = null)
        {
            Console.WriteLine("DeleteProject with pid {0}", rms_project_id);
            //Delete data in cache.
            bool ret = mProjectCache.DeleteProject(rms_project_id);
            //If clear success
            if (ret)
            {
                Console.WriteLine("DeleteProject with pid {0} start clearing local", rms_project_id);
                //Clear data in db.
                //ProjectLocalLoader.GetInstance().Load(new Config(DeleteLocalProject, rms_project_id, null));
                if (mProjectLocal.DeleteProject(rms_project_id))
                {
                    callback?.Invoke();
                }
            }
            return ret;
        }

        public object DeleteLocalProject(object bundle)
        {
            int rms_project_id = (int)bundle;
            return mProjectLocal.DeleteProject(rms_project_id);
        }

        public bool UpsertProject(int project_id, string project_name, string project_display_name,
                                 string project_description, bool isOwner, string tenant_id,
                                 OnUpsertProjectResult Callback)
        {
            //Upsert data in cache.
            bool ret = mProjectCache.UpsertProject(project_id, project_name,
                project_display_name, project_description,
                isOwner, tenant_id, null);
            //If Upsert success.
            if (ret)
            {
                //Build config.
                ProjectUpsertConfig config = new ProjectUpsertConfig
                {
                    Project_id = project_id,
                    Project_name = project_name,
                    Project_display_name = project_display_name,
                    Project_description = project_description,
                    IsOwner = isOwner,
                    Tenant_id = tenant_id
                };

                //Upsert data in db.
                int id = (int)UpsertLocalProject(config);
                if (id != -1)
                {
                    mProjectCache.UpdateProjectItem(project_id, id);
                    Console.WriteLine("UpsertProject success send callback with project table primary key {0} --- with key[project id] {1}", id, project_id);
                    Callback?.Invoke(id);
                }

                //ProjectLocalLoader.GetInstance().Load(new Config(UpsertLocalProject, config, (data) =>
                //{
                //    int id = (int)data;
                //    mProjectCache.UpdateProjectItem(project_id, id);
                //    Console.WriteLine("UpsertProject success send callback with project table primary key {0} --- with key[project id] {1}", id, project_id);
                //    Callback?.Invoke(id);
                //}));
            }
            else
            {
                Console.WriteLine("[C]No item changed when invoke Upsert Project.");
            }
            return ret;
        }
        public object UpsertLocalProject(object bundle)
        {
            ProjectUpsertConfig config = bundle as ProjectUpsertConfig;
            return mProjectLocal.UpsertProject(config.Project_id, config.Project_name,
                config.Project_display_name, config.Project_description,
                config.IsOwner, config.Tenant_id);
        }

        public bool UpsertProjectIsEnabledAdhoc(int project_table_pk, bool isEnabled)
        {
            bool ret = mProjectCache.UpsertProjectIsEnabledAdhoc(project_table_pk, isEnabled);
            if(ret)
            {
                ret = mProjectLocal.UpsertProjectIsEnabledAdhoc(project_table_pk, isEnabled);
            }
            return ret;
        }
        #endregion

        #region Project file table.
        public List<ProjectFile> ListAllProjectFile(int project_table_pk)
        {
            if (isProjectFileDataDirty)
            {
                ProcessDirtyData(project_table_pk);
                isProjectFileDataDirty = false;
            }
            var cacheRet = mProjectCache.ListAllProjectFile(project_table_pk);
            if (cacheRet != null && cacheRet.Count != 0)
            {
                Console.WriteLine("[C]List all project file with size {0} under path '/'", cacheRet.Count);
                return cacheRet;
            }
            ProjectLocalLoader.GetInstance().Load(new Config(ListAllProjectLocalFile, project_table_pk, OnListAllProjectLocalFile));
            return new List<ProjectFile>();
        }
        
        private bool ProcessDirtyData(int project_table_pk)
        {
            var pair = (KeyValuePair<int, List<ProjectFile>>)ListAllProjectLocalFile(project_table_pk);
            mProjectCache.PaddingProjectFile(pair.Key, pair.Value);
            return true;
        }

        private object ListAllProjectLocalFile(object bundle)
        {
            int project_table_pk = (int)bundle;
            return new KeyValuePair<int, List<ProjectFile>>(project_table_pk,
                mProjectLocal.ListAllProjectFile(project_table_pk));
        }
        private void OnListAllProjectLocalFile(object data)
        {
            //If local has data then padding them into caches.
            KeyValuePair<int, List<ProjectFile>> pair = (KeyValuePair<int, List<ProjectFile>>)data;
            int ptk = pair.Key;
            List<ProjectFile> locals = pair.Value;
            bool notify = mProjectCache.PaddingProjectFile(ptk, locals);
            if (notify)
            {
                //Notify with caller project file changed.
            }
        }

        public List<ProjectFile> ListProjectFile(int project_table_pk, string path)
        {
            if (isProjectFileDataDirty)
            {
                ProcessDirtyData(project_table_pk);
                isProjectFileDataDirty = false;
            }
            var cacheRet = mProjectCache.ListProjectFile(project_table_pk, path);
            if (cacheRet != null && cacheRet.Count != 0)
            {
                Console.WriteLine("[C]List project file with size {0} under path {1}", cacheRet.Count,path);
                return cacheRet;
            }
            ProjectLocalLoader.GetInstance().Load(new Config(ListAllProjectLocalFile, project_table_pk, OnListAllProjectLocalFile));
            return new List<ProjectFile>();
        }

        public List<ProjectFile> ListProjectOfflineFile(int project_table_pk)
        {
            if (isProjectFileDataDirty)
            {
                ProcessDirtyData(project_table_pk);
                isProjectFileDataDirty = false;
            }
            var cacheRet = mProjectCache.ListProjectOfflineFile(project_table_pk);
            if (cacheRet != null && cacheRet.Count != 0)
            {
                Console.WriteLine("[C]List project offline file with size {0}", cacheRet.Count);
                return cacheRet;
            }
            ProjectLocalLoader.GetInstance().Load(new Config(ListAllProjectLocalFile, project_table_pk, OnListAllProjectLocalFile));
            return new List<ProjectFile>();
        }

        public bool DeleteProjectFolderAndAllSubFiles(int project_table_pk, string rms_path_id)
        {
            bool ret = mProjectCache.DeleteProjectFolderAndAllSubFiles(project_table_pk, rms_path_id);
            if (ret)
            {
                KeyValuePair<int, string> bundle = new KeyValuePair<int, string>(project_table_pk, rms_path_id);
                ProjectLocalLoader.GetInstance().Load(new Config(DeleteProjectLocalFolderAndAllSubFiles, bundle, null));
                return true;
            }
            return false;
        }
        public object DeleteProjectLocalFolderAndAllSubFiles(object bundle)
        {
            KeyValuePair<int, string> pair = (KeyValuePair<int, string>)bundle;
            int ptk = pair.Key;
            string rms_path_id = pair.Value;
            return mProjectLocal.DeleteProjectFolderAndAllSubFiles(ptk, rms_path_id);
        }

        public bool DeleteProjectFile(int project_table_pk, string rms_file_id)
        {
            var ret = mProjectCache.DeleteProjectFile(project_table_pk, rms_file_id);
            if (ret)
            {
                KeyValuePair<int, string> bundle = new KeyValuePair<int, string>(project_table_pk, rms_file_id);
                ProjectLocalLoader.GetInstance().Load(new Config(DeleteLocalProjectFile, bundle, null));
                return true;
            }
            return false;
        }
        private object DeleteLocalProjectFile(object bundle)
        {
            KeyValuePair<int, string> pair = (KeyValuePair<int, string>)bundle;
            int ptk = pair.Key;
            string rms_file_id = pair.Value;
            return mProjectLocal.DeleteProjectFile(ptk, rms_file_id);
        }

        public bool UpsertProjectFileBatch(InstertProjectFile[] files, Dictionary<int, int> Project_Id2PK)
        {
            var ret = mProjectCache.UpsertProjectFileBatch(files, Project_Id2PK);
            if (ret)
            {
                isProjectFileDataDirty = true;
                var bundle = new KeyValuePair<InstertProjectFile[], Dictionary<int, int>>(files, Project_Id2PK);
                //ProjectLocalLoader.GetInstance().Load(new Config(UpsertProjectLocalFileBatch, bundle, null));
                UpsertProjectLocalFileBatch(bundle);
                return true;
            }
            return false;
        }
        private object UpsertProjectLocalFileBatch(object bundle)
        {
            var data = (KeyValuePair<InstertProjectFile[], Dictionary<int, int>>)bundle;
            InstertProjectFile[] files = data.Key;
            Dictionary<int, int> Project_Id2PK = data.Value;
            bool ret = mProjectLocal.UpsertProjectFileBatch(files, Project_Id2PK);
            if (ret)
            {
                var pks = Project_Id2PK.Values;
                List<int> cpyPks = new List<int>(pks);
                foreach (var pk in cpyPks)
                {
                    ProcessDirtyData(pk);
                }
            }
            isProjectFileDataDirty = false;
            return true;
        }

        public bool InsertFakedRoot(int project_table_pk)
        {
            var ret = mProjectCache.InsertFakedRoot(project_table_pk);
            if (ret)
            {
                ProjectLocalLoader.GetInstance().Load(new Config(InsertLocalFakedRoot, project_table_pk, null));
                return true;
            }
            return false;
        }
        private object InsertLocalFakedRoot(object bundle)
        {
            int ptk = (int)bundle;
            return mProjectLocal.InsertFakedRoot(ptk);
        }

        public bool UpdateProjectFileOperationStatus(int project_table_pk, int project_file_table_pk, int newStatus)
        {
            var ret = mProjectCache.UpdateProjectFileOperationStatus(project_table_pk, project_file_table_pk, newStatus);
            if (ret)
            {
                //var bundle = new KeyValuePair<int, int>(project_file_table_pk, newStatus);
                //ProjectLocalLoader.GetInstance().Load(new Config(UpdateProjectLocalFileOperationStatus, bundle, null));
                //return true;
                return mProjectLocal.UpdateProjectFileOperationStatus(project_file_table_pk, newStatus);
            }
            return false;
        }
        private object UpdateProjectLocalFileOperationStatus(object bundle)
        {
            var pair = (KeyValuePair<int, int>)bundle;
            int pftk = pair.Key;
            int newStatus = pair.Value;
            return mProjectLocal.UpdateProjectFileOperationStatus(pftk, newStatus);
        }

        public bool UpdateProjectFileOfflineMark(int project_table_pk, int project_file_table_pk, bool newMark)
        {
            var ret = mProjectCache.UpdateProjectFileOfflineMark(project_table_pk, project_file_table_pk, newMark);
            if (ret)
            {
                //var bundle = new KeyValuePair<int, bool>(project_file_table_pk, newMark);
                //ProjectLocalLoader.GetInstance().Load(new Config(UpdateProjectLocalFileOfflineMark, bundle, null));
                return mProjectLocal.UpdateProjectFileOfflineMark(project_file_table_pk, newMark);
            }
            return false;
        }
        private object UpdateProjectLocalFileOfflineMark(object bundle)
        {
            var pair = (KeyValuePair<int, bool>)bundle;
            int pftk = pair.Key;
            bool newMark = pair.Value;
            return mProjectLocal.UpdateProjectFileOfflineMark(pftk, newMark);
        }

        public bool UpdateProjectFileLocalpath(int project_table_pk, int project_file_table_pk, string newPath)
        {
            var ret = mProjectCache.UpdateProjectFileLocalpath(project_table_pk, project_file_table_pk, newPath);
            if (ret)
            {
                //var bundle = new KeyValuePair<int, string>(project_file_table_pk, newPath);
                //ProjectLocalLoader.GetInstance().Load(new Config(UpdateProjectLocalFileLocalPath, bundle, null));
                return mProjectLocal.UpdateProjectFileLocalpath(project_file_table_pk, newPath);
            }
            return false;
        }
        private object UpdateProjectLocalFileLocalPath(object bundle)
        {
            var pair = (KeyValuePair<int, string>)bundle;
            int pftk = pair.Key;
            string newPath = pair.Value;
            return mProjectLocal.UpdateProjectFileLocalpath(pftk, newPath);
        }

        public bool UpdateProjectFileLastModifiedTime(int project_table_pk, int project_file_table_pk, DateTime lastModifiedTime)
        {
            var ret = mProjectCache.UpdateProjectFileLastModifiedTime(project_table_pk, project_file_table_pk, lastModifiedTime);
            if (ret)
            {
                return mProjectLocal.UpdateProjectFileLastModifiedTime(project_file_table_pk, lastModifiedTime);
            }
            return ret;
        }

        public bool UpdateProjectFileFileSize(int project_table_pk, int project_file_table_pk, long filesize)
        {
            var ret = mProjectCache.UpdateProjectFileFileSize(project_table_pk, project_file_table_pk, filesize);
            if(ret)
            {
                return mProjectLocal.UpdateProjectFileFileSize(project_file_table_pk, filesize);
            }
            return ret;
        }

        public bool UpdateProjectFileEditStatus(int project_table_pk, int project_file_table_pk, int newStatus)
        {
            var ret = mProjectCache.UpdateProjectFileEditStatus(project_table_pk, project_file_table_pk, newStatus);
            if (ret)
            {
                return mProjectLocal.UpdateProjectFileEditStatus(project_file_table_pk, newStatus);
            }
            return false;
        }

        public bool UpdateProjectFileModifyRightsStatus(int project_table_pk, int project_file_table_pk, int newStatus)
        {
            var ret = mProjectCache.UpdateProjectFileModifyRightsStatus(project_table_pk, project_file_table_pk, newStatus);
            if (ret)
            {
                return mProjectLocal.UpdateProjectFileModifyRightsStatus(project_file_table_pk, newStatus);
            }
            return false;
        }

        public string QueryProjectLocalFileRMSParentFolder(int project_table_pk, int projectFile_RowNumber)
        {
            string rms_display_path= mProjectCache.QueryProjectLocalFileRMSParentFolder(project_table_pk, projectFile_RowNumber);
            if (string.IsNullOrEmpty(rms_display_path))
            {
                return mProjectLocal.QueryProjectLocalFileRMSParentFolder(project_table_pk, projectFile_RowNumber);
            }
            return rms_display_path;
        }

        public int QueryProjectFileId(int project_table_pk, string rms_path_id)
        {
            int id = mProjectCache.QueryProjectFileId(project_table_pk, rms_path_id);
            if (id == -1)
            {
                return mProjectLocal.QueryProjectFileId(project_table_pk, rms_path_id);
            }
            return id;
        }
        #endregion

        #region Project local file table.
        public List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk)
        {
            var cacheRet = mProjectCache.ListProjectLocalFiles(project_table_pk);
            if (cacheRet != null && cacheRet.Count != 0)
            {
                Console.WriteLine("[C] list project local file with size {0}", cacheRet.Count);
                return cacheRet;
            }

            //ProjectLocalLoader.GetInstance().Load(new Config(ListProjectLocalFilesFromDB, project_table_pk, OnListProjectLocalFileResult));

            var localRet = mProjectLocal.ListProjectLocalFiles(project_table_pk);
            bool ready = mProjectCache.PaddingProjectLocalFile(project_table_pk, localRet);
            if (ready)
            {
                return localRet;
            }
            return new List<ProjectLocalFile>();
        }
        private object ListProjectLocalFilesFromDB(object bundle)
        {
            int project_table_pk = (int)bundle;
            return new KeyValuePair<int, List<ProjectLocalFile>>(project_table_pk, 
                mProjectLocal.ListProjectLocalFiles(project_table_pk));
        }
        private void OnListProjectLocalFileResult(object data)
        {
            var local = (KeyValuePair<int, List<ProjectLocalFile>>)data;
            //Padding local data into cache.
            bool notify = mProjectCache.PaddingProjectLocalFile(local.Key, local.Value);
            //Notify user local file cache changed if necessary.
            if(notify)
            {
                //Do notify project lcoal file data changed work.
            }
        }

        public List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk, string FolderId)
        {
            var cacheRet = mProjectCache.ListProjectLocalFiles(project_table_pk, FolderId);
            if (cacheRet != null && cacheRet.Count != 0)
            {
                Console.WriteLine("[C] list project local file with size {0} under path {1}", cacheRet.Count, FolderId);
                SkydrmLocalApp.Singleton.Log.Info(string.Format(@"list project local file with size {0} under path {1}", cacheRet.Count, FolderId));
                return cacheRet;
            }
            
            var localRet = mProjectLocal.ListProjectLocalFiles(project_table_pk);
            bool ready = mProjectCache.PaddingProjectLocalFile(project_table_pk, localRet);
            if (ready)
            {
                return mProjectCache.ListProjectLocalFiles(project_table_pk, FolderId);
            }
            return new List<ProjectLocalFile>();
        }

        public bool DeleteProjectLocalFile(int project_table_pk, int project_local_file_table_pk)
        {
            bool ret = mProjectCache.DeleteProjectLocalFile(project_table_pk, project_local_file_table_pk);
            if (ret)
            {
                SkydrmLocalApp.Singleton.Log.Info("Delete Project Local File Table DB, Id:" + project_local_file_table_pk);
                ProjectLocalLoader.GetInstance().Load(new Config(DeleteProjectLocalFileInDB, project_local_file_table_pk, null));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Delete specified project all local files in mem cache.
        /// When user remove a project which contains some local files(waiting upload files), we also need to delete them from mem cache,
        /// or else, the files can still display in 'Outbox' even though user do some refresh.
        /// </summary>
        public bool DeleteProjectAllLocalFiles(int project_table_pk)
        {
            return mProjectCache.DeleteProjectAllLocalFiles(project_table_pk);
        }

        /// <summary>
        /// Delete specified project folder all local files in mem cache
        /// </summary>
        public bool DeleteProjectFolderLocalFiles(int project_table_pk, int projectFile_RowNumber)
        {
            return mProjectCache.DeleteProjectFolderLocalFiles(project_table_pk, projectFile_RowNumber);
        }

        private object DeleteProjectLocalFileInDB(object bundle)
        {
            int project_local_file_table_pk = (int)bundle;
            return mProjectLocal.DeleteProjectLocalFile(project_local_file_table_pk);
        }

        public bool AddLocalFileToProject(int project_table_pk, string FolderId, string name, string path, int size, DateTime lastModified)
        {
            //Here forbidden call method mProjectCache.AddLocalFileToProject()
            //Cause we can't make a faked local file item id which we will use it delete file.
            //mProjectCache.AddLocalFileToProject(project_table_pk, FolderId, name, path, size, lastModified);
            //ProjectLocalFileConfig bundle = new ProjectLocalFileConfig()
            //{
            //    Project_table_pk = project_table_pk,
            //    FolderId = FolderId,
            //    Name = name,
            //    Path = path,
            //    Size = size,
            //    LastModified = lastModified
            //};
            //ProjectLocalLoader.GetInstance().Load(new Config(AddLocalFileIntoDB, bundle, OnAddLocalFileResult));

            int id = mProjectLocal.AddLocalFileToProject(project_table_pk, FolderId, name, path, size, lastModified);
            if (id != -1)
            {
                Console.WriteLine("AddLocalFileToProject with id {0}", id);
                SkydrmLocalApp.Singleton.Log.Info("Project Cache Proxy Add LocalFile To Project with id "+id);
                return mProjectCache.AddLocalFileToProject(id, project_table_pk, FolderId, name, path, size, lastModified);
            }
            else
            {
                //ProjectLocalLoader.GetInstance().Load(new Config(ListProjectLocalFilesFromDB, project_table_pk, OnListProjectLocalFileResult));
                SkydrmLocalApp.Singleton.Log.Info("Project Cache Proxy Add LocalFile To Project failed, cache padding project local file");
                var localRet = mProjectLocal.ListProjectLocalFiles(project_table_pk);
                return mProjectCache.PaddingProjectLocalFile(project_table_pk, localRet);
            }
        }
        private object AddLocalFileIntoDB(object bundle)
        {
            ProjectLocalFileConfig config = bundle as ProjectLocalFileConfig;
            int ppk = config.Project_table_pk;
            string folderId = config.FolderId;
            string name = config.Name;
            string path = config.Path;
            int size = config.Size;
            DateTime lastModified = config.LastModified;
            bool added = mProjectLocal.AddLocalFileToProject(ppk, folderId, name, path, size, lastModified) != -1;
            return new KeyValuePair<int, List<ProjectLocalFile>>(ppk, mProjectLocal.ListProjectLocalFiles(ppk));
        }
        private void OnAddLocalFileResult(object data)
        {
            var local = (KeyValuePair<int, List<ProjectLocalFile>>)data;
            //Padding local data into cache.
            bool notify = mProjectCache.PaddingProjectLocalFile(local.Key, local.Value);
            //Notify user local file cache changed if necessary.
            if (notify)
            {
                //Do notify project lcoal file data changed work.
            }
        }

        public bool UpdateProjectLocalFileOperationStatus(int project_table_pk, int project_local_file_table_pk, int newStatus)
        {
            var ret = mProjectCache.UpdateProjectLocalFileOperationStatus(project_table_pk, project_local_file_table_pk, newStatus);
            if (ret)
            {
                var bundle = new KeyValuePair<int, int>(project_local_file_table_pk, newStatus);
                ProjectLocalLoader.GetInstance().Load(new Config(UpdateProjectLocalFileOperationStatusInDB, bundle, null));
                return true;
            }
            return false;
        }
        private object UpdateProjectLocalFileOperationStatusInDB(object bundle)
        {
            var pair = (KeyValuePair<int, int>)bundle;
            int plfk = pair.Key;
            int newStatus = pair.Value;
            return mProjectLocal.UpdateProjectLocalFileOperationStatus(plfk, newStatus);
        }
        #endregion

        #region Project classification table.
        public string GetProjectClassification(int project_table_pk)
        {
            return mProjectCache.GetProjectClassification(project_table_pk);
        }

        public bool UpdateProjectClassification(int project_table_pk, string classificationJson)
        {
            var ret = mProjectCache.UpdateProjectClassification(project_table_pk, classificationJson);
            if (ret)
            {
                var bundle = new KeyValuePair<int, string>(project_table_pk, classificationJson);
                ProjectLocalLoader.GetInstance().Load(new Config(UpdateProjectDBClassification, bundle, null));
                return true;
            }
            return false;
        }

        private object UpdateProjectDBClassification(object bundle)
        {
            var pair = (KeyValuePair<int, string>)bundle;
            int ptpk = pair.Key;
            string classificationJson = pair.Value;
            return mProjectLocal.UpdateProjectClassification(ptpk, classificationJson);
        }
        #endregion
    }

    #region Project upsert config
    class ProjectUpsertConfig
    {
        private int project_id;
        private string project_name;
        private string project_display_name;
        private string project_description;
        private bool isOwner;
        private string tenant_id;

        public int Project_id { get => project_id; set => project_id = value; }
        public string Project_name { get => project_name; set => project_name = value; }
        public string Project_display_name { get => project_display_name; set => project_display_name = value; }
        public string Project_description { get => project_description; set => project_description = value; }
        public bool IsOwner { get => isOwner; set => isOwner = value; }
        public string Tenant_id { get => tenant_id; set => tenant_id = value; }
    }
    #endregion

    #region Project local file insert config
    class ProjectLocalFileConfig
    {
        private int project_table_pk;
        private string folderId;
        private string name;
        private string path;
        private int size;
        private DateTime lastModified;

        public int Project_table_pk { get => project_table_pk; set => project_table_pk = value; }
        public string FolderId { get => folderId; set => folderId = value; }
        public string Name { get => name; set => name = value; }
        public string Path { get => path; set => path = value; }
        public int Size { get => size; set => size = value; }
        public DateTime LastModified { get => lastModified; set => lastModified = value; }
    }
    #endregion
}
