using SkydrmLocal.rmc.common.helper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.sharedworkspace
{
    public class SharedWorkspaceFile
    {
        public int Id { get; set; }
        // foreign key
        public int Rms_external_repo_table_pk { get; set; }

        // remote fields
        public string RmsFileid { get; set; }
        public string RmsDuid { get; set; }
        public string RmsPath { get; set; }
        public string RmsPathid { get; set; }
        public string RmsName { get; set; }
        public string RmsType { get; set; }
        public DateTime RmsLastModified { get; set; }
        public DateTime RmsCreatedTime { get; set; }
        public int RmsSize { get; set; }
        public bool RmsIsFolder { get; set; }
        public bool RmsIsProtectedFile { get; set; }

        // all local fields
        public string LocalPath { get; set; }
        public bool IsOffline { get; set; }
        public DateTime OfflineTime { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime FavoriteTime { get; set; }
        public int Status { get; set; }
        public int Edit_Status { get; set; }
        public int ModifyRightsStatus { get; set; }

        // reserved
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }

        public static SharedWorkspaceFile NewByReader(SQLiteDataReader reader)
        {
            SharedWorkspaceFile item = new SharedWorkspaceFile();
            {
                item.Id = int.Parse(reader["id"].ToString());
                item.Rms_external_repo_table_pk = int.Parse(reader["rms_external_repo_table_pk"].ToString());
                // remote
                item.RmsFileid = reader["rms_file_id"].ToString();
                item.RmsDuid = reader["rms_duid"].ToString();
                item.RmsPath = reader["rms_path"].ToString();
                item.RmsPathid = reader["rms_path_id"].ToString();
                item.RmsName = reader["rms_name"].ToString();
                item.RmsType = reader["rms_file_type"].ToString();

                item.RmsLastModified = new DateTime(Int64.Parse(reader["rms_last_modified"].ToString()));
                item.RmsCreatedTime = new DateTime(Int64.Parse(reader["rms_created"].ToString()));

                item.RmsSize = int.Parse(reader["rms_size"].ToString());
                item.RmsIsFolder = int.Parse(reader["rms_is_folder"].ToString()) == 1;
                item.RmsIsProtectedFile = int.Parse(reader["rms_is_protectedFile"].ToString()) == 1;

                // local
                item.LocalPath = reader["local_path"].ToString();
                item.IsOffline = int.Parse(reader["is_offline"].ToString()) == 1;
                item.OfflineTime = DateTime.Parse(reader["offline_time"].ToString()).ToUniversalTime();
                item.IsFavorite = int.Parse(reader["is_favorite"].ToString()) == 1;
                item.FavoriteTime = DateTime.Parse(reader["favorite_time"].ToString()).ToUniversalTime();
                item.Status = int.Parse(reader["operation_status"].ToString());
                item.Edit_Status = int.Parse(reader["edit_status"].ToString());
                item.ModifyRightsStatus = int.Parse(reader["modify_rights_status"].ToString());

                // reserved
                item.Reserved1 = "";
                item.Reserved2 = "";
                item.Reserved3 = "";
                item.Reserved4 = "";
            }

            return item;
        }

    }

    public class SharedWorkspaceFileDao
    {
        public static readonly string SQL_Create_Table_SharedWorkspaceFile = @"
            CREATE TABLE IF NOT EXISTS SharedWorkspaceFile (
                id                             integer                  NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE ,
                rms_external_repo_table_pk     integer                  default 0,

                ----- rms returned ----
                rms_file_id                    varchar(255)             NOT NULL default '',
                rms_duid                       varchar(255)             NOT NULL default '',
                rms_path                       varchar(255)             NOT NULL default ''  COLLATE NOCASE,
                rms_path_id                    varchar(255)             NOT NULL default '',
                rms_name                       varchar(255)             NOT NULL default '',
                rms_file_type                  varchar(255)             NOT NULL default '',
                rms_last_modified              integer                  default 0,
                rms_created                    integer                  default 0,
                rms_size                       integer                  default 0,
                rms_is_folder                  integer                  default 0,
                rms_is_protectedFile           integer                  default 0,
               
                ------ local added ----
                local_path                     varchar(255)             NOT NULL default '',
                is_offline                     integer                  default 0,
                offline_time                   DateTime                 NOT NULL default (datetime('now','localtime')), 
                is_favorite                    integer                  default 0,
                favorite_time                  DateTime                 NOT NULL default (datetime('now','localtime')),
                operation_status               integer                  DEFAULT 4,
                edit_status                    integer                  DEFAULT 0,
                modify_rights_status           integer                  DEFAULT 0,

               ----- reserved ---- 
                reserved1                      text                     DEFAULT '',
                reserved2                      text                     DEFAULT '',
                reserved3                      text                     DEFAULT '',
                reserved4                      text                     DEFAULT '',

                UNIQUE(rms_external_repo_table_pk, rms_file_id),
                foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
            int rms_external_repo_table_pk, string rms_file_id, string rms_duid, string rms_path,
            string rms_pathid, string rms_name, string rms_type, long rms_modified_time, long rms_create_time,
            long rms_size, int rms_isFolder, int rms_isProtectedFile)
        {
            // Need to convert, or else, will failed when get from db(since sql parse exception: invalid date)
            var convertedLastModified = JavaTimeConverter.ToCSLongTicks(rms_modified_time);
            var convertedCreated = JavaTimeConverter.ToCSLongTicks(rms_create_time);

            string sql = @"
                    UPDATE SharedWorkspaceFile 
                    SET 
                        rms_file_id=@rms_file_id,
                        rms_duid=@rms_duid,
                        rms_path=@path,
                        rms_path_id=@path_id,
                        rms_name=@name,
                        rms_file_type=@file_type,
                        rms_last_modified=@last_modified_time,
                        rms_created=@created_time,
                        rms_size=@size,
                        rms_is_folder=@is_folder,
                        rms_is_protectedFile=@rms_isProtectedFile
                    WHERE
                        rms_external_repo_table_pk = @rms_external_repo_table_pk AND rms_file_id=@rms_file_id;

                   ---------if no updated happeded, then insert one--------------------------
                    INSERT INTO  
                            SharedWorkspaceFile(rms_external_repo_table_pk,
                                            rms_file_id,
                                            rms_duid,
                                            rms_path,
                                            rms_path_id,
                                            rms_name,
                                            rms_file_type,
                                            rms_last_modified,
                                            rms_created,
                                            rms_size,                               
                                            rms_is_folder,
                                            rms_is_protectedFile)
                    SELECT 
                            @rms_external_repo_table_pk,
                            @rms_file_id,
                            @rms_duid,
                            @path,
                            @path_id,
                            @name,
                            @file_type,
                            @last_modified_time,
                            @created_time,
                            @size,
                            @is_folder,
                            @rms_isProtectedFile
                    WHERE
                        ( SELECT changes() = 0 );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@rms_file_id",rms_file_id),
                new SQLiteParameter("@rms_duid",rms_duid),

                new SQLiteParameter("@path",rms_path),
                new SQLiteParameter("@path_id",rms_pathid),
                new SQLiteParameter("@name",rms_name),

                new SQLiteParameter("@file_type",rms_type),
                new SQLiteParameter("@last_modified_time",convertedLastModified),
                new SQLiteParameter("@created_time",convertedCreated),

                new SQLiteParameter("@size",rms_size),
                new SQLiteParameter("@is_folder",rms_isFolder),
                new SQLiteParameter("@rms_isProtectedFile",rms_isProtectedFile)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> InsertFakedRoot_SQL(
           int rms_external_repo_table_pk)
        {
            string sql = @"
                INSERT OR IGNORE INTO 
                    SharedWorkspaceFile(rms_external_repo_table_pk,rms_file_id,rms_path,rms_path_id)
                    VALUES(@rms_external_repo_table_pk,'00000000-0000-0000-0000-000000000000','/','/');
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> QueryAll_SQL(
            int rms_external_repo_table_pk)
        {
            string sql = @"
              SELECT   
                *
            FROM
                SharedWorkspaceFile
            WHERE
                 rms_external_repo_table_pk=@rms_external_repo_table_pk;                 
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Note: we use 'path' to qeury data, like rest api to use 'path' to get data.
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
            int rms_external_repo_table_pk, string path)
        {
            string sql = @"
              SELECT   
                *
            FROM
                SharedWorkspaceFile
            WHERE
                 rms_external_repo_table_pk=@rms_external_repo_table_pk AND rms_path like @path;                 
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@path",path+'%')
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Delete file
        public static KeyValuePair<String, SQLiteParameter[]> Delete_File_SQL(
            int rms_external_repo_table_pk,
            string rms_file_id)
        {
            string sql = @"
                DELETE FROM 
                    SharedWorkspaceFile
                WHERE 
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
                    rms_file_id=@rms_file_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@rms_file_id",rms_file_id),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Delete folder.
        public static KeyValuePair<String, SQLiteParameter[]> Delete_Folder_And_SubChildren_SQL(
            int rms_external_repo_table_pk,
            string rms_path)
        {
            string sql = @"
                DELETE FROM 
                    SharedWorkspaceFile
                WHERE 
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
                    rms_path like @rms_path
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@rms_path",rms_path+'%'),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Update fields

        public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_For_Overwrite_LeaveCopyModel(
                int table_pk, int status, long size, DateTime lastmodifed)
        {
            var modifiedTime_Universal = lastmodifed.ToUniversalTime(); // keep consistent time unit with rms returned.
            var modifiedTime = modifiedTime_Universal.Ticks;
            string sql = @"UPDATE SharedWorkspaceFile
                           SET 
                                operation_status=@status,
                                rms_size=@size,
                                rms_last_modified=@last_modified_time
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@status",status),
                new SQLiteParameter("@size",size),
                new SQLiteParameter("@last_modified_time", modifiedTime),
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
            int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    SharedWorkspaceFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_EditStatus_SQL(
           int table_pk, int newStatus)
        {
            string sql = @"UPDATE SharedWorkspaceFile
                           SET 
                                edit_status=@status
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@status",newStatus),
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_IsOffline_SQL(
            int table_pk, bool newMark)
        {
            string sql = @"
                UPDATE 
                    SharedWorkspaceFile
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
                    SharedWorkspaceFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Query_Path_SQL(
            int rms_external_repo_table_pk,
            int this_table_pk)
        {
            string sql = @"
                SELECT 
                    rms_path
                FROM
                    SharedWorkspaceFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Query_PathId_SQL(
            int rms_external_repo_table_pk,
            string displayPath)
        {
            string sql = @"
                SELECT 
                    rms_path_id
                FROM
                    SharedWorkspaceFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND
                    rms_path =@rmsPath
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@rmsPath",displayPath)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

    }
}
