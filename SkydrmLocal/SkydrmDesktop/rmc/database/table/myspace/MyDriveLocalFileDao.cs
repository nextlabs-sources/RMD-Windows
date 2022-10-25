using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.myspace
{
    public class MyDriveLocalFile
    {
        public int Id { get; set; }
        public int User_Table_Pk { get; set; }
        public int MyDriveFile_Table_Pk { get; set; }
        public string Name { get; set; }
        public string LocalPath { get; set; }
        public DateTime Last_Modified_Time { get; set; }
        public long Size { get; set; }
        public int OperationStatus { get; set; }

        public bool IsNxlFile { get; set; }

        // Reserved
        /// <summary>
        /// Reserved1 use for save file already exist in server 
        /// </summary>
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }

        public static MyDriveLocalFile NewByReader(SQLiteDataReader reader)
        {
            return new MyDriveLocalFile
            {
                Id = int.Parse(reader["id"].ToString()),
                User_Table_Pk = int.Parse(reader["user_table_pk"].ToString()),
                MyDriveFile_Table_Pk = int.Parse(reader["mydrivefile_table_pk"].ToString()),
                Name = reader["nxl_name"].ToString(),
                LocalPath = reader["nxl_local_path"].ToString(),
                Last_Modified_Time = DateTime.Parse(reader["last_modified_time"].ToString()).ToUniversalTime(),
                Size = Int64.Parse(reader["file_size"].ToString()),
                OperationStatus = int.Parse(reader["operation_status"].ToString()),
                IsNxlFile = int.Parse(reader["is_nxl_file"].ToString()) == 1,
                Reserved1= reader["reserved1"].ToString()
            };
        }

    }

    public class MyDriveLocalFileDao
    {
        public static readonly string SQL_Create_Table_MyDriveLocalFile = @"
                CREATE TABLE IF NOT EXISTS MyDriveLocalFile (
                   id                       integer          NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   user_table_pk            integer          NOT NULL default 0,
                    
                   ----- which rms folder will hold this file -----                    
                   mydrivefile_table_pk        integer          NOT NULL default 0,
                   
                   nxl_name                 varchar(255)     NOT NULL default '',   
                   nxl_local_path           varchar(255)     NOT NULL default '',
                   last_modified_time       datetime         NOT NULL default (datetime('now','localtime')),
                   file_size                integer          NOT NULL default 0,
                   operation_status         integer          NOT NULL default 3, 
                   
                   is_nxl_file                        integer   NOT NULL DEFAULT 0, 

                   ----- reserved ---- 
                    reserved1                   text                DEFAULT '',
                    reserved2                   text                DEFAULT '',
                    reserved3                   text                DEFAULT '',
                    reserved4                   text                DEFAULT '',

                   UNIQUE(user_table_pk, mydrivefile_table_pk, nxl_name), 
                   foreign key(user_table_pk) references User(id) on delete cascade,
                   foreign key(mydrivefile_table_pk) references MyDriveFile(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> UPSET_SQL(int user_tb_pk, string parentFolder,
            string nxl_name, string nxl_local_path,
            DateTime last_modified_time,
            long size, string reserved1, int isNxl = 0)
        {
            // convert datetime
            string cstime = last_modified_time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = @"
                UPDATE 
                    MyDriveLocalFile
                SET
                    last_modified_time=@last_modified_time,
                    file_size=@size,
                    reserved1=@reserved1
                WHERE
                    user_table_pk=@user_table_pk AND nxl_name=@nxl_name  COLLATE NOCASE  AND mydrivefile_table_pk=
                    (SELECT
                                  MyDriveFile.id
                              FROM
                                  MyDriveFile
                              WHERE
                                   MyDriveFile.user_table_pk=@user_table_pk  AND  MyDriveFile.rms_path_id=@parentFolder);
                
                ---------if no updated happeded, then insert one--------------------------

                INSERT INTO
                    MyDriveLocalFile(user_table_pk,
                                    mydrivefile_table_pk,
                                    nxl_name,
                                    nxl_local_path,
                                    last_modified_time,
                                    file_size,
                                    is_nxl_file,
                                    reserved1)
                SELECT
                    @user_table_pk,
                    (SELECT
                                  MyDriveFile.id
                              FROM
                                  MyDriveFile
                              WHERE
                                   MyDriveFile.user_table_pk=@user_table_pk  AND  MyDriveFile.rms_path_id=@parentFolder),
                    @nxl_name,@nxl_local_path,
                    @last_modified_time,@size,
                    @is_nxl_file,
                    @reserved1
                WHERE
                       ( SELECT changes() = 0 );
            ";


            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_tb_pk),
                new SQLiteParameter("@parentFolder",parentFolder),
                new SQLiteParameter("@nxl_name",nxl_name),
                new SQLiteParameter("@nxl_local_path",nxl_local_path),

                new SQLiteParameter("@last_modified_time",cstime),
                new SQLiteParameter("@size",size),
                
                new SQLiteParameter("@is_nxl_file",isNxl),
                new SQLiteParameter("@reserved1",reserved1),
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// Query specified folder MyDriveLocalFile sql.
        /// </summary>
        /// <param name="user_id">MyDriveLocalFile belongs to which user.</param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk, string folderId)
        {
            //query sql string.
            string sql = @"SELECT 
                                * 
                            FROM 
                                MyDriveLocalFile 
                            WHERE 
                                user_table_pk = @user_tb_pk and
                                mydrivefile_table_pk=(
                             SELECT
                                  MyDriveFile.id
                              FROM
                                  MyDriveFile
                              WHERE
                                   MyDriveFile.user_table_pk=@user_tb_pk  AND  MyDriveFile.rms_path_id=@parentFolder);
                            ";
            //query sql params need.
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk", user_tb_pk),
                new SQLiteParameter("@parentFolder",folderId)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// Query all MyDriveLocalFile
        /// </summary>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk)
        {
            //query sql string.
            string sql = @"SELECT 
                                * 
                            FROM 
                                MyDriveLocalFile 
                            WHERE 
                                user_table_pk = @user_tb_pk;
                            ";
            //query sql params need.
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk", user_tb_pk)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// This method providers Update MyDriveLocalFile item status SQL.
        /// </summary>
        /// <param name="user_primary_key"></param>
        /// <param name="file_name"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Status_SQL(
            int tb_pk, int status)
        {
            string sql = @"UPDATE 
                                MyDriveLocalFile
                           SET 
                                operation_status=@status
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk),
                new SQLiteParameter("@status",status)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Name_SQL(
            int tb_pk, string name)
        {
            string sql = @"UPDATE 
                                MyDriveLocalFile
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
                                MyDriveLocalFile
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
                                MyDriveLocalFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(int tb_pk)
        {
            string sql = @"DELETE FROM 
                                MyDriveLocalFile
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }
    }
}
