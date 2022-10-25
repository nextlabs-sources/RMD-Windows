using SkydrmLocal.rmc.common.helper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo
{
    public class GooglDriveFileDao
    {
        /// <summary>
        /// The table is used for storing the files of repository which type is "GoogleDrive",
        /// but user can add or bind multiply different Google Drives, they are stored in 
        /// the same one table.
        /// </summary>
        public static readonly string SQL_Create_Table_GoogleDriveFile = @"
            CREATE TABLE IF NOT EXISTS GoogleDriveFile (
                id                          integer             NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                rms_external_repo_table_pk  integer             default 0, 

                ------ server returned -----
                ser_file_id                 varchar(255)        NOT NULL default '',
                ser_isFolder                integer             NOT NULL default 0,
                ser_file_name               varchar(255)        NOT NULL default '',
                ser_size                    integer             NOT NULL default 0,
                ser_modified_time           integer             NOT NULL default 0,
                ser_display_path            varchar(255)        NOT NULL default '',
                ser_cloud_pathid            varchar(255)        NOT NULL default '',
           
                ------ local added -----
                is_offline                  integer             NOT NULL default 0,
                is_favorite                 integer             NOT NULL default 0,
                local_path                  varchar(255)        NOT NULL default '',
                is_nxl_file                 integer             NOT NULL default 0,
                status                      integer             NOT NULL default 4,
                custom_string               varchar(255)        NOT NULL default '',
                edit_status                 integer             NOT NULL default 0,
                modify_rights_status        integer             NOT NULL default 0,

               ----- reserved ---- 
                reserved1                   text                DEFAULT '',
                reserved2                   text                DEFAULT '',
                reserved3                   text                DEFAULT '',
                reserved4                   text                DEFAULT '',

                UNIQUE(rms_external_repo_table_pk, ser_file_id),
                foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> Delete_File_SQL(
            int rms_external_repo_table_pk,
            string ser_file_id)
        {
            string sql = @"
                DELETE FROM 
                    GoogleDriveFile
                WHERE 
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
                    ser_file_id=@ser_file_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@ser_file_id",ser_file_id),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Delete_Folder_And_SubChildren_SQL(
            int rms_external_repo_table_pk,
            string ser_cloud_pathid)
        {
            string sql = @"
                DELETE FROM 
                    GoogleDriveFile
                WHERE 
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
                    ser_cloud_pathid like @ser_cloud_pathid
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@ser_cloud_pathid",ser_cloud_pathid+'%'),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
            int rms_external_repo_table_pk, string ser_file_id, int ser_isFolder,
            string ser_file_name, long ser_size, long ser_modified_time, string ser_display_path,
            string ser_cloud_pathid, int is_nxl)
        {
            // Need to convert, or else, will failed when get from db(since sql parse exception: invalid date)
            var convertedLastModified = JavaTimeConverter.ToCSLongTicks(ser_modified_time);

            string sql = @"
                    UPDATE GoogleDriveFile 
                    SET 
                        ser_file_id=@ser_file_id,
                        ser_isFolder=@ser_isFolder,
                        ser_file_name=@ser_file_name,
                        ser_size=@ser_size,
                        ser_modified_time=@ser_modified_time,
                        ser_display_path=@ser_display_path,
                        ser_cloud_pathid=@ser_cloud_pathid,
                        is_nxl_file=@is_nxl_file
                    WHERE
                        rms_external_repo_table_pk = @rms_external_repo_table_pk AND ser_file_id=@ser_file_id;

                   ---------if no updated happeded, then insert one--------------------------
                    INSERT INTO  
                            GoogleDriveFile(rms_external_repo_table_pk,
                                            ser_file_id,
                                            ser_isFolder,
                                            ser_file_name,
                                            ser_size,
                                            ser_modified_time,
                                            ser_display_path,
                                            ser_cloud_pathid,
                                            is_nxl_file)
                    SELECT 
                            @rms_external_repo_table_pk,
                            @ser_file_id,
                            @ser_isFolder,
                            @ser_file_name,
                            @ser_size,
                            @ser_modified_time,
                            @ser_display_path,
                            @ser_cloud_pathid,
                            @is_nxl_file
                    WHERE
                        ( SELECT changes() = 0 );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@ser_file_id",ser_file_id),
                new SQLiteParameter("@ser_isFolder",ser_isFolder),
                new SQLiteParameter("@ser_file_name",ser_file_name),
                new SQLiteParameter("@ser_size",ser_size),
                new SQLiteParameter("@ser_modified_time",convertedLastModified),

                new SQLiteParameter("@ser_display_path",ser_display_path),
                new SQLiteParameter("@ser_cloud_pathid",ser_cloud_pathid),
                new SQLiteParameter("@is_nxl_file",is_nxl),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> InsertFakedRoot_SQL(
               int rms_external_repo_table_pk)
        {
            string sql = @"
                INSERT OR IGNORE INTO 
                    GoogleDriveFile(rms_external_repo_table_pk,ser_file_id,ser_display_path,ser_cloud_pathid)
                    VALUES(@rms_external_repo_table_pk,'00000000-0000-0000-0000-000000000000','/','/');
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
               int rms_external_repo_table_pk, string couldPathId)
        {
            string sql = @"
              SELECT   
                *
            FROM
                GoogleDriveFile
            WHERE
                 rms_external_repo_table_pk=@rms_external_repo_table_pk AND ser_cloud_pathid like @path;                 
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@path",couldPathId+'%')
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_DisplayPath_By_PathId(int rms_external_repo_table_pk, string pathid)
        {
            string sql = @"
              SELECT   
                ser_display_path
            FROM
                GoogleDriveFile
            WHERE
                 rms_external_repo_table_pk=@rms_external_repo_table_pk AND ser_cloud_pathid = @ser_cloud_pathid;                
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@ser_cloud_pathid", pathid)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
            int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    GoogleDriveFile
                SET
                    status=@newStatus
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
            int table_pk, bool newMark)
        {
            string sql = @"
                UPDATE 
                    GoogleDriveFile
                SET
                    is_offline=@newMark
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@newMark",newMark?1:0),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_LocalPath_SQL(
          int table_pk, string newPath)
        {
            string sql = @"
                UPDATE 
                    GoogleDriveFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Query_DisplayPath_SQL(
            int rms_external_repo_table_pk,
            int this_table_pk)
        {
            string sql = @"
                SELECT 
                    ser_display_path
                FROM
                    GoogleDriveFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND
                    id =@this_table_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@this_table_id",this_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Used for query googledrive local file parent folder(cloudPathId).
        public static KeyValuePair<String, SQLiteParameter[]> Query_CloudPathId_SQL(
            int rms_external_repo_table_pk,
            int this_table_pk)
        {
            string sql = @"
                SELECT 
                    ser_cloud_pathid
                FROM
                    GoogleDriveFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND
                    id =@this_table_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@this_table_id",this_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

    }
}
