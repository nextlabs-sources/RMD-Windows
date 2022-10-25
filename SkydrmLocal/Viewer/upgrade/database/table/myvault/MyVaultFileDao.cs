using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.database
{
    public class MyVaultFile
    {
        //Table ownered.
        private int id;
        private int user_table_pk;

        //All remote fields.
        private string rms_path_id;
        private string rms_display_path;
        private string rms_repo_id;
        private string rms_duid;
        private string rms_name;
        private DateTime rms_last_modified_time;
        private DateTime rms_creation_time;
        private DateTime rms_shared_time;
        private string rms_shared_with;
        private long rms_size;
        private bool rms_is_deleted;
        private bool rms_is_revoked;
        private bool rms_is_shared;
        private string source_repo_type;
        private string source_file_display_path;
        private string source_file_path_id;
        private string source_repo_name;
        private string source_repo_id;

        //All local fields.
        private string local_path;
        private bool is_offline;
        private DateTime offline_time;
        private bool is_favorite;
        private DateTime favorite_time;
        private int operation_status;

        // 3/27/2019 new added support edit file&modify rights feature.
        private int edit_status;
        private int modify_rights_status;
        private string reserved1;
        private string reserved2;
        private string reserved3;
        private string reserved4;

        public int Id { get => id; set => id = value; }
        public int User_Table_Pk { get => user_table_pk; set => user_table_pk = value; }
        public string RmsPathId { get => rms_path_id; set => rms_path_id = value; }
        public string RmsDisplayPath { get => rms_display_path; set => rms_display_path = value; }
        public string RmsRepoId { get => rms_repo_id; set => rms_repo_id = value; }
        public string RmsDuid { get => rms_duid; set => rms_duid = value; }
        public string RmsName { get => rms_name; set => rms_name = value; }
        public DateTime RmsLastModifiedTime { get => rms_last_modified_time; set => rms_last_modified_time = value; }
        public DateTime RmsCreationTime { get => rms_creation_time; set => rms_creation_time = value; }
        public DateTime RmsSharedTime { get => rms_shared_time; set => rms_shared_time = value; }
        public string RmsSharedWith { get => rms_shared_with; set => rms_shared_with = value; }
        public long RmsSize { get => rms_size; set => rms_size = value; }
        public bool RmsIsDeleted { get => rms_is_deleted; set => rms_is_deleted = value; }
        public bool RmsIsRevoked { get => rms_is_revoked; set => rms_is_revoked = value; }
        public bool RmsIsShared { get => rms_is_shared; set => rms_is_shared = value; }
        public string Source_Repo_Type { get => source_repo_type; set => source_repo_type = value; }
        public string Source_File_Display_Path { get => source_file_display_path; set => source_file_display_path = value; }
        public string Source_File_Path_Id { get => source_file_path_id; set => source_file_path_id = value; }
        public string Source_Repo_Name { get => source_repo_name; set => source_repo_name = value; }
        public string Source_Repo_Id { get => source_repo_id; set => source_repo_id = value; }
        public string LocalPath { get => local_path; set => local_path = value; }
        public bool Is_Offline { get => is_offline; set => is_offline = value; }
        public DateTime Offline_Time { get => offline_time; set => offline_time = value; }
        public bool Is_Favorite { get => is_favorite; set => is_favorite = value; }
        public DateTime Favorite_Time { get => favorite_time; set => favorite_time = value; }
        public int Status { get => operation_status; set => operation_status = value; }

        public int Edit_Status { get => edit_status; set => edit_status = value; }
        public int Modify_Rights_Status { get => modify_rights_status; set => modify_rights_status = value; }

        public string Reserved1 { get => reserved1; set => reserved1 = value; }
        public string Reserved2 { get => reserved2; set => reserved2 = value; }
        public string Reserved3 { get => reserved3; set => reserved3 = value; }
        public string Reserved4 { get => reserved4; set => reserved4 = value; }

        public static MyVaultFile NewByReader(SQLiteDataReader reader)
        {
            MyVaultFile item = new MyVaultFile
            {
                Id = int.Parse(reader["id"].ToString()),
                User_Table_Pk = int.Parse(reader["user_table_pk"].ToString()),
                RmsPathId = reader["rms_path_id"].ToString(),
                RmsDisplayPath = reader["rms_display_path"].ToString(),
                RmsRepoId = reader["rms_repo_id"].ToString(),
                RmsDuid = reader["rms_duid"].ToString(),
                RmsName = reader["rms_name"].ToString(),
                RmsLastModifiedTime = DateTime.Parse(reader["rms_last_modified_time"].ToString()).ToUniversalTime(),
                RmsCreationTime = DateTime.Parse(reader["rms_creation_time"].ToString()).ToUniversalTime(),
                RmsSharedTime = DateTime.Parse(reader["rms_shared_time"].ToString()).ToUniversalTime(),
                RmsSharedWith = reader["rms_shared_with"].ToString(),
                RmsSize = Int64.Parse(reader["rms_size"].ToString()),
                RmsIsDeleted = int.Parse(reader["rms_is_deleted"].ToString()) == 1,
                RmsIsRevoked = int.Parse(reader["rms_is_revoked"].ToString()) == 1,
                RmsIsShared = int.Parse(reader["rms_is_shared"].ToString()) == 1,
                Source_Repo_Type = reader["source_repo_type"].ToString(),
                Source_File_Display_Path = reader["source_file_display_path"].ToString(),
                Source_File_Path_Id = reader["source_file_path_id"].ToString(),
                Source_Repo_Name = reader["source_repo_name"].ToString(),
                Source_Repo_Id = reader["source_repo_id"].ToString(),

                LocalPath = reader["local_path"].ToString(),
                Is_Offline = int.Parse(reader["is_offline"].ToString()) == 1,
                Offline_Time = DateTime.Parse(reader["offline_time"].ToString()).ToUniversalTime(),

                Is_Favorite = int.Parse(reader["is_favorite"].ToString()) == 1,
                Favorite_Time = DateTime.Parse(reader["favorite_time"].ToString()),
                Status = int.Parse(reader["operation_status"].ToString()),

                Edit_Status = int.Parse(reader["edit_status"].ToString()),
                Modify_Rights_Status = int.Parse(reader["modify_rights_status"].ToString()),
                Reserved1 = "",
                Reserved2 = "",
                Reserved3 = "",
                Reserved4 = ""
            };
            return item;
        }
    }

    public class MyVaultFileDao
    {
        public static readonly string SQL_Create_Table_MyVaultFile = @"
            CREATE TABLE IF NOT EXISTS MyVaultFile (
                id                          integer             NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                user_table_pk               integer             default 0, 
                
                ------ rms returned -----
                rms_path_id                 varchar(255)        NOT NULL default '',
                rms_display_path            varchar(255)        NOT NULL default '',   
                rms_repo_id                 varchar(255)        NOT NULL default '',
                rms_duid                    varchar(255)        NOT NULL default '',
                rms_name                    varchar(255)        NOT NULL default '',
                rms_last_modified_time      datetime            NOT NULL default (datetime('now','localtime')),
                rms_creation_time           datetime            NOT NULL default (datetime('now','localtime')),
                rms_shared_time             datetime            NOT NULL default (datetime('now','localtime')),
                rms_shared_with             varchar(255)        NOT NULL default '',
                rms_size                    integer             NOT NULL default 0,
                rms_is_deleted              integer             NOT NULL default 0,
                rms_is_revoked              integer             NOT NULL default 0,
                rms_is_shared               integer             NOT NULL default 0,
                source_repo_type            varchar(255)        NOT NULL default '',
                source_file_display_path    varchar(255)        NOT NULL default '',
                source_file_path_id         varchar(255)        NOT NULL default '',
                source_repo_name            varchar(255)        NOT NULL default '',
                source_repo_id              varchar(255)        NOT NULL default '',
                
                ----- local added--------
                local_path                  varchar(255)        NOT NULL default '',
                is_offline                  integer             NOT NULL default 0,
                offline_time                DateTime            NOT NULL default (datetime('now','localtime')), 
                is_favorite                 integer             NOT NULL default 0,
                favorite_time               DateTime            NOT NULL default (datetime('now','localtime')),
                operation_status            integer             NOT NULL DEFAULT 4,
                
                ----- V2 added -----------
                edit_status                 integer             DEFAULT 0,
                modify_rights_status        integer             DEFAULT 0,
                reserved1                   text                DEFAULT '',
                reserved2                   text                DEFAULT '',
                reserved3                   text                DEFAULT '',
                reserved4                   text                DEFAULT '',

                UNIQUE(user_table_pk,rms_name),
                foreign key(user_table_pk) references User(id) on delete cascade);
        ";

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Edit_Status_V2 = @"
                   ALTER TABLE MyVaultFile ADD COLUMN 
                        edit_status             integer     DEFAULT 0;
        ";

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_ModifyRights_Status_V2 = @"
                   ALTER TABLE MyVaultFile ADD COLUMN 
                        modify_rights_status    integer     DEFAULT 0;
        ";

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Reserved1_V2 = @"
                   ALTER TABLE MyVaultFile ADD COLUMN 
                        reserved1               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Reserved2_V2 = @"
                   ALTER TABLE MyVaultFile ADD COLUMN 
                        reserved2               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Reserved3_V2 = @"
                   ALTER TABLE MyVaultFile ADD COLUMN 
                        reserved3               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Reserved4_V2 = @"
                   ALTER TABLE MyVaultFile ADD COLUMN 
                        reserved4               text         DEFAULT '';
        ";

        public static KeyValuePair<String, SQLiteParameter[]> UPSERT_SQL(
            int user_tb_pk,
            string path_id, string display_path, string repo_id, string duid,
            string nxl_name, Int64 last_modified_time, Int64 creation_time, Int64 shared_time,
            string shared_with_list, Int64 size,
            bool is_deleted, bool is_revoked, bool is_shared,
            string source_repo_type, string source_file_display_path,
            string source_file_path_id, string source_repo_name, string source_repo_id)
        {
            // comments by osmond, bug prone, but we have to do
            // RMS will return a millis as java 
            // use shareTime as last_modifyied and creation time
            var csTime = JavaTimeConverter.ToCSDateTime(shared_time);
            string sharedTimeStr = csTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"
                UPDATE MyVaultFile
                SET
                    rms_last_modified_time=@shared_time,
                    rms_creation_time=@shared_time,
                    rms_shared_time=@shared_time,
                    rms_shared_with=@shared_with_list,
                    rms_is_deleted=@is_deleted,
                    rms_is_revoked=@is_revoked,
                    rms_is_shared=@is_shared
                WHERE
                    user_table_pk=@user_table_pk AND rms_name=@nxl_name;

                ---------if no updated happeded, then insert one--------------------------
                
                INSERT INTO
                    MyVaultFile(user_table_pk,rms_path_id,
                                rms_display_path,
                                rms_repo_id,
                                rms_duid,rms_name,
                                rms_last_modified_time,
                                rms_creation_time,
                                rms_shared_time,
                                rms_shared_with,
                                rms_size,
                                rms_is_deleted,rms_is_revoked,rms_is_shared,                               
                                source_repo_type,
                                source_file_display_path,
                                source_file_path_id,
                                source_repo_name,
                                source_repo_id
                                )
                SELECT
                    @user_table_pk,@path_id,
                    @display_path,@repo_id,
                    @duid,@nxl_name,
                    @last_modified_time,@creation_time,@shared_time,
                    @shared_with_list,@size,
                    @is_deleted,@is_revoked,@is_shared,
                    @source_repo_type,@source_file_display_path,
                    @source_file_path_id,@source_repo_name,@source_repo_id
                    
                WHERE
                       ( SELECT changes() = 0 );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_tb_pk),
                new SQLiteParameter("@path_id",path_id),
                new SQLiteParameter("@display_path",display_path),
                new SQLiteParameter("@repo_id",repo_id),
                new SQLiteParameter("@duid",duid),
                new SQLiteParameter("@nxl_name",nxl_name),

                new SQLiteParameter("@last_modified_time",sharedTimeStr),
                new SQLiteParameter("@creation_time",sharedTimeStr),
                new SQLiteParameter("@shared_time",sharedTimeStr),

                new SQLiteParameter("@shared_with_list",shared_with_list),
                new SQLiteParameter("@size",size),

                new SQLiteParameter("@is_deleted",is_deleted),
                new SQLiteParameter("@is_revoked",is_revoked),
                new SQLiteParameter("@is_shared",is_shared),

                new SQLiteParameter("@source_repo_type",source_repo_type),
                new SQLiteParameter("@source_file_display_path",source_file_display_path),
                new SQLiteParameter("@source_file_path_id",source_file_path_id),
                new SQLiteParameter("@source_repo_name",source_repo_name),
                new SQLiteParameter("@source_repo_id",source_repo_id)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// This method provide query all MyVaultFile sql.
        /// </summary>
        /// <param name="user_id">MyVaultFile belongs to which user.</param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk)
        {
            //query sql string.
            string sql = @"SELECT * FROM 
                                MyVaultFile 
                           WHERE 
                                user_table_pk = @user_table_pk
                           ORDER BY rms_name ASC;
                            ";
            //query sql params need.
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk", user_tb_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        //query data_modified_time
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQl(int user_tb_pk, string nxl_name)
        {
            string sql = @"
                SELECT 
                        rms_last_modified_time 
                FROM 
                        MyVaultFile 
                WHERE 
                        user_table_pk=@user_tb_pk AND rms_name=@nxl_name;
              ";

            SQLiteParameter[] parameters = {
                   new SQLiteParameter("@user_tb_pk" , user_tb_pk),
                   new SQLiteParameter("@nxl_name" , nxl_name)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        //query MyVaultFile
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQl_by_duid(int user_tb_pk, string rms_duid)
        {
            string sql = @"
                SELECT 
                        * 
                FROM 
                        MyVaultFile 
                WHERE 
                        user_table_pk=@user_tb_pk AND rms_duid=@rms_duid;
              ";

            SQLiteParameter[] parameters = {
                   new SQLiteParameter("@user_tb_pk" , user_tb_pk),
                   new SQLiteParameter("@rms_duid" , rms_duid)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Query_Offline_SQL(int user_tb_pk)
        {
            string sql = @"
                SELECT 
                        * 
                FROM 
                        MyVaultFile 
                WHERE 
                        user_table_pk=@user_tb_pk AND is_offline=@is_offline;
              ";

            SQLiteParameter[] parameters = {
                   new SQLiteParameter("@user_tb_pk" , user_tb_pk),
                   new SQLiteParameter("@is_offline" , 1)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_Status_SQL(
            int table_pk, int status)
        {
            string sql = @"UPDATE MyVaultFile
                           SET 
                                operation_status=@status
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@status",status),
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Update_Offline_Mark_SQL(
            int table_pk, bool is_offline)
        {
            string sql = @"UPDATE MyVaultFile
                           SET 
                                is_offline=@is_offline
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@is_offline",is_offline)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Shared_Mark_SQL(
            int table_pk, bool is_shared)
        {
            string sql = @"UPDATE MyVaultFile
                           SET 
                                rms_is_shared=@is_shared
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@is_shared",is_shared)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Nxl_Local_Path(
            int table_pk, string nxl_local_path)
        {
            string sql = @"UPDATE 
                                MyVaultFile
                           SET 
                                local_path=@nxl_local_path
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@nxl_local_path",nxl_local_path)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Nxl_SharedWithList(
            int table_pk, string new_list)
        {
            string sql = @"UPDATE 
                                MyVaultFile
                           SET 
                                rms_shared_with=@new_list
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@new_list",new_list)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Update_EditStatus_SQL(int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    MyVaultFile
                SET
                    edit_status=@newStatus
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@newStatus",newStatus)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Update_ModifiRights_Status_SQL(int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    MyVaultFile
                SET
                    modify_rights_status=@newStatus
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@newStatus",newStatus)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(
          int user_table_pk, string rms_name
          )
        {
            string sql = @"DELETE FROM 
                                MyVaultFile
                           WHERE
                                user_table_pk=@user_tb_pk AND rms_name=@rms_name;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_table_pk),
                new SQLiteParameter("@rms_name",rms_name)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }
    }
}
