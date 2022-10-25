using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.googledrive
{
    public class GoogleDriveLocalFileDao
    {
        /// <summary>
        /// The table is used for storing the local protected files of repository which type is "GoogleDrive",
        /// but user can add or bind multiply different Google Drives, they are stored in 
        /// the same one table.
        /// </summary>
        public static readonly string SQL_Create_Table_GoogleDriveLocalFile = @"
            CREATE TABLE IF NOT EXISTS GoogleDriveLocalFile(
                   id                                 integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   rms_external_repo_table_pk         integer NOT NULL, 

                   ---- which rms folder will hold this file ---- 
                   external_drive_file_table_pk           integer NOT NULL, 

                   nxl_name                           varchar(255) NOT NULL DEFAULT '',
                   nxl_local_path                     varchar(255) NOT NULL DEFAULT '',   
                   last_modified_time                 datetime NOT NULL default (datetime('now','localtime')),
                   file_size                          integer   NOT NULL DEFAULT 0,
                   operation_status                   integer   NOT NULL DEFAULT 3,

                   ---- For uploading native file, maybe also support it in offline model ---- 
                   is_nxl_file                        integer   NOT NULL DEFAULT 1, 

                   ----- reserved ---- 
                    reserved1                   text                DEFAULT '',
                    reserved2                   text                DEFAULT '',
                    reserved3                   text                DEFAULT '',
                    reserved4                   text                DEFAULT '',
                   
                   unique(rms_external_repo_table_pk,external_drive_file_table_pk,nxl_name),
                   foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade,
                   foreign key(external_drive_file_table_pk) references GoogleDriveFile(id) on delete cascade);
        ";


        /// <summary>
        /// Find local files under specified folder(cloudPathId) of repository.
        /// </summary>
        /// <param name="rms_external_repo_table_pk"> primary key of the table </param>
        /// <param name="folderInGoogleDrive">cloudPathId in google drive file</param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL_Under_TargetFolder(
            int rms_external_repo_table_pk, string folderInGoogleDrive)
        {
            string sql = @"
                SELECT  *
                FROM GoogleDriveLocalFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk and
                    external_drive_file_table_pk=(
                                            select 
                                                GoogleDriveFile.id 
                                            from 
                                                GoogleDriveFile 
                                            where 
                                                GoogleDriveFile.rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
                                                GoogleDriveFile.ser_cloud_pathid=@folderInGoogleDrive
                                            );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@folderInGoogleDrive",folderInGoogleDrive),
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
                FROM GoogleDriveLocalFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
            int rms_external_repo_table_pk, string folderGoogledrive, string name,
            string path, int size, DateTime lastModified)
        {
            // Convert
            var cstime = lastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"
                INSERT INTO 
                    GoogleDriveLocalFile(rms_external_repo_table_pk,
                                             external_drive_file_table_pk,
                                             nxl_name,
                                             nxl_local_path,
                                             last_modified_time,
                                             file_size)
                    Values(@rms_external_repo_table_pk,
                           ( SELECT 
                                    GoogleDriveFile.id 
                                FROM 
                                    GoogleDriveFile 
                                WHERE 
                                    GoogleDriveFile.rms_external_repo_table_pk=@rms_external_repo_table_pk  
                                AND  
                                    GoogleDriveFile.ser_cloud_pathid=@folderGoogledrive
                            ),
                            @name,
                            @path,
                            @timestr,
                            @size
                            );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@folderGoogledrive",folderGoogledrive),
                new SQLiteParameter("@name",name),
                new SQLiteParameter("@path",path),
                new SQLiteParameter("@timestr",cstime),
                new SQLiteParameter("@size",size),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
           int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    GoogleDriveLocalFile
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


        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(int table_pk)
        {
            string sql = @"
                DELETE FROM
                    GoogleDriveLocalFile
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
