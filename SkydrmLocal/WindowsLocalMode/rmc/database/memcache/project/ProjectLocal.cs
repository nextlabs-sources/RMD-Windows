using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database2.table.project;
using static Skydrmlocal.rmc.database2.FunctionProvider;

namespace SkydrmLocal.rmc.database.memcache.project
{
    class ProjectLocal : IProjectLocal
    {
        private readonly string dataBaseConnectionString;
        private readonly int user_Primary_Key;

        public ProjectLocal(string connectionString, int upk)
        {
            dataBaseConnectionString = connectionString;
            user_Primary_Key = upk;
        }

        #region Project table.
        public List<Project> ListProject()
        {
            string cs = CheckNoNull<string>("ListProject", dataBaseConnectionString);
            int upk = CheckNoNull<int>("ListProject", user_Primary_Key);

            List<Project> rt = new List<Project>();
            SqliteOpenHelper.ExecuteReader(cs, ProjectDao.Query_SQL(upk), (reader) =>
             {
                //Return project infos.
                while (reader.Read())
                 {
                     rt.Add(Project.NewByReader(reader));
                 }
             });

            return rt;
        }

        public bool DeleteProject(int rms_project_id)
        {
            string cs = CheckNoNull<string>("DeleteProject", dataBaseConnectionString);
            int upk = CheckNoNull<int>("DeleteProject", user_Primary_Key);

            SqliteOpenHelper.ExecuteNonQuery(cs, ProjectDao.Delete_SQL(upk, rms_project_id));

            return true;
        }

        public int UpsertProject(int project_id, string project_name,
            string project_display_name, string project_description,
            bool isOwner, string tenant_id)
        {
            string cs = CheckNoNull<string>("UpsertProject", dataBaseConnectionString);
            int upk = CheckNoNull<int>("UpsertProject", user_Primary_Key);
            int rt = -1;
            SqliteOpenHelper.ExecuteReader(cs, ProjectDao.Upsert_SQL(upk,
                project_id, project_name,
                project_display_name, project_description,
                isOwner, tenant_id), (reader) =>
                {
                    //Get the new insert item.
                    if (reader.Read())
                    {
                        rt = int.Parse(reader[0].ToString());
                    }
                });
            return rt;
        }

        public bool UpsertProjectIsEnabledAdhoc(int project_table_pk, bool isEnabled)
        {
            string cs = CheckNoNull<string>("UpsertProjectIsEnabledAdhoc", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("UpsertProjectIsEnabledAdhoc", project_table_pk);
            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectDao.Update_IsEnabledAdhoc(ppk, isEnabled));
            return true;
        }

        #endregion

        #region Project file table.
        public List<ProjectFile> ListAllProjectFile(int project_table_pk)
        {
            string cs = CheckNoNull<string>("ListAllProjectFile", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("ListAllProjectFile", project_table_pk);
            List<ProjectFile> rt = new List<ProjectFile>();
            SqliteOpenHelper.ExecuteReader(cs, ProjectFileDao.Query_SQL(ppk, "/"), (reader) =>
            {
                while (reader.Read())
                {
                    rt.Add(ProjectFile.NewByReader(reader));
                }
            });
            return rt;
        }

        public List<ProjectFile> ListProjectFile(int project_table_pk, string path)
        {
            string cs = CheckNoNull<string>("ListProjectFile", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("ListProjectFile", project_table_pk);

            if (path == null || path.Length == 0)
            {
                path = "/";
            }
            if (path.Length > 1 && !path.EndsWith("/"))
            {
                path += '/';
            }

            List<ProjectFile> rt = new List<ProjectFile>();
            SqliteOpenHelper.ExecuteReader(cs, ProjectFileDao.Query_SQL(ppk, path), (reader) =>
            {
                while (reader.Read())
                {
                    ProjectFile f = ProjectFile.NewByReader(reader);
                    // filter by Rms_display_path, only return first level items
                    if (Utils.IsDirectChild(f.Rms_display_path, path))
                    {
                        rt.Add(f);
                    }
                }
            });
            return rt;
        }

        public List<ProjectFile> ListProjectOfflineFile(int project_table_pk)
        {
            string cs = CheckNoNull<string>("ListProjectOfflineFile", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("ListProjectOfflineFile", project_table_pk);

            List<ProjectFile> rt = new List<ProjectFile>();
            SqliteOpenHelper.ExecuteReader(cs, ProjectFileDao.Query_SQL(ppk, "/"), (reader) =>
            {
                while (reader.Read())
                {
                    ProjectFile f = ProjectFile.NewByReader(reader);
                    // avoid path it self
                    if (!f.Rms_is_folder && f.Is_offline == true)
                    {
                        rt.Add(f);
                    }
                }
            });
            return rt;
        }

        public bool DeleteProjectFolderAndAllSubFiles(int project_table_pk, string rms_path_id)
        {
            string cs = CheckNoNull<string>("DeleteProjectFolderAndAllSubFiles", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("DeleteProjectFolderAndAllSubFiles", project_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Delete_Folder_And_SubChildren_SQL(ppk, rms_path_id));
            return true;
        }

        public bool DeleteProjectFile(int project_table_pk, string rms_file_id)
        {
            string cs = CheckNoNull<string>("DeleteProjectFile", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("DeleteProjectFile", project_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Delete_File_SQL(ppk, rms_file_id));
            return true;
        }

        public bool UpsertProjectFileBatch(InstertProjectFile[] files, Dictionary<int, int> Project_Id2PK)
        {
            string cs = CheckNoNull<string>("UpsertProjectFileBatch", dataBaseConnectionString);

            List<KeyValuePair<string, SQLiteParameter[]>> sqls = new List<KeyValuePair<string, SQLiteParameter[]>>();
            foreach (var i in files)
            {
                // Java time mills is not same as C# time mills
                sqls.Add(ProjectFileDao.Upsert_SQL(Project_Id2PK[i.project_id],
                i.file_id, i.file_duid, i.file_display_path, i.file_path_id, i.file_nxl_name,
                JavaTimeConverter.ToCSLongTicks(i.file_lastModifiedTime),
                JavaTimeConverter.ToCSLongTicks(i.file_creationTime),
                i.file_size,
                i.file_rms_ownerId,
                i.file_ownerDisplayName,
                i.file_ownerEmail));
            }

            SqliteOpenHelper.ExecuteNonQueryBatch(cs,
                sqls.ToArray());
            return true;
        }

        public bool InsertFakedRoot(int project_table_pk)
        {
            string cs = CheckNoNull<string>("InsertFakedRoot", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("InsertFakedRoot", project_table_pk);

            // Fix bug 52664 -- crash caused by SQLiteException (the reproduce rate is low).
            try
            {
                SqliteOpenHelper.ExecuteNonQuery(cs,
                    ProjectFileDao.InsertFakedRoot_SQL(ppk));
            } catch(SQLiteException e)
            {
                SkydrmLocalApp.Singleton.Log.Error(e.ToString());
                return false;
            }

            return true;
        }

        public bool UpdateProjectFileOperationStatus(int project_file_table_pk, int newStatus)
        {
            string cs = CheckNoNull<string>("UpdateProjectFileOperationStatus", dataBaseConnectionString);
            int pftk = CheckNoNull<int>("UpdateProjectFileOperationStatus", project_file_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Update_FileStatus_SQL(pftk, newStatus));
            return true;
        }

        public bool UpdateProjectFileOfflineMark(int project_file_table_pk, bool newMark)
        {
            string cs = CheckNoNull<string>("UpdateProjectFileOfflineMark", dataBaseConnectionString);
            int pftk = CheckNoNull<int>("UpdateProjectFileOfflineMark", project_file_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Update_IsOffline_SQL(pftk, newMark));
            return true;
        }

        public bool UpdateProjectFileLocalpath(int project_file_table_pk, string newPath)
        {
            string cs = CheckNoNull<string>("UpdateProjectFileLocalpath", dataBaseConnectionString);
            int pftk = CheckNoNull<int>("UpdateProjectFileLocalpath", project_file_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Update_LocalPath_SQL(pftk, newPath));
            return true;
        }

        public bool UpdateProjectFileLastModifiedTime(int project_file_table_pk, DateTime lastModifiedTime)
        {
            string cs = CheckNoNull<string>("UpdateProjectFileLastModifiedTime", dataBaseConnectionString);
            int pftk = CheckNoNull<int>("UpdateProjectFileLastModifiedTime", project_file_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Update_LastModifiedTime_SQL(pftk, lastModifiedTime));

            return true;
        }

        public bool UpdateProjectFileFileSize(int project_file_table_pk, long filesize)
        {
            string cs = CheckNoNull<string>("UpdateProjectFileFileSize", dataBaseConnectionString);
            int pftk = CheckNoNull<int>("UpdateProjectFileFileSize", project_file_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Update_FileSize_SQL(pftk, filesize));

            return true;
        }

        public bool UpdateProjectFileEditStatus(int project_file_table_pk, int newStatus)
        {
            string cs = CheckNoNull<string>("UpdateProjectFileEditStatus", dataBaseConnectionString);
            int pftk = CheckNoNull<int>("UpdateProjectFileEditStatus", project_file_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Update_EditStatus_SQL(pftk, newStatus));

            return true;
        }
        
        public bool UpdateProjectFileModifyRightsStatus(int project_file_table_pk, int newStatus)
        {
            string cs = CheckNoNull<string>("UpdateProjectFileModifyRightsStatus", dataBaseConnectionString);
            int pftk = CheckNoNull<int>("UpdateProjectFileModifyRightsStatus", project_file_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectFileDao.Update_ModifyRights_Status_SQL(pftk, newStatus));

            return true;
        }

        #endregion

        #region Project local file table.
        public List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk)
        {
            string cs = CheckNoNull<string>("ListProjectLocalFiles", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("ListProjectLocalFiles", project_table_pk);
            List<ProjectLocalFile> rt = new List<ProjectLocalFile>();

            SqliteOpenHelper.ExecuteReader(cs,
                ProjectLocalFileDao.Query_SQL(ppk),
                (reader) =>
                {
                    while (reader.Read())
                    {
                        rt.Add(ProjectLocalFile.NewByReader(reader));
                    }
                });

            return rt;

        }

        public List<ProjectLocalFile> ListProjectLocalFiles(int project_table_pk, string FolderId)
        {
            string cs = CheckNoNull<string>("ListProjectLocalFiles", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("ListProjectLocalFiles", project_table_pk);

            List<ProjectLocalFile> rt = new List<ProjectLocalFile>();
            SqliteOpenHelper.ExecuteReader(cs,
                ProjectLocalFileDao.Query_SQL(ppk, FolderId), (reader) =>
             {
                 while (reader.Read())
                 {
                     rt.Add(ProjectLocalFile.NewByReader(reader));
                 }
             });
            return rt;
        }

        public bool DeleteProjectLocalFile(int project_local_file_table_pk)
        {
            string cs = CheckNoNull<string>("DeleteProjectLocalFile", dataBaseConnectionString);
            int plfk = CheckNoNull<int>("DeleteProjectLocalFile", project_local_file_table_pk);

            // Add try...catch, fixed bug 51916
            try
            {
                SqliteOpenHelper.ExecuteNonQuery(cs,
                    ProjectLocalFileDao.Delete_Item_SQL(plfk));
            }
            catch (SQLiteException e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Sqlite execute delete ProjectLocalFile failed, Id:"+ project_local_file_table_pk, e);
                return false;
            }

            return true;
        }

        public int AddLocalFileToProject(int project_table_pk, string FolderId, string name, string path, int size, DateTime lastModified)
        {
            string cs = CheckNoNull<string>("AddLocalFileToProject", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("AddLocalFileToProject", project_table_pk);
            int rt = -1;
            SqliteOpenHelper.ExecuteReader(cs,
                ProjectLocalFileDao.Upsert_SQL(ppk,
                FolderId, name,
                path, size,
                lastModified), (reader) =>
             {
                //Get the new insert item id.
                if (reader.Read())
                 {
                     rt = int.Parse(reader[0].ToString());
                 }
             });
            return rt;
        }

        public string QueryProjectLocalFileRMSParentFolder(int project_table_pk, int projectFile_RowNumber)
        {
            string cs = CheckNoNull<string>("QueryProjectLocalFileRMSParentFolder", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("QueryProjectLocalFileRMSParentFolder", project_table_pk);
            string rt = "/";
            SqliteOpenHelper.ExecuteReader(cs,
                ProjectFileDao.Query_DisplayPath_SQL(ppk, projectFile_RowNumber),
                (reader) =>
            {
                if (reader.Read())
                {
                    rt = reader["rms_display_path"].ToString();
                }
            });
            return rt;
        }

        public int QueryProjectFileId(int project_table_pk, string rms_path_id)
        {
            string cs = CheckNoNull<string>("QueryProjectFileId", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("QueryProjectFileId", project_table_pk);

            int ret = -1;

            SqliteOpenHelper.ExecuteReader(cs,
                ProjectFileDao.Query_RowNumberId_SQL(ppk, rms_path_id),
                (reader) =>
                {
                    if (reader.Read())
                    {
                        ret = int.Parse(reader["id"].ToString());
                    }
                });

            return ret;
        }

        public bool UpdateProjectLocalFileOperationStatus(int project_local_file_table_pk, int newStatus)
        {
            string cs = CheckNoNull<string>("UpdateProjectLocalFileOperationStatus", dataBaseConnectionString);
            int plfk = CheckNoNull<int>("UpdateProjectLocalFileOperationStatus", project_local_file_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectLocalFileDao.Update_FileStatus_SQL(plfk, newStatus));

            return true;
        }
        #endregion

        #region Project classification table.
        public string GetProjectClassification(int project_table_pk)
        {
            string cs = CheckNoNull<string>("GetProjectClassification", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("GetProjectClassification", project_table_pk);
            string rt = "{}";
            SqliteOpenHelper.ExecuteReader(cs,
                ProjectDao.Query_Classification_Json(ppk), (reader) =>
             {
                 if (reader.Read())
                 {
                     rt = reader["rms_classifcation_json"].ToString();
                 }
             });
            return rt;
        }

        public bool UpdateProjectClassification(int project_table_pk, string classificationJson)
        {
            string cs = CheckNoNull<string>("GetProjectClassification", dataBaseConnectionString);
            int ppk = CheckNoNull<int>("GetProjectClassification", project_table_pk);

            SqliteOpenHelper.ExecuteNonQuery(cs,
                ProjectDao.Update_Classification_Json(ppk, classificationJson));

            return true;
        }
        #endregion

        #region common define.
        private static T CheckNoNull<T>(string caller, T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Exception occurred in ProjectLocal {0} invoke checkNoNull, " +
                    "illegal argument presented.", caller);
            }
            return item;
        }
        #endregion
    }
}
