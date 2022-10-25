using SkydrmLocal.rmc.common.helper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.sharepoint
{
    public class SharePointFileDao
    {
        /// <summary>
        /// The table is used for storing the files of repository which type is "SharePointOnline|SharePointOnPremise",
        /// but user can add or bind multiply different SharePoint repos, they are stored in 
        /// the same table.
        /// </summary>
        public static readonly string SQL_Create_Table_SharePointFile = @"
            CREATE TABLE IF NOT EXISTS SharePointFile (
                id                          integer             NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                rms_external_repo_table_pk  integer             default 0, 

                ------ server returned -----
                file_id                     varchar(255)        NOT NULL default '',
                is_folder                   integer             NOT NULL default 0,
                is_site                     integer             NOT NULL default 0,
                name                        varchar(255)        NOT NULL default '',
                size                        long                NOT NULL default 0,
                last_modified_time          long                NOT NULL default 0,
                path_id                     varchar(255)        NOT NULL default '',
                path_display                varchar(255)        NOT NULL default '',
                cloud_path_id               varchar(255)        NOT NULL default '',
                
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
                reserved5                   text                DEFAULT '',
                reserved6                   text                DEFAULT '',
                reserved7                   text                DEFAULT '',
                reserved8                   text                DEFAULT '',

                UNIQUE(rms_external_repo_table_pk, file_id),
                foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade);
        ";

        public static KeyValuePair<string, SQLiteParameter[]> InsertFakedRoot_SQL(int rms_external_repo_table_pk)
        {
            string sql = @"
                INSERT OR IGNORE INTO 
                    SharePointFile(rms_external_repo_table_pk,file_id,path_id,path_display,cloud_path_id)
                    VALUES(@rms_external_repo_table_pk,'00000000-0000-0000-0000-000000000000','/','/','/');
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Upsert_SQL(int rms_external_repo_table_pk,
            string file_id, bool is_folder, bool is_site,
            string name, long size, long last_modified_time,
            string path_id, string path_display, string cloud_path_id, bool is_nxl_file)
        {
            // Need to convert, or else, will failed when get from db(since sql parse exception: invalid date)
            var convertedLastModified = JavaTimeConverter.ToCSLongTicks(last_modified_time);

            string sql = @"
                    UPDATE SharePointFile 
                    SET 
                        file_id=@file_id,
                        is_folder=@is_folder,
                        is_site=@is_site,
                        name=@name,
                        size=@size,
                        last_modified_time=@last_modified_time,
                        path_id=@path_id,
                        path_display=@path_display,
                        cloud_path_id=@cloud_path_id,
                        is_nxl_file=@is_nxl_file
                    WHERE
                        rms_external_repo_table_pk = @rms_external_repo_table_pk AND file_id=@file_id;

                   ---------if no updated happeded, then insert one--------------------------

                    INSERT INTO  
                            SharePointFile(rms_external_repo_table_pk,
                                            file_id,
                                            is_folder,
                                            is_site,
                                            name,
                                            size,
                                            last_modified_time,
                                            path_id,
                                            path_display,
                                            cloud_path_id,
                                            is_nxl_file)
                    SELECT 
                            @rms_external_repo_table_pk,
                            @file_id,
                            @is_folder,
                            @is_site,
                            @name,
                            @size,
                            @last_modified_time,
                            @path_id,
                            @path_display,
                            @cloud_path_id,
                            @is_nxl_file
                    WHERE
                        ( SELECT changes() = 0 );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@file_id",file_id),
                new SQLiteParameter("@is_folder",is_folder?1:0),
                new SQLiteParameter("@is_site",is_site?1:0),
                new SQLiteParameter("@name",name),
                new SQLiteParameter("@size",size),
                new SQLiteParameter("@last_modified_time",convertedLastModified),
                new SQLiteParameter("@path_id",path_id),
                new SQLiteParameter("@path_display",path_display),
                new SQLiteParameter("@cloud_path_id",cloud_path_id),
                new SQLiteParameter("@is_nxl_file",is_nxl_file),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_id">SharePointFile table primary key</param>
        /// <returns></returns>
        public static KeyValuePair<string, SQLiteParameter[]> Delete_File_SQL(int _id)
        {
            string sql = @"
                DELETE FROM 
                    SharePointFile
                WHERE 
                    id=@_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@_id",_id)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Delete_File_SQL(int rms_external_repo_table_pk, string path_id)
        {
            string sql = @"
                DELETE FROM 
                    SharePointFile
                WHERE 
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
                    cloud_path_id=@cloud_path_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@cloud_path_id",path_id),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Delete_Folder_And_SubChildren_SQL(int rms_external_repo_table_pk, string path_id)
        {
            string sql = @"
                DELETE FROM 
                    SharePointFile
                WHERE 
                    rms_external_repo_table_pk=@rms_external_repo_table_pk 
                AND 
                    cloud_path_id like @cloud_path_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@cloud_path_id",path_id+'%'),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Query_SQL(int rms_external_repo_table_pk, string path_id)
        {
            string sql = @"
              SELECT * FROM
                    SharePointFile
              WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk 
              AND   
                    cloud_path_id like @cloud_path_id;                 
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@cloud_path_id",path_id+'%')
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Query_OFFLINE_SQL(int rms_external_repo_table_pk)
        {
            string sql = @"
              SELECT * FROM
                    SharePointFile
              WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk 
              AND   
                    is_offline=1;                 
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Update_FileStatus_SQL(int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    SharePointFile
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

        public static KeyValuePair<string, SQLiteParameter[]> Update_IsOffline_SQL(int table_pk, bool newMark)
        {
            string sql = @"
                UPDATE 
                    SharePointFile
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

        public static KeyValuePair<string, SQLiteParameter[]> Update_LocalPath_SQL(int table_pk, string local_path)
        {
            string sql = @"
                UPDATE 
                    SharePointFile
                SET
                    local_path=@newPath
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@newPath",local_path),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Query_PATH_ID_SQL(int sharepoint_file_table_pk)
        {
            string sql = @"
                SELECT 
                    path_id
                FROM
                    SharePointFile
                WHERE
                    id =@id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@id",sharepoint_file_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Query_PATH_ID_SQL(int rms_external_repo_table_pk, string cloudPathId)
        {
            string sql = @"
                SELECT 
                    path_id
                FROM
                    SharePointFile
                WHERE
                    rms_external_repo_table_pk =@rms_external_repo_table_pk
                AND cloud_path_id=@cloud_path_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@cloud_path_id",cloudPathId)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }
    }
}
