using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.oneDrive
{
    public class OneDriveLocalFileDao
    {
        /// <summary>
        /// The table is used for storing the local protected files of repository which type is "OneDrive",
        /// but user can add or bind multiply different OneDrives, they are stored in 
        /// the same one table.
        /// </summary>
        public static readonly string SQL_Create_Table_OneDriveLocalFile = @"
            CREATE TABLE IF NOT EXISTS OneDriveLocalFile(
                   id                                 integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
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
                   
                   unique(rms_external_repo_table_pk,external_drive_file_table_pk, nxl_local_path),
                   foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade,
                   foreign key(external_drive_file_table_pk) references OneDriveFileCommon(id) on delete cascade);
                ";


        /// <summary>
        /// Find local files under specified folder(cloudPathId) of repository.
        /// </summary>
        /// <param name="rms_external_repo_table_pk"> primary key of the table </param>
        /// <param name="folderInOneDrive">cloudPathId in OneDrive file</param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL_Under_TargetFolder(int rms_external_repo_table_pk, string parent_item_id)
        {
            string sql = @"
                SELECT *
                FROM OneDriveLocalFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk 
                AND
                    reserved1=@parent_item_id;
               ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@parent_item_id",parent_item_id),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// Find all local files of specified repository.
        /// </summary>
        /// <param name="rms_external_repo_table_pk"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int rms_external_repo_table_pk)
        {
            string sql = @"
                SELECT  *
                FROM OneDriveLocalFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(int rms_external_repo_table_pk, string parentItemId, string name,
                                                                         string path, int size, DateTime lastModified)
        {
            // Convert
            var cstime = lastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"
                INSERT INTO 
                    OneDriveLocalFile(rms_external_repo_table_pk,
                                        external_drive_file_table_pk,
                                        nxl_name,
                                        nxl_local_path,
                                        last_modified_time,
                                        file_size,
                                        reserved1
                                        )
                    Values(@rms_external_repo_table_pk,
                           ( SELECT 
                                    OneDriveFileCommon.id 
                                FROM 
                                    OneDriveFileCommon 
                                WHERE 
                                    OneDriveFileCommon.rms_external_repo_table_pk=@rms_external_repo_table_pk  
                                AND  
                                    OneDriveFileCommon.item_id=@parentItemId
                            ),
                            @nxl_name,
                            @nxl_local_path,
                            @last_modified_time,
                            @file_size,
                            @reserved1
                            );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@nxl_name",name),
                new SQLiteParameter("@nxl_local_path",path),
                new SQLiteParameter("@last_modified_time",cstime),
                new SQLiteParameter("@file_size",size),
                new SQLiteParameter("@reserved1",parentItemId),
                new SQLiteParameter("@parentItemId",parentItemId),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    OneDriveLocalFile
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
                    OneDriveLocalFile
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        //public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(int table_pk)
        //{
        //    string sql = @"
        //        DELETE FROM
        //            OneDriveLocalFile
        //        WHERE
        //            id=@table_pk;
        //    ";
        //    SQLiteParameter[] parameters = {
        //        new SQLiteParameter("@table_pk",table_pk),
        //    };
        //    return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        //}


    }
}
