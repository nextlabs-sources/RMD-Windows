using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.database
{
    public class SharedWithMeFile
    {
        private int id;
        private int user_table_pk;
        private string duid;
        private string name;
        private string type;
        private Int64 size;
        private DateTime shared_date;
        private string shared_by;
        private string transaction_id;
        private string transaction_code;
        private string shared_link_url;
        private string rights_json;
        private string comments;
        private bool is_owner;
        //local feature
        private bool is_offline;
        private string local_path;
        private int operation_status;

        // 3/27/2019 new added support edit file&modify rights feature.
        private int edit_status;
        private int modify_rights_status;
        private string reserved1;
        private string reserved2;
        private string reserved3;
        private string reserved4;

        public int Id { get => id; set => id = value; }
        public int User_table_pk { get => user_table_pk; set => user_table_pk = value; }
        public string Duid { get => duid; set => duid = value; }
        public string Name { get => name; set => name = value; }
        public string Type { get => type; set => type = value; }
        public long Size { get => size; set => size = value; }
        public DateTime Shared_date { get => shared_date; set => shared_date = value; }
        public string Shared_by { get => shared_by; set => shared_by = value; }
        public string Transaction_id { get => transaction_id; set => transaction_id = value; }
        public string Transaction_code { get => transaction_code; set => transaction_code = value; }
        public string Shared_link_url { get => shared_link_url; set => shared_link_url = value; }
        public string Rights_json { get => rights_json; set => rights_json = value; }
        public string Comments { get => comments; set => comments = value; }
        public bool Is_owner { get => is_owner; set => is_owner = value; }
        public bool Is_offline { get => is_offline; set => is_offline = value; }
        public string Local_path { get => local_path; set => local_path = value; }
        public int Operation_status { get => operation_status; set => operation_status = value; }

        public int Edit_Status { get => edit_status; set => edit_status = value; }
        public int Modify_Rights_Status { get => modify_rights_status; set => modify_rights_status = value; }

        public string Reserved1 { get => reserved1; set => reserved1 = value; }
        public string Reserved2 { get => reserved2; set => reserved2 = value; }
        public string Reserved3 { get => reserved3; set => reserved3 = value; }
        public string Reserved4 { get => reserved4; set => reserved4 = value; }

        public static SharedWithMeFile NewByReader(SQLiteDataReader reader)
        {
            return new SharedWithMeFile()
            {
                Id = int.Parse(reader["id"].ToString()),
                User_table_pk = int.Parse(reader["user_table_pk"].ToString()),
                Duid = reader["rms_duid"].ToString(),
                Name = reader["rms_name"].ToString(),
                Type = reader["rms_file_type"].ToString(),
                Size = long.Parse(reader["rms_file_size"].ToString()),
                Shared_date = new DateTime(long.Parse(reader["rms_shared_date"].ToString())),
                Shared_by = reader["rms_shared_by"].ToString(),
                Transaction_id = reader["rms_transaction_id"].ToString(),
                Transaction_code = reader["rms_transaction_code"].ToString(),
                Shared_link_url = reader["rms_shared_link_url"].ToString(),
                Rights_json = reader["rms_rights_json"].ToString(),
                Comments = reader["rms_comments"].ToString(),
                Is_owner = int.Parse(reader["rms_is_owner"].ToString()) == 1 ? true : false,
                // local feature
                Is_offline = int.Parse(reader["is_offline"].ToString()) == 1 ? true : false,
                Local_path = reader["local_path"].ToString(),
                Operation_status = int.Parse(reader["operation_status"].ToString()),

                Edit_Status = int.Parse(reader["edit_status"].ToString()),
                Modify_Rights_Status = int.Parse(reader["modify_rights_status"].ToString()),
                Reserved1 = "",
                Reserved2 = "",
                Reserved3 = "",
                Reserved4 = ""
            };
        }

    }
    public class SharedWithMeFileDao
    {
        public static readonly string SQL_Create_Table_SharedWithMeFileDao = @"
        CREATE TABLE IF NOT EXISTS SharedWithMeFile(
               id                  integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE ,
               user_table_pk       integer      NOT NULL, 
               rms_duid                varchar(255) NOT NULL,
               rms_name            varchar(255) DEFAULT '',
               rms_file_type           varchar(255) DEFAULT '',
               rms_file_size           integer DEFAULT 0,
               rms_shared_date     integer DEFAULT 0,
               rms_shared_by           varchar(255) DEFAULT '',
               rms_transaction_id  varchar(255) DEFAULT '',
               rms_transaction_code             varchar(255) DEFAULT '',
               rms_shared_link_url                  varchar(255) DEFAULT '',
               rms_rights_json                      varchar(255) DEFAULT '',
               rms_comments                         varchar(255) DEFAULT '',
               rms_is_owner                          integer DEFAULT 0,
               -- local feature--
               is_offline                integer DEFAULT 0, 
               local_path                varchar(255) DEFAULT '', 
               operation_status          integer   DEFAULT 4,    

               ----- V2 added -----------
               edit_status                 integer             DEFAULT 0,
               modify_rights_status        integer             DEFAULT 0,
               reserved1                   text                DEFAULT '',
               reserved2                   text                DEFAULT '',
               reserved3                   text                DEFAULT '',
               reserved4                   text                DEFAULT '',

               --talbe restrictions--
               unique(user_table_pk,rms_duid), 
               foreign key(user_table_pk) references User(id) on delete cascade
        );
        ";
        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Edit_Status_V2 = @"
                   ALTER TABLE SharedWithMeFile ADD COLUMN 
                        edit_status             integer     DEFAULT 0;
        ";

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_ModifyRights_Status_V2 = @"
                   ALTER TABLE SharedWithMeFile ADD COLUMN 
                        modify_rights_status    integer     DEFAULT 0;
        ";

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Reserved1_V2 = @"
                   ALTER TABLE SharedWithMeFile ADD COLUMN 
                        reserved1               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Reserved2_V2 = @"
                   ALTER TABLE SharedWithMeFile ADD COLUMN 
                        reserved2               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Reserved3_V2 = @"
                   ALTER TABLE SharedWithMeFile ADD COLUMN 
                        reserved3               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Reserved4_V2 = @"
                   ALTER TABLE SharedWithMeFile ADD COLUMN 
                        reserved4               text         DEFAULT '';
        ";

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
            int user_table_pk, string duid, string name,
            string type, Int64 size, Int64 shared_date, string shared_by,
            string transcation_id, string transcation_code,
            string shared_link_url, string rights_json, string comments,
            bool is_owner)
        {
            string sql = @"
                UPDATE SharedWithMeFile 
                SET 
                    user_table_pk=@user_table_pk,
                    rms_duid=@duid,
                    rms_name=@name,
                    rms_file_type=@type,
                    rms_file_size=@size,
                    rms_shared_date=@shared_date,
                    rms_shared_by=@shared_by,
                    rms_transaction_id=@transcation_id,
                    rms_transaction_code=@transcation_code,
                    rms_shared_link_url=@shared_link_url,
                    rms_rights_json=@rights_json,
                    rms_comments=@comments,
                    rms_is_owner=@is_owner
                WHERE
                    user_table_pk = @user_table_pk AND rms_duid=@duid;
                ---------if no updated happeded, then insert one--------------------------
                    INSERT INTO  
                            SharedWithMeFile(
                                        user_table_pk,
                                        rms_duid,rms_name,
                                        rms_file_type,rms_file_size,
                                        rms_shared_date,rms_shared_by,
                                        rms_transaction_id,rms_transaction_code,
                                        rms_shared_link_url,rms_rights_json,
                                        rms_comments,rms_is_owner
                                        )
                    SELECT 
                            @user_table_pk,
                            @duid,
                            @name,
                            @type,
                            @size,
                            @shared_date,
                            @shared_by,
                            @transcation_id,
                            @transcation_code,
                            @shared_link_url,
                            @rights_json,
                            @comments,
                            @is_owner
                    WHERE
                        ( SELECT changes() = 0 );
                
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk),
                new SQLiteParameter("@duid",duid),
                new SQLiteParameter("@name",name),
                new SQLiteParameter("@type",type),
                new SQLiteParameter("@size",size),
                new SQLiteParameter("@shared_date",shared_date),
                new SQLiteParameter("@shared_by",shared_by),
                new SQLiteParameter("@transcation_id",transcation_id),
                new SQLiteParameter("@transcation_code",transcation_code),
                new SQLiteParameter("@shared_link_url",shared_link_url),
                new SQLiteParameter("@rights_json",rights_json),
                new SQLiteParameter("@comments",comments),
                new SQLiteParameter("@is_owner",is_owner),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
            int user_table_pk)
        {
            string sql = @"
               SELECT *
               FROM  SharedWithMeFile
               WHERE user_table_pk=@user_table_pk
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_LocalPath_SQL(
            int table_pk, string newPath)
        {
            string sql = @"
                UPDATE 
                    SharedWithMeFile
                SET
                    local_path=@newPath
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@newPath",newPath),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
            int table_pk,
            int newStatus
            )
        {
            string sql = @"
                UPDATE 
                    SharedWithMeFile
                SET
                    operation_status=@newStatus
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@newStatus",newStatus)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Update_IsOffline_SQL(
            int table_pk,
            bool isOffline
            )
        {
            string sql = @"
                UPDATE 
                    SharedWithMeFile
                SET
                    is_offline=@isOffline
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@isOffline",isOffline?1:0)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(
            int user_table_pk, string rms_name
            )
        {
            string sql = @"DELETE FROM 
                                SharedWithMeFile
                           WHERE
                                user_table_pk=@user_tb_pk AND rms_name=@rms_name;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_table_pk),
                new SQLiteParameter("@rms_name",rms_name)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Update_EditStatus_SQL(int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    SharedWithMeFile
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
                    SharedWithMeFile
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
    }
}
