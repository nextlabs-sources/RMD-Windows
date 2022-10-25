using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.database
{
    public class FunctionProvider
    {
        string DataBasePath;
        string DataBaseConnectionString;

        // need to wrapper to class ActivePresenterObj
        int Server_Primary_Key = -1 ;
        int User_Primary_Key = -1 ; 
        int User_Login_Counts = 0;

        public Exception GetLastException { get; set; }

        // end need

        #region Init
        public FunctionProvider(string DbPath, string email, string router, string tenant)
        {
            GetLastException = null;
            DataBasePath = Path.Combine(DbPath, Config.Database_Name);
            // by omsond, normally, we should set busy_timeout as 10s,
            // but there must be some unoptimized code, so I set it as 60s
            DataBaseConnectionString =
                @"Data Source=" + DataBasePath + ";foreign_keys=true;busy_timeout=60000;";

            OnUserRecovered(email, router, tenant);
        }

        #endregion

        private int OnUserRecovered(string email, string router, string tenant)
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
                });

            return rms_user_id;
        }

        private SharedWithMeFile[] ListSharedWithMeFile()
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

        public SharedWithMeFile QuerySharedWithMeFileByDuid(string rms_duid)
        {
            SharedWithMeFile result = null;

            if (User_Primary_Key == -1)
            {
                return result;
            }

            SharedWithMeFile[] sharedWithMeFiles = ListSharedWithMeFile();

            for(int i=0; i< sharedWithMeFiles.Length; i++)
            {
                if (string.Compare(rms_duid, sharedWithMeFiles[i].Duid, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    result = sharedWithMeFiles[i];
                    break;
                }
            }
            return result;
        }

        private List<Project> ListProject()
        {       
            List<Project> rt = new List<Project>();
            ExecuteReader(ProjectDao.Query_SQL(User_Primary_Key), (reader) =>
            {
                //Return project infos.
                while (reader.Read())
                {
                    rt.Add(Project.NewByReader(reader));
                }
            });

            return rt;
        }

        public ProjectFile QueryProjectFileByDuid(string rms_duid, out int projectId)
        {
            ProjectFile result = null;
            projectId = -1;

            if (User_Primary_Key == -1)
            {
                return result;
            }

            List<Project> projects = ListProject();

            foreach(Project item in projects){

                ExecuteReader(ProjectFileDao.Query_SQL_By_Duid(item.Id, rms_duid), (reader) =>
                {
                    if (reader.Read())
                    {
                        result = ProjectFile.NewByReader(reader);
                    }
                });

                if (null!= result)
                {
                    projectId = item.Rms_project_id;
                    break;
                }
            }

            return result;
        }

        public MyVaultFile QueryMyVaultFileByDuid(string rms_duid)
        {
            MyVaultFile result = null;

            if (User_Primary_Key == -1)
            {
                return result;
            }

            ExecuteReader(MyVaultFileDao.Query_SQl_by_duid(User_Primary_Key, rms_duid), (reader) =>
            {
                if (reader.Read())
                {
                    result = MyVaultFile.NewByReader(reader);
                }
            });

            return result;
        }

        public WorkSpaceFile QueryWorkSpacetFileByDuid(string rms_duid)
        {
            WorkSpaceFile result = null;

            if (User_Primary_Key == -1)
            {
                return result;
            }

            ExecuteReader(WorkSpaceFileDao.Query_By_Duid_SQL(User_Primary_Key, rms_duid), (reader) =>
            {
                if (reader.Read())
                {
                    result = WorkSpaceFile.NewByReader(reader);
                }
            });

            return result;
        }


        private void ExecuteReader(KeyValuePair<String, SQLiteParameter[]> pair, Action<SQLiteDataReader> action)
        {
            ExecuteReader(pair.Key, pair.Value, action);
        }

        private void ExecuteReader(string sql, SQLiteParameter[] parameters , Action<SQLiteDataReader> action)
        {
            try
            {
                SqliteOpenHelper.ExecuteReader(DataBaseConnectionString, sql, parameters, action);
            }
            catch (Exception ex)
            {
                GetLastException = ex;
            }
        }

    }
}
