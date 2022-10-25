using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Skydrmlocal.rmc.database2;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database2.table.project;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmLocal.rmc.database.memcache.project
{
    class ProjectCache : IProjectCache
    {
        #region Project table cache field.
        //Project table container.
        private static Dictionary<int, List<Project>> mProjectTableCaches = new Dictionary<int, List<Project>>();
        //mProjectTableCaches's lock.
        private static ReaderWriterLockSlim mProjectTableLock = new ReaderWriterLockSlim();
        #endregion
        #region Project file table cache field.
        //Project file table container.
        private static Dictionary<int, List<ProjectFile>> mProjectFileTableCaches = new Dictionary<int, List<ProjectFile>>();
        //mProjectFileTableCaches's lock.
        private static ReaderWriterLockSlim mProjectFileTableLock = new ReaderWriterLockSlim();
        #endregion
        #region Project local file table cache field.
        //Project local file table container.
        private static Dictionary<int, List<ProjectLocalFile>> mProjectLocalFileTableCaches = new Dictionary<int, List<ProjectLocalFile>>();
        //mProjectLocalFileTableCaches's lock.
        private static ReaderWriterLockSlim mProjectLocalFileTableLock = new ReaderWriterLockSlim();
        #endregion

        private int User_Primary_key = -1;

        public ProjectCache(int upk)
        {
            User_Primary_key = upk;
        }

        #region Project cache lifecyle.
        public void OnInitialize()
        {
            if (mProjectTableCaches == null)
            {
                mProjectTableCaches = new Dictionary<int, List<Project>>();
                mProjectTableLock = new ReaderWriterLockSlim();
            }
            if (mProjectFileTableCaches == null)
            {
                mProjectFileTableCaches = new Dictionary<int, List<ProjectFile>>();
                mProjectFileTableLock = new ReaderWriterLockSlim();
            }
            if (mProjectLocalFileTableCaches == null)
            {
                mProjectLocalFileTableCaches = new Dictionary<int, List<ProjectLocalFile>>();
                mProjectLocalFileTableLock = new ReaderWriterLockSlim();
            }
        }

        public void OnDestroy()
        {
            //Clear project table caches.
            if (mProjectTableCaches != null)
            {
                mProjectTableCaches.Clear();
                //mProjectTableCaches = null;
                //mProjectTableLock = null;
            }
            //Clear project file table caches.
            if (mProjectFileTableCaches != null)
            {
                mProjectFileTableCaches.Clear();
                //mProjectFileTableCaches = null;
                //mProjectFileTableLock = null;
            }
            //Clear project local file table caches.
            if (mProjectLocalFileTableCaches != null)
            {
                mProjectLocalFileTableCaches.Clear();
                //mProjectLocalFileTableCaches = null;
                //mProjectLocalFileTableLock = null;
            }
            User_Primary_key = -1;
        }
        #endregion

        #region Project table.
        /// <summary>
        /// Get all projects belong to current login user.
        /// </summary>
        /// <param name="upk"></param>
        /// <returns></returns>
        public List<Project> ListProject()
        {
            try
            {
                mProjectTableLock.EnterReadLock();

                int key = User_Primary_key;
                if (!mProjectTableCaches.ContainsKey(key))
                {
                    return null;
                }
                return mProjectTableCaches[key];
            }
            finally
            {
                mProjectTableLock.ExitReadLock();
            }
        }

        public bool PaddingProjectLists(List<Project> data)
        {
            try
            {
                mProjectTableLock.EnterWriteLock();

                if (data == null || data.Count == 0)
                {
                    return false;
                }
                int key = User_Primary_key;
                //If data source already exists in cache.
                if (!mProjectTableCaches.ContainsKey(key))
                {
                    //Insert new item into cache.
                    mProjectTableCaches.Add(key, data);
                    return true;
                }
                //Refresh data source.
                mProjectTableCaches[key] = data;
                return true;
            }
            finally
            {
                mProjectTableLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Delete a specific project belong to current login user.
        /// </summary>
        /// <param name="upk"></param>
        /// <param name="rms_project_id">which project being tackled.</param>
        /// <returns>true means delete success else failed.</returns>
        public bool DeleteProject(int rms_project_id, OnDeleteSuccess callback = null)
        {
            try
            {
                mProjectTableLock.EnterWriteLock();

                int key = User_Primary_key;
                if (!mProjectTableCaches.ContainsKey(key))
                {
                    return false;
                }
                int index = -1;
                var projects = mProjectTableCaches[key];
                if (projects != null && projects.Count != 0)
                {
                    for (int i = 0; i < projects.Count; i++)
                    {
                        if (projects[i].Rms_project_id == rms_project_id)
                        {
                            index = i;
                        }
                    }
                }
                //If target project not found in the List
                if (index == -1)
                {
                    return false;
                }
                projects.RemoveAt(index);
                return true;
            }
            finally
            {
                mProjectTableLock.ExitWriteLock();
            }
        }

        public bool UpsertProject(int project_id, string project_name,
            string project_display_name, string project_description,
            bool isOwner, string tenant_id, OnUpsertProjectResult Callback)
        {
            try
            {
                mProjectTableLock.EnterWriteLock();

                int key = User_Primary_key;
                if (mProjectTableCaches.ContainsKey(key))
                {
                    var projects = mProjectTableCaches[key];
                    //If there are any projects created by user.
                    if (projects != null && projects.Count != 0)
                    {
                        //1.if there is a item aleady exists then update.
                        bool found = false;
                        foreach (var item in projects)
                        {
                            if (item.Rms_project_id == project_id)
                            {
                                //Check whether need to update or not.
                                //Use strict comparable mode.
                                if (item.User_table_pk == key &&
                                    item.Rms_project_id == project_id &&
                                    item.Rms_name == project_name &&
                                    item.Rms_display_name == project_display_name &&
                                    item.Rms_description == project_description &&
                                    item.Rms_is_owner == isOwner &&
                                    item.Rms_tenant_id == tenant_id)
                                {
                                    return false;
                                }
                                //Update item.
                                item.User_table_pk = key;
                                item.Rms_project_id = project_id;
                                item.Rms_name = project_name;
                                item.Rms_display_name = project_display_name;
                                item.Rms_description = project_description;
                                item.Rms_is_owner = isOwner;
                                item.Rms_tenant_id = tenant_id;
                                found = true;
                            }
                        }
                        //2.insert a new item into list.
                        if (!found)
                        {
                            Project item = Project.ConstructItem(key, project_id, project_name,
                                  project_display_name, project_description,
                                  isOwner, tenant_id);
                            projects.Add(item);
                        }
                    }
                    else //No project exists in local.
                    {
                        List<Project> items = new List<Project>()
                        {
                            Project.ConstructItem(key, project_id, project_name,
                            project_display_name, project_description,
                            isOwner, tenant_id)
                        };
                        mProjectTableCaches[key] = items;
                    }
                }
                else //No key found in mProjectTableCaches
                {
                    //Construct a new container belong to current usr
                    //And set it as the key's value.
                    List<Project> items = new List<Project>()
                    {
                        Project.ConstructItem(key, project_id, project_name,
                        project_display_name, project_description,
                        isOwner, tenant_id)
                    };
                    
                    mProjectTableCaches.Add(key, items);
                }
                return true;
            }
            finally
            {
                mProjectTableLock.ExitWriteLock();
            }
        }

        public bool UpsertProjectIsEnabledAdhoc(int project_table_pk, bool isEnabled)
        {
            try
            {
                mProjectTableLock.EnterWriteLock();

                int key = User_Primary_key;
                if (!mProjectTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var p in data)
                    {
                        if (p.Id == project_table_pk)
                        {
                            p.Rms_is_enable_adhoc = isEnabled;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectTableLock.ExitWriteLock();
            }
        }

        public bool UpdateProjectItem(int project_id, int id)
        {
            try
            {
                if (id == -1)
                {
                    return false;
                }
                mProjectTableLock.EnterWriteLock();

                //Means already item exits in cache.
                int key = User_Primary_key;
                if (!mProjectTableCaches.ContainsKey(key))
                {
                    return false;
                }
                //Get items belong to current user.
                var projects = mProjectTableCaches[key];
                if (projects != null && projects.Count != 0)
                {
                    foreach (var item in projects)
                    {
                        //Find target by project_id[project identifier]
                        if (item.Rms_project_id == project_id)
                        {
                            item.Id = id;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectTableLock.ExitWriteLock();
            }
        }
        #endregion

        #region Project file table.
        public List<ProjectFile> ListAllProjectFile(int project_table_pk)
        {
            try
            {
                mProjectFileTableLock.EnterReadLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return null;
                }
                return mProjectFileTableCaches[key];
            }
            finally
            {
                mProjectFileTableLock.ExitReadLock();
            }
        }

        public bool PaddingProjectFile(int project_table_pk, List<ProjectFile> data)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                if (data == null || data.Count == 0)
                {
                    return false;
                }
                int key = project_table_pk;
                if (mProjectFileTableCaches.ContainsKey(key))
                {
                    mProjectFileTableCaches[key] = data;
                    return true;
                }
                //Insert new item into cache.
                mProjectFileTableCaches.Add(key, data);
                return true;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public List<ProjectFile> ListProjectFile(int project_table_pk, string path)
        {
            try
            {
                mProjectFileTableLock.EnterReadLock();

                if (path == null || path.Length == 0)
                {
                    path = "/";
                }
                if (path.Length > 1 && !path.EndsWith("/"))
                {
                    path += '/';
                }

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return null;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    var cachedRt = from f in data
                                   where Utils.IsDirectChild(f.Rms_display_path, path) == true
                                   select f;
                    return cachedRt.ToList();
                }
                return null;
            }
            finally
            {
                mProjectFileTableLock.ExitReadLock();
            }
        }

        public List<ProjectFile> ListProjectOfflineFile(int project_table_pk)
        {
            try
            {
                mProjectFileTableLock.EnterReadLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return null;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    var cachedRt = from f in data
                                   where !f.Rms_is_folder && f.Is_offline == true
                                   select f;
                    return cachedRt.ToList();
                }
                return null;
            }
            finally
            {
                mProjectFileTableLock.ExitReadLock();
            }
        }

        public bool DeleteProjectFolderAndAllSubFiles(int project_table_pk, string rms_path_id)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    return data.RemoveAll(x => x.Rms_path_id.StartsWith(rms_path_id, StringComparison.CurrentCultureIgnoreCase)) != 0;
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool DeleteProjectFile(int project_table_pk, string rms_file_id)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    return data.RemoveAll(x => string.Compare(x.Rms_file_id, rms_file_id) == 0) != 0;
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool UpsertProjectFileBatch(InstertProjectFile[] files, Dictionary<int, int> Project_Id2PK)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                var data = ConvertFromInternal(files, Project_Id2PK);
                if (data == null || data.Count == 0)
                {
                    return false;
                }
                foreach (var d in data)
                {
                    int key = d.ProjectTablePk;
                    var value = mProjectFileTableCaches[key];
                    if (value != null && value.Count != 0)
                    {
                        value.Add(d);
                    }
                    else
                    {
                        List<ProjectFile> newvalue = new List<ProjectFile>
                        {
                            d
                        };
                        mProjectFileTableCaches.Add(key, newvalue);
                    }
                }
                return true;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        private List<ProjectFile> ConvertFromInternal(InstertProjectFile[] inserts, Dictionary<int, int> Project_Id2PK)
        {
            List<ProjectFile> cacheRet = new List<ProjectFile>();
            if (inserts != null && inserts.Length != 0)
            {
                foreach (var i in inserts)
                {
                    ProjectFile f = new ProjectFile
                    {
                        ProjectTablePk = Project_Id2PK[i.project_id],
                        Rms_file_id = i.file_id,
                        Rms_duid=i.file_duid,
                        Rms_display_path = i.file_display_path,
                        Rms_path_id = i.file_path_id,
                        Rms_name = i.file_nxl_name,
                        Rms_lastModifiedTime = new DateTime(JavaTimeConverter.ToCSLongTicks(i.file_lastModifiedTime)),
                        Rms_creationTime = new DateTime(JavaTimeConverter.ToCSLongTicks(i.file_creationTime)),
                        Rms_fileSize = i.file_size,
                        Rms_OwnerId = i.file_rms_ownerId,
                        Rms_OwnerDisplayName = i.file_ownerDisplayName,
                        Rms_OwnerEmail = i.file_ownerEmail,
                        Rms_is_folder = i.file_display_path.EndsWith("/"),
                        //default value.
                        Is_offline = false,
                        Local_path = "",
                        Operation_status = 4
                    };
                    cacheRet.Add(f);
                }
            }
            return cacheRet;
        }

        public bool InsertFakedRoot(int project_table_pk)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();


                int key = project_table_pk;
                ProjectFile fakeRoot = new ProjectFile
                {
                    ProjectTablePk = key,
                    Rms_file_id = "00000000-0000-0000-0000-000000000000",
                    Rms_display_path = "/",
                    Rms_path_id = "/"
                };

                if (mProjectFileTableCaches.ContainsKey(key))
                {
                    var data = mProjectFileTableCaches[key];
                    if (data != null && data.Count != 0)
                    {
                        if (data.Contains(fakeRoot))
                        {
                            return false;
                        }
                        data.Add(fakeRoot);
                    }
                    else
                    {
                        List<ProjectFile> value = new List<ProjectFile>()
                        {
                            fakeRoot
                        };
                        mProjectFileTableCaches.Add(key, value);
                    }
                }
                else
                {
                    List<ProjectFile> value = new List<ProjectFile>()
                    {
                        fakeRoot
                    };
                    mProjectFileTableCaches.Add(key, value);
                }
                return true;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool UpdateProjectFileOperationStatus(int project_table_pk, int project_file_table_pk, int newStatus)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == project_file_table_pk)
                        {
                            f.Operation_status = newStatus;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool UpdateProjectFileOfflineMark(int project_table_pk, int project_file_table_pk, bool newMark)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == project_file_table_pk)
                        {
                            f.Is_offline = newMark;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool UpdateProjectFileLocalpath(int project_table_pk, int project_file_table_pk, string newPath)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == project_file_table_pk)
                        {
                            f.Local_path = newPath;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool UpdateProjectFileLastModifiedTime(int project_table_pk, int project_file_table_pk, DateTime lastModifiedTime)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == project_file_table_pk)
                        {
                            f.Rms_lastModifiedTime = lastModifiedTime;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool UpdateProjectFileFileSize(int project_table_pk, int project_file_table_pk, long filesize)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == project_file_table_pk)
                        {
                            f.Rms_fileSize = filesize;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool UpdateProjectFileEditStatus(int project_table_pk, int project_file_table_pk, int newStatus)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == project_file_table_pk)
                        {
                            f.Edit_Status = newStatus;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public bool UpdateProjectFileModifyRightsStatus(int project_table_pk, int project_file_table_pk, int newStatus)
        {
            try
            {
                mProjectFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == project_file_table_pk)
                        {
                            f.Modify_Rights_Status = newStatus;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectFileTableLock.ExitWriteLock();
            }
        }

        public string QueryProjectLocalFileRMSParentFolder(int project_table_pk, int projectFile_RowNumber)
        {
            try
            {
                mProjectFileTableLock.EnterReadLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return null;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == projectFile_RowNumber)
                        {
                            return f.Rms_display_path;
                        }
                    }
                }
                return null;
            }
            finally
            {
                mProjectFileTableLock.ExitReadLock();
            }
        }

        public int QueryProjectFileId(int project_table_pk, string rms_path_id)
        {
            try
            {
                mProjectFileTableLock.EnterReadLock();

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return -1;
                }
                var data = mProjectFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Rms_path_id == rms_path_id)
                        {
                            return f.Id;
                        }
                    }
                }
                return -1;
            }
            finally
            {
                mProjectFileTableLock.ExitReadLock();
            }
        }
        #endregion

        #region Project local file table.
        /// <summary>
        /// Get all project local files belong to a project.
        /// </summary>
        /// <param name="projectId">project identifier</param>
        /// <returns></returns>
        public List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk)
        {
            try
            {
                mProjectLocalFileTableLock.EnterReadLock();

                int key = project_table_pk;
                if (!mProjectLocalFileTableCaches.ContainsKey(key))
                {
                    return null;
                }
                return mProjectLocalFileTableCaches[key];
            }
            finally
            {
                mProjectLocalFileTableLock.ExitReadLock();
            }
        }
        public bool PaddingProjectLocalFile(int project_table_pk, List<ProjectLocalFile> data)
        {
            try
            {
                //Enter write lock first
                mProjectLocalFileTableLock.EnterWriteLock();

                //Will check data list from db first.
                //If there is no data just ignore & won't feed by to caller.
                if (data == null || data.Count == 0)
                {
                    return false;
                }
                int key = project_table_pk;
                //If key already exists in data container.
                if (mProjectLocalFileTableCaches.ContainsKey(key))
                {
                    //Replace it with new one then return & notify with caller data changed.
                    mProjectLocalFileTableCaches[key] = data;
                    return true;
                }
                //If key not exists use it as newone, insert data into cache & notify with caller data changed.
                mProjectLocalFileTableCaches.Add(key, data);
                return true;
            }
            finally
            {
                //Exit write lock after all write operation done or error occurred. 
                mProjectLocalFileTableLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get all local files belong to a project folder.
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="FolderId"></param>
        /// <returns></returns>
        public List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk, string FolderId)
        {
            try
            {
                mProjectLocalFileTableLock.EnterReadLock();

                if (!mProjectFileTableCaches.ContainsKey(project_table_pk))
                {
                    return null;
                }
                var pFileCaches = mProjectFileTableCaches[project_table_pk];
                List<ProjectFile> folders = new List<ProjectFile>();
                if (pFileCaches != null && pFileCaches.Count != 0)
                {
                    foreach (var f in pFileCaches)
                    {
                        if (f.Rms_display_path.Equals(FolderId, StringComparison.OrdinalIgnoreCase))
                        {
                            folders.Add(f);
                        }
                    }
                }
                if (folders.Count == 0)
                {
                    return null;
                }
                if (!mProjectLocalFileTableCaches.ContainsKey(project_table_pk))
                {
                    return null;
                }
                var data = mProjectLocalFileTableCaches[project_table_pk];
                List<ProjectLocalFile> cacheRet = new List<ProjectLocalFile>();
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        foreach (var ff in folders)
                        {
                            if (f.ProjectFileTablePk == ff.Id)
                            {
                                cacheRet.Add(f);
                            }
                        }
                    }
                }
                return cacheRet;
            }
            finally
            {
                mProjectLocalFileTableLock.ExitReadLock();
            }
        }

        public bool DeleteProjectLocalFile(int project_table_pk, int project_local_file_table_pk)
        {
            try
            {
                mProjectLocalFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectLocalFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectLocalFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    //int worked = data.RemoveAll((x => (x.Id == project_local_file_table_pk)));
                    //Console.WriteLine("DeleteProjectLocalFile the size {0}", worked);
                    //return worked != 0;
                    int index = -1;
                    for (int i = 0; i < data.Count; i++)
                    {
                        if (project_local_file_table_pk == data[i].Id)
                        {
                            index = i;
                        }
                    }
                    if (index != -1)
                    {
                        SkydrmLocalApp.Singleton.Log.Info("Delete Project Local File Table Cache:"+ data[index].Name);
                        data.RemoveAt(index);
                    }
                    return index != -1;
                }
                return false;
            }
            finally
            {
                mProjectLocalFileTableLock.ExitWriteLock();
            }
        }

        public bool DeleteProjectAllLocalFiles(int project_table_pk)
        {
            try
            {
                mProjectLocalFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectLocalFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectLocalFileTableCaches[key];

                if (data != null)
                {
                    data.Clear();
                }

                return false;
            }
            finally
            {
                mProjectLocalFileTableLock.ExitWriteLock();
            }
        }

        public bool DeleteProjectFolderLocalFiles(int project_table_pk, int projectFile_RowNumber)
        {
            try
            {
                mProjectLocalFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectLocalFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                List<ProjectLocalFile> data = mProjectLocalFileTableCaches[key];

                if (data != null && data.Count != 0)
                {
                    for (int i = data.Count - 1; i >= 0; i--)
                    {
                        if (projectFile_RowNumber == data[i].ProjectFileTablePk)
                        {
                            data.Remove(data[i]);
                        }
                    }
                }

                return false;
            }
            finally
            {
                mProjectLocalFileTableLock.ExitWriteLock();
            }
        }

        public bool AddLocalFileToProject(int project_table_pk, string FolderId,
            string name, string path,
            int size, DateTime lastModified)
        {
            throw new NotImplementedException("Should never reach here.");
        }

        public bool AddLocalFileToProject(int id, int project_table_pk, string FolderId,
            string name, string path,
            int size, DateTime lastModified)
        {
            try
            {
                mProjectFileTableLock.EnterReadLock();
                mProjectLocalFileTableLock.EnterWriteLock();
                

                int key = project_table_pk;
                if (!mProjectFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var pFileCaches = mProjectFileTableCaches[key];
                List<ProjectFile> folders = new List<ProjectFile>();
                if (pFileCaches != null && pFileCaches.Count != 0)
                {
                    foreach (var f in pFileCaches)
                    {
                        if (f.Rms_display_path.Equals(FolderId, StringComparison.OrdinalIgnoreCase))
                        {
                            folders.Add(f);
                        }
                    }
                }
                if (folders.Count == 0)
                {
                    return false;
                }
                int projectfile_table_pk = -1;
                foreach (var f in folders)
                {
                    if (f.Rms_display_path.Equals(FolderId, StringComparison.OrdinalIgnoreCase))
                    {
                        projectfile_table_pk = f.Id;
                    }
                }
                if (project_table_pk == -1)
                {
                    return false;
                }
                var newItem = ConstructNewItem(id, project_table_pk, projectfile_table_pk, name, path, size, lastModified);
                if (mProjectLocalFileTableCaches.ContainsKey(key))
                {
                    var data = mProjectLocalFileTableCaches[key];
                    if (data != null)
                    {
                        data.Add(newItem);
                    }
                    else
                    {
                        List<ProjectLocalFile> container = new List<ProjectLocalFile>()
                        {
                            newItem
                        };
                        mProjectLocalFileTableCaches.Add(key, container);
                    }
                }
                else
                {
                    List<ProjectLocalFile> container = new List<ProjectLocalFile>()
                        {
                            newItem
                        };
                    mProjectLocalFileTableCaches.Add(key, container);
                }
                return true;
            }
            finally
            {
                mProjectLocalFileTableLock.ExitWriteLock();
                mProjectFileTableLock.ExitReadLock();
            }
        }
        private ProjectLocalFile ConstructNewItem(int id, int project_table_pk, int projectfile_table_pk,
                        string name, string path, int size, DateTime lastModified)
        {
            ProjectLocalFile item = new ProjectLocalFile
            {
                Id = id,
                ProjectTablePk = project_table_pk,
                ProjectFileTablePk = project_table_pk,
                Name = name,
                Path = path,
                Size = size,
                Last_modified_time = lastModified
            };
            return item;
        }

        public bool UpdateProjectLocalFileOperationStatus(int project_table_pk, int project_local_file_table_pk, int newStatus)
        {
            try
            {
                mProjectLocalFileTableLock.EnterWriteLock();

                int key = project_table_pk;
                if (!mProjectLocalFileTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectLocalFileTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var f in data)
                    {
                        if (f.Id == project_local_file_table_pk)
                        {
                            f.Operation_status = newStatus;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectLocalFileTableLock.ExitWriteLock();
            }
        }
        #endregion

        #region Project classification table.
        public string GetProjectClassification(int project_table_pk)
        {
            try
            {
                mProjectTableLock.EnterReadLock();

                int key = User_Primary_key;
                if (!mProjectTableCaches.ContainsKey(key))
                {
                    return "";
                }
                var data = mProjectTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var p in data)
                    {
                        if (p.Id == project_table_pk)
                        {
                            return p.Rms_classifcation_json;
                        }
                    }
                }
                return "";
            }
            finally
            {
                mProjectTableLock.ExitReadLock();
            }
        }

        public bool UpdateProjectClassification(int project_table_pk, string classificationJson)
        {
            try
            {
                mProjectTableLock.EnterWriteLock();

                int key = User_Primary_key;
                if (!mProjectTableCaches.ContainsKey(key))
                {
                    return false;
                }
                var data = mProjectTableCaches[key];
                if (data != null && data.Count != 0)
                {
                    foreach (var p in data)
                    {
                        if (p.Id == project_table_pk)
                        {
                            p.Rms_classifcation_json = classificationJson;
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                mProjectTableLock.ExitWriteLock();
            }
        }
        #endregion
    }
}
