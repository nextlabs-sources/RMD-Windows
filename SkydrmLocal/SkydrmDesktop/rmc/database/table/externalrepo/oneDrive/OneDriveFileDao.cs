using SkydrmLocal.rmc.common.helper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.database.table.externalrepo.oneDrive;
using static SkydrmDesktop.rmc.database.table.externalrepo.oneDrive.OneDriveItem;
using System.Data;

namespace SkydrmDesktop.rmc.database.table.externalrepo.oneDrive
{
    public class OneDriveFileDao
    {
        public static KeyValuePair<String, SQLiteParameter[]> Inner_Join_Query_OneDriveRootFolder_SQL(int rms_external_repo_table_pk)
        {
            string sql = @"
                    SELECT   
                            OneDriveFileCommon.createdDateTime,
                            OneDriveFileCommon.cTag,
                            OneDriveFileCommon.eTag,
                            OneDriveFileCommon.item_id,
                            OneDriveFileCommon.lastModifiedDateTime,
                            OneDriveFileCommon.name,
                            OneDriveFileCommon.size,
                            OneDriveFileCommon.webUrl,
                            OneDriveFileCommon.reactions_commentCount,
                            OneDriveFileCommon.createdBy_application_displayName,
                            OneDriveFileCommon.createdBy_application_id,
                            OneDriveFileCommon.createdBy_user_displayName,
                            OneDriveFileCommon.createdBy_user_id,
                            OneDriveFileCommon.lastModifiedBy_application_displayName,
                            OneDriveFileCommon.lastModifiedBy_application_id,
                            OneDriveFileCommon.lastModifiedBy_user_displayName,
                            OneDriveFileCommon.lastModifiedBy_user_id,
                            OneDriveFileCommon.parentReference_driveId,
                            OneDriveFileCommon.parentReference_driveType,
                            OneDriveFileCommon.parentReference_id,
                            OneDriveFileCommon.parentReference_path,
                            OneDriveFileCommon.fileSystemInfo_createdDateTime,
                            OneDriveFileCommon.fileSystemInfo_lastModifiedDateTime,
                            OneDriveFolders.folder_childCount,
                            OneDriveFolders.folder_view_viewType,
                            OneDriveFolders.folder_view_sortBy,
                            OneDriveFolders.folder_view_sortOrder,
                            OneDriveFolders.specialFolder_name,
                            OneDriveFolders.isRootFolder
                    FROM
                        OneDriveFileCommon
                    INNER JOIN
                        OneDriveFolders
                    ON
                        OneDriveFileCommon.item_id=OneDriveFolders.item_id 
                        AND 
                        OneDriveFolders.rms_external_repo_table_pk=@rms_external_repo_table_pk 
                        AND 
                        OneDriveFolders.isRootFolder=1;
                    ";
            SQLiteParameter[] parameters = {
                      new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk)
                    };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Inner_Join_Query_Children_Folder_SQL(int rms_external_repo_table_pk, string parent_item_id)
        {
            string sql = @"
                        SELECT   
                            OneDriveFileCommon.createdDateTime,
                            OneDriveFileCommon.cTag,
                            OneDriveFileCommon.eTag,
                            OneDriveFileCommon.item_id,
                            OneDriveFileCommon.lastModifiedDateTime,
                            OneDriveFileCommon.name,
                            OneDriveFileCommon.size,
                            OneDriveFileCommon.webUrl,
                            OneDriveFileCommon.reactions_commentCount,
                            OneDriveFileCommon.createdBy_application_displayName,
                            OneDriveFileCommon.createdBy_application_id,
                            OneDriveFileCommon.createdBy_user_displayName,
                            OneDriveFileCommon.createdBy_user_id,
                            OneDriveFileCommon.lastModifiedBy_application_displayName,
                            OneDriveFileCommon.lastModifiedBy_application_id,
                            OneDriveFileCommon.lastModifiedBy_user_displayName,
                            OneDriveFileCommon.lastModifiedBy_user_id,
                            OneDriveFileCommon.parentReference_driveId,
                            OneDriveFileCommon.parentReference_driveType,
                            OneDriveFileCommon.parentReference_id,
                            OneDriveFileCommon.parentReference_path,
                            OneDriveFileCommon.fileSystemInfo_createdDateTime,
                            OneDriveFileCommon.fileSystemInfo_lastModifiedDateTime,
                            OneDriveFolders.folder_childCount,
                            OneDriveFolders.folder_view_viewType,
                            OneDriveFolders.folder_view_sortBy,
                            OneDriveFolders.folder_view_sortOrder,
                            OneDriveFolders.specialFolder_name,
                            OneDriveFolders.isRootFolder
                        FROM
                            OneDriveFileCommon
                        INNER JOIN
                            OneDriveFolders
                        ON
                            OneDriveFileCommon.item_id=OneDriveFolders.item_id 
                            AND 
                            OneDriveFolders.rms_external_repo_table_pk=@rms_external_repo_table_pk 
                            AND 
                            OneDriveFileCommon.parentReference_id=@parent_item_id;
                        ";
            SQLiteParameter[] parameters = {
                          new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                          new SQLiteParameter("@parent_item_id",parent_item_id)
                        };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Inner_Join_Query_Children_File_SQL(int rms_external_repo_table_pk, string parent_item_id)
        {
            string sql = @"
                        SELECT   
                            OneDriveFileCommon.createdDateTime,
                            OneDriveFileCommon.cTag,
                            OneDriveFileCommon.eTag,
                            OneDriveFileCommon.item_id,
                            OneDriveFileCommon.lastModifiedDateTime,
                            OneDriveFileCommon.name,
                            OneDriveFileCommon.size,
                            OneDriveFileCommon.webUrl,
                            OneDriveFileCommon.reactions_commentCount,
                            OneDriveFileCommon.createdBy_application_displayName,
                            OneDriveFileCommon.createdBy_application_id,
                            OneDriveFileCommon.createdBy_user_displayName,
                            OneDriveFileCommon.createdBy_user_id,
                            OneDriveFileCommon.lastModifiedBy_application_displayName,
                            OneDriveFileCommon.lastModifiedBy_application_id,
                            OneDriveFileCommon.lastModifiedBy_user_displayName,
                            OneDriveFileCommon.lastModifiedBy_user_id,
                            OneDriveFileCommon.parentReference_driveId,
                            OneDriveFileCommon.parentReference_driveType,
                            OneDriveFileCommon.parentReference_id,
                            OneDriveFileCommon.parentReference_path,
                            OneDriveFileCommon.fileSystemInfo_createdDateTime,
                            OneDriveFileCommon.fileSystemInfo_lastModifiedDateTime,
                            OneDriveFiles.downloadUrl,
                            OneDriveFiles.file_mimeType,
                            OneDriveFiles.hashes_quickXorHash,
                            OneDriveFiles.hashes_sha1Hash
                        FROM
                            OneDriveFileCommon
                        INNER JOIN
                            OneDriveFiles
                        ON
                            OneDriveFiles.rms_external_repo_table_pk=@rms_external_repo_table_pk
                            AND 
                            OneDriveFileCommon.parentReference_id=@parent_item_id
                            AND 
                            OneDriveFileCommon.item_id=OneDriveFiles.item_id;
                        ";
            SQLiteParameter[] parameters = {
                          new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                          new SQLiteParameter("@parent_item_id",parent_item_id)
                        };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Inner_Join_Query_Children_Folder_Id_SQL(int rms_external_repo_table_pk, string parent_item_id)
        {
            string sql = @"
                        SELECT
                            OneDriveFolders.item_id
                        FROM
                            OneDriveFolders
                        INNER JOIN
                            OneDriveFileCommon
                        ON
                            OneDriveFolders.item_id=OneDriveFileCommon.item_id
                            AND 
                            OneDriveFolders.rms_external_repo_table_pk=@rms_external_repo_table_pk
                            AND 
                            OneDriveFileCommon.parentReference_id=@parent_item_id;
                        ";
            SQLiteParameter[] parameters = {
                          new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                          new SQLiteParameter("@parent_item_id",parent_item_id)
                        };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        //OneDriveFileCommon.isFolder,

        public static KeyValuePair<String, SQLiteParameter[]> Inner_Join_Query_Children_File_UI_SQL(int rms_external_repo_table_pk, string parent_item_id)
        {
            string sql = @"SELECT
                                OneDriveFileCommon.id,
                                OneDriveFileCommon.item_id,
                                OneDriveFileCommon.isFolder,
                                OneDriveFileCommon.name,
                                OneDriveFileCommon.size,
                                OneDriveFileCommon.lastModifiedDateTime,
                                OneDriveFileCommon.parentReference_path,
                                OneDriveFileLocalStatus.is_offline,
                                OneDriveFileLocalStatus.is_favorite,
                                OneDriveFileLocalStatus.local_path,
                                OneDriveFileLocalStatus.is_nxl_file,
                                OneDriveFileLocalStatus.status,
                                OneDriveFileLocalStatus.custom_string,
                                OneDriveFileLocalStatus.edit_status,
                                OneDriveFileLocalStatus.modify_rights_status
                           FROM
                                OneDriveFileCommon
                           LEFT JOIN
                                OneDriveFileLocalStatus
                           ON
                                OneDriveFileCommon.item_id=OneDriveFileLocalStatus.item_id
                                AND
                                OneDriveFileLocalStatus.rms_external_repo_table_pk=@rms_external_repo_table_pk
                            WHERE
                                OneDriveFileCommon.parentReference_id=@parent_item_id;
                          ";
            SQLiteParameter[] parameters = {
                          new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                          new SQLiteParameter("@parent_item_id",parent_item_id)
                        };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Inner_Join_Query_All_File_UI_SQL(int rms_external_repo_table_pk)
        {
            string sql = @"SELECT
                                OneDriveFileCommon.id,
                                OneDriveFileCommon.item_id,
                                OneDriveFileCommon.isFolder,
                                OneDriveFileCommon.name,
                                OneDriveFileCommon.size,
                                OneDriveFileCommon.lastModifiedDateTime,
                                OneDriveFileCommon.parentReference_path,
                                OneDriveFileLocalStatus.is_offline,
                                OneDriveFileLocalStatus.is_favorite,
                                OneDriveFileLocalStatus.local_path,
                                OneDriveFileLocalStatus.is_nxl_file,
                                OneDriveFileLocalStatus.status,
                                OneDriveFileLocalStatus.custom_string,
                                OneDriveFileLocalStatus.edit_status,
                                OneDriveFileLocalStatus.modify_rights_status
                           FROM
                                OneDriveFileCommon
                           LEFT JOIN
                                OneDriveFileLocalStatus
                           ON
                                OneDriveFileCommon.item_id=OneDriveFileLocalStatus.item_id
                           WHERE    
                                OneDriveFileCommon.rms_external_repo_table_pk=@rms_external_repo_table_pk;
                          ";
            SQLiteParameter[] parameters = {
                          new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk)
                        };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


    }

    //public class OneDriveFileDao
    //{
    //    public static readonly string SQL_Create_Table_OneDriveFile = @"
    //        CREATE TABLE IF NOT EXISTS OneDriveFile (
    //            id                          integer             NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
    //            rms_external_repo_table_pk  integer             default 0, 

    //            ------ server returned -----
    //            ser_downloadUrl                    varchar(500)             NOT NULL default '',
    //            ser_createdDateTime                varchar(255)             NOT NULL default '',
    //            ser_cTag                           varchar(255)             NOT NULL default '',
    //            ser_eTag                           varchar(255)             NOT NULL default '',
    //            ser_id                             varchar(255)             NOT NULL default '',
    //            ser_lastModifiedDateTime           varchar(255)             NOT NULL default '',
    //            ser_name                           varchar(255)             NOT NULL default '',
    //            ser_size                           integer                  NOT NULL default 0,
    //            ser_webUrl                         varchar(255)             NOT NULL default '',
    //            ser_reactions_commentCount         integer                  NOT NULL default 0,
    //            ser_createdBy_application_displayName    varchar(255)      NOT NULL default '',
    //            ser_createdBy_application_id             varchar(255)      NOT NULL default '',
    //            ser_createdBy_user_displayName           varchar(255)      NOT NULL default '',
    //            ser_createdBy_user_id                    varchar(255)      NOT NULL default '',
    //            ser_lastModifiedBy_application_displayName    varchar(255)            NOT NULL default '',
    //            ser_lastModifiedBy_application_id             varchar(255)            NOT NULL default '',
    //            ser_lastModifiedBy_user_displayName           varchar(255)            NOT NULL default '',
    //            ser_lastModifiedBy_user_id                    varchar(255)            NOT NULL default '',
    //            ser_parentReference_driveId                 varchar(255)              NOT NULL default '',
    //            ser_parentReference_driveType               varchar(255)              NOT NULL default '',
    //            ser_parentReference_id                      varchar(255)              NOT NULL default '',
    //            ser_parentReference_path                    varchar(255)              NOT NULL default '',
    //            ser_fileSystemInfo_createdDateTime          varchar(255)             NOT NULL default '',
    //            ser_fileSystemInfo_lastModifiedDateTime     varchar(255)             NOT NULL default '',
    //            ser_isFolder                       integer             NOT NULL default 0,
    //            ser_folder_childCount              integer             NOT NULL default 0,
    //            ser_folder_view_viewType           varchar(255)        NOT NULL default '',
    //            ser_folder_view_sortBy             varchar(255)        NOT NULL default '',
    //            ser_folder_view_sortOrder          varchar(255)        NOT NULL default '',
    //            ser_specialFolder_name             varchar(255)        NOT NULL default '',
    //            ser_file_mimeType                  varchar(255)        NOT NULL default '',
    //            ser_hashes_quickXorHash            varchar(255)        NOT NULL default '',
    //            ser_hashes_sha1Hash                varchar(255)        NOT NULL default '',

    //            ------ local added -----
    //            is_offline                  integer             NOT NULL default 0,
    //            is_favorite                 integer             NOT NULL default 0,
    //            local_path                  varchar(255)        NOT NULL default '',
    //            is_nxl_file                 integer             NOT NULL default 0,
    //            status                      integer             NOT NULL default 4,
    //            custom_string               varchar(255)        NOT NULL default '',
    //            edit_status                 integer             NOT NULL default 0,
    //            modify_rights_status        integer             NOT NULL default 0,

    //           ----- reserved ---- 
    //            reserved1                   text                DEFAULT '',
    //            reserved2                   text                DEFAULT '',
    //            reserved3                   text                DEFAULT '',
    //            reserved4                   text                DEFAULT '',

    //            UNIQUE(rms_external_repo_table_pk, ser_id),
    //            foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade);
    //    ";

    //    public static KeyValuePair<String, SQLiteParameter[]> Delete_File_SQL(int rms_external_repo_table_pk,string fileId)
    //    {
    //            string sql = @"
    //                DELETE FROM 
    //                    OneDriveFile
    //                WHERE 
    //                    rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
    //                    ser_id=@fileId
    //                ;
    //            ";
    //            SQLiteParameter[] parameters = {
    //                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
    //                new SQLiteParameter("@fileId",fileId),
    //            };
    //            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
    //    }

    //    public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(int rms_external_repo_table_pk, ValueItem item)
    //    {
    //        string sql = @"UPDATE 
    //                        OneDriveFile 
    //                        SET
    //                        ser_downloadUrl=@ser_downloadUrl,
    //                        ser_createdDateTime=@ser_createdDateTime,
    //                        ser_cTag=@ser_cTag,
    //                        ser_eTag=@ser_eTag,
    //                        ser_id=@ser_id,
    //                        ser_lastModifiedDateTime=@ser_lastModifiedDateTime,
    //                        ser_name=@ser_name,
    //                        ser_size=@ser_size,
    //                        ser_webUrl=@ser_webUrl,
    //                        ser_reactions_commentCount=@ser_reactions_commentCount,
    //                        ser_createdBy_application_displayName=@ser_createdBy_application_displayName,
    //                        ser_createdBy_application_id=@ser_createdBy_application_id,
    //                        ser_createdBy_user_displayName=@ser_createdBy_user_displayName,
    //                        ser_createdBy_user_id=@ser_createdBy_user_id,
    //                        ser_lastModifiedBy_application_displayName=@ser_lastModifiedBy_application_displayName,
    //                        ser_lastModifiedBy_application_id=@ser_lastModifiedBy_application_id,
    //                        ser_lastModifiedBy_user_displayName=@ser_lastModifiedBy_user_displayName,
    //                        ser_lastModifiedBy_user_id=@ser_lastModifiedBy_user_id,
    //                        ser_parentReference_driveId=@ser_parentReference_driveId,
    //                        ser_parentReference_driveType=@ser_parentReference_driveType,
    //                        ser_parentReference_id=@ser_parentReference_id,
    //                        ser_parentReference_path=@ser_parentReference_path,
    //                        ser_fileSystemInfo_createdDateTime=@ser_fileSystemInfo_createdDateTime,
    //                        ser_fileSystemInfo_lastModifiedDateTime=@ser_fileSystemInfo_lastModifiedDateTime,
    //                        ser_isFolder=@ser_isFolder,
    //                        ser_folder_childCount=@ser_folder_childCount,
    //                        ser_folder_view_viewType=@ser_folder_view_viewType,
    //                        ser_folder_view_sortBy=@ser_folder_view_sortBy,
    //                        ser_folder_view_sortOrder=@ser_folder_view_sortOrder,
    //                        ser_specialFolder_name=@ser_specialFolder_name,
    //                        ser_file_mimeType=@ser_file_mimeType,
    //                        ser_hashes_quickXorHash=@ser_hashes_quickXorHash,
    //                        ser_hashes_sha1Hash=@ser_hashes_sha1Hash
    //                    WHERE
    //                        rms_external_repo_table_pk =@rms_external_repo_table_pk AND ser_id=@ser_id;

    //                   ---------if no updated happeded, then insert one--------------------------
    //                    INSERT INTO
    //                                OneDriveFile(rms_external_repo_table_pk,
    //                                        ser_downloadUrl,
    //                                        ser_createdDateTime,
    //                                        ser_cTag,
    //                                        ser_eTag,
    //                                        ser_id,
    //                                        ser_lastModifiedDateTime,
    //                                        ser_name,
    //                                        ser_size,
    //                                        ser_webUrl,
    //                                        ser_reactions_commentCount,
    //                                        ser_createdBy_application_displayName,
    //                                        ser_createdBy_application_id,
    //                                        ser_createdBy_user_displayName,
    //                                        ser_createdBy_user_id,
    //                                        ser_lastModifiedBy_application_displayName,
    //                                        ser_lastModifiedBy_application_id,
    //                                        ser_lastModifiedBy_user_displayName,
    //                                        ser_lastModifiedBy_user_id,
    //                                        ser_parentReference_driveId,
    //                                        ser_parentReference_driveType,
    //                                        ser_parentReference_id,
    //                                        ser_parentReference_path,
    //                                        ser_fileSystemInfo_createdDateTime,
    //                                        ser_fileSystemInfo_lastModifiedDateTime,
    //                                        ser_isFolder,
    //                                        ser_folder_childCount,
    //                                        ser_folder_view_viewType,
    //                                        ser_folder_view_sortBy,
    //                                        ser_folder_view_sortOrder,
    //                                        ser_specialFolder_name,
    //                                        ser_file_mimeType,
    //                                        ser_hashes_quickXorHash,
    //                                        ser_hashes_sha1Hash)
    //                    SELECT 
    //                            @rms_external_repo_table_pk,
    //                            @ser_downloadUrl,
    //                            @ser_createdDateTime,
    //                            @ser_cTag,
    //                            @ser_eTag,
    //                            @ser_id,
    //                            @ser_lastModifiedDateTime,
    //                            @ser_name,
    //                            @ser_size,
    //                            @ser_webUrl,
    //                            @ser_reactions_commentCount,
    //                            @ser_createdBy_application_displayName,
    //                            @ser_createdBy_application_id,
    //                            @ser_createdBy_user_displayName,
    //                            @ser_createdBy_user_id,
    //                            @ser_lastModifiedBy_application_displayName,
    //                            @ser_lastModifiedBy_application_id,
    //                            @ser_lastModifiedBy_user_displayName,
    //                            @ser_lastModifiedBy_user_id,
    //                            @ser_parentReference_driveId,
    //                            @ser_parentReference_driveType,
    //                            @ser_parentReference_id,
    //                            @ser_parentReference_path,
    //                            @ser_fileSystemInfo_createdDateTime,
    //                            @ser_fileSystemInfo_lastModifiedDateTime,
    //                            @ser_isFolder,
    //                            @ser_folder_childCount,
    //                            @ser_folder_view_viewType,
    //                            @ser_folder_view_sortBy,
    //                            @ser_folder_view_sortOrder,
    //                            @ser_specialFolder_name,
    //                            @ser_file_mimeType,
    //                            @ser_hashes_quickXorHash,
    //                            @ser_hashes_sha1Hash
    //                     WHERE
    //                           (SELECT changes() =0);
    //            ";
    //        List<SQLiteParameter> parameters = new List<SQLiteParameter>();
    //        try
    //        {
    //            parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
    //            parameters.Add(new SQLiteParameter("@ser_createdDateTime", item.createdDateTime));
    //            parameters.Add(new SQLiteParameter("@ser_cTag", item.cTag));
    //            parameters.Add(new SQLiteParameter("@ser_eTag", item.eTag));
    //            parameters.Add(new SQLiteParameter("@ser_id", item.id));
    //            parameters.Add(new SQLiteParameter("@ser_lastModifiedDateTime", item.lastModifiedDateTime));
    //            parameters.Add(new SQLiteParameter("@ser_name", item.name));
    //            parameters.Add(new SQLiteParameter("@ser_size", item.size));
    //            parameters.Add(new SQLiteParameter("@ser_webUrl", item.webUrl));
    //            parameters.Add(new SQLiteParameter("@ser_reactions_commentCount", item.reactions.commentCount));
    //            parameters.Add(new SQLiteParameter("@ser_createdBy_application_displayName", item.createdBy.application.displayName));
    //            parameters.Add(new SQLiteParameter("@ser_createdBy_application_id", item.createdBy.application.id));
    //            parameters.Add(new SQLiteParameter("@ser_createdBy_user_displayName", item.createdBy.user.displayName));
    //            parameters.Add(new SQLiteParameter("@ser_createdBy_user_id", item.createdBy.user.id));
    //            parameters.Add(new SQLiteParameter("@ser_lastModifiedBy_application_displayName", item.lastModifiedBy.application.displayName));
    //            parameters.Add(new SQLiteParameter("@ser_lastModifiedBy_application_id", item.lastModifiedBy.application.id));
    //            parameters.Add(new SQLiteParameter("@ser_lastModifiedBy_user_displayName", item.lastModifiedBy.user.displayName));
    //            parameters.Add(new SQLiteParameter("@ser_lastModifiedBy_user_id", item.lastModifiedBy.user.id));
    //            parameters.Add(new SQLiteParameter("@ser_parentReference_driveId", item.parentReference.driveId));
    //            parameters.Add(new SQLiteParameter("@ser_parentReference_driveType", item.parentReference.driveType));
    //            parameters.Add(new SQLiteParameter("@ser_parentReference_id", item.parentReference.id));
    //            parameters.Add(new SQLiteParameter("@ser_parentReference_path", item.parentReference.path));
    //            parameters.Add(new SQLiteParameter("@ser_fileSystemInfo_createdDateTime", item.fileSystemInfo.createdDateTime));
    //            parameters.Add(new SQLiteParameter("@ser_fileSystemInfo_lastModifiedDateTime", item.fileSystemInfo.lastModifiedDateTime));

    //            //==================

    //            parameters.Add(new SQLiteParameter("@ser_isFolder", item.isFolder));

    //            if (item.isFolder == 0)
    //            {
    //                FileItem fileItem = item as FileItem;
    //                parameters.Add(new SQLiteParameter("@ser_file_mimeType", fileItem.file.mimeType));
    //                parameters.Add(new SQLiteParameter("@ser_hashes_quickXorHash", fileItem.file.hashes.quickXorHash));
    //                parameters.Add(new SQLiteParameter("@ser_hashes_sha1Hash", fileItem.file.hashes.sha1Hash));
    //                parameters.Add(new SQLiteParameter("@ser_downloadUrl", fileItem.downloadUrl));

    //                parameters.Add(new SQLiteParameter("@ser_folder_childCount", string.Empty));
    //                parameters.Add(new SQLiteParameter("@ser_folder_view_viewType", string.Empty));
    //                parameters.Add(new SQLiteParameter("@ser_folder_view_sortBy", string.Empty));
    //                parameters.Add(new SQLiteParameter("@ser_folder_view_sortOrder", string.Empty));
    //                parameters.Add(new SQLiteParameter("@ser_specialFolder_name", string.Empty));

    //            }
    //            else if (item.isFolder == 1)
    //            {
    //                FolderItem folderItem = item as FolderItem;
    //                parameters.Add(new SQLiteParameter("@ser_folder_childCount", folderItem.folder.childCount));
    //                parameters.Add(new SQLiteParameter("@ser_folder_view_viewType", folderItem.folder.view.viewType));
    //                parameters.Add(new SQLiteParameter("@ser_folder_view_sortBy", folderItem.folder.view.sortBy));
    //                parameters.Add(new SQLiteParameter("@ser_folder_view_sortOrder", folderItem.folder.view.sortOrder));
    //                parameters.Add(new SQLiteParameter("@ser_specialFolder_name", folderItem.specialFolder.name));

    //                parameters.Add(new SQLiteParameter("@ser_file_mimeType", string.Empty));
    //                parameters.Add(new SQLiteParameter("@ser_hashes_quickXorHash", string.Empty));
    //                parameters.Add(new SQLiteParameter("@ser_hashes_sha1Hash", string.Empty));
    //                parameters.Add(new SQLiteParameter("@ser_downloadUrl", string.Empty));
    //            }

    //            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }

    //    public static KeyValuePair<String, SQLiteParameter[]> Query_SQL_by_parent_file_id(int rms_external_repo_table_pk, string parentFileId)
    //    {
    //        string sql = @"
    //          SELECT   
    //            *
    //        FROM
    //            OneDriveFile
    //        WHERE
    //             rms_external_repo_table_pk=@rms_external_repo_table_pk AND ser_parentReference_id =@parentFileId;                   
    //        ";
    //        SQLiteParameter[] parameters = {
    //          new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
    //            new SQLiteParameter("@parentFileId",parentFileId)
    //        };
    //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
    //    }

    //    public static KeyValuePair<String, SQLiteParameter[]> Query_SQL_by_parent_file_Path(int rms_external_repo_table_pk, string parentFilePath)
    //    {
    //        string sql = @"
    //          SELECT   
    //            *
    //        FROM
    //            OneDriveFile
    //        WHERE
    //             rms_external_repo_table_pk=@rms_external_repo_table_pk AND ser_parentReference_path =@parentFilePath;                   
    //        ";
    //        SQLiteParameter[] parameters = {
    //          new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
    //            new SQLiteParameter("@parentFilePath",parentFilePath)
    //        };
    //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
    //    }

    //    public static KeyValuePair<String, SQLiteParameter[]> Query_SQL_by_file_id(int rms_external_repo_table_pk, string fileId)
    //    {
    //        string sql = @"
    //          SELECT   
    //             *
    //            FROM
    //                OneDriveFile
    //            WHERE
    //                 rms_external_repo_table_pk=@rms_external_repo_table_pk AND ser_file_id=@fileId;                 
    //        ";
    //        SQLiteParameter[] parameters = {
    //            new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
    //            new SQLiteParameter("@fileId",fileId)
    //        };
    //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
    //    }

    //    public static KeyValuePair<String, SQLiteParameter[]> Query_root_folder_SQL(int rms_external_repo_table_pk)
    //    {
    //        string sql = @"
    //          SELECT   
    //             *
    //            FROM
    //                OneDriveFile
    //            WHERE
    //                 rms_external_repo_table_pk=@rms_external_repo_table_pk AND ser_name=@ser_name;                 
    //        ";
    //        SQLiteParameter[] parameters = {
    //            new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
    //            new SQLiteParameter("@ser_name","root")
    //        };
    //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
    //    }

    //    public static KeyValuePair<String, SQLiteParameter[]> Query_files_by_repoId_SQL(int rms_external_repo_table_pk)
    //    {
    //        string sql = @"
    //          SELECT   
    //             *
    //            FROM
    //                OneDriveFile
    //            WHERE
    //                 rms_external_repo_table_pk=@rms_external_repo_table_pk;                 
    //        ";
    //        SQLiteParameter[] parameters = {
    //            new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
    //        };
    //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
    //    }

    //    public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(int table_pk, int newStatus)
    //    {
    //        string sql = @"
    //            UPDATE 
    //                OneDriveFile
    //            SET
    //                status=@newStatus
    //            WHERE
    //                id=@table_pk;
    //        ";
    //        SQLiteParameter[] parameters = {
    //            new SQLiteParameter("@table_pk",table_pk),
    //            new SQLiteParameter("@newStatus",newStatus)
    //        };
    //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
    //    }

    //    public static KeyValuePair<String, SQLiteParameter[]> Update_IsOffline_SQL(int rms_external_repo_table_pk, string ser_id, bool newMark)
    //    {
    //        string sql = @"
    //            UPDATE 
    //                OneDriveFile
    //            SET
    //                is_offline=@newMark
    //            WHERE
    //                rms_external_repo_table_pk=@rms_external_repo_table_pk AND ser_id=@ser_id;
    //        ";
    //        SQLiteParameter[] parameters = {
    //            new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
    //            new SQLiteParameter("@ser_id",ser_id),
    //            new SQLiteParameter("@newMark",newMark?1:0),
    //        };
    //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
    //    }

    //}
}
