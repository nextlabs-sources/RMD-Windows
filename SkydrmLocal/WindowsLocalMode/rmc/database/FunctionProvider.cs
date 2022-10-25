
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
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.database2.manager;
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
        // end need

        private DbVersionControl versionControl = new DbVersionControl();

        SkydrmLocal.rmc.database.memcache.project.IProjectCache mProjectCache;
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
        // for new user login
        public void OnUserLogin(int rms_user_id,
                                string name,
                                string email,
                                string passcode,
                                int rms_user_type,
                                string rms_user_raw_json)
        {
            
            string defaultWaterMark = JsonConvert.SerializeObject(new SkydrmLocal.rmc.sdk.WaterMarkInfo() { text= "$(User)$(Break)$(Date)$(Time)" });
            string defaultExpiration = JsonConvert.SerializeObject(new SkydrmLocal.rmc.sdk.Expiration());
            string defaultQuota = JsonConvert.SerializeObject(new SkydrmLocal.rmc.featureProvider.Quota());
            string defaultPreference = JsonConvert.SerializeObject(new SkydrmLocal.rmc.featureProvider.User.UserPreference()
            {
                isStartUpload = true,
                heartBeatIntervalSec = SkydrmLocal.rmc.sdk.Config.Deault_Heartbeat,
                isFirstProtectFile=true,
                isCentralLocationRadio = false,
                isCentralPlcRadio=true
             });

            ExecuteReader(UserDao.Upsert_SQL(rms_user_id,
                    name, email, passcode, Server_Primary_Key,
                    rms_user_type, rms_user_raw_json, defaultWaterMark,
                    defaultExpiration, defaultQuota, defaultPreference), (reader) =>
             {
                 SkydrmLocalApp.Singleton.Log.Info("***********get this user's primary key ***********");
                 // get this user's primary key
                 if (reader.Read())
                 {
                     User_Primary_Key = Int32.Parse(reader[0].ToString());
                     User_Login_Counts = Int32.Parse(reader[1].ToString());
                     SkydrmLocalApp.Singleton.Log.Info("***********User_Primary_Key :***********" + User_Primary_Key);
                     SkydrmLocalApp.Singleton.Log.Info("***********User_Login_Counts :***********" + User_Login_Counts);
                 }
             });

            mProjectCache = new SkydrmLocal.rmc.database.memcache.project.ProjectCacheProxy(DataBaseConnectionString, User_Primary_Key);
            //Project pre-load tasks must be started after project id map has been filled.
            FillMapOfProjectId2PK(() =>
            {
                mProjectCache.OnInitialize();
            });
           
        }

        // locate server_id by router     
        // set serverID and userID as this user
        // return rms_user_id
        public int OnUserRecovered(string email, string router, string tenant)
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
             });

            //get rms_user_id
            ExecuteReader(UserDao.Query_User_Id_SQL(email, Server_Primary_Key), (reader) =>
             {
                 if (reader.Read())
                 {
                     // set rms_user_id
                     rms_user_id = Int32.Parse(reader[0].ToString());
                 }
             });
            // update user
            ExecuteReader(UserDao.Update_Auto_SQL(email, rms_user_id, Server_Primary_Key), (reader) =>
             {
                 if (reader.Read())
                 {
                    // set current user
                    User_Primary_Key = Int32.Parse(reader[0].ToString());
                     User_Login_Counts = Int32.Parse(reader[1].ToString());
                 }

                 mProjectCache = new SkydrmLocal.rmc.database.memcache.project.ProjectCacheProxy(DataBaseConnectionString, User_Primary_Key);
             });
            //Project pre-load tasks must be started after project id map has been filled.
            FillMapOfProjectId2PK(() =>
            {
                mProjectCache.OnInitialize();
            });

            return rms_user_id;
        }


        public void OnUserLogout()
        {
            try
            {
                ExecuteNonQuery(UserDao.Update_LastLogout_SQL(User_Primary_Key));
                ExecuteNonQuery(ServerDao.Update_LastLogout_SQL(Server_Primary_Key));

                this.User_Primary_Key = -1;

                Project_Id2PK.Clear();

                // wait for critical mem data recreate;
                if (mProjectCache != null)
                {
                    mProjectCache.OnDestroy();
                    //mProjectCache = null;
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
                     SkydrmLocalApp.Singleton.Log.Info("***********Server_Primary_Key :***********" + Server_Primary_Key);
                 }
             });
        }

        //only select 5 dataitem by desc
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
            User  u=null;
            ExecuteReader(UserDao.Query_User(User_Primary_Key), (reader) =>
            {
                SkydrmLocalApp.Singleton.Log.Info("***********User_Primary_Key :***********" + User_Primary_Key);
                SkydrmLocalApp.Singleton.Log.Info("***********reader.HasRows :***********" + reader.HasRows);
                if (reader.Read())
                {
                    SkydrmLocalApp.Singleton.Log.Info("***********reader.Read() :***********" + "true");
                    u = User.NewByReader(reader);
                }
            });
            if (u == null)
            {
                SkydrmLocalApp.Singleton.Log.Info("***********User is null**********");

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

        public void UpdateOrInsertRecentTouchedFile(string status, string fileName)
        {
            ExecuteNonQuery(RecentTouchedFileDao.Update_Or_Insert_SQL(User_Primary_Key, status, fileName));
        }

        public RecentTouchedFile[] GetRecentTouchedFile()
        {
            List<RecentTouchedFile> result = new List<RecentTouchedFile>();
            ExecuteReader(RecentTouchedFileDao.Query_SQL(User_Primary_Key), (reader) =>
            {
                while (reader.Read())
                {
                    result.Add(RecentTouchedFile.NewByReader(reader));
                }
            });
            return result.ToArray();
        }

        #endregion // end UserSession

        #region Project
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
            mProjectCache.InsertFakedRoot(Project_Id2PK[project_id]);
        }

        public Project[] ListProject()
        {
            return mProjectCache.ListProject().ToArray();
        }

        public void DeleteProject(int rms_project_id)
        {
            //
            // Note: Now project heartBeat will trigger the whole treeView Refresh, don't need this callback to refresh ui.
            //
            //mProjectCache.DeleteProject(rms_project_id, () => {
            //    // Notify refresh ui.
            //    SkydrmLocalApp.Singleton.InvokeEvent_ProjectUpdate();
            //});

            mProjectCache.DeleteProject(rms_project_id, null);
        }

        public void UpsertProject(int project_id, string project_name, string project_display_name,
                                 string project_description, bool isOwner, string tenant_id, OnResult Result)
        {
            mProjectCache.UpsertProject(project_id, project_name, project_display_name,
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
            mProjectCache.UpsertProjectIsEnabledAdhoc(Project_Id2PK[project_id], isEnabled);
        }

        public delegate void OnResult();

        public string GetProjectClassification(int projectId)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return "";
            }
            return mProjectCache.GetProjectClassification(Project_Id2PK[projectId]);
        }

        public void UpdateProjectClassification(int projectId, string classificationJson)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return;
            }
            Console.WriteLine("classificationJson {0}", classificationJson);
            mProjectCache.UpdateProjectClassification(Project_Id2PK[projectId], classificationJson);
        }

        public void DeleteProjectFile(int project_id,string rms_file_id)
        {
            if (!IsProjectIdMapContainsKey(project_id))
            {
                return;
            }
            mProjectCache.DeleteProjectFile(Project_Id2PK[project_id], rms_file_id);
        }

        public void DeleteProjectFolderAndAllSubFiles(int project_id, string rms_path_id)
        {
            if (!IsProjectIdMapContainsKey(project_id))
            {
                return;
            }
            mProjectCache.DeleteProjectFolderAndAllSubFiles(Project_Id2PK[project_id], rms_path_id);
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
               
        public void UpsertProjectFileBatch(InstertProjectFile[] files)
        {
            if (files.Length == 0)
            {
                return;
            }
            mProjectCache.UpsertProjectFileBatch(files, Project_Id2PK);
        }

        public ProjectFile[] ListProjectFile(int rms_project_id, string path)
        {
            if (!IsProjectIdMapContainsKey(rms_project_id))
            {
                return new ProjectFile[0];
            }
            return mProjectCache.ListProjectFile(Project_Id2PK[rms_project_id], path).ToArray();
        }

        public List<ProjectFile> ListAllProjectFile(int rms_project_id)
        {
            if (!IsProjectIdMapContainsKey(rms_project_id))
            {
                return new List<ProjectFile>();
            }
            return mProjectCache.ListAllProjectFile(Project_Id2PK[rms_project_id]);
        }

        public ProjectFile[] ListProjectOfflineFile(int rms_project_id)
        {
            if (!IsProjectIdMapContainsKey(rms_project_id))
            {
                return new ProjectFile[0];
            }
            return mProjectCache.ListProjectOfflineFile(Project_Id2PK[rms_project_id]).ToArray();
        }

        public void UpdateProjectFileOperationStatus(int projectTablePk, int project_file_table_pk,int newStatus)
        {
            mProjectCache.UpdateProjectFileOperationStatus(projectTablePk, project_file_table_pk, newStatus);
        }

        public void UpdateProjectFileOfflineMark(int projectTablePk, int project_file_table_pk, bool newMark)
        {
            mProjectCache.UpdateProjectFileOfflineMark(projectTablePk, project_file_table_pk, newMark);
        }

        public void UpdateProjectFileLocalpath(int projectTablePk, int project_file_table_pk, string newPath)
        {
            mProjectCache.UpdateProjectFileLocalpath(projectTablePk, project_file_table_pk, newPath);
        }

        public void UpdateProjectFileLastModifiedTime(int projectTablePk, int project_file_table_pk, DateTime lastModifiedTime)
        {
            mProjectCache.UpdateProjectFileLastModifiedTime(projectTablePk, project_file_table_pk, lastModifiedTime);
        }

        public void UpdateProjectFileFileSize(int projectTablePk, int project_file_table_pk, long fileSize)
        {
            mProjectCache.UpdateProjectFileFileSize(projectTablePk, project_file_table_pk, fileSize);
        }

        // find a project local added file's rms parent folder name for UI to use
        public string QueryProjectLocalFileRMSParentFolder(int projectId, int projectFile_RowNumber)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return "";
            }
            return mProjectCache.QueryProjectLocalFileRMSParentFolder(Project_Id2PK[projectId], projectFile_RowNumber);
        }

        // Query project file id (row number)
        public int QueryProjectFileId(int projectId, string rms_path_id)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return -1;
            }

            return mProjectCache.QueryProjectFileId(projectId, rms_path_id);
        }

        public void UpdateProjectLocalFileOperationStatus(int project_table_pk, int project_local_file_table_pk, int newStatus)
        {
            mProjectCache.UpdateProjectLocalFileOperationStatus(project_table_pk, project_local_file_table_pk, newStatus);
        }

        public void DeleteProjectLocalFile(int project_table_pk, int project_local_file_table_pk)
        {
            mProjectCache.DeleteProjectLocalFile(project_table_pk, project_local_file_table_pk);
        }

        public void DeleteProjectAllLocalFiles(int project_table_pk)
        {
            mProjectCache.DeleteProjectAllLocalFiles(project_table_pk);
        }

        public void DeleteProjectFolderLocalFiles(int project_table_pk, int projectFile_RowNumber)
        {
            mProjectCache.DeleteProjectFolderLocalFiles(project_table_pk, projectFile_RowNumber);
        }

        public void AddLocalFileToProject(int projectId, string FolderId,
            string name, string path, int size, DateTime lastModified)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return;
            }
            mProjectCache.AddLocalFileToProject(Project_Id2PK[projectId], FolderId, name, path, size, lastModified);
        }

        public ProjectLocalFile[] ListProjectLocalFiles(int projectId, string FolderId)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return new ProjectLocalFile[0];
            }
            return mProjectCache.ListProjectLocalFiles(Project_Id2PK[projectId], FolderId).ToArray();
        }

        public ProjectLocalFile[] ListProjectLocalFiles(int projectId)
        {
            if (!IsProjectIdMapContainsKey(projectId))
            {
                return new ProjectLocalFile[0];
            }
            return mProjectCache.ListProjectLocalFiles(Project_Id2PK[projectId]).ToArray();
        }

        public void UpdateProjectFileEditStatus(int projectTablePk, int project_file_table_pk, int newStatus)
        {
            mProjectCache.UpdateProjectFileEditStatus(projectTablePk, project_file_table_pk, newStatus);
        }

        public void UpdateProjectFileModifyRightsStatus(int projectTablePk, int project_file_table_pk, int newStatus)
        {
            mProjectCache.UpdateProjectFileModifyRightsStatus(projectTablePk, project_file_table_pk, newStatus);
        }

        #endregion  // end project`

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
                User_Primary_Key,duid,name,
                type,size,shared_date,shared_by,transcation_id,transcation_code,
                shared_link_url,rights_json,comments,is_owner
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
                    return ;
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

        public void UpdateSharedWithMeFileIsOffline (
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
            ExecuteNonQuery(SharedWithMeFileDao.Delete_Item_SQL(User_Primary_Key,RmsName));
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
                    string originalPath)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultLocalFileDao.UPSET_SQL(user_tb_pk,
                nxl_name, nxl_local_path,
                last_modified_time,
                shared_with_list, size,
                status, comment, originalPath));
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

        public void UpdateMyVaultFileStatus(int table_pk , int status)
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
      
        public void UpdateMyVaultFileLocalPath(int  table_pk,string nxl_local_path)
        {
            int user_tb_pk = User_Primary_Key;
            ExecuteNonQuery(MyVaultFileDao.Update_Nxl_Local_Path( table_pk, nxl_local_path));
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
                    rt= reader["shared_with_list"].ToString();
                }
            });
            return rt;
        }

        public void UpdateMyVaultLocalFileSharedWithList(string nxl_name,string shared_with_list)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Update_Shared_With_SQL(User_Primary_Key, nxl_name,shared_with_list));
        }

        public void UpdateMyVaultLocalFileOriginalPath(string nxl_name, string originalPath)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Update_Original_Path_SQL(User_Primary_Key, nxl_name, originalPath));
        }

        public void DeleteMyVaultLocalFile(string nxl_name)
        {
            ExecuteNonQuery(MyVaultLocalFileDao.Delete_Item_SQL(User_Primary_Key, nxl_name));
        }
        #endregion

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
             ExecuteReader(pair.Key, pair.Value,action);
        }

        private void ExecuteReader(string sql, SQLiteParameter[] parameters
            , Action<SQLiteDataReader> action)
        {
             SqliteOpenHelper.ExecuteReader(DataBaseConnectionString, sql,parameters,action);
        }

        #endregion
    }
}
