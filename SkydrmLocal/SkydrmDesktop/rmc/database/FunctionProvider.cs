
using Newtonsoft.Json;
using SkydrmLocal.rmc;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database.memcache;
using SkydrmLocal.rmc.database.table.myvault;
using SkydrmLocal.rmc.database.table.recentTouchedFile;
using SkydrmLocal.rmc.database.table.sharedwithme;
using SkydrmLocal.rmc.database2.manager;
using SkydrmLocal.rmc.database2.table.myvault;
using SkydrmLocal.rmc.database2.table.project;
using SkydrmLocal.rmc.database2.table.server;
using SkydrmLocal.rmc.database2.table.user;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.database.memcache.project.ProjectCacheProxy;
using SkydrmLocal.rmc.database;
using SkydrmLocal.rmc.database.table.systembucket;
using SkydrmDesktop;
using SkydrmDesktop.rmc.database.table.workspace;
using SkydrmDesktop.rmc.database.table.project;
using SkydrmDesktop.rmc.database.table.myspace;
using SkydrmDesktop.rmc.database.table.externalrepo;
using SkydrmDesktop.rmc.database.table.externalrepo.googledrive;
using SkydrmDesktop.rmc.database.table.externalrepo.dropbox;
using SkydrmDesktop.rmc.database.table.externalrepo.Box;
using OneDrive = SkydrmDesktop.rmc.database.table.externalrepo.oneDrive;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmDesktop.rmc.database.table.sharedworkspace;

namespace Skydrmlocal.rmc.database2
{
    // Designed as presentation-layer to provide all functionalities local db can provide.
    // Required that restrictly bind to current login user. 
    // need to wrapper db_driver to a separate class

    public class FunctionProvider
    {
        string DataBasePath;
        string DataBaseConnectionString;

        // need to wrapper to class ActivePresenterObj
        int Server_Primary_Key;
        int User_Primary_Key;
        int User_Login_Counts = 0;
        // user's project id TO PK
        Dictionary<int, int> Project_Id2PK = new Dictionary<int, int>();

        // user's external repo id to PK
        Dictionary<string, int> RmsExternalRepo_Id2PK = new Dictionary<string, int>();
        // end need

        private DbVersionControl versionControl = new DbVersionControl();

        SkydrmLocal.rmc.database.memcache.project.IProjectCache mProjectCacheProxy;

        #region Init
        public FunctionProvider(string DbPath)
        {

            DataBasePath = Path.Combine(DbPath, Config.Database_Name);
            // by omsond, normally, we should set busy_timeout as 10s,
            // but there must be some unoptimized code, so I set it as 60s
            DataBaseConnectionString =
                @"Data Source=" + DataBasePath + ";foreign_keys=true;busy_timeout=60000;";

            versionControl.DetectVersion(DataBaseConnectionString);
        }

        #endregion

        #region UserSession

        public void UpsertUser(int rms_user_id,
                                string name,
                                string email,
                                string passcode,
                                int rms_user_type,
                                string rms_user_raw_json)
        {
            string defaultWaterMark = JsonConvert.SerializeObject(new SkydrmLocal.rmc.sdk.WaterMarkInfo() { text = "$(User)$(Break)$(Date)$(Time)" });
            string defaultExpiration = JsonConvert.SerializeObject(new SkydrmLocal.rmc.sdk.Expiration());
            string defaultQuota = JsonConvert.SerializeObject(new SkydrmLocal.rmc.featureProvider.Quota());
            string defaultPreference = JsonConvert.SerializeObject(new SkydrmLocal.rmc.featureProvider.User.UserPreference()
            {
                isStartUpload = true,
                heartBeatIntervalSec = SkydrmLocal.rmc.sdk.Config.Deault_Heartbeat,
                isCentralLocationRadio = false,
                isCentralPlcRadio = true
            });

            ExecuteReader(UserDao.Upsert_SQL(rms_user_id,
                    name, email, passcode, Server_Primary_Key,
                    rms_user_type, rms_user_raw_json, defaultWaterMark,
                    defaultExpiration, defaultQuota, defaultPreference), (reader) =>
                    {
                        SkydrmApp.Singleton.Log.Info("***********get this user's primary key ***********");
                        // get this user's primary key
                        if (reader.Read())
                        {
                            User_Primary_Key = Int32.Parse(reader[0].ToString());
                            User_Login_Counts = Int32.Parse(reader[1].ToString());
                            SkydrmApp.Singleton.Log.Info("***********User_Primary_Key :***********" + User_Primary_Key);
                            SkydrmApp.Singleton.Log.Info("***********User_Login_Counts :***********" + User_Login_Counts);
                        }
                    });
        }

        // locate server_id by router     
        // set serverID and userID as this user
        public bool QueryUserPK(string email, string router, string tenant)
        {
            bool ret = true;

            try
            {
                // locate server_id
                int rms_user_id = -1;
                ExecuteReader(ServerDao.Query_ID_SQL(router, tenant), (reader) =>
                {
                    if (reader.Read())
                    {
                        // set current server
                        Server_Primary_Key = Int32.Parse(reader[0].ToString());
                    }
                    else
                    {
                        ret = false;
                    }
                });


                if (ret)
                {
                    // get rms_user_id
                    ExecuteReader(UserDao.Query_User_Id_SQL(email, Server_Primary_Key), (reader) =>
                    {
                        if (reader.Read())
                        {
                            // set rms_user_id
                            rms_user_id = Int32.Parse(reader[0].ToString());
                        }
                        else
                        {
                            ret = false;
                        }
                    });
                }

                if (ret)
                {
                    // update user
                    ExecuteReader(UserDao.Update_Auto_SQL(email, rms_user_id, Server_Primary_Key), (reader) =>
                    {
                        if (reader.Read())
                        {
                            // set current user
                            User_Primary_Key = Int32.Parse(reader[0].ToString());
                            User_Login_Counts = Int32.Parse(reader[1].ToString());
                        }
                        else
                        {
                            ret = false;
                        }
                    });
                }

            }
            catch (Exception e)
            {
                ret = false;
                SkydrmApp.Singleton.Log.Error("Query user primary key failed: " + e.ToString());
            }


            return ret;
        }

        // Init project cache and do pre-load when user login.
        public void InitProjectCache()
        {
            mProjectCacheProxy = new SkydrmLocal.rmc.database.memcache.project.ProjectCacheProxy(DataBaseConnectionString, User_Primary_Key);

            // Project pre-load tasks must be started after project id map has been filled.
            FillMapOfProjectId2PK(() =>
            {
                mProjectCacheProxy.OnInitialize();
            });

        }
        private void InitExternalRepoDb()
        {
            FillMapOfRepoId2PK();
            // todo other
        }

        public void InitDb()
        {
            InitProjectCache();

            InitExternalRepoDb();
        }

        public void OnUserLogout()
        {
            try
            {
                ExecuteNonQuery(UserDao.Update_LastLogout_SQL(User_Primary_Key));
                ExecuteNonQuery(ServerDao.Update_LastLogout_SQL(Server_Primary_Key));

                this.User_Primary_Key = -1;

                Project_Id2PK.Clear();
                RmsExternalRepo_Id2PK.Clear();

                // wait for critical mem data recreate;
                if (mProjectCacheProxy != null)
                {
                    mProjectCacheProxy.OnDestroy();
                    //mProjectCacheProxy = null;
                }

            }
            catch
            {

            }

        }
        public void UpsertServer(string router, string url, string tenand, bool isOnPremise)
        {
            ExecuteReader(ServerDao.Upsert_SQL(router, url, tenand, isOnPremise), (reader) =>
             {
                 // get this server item's primary key
                 if (reader.Read())
                 {

                     Server_Primary_Key = Int32.Parse(reader[0].ToString());
                     SkydrmApp.Singleton.Log.Info("***********Server_Primary_Key :***********" + Server_Primary_Key);
                 }
             });
        }

        //only select 5 data item by desc
        //display to user
        public List<string> GetRouterUrl()
        {
            List<string> result = new List<string>();

            ExecuteReader(ServerDao.Query_Router_Url_SQL(), (reader) =>
            {
                while (reader.Read())
                {
                    string router_url = reader["router_url"].ToString();

                    result.Add(router_url);
                }
            });
            return result;
        }

        public string GetCurrentRouterUrl()
        {
            string result = SkydrmLocal.rmc.sdk.Config.Default_Router;

            ExecuteReader(ServerDao.Query_Current_Router_Url_SQL(Server_Primary_Key), (reader) =>
            {
                while (reader.Read())
                {
                    result = reader["router_url"].ToString();
                }
            });
            return result;
        }

        //display to user, Url is a combination of router and tenant.
        public string GetUrl()
        {
            string result = SkydrmLocal.rmc.sdk.Config.Default_Router;

            ExecuteReader(ServerDao.Query_Url_SQL(Server_Primary_Key), (reader) =>
            {
                while (reader.Read())
                {
                    result = reader["url"].ToString();
                }
            });
            return result;
        }

        public void UpdateUserName(string new_name)
        {
            try
            {
                ExecuteNonQuery(UserDao.Update_Name_SQL(new_name, User_Primary_Key));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public User GetUser()
        {
            User u = null;
            ExecuteReader(UserDao.Query_User(User_Primary_Key), (reader) =>
            {
                SkydrmApp.Singleton.Log.Info("***********User_Primary_Key :***********" + User_Primary_Key);
                SkydrmApp.Singleton.Log.Info("***********reader.HasRows :***********" + reader.HasRows);
                if (reader.Read())
                {
                    SkydrmApp.Singleton.Log.Info("***********reader.Read() :***********" + "true");
                    u = User.NewByReader(reader);
                }
            });
            if (u == null)
            {
                SkydrmApp.Singleton.Log.Info("***********User is null**********");

                throw new Exception("Critical Error,Get user failed");
            }
            return u;
        }


        public void UpdateUserWaterMark(string watermark)
        {
            ExecuteNonQuery(UserDao.Update_Watermark(User_Primary_Key, watermark));
        }

        public void UpdateUserExpiration(string expiration)
        {
            ExecuteNonQuery(UserDao.Update_Expiration(User_Primary_Key, expiration));
        }

        public void UpdateUserPreference(string preference)
        {
            ExecuteNonQuery(UserDao.Update_Preference(User_Primary_Key, preference));
        }


        public delegate void OnFillMapOfProjectId();

        private void FillMapOfProjectId2PK(OnFillMapOfProjectId callback)
        {
            ExecuteReader(ProjectDao.Query_PK_PID_SQL(User_Primary_Key), (reader) =>
            {
                while (reader.Read())
                {
                    int project_id = int.Parse(reader["rms_project_id"].ToString());
                    int pk = int.Parse(reader["id"].ToString());
                    if (Project_Id2PK.ContainsKey(project_id))
                    {
                        Project_Id2PK[project_id] = pk;
                    }
                    else
                    {
                        Project_Id2PK.Add(project_id, pk);
                    }
                }
                callback?.Invoke();
            });
        }

        private void FillMapOfRepoId2PK()
        {
            ExecuteReader(RmsExternalRepoDao.Query_PK_RepoId_SQL(User_Primary_Key), (reader) =>
            {
                while (reader.Read())
                {
                    string repoid = reader["rms_repo_id"].ToString();
                    int pk = int.Parse(reader["id"].ToString());
                    if (RmsExternalRepo_Id2PK.ContainsKey(repoid))
                    {
                        RmsExternalRepo_Id2PK[repoid] = pk;
                    }
                    else
                    {
                        RmsExternalRepo_Id2PK.Add(repoid, pk);
                    }
                }
            });
        }

        #endregion // end UserSession

        #region Project

        //
        // Operation for project table
        //
        public Project[] ListProject()
        {
            return mProjectCacheProxy.ListProject().ToArray();
        }

        public void DeleteProject(int rms_project_id)
        {
            //
            // Note: Now project heartBeat will trigger the whole treeView Refresh, don't need this callback to refresh ui.
            //
            //mProjectCacheProxy.DeleteProject(rms_project_id, () => {
            //    // Notify refresh ui.
            //    SkydrmApp.Singleton.InvokeEvent_ProjectUpdate();
            //});

            mProjectCacheProxy.DeleteProject(rms_project_id, null);
        }

        public void UpsertProject(int project_id, string project_name, string project_display_name,
                                 string project_description, bool isOwner, string tenant_id, OnResult Result)
        {
            mProjectCacheProxy.UpsertProject(project_id, project_name, project_display_name,
                project_description, isOwner, tenant_id, (ppk) =>
                {
                    Console.WriteLine("Invoke UpsertProject padding project_id {0}", ppk);
                    if (Project_Id2PK.ContainsKey(project_id))
                    {
                        Project_Id2PK[project_id] = ppk;
                    }
                    else
                    {
                        Project_Id2PK.Add(project_id, ppk);
                    }
                    // fix bug; for each project we have to generate a faked root "/" in projectFile
                    InsertFakedRoot(project_id);
                });

            Result?.Invoke();
        }

        public void UpsertProjectIsEnabledAdhoc(int project_id, bool isEnabled)
        {
            if (!IsProjectIdMapContainsKey(project_id))
            {
                return;
            }
            mProjectCacheProxy.UpsertProjectIsEnabledAdhoc(Project_Id2PK[project_id], isEnabled);
        }

        public delegate void OnResult();

        public string GetProjectClassification(int projectId)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return "";
            }
            return mProjectCacheProxy.GetProjectClassification(Project_Id2PK[projectId]);
        }

        public void UpdateProjectClassification(int projectId, string classificationJson)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return;
            }
            Console.WriteLine("classificationJson {0}", classificationJson);
            mProjectCacheProxy.UpdateProjectClassification(Project_Id2PK[projectId], classificationJson);
        }


        //
        // Operation for project file table
        //
        public void DeleteProjectFile(int project_id, string rms_file_id)
        {
            if (!IsProjectIdMapContainsKey(project_id))
            {
                return;
            }
            mProjectCacheProxy.DeleteProjectFile(Project_Id2PK[project_id], rms_file_id);
        }

        public void DeleteProjectFolderAndAllSubFiles(int project_id, string rms_path_id)
        {
            if (!IsProjectIdMapContainsKey(project_id))
            {
                return;
            }
            mProjectCacheProxy.DeleteProjectFolderAndAllSubFiles(Project_Id2PK[project_id], rms_path_id);
        }

        public struct InstertProjectFile
        {
            public int project_id;

            public string file_id;
            public string file_duid;
            public string file_display_path;
            public string file_path_id;
            public string file_nxl_name;
            // sdk new added
            public Int64 file_lastModifiedTime;
            public Int64 file_creationTime;
            public Int64 file_size;
            public Int32 file_rms_ownerId;
            public string file_ownerDisplayName;
            public string file_ownerEmail;
        }

        // These will be inserted into db table.
        public struct InstertProjectFileEx
        {
            public int project_id;
            //
            public string file_id;
            public string file_duid;
            public string file_display_path;
            public string file_path_id;
            public string file_nxl_name;
            // 
            public Int64 file_lastModifiedTime;
            public Int64 file_creationTime;
            public Int64 file_size;
            public Int32 file_rms_ownerId;
            public string file_ownerDisplayName;
            public string file_ownerEmail;
            // Extend
            public int isShared;
            public int isRevoked;
            public List<uint> sharedWithProject;
        }

        // Deprecated.
        public void UpsertProjectFileBatch(InstertProjectFile[] files)
        {
            //if (files.Length == 0)
            //{
            //    return;
            //}
            //mProjectCacheProxy.UpsertProjectFileBatch(files, Project_Id2PK);
        }

        // Extend for supporting sharing transaction.
        public void UpsertProjectFileBatchEx(InstertProjectFileEx[] files)
        {
            if (files.Length == 0)
            {
                return;
            }
            mProjectCacheProxy.UpsertProjectFileBatch(files, Project_Id2PK);
        }

        public ProjectFile[] ListProjectFile(int rms_project_id, string path)
        {
            if (!IsProjectIdMapContainsKey(rms_project_id))
            {
                return new ProjectFile[0];
            }
            return mProjectCacheProxy.ListProjectFile(Project_Id2PK[rms_project_id], path).ToArray();
        }

        public List<ProjectFile> ListAllProjectFile(int rms_project_id)
        {
            if (!IsProjectIdMapContainsKey(rms_project_id))
            {
                return new List<ProjectFile>();
            }
            return mProjectCacheProxy.ListAllProjectFile(Project_Id2PK[rms_project_id]);
        }

        public ProjectFile[] ListProjectOfflineFile(int rms_project_id)
        {
            if (!IsProjectIdMapContainsKey(rms_project_id))
            {
                return new ProjectFile[0];
            }
            return mProjectCacheProxy.ListProjectOfflineFile(Project_Id2PK[rms_project_id]).ToArray();
        }

        public void UpdateProjectFileOperationStatus(int projectTablePk, int project_file_table_pk, int newStatus)
        {
            mProjectCacheProxy.UpdateProjectFileOperationStatus(projectTablePk, project_file_table_pk, newStatus);
        }

        public void UpdateProjectFileWhenOverwriteInLeaveCopy(int projectTablePk, int project_file_table_pk, string duid, int newStatus,
            long size, DateTime lastModifed)
        {
            mProjectCacheProxy.UpdateProjectFileWhenOverwriteInLeaveCopy(projectTablePk, project_file_table_pk, duid, newStatus,
                size, lastModifed);
        }

        public void UpdateProjectFileOfflineMark(int projectTablePk, int project_file_table_pk, bool newMark)
        {
            mProjectCacheProxy.UpdateProjectFileOfflineMark(projectTablePk, project_file_table_pk, newMark);
        }

        public void UpdateProjectFileLocalpath(int projectTablePk, int project_file_table_pk, string newPath)
        {
            mProjectCacheProxy.UpdateProjectFileLocalpath(projectTablePk, project_file_table_pk, newPath);
        }

        public void UpdateProjectFileLastModifiedTime(int projectTablePk, int project_file_table_pk, DateTime lastModifiedTime)
        {
            mProjectCacheProxy.UpdateProjectFileLastModifiedTime(projectTablePk, project_file_table_pk, lastModifiedTime);
        }

        public void UpdateProjectFileFileSize(int projectTablePk, int project_file_table_pk, long fileSize)
        {
            mProjectCacheProxy.UpdateProjectFileFileSize(projectTablePk, project_file_table_pk, fileSize);
        }

        public void UpdateProjectFileEditStatus(int projectTablePk, int project_file_table_pk, int newStatus)
        {
            mProjectCacheProxy.UpdateProjectFileEditStatus(projectTablePk, project_file_table_pk, newStatus);
        }

        public void UpdateProjectFileModifyRightsStatus(int projectTablePk, int project_file_table_pk, int newStatus)
        {
            mProjectCacheProxy.UpdateProjectFileModifyRightsStatus(projectTablePk, project_file_table_pk, newStatus);
        }

        // sharing transaction
        public void UpdateProjectFileIsShared(int projectTablePk, int project_file_table_pk, int newValue)
        {
            mProjectCacheProxy.UpdateProjectFileIsShared(projectTablePk, project_file_table_pk, newValue);
        }
        public void UpdateProjectFileIsRevoked(int projectTablePk, int project_file_table_pk, int newValue)
        {
            mProjectCacheProxy.UpdateProjectFileIsRevoked(projectTablePk, project_file_table_pk, newValue);
        }
        public void UpdateProjectFileSharedWith(int projectTablePk, int project_file_table_pk, List<uint> newList)
        {
            mProjectCacheProxy.UpdateProjectFileSharedWith(projectTablePk, project_file_table_pk, newList);
        }

        //
        // Operation for project local file.
        //
        // find a project local added file's rms parent folder name for UI to use
        public string QueryProjectLocalFileRMSParentFolder(int projectId, int projectFile_RowNumber)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return "";
            }
            return mProjectCacheProxy.QueryProjectLocalFileRMSParentFolder(Project_Id2PK[projectId], projectFile_RowNumber);
        }

        // Query project file table id (row number)
        public int QueryProjectFileId(int projectId, string rms_path_id)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return -1;
            }

            return mProjectCacheProxy.QueryProjectFileId(projectId, rms_path_id);
        }

        public void UpdateProjectLocalFileOperationStatus(int project_table_pk, int project_local_file_table_pk, int newStatus)
        {
            mProjectCacheProxy.UpdateProjectLocalFileOperationStatus(project_table_pk, project_local_file_table_pk, newStatus);
        }

        public void UpdateProjectLocalFileName(int project_table_pk, int project_local_file_table_pk, string newName)
        {
            mProjectCacheProxy.UpdateProjectLocalFileName(project_table_pk, project_local_file_table_pk, newName);
        }

        public void UpdateProjectLocalFileLocalPath(int project_table_pk, int project_local_file_table_pk, string localPath)
        {
            mProjectCacheProxy.UpdateProjectLocalFileLocalPath(project_table_pk, project_local_file_table_pk, localPath);
        }

        public void UpdateProjectLocalFileReserved1(int project_table_pk, int project_local_file_table_pk, string reserved1)
        {
            mProjectCacheProxy.UpdateProjectLocalFileReserved1(project_table_pk, project_local_file_table_pk, reserved1);
        }

        public void DeleteProjectLocalFile(int project_table_pk, int project_local_file_table_pk)
        {
            mProjectCacheProxy.DeleteProjectLocalFile(project_table_pk, project_local_file_table_pk);
        }

        public void DeleteProjectAllLocalFiles(int project_table_pk)
        {
            mProjectCacheProxy.DeleteProjectAllLocalFiles(project_table_pk);
        }

        public void DeleteProjectFolderLocalFiles(int project_table_pk, int projectFile_RowNumber)
        {
            mProjectCacheProxy.DeleteProjectFolderLocalFiles(project_table_pk, projectFile_RowNumber);
        }

        public void AddLocalFileToProject(int projectId, string FolderId,
            string name, string path, int size, DateTime lastModified, string reserved1)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return;
            }
            mProjectCacheProxy.AddLocalFileToProject(Project_Id2PK[projectId], FolderId, name, path, size, lastModified, reserved1);
        }

        public ProjectLocalFile[] ListProjectLocalFiles(int projectId, string FolderId)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return new ProjectLocalFile[0];
            }
            return mProjectCacheProxy.ListProjectLocalFiles(Project_Id2PK[projectId], FolderId).ToArray();
        }

        public ProjectLocalFile[] ListProjectLocalFiles(int projectId)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return new ProjectLocalFile[0];
            }
            return mProjectCacheProxy.ListProjectLocalFiles(Project_Id2PK[projectId]).ToArray();
        }


        private bool IsProjectIdMapContainsKey(int key)
        {
            return Project_Id2PK != null && Project_Id2PK.ContainsKey(key);
        }

        // adding any new function that may changed the rms_project_file table, 
        // must tell the cache table to update syncedly.
        private void InsertFakedRoot(int project_id)
        {
            if (!IsProjectIdMapContainsKey(project_id))
            {
                return;
            }
            mProjectCacheProxy.InsertFakedRoot(Project_Id2PK[project_id]);
        }

        //
        // Operation for shared with project file.
        //
        public struct InstertSharedWithProjectFile
        {
            public int project_id;
            public string file_duid;
            public string file_name;
            public string file_type;
            public Int64 file_size;
            public Int64 shared_date;
            public string shared_by;
            public string transaction_id;
            public string transaction_code;
            public string shared_url;
            public string rights_json;
            public int is_owner;
            public int protection_type;
            public string shared_by_project;
        }

        public List<SharedWithProjectFile> ListSharedWithProjectFile(int rms_project_id)
        {
            if (!IsProjectIdMapContainsKey(rms_project_id))
            {
                return new List<SharedWithProjectFile>();
            }
            return mProjectCacheProxy.ListSharedWithProjectFile(Project_Id2PK[rms_project_id]);
        }

        public List<SharedWithProjectFile> ListSharedWithProjectOfflineFile(int rms_project_id)
        {
            if (!IsProjectIdMapContainsKey(rms_project_id))
            {
                return new List<SharedWithProjectFile>();
            }
            return mProjectCacheProxy.ListSharedWithProjectOfflineFile(Project_Id2PK[rms_project_id]);
        }

        public void UpsertSharedWithProjectFileBatch(InstertSharedWithProjectFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }
            mProjectCacheProxy.UpsertSharedWithProjectFileBatch(files, Project_Id2PK);
        }

        public void DeleteSharedWithProjectFile(int project_table_pk, string duid)
        {
            mProjectCacheProxy.DeleteSharedWithProjectFile(project_table_pk, duid);
        }

        public void UpdateSharedWithProjectFileStatus(int project_table_pk, int shared_with_project_file_table_pk, int newStatus)
        {
            mProjectCacheProxy.UpdateSharedWithProjectFileStatus(project_table_pk, shared_with_project_file_table_pk, newStatus);
        }

        public void UpdateSharedWithProjectFileOfflineMark(int project_table_pk, int shared_with_project_file_table_pk, bool newMark)
        {
            mProjectCacheProxy.UpdateSharedWithProjectFileOfflineMark(project_table_pk, shared_with_project_file_table_pk, newMark);
        }

        public void UpdateSharedWithProjectFileLocalpath(int project_table_pk, int shared_with_project_file_table_pk, string newPath)
        {
            mProjectCacheProxy.UpdateSharedWithProjectFileLocalpath(project_table_pk, shared_with_project_file_table_pk, newPath);
        }

        #endregion  // end project

        #region SharedWithMe
        public struct InsertSharedWithMeFile
        {
            public string duid;
            public string name;
            public string type;
            public Int64 size;
            public Int64 shared_date;
            public string shared_by;
            public string transcation_id;
            public string transcation_code;
            public string shared_link_url;
            public string rights_json;
            public string comments;
            public bool is_owner;
        }
        public void UpsertSharedWithMeFileBatch(InsertSharedWithMeFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }
            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //stopwatch.Start();
            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                    SharedWithMeFileDao.Upsert_SQL(User_Primary_Key,
                    i.duid, i.name, i.type, i.size,
                    JavaTimeConverter.ToCSLongTicks(i.shared_date),
                    i.shared_by,
                    i.transcation_id,
                    i.transcation_code,
                    i.shared_link_url,
                    i.rights_json,
                    i.comments,
                    i.is_owner
                    )
                    );
            }
            ExecuteNonQueryBatch(sqls.ToArray());

            //stopwatch.Stop();
            //Console.WriteLine("UpsertSharedWithMeFileBatch time: {0} mills", stopwatch.Elapsed.TotalMilliseconds);
        }
        public void UpsertSharedWithMeFile(string duid, string name,
            string type, Int64 size, Int64 shared_date, string shared_by,
            string transcation_id, string transcation_code,
            string shared_link_url, string rights_json, string comments,
            bool is_owner
            )
        {
            // Java time mills is not same as C# time mills
            shared_date = JavaTimeConverter.ToCSLongTicks(shared_date);

            // convert project id t= project talbe pk
            ExecuteNonQuery(SharedWithMeFileDao.Upsert_SQL(
                User_Primary_Key, duid, name,
                type, size, shared_date, shared_by, transcation_id, transcation_code,
                shared_link_url, rights_json, comments, is_owner
                ));
        }
        public SharedWithMeFile[] ListSharedWithMeFile()
        {
            // return Prjectinfos
            List<SharedWithMeFile> rt = new List<SharedWithMeFile>();
            ExecuteReader(SharedWithMeFileDao.Query_SQL(User_Primary_Key), (reader) =>
            {
                if (!reader.HasRows)
                {
                    return;
                }
                while (reader.Read())
                {
                    rt.Add(SharedWithMeFile.NewByReader(reader));
                }
            });


            return rt.ToArray();

        }

        public void UpdateSharedWithMeFileLocalpath(
                int sharedWithMeFileTablePk,
                string newPath)
        {
            ExecuteNonQuery(SharedWithMeFileDao.Update_LocalPath_SQL(
                sharedWithMeFileTablePk, newPath)
            );
        }

        public void UpdateSharedWithMeFileIsOffline(
            int sharedWithMeFileTablePk, bool isOffline)
        {
            ExecuteNonQuery(SharedWithMeFileDao.Update_IsOffline_SQL(
                sharedWithMeFileTablePk, isOffline)
            );
        }

        public void UpdateSharedWithMeFileOperationStatus(
            int sharedWithMeFileTablePk, int newStatus)
        {
            ExecuteNonQuery(SharedWithMeFileDao.Update_FileStatus_SQL(
                sharedWithMeFileTablePk, newStatus)
            );
        }


        public void DeleteSharedWithMeFile(string RmsName)
        {
            ExecuteNonQuery(SharedWithMeFileDao.Delete_Item_SQL(User_Primary_Key, RmsName));
        }

        public void UpdateSharedWithMeFileEditStatus(int sharedWithMeFileTablePk, int newStatus)
        {
            ExecuteNonQuery(SharedWithMeFileDao.Update_EditStatus_SQL(sharedWithMeFileTablePk, newStatus));
        }

        public void UpdateSharedWithMeFileModifyRightsStatus(int sharedWithMeFileTablePk, int newStatus)
        {
            ExecuteNonQuery(SharedWithMeFileDao.Update_ModifiRights_Status_SQL(sharedWithMeFileTablePk, newStatus));
        }
        #endregion

        #region WorkSpace

        //
        // For WorkSpace file
        //
        public struct InsertWorkSpaceFile
        {
            public string file_id;
            public string duid;
            public string path_display;
            public string path_id;
            public string nxl_name;
            public string file_type;
            public Int64 last_modified;
            public Int64 created_time;
            public Int64 size;
            public bool is_folser;
            public int owner_id;
            public string owner_display_name;
            public string owner_email;
            public int modified_by; // by who(userId) modified
            public string modified_by_name;
            public string modified_by_email;
        }

        public void UpsertWorkSpaceFileBatch(InsertWorkSpaceFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }

            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                        WorkSpaceFileDao.UPSERT_SQL(User_Primary_Key,
                        i.file_id, i.duid, i.path_display, i.path_id, i.nxl_name, i.file_type,
                        i.last_modified, i.created_time, i.size,
                        i.is_folser, i.owner_id, i.owner_display_name, i.owner_email,
                        i.modified_by, i.modified_by_name, i.modified_by_email)
                    );
            }
            ExecuteNonQueryBatch(sqls.ToArray());

        }

        // Insert faked root for workSpace that make added local file into workspace root folder.
        public bool InsertWorkSpaceFakedRoot()
        {
            try
            {
                SqliteOpenHelper.ExecuteNonQuery(DataBaseConnectionString,
                    WorkSpaceFileDao.InsertFakedRoot_SQL(User_Primary_Key));
            }
            catch (SQLiteException e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
                return false;
            }

            return true;
        }

        public WorkSpaceFile[] ListAllWorkSpaceFiles()
        {
            int user_tb_pk = User_Primary_Key;
            List<WorkSpaceFile> rt = new List<WorkSpaceFile>();
            ExecuteReader(WorkSpaceFileDao.QueryAll_SQL(user_tb_pk), (reader) =>
            {
                while (reader.Read())
                {
                    WorkSpaceFile item = WorkSpaceFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public WorkSpaceFile[] ListWorkSpaceFiles(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if (path.Length > 1 && !path.EndsWith("/"))
            {
                path += '/';
            }

            var rt = new List<WorkSpaceFile>();
            int user_tb_pk = User_Primary_Key;
            ExecuteReader(WorkSpaceFileDao.Query_SQL(user_tb_pk, path), (reader) =>
            {
                while (reader.Read())
                {
                    WorkSpaceFile item = WorkSpaceFile.NewByReader(reader);
                    // filter by Rms_path_id, only return first level items
                    if (FileHelper.IsDirectChild(item.RmsPathId, path))
                    {
                        rt.Add(item);
                    }
                }
            });

            return rt.ToArray();
        }

        public string GetWorkSpaceLocalFileRmsParentFolder(int workspaceFile_pk)
        {
            int user_tb_pk = User_Primary_Key;
            string rt = "/";
            ExecuteReader(WorkSpaceFileDao.Query_DisplayPath_SQL(user_tb_pk, workspaceFile_pk), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["rms_path_display"].ToString();
                }
            });

            return rt;
        }

        public void UpdateWhenOverwriteInLeaveCopy(int table_pk, string duid, int status, long size, DateTime lastModifed)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(WorkSpaceFileDao.Update_NXL_For_Overwrite_LeaveCopyModel(table_pk, duid, status, size, lastModifed));
        }

        public void UpdateWorkSpaceFileStatus(int table_pk, int status)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(WorkSpaceFileDao.Update_NXL_Status_SQL(table_pk, status));
        }

        public void UpdateWorkSpaceFileModifyRightsStatus(int table_pk, int newStatus)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(WorkSpaceFileDao.Update_NXL_ModifyRightsStatus_SQL(table_pk, newStatus));
        }

        public void UpdateWorkSpaceFileEditsStatus(int table_pk, int newStatus)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(WorkSpaceFileDao.Update_NXL_EditStatus_SQL(table_pk, newStatus));
        }

        public void UpdateWorkSpaceFileOffline(int table_pk, bool is_offline)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(WorkSpaceFileDao.Update_Offline_Mark_SQL(table_pk, is_offline));
        }

        public void UpdateWorkSpaceFileLocalPath(int table_pk, string localPath)
        {
            ExecuteNonQuery(WorkSpaceFileDao.Update_Nxl_Local_Path(table_pk, localPath));
        }

        public void DeleteWorkSpaceFile(string rmsFileId)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(WorkSpaceFileDao.Delete_Item_SQL(User_Primary_Key, rmsFileId));
        }

        public void DeleteWorkSpaceFolderAndSubChildren(string rmsPathId)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(WorkSpaceFileDao.Delete_Folder_And_SubChildren_SQL(User_Primary_Key, rmsPathId));
        }

        //
        // For WorkSpace local file
        //
        public void InsertWorkSpaceLocalFile(string nxlName,
            string localPath,
            string folderId,
            DateTime lastModified,
            long size, string reserved1)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(WorkSpaceLocalFileDao.UPSERT_SQL(user_tb_pk,
                folderId,
                nxlName,
                localPath,
                lastModified,
                size, reserved1));
        }

        public WorkSpaceLocalFile[] ListAllWorkSpaceLocalFiles()
        {
            int user_tb_pk = User_Primary_Key;
            List<WorkSpaceLocalFile> rt = new List<WorkSpaceLocalFile>();
            ExecuteReader(WorkSpaceLocalFileDao.Query_SQL(user_tb_pk), (reader) =>
            {
                while (reader.Read())
                {
                    rt.Add(WorkSpaceLocalFile.NewByReader(reader));
                }
            });

            return rt.ToArray();
        }

        public WorkSpaceLocalFile[] ListWorkSpaceLocalFiles(string folderId)
        {
            int user_tb_pk = User_Primary_Key;
            List<WorkSpaceLocalFile> rt = new List<WorkSpaceLocalFile>();
            ExecuteReader(WorkSpaceLocalFileDao.Query_SQL(user_tb_pk, folderId), (reader) =>
            {
                while (reader.Read())
                {
                    rt.Add(WorkSpaceLocalFile.NewByReader(reader));
                }
            });

            return rt.ToArray();
        }

        public void UpdateWorkSpaceLocalFileStatus(int id, int status)
        {
            ExecuteNonQuery(WorkSpaceLocalFileDao.Update_NXL_Status_SQL(id, status));
        }

        public void UpdateWorkSpaceLocalFileOriginalPath(int id, string originalPath)
        {
            ExecuteNonQuery(WorkSpaceLocalFileDao.Update_Original_Path_SQL(id, originalPath));
        }

        public void UpdateWorkSpaceLocalFileName(int id, string name)
        {
            ExecuteNonQuery(WorkSpaceLocalFileDao.Update_File_Name_SQL(id, name));
        }

        public void UpdateWorkSpaceLocalFilePath(int id, string path)
        {
            ExecuteNonQuery(WorkSpaceLocalFileDao.Update_File_LocalPath_SQL(id, path));
        }

        public void UpdateWorkSpaceLocalFileReserved1(int id, string reserved1)
        {
            ExecuteNonQuery(WorkSpaceLocalFileDao.Update_File_Reserved1_SQL(id, reserved1));
        }

        public void DeleteWorkSpaceLocalFile(int tablePk)
        {
            ExecuteNonQuery(WorkSpaceLocalFileDao.Delete_Item_SQL(tablePk));
        }

        #endregion // WorkSpace

        #region MyVault
        public struct InsertMyVaultFile
        {
            public string path_id;
            public string display_path;
            public string repo_id;
            public string duid;
            public string nxl_name;
            public Int64 last_modified_time;
            public Int64 creation_time;
            public Int64 shared_time;
            public string shared_with_list;
            public Int64 size;
            public bool is_deleted;
            public bool is_revoked;
            public bool is_shared;
            public string source_repo_type;
            public string source_file_display_path;
            public string source_file_path_id;
            public string source_repo_name;
            public string source_repo_id;
        }
        public void UpsertMyVaultFileBatch(InsertMyVaultFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }
            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //stopwatch.Start();
            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                    MyVaultFileDao.UPSERT_SQL(User_Primary_Key,
                    i.path_id, i.display_path, i.repo_id, i.duid,
                    i.nxl_name, i.last_modified_time, i.creation_time, i.shared_time,
                    i.shared_with_list, i.size,
                    i.is_deleted, i.is_revoked, i.is_shared,
                    i.source_repo_type, i.source_file_display_path,
                    i.source_file_path_id, i.source_repo_name, i.source_repo_id)
                    );
            }
            ExecuteNonQueryBatch(sqls.ToArray());

            //stopwatch.Stop();
            //Console.WriteLine("UpsertMyVaultFileBatch time: {0} mills", stopwatch.Elapsed.TotalMilliseconds);
        }

        public void UpsertMyVaultFile(string path_id,
            string display_path, string repo_id, string duid, string nxl_name,
            Int64 last_modified_time, Int64 creation_time, Int64 shared_time,
            string shared_with_list, Int64 size,
            bool is_deleted, bool is_revoked, bool is_shared,
            string source_repo_type, string source_file_display_path,
            string source_file_path_id, string source_repo_name,
            string source_repo_id)
        {
            ExecuteNonQuery(MyVaultFileDao.UPSERT_SQL(
                User_Primary_Key,
                path_id, display_path, repo_id, duid, nxl_name,
                last_modified_time, creation_time, shared_time,
                shared_with_list, size,
                is_deleted, is_revoked, is_shared,
                source_repo_type, source_file_display_path,
                source_file_path_id, source_repo_name, source_repo_id));
        }

        public MyVaultFile[] ListMyVaultFile()
        {
            int user_tb_pk = User_Primary_Key;
            List<MyVaultFile> rt = new List<MyVaultFile>();
            ExecuteReader(MyVaultFileDao.Query_SQL(user_tb_pk), (reader) =>
            {
                while (reader.Read())
                {
                    MyVaultFile item = MyVaultFile.NewByReader(reader);
                    rt.Add(item);
                }
            });
            return rt.ToArray();
        }

        public MyVaultFile[] ListMyVaultOfflineFile()
        {
            int user_tb_pk = User_Primary_Key;
            List<MyVaultFile> rt = new List<MyVaultFile>();
            ExecuteReader(MyVaultFileDao.Query_Offline_SQL(user_tb_pk), (reader) =>
            {
                while (reader.Read())
                {
                    MyVaultFile item = MyVaultFile.NewByReader(reader);
                    rt.Add(item);
                }
            });
            return rt.ToArray();
        }


        public void InsertMyVaultLocalFile(string nxl_name,
                    string nxl_local_path,
                    DateTime last_modified_time,
                    string shared_with_list,
                    long size,
                    int status,
                    string comment,
                    string originalPath, string reserved1)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultLocalFileDao.UPSET_SQL(user_tb_pk,
                nxl_name, nxl_local_path,
                last_modified_time,
                shared_with_list, size,
                status, comment, originalPath, reserved1));
        }

        public MyVaultLocalFile[] ListMyVaultLocalFile()
        {
            int user_tb_pk = User_Primary_Key;
            List<MyVaultLocalFile> rt = new List<MyVaultLocalFile>();
            ExecuteReader(MyVaultLocalFileDao.Query_SQL(user_tb_pk), (reader) =>
            {
                while (reader.Read())
                {
                    rt.Add(MyVaultLocalFile.NewByReader(reader));
                }
            });

            return rt.ToArray();
        }

        public void UpdateMyVaultFile_IsOverwriteFromLocal(int table_pk, int value)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultFileDao.Update_Resereved1_SQL(table_pk, Convert.ToString(value)));
        }

        public void UpdateMyVaultFileWhenOverwriteInLeaveCopy(int table_pk, int status, long size, DateTime lastModifed)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultFileDao.Update_NXL_For_Overwrite_LeaveCopyModel(table_pk, status, size, lastModifed));
        }

        public void UpdateMyVaultFileStatus(int table_pk, int status)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultFileDao.Update_NXL_Status_SQL(table_pk, status));
        }

        public void UpdateMyVaultFileOffline(int table_pk, bool is_offline)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultFileDao.Update_Offline_Mark_SQL(table_pk, is_offline));
        }

        public void UpdateMyVaultFileShareStatus(int table_pk, bool is_Shared)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultFileDao.Update_Shared_Mark_SQL(table_pk, is_Shared));
        }

        public void UpdateMyVaultFileLocalPath(int table_pk, string nxl_local_path)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultFileDao.Update_Nxl_Local_Path(table_pk, nxl_local_path));
        }

        public void UpdateMyVaultFileSharedWithList(int table_pk, string new_List)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultFileDao.Update_Nxl_SharedWithList(table_pk, new_List));
        }

        public void UpdateMyVaultFileEditStatus(int myVaultfileTablePk, int newStatus)
        {
            ExecuteNonQuery(MyVaultFileDao.Update_EditStatus_SQL(myVaultfileTablePk, newStatus));
        }

        public void UpdateMyVaultFileModifyRightsStatus(int myVaultfileTablePk, int newStatus)
        {
            ExecuteNonQuery(MyVaultFileDao.Update_ModifiRights_Status_SQL(myVaultfileTablePk, newStatus));
        }

        public void DeleteMyVaultFile(string RmsName)
        {
            ExecuteNonQuery(MyVaultFileDao.Delete_Item_SQL(User_Primary_Key, RmsName));
        }

        public void UpdateMyVaultLocalFileStatus(string nxl_name, int status)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultLocalFileDao.Update_NXL_Status_SQL(user_tb_pk, nxl_name, status));
        }

        //public void UpdateMyVaultLocalFileStatusModifiedTime(string nxl_name,DateTime status_modified_time)
        //{
        //    int user_tb_pk = User_Primary_Key;
        //    ExecuteNonQuery(MyVaultLocalFileDao.Update_NXL_Status_Modified_Time(user_tb_pk, nxl_name, status_modified_time));
        //}

        public string GetMyVaultLocalFileSharedWithList(string nxl_name)
        {

            int user_tb_pk = User_Primary_Key;
            string rt = "";
            ExecuteReader(MyVaultLocalFileDao.Query_SQL(user_tb_pk, nxl_name), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["shared_with_list"].ToString();
                }
            });
            return rt;
        }

        public void UpdateMyVaultLocalFileSharedWithList(string nxl_name, string shared_with_list)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Update_Shared_With_SQL(User_Primary_Key, nxl_name, shared_with_list));
        }

        public void UpdateMyVaultLocalFileOriginalPath(string nxl_name, string originalPath)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Update_Original_Path_SQL(User_Primary_Key, nxl_name, originalPath));
        }

        public void UpdateMyVaultLocalFileName(int id, string name)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Update_File_Name_SQL(id, name));
        }

        public void UpdateMyVaultLocalFilePath(int id, string path)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Update_File_LocalPath_SQL(id, path));
        }

        public void UpdateMyVaultLocalFileReserved1(int id, string reserved)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Update_File_Reserved1_SQL(id, reserved));
        }

        public void DeleteMyVaultLocalFile(string nxl_name)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Delete_Item_SQL(User_Primary_Key, nxl_name));
        }
        #endregion

        #region MyDrive

        #region MyDriveFile
        public struct InsertMyDriveFile
        {
            public string pathId;
            public string pathDisplay;
            public string name;
            public Int64 size;
            public Int64 lastModified;
            public Int32 isFolder;
        }

        public void UpsertMyDriveFileBatch(InsertMyDriveFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }

            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                        MyDriveFileDao.UPSERT_SQL(User_Primary_Key,
                        i.pathId, i.pathDisplay, i.name, "", i.lastModified, i.size, i.isFolder)
                    );
            }
            ExecuteNonQueryBatch(sqls.ToArray());

        }

        public bool InsertMyDriveFakedRoot()
        {
            try
            {
                SqliteOpenHelper.ExecuteNonQuery(DataBaseConnectionString,
                    MyDriveFileDao.InsertFakedRoot_SQL(User_Primary_Key));
            }
            catch (SQLiteException e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
                return false;
            }

            return true;
        }

        public MyDriveFile[] ListAllMyDriveFiles()
        {
            int user_tb_pk = User_Primary_Key;
            List<MyDriveFile> rt = new List<MyDriveFile>();
            ExecuteReader(MyDriveFileDao.Query_SQL(user_tb_pk, "/"), (reader) =>
            {
                while (reader.Read())
                {
                    MyDriveFile item = MyDriveFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public MyDriveFile[] ListMyDriveFiles(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if (path.Length > 1 && !path.EndsWith("/"))
            {
                path += '/';
            }

            var rt = new List<MyDriveFile>();
            int user_tb_pk = User_Primary_Key;
            ExecuteReader(MyDriveFileDao.Query_SQL(user_tb_pk, path), (reader) =>
            {
                while (reader.Read())
                {
                    MyDriveFile item = MyDriveFile.NewByReader(reader);
                    // filter by Rms_path_id, only return first level items, and will filter fake root
                    if (FileHelper.IsDirectChild(item.RmsPathId, path))
                    {
                        rt.Add(item);
                    }
                }
            });

            return rt.ToArray();
        }

        public string GetMyDriveLocalFileParentFolderDisplayPath(int myDriveFile_pk)
        {
            int user_tb_pk = User_Primary_Key;
            string rt = "/";
            ExecuteReader(MyDriveFileDao.Query_DisplayPath_SQL(user_tb_pk, myDriveFile_pk), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["rms_display_path"].ToString();
                }
            });

            return rt;
        }

        public void UpdateMyDriveFileStatus(int table_pk, int status)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyDriveFileDao.Update_File_Status_SQL(table_pk, status));
        }

        public void UpdateMyDriveFileOffline(int table_pk, bool is_offline)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyDriveFileDao.Update_Offline_Mark_SQL(table_pk, is_offline));
        }

        public void UpdateMyDriveFileLocalPath(int table_pk, string localPath)
        {
            ExecuteNonQuery(MyDriveFileDao.Update_Nxl_Local_Path(table_pk, localPath));
        }

        public void DeleteMyDriveFile(string rmsPathId)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyDriveFileDao.Delete_Item_SQL(User_Primary_Key, rmsPathId));
        }

        public void DeleteMyDriveFolderAndSubChildren(string rmsPathId)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyDriveFileDao.Delete_Folder_And_SubChildren_SQL(User_Primary_Key, rmsPathId));
        }
        #endregion

        #region MyDriveLocalFile
        public void InsertMyDriveLocalFile(string nxlName,
            string localPath,
            string folderId,
            DateTime lastModified,
            long size, string reserved1)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyDriveLocalFileDao.UPSET_SQL(user_tb_pk,
                folderId,
                nxlName,
                localPath,
                lastModified,
                size, reserved1));
        }

        public MyDriveLocalFile[] ListAllMyDriveLocalFiles()
        {
            int user_tb_pk = User_Primary_Key;
            List<MyDriveLocalFile> rt = new List<MyDriveLocalFile>();
            ExecuteReader(MyDriveLocalFileDao.Query_SQL(user_tb_pk), (reader) =>
            {
                while (reader.Read())
                {
                    rt.Add(MyDriveLocalFile.NewByReader(reader));
                }
            });

            return rt.ToArray();
        }

        public MyDriveLocalFile[] ListMyDriveLocalFiles(string folderId)
        {
            int user_tb_pk = User_Primary_Key;
            List<MyDriveLocalFile> rt = new List<MyDriveLocalFile>();
            ExecuteReader(MyDriveLocalFileDao.Query_SQL(user_tb_pk, folderId), (reader) =>
            {
                while (reader.Read())
                {
                    rt.Add(MyDriveLocalFile.NewByReader(reader));
                }
            });

            return rt.ToArray();
        }

        public void UpdateMyDriveLocalFileStatus(int id, int status)
        {
            ExecuteNonQuery(MyDriveLocalFileDao.Update_File_Status_SQL(id, status));
        }

        public void UpdateMyDriveLocalFileName(int id, string name)
        {
            ExecuteNonQuery(MyDriveLocalFileDao.Update_File_Name_SQL(id, name));
        }

        public void UpdateMyDriveLocalFilePath(int id, string path)
        {
            ExecuteNonQuery(MyDriveLocalFileDao.Update_File_LocalPath_SQL(id, path));
        }

        public void UpdateMyDriveLocalFileReserved1(int id, string reserved1)
        {
            ExecuteNonQuery(MyDriveLocalFileDao.Update_File_Reserved1_SQL(id, reserved1));
        }

        public void DeleteMyDriveLocalFile(int tablePk)
        {
            ExecuteNonQuery(MyDriveLocalFileDao.Delete_Item_SQL(tablePk));
        }
        #endregion

        #endregion // MyDrive

        #region SystemBucket
        public void UpsertSystemBucket(
            int rms_systembucket_id,
            string rms_systembucket_tenant_id,
            string rms_systembucket_classification,
            bool rms_isEnableAdhoc
            )
        {
            int user_table_pk = User_Primary_Key;
            ExecuteNonQuery(SystemBucketDao.Upsert_SQL(user_table_pk, rms_systembucket_id, rms_systembucket_tenant_id, rms_systembucket_classification, rms_isEnableAdhoc));
        }

        public SystemBucket GetSystemBucket()
        {
            int user_table_pk = User_Primary_Key;
            SystemBucket rt = null;
            ExecuteReader(SystemBucketDao.Query_SQL(user_table_pk), (reader) =>
             {
                 if (reader.Read())
                 {
                     rt = SystemBucket.NewByReader(reader);
                 }
             });
            if (rt == null)
            {
                throw new Exception("Critical Error,Get user  system butcket failed");
            }
            return rt;
        }

        #endregion

        #region External repository

        #region Rms external repository table
        public struct InsertRepoInfo
        {
            public int isShared;
            public int isDefault;
            // 
            public long createTime;
            public long updateTime;
            // 
            public string repoid;
            public string name;
            public string type;
            public string providerClass; // New added
            public string accountName;
            public string accountId;
            public string token;
            public string preference;
        }
        public void UpsertRepoInfoBatch(InsertRepoInfo[] infos)
        {
            if (infos.Length == 0)
            {
                return;
            }

            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in infos)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                        RmsExternalRepoDao.Upsert_SQL(User_Primary_Key, i.repoid, i.name,
                        i.type, i.isShared, i.isDefault, i.accountName, i.accountId, i.token,
                        i.createTime, i.updateTime, i.preference, i.providerClass)
                    );
            }

            ExecuteNonQueryBatch(sqls.ToArray());
        }

        // Mainly used for add repository from local(Reserved)
        public void UpsertRepoInfo(InsertRepoInfo i)
        {
            int pk = -1;
            ExecuteReader(RmsExternalRepoDao.Upsert_SQL_And_Query(User_Primary_Key, i.repoid, i.name,
                    i.type, i.isShared, i.isDefault, i.accountName, i.accountId, i.token,
                    i.createTime, i.updateTime, i.preference, i.providerClass), (reader) =>
                    {
                        //Get the new insert item.
                        if (reader.Read())
                        {
                            pk = int.Parse(reader[0].ToString());
                        }

                        if (pk != -1)
                        {
                            if (RmsExternalRepo_Id2PK.ContainsKey(i.repoid))
                            {
                                RmsExternalRepo_Id2PK[i.repoid] = pk;
                            }
                            else
                            {
                                RmsExternalRepo_Id2PK.Add(i.repoid, pk);
                            }
                        }
                    });
        }
        public RmsExternalRepo[] ListRepositories()
        {
            int user_tb_pk = User_Primary_Key;
            List<RmsExternalRepo> rt = new List<RmsExternalRepo>();
            ExecuteReader(RmsExternalRepoDao.Query_SQL(user_tb_pk), (reader) =>
            {
                while (reader.Read())
                {
                    var item = RmsExternalRepo.NewByReader(reader);
                    rt.Add(item);
                    // 
                    int pk = int.Parse(reader[0].ToString());
                    string repoId = reader["rms_repo_id"].ToString();
                    if (RmsExternalRepo_Id2PK.ContainsKey(repoId))
                    {
                        RmsExternalRepo_Id2PK[repoId] = pk;
                    }
                    else
                    {
                        RmsExternalRepo_Id2PK.Add(repoId, pk);
                    }
                }
            });

            return rt.ToArray();
        }
        public void DeleteRepository(string repoid)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(RmsExternalRepoDao.Delete_SQL(user_tb_pk, repoid));
        }
        public string QueryToken(string repoid)
        {
            string rt = "";
            int user_tb_pk = User_Primary_Key;
            ExecuteReader(RmsExternalRepoDao.Query_Token_SQL(user_tb_pk, repoid), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["rms_token"].ToString();
                }
            });
            return rt;
        }
        public void UpdateRepoName(int table_pk, string name)
        {
            ExecuteNonQuery(RmsExternalRepoDao.Update_Repo_Name(table_pk, name));
        }
        // Update by table pk
        public void UpdateRepoToken(int table_pk, string token)
        {
            ExecuteNonQuery(RmsExternalRepoDao.Update_Token(table_pk, token));
        }
        // Update by repo id
        public void UpdateRepoToken(string repoId, string token)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(RmsExternalRepoDao.Update_Token_By_RepoId(user_tb_pk, repoId, token));
        }

        public string QueryExternalRepoReserved1(string repoid)
        {
            string rt = "";
            int user_tb_pk = User_Primary_Key;
            ExecuteReader(RmsExternalRepoDao.Query_Reserved1(user_tb_pk, repoid), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["reserved1"].ToString();
                }
            });
            return rt;
        }

        public string QueryExternalRepoReserved2(string repoid)
        {
            string rt = "";
            int user_tb_pk = User_Primary_Key;
            ExecuteReader(RmsExternalRepoDao.Query_Reserved2(user_tb_pk, repoid), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["reserved2"].ToString();
                }
            });
            return rt;
        }

        public string QueryExternalRepoReserved3(string repoid)
        {
            string rt = "";
            int user_tb_pk = User_Primary_Key;
            ExecuteReader(RmsExternalRepoDao.Query_Reserved3(user_tb_pk, repoid), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["reserved3"].ToString();
                }
            });
            return rt;
        }

        #endregion // Rms external repository table

        public struct InsertExternalDriveFile
        {
            public string repoId;
            public string fileId;
            public int isFolder;
            public string name;
            public long size;
            public long modifiedTime;
            public string displaypath;
            public string cloudPathid;
            public int isNxlFile; // extend
        }
        private bool IsRmsExternalRepoIdMapContainsKey(string key)
        {
            return RmsExternalRepo_Id2PK != null && RmsExternalRepo_Id2PK.ContainsKey(key);
        }

        #region GoogleDrive

        #region Google drive file
       
        public ExternalDriveFile[] ListGoogleDriveAllFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveFile[0];
            }

            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            ExecuteReader(GooglDriveFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], "/"), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public ExternalDriveFile[] ListGoogleDriveFile(string repoId, string path)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveFile[0];
            }

            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if (path.Length > 1 && !path.EndsWith("/"))
            {
                path += "/";
            }

            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            ExecuteReader(GooglDriveFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], path), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveFile.NewByReader(reader);

                    // filter by cloud_path_id, only return first level items
                    if (FileHelper.IsDirectChild(item.CloudPathId, path))
                    {
                        rt.Add(item);
                    }
                }
            });

            return rt.ToArray();
        }

        public ExternalDriveFile[] ListGoogleDriveOfflineFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveFile[0];
            }

            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            ExecuteReader(GooglDriveFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], "/"), (reader) =>
            {
                while (reader.Read())
                {
                    var f = ExternalDriveFile.NewByReader(reader);
                    if (!f.IsFolder && (f.IsOffline || f.Status == 1)) // 1 -- AvailableOffline
                    {
                        rt.Add(f);
                    }
                }
            });

            return rt.ToArray();
        }

        public void UpsertGoogleDriveFileBatchEx(InsertExternalDriveFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }

            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                        GooglDriveFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[i.repoId],
                        i.fileId, i.isFolder, i.name, i.size, i.modifiedTime, i.displaypath, i.cloudPathid, i.isNxlFile)
                    );
            }
            ExecuteNonQueryBatch(sqls.ToArray());
        }

        // Insert faked root for Googledrive that make added local file into googledrive root folder.
        // Should be invoked in SyncFile function.
        public bool InsertGoogleDriveFakedRoot(string repoid)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                return false;
            }
            try
            {
                SqliteOpenHelper.ExecuteNonQuery(DataBaseConnectionString,
                    GooglDriveFileDao.InsertFakedRoot_SQL(RmsExternalRepo_Id2PK[repoid]));
            }
            catch (SQLiteException e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
                return false;
            }

            return true;
        }

        public string GetGoogleDriveFileCloudPathId(string repoid, int googledriveFile_pk)
        {
            string rt = "/";
            if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                return rt;
            }
            ExecuteReader(GooglDriveFileDao.Query_CloudPathId_SQL(RmsExternalRepo_Id2PK[repoid],
                googledriveFile_pk), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["ser_cloud_pathid"].ToString();
                }
            });

            return rt;
        }

        public string GetGoogleDriveFileDisplayPath(string repoid, string cloudPathid)
        {
            string rt = string.Empty;
            ExecuteReader(GooglDriveFileDao.Query_DisplayPath_By_PathId(RmsExternalRepo_Id2PK[repoid], cloudPathid), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["ser_display_path"].ToString();
                }
            });
            return rt;
        }

        public void UpdateGoogleDriveFileStatus(int table_pk, int status)
        {
            ExecuteNonQuery(GooglDriveFileDao.Update_FileStatus_SQL(table_pk, status));
        }
        public void UpdateGoogleDriveFileOffline(int table_pk, bool is_offline)
        {
            ExecuteNonQuery(GooglDriveFileDao.Update_IsOffline_SQL(table_pk, is_offline));
        }
        public void UpdateGoogleDriveFileLocalPath(int table_pk, string localPath)
        {
            ExecuteNonQuery(GooglDriveFileDao.Update_LocalPath_SQL(table_pk, localPath));
        }

        public void DeleteGoogleDriveFile(string repoid, string fileId)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                ExecuteNonQuery(GooglDriveFileDao.Delete_File_SQL(RmsExternalRepo_Id2PK[repoid], fileId));
            }
        }

        public void DeleteGoogleDriveFolderAndAllSubFiles(string repoId, string cloudPathid)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                ExecuteNonQuery(GooglDriveFileDao.Delete_Folder_And_SubChildren_SQL(RmsExternalRepo_Id2PK[repoId], cloudPathid));
            }
        }
        #endregion // Google drive file

        #region Google drive local file
        public ExternalDriveLocalFile[] ListGoogleDriveLocalFile(string repoId, string cloudPathid)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveLocalFile[0];
            }

            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            ExecuteReader(GoogleDriveLocalFileDao.Query_SQL_Under_TargetFolder(RmsExternalRepo_Id2PK[repoId], cloudPathid), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }
        public ExternalDriveLocalFile[] ListGoogleDriveAllLocalFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveLocalFile[0];
            }

            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            ExecuteReader(GoogleDriveLocalFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public void InertLocalFileToGoogleDrive(string repoid, string folderId, string name,
             string localPath, int size, DateTime lastModified)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                ExecuteNonQuery(GoogleDriveLocalFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[repoid],
                    folderId,
                    name,
                    localPath,
                    size,
                    lastModified));
            }
        }

        public string QueryGoogleDriveLocalFileDisplayPath(string repoid, int googledriveFile_pk)
        {
            string rt = "/";
            if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                return rt;
            }
            ExecuteReader(GooglDriveFileDao.Query_DisplayPath_SQL(RmsExternalRepo_Id2PK[repoid],
                googledriveFile_pk), (reader) =>
                {
                    if (reader.Read())
                    {
                        rt = reader["ser_display_path"].ToString();
                    }
                });

            return rt;
        }

        public void UpdateGoogleDriveLocalFileStatus(int table_pk, int status)
        {
            ExecuteNonQuery(GoogleDriveLocalFileDao.Update_FileStatus_SQL(table_pk, status));
        }
        public void DeleteGoogleDriveLocalFile(int tablePk)
        {
            ExecuteNonQuery(GoogleDriveLocalFileDao.Delete_Item_SQL(tablePk));
        }
        #endregion // Google drive local file

        #endregion // GoogleDrive


        #region DropBoxDrive

        public bool InsertDropBoxFakedRoot(string repoid)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                return false;
            }
            try
            {
                SqliteOpenHelper.ExecuteNonQuery(DataBaseConnectionString,
                    DropBoxFileDao.InsertFakedRoot_SQL(RmsExternalRepo_Id2PK[repoid]));
            }
            catch (SQLiteException e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
                return false;
            }

            return true;
        }

        public ExternalDriveFile[] ListDropBoxFile(string repoId, string path)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveFile[0];
            }

            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if (path.Length > 1 && !path.EndsWith("/"))
            {
                path += "/";
            }

            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            ExecuteReader(DropBoxFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], path), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveFile.NewByReader(reader);

                    // filter by cloud_path_id, only return first level items
                    if (FileHelper.IsDirectChild(item.CloudPathId, path))
                    {
                        rt.Add(item);
                    }
                }
            });

            return rt.ToArray();
        }

        public ExternalDriveLocalFile[] ListDropBoxAllLocalFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveLocalFile[0];
            }

            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            ExecuteReader(DropBoxLocalFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public ExternalDriveLocalFile[] ListDropBoxLocalFile(string repoId, string cloudPathid)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveLocalFile[0];
            }

            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            ExecuteReader(DropBoxLocalFileDao.Query_SQL_Under_TargetFolder(RmsExternalRepo_Id2PK[repoId], cloudPathid), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }


        public ExternalDriveFile[] ListDropBoxOfflineFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveFile[0];
            }

            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            ExecuteReader(DropBoxFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], "/"), (reader) =>
            {
                while (reader.Read())
                {
                    var f = ExternalDriveFile.NewByReader(reader);
                    if (!f.IsFolder && f.IsOffline)
                    {
                        rt.Add(f);
                    }
                }
            });

            return rt.ToArray();
        }

        public void InsertLocalFileToDropBox(string repoid, string folderId, string name,
                    string localPath, int size, DateTime lastModified)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                ExecuteNonQuery(DropBoxLocalFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[repoid],
                    folderId,
                    name,
                    localPath,
                    size,
                    lastModified));
            }
        }

        public void UpsertDropBoxFileBatchEx(InsertExternalDriveFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }

            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                        DropBoxFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[i.repoId],
                        i.fileId, i.isFolder, i.name, i.size, i.modifiedTime, i.displaypath, i.cloudPathid, i.isNxlFile)
                    );
            }
            ExecuteNonQueryBatch(sqls.ToArray());
        }


        public void DeleteDropBoxFile(string repoid, string fileId)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                ExecuteNonQuery(DropBoxFileDao.Delete_File_SQL(RmsExternalRepo_Id2PK[repoid], fileId));
            }
        }

        public void DeleteDropBoxFolderAndAllSubFiles(string repoId, string displayPath)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                ExecuteNonQuery(DropBoxFileDao.Delete_Folder_And_SubChildren_SQL(RmsExternalRepo_Id2PK[repoId], displayPath));
            }
        }

        public void DeleteDropBoxLocalFile(int tablePk)
        {
            ExecuteNonQuery(DropBoxLocalFileDao.Delete_Item_SQL(tablePk));
        }

        public void UpdateDropBoxFileStatus(int table_pk, int status)
        {
            ExecuteNonQuery(DropBoxFileDao.Update_FileStatus_SQL(table_pk, status));
        }
        public void UpdateDropBoxFileLocalPath(int table_pk, string localPath)
        {
            ExecuteNonQuery(DropBoxFileDao.Update_LocalPath_SQL(table_pk, localPath));
        }

        public void UpdateDropBoxFileOffline(int table_pk, bool is_offline)
        {
            ExecuteNonQuery(DropBoxFileDao.Update_IsOffline_SQL(table_pk, is_offline));
        }

        public void UpdateDropBoxLocalFileStatus(int table_pk, int status)
        {
            // todo: osmond
            //ExecuteNonQuery(GoogleDriveLocalFileDao.Update_FileStatus_SQL(table_pk, status));
        }

        public string GetDropBoxFileCloudFileId(string repoid, int dropboxfile_pk)
        {
            string rt = "/";
            if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                return rt;
            }
            ExecuteReader(DropBoxFileDao.Query_CloudFileId_SQL(RmsExternalRepo_Id2PK[repoid],
                dropboxfile_pk), (reader) =>
                {
                    if (reader.Read())
                    {
                        rt = reader["ser_file_id"].ToString();
                    }
                });

            return rt;
        }

        public string QueryDropBoxLocalFileRMSParentFolder(string repoid, int googledriveFile_RowNumber)
        {
            string rt = "/";
            //if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            //{
            //    return rt;
            //}
            //ExecuteReader(GooglDriveFileDao.Query_DisplayPath_SQL(RmsExternalRepo_Id2PK[repoid],
            //    googledriveFile_RowNumber), (reader) =>
            //    {
            //        if (reader.Read())
            //        {
            //            rt = reader["ser_display_path"].ToString();
            //        }
            //    });
            return rt;
        }

        #endregion

        #region BoxDrive

        public bool InsertBoxFakedRoot(string repoid)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                return false;
            }
            try
            {
                SqliteOpenHelper.ExecuteNonQuery(DataBaseConnectionString,
                    BoxFileDao.InsertFakedRoot_SQL(RmsExternalRepo_Id2PK[repoid]));
            }
            catch (SQLiteException e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
                return false;
            }

            return true;
        }

        public ExternalDriveFile[] ListBoxFile(string repoId, string cloudPathId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveFile[0];
            }

            if (string.IsNullOrEmpty(cloudPathId))
            {
                cloudPathId = "/";
            }
            if (cloudPathId.Length > 1 && !cloudPathId.EndsWith("/"))
            {
                cloudPathId += "/";
            }

            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            ExecuteReader(BoxFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], cloudPathId), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveFile.NewByReader(reader);

                    // filter by cloud_path_id, only return first level items
                    if (FileHelper.IsDirectChild(item.CloudPathId, cloudPathId))
                    {
                        rt.Add(item);
                    }
                }
            });

            return rt.ToArray();
        }


        public ExternalDriveLocalFile[] ListBoxAllLocalFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveLocalFile[0];
            }

            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            ExecuteReader(BoxLocalFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public ExternalDriveLocalFile[] ListBoxLocalFile(string repoId, string cloudPathid)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveLocalFile[0];
            }

            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            ExecuteReader(BoxLocalFileDao.Query_SQL_Under_TargetFolder(RmsExternalRepo_Id2PK[repoId], cloudPathid), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }


        public ExternalDriveFile[] ListBoxOfflineFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveFile[0];
            }

            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            ExecuteReader(BoxFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], "/"), (reader) =>
            {
                while (reader.Read())
                {
                    var f = ExternalDriveFile.NewByReader(reader);
                    if (!f.IsFolder && f.IsOffline)
                    {
                        rt.Add(f);
                    }
                }
            });

            return rt.ToArray();
        }

        public void InsertLocalFileToBox(string repoid, string folderId, string name,
                    string localPath, int size, DateTime lastModified)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                ExecuteNonQuery(BoxLocalFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[repoid],
                    folderId,
                    name,
                    localPath,
                    size,
                    lastModified));
            }
        }


        public void UpsertBoxFileBatchEx(InsertExternalDriveFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }

            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                        BoxFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[i.repoId],
                        i.fileId, i.isFolder, i.name, i.size, i.modifiedTime, i.displaypath, i.cloudPathid, i.isNxlFile)
                    );
            }
            ExecuteNonQueryBatch(sqls.ToArray());
        }

        public string GetBoxCloudDispalyPathByFileId(string repoId, string fileId)
        {
            string rt = string.Empty;
            ExecuteReader(BoxFileDao.Query_CloudDisplayPath(RmsExternalRepo_Id2PK[repoId], fileId), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader.GetString(0);
                }
            });
            return rt;
        }

        public string GetBoxCloudPathIdByFileId(string repoId, string fileId)
        {
            string rt = string.Empty;
            ExecuteReader(BoxFileDao.Query_CloudPathId(RmsExternalRepo_Id2PK[repoId], fileId), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader.GetString(0);
                }
            });
            return rt;
        }


        public void DeleteBoxFile(string repoid, string fileId)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                ExecuteNonQuery(BoxFileDao.Delete_File_SQL(RmsExternalRepo_Id2PK[repoid], fileId));
            }
        }

        public void DeleteBoxFolderAndAllSubFiles(string repoId, string displayPath)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                ExecuteNonQuery(BoxFileDao.Delete_Folder_And_SubChildren_SQL(RmsExternalRepo_Id2PK[repoId], displayPath));
            }
        }

        public void DeleteBoxLocalFile(int tablePk)
        {
            ExecuteNonQuery(BoxLocalFileDao.Delete_Item_SQL(tablePk));
        }

        public void UpdateBoxFileStatus(int table_pk, int status)
        {
            ExecuteNonQuery(BoxFileDao.Update_FileStatus_SQL(table_pk, status));
        }
        public void UpdateBoxFileLocalPath(int table_pk, string localPath)
        {
            ExecuteNonQuery(BoxFileDao.Update_LocalPath_SQL(table_pk, localPath));
        }

        public void UpdateBoxFileOffline(int table_pk, bool is_offline)
        {
            ExecuteNonQuery(BoxFileDao.Update_IsOffline_SQL(table_pk, is_offline));
        }


        public void UpdateBoxLocalFileStatus(int table_pk, int status)
        {
            // todo: osmond
            //ExecuteNonQuery(GoogleDriveLocalFileDao.Update_FileStatus_SQL(table_pk, status));
        }

        public string GetBoxFileCloudFileId(string repoid, int dropboxfile_pk)
        {
            string rt = "/";
            if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                return rt;
            }
            ExecuteReader(BoxFileDao.Query_CloudFileId_SQL(RmsExternalRepo_Id2PK[repoid],
                dropboxfile_pk), (reader) =>
                {
                    if (reader.Read())
                    {
                        rt = reader["ser_file_id"].ToString();
                    }
                });

            return rt;
        }


        public string QueryBoxLocalFileRMSParentFolder(string repoid, int googledriveFile_RowNumber)
        {
            string rt = "/";
            //if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            //{
            //    return rt;
            //}
            //ExecuteReader(GooglDriveFileDao.Query_DisplayPath_SQL(RmsExternalRepo_Id2PK[repoid],
            //    googledriveFile_RowNumber), (reader) =>
            //    {
            //        if (reader.Read())
            //        {
            //            rt = reader["ser_display_path"].ToString();
            //        }
            //    });
            return rt;
        }
        #endregion


        #region OneDrive

        public OneDrive.FolderItem GetOneDriveRootFolder(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            OneDrive.FolderItem rootFolderItem = null;
            ExecuteReader(OneDrive.OneDriveFileDao.Inner_Join_Query_OneDriveRootFolder_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                if (reader.Read())
                {
                    rootFolderItem = new OneDrive.FolderItem(reader);
                }
            });
            return rootFolderItem;
        }

        public bool UpdateOneDriveFolder(string repoId, OneDrive.FolderItem folderItem)
        {
            bool res = false;

            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            if(ExecuteNonQuery(OneDrive.OneDriveFoldersDao.Update_Folder_SQL(RmsExternalRepo_Id2PK[repoId], folderItem)) == 1)
            {
                res = true;
            }
            return res;
        }

        public bool UpdateOneDriveFileCommon(string repoId, OneDrive.ValueItem item)
        {
            bool res = false;

            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            if (ExecuteNonQuery(OneDrive.OneDriveFileCommonDao.Update_File_SQL(RmsExternalRepo_Id2PK[repoId], item)) == 1)
            {
                res = true;
            }
            return res;
        }


        public ExternalDriveFile[] ListOneDriveAllFilesForUI(string repoId)
        {
            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            ExecuteReader(OneDrive.OneDriveFileDao.Inner_Join_Query_All_File_UI_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                if (reader.Read())
                {
                    var item = ExternalDriveFile.AdapterOneDriveDataStructure(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public ExternalDriveFile[] ListOneDriveFileForUI(string repoId, string parentItemId)
        {
            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            if (string.Equals("/", parentItemId, StringComparison.CurrentCultureIgnoreCase))
            {
                parentItemId = string.Empty;
                ExecuteReader(OneDrive.OneDriveFoldersDao.Query_OneDriveRootFolder_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
                {
                    if (reader.Read())
                    {
                        parentItemId = reader["item_id"].ToString();
                    }
                });

                if (string.IsNullOrEmpty(parentItemId))
                {
                    return rt.ToArray();
                }
            }

            ExecuteReader(OneDrive.OneDriveFileDao.Inner_Join_Query_Children_File_UI_SQL(RmsExternalRepo_Id2PK[repoId], parentItemId),(reader)=> 
            {
                while (reader.Read())
                {
                    var item = ExternalDriveFile.AdapterOneDriveDataStructure(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public List<OneDrive.ValueItem> ListOneDriveFile(string repoId, string parentItemId)
        {
            List<OneDrive.ValueItem> rt = new List<OneDrive.ValueItem>();
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            if (string.Equals("/", parentItemId, StringComparison.CurrentCultureIgnoreCase))
            {
                parentItemId = string.Empty;
                ExecuteReader(OneDrive.OneDriveFoldersDao.Query_OneDriveRootFolder_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) => 
                {
                    if (reader.Read())
                    {
                        parentItemId = reader["item_id"].ToString();
                    }
                });

                if (string.IsNullOrEmpty(parentItemId))
                {
                    return rt;
                }
            }

            ExecuteReader(OneDrive.OneDriveFileDao.Inner_Join_Query_Children_Folder_SQL(RmsExternalRepo_Id2PK[repoId], parentItemId), (reader) =>
            {
                while (reader.Read())
                {
                    OneDrive.FolderItem folderItem = new OneDrive.FolderItem(reader);
                    rt.Add(folderItem);
                }
            });

            ExecuteReader(OneDrive.OneDriveFileDao.Inner_Join_Query_Children_File_SQL(RmsExternalRepo_Id2PK[repoId], parentItemId), (reader) =>
            {
                while (reader.Read())
                {
                    OneDrive.FileItem fileItem = new OneDrive.FileItem(reader);
                    rt.Add(fileItem);
                }
            });
            return rt;
        }

        public void DeleteOneDriveFile(string repoId, string itemId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }
         
            ExecuteNonQuery(OneDrive.OneDriveFileCommonDao.Delete_SQL(RmsExternalRepo_Id2PK[repoId], new List<string> { itemId }));
        }

        public void DeleteOneDriveFolderAndAllSubFiles(string repoId, string itemId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }
            List<string> allWaitDeleteFileItemIdList = new List<string>();
            List<string> allSubFileItemIdList = new List<string>();
            List<string> allWaitDeleteFolderItemIdList = new List<string>();
            GetAllChildrenFolder(repoId, itemId,ref allWaitDeleteFolderItemIdList);
            allWaitDeleteFolderItemIdList.Add(itemId);
            ExecuteReader(OneDrive.OneDriveFileCommonDao.Query_Children_SQL(RmsExternalRepo_Id2PK[repoId], allWaitDeleteFolderItemIdList, true), (reader) =>
            {
                while (reader.Read())
                {
                    allSubFileItemIdList.Add(reader["item_id"].ToString());
                }
            });
            allWaitDeleteFileItemIdList.AddRange(allWaitDeleteFolderItemIdList);
            allWaitDeleteFileItemIdList.AddRange(allSubFileItemIdList);
            ExecuteNonQuery(OneDrive.OneDriveFileCommonDao.Delete_SQL(RmsExternalRepo_Id2PK[repoId], allWaitDeleteFileItemIdList));
        }


        public void GetAllChildrenFolder(string repoId, string parentItemId, ref List<string> itemIds)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }
            List<string> childrenFolderItemIds = new List<string>();
            ExecuteReader(OneDrive.OneDriveFileCommonDao.Query_Children_folder_SQL(RmsExternalRepo_Id2PK[repoId], parentItemId), (reader) =>
            {
                while (reader.Read())
                {
                    childrenFolderItemIds.Add(reader["item_id"].ToString());
                }
            });

            itemIds.AddRange(childrenFolderItemIds);
       
            foreach (string folderItemId in childrenFolderItemIds)
            {
                GetAllChildrenFolder(repoId, folderItemId, ref itemIds);
            }
        }


        public void UpdateOneDriveFileStatus(int table_pk, int status)
        {
            ExecuteNonQuery(OneDrive.OneDriveFileLocalStatusDao.Update_Status_SQL(table_pk,status));
        }

        public void UpdateOneDriveFileStatus(string repoId, string item_id, int status)
        {
            ExecuteNonQuery(OneDrive.OneDriveFileLocalStatusDao.Update_Status_SQL(RmsExternalRepo_Id2PK[repoId], item_id, status));
        }

        public ExternalDriveLocalFile[] ListOneDriveAllLocalFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            ExecuteReader(OneDrive.OneDriveLocalFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId]),(reader)=> 
            {
                while (reader.Read())
                {
                    var item = ExternalDriveLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });
            return rt.ToArray();
        }

        public ExternalDriveLocalFile[] ListOneDriveLocalFile(string repoId, string parentItemId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            if (string.Equals("/", parentItemId, StringComparison.CurrentCultureIgnoreCase))
            {
                parentItemId = string.Empty;
                ExecuteReader(OneDrive.OneDriveFoldersDao.Query_OneDriveRootFolder_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
                {
                    if (reader.Read())
                    {
                        parentItemId = reader["item_id"].ToString();
                    }
                });

                if (string.IsNullOrEmpty(parentItemId))
                {
                    return rt.ToArray();
                }
            }

            ExecuteReader(OneDrive.OneDriveLocalFileDao.Query_SQL_Under_TargetFolder(RmsExternalRepo_Id2PK[repoId], parentItemId), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }


        public ExternalDriveFile[] ListOneDriveOfflineFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            List<ExternalDriveFile> rt = new List<ExternalDriveFile>();
      
            ExecuteReader(OneDrive.OneDriveFileDao.Inner_Join_Query_All_File_UI_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                while (reader.Read())
                {
                    var item = ExternalDriveFile.AdapterOneDriveDataStructure(reader);
                    if (!item.IsFolder && item.IsOffline)
                    {
                        rt.Add(item);
                    }
                }
            });

            return rt.ToArray();
        }

        public void InertLocalFileToOneDrive(string repoId, string parentItemId, string name, string localPath, int size, DateTime lastModified)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            if (string.Equals("/", parentItemId, StringComparison.CurrentCultureIgnoreCase))
            {
                parentItemId = string.Empty;
                ExecuteReader(OneDrive.OneDriveFoldersDao.Query_OneDriveRootFolder_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
                {
                    if (reader.Read())
                    {
                        parentItemId = reader["item_id"].ToString();
                    }
                });

                if (string.IsNullOrEmpty(parentItemId))
                {
                    return;
                }
            }
            ExecuteNonQuery(OneDrive.OneDriveLocalFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[repoId], parentItemId, name, localPath, size, lastModified));
        }

        public void InsertOneDriveFileBatchEx(string repoId, List<OneDrive.ValueItem> newFiles)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            foreach (OneDrive.ValueItem item in newFiles)
            {
                if (1 == ExecuteNonQuery(OneDrive.OneDriveFileCommonDao.Insert_SQL(RmsExternalRepo_Id2PK[repoId], item)))
                {
                    string table_pk = string.Empty;
                    ExecuteReader(OneDrive.OneDriveFileCommonDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], item.id), (reader) =>
                    {
                        if (reader.Read())
                        {
                            table_pk = reader["id"].ToString();
                        }
                    });

                    if (!string.IsNullOrEmpty(table_pk))
                    {
                        if (1 == item.isFolder)
                        {
                            ExecuteNonQuery(OneDrive.OneDriveFoldersDao.Insert_SQL(RmsExternalRepo_Id2PK[repoId], table_pk, item as OneDrive.FolderItem));
                        }
                        else if (0 == item.isFolder)
                        {
                            ExecuteNonQuery(OneDrive.OneDriveFilesDao.Insert_SQL(RmsExternalRepo_Id2PK[repoId], table_pk, item as OneDrive.FileItem));
                            ExecuteNonQuery(OneDrive.OneDriveFileLocalStatusDao.Insert_SQL(RmsExternalRepo_Id2PK[repoId], table_pk, item as OneDrive.FileItem));
                        }
                    }
                }
            }
        }

        public void UpdateOneDriveFileBatchEx(string repoId, List<OneDrive.ValueItem> updateFiles)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }

            foreach (OneDrive.ValueItem item in updateFiles)
            {
                ExecuteNonQuery(OneDrive.OneDriveFileCommonDao.Update_File_SQL(RmsExternalRepo_Id2PK[repoId], item));
                if (1 == item.isFolder)
                {
                    ExecuteNonQuery(OneDrive.OneDriveFoldersDao.Update_Folder_SQL(RmsExternalRepo_Id2PK[repoId], item as OneDrive.FolderItem));
                }
                else if (0 == item.isFolder)
                {
                    ExecuteNonQuery(OneDrive.OneDriveFilesDao.Update_SQL(RmsExternalRepo_Id2PK[repoId], item as OneDrive.FileItem));
                }
            }
        }

        public string GetOneDriveItemId(string repoId ,int fileCommomTablePk)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                throw new Exception();
            }
            string res = string.Empty;
            ExecuteReader(OneDrive.OneDriveFileCommonDao.Query_By_Table_PK_SQL(RmsExternalRepo_Id2PK[repoId], fileCommomTablePk), (reader) => 
            {
                if (reader.Read())
                {
                    res = reader["item_id"].ToString();
                }
            });
            return res;
        }

        public void DeleteOneDriveLocalFile(int tablePk)
        {
            ExecuteNonQuery(OneDrive.OneDriveLocalFileDao.Delete_Item_SQL(tablePk));
        }

        public void UpdateOneDriveLocalFileStatus(int table_pk, int status)
        {
            ExecuteNonQuery(OneDrive.OneDriveLocalFileDao.Update_FileStatus_SQL(table_pk, status));
        }

        public void UpdateOneDriveFileLocalPath(string repoId, string itemId, string filePath)
        {
            ExecuteNonQuery(OneDrive.OneDriveFileLocalStatusDao.Update_File_Local_Path_SQL(RmsExternalRepo_Id2PK[repoId], itemId, filePath));
        }

        public void UpdateOneDriveFileLocalStatusIsOffline(string repoId, string itemId, bool offline)
        {
            ExecuteNonQuery(OneDrive.OneDriveFileLocalStatusDao.Update_File_Local_Is_Offline_SQL(RmsExternalRepo_Id2PK[repoId], itemId, offline));
        }

        #endregion


        #region SharePoint

        #region SharePointFile
        private readonly object mSharePointDriveFileLock = new object();
        public bool InsertSharePointDriveFileRoot(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return false;
            }
            try
            {
                lock (mSharePointDriveFileLock)
                {
                    SqliteOpenHelper.ExecuteNonQuery(DataBaseConnectionString,
                    SharePointFileDao.InsertFakedRoot_SQL(RmsExternalRepo_Id2PK[repoId]));
                }
                return true;
            }
            catch (SQLiteException e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
                return false;
            }

        }
        public bool UpsertSharePointDriveFile(string repoId, SharePointDriveFile[] data)
        {
            //Invalid external repo table pk.
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return false;
            }
            if (data == null || data.Length == 0)
            {
                return false;
            }
            List<KeyValuePair<string, SQLiteParameter[]>> upserts = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in data)
            {
                if (i == null)
                {
                    continue;
                }
                // Java time mills is not same as C# time mills
                upserts.Add(SharePointFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[repoId],
                    i.FileId, i.IsFolder, i.IsSite,
                    i.Name, i.Size, DateTimeHelper.DateTimeToTimestamp(i.ModifiedTime),
                    i.PathId, i.DisplayPath, i.CloudPathId, i.IsNxlFile));
            }
            lock (mSharePointDriveFileLock)
            {
                ExecuteNonQueryBatch(upserts.ToArray());
            }
            return true;
        }
        public SharePointDriveFile[] QuerySharePointFile(string repoId, string pathId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new SharePointDriveFile[0];
            }
            if (string.IsNullOrEmpty(pathId))
            {
                pathId = "/";
            }
            if (pathId.Length > 1 && !pathId.EndsWith("/"))
            {
                pathId += "/";
            }
            List<SharePointDriveFile> rt = new List<SharePointDriveFile>();
            lock (mSharePointDriveFileLock)
            {
                ExecuteReader(SharePointFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], pathId), (reader) =>
                {
                    while (reader != null && reader.Read())
                    {
                        var item = SharePointDriveFile.NewByLocal(reader);
                        if (item == null)
                        {
                            continue;
                        }
                        // filter by cloud_path_id, only return first level items
                        if (FileHelper.IsDirectChild(item.CloudPathId, pathId))
                        {
                            rt.Add(item);
                        }
                    }
                });
                return rt.ToArray();
            }
        }
        public SharePointDriveFile[] QuerySharePointOfflineFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new SharePointDriveFile[0];
            }
            List<SharePointDriveFile> rt = new List<SharePointDriveFile>();
            lock (mSharePointDriveFileLock)
            {
                ExecuteReader(SharePointFileDao.Query_OFFLINE_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
                {
                    while (reader != null && reader.Read())
                    {
                        var item = SharePointDriveFile.NewByLocal(reader);
                        if (item == null)
                        {
                            continue;
                        }
                        rt.Add(item);
                    }
                });
                return rt.ToArray();
            }
        }
        public string QuerySharePointDriveFilePathId(int sharePointDriveFileTablePk)
        {
            string rt = "/";
            lock (mSharePointDriveFileLock)
            {
                ExecuteReader(SharePointFileDao.Query_PATH_ID_SQL(sharePointDriveFileTablePk), (reader) =>
                {
                    if (reader != null && reader.Read())
                    {
                        rt = reader["path_id"].ToString();
                    }
                });
                return rt;
            }
        }
        public string QuerySharePointDriveFilePathId(string repoId, string cloudPathId)
        {
            string rt = "/";
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return rt;
            }
            lock (mSharePointDriveFileLock)
            {
                ExecuteReader(SharePointFileDao.Query_PATH_ID_SQL(RmsExternalRepo_Id2PK[repoId], cloudPathId), (reader) =>
                 {
                     if (reader != null && reader.Read())
                     {
                         rt = reader["path_id"].ToString();
                     }
                 });
                return rt;
            }
        }
        public bool DeleteSharePointDriveFile(string repoId, string pathId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return false;
            }
            lock (mSharePointDriveFileLock)
            {
                ExecuteNonQuery(SharePointFileDao.Delete_File_SQL(RmsExternalRepo_Id2PK[repoId], pathId));
            }
            return true;
        }
        public bool DeleteSharePointDriveFolder(string repoId, string pathId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return false;
            }
            lock (mSharePointDriveFileLock)
            {
                ExecuteNonQuery(SharePointFileDao.Delete_Folder_And_SubChildren_SQL(RmsExternalRepo_Id2PK[repoId], pathId));
            }
            return true;
        }
        public void UpdateSharePointDriveFileLocalPath(int table_pk, string localPath)
        {
            lock (mSharePointDriveFileLock)
            {
                ExecuteNonQuery(SharePointFileDao.Update_LocalPath_SQL(table_pk, localPath));
            }
        }
        public void UpdateSharePointDriveFileStatus(int table_pk, int status)
        {
            lock (mSharePointDriveFileLock)
            {
                ExecuteNonQuery(SharePointFileDao.Update_FileStatus_SQL(table_pk, status));
            }
        }
        public void UpdateSharePointDriveFileOfflineMark(int table_pk, bool offline)
        {
            lock (mSharePointDriveFileLock)
            {
                ExecuteNonQuery(SharePointFileDao.Update_IsOffline_SQL(table_pk, offline));
            }
        }
        #endregion

        #region SharePointLocalFile
        private readonly object mSharePointDriveLocalFileLock = new object();
        public ExternalDriveLocalFile[] QuerySharePointDriveAllLocalFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveLocalFile[0];
            }
            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            lock (mSharePointDriveLocalFileLock)
            {
                ExecuteReader(SharePointLocalFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
                {
                    while (reader != null && reader.Read())
                    {
                        var item = SharePointDriveLocalFile.NewByDBItem(reader);
                        if (item == null)
                        {
                            continue;
                        }
                        rt.Add(item);
                    }
                });
                return rt.ToArray();
            }
        }
        public ExternalDriveLocalFile[] QuerySharePointDriveLocalFile(string repoId, string pathId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new ExternalDriveLocalFile[0];
            }
            List<ExternalDriveLocalFile> rt = new List<ExternalDriveLocalFile>();
            lock(mSharePointDriveLocalFileLock)
            {
                ExecuteReader(SharePointLocalFileDao.Query_SQL_Under_TargetFolder(RmsExternalRepo_Id2PK[repoId], pathId), (reader) =>
                {
                    while (reader != null && reader.Read())
                    {
                        var item = SharePointDriveLocalFile.NewByDBItem(reader);
                        if (item == null)
                        {
                            continue;
                        }
                        rt.Add(item);
                    }
                });
                return rt.ToArray();
            }
        }
        public void InsertSharePointDriveLocalFile(string repoId, string parentPathId, string name,
             string localPath, int size, DateTime lastModified)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return;
            }
            lock (mSharePointDriveLocalFileLock)
            {
                ExecuteNonQuery(SharePointLocalFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[repoId],
                    parentPathId, name, localPath, size, lastModified));
            }
        }
        public void UpdateSharePointDriveLocalFileStatus(int table_pk, int status)
        {
            lock(mSharePointDriveLocalFileLock)
            {
                ExecuteNonQuery(SharePointLocalFileDao.Update_FileStatus_SQL(table_pk, status));
            }
        }
        public void DeleteSharePointDriveLocalFile(int table_pk)
        {
            lock (mSharePointDriveLocalFileLock)
            {
                ExecuteNonQuery(SharePointLocalFileDao.Delete_Item_SQL(table_pk));
            }
        }
        #endregion

        #endregion

        #endregion // External repository

        #region Shared workspace

        public struct InsertSharedWorkspaceFile
        {
            public string repoId; // added

            public string fileId;
            public string path;
            public string pathId;
            public string name;
            public string type;
            //
            public long modifiedTime;
            public long createTime;
            //
            public int size;
            public int isProtectedFile;
            public int isFolder;
        }

        #region Shared workspace file

        // Insert
        //
        public void UpsertSharedWorkspaceFileBatchEx(InsertSharedWorkspaceFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }

            List<KeyValuePair<String, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(
                        SharedWorkspaceFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[i.repoId],
                        i.fileId, "", i.path, i.pathId, i.name, i.type, i.modifiedTime, i.createTime, i.size,
                        i.isFolder, i.isProtectedFile)
                    );
            }
            ExecuteNonQueryBatch(sqls.ToArray());
        }

        // Insert faked root for shared workspace that make added local file into its root folder.
        // Should be invoked in SyncFile function.
        public bool InsertSharedWorkspaceFakedRoot(string repoid)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                return false;
            }
            try
            {
                SqliteOpenHelper.ExecuteNonQuery(DataBaseConnectionString,
                    SharedWorkspaceFileDao.InsertFakedRoot_SQL(RmsExternalRepo_Id2PK[repoid]));
            }
            catch (SQLiteException e)
            {
                SkydrmApp.Singleton.Log.Error(e.ToString());
                return false;
            }

            return true;
        }

        // Query file
        //
        public SharedWorkspaceFile[] ListSharedWorkspaceAllFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new SharedWorkspaceFile[0];
            }

            List<SharedWorkspaceFile> rt = new List<SharedWorkspaceFile>();
            ExecuteReader(SharedWorkspaceFileDao.QueryAll_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                while (reader.Read())
                {
                    var item = SharedWorkspaceFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        public SharedWorkspaceFile[] ListSharedWorkspaceFile(string repoId, string path)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new SharedWorkspaceFile[0];
            }

            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if (path.Length > 1 && !path.EndsWith("/"))
            {
                path += "/";
            }

            List<SharedWorkspaceFile> rt = new List<SharedWorkspaceFile>();
            ExecuteReader(SharedWorkspaceFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId], path), (reader) =>
            {
                while (reader.Read())
                {
                    var item = SharedWorkspaceFile.NewByReader(reader);

                    // filter by cloud_path_id, only return first level items
                    if (FileHelper.IsDirectChild(item.RmsPath, path))
                    {
                        rt.Add(item);
                    }
                }
            });

            return rt.ToArray();
        }

        public SharedWorkspaceFile[] ListSharedWorkspaceOfflineFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new SharedWorkspaceFile[0];
            }

            List<SharedWorkspaceFile> rt = new List<SharedWorkspaceFile>();
            ExecuteReader(SharedWorkspaceFileDao.QueryAll_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                while (reader.Read())
                {
                    var f = SharedWorkspaceFile.NewByReader(reader);
                    if (!f.RmsIsFolder && (f.IsOffline || f.Status == 1)) // 1 -- AvailableOffline
                    {
                        rt.Add(f);
                    }
                }
            });

            return rt.ToArray();
        }

        /// <summary>
        /// Get rms display path
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="sharedWorkspaceFile_pk"></param>
        /// <returns></returns>
        public string GetSharedWorkSpaceLocalFileRmsParentFolder(string repoId, int sharedWorkspaceFile_pk)
        {
            string rt = "/";
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return rt;
            }

            ExecuteReader(SharedWorkspaceFileDao.Query_Path_SQL(RmsExternalRepo_Id2PK[repoId], sharedWorkspaceFile_pk), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["rms_path"].ToString();
                }
            });

            return rt;
        }

        /// <summary>
        /// Get rms path id
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="sharedWorkspaceFile_pk"></param>
        /// <returns></returns>
        public string GetSharedWorkSpaceFileRmsParentFolderId(string repoId, string displayPath)
        {
            string rt = "";
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return rt;
            }

            ExecuteReader(SharedWorkspaceFileDao.Query_PathId_SQL(RmsExternalRepo_Id2PK[repoId], displayPath), (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["rms_path_id"].ToString();
                }
            });

            return rt;
        }

        // Delete
        //
        public void DeleteSharedWorkspaceFile(string repoid, string fileId)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                ExecuteNonQuery(SharedWorkspaceFileDao.Delete_File_SQL(RmsExternalRepo_Id2PK[repoid], fileId));
            }
        }

        public void DeleteSharedWorkspaceFolderAndAllSubFiles(string repoId, string path)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                ExecuteNonQuery(SharedWorkspaceFileDao.Delete_Folder_And_SubChildren_SQL(RmsExternalRepo_Id2PK[repoId], path));
            }
        }

        // Update fields
        //
        public void UpdateSharedWorkspaceFileStatus(int table_pk, int status)
        {
            ExecuteNonQuery(SharedWorkspaceFileDao.Update_FileStatus_SQL(table_pk, status));
        }
        public void UpdateSharedWorkspaceFileOffline(int table_pk, bool is_offline)
        {
            ExecuteNonQuery(SharedWorkspaceFileDao.Update_IsOffline_SQL(table_pk, is_offline));
        }
        public void UpdateSharedWorkspaceFileLocalPath(int table_pk, string localPath)
        {
            ExecuteNonQuery(SharedWorkspaceFileDao.Update_LocalPath_SQL(table_pk, localPath));
        }
        public void UpdateSharedWorkspaceFileEditsStatus(int table_pk, int newStatus)
        {
            ExecuteNonQuery(SharedWorkspaceFileDao.Update_NXL_EditStatus_SQL(table_pk, newStatus));
        }
        public void UpdateSharedWorkspaceWhenOverwriteInLeaveCopy(int table_pk, int status, long size, DateTime lastModifed)
        {
            ExecuteNonQuery(SharedWorkspaceFileDao.Update_NXL_For_Overwrite_LeaveCopyModel(table_pk, status, size, lastModifed));
        }

        #endregion // Shared workspace file

        #region Shared workspace local file

        // Insert
        //
        public void InertLocalFileToSharedWorkspace(string repoid, string folderId, string name, // folderId: Path
            string localPath, int size, DateTime lastModified, string reserved1)
        {
            if (IsRmsExternalRepoIdMapContainsKey(repoid))
            {
                ExecuteNonQuery(SharedWorkspaceLocalFileDao.Upsert_SQL(RmsExternalRepo_Id2PK[repoid],
                    folderId,
                    name,
                    localPath,
                    size,
                    lastModified, reserved1));
            }
        }

        // Query 
        //
        public SharedWorkspaceLocalFile[] ListSharedWorkspaceLocalFile(string repoId, string path)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new SharedWorkspaceLocalFile[0];
            }

            List<SharedWorkspaceLocalFile> rt = new List<SharedWorkspaceLocalFile>();
            ExecuteReader(SharedWorkspaceLocalFileDao.Query_SQL_Under_TargetFolder(RmsExternalRepo_Id2PK[repoId], path), (reader) =>
            {
                while (reader.Read())
                {
                    var item = SharedWorkspaceLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }
        public SharedWorkspaceLocalFile[] ListSharedWorkspaceAllLocalFile(string repoId)
        {
            if (!IsRmsExternalRepoIdMapContainsKey(repoId))
            {
                return new SharedWorkspaceLocalFile[0];
            }

            List<SharedWorkspaceLocalFile> rt = new List<SharedWorkspaceLocalFile>();
            ExecuteReader(SharedWorkspaceLocalFileDao.Query_SQL(RmsExternalRepo_Id2PK[repoId]), (reader) =>
            {
                while (reader.Read())
                {
                    var item = SharedWorkspaceLocalFile.NewByReader(reader);
                    rt.Add(item);
                }
            });

            return rt.ToArray();
        }

        // Update fields
        public void UpdateSharedWorkspaceLocalFileStatus(int table_pk, int status)
        {
            ExecuteNonQuery(SharedWorkspaceLocalFileDao.Update_FileStatus_SQL(table_pk, status));
        }
        public void UpdateSharedWorkspaceLocalFileLocalPath(int table_pk, string localPath)
        {
            ExecuteNonQuery(SharedWorkspaceLocalFileDao.Update_File_LocalPath_SQL(table_pk, localPath));
        }
        public void UpdateSharedWorkspaceLocalFileName(int table_pk, string name)
        {
            ExecuteNonQuery(SharedWorkspaceLocalFileDao.Update_File_Name_SQL(table_pk, name));
        }
        public void UpdateSharedWorkspaceLocalFileOriginalPath(int table_pk, string originalPath)
        {
            ExecuteNonQuery(SharedWorkspaceLocalFileDao.Update_Original_Path_SQL(table_pk, originalPath));
        }
        public void UpdateSharedWorkspaceLocalFileReserved1(int table_pk, string reserved1)
        {
            ExecuteNonQuery(SharedWorkspaceLocalFileDao.Update_File_Reserved1_SQL(table_pk, reserved1));
        }

        // Delete
        public void DeleteSharedWorkspaceLocalFile(int tablePk)
        {
            ExecuteNonQuery(SharedWorkspaceLocalFileDao.Delete_Item_SQL(tablePk));
        }

        #endregion // Shared workspace local file

        #endregion // Shared workspace

        #region DbEngine
        // forward to SqiliteOpenHelper
        private int ExecuteNonQuery(KeyValuePair<String, SQLiteParameter[]> pair)
        {
            return ExecuteNonQuery(pair.Key, pair.Value);
        }

        private int ExecuteNonQuery(string sql, SQLiteParameter[] parameters)
        {
            return SqliteOpenHelper.ExecuteNonQuery(DataBaseConnectionString, sql, parameters);
        }

        private int ExecuteNonQueryBatch(KeyValuePair<string, SQLiteParameter[]>[] sqls)
        {
            return SqliteOpenHelper.ExecuteNonQueryBatch(DataBaseConnectionString, sqls);
        }

        private void ExecuteReader(KeyValuePair<String, SQLiteParameter[]> pair,
            Action<SQLiteDataReader> action)
        {
            ExecuteReader(pair.Key, pair.Value, action);
        }

        private void ExecuteReader(string sql, SQLiteParameter[] parameters
            , Action<SQLiteDataReader> action)
        {
            SqliteOpenHelper.ExecuteReader(DataBaseConnectionString, sql, parameters, action);
        }

        #endregion
    }
}
