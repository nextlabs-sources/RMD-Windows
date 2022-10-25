using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.sharedworkspace
{
    public class SharedWorkspaceLocalFile
    {
        public int Id { get; set; }
        public int RmsExternalRepoTablePk { get; set; }
        public int SharedWorkspaceFileTablePk { get; set; }

        public string Name { get; set; }
        public string LocalPath { get; set; }
        public DateTime ModifiedTime { get; set; }
        public long Size { get; set; }
        public int OperationStatus { get; set; }

        /// <summary>
        /// Reserved1 use for save file already exist in server 
        /// </summary>
        public string Reserved1 { get; set; }

        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }

        public static SharedWorkspaceLocalFile NewByReader(SQLiteDataReader reader)
        {
            SharedWorkspaceLocalFile file = new SharedWorkspaceLocalFile()
            {
                Id = int.Parse(reader["id"].ToString()),
                RmsExternalRepoTablePk = int.Parse(reader["rms_external_repo_table_pk"].ToString()),
                SharedWorkspaceFileTablePk = int.Parse(reader["sharedWorkspaceFile_table_pk"].ToString()),
                Name = reader["nxl_name"].ToString(),
                LocalPath = reader["nxl_local_path"].ToString(),
                ModifiedTime = DateTime.Parse(reader["last_modified_time"].ToString()).ToUniversalTime(),
                Size = long.Parse(reader["file_size"].ToString()),
                OperationStatus = int.Parse(reader["operation_status"].ToString()),
                Reserved1 = reader["reserved1"].ToString(),
                Reserved2 = "",
                Reserved3 = "",
                Reserved4 = ""
            };

            return file;
        }
    }

    public class SharedWorkspaceLocalFileDao
    {
        public static readonly string SQL_Create_Table_SharedWorkspaceLocalFile = @"
            CREATE TABLE IF NOT EXISTS SharedWorkspaceLocalFile(
                   id                                 integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   rms_external_repo_table_pk         integer NOT NULL, 

                   ---- which rms folder will hold this file ---- 
                   sharedWorkspaceFile_table_pk       integer NOT NULL, 

                   nxl_name                           varchar(255) NOT NULL DEFAULT '',
                   nxl_local_path                     varchar(255) NOT NULL DEFAULT '',   
                   last_modified_time                 datetime NOT NULL default (datetime('now','localtime')),
                   file_size                          integer   NOT NULL DEFAULT 0,
                   operation_status                   integer   NOT NULL DEFAULT 3,
                   nxl_original_path                  text             DEFAULT '',

                   ----- reserved ---- 
                    reserved1                   text                DEFAULT '',
                    reserved2                   text                DEFAULT '',
                    reserved3                   text                DEFAULT '',
                    reserved4                   text                DEFAULT '',
                   
                   unique(rms_external_repo_table_pk,sharedWorkspaceFile_table_pk,nxl_name),
                   foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade,
                   foreign key(sharedWorkspaceFile_table_pk) references SharedWorkspaceFile(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
        int rms_external_repo_table_pk, string parentFolder, string name,
        string localPath, int size, DateTime lastModified, string reserved1)
        {
            // Convert
            var cstime = lastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"
                UPDATE 
                    SharedWorkspaceLocalFile
                SET
                    last_modified_time=@timestr,
                    file_size=@size,
                    reserved1=@reserved1
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND nxl_name=@name COLLATE NOCASE
                    AND sharedWorkspaceFile_table_pk=
                    ( SELECT
                                  SharedWorkspaceFile.id
                              FROM
                                  SharedWorkspaceFile
                              WHERE
                                   SharedWorkspaceFile.rms_external_repo_table_pk=@rms_external_repo_table_pk  AND  SharedWorkspaceFile.rms_path=@folderPath
                            );
                
                ---------if no updated happeded, then insert one--------------------------

                INSERT INTO
                    SharedWorkspaceLocalFile(rms_external_repo_table_pk,
                                    sharedWorkspaceFile_table_pk,
                                    nxl_name,
                                    nxl_local_path,
                                    last_modified_time,
                                    file_size,
                                    nxl_original_path,
                                    reserved1)
                  SELECT
                     @rms_external_repo_table_pk,
                            ( SELECT
                                  SharedWorkspaceFile.id
                              FROM
                                  SharedWorkspaceFile
                              WHERE
                                  SharedWorkspaceFile.rms_external_repo_table_pk=@rms_external_repo_table_pk  AND  SharedWorkspaceFile.rms_path=@folderPath
                            ),
                            @name,
                            @localPath,
                            @timestr,
                            @size,
                            @nxl_original_path,
                            @reserved1
                 WHERE
                       ( SELECT changes() = 0 );  
            ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@folderPath",parentFolder),
                new SQLiteParameter("@name",name),
                new SQLiteParameter("@localPath",localPath),
                new SQLiteParameter("@timestr",cstime),
                new SQLiteParameter("@size",size),
                new SQLiteParameter("@nxl_original_path",localPath),
                new SQLiteParameter("@reserved1",reserved1),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        /// <summary>
        /// Find local files under specified folder(rms_path) of repository.
        /// </summary>
        /// <param name="rms_external_repo_table_pk"> primary key of the table </param>
        /// <param name="folderPath">path</param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL_Under_TargetFolder(
            int rms_external_repo_table_pk, string folderPath)
        {
            string sql = @"
                SELECT  *
                FROM SharedWorkspaceLocalFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk and
                    sharedWorkspaceFile_table_pk=(
                                            select 
                                                SharedWorkspaceFile.id 
                                            from 
                                                SharedWorkspaceFile 
                                            where 
                                                SharedWorkspaceFile.rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
                                                SharedWorkspaceFile.rms_path=@folderPath
                                            );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@folderPath",folderPath),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        /// <summary>
        /// Find all local files of specified repository.
        /// </summary>
        /// <param name="rms_external_repo_table_pk"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
             int rms_external_repo_table_pk)
        {
            string sql = @"
                SELECT  *
                FROM SharedWorkspaceLocalFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
            int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    SharedWorkspaceLocalFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Name_SQL(
            int tb_pk, string name)
        {
            string sql = @"UPDATE 
                                SharedWorkspaceLocalFile
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
                                SharedWorkspaceLocalFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_Original_Path_SQL(int tb_pk, string originalPath)
        {
            string sql = @"UPDATE 
                                SharedWorkspaceLocalFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Reserved1_SQL(
          int tb_pk, string reserved1)
        {
            string sql = @"UPDATE 
                                SharedWorkspaceLocalFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(int table_pk)
        {
            string sql = @"
                DELETE FROM
                    SharedWorkspaceLocalFile
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

    }
}
