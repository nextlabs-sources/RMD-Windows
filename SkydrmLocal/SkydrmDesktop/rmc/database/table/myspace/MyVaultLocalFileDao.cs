using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database.table.myvault
{
    public class MyVaultLocalFile
    {
        private int id;
        private int user_table_pk;

        private string nxl_name;
        private string nxl_local_path;
        private DateTime last_modified_time;
        private string nxl_shared_with_list;
        private long file_size;
        private int operation_status;

        // 05/30/2019 add comment colunmn for upload.
        private string comment;
        private string originalPath;
        // extend
        private string reserved1;
        private string reserved2;
        private string reserved3;
        private string reserved4;
        private string reserved5;

        public int Id { get => id; set => id = value; }
        public int User_Table_Pk { get => user_table_pk; set => user_table_pk = value; }
        public string Nxl_Name { get => nxl_name; set => nxl_name = value; }
        public string Nxl_Local_Path { get => nxl_local_path; set => nxl_local_path = value; }
        public DateTime Last_Modified_Time { get => last_modified_time; set => last_modified_time = value; }
        public string Shared_With_List { get => nxl_shared_with_list; set => nxl_shared_with_list = value; }
        public long Size { get => file_size; set => file_size = value; }
        public int Status { get => operation_status; set => operation_status = value; }

        public string Comment { get => comment; set => comment = value; }
        /// <summary>
        /// File source path before encryption
        /// </summary>
        public string OriginalPath { get => originalPath; set => originalPath = value; }

        // Reserved
        /// <summary>
        /// Reserved1 use for save file already exist in server 
        /// </summary>
        public string Reserved1 { get => reserved1; set => reserved1 = value; }
        public string Reserved2 { get => reserved2; set => reserved2 = value; }
        public string Reserved3 { get => reserved3; set => reserved3 = value; }
        public string Reserved4 { get => reserved4; set => reserved4 = value; }
        public string Reserved5 { get => reserved5; set => reserved5 = value; }

        public static MyVaultLocalFile NewByReader(SQLiteDataReader reader)
        {
            return new MyVaultLocalFile
            {
                Id = int.Parse(reader["id"].ToString()),
                User_Table_Pk = int.Parse(reader["user_table_pk"].ToString()),
                Nxl_Name = reader["nxl_name"].ToString(),
                Nxl_Local_Path = reader["nxl_local_path"].ToString(),
                Last_Modified_Time = DateTime.Parse(reader["last_modified_time"].ToString()).ToUniversalTime(),
                Shared_With_List = reader["nxl_shared_with_list"].ToString(),
                Size = Int64.Parse(reader["file_size"].ToString()),
                Status = int.Parse(reader["operation_status"].ToString()),
                Comment=reader["nxl_comment"].ToString(),
                OriginalPath=reader["nxl_original_path"].ToString(),
                Reserved1 = reader["reserved1"].ToString(),
                Reserved2 = "",
                Reserved3 = "",
                Reserved4 = "",
                Reserved5 = ""
            }; 
        }

    }

    public class MyVaultLocalFileDao
    {
        public static readonly string SQL_Create_Table_MyVaultLocalFile = @"
                CREATE TABLE IF NOT EXISTS MyVaultLocalFile (
                   id                       integer          NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   user_table_pk            integer          NOT NULL default 0, 
                   nxl_name                 varchar(255)     NOT NULL default '',   
                   nxl_local_path           varchar(255)     NOT NULL default '',
                   last_modified_time       datetime         NOT NULL default (datetime('now','localtime')),
                   nxl_shared_with_list     varchar(255)     NOT NULL default '', 
                   file_size                integer          NOT NULL default 0,
                   operation_status         integer          NOT NULL default 3, 
                   
                   ----- V3 added -----------
                   nxl_comment              text             DEFAULT '',
                   nxl_original_path        text             DEFAULT '',

                   ----- V4 added -----------
                   reserved1                text             DEFAULT '',
                   reserved2                text             DEFAULT '',
                   reserved3                text             DEFAULT '',
                   reserved4                text             DEFAULT '',
                   reserved5                text             DEFAULT '',

                   UNIQUE(user_table_pk,nxl_name), 
                   foreign key(user_table_pk) references User(id) on delete cascade);
        ";

        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Comment_V3 = @"
                   ALTER TABLE MyVaultLocalFile ADD COLUMN 
                        nxl_comment        text       DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_OriginalPath_V3 = @"
                   ALTER TABLE MyVaultLocalFile ADD COLUMN 
                         nxl_original_path    text       DEFAULT '';
        ";

        // Extend db table reserved field (version 4).
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved1_V4 = @"
                   ALTER TABLE MyVaultLocalFile ADD COLUMN 
                        reserved1               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved2_V4 = @"
                   ALTER TABLE MyVaultLocalFile ADD COLUMN 
                        reserved2               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved3_V4 = @"
                   ALTER TABLE MyVaultLocalFile ADD COLUMN 
                        reserved3               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved4_V4 = @"
                   ALTER TABLE MyVaultLocalFile ADD COLUMN 
                        reserved4               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved5_V4 = @"
                   ALTER TABLE MyVaultLocalFile ADD COLUMN 
                        reserved5               text         DEFAULT '';
        ";

        public static KeyValuePair<String, SQLiteParameter[]> UPSET_SQL(int user_tb_pk,
            string nxl_name, string nxl_local_path,
            DateTime last_modified_time,
            string shared_with_list,
            long size,
            int status,
            string comment,
            string originalPath, string reserved1)
        {
            // convert datetime
            string cstime = last_modified_time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = @"
                UPDATE 
                    MyVaultLocalFile
                SET
                    last_modified_time=@last_modified_time,
                    nxl_shared_with_list=@shared_with_list,
                    file_size=@size,
                    nxl_comment=@nxl_comment,
                    nxl_original_path=@nxl_original_path,
                    reserved1=@reserved1
                WHERE
                    user_table_pk=@user_table_pk AND nxl_name=@nxl_name COLLATE NOCASE;
                
                ---------if no updated happeded, then insert one--------------------------

                INSERT INTO
                    MyVaultLocalFile(user_table_pk,
                                    nxl_name,
                                    nxl_local_path,
                                    last_modified_time,
                                    nxl_shared_with_list,
                                    file_size,
                                    operation_status,
                                    nxl_comment,
                                    nxl_original_path,
                                    reserved1)
                SELECT
                    @user_table_pk,@nxl_name,@nxl_local_path,
                    @last_modified_time,
                    @shared_with_list,@size,
                    @status,@nxl_comment,
                    @nxl_original_path,
                    @reserved1
                WHERE
                       ( SELECT changes() = 0 );
            ";


            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_tb_pk),
                new SQLiteParameter("@nxl_name",nxl_name),
                new SQLiteParameter("@nxl_local_path",nxl_local_path),

                new SQLiteParameter("@last_modified_time",cstime),

                new SQLiteParameter("@shared_with_list",shared_with_list),
                new SQLiteParameter("@size",size),

                new SQLiteParameter("@status",status),

                new SQLiteParameter("@nxl_comment",comment),
                new SQLiteParameter("@nxl_original_path",originalPath),
                new SQLiteParameter("@reserved1",reserved1),
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// This method provide query all MyVaultLocalFile sql.
        /// </summary>
        /// <param name="user_id">MyVaultLocalFile belongs to which user.</param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk)
        {
            //query sql string.
            string sql = @"SELECT 
                                * 
                            FROM 
                                MyVaultLocalFile 
                            WHERE 
                                user_table_pk = @user_tb_pk;
                            ";
            //query sql params need.
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk", user_tb_pk)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String,SQLiteParameter[]> Query_SQL(int user_tb_pk,string nxl_name)
        {
            //query sql string.
            string sql = @"SELECT 
                                nxl_shared_with_list
                           FROM 
                                MyVaultLocalFile 
                           WHERE 
                                user_table_pk = @user_tb_pk AND nxl_name=@nxl_name
                          ";
            //query sql params need.
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk", user_tb_pk),
                new SQLiteParameter("@nxl_name", nxl_name)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// This method providers Update MyVaultLocalFile item status SQL.
        /// </summary>
        /// <param name="user_primary_key"></param>
        /// <param name="file_name"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_Status_SQL(
            int user_tb_pk, string nxl_name, int status)
        {
            string sql = @"UPDATE 
                                MyVaultLocalFile
                           SET 
                                operation_status=@status
                           WHERE
                                user_table_pk=@user_tb_pk AND nxl_name=@nxl_name;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_tb_pk),
                new SQLiteParameter("@nxl_name",nxl_name),
                new SQLiteParameter("@status",status)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        //public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_Status_Modified_Time(int user_tb_pk, string nxl_name, DateTime status_modified_time)
        //{
        //    string sql = @"UPDATE 
        //                        MyVaultLocalFile
        //                   SET 
        //                        status_modified_time=@status_modified_time
        //                   WHERE
        //                        user_table_pk=@user_tb_pk AND nxl_name=@nxl_name;
        //                ";

        //    SQLiteParameter[] parameters = {
        //        new SQLiteParameter("@user_tb_pk",user_tb_pk),
        //        new SQLiteParameter("@nxl_name",nxl_name),
        //        new SQLiteParameter("@status_modified_time",status_modified_time)
        //    };

        //    return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        //}

        /// <summary>
        /// This method providers Update MyVaultLocalFile item shared with SQL.
        /// </summary>
        /// <param name="user_primary_key"></param>
        /// <param name="file_name"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Update_Shared_With_SQL(int user_tb_pk, string nxl_name, string shared_with_list)
        {
            string sql = @"UPDATE 
                                MyVaultLocalFile
                           SET 
                                nxl_shared_with_list=@shared_with_list
                           WHERE
                                user_table_pk=@user_tb_pk AND nxl_name=@nxl_name;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_tb_pk),
                new SQLiteParameter("@nxl_name",nxl_name),
                new SQLiteParameter("@shared_with_list",shared_with_list)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// This method providers Update MyVaultLocalFile item original path SQL.
        /// </summary>
        /// <param name="user_primary_key"></param>
        /// <param name="file_name"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Update_Original_Path_SQL(int user_tb_pk, string nxl_name, string originalPath)
        {
            string sql = @"UPDATE 
                                MyVaultLocalFile
                           SET 
                                nxl_original_path=@nxl_original_path
                           WHERE
                                user_table_pk=@user_tb_pk AND nxl_name=@nxl_name;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_tb_pk),
                new SQLiteParameter("@nxl_name",nxl_name),
                new SQLiteParameter("@nxl_original_path",originalPath)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Name_SQL(
      int tb_pk, string name)
        {
            string sql = @"UPDATE 
                                MyVaultLocalFile
                           SET 
                                nxl_name=@name
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk),
                new SQLiteParameter("@name",name)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_LocalPath_SQL(
            int tb_pk, string localPath)
        {
            string sql = @"UPDATE 
                                MyVaultLocalFile
                           SET 
                                nxl_local_path=@path
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk),
                new SQLiteParameter("@path",localPath)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Reserved1_SQL(
          int tb_pk, string reserved1)
        {
            string sql = @"UPDATE 
                                MyVaultLocalFile
                           SET 
                                reserved1=@reserved1
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk),
                new SQLiteParameter("@reserved1", reserved1)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String,SQLiteParameter[]> Delete_Item_SQL(int user_tb_pk, string nxl_name)
        {
            string sql = @"DELETE FROM 
                                MyVaultLocalFile
                           WHERE
                                user_table_pk=@user_tb_pk AND nxl_name=@nxl_name;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_tb_pk),
                new SQLiteParameter("@nxl_name",nxl_name)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }
    }
}
