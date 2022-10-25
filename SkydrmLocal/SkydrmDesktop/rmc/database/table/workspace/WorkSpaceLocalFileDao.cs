using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.workspace
{
    public class WorkSpaceLocalFile
    {
        public int Id { get; set; }
        // foreign key, table owner
        public int User_table_pk { get; set; }
        public int Workspacefile_table_pk { get; set; }

        public string Nxl_name { get; set; }
        public string LocalPath { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public int Status { get; set; }
        public string OriginalPath { get; set; }

        // reserved
        /// <summary>
        /// Reserved1 use for save file already exist in server 
        /// </summary>
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }

        public static WorkSpaceLocalFile NewByReader(SQLiteDataReader reader)
        {
            return new WorkSpaceLocalFile
            {
                Id = int.Parse(reader["id"].ToString()),
                User_table_pk = int.Parse(reader["user_table_pk"].ToString()),
                Workspacefile_table_pk = int.Parse(reader["workspacefile_table_pk"].ToString()),
                Nxl_name = reader["nxl_name"].ToString(),
                LocalPath = reader["nxl_local_path"].ToString(),
                LastModified = DateTime.Parse(reader["last_modified_time"].ToString()).ToUniversalTime(),
                Size = Int64.Parse(reader["file_size"].ToString()),
                Status = int.Parse(reader["operation_status"].ToString()),
                OriginalPath = reader["nxl_original_path"].ToString(),
                // reserved
                Reserved1 = reader["reserved1"].ToString(),
                Reserved2 = "",
                Reserved3 = "",
                Reserved4 = ""
            };
        }
    }

    public class WorkSpaceLocalFileDao
    {
        public static readonly string SQL_Create_Table_WorkSpaceLocalFile = @"
                CREATE TABLE IF NOT EXISTS WorkSpaceLocalFile (
                   id                       integer          NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   user_table_pk            integer          NOT NULL default 0, 

                   ----- which rms folder will hold this file ----- 
                   workspacefile_table_pk   integer          NOT NULL default 0,

                   nxl_name                 varchar(255)     NOT NULL default '',   
                   nxl_local_path           varchar(255)     NOT NULL default '',
                   last_modified_time       datetime         NOT NULL default (datetime('now','localtime')),
                   file_size                integer          NOT NULL default 0,
                   operation_status         integer          NOT NULL default 3, 
                   nxl_original_path        text             DEFAULT '',

                   ----- reserved ---- 
                   reserved1                text             DEFAULT '',
                   reserved2                text             DEFAULT '',
                   reserved3                text             DEFAULT '',
                   reserved4                text             DEFAULT '',

                   UNIQUE(user_table_pk,workspacefile_table_pk,nxl_name), 
                   foreign key(user_table_pk) references User(id) on delete cascade,
                   foreign key(workspacefile_table_pk) references WorkSpaceFile(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> UPSERT_SQL(int user_tb_pk,
            string parentFolder, // Workspace support folder
            string name,
            string localPath,
            DateTime lastModified,
            long size, string reserved1)
        {
            // convert datetime
            string cstime = lastModified.ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"
                UPDATE 
                    WorkSpaceLocalFile
                SET
                    last_modified_time=@last_modified_time,
                    file_size=@size,
                    reserved1=@reserved1
                WHERE
                    user_table_pk=@user_table_pk AND nxl_name=@nxl_name COLLATE NOCASE
                    AND workspacefile_table_pk=
                    ( SELECT
                                  WorkSpaceFile.id
                              FROM
                                  WorkSpaceFile
                              WHERE
                                   WorkSpaceFile.user_table_pk=@user_table_pk  AND  WorkSpaceFile.rms_path_id=@parentFolder
                            );
                
                ---------if no updated happeded, then insert one--------------------------

                INSERT INTO
                    WorkSpaceLocalFile(user_table_pk,
                                    workspacefile_table_pk,
                                    nxl_name,
                                    nxl_local_path,
                                    last_modified_time,
                                    file_size,
                                    nxl_original_path,
                                    reserved1)
                  SELECT
                     @user_table_pk,
                            ( SELECT
                                  WorkSpaceFile.id
                              FROM
                                  WorkSpaceFile
                              WHERE
                                   WorkSpaceFile.user_table_pk=@user_table_pk  AND  WorkSpaceFile.rms_path_id=@parentFolder
                            ),
                            @nxl_name,
                            @nxl_local_path,
                            @last_modified_time,
                            @size,
                            @nxl_original_path,
                            @reserved1
                 WHERE
                       ( SELECT changes() = 0 );  
            ";


            SQLiteParameter[] parameters = {
                    new SQLiteParameter("@user_table_pk",user_tb_pk),
                    new SQLiteParameter("@parentFolder",parentFolder),
                    new SQLiteParameter("@nxl_name",name),
                    new SQLiteParameter("@nxl_local_path",localPath),

                    new SQLiteParameter("@last_modified_time",cstime),
                    new SQLiteParameter("@size",size),

                    new SQLiteParameter("@nxl_original_path",localPath),
                    new SQLiteParameter("@reserved1",reserved1),
                };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);

        }

        /// <summary>
        /// Find workspace files in specified folder.
        /// </summary>
        /// <param name="user_tb_pk"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk, string folderId)
        {
            string sql = @"
                SELECT  *
                FROM WorkSpaceLocalFile
                WHERE
                    user_table_pk=@user_tb_pk and
                    workspacefile_table_pk=(
                                            select 
                                                WorkSpaceFile.id 
                                            from 
                                                WorkSpaceFile 
                                            where 
                                                WorkSpaceFile.user_table_pk=@user_tb_pk  AND 
                                                WorkSpaceFile.rms_path_id=@folderInPorject
                                            );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_tb_pk),
                new SQLiteParameter("@folderInPorject",folderId),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// Query all workspace local files
        /// </summary>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk)
        {
            //query sql string.
            string sql = @"SELECT 
                                * 
                            FROM 
                                WorkSpaceLocalFile 
                            WHERE 
                                user_table_pk = @user_tb_pk;
                            ";
            //query sql params need.
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk", user_tb_pk)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_Status_SQL(
             int tb_pk, int status)
        {
            // for new name has timestamp, so its unique, we can ignore FolderID

            string sql = @"UPDATE 
                                WorkSpaceLocalFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_Original_Path_SQL(int tb_pk, string originalPath)
        {
            string sql = @"UPDATE 
                                WorkSpaceLocalFile
                           SET 
                                nxl_original_path=@nxl_original_path
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk),
                new SQLiteParameter("@nxl_original_path",originalPath)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Name_SQL(
           int tb_pk, string name)
        {
            string sql = @"UPDATE 
                                WorkSpaceLocalFile
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
                                WorkSpaceLocalFile
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
                                WorkSpaceLocalFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL( int table_pk)
        {
            string sql = @"DELETE FROM 
                                WorkSpaceLocalFile
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

    }

}
