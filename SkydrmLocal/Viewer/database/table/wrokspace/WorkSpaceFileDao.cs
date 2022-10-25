using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.database
{
    public class WorkSpaceFile
    {
        public int Id { get; set; }
        // foreign key, table owner
        public int User_table_pk { get; set; }

        // all remoete fields
        public string RmsFileId { get; set; }
        public string RmsDuid { get; set; }
        public string RmsPathDisplay { get; set; }
        public string RmsPathId { get; set; }
        public string RmsNxlName { get; set; }
        public string RmsFileType { get; set; }
        public DateTime RmsLastModified { get; set; }
        public DateTime RmsCreatedTime { get; set; }
        public int RmsSize { get; set; }
        public bool RmsIsFolder { get; set; }
        public int RmsOwnerId { get; set; }
        public string RmsOwnerDisplayName { get; set; }
        public string RmsOwnerEmail { get; set; }
        public int RmsModifiedBy { get; set; } // by who(userId) modified
        public string RmsModifedByName { get; set; }
        public string RmsModifedByEmail { get; set; }

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

        public static WorkSpaceFile NewByReader(SQLiteDataReader reader)
        {
            WorkSpaceFile item = new WorkSpaceFile();
            {
                item.Id = int.Parse(reader["id"].ToString());
                item.User_table_pk = int.Parse(reader["user_table_pk"].ToString());
                // remote
                item.RmsFileId = reader["rms_file_id"].ToString();
                item.RmsDuid = reader["rms_duid"].ToString();
                item.RmsPathDisplay = reader["rms_path_display"].ToString();
                item.RmsPathId = reader["rms_path_id"].ToString();
                item.RmsNxlName = reader["rms_name"].ToString();
                item.RmsFileType = reader["rms_file_type"].ToString();

                item.RmsLastModified = new DateTime(Int64.Parse(reader["rms_last_modified"].ToString()));
                item.RmsCreatedTime = new DateTime(Int64.Parse(reader["rms_created"].ToString()));

                item.RmsSize = int.Parse(reader["rms_size"].ToString());
                item.RmsIsFolder = int.Parse(reader["rms_is_folder"].ToString()) == 1;
                item.RmsOwnerId = int.Parse(reader["rms_owner_id"].ToString());
                item.RmsOwnerDisplayName = reader["rms_owner_display_name"].ToString();
                item.RmsOwnerEmail = reader["rms_owner_email"].ToString();
                item.RmsModifiedBy = int.Parse(reader["rms_modified_by"].ToString());
                item.RmsModifedByName = reader["rms_modified_by_name"].ToString();
                item.RmsModifedByEmail = reader["rms_modified_by_email"].ToString();
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
            };

            return item;
        }
    }

    public class WorkSpaceFileDao
    {
        public static readonly string SQL_Create_Table_WorkspaceFile = @"
            CREATE TABLE IF NOT EXISTS WorkSpaceFile (
                id                             integer                  NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE ,
                user_table_pk                  integer                  default 0,

                ----- rms returned ----
                rms_file_id                    varchar(255)             NOT NULL default '',
                rms_duid                       varchar(255)             NOT NULL default '',
                rms_path_display               varchar(255)             NOT NULL default ''  COLLATE NOCASE,
                rms_path_id                    varchar(255)             NOT NULL default '',
                rms_name                       varchar(255)             NOT NULL default '',
                rms_file_type                  varchar(255)             NOT NULL default '',
                rms_last_modified              integer                 default 0,
                rms_created                    integer                     default 0,
                rms_size                       integer                  default 0,
                rms_is_folder                  integer                  default 0,
                rms_owner_id                   integer                  default 0,
                rms_owner_display_name         varchar(255)             NOT NULL default '',
                rms_owner_email                varchar(255)             NOT NULL default '',
                rms_modified_by                integer                  default 0,
                rms_modified_by_name           varchar(255)             NOT NULL default '',
                rms_modified_by_email          varchar(255)             NOT NULL default '',
               
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

                UNIQUE(user_table_pk, rms_file_id),
                foreign key(user_table_pk) references User(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> UPSERT_SQL(
            int user_tb_pk,
            string fileId, string duid, string pathDisplay, string pathId, string name, string fileType,
            Int64 lastModified, Int64 created, Int64 size,
            bool isFolder, int ownerId, string ownerDisplayName, string ownerEmail,
            int modifiedBy, string modifiedByName, string modifiedByEmail)
        {
            // Need to convert, or else, will failed when get from db(since sql parse exception: invalid date)
            var convertedLastModified = JavaTimeConverter.ToCSLongTicks(lastModified);
            var convertedCreated = JavaTimeConverter.ToCSLongTicks(created);

            string sql = @"     
               UPDATE 
                    WorkSpaceFile
                SET
                    rms_last_modified=@last_modified_time,
                    rms_size=@size
                WHERE
                    user_table_pk=@user_table_pk AND rms_file_id=@file_id;
                
                ---------if no updated happeded, then insert one--------------------------
                INSERT INTO
                    WorkSpaceFile(user_table_pk,
                                rms_file_id,
                                rms_duid,
                                rms_path_display,
                                rms_path_id,
                                rms_name,
                                rms_file_type,
                                rms_last_modified,
                                rms_created,
                                rms_size,                               
                                rms_is_folder,
                                rms_owner_id,
                                rms_owner_display_name,
                                rms_owner_email,
                                rms_modified_by,
                                rms_modified_by_name,
                                rms_modified_by_email
                                )
                SELECT
                    @user_table_pk,
                    @file_id,
                    @duid,
                    @path_display,
                    @path_id,
                    @name,
                    @file_type,
                    @last_modified_time,@created_time,
                    @size,
                    @is_folder,
                    @owner_id,
                    @owner_display_name,@owner_email,
                    @modified_by,@modified_by_name,@modified_by_email
                    
                WHERE
                       ( SELECT changes() = 0 );
            ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_tb_pk),
                new SQLiteParameter("@file_id",fileId),
                new SQLiteParameter("@duid",duid),
                new SQLiteParameter("@path_display",pathDisplay),
                new SQLiteParameter("@path_id",pathId),
                new SQLiteParameter("@name",name),

                new SQLiteParameter("@file_type",fileType),
                new SQLiteParameter("@last_modified_time",convertedLastModified),
                new SQLiteParameter("@created_time",convertedCreated),

                new SQLiteParameter("@size",size),
                new SQLiteParameter("@is_folder",isFolder ? 1: 0),

                new SQLiteParameter("@owner_id",ownerId),
                new SQLiteParameter("@owner_display_name",ownerDisplayName),
                new SQLiteParameter("@owner_email",ownerEmail),

                new SQLiteParameter("@modified_by",modifiedBy),
                new SQLiteParameter("@modified_by_name",modifiedByName),
                new SQLiteParameter("@modified_by_email",modifiedByEmail)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> InsertFakedRoot_SQL(
             int user_tb_pk)
        {
            string sql = @"
                INSERT OR IGNORE INTO 
                    WorkSpaceFile(user_table_pk,rms_file_id,rms_path_display,rms_path_id)
                    VALUES(@user_table_pk,'00000000-0000-0000-0000-000000000000','/','/');
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_tb_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        /// <summary>
        /// Query workspace files specified folser path.
        /// </summary>
        /// <param name="user_tb_pk">user table primary key</param>
        /// <param name="folderPath">specified folder path.</param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk, string folderPath)
        {
            string sql = @"SELECT * FROM
                                WorkSpaceFile 
                           WHERE
                                user_table_pk = @user_table_pk AND rms_path_display like @path
                           ORDER BY 
                                rms_path_id ASC";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_tb_pk),
                new SQLiteParameter("@path", folderPath+'%')
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_By_Duid_SQL(int user_tb_pk, string duid)
        {
            string sql = @"SELECT * FROM
                                WorkSpaceFile 
                           WHERE
                                user_table_pk = @user_table_pk AND rms_duid = @duid
                           ORDER BY 
                                rms_path_id ASC";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_tb_pk),
                new SQLiteParameter("@duid", duid)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        /// <summary>
        /// Query the displayPath as the parentFolder when upload workspace local file.
        /// </summary>
        /// <param name="user_tb_pk"></param>
        /// <param name="this_table_pk"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_DisplayPath_SQL(int user_tb_pk, int this_table_pk)
        {
            string sql = @"
                SELECT 
                    rms_path_display
                FROM
                    WorkSpaceFile
                WHERE
                    user_table_pk=@user_tb_pk AND
                    id =@this_table_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_tb_pk),
                new SQLiteParameter("@this_table_id",this_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_Offline_SQL(int user_tb_pk)
        {
            string sql = @"
                SELECT 
                        * 
                FROM 
                        WorkSpaceFile 
                WHERE 
                        user_table_pk=@user_tb_pk AND is_offline=@is_offline;
              ";

            SQLiteParameter[] parameters = {
                   new SQLiteParameter("@user_tb_pk" , user_tb_pk),
                   new SQLiteParameter("@is_offline" , 1)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_Status_SQL(
             int table_pk, int status)
        {
            string sql = @"UPDATE WorkSpaceFile
                           SET 
                                operation_status=@status
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@status",status),
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_ModifyRightsStatus_SQL(
            int table_pk, int newStatus)
        {
            string sql = @"UPDATE WorkSpaceFile
                           SET 
                                modify_rights_status=@status
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@status",newStatus),
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_NXL_EditStatus_SQL(
            int table_pk, int newStatus)
        {
            string sql = @"UPDATE WorkSpaceFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_Offline_Mark_SQL(
            int table_pk, bool is_offline)
        {
            string sql = @"UPDATE WorkSpaceFile
                           SET 
                                is_offline=@is_offline
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@is_offline",is_offline)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Nxl_Local_Path(
                int table_pk, string nxl_local_path)
        {
            string sql = @"UPDATE 
                                WorkSpaceFile
                           SET 
                                local_path=@nxl_local_path
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@nxl_local_path",nxl_local_path)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        // Delete file
        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(
          int user_table_pk, string rms_file_id)
        {
            string sql = @"DELETE FROM 
                                WorkSpaceFile
                           WHERE
                                user_table_pk=@user_tb_pk AND rms_file_id=@rms_file_id;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_table_pk),
                new SQLiteParameter("@rms_file_id",rms_file_id)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Delete specified folder and its all children nodes.
        public static KeyValuePair<String, SQLiteParameter[]> Delete_Folder_And_SubChildren_SQL(
            int user_table_pk, string rms_path_id)
        {
            string sql = @"DELETE FROM 
                                WorkSpaceFile
                           WHERE
                                user_table_pk=@user_tb_pk AND 
                                rms_path_id like @rms_path_id;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_table_pk),
                new SQLiteParameter("@rms_path_id",rms_path_id+'%')
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }
    }
}
