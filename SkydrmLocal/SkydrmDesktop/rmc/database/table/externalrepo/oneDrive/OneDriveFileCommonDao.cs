using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.oneDrive
{
    public class OneDriveFileCommonDao
    {
        public static readonly string SQL_Create_Table_OneDriveFileCommon = @"
               CREATE TABLE IF NOT EXISTS OneDriveFileCommon(
                            id                             integer                  NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, 
                            rms_external_repo_table_pk     integer                  default 0,
                            createdDateTime                varchar(255)             NOT NULL default '',
                            cTag                           varchar(255)             NOT NULL default '',
                            eTag                           varchar(255)             NOT NULL default '',
                            item_id                        varchar(255)             NOT NULL default '',
                            lastModifiedDateTime           varchar(255)             NOT NULL default '',
                            name                           varchar(255)             NOT NULL default '',
                            size                           integer                  NOT NULL default 0,
                            webUrl                         varchar(255)             NOT NULL default '',
                            reactions_commentCount         integer                  NOT NULL default 0,
                            createdBy_application_displayName    varchar(255)       NOT NULL default '',
                            createdBy_application_id             varchar(255)       NOT NULL default '',
                            createdBy_user_displayName           varchar(255)       NOT NULL default '',
                            createdBy_user_id                    varchar(255)       NOT NULL default '',
                            lastModifiedBy_application_displayName    varchar(255)            NOT NULL default '',
                            lastModifiedBy_application_id             varchar(255)            NOT NULL default '',
                            lastModifiedBy_user_displayName           varchar(255)            NOT NULL default '',
                            lastModifiedBy_user_id                    varchar(255)            NOT NULL default '',
                            parentReference_driveId                 varchar(255)              NOT NULL default '',
                            parentReference_driveType               varchar(255)              NOT NULL default '',
                            parentReference_id                      varchar(255)              NOT NULL default '',
                            parentReference_path                    varchar(255)              NOT NULL default '',
                            fileSystemInfo_createdDateTime          varchar(255)              NOT NULL default '',
                            fileSystemInfo_lastModifiedDateTime     varchar(255)              NOT NULL default '',
                            isFolder                                integer                   NOT NULL default 0,
                            UNIQUE(rms_external_repo_table_pk, item_id),
                            foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade);
           ";

        //public static KeyValuePair<String, SQLiteParameter[]> Upsert_OneDriveFileCommon_SQL(int rms_external_repo_table_pk, ValueItem item)
        //{
        //    string sql = @"UPDATE 
        //                    OneDriveFileCommon 
        //                    SET
        //                        createdDateTime=@createdDateTime, 
        //                        cTag=@cTag,
        //                        eTag=@eTag,
        //                        item_id=@item_id,
        //                        lastModifiedDateTime=@lastModifiedDateTime,
        //                        name=@name,
        //                        size=@size,
        //                        webUrl=@webUrl,
        //                        reactions_commentCount=@reactions_commentCount,
        //                        createdBy_application_displayName=@createdBy_application_displayName,
        //                        createdBy_application_id=@createdBy_application_id,
        //                        createdBy_user_displayName=@createdBy_user_displayName,
        //                        createdBy_user_id=@createdBy_user_id,
        //                        lastModifiedBy_application_displayName=@lastModifiedBy_application_displayName,
        //                        lastModifiedBy_application_id=@lastModifiedBy_application_id,
        //                        lastModifiedBy_user_displayName=@lastModifiedBy_user_displayName,
        //                        lastModifiedBy_user_id=@lastModifiedBy_user_id,
        //                        parentReference_driveId=@parentReference_driveId,
        //                        parentReference_driveType=@parentReference_driveType,
        //                        parentReference_id=@parentReference_id,
        //                        parentReference_path=@parentReference_path,
        //                        fileSystemInfo_createdDateTime=@fileSystemInfo_createdDateTime,
        //                        fileSystemInfo_lastModifiedDateTime=@fileSystemInfo_lastModifiedDateTime
        //                  WHERE
        //                      rms_external_repo_table_pk =@rms_external_repo_table_pk AND item_id=@item_id;

        //                  INSERT INTO
        //                        OneDriveFileCommon(
        //                                            rms_external_repo_table_pk,
        //                                            createdDateTime,
        //                                            cTag,
        //                                            eTag,
        //                                            item_id,
        //                                            lastModifiedDateTime,
        //                                            name,
        //                                            size,
        //                                            webUrl,
        //                                            reactions_commentCount,
        //                                            createdBy_application_displayName,
        //                                            createdBy_application_id,
        //                                            createdBy_user_displayName,
        //                                            createdBy_user_id,
        //                                            lastModifiedBy_application_displayName,
        //                                            lastModifiedBy_application_id,
        //                                            lastModifiedBy_user_displayName,
        //                                            lastModifiedBy_user_id,
        //                                            parentReference_driveId,
        //                                            parentReference_driveType,
        //                                            parentReference_id,
        //                                            parentReference_path,
        //                                            fileSystemInfo_createdDateTime,
        //                                            fileSystemInfo_lastModifiedDateTime
        //                                           )
        //                         SELECT 
        //                                            @rms_external_repo_table_pk,
        //                                            @createdDateTime,
        //                                            @cTag,
        //                                            @eTag,
        //                                            @item_id,
        //                                            @lastModifiedDateTime,
        //                                            @name,
        //                                            @size,
        //                                            @webUrl,
        //                                            @reactions_commentCount,
        //                                            @createdBy_application_displayName,
        //                                            @createdBy_application_id,
        //                                            @createdBy_user_displayName,
        //                                            @createdBy_user_id,
        //                                            @lastModifiedBy_application_displayName,
        //                                            @lastModifiedBy_application_id,
        //                                            @lastModifiedBy_user_displayName,
        //                                            @lastModifiedBy_user_id,
        //                                            @parentReference_driveId,
        //                                            @parentReference_driveType,
        //                                            @parentReference_id,
        //                                            @parentReference_path,
        //                                            @fileSystemInfo_createdDateTime,
        //                                            @fileSystemInfo_lastModifiedDateTime
        //                          WHERE
        //                               (SELECT changes() =0);
        //                   ";

        //    List<SQLiteParameter> parameters = new List<SQLiteParameter>();
        //    try
        //    {
        //        parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
        //        parameters.Add(new SQLiteParameter("@createdDateTime", item.createdDateTime));
        //        parameters.Add(new SQLiteParameter("@cTag", item.cTag));
        //        parameters.Add(new SQLiteParameter("@eTag", item.eTag));
        //        parameters.Add(new SQLiteParameter("@item_id", item.id));
        //        parameters.Add(new SQLiteParameter("@lastModifiedDateTime", item.lastModifiedDateTime));
        //        parameters.Add(new SQLiteParameter("@name", item.name));
        //        parameters.Add(new SQLiteParameter("@size", item.size));
        //        parameters.Add(new SQLiteParameter("@webUrl", item.webUrl));
        //        parameters.Add(new SQLiteParameter("@reactions_commentCount", item.reactions.commentCount));
        //        parameters.Add(new SQLiteParameter("@createdBy_application_displayName", item.createdBy.application.displayName));
        //        parameters.Add(new SQLiteParameter("@createdBy_application_id", item.createdBy.application.id));
        //        parameters.Add(new SQLiteParameter("@createdBy_user_displayName", item.createdBy.user.displayName));
        //        parameters.Add(new SQLiteParameter("@createdBy_user_id", item.createdBy.user.id));
        //        parameters.Add(new SQLiteParameter("@lastModifiedBy_application_displayName", item.lastModifiedBy.application.displayName));
        //        parameters.Add(new SQLiteParameter("@lastModifiedBy_application_id", item.lastModifiedBy.application.id));
        //        parameters.Add(new SQLiteParameter("@lastModifiedBy_user_displayName", item.lastModifiedBy.user.displayName));
        //        parameters.Add(new SQLiteParameter("@lastModifiedBy_user_id", item.lastModifiedBy.user.id));
        //        parameters.Add(new SQLiteParameter("@parentReference_driveId", item.parentReference.driveId));
        //        parameters.Add(new SQLiteParameter("@parentReference_driveType", item.parentReference.driveType));
        //        parameters.Add(new SQLiteParameter("@parentReference_id", item.parentReference.id));
        //        parameters.Add(new SQLiteParameter("@parentReference_path", item.parentReference.path));
        //        parameters.Add(new SQLiteParameter("@fileSystemInfo_createdDateTime", item.fileSystemInfo.createdDateTime));
        //        parameters.Add(new SQLiteParameter("@fileSystemInfo_lastModifiedDateTime", item.fileSystemInfo.lastModifiedDateTime));
        //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_SQL(int rms_external_repo_table_pk, ValueItem item)
        {
            string sql = @"UPDATE 
                            OneDriveFileCommon 
                            SET
                                createdDateTime=@createdDateTime, 
                                cTag=@cTag,
                                eTag=@eTag,
                                item_id=@item_id,
                                lastModifiedDateTime=@lastModifiedDateTime,
                                name=@name,
                                size=@size,
                                webUrl=@webUrl,
                                reactions_commentCount=@reactions_commentCount,
                                createdBy_application_displayName=@createdBy_application_displayName,
                                createdBy_application_id=@createdBy_application_id,
                                createdBy_user_displayName=@createdBy_user_displayName,
                                createdBy_user_id=@createdBy_user_id,
                                lastModifiedBy_application_displayName=@lastModifiedBy_application_displayName,
                                lastModifiedBy_application_id=@lastModifiedBy_application_id,
                                lastModifiedBy_user_displayName=@lastModifiedBy_user_displayName,
                                lastModifiedBy_user_id=@lastModifiedBy_user_id,
                                parentReference_driveId=@parentReference_driveId,
                                parentReference_driveType=@parentReference_driveType,
                                parentReference_id=@parentReference_id,
                                parentReference_path=@parentReference_path,
                                fileSystemInfo_createdDateTime=@fileSystemInfo_createdDateTime,
                                fileSystemInfo_lastModifiedDateTime=@fileSystemInfo_lastModifiedDateTime
                          WHERE
                              rms_external_repo_table_pk =@rms_external_repo_table_pk AND item_id=@item_id;
                   ";

                    List<SQLiteParameter> parameters = new List<SQLiteParameter>();
                    try
                    {
                        parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                        parameters.Add(new SQLiteParameter("@createdDateTime", item.createdDateTime));
                        parameters.Add(new SQLiteParameter("@cTag", item.cTag));
                        parameters.Add(new SQLiteParameter("@eTag", item.eTag));
                        parameters.Add(new SQLiteParameter("@item_id", item.id));
                        parameters.Add(new SQLiteParameter("@lastModifiedDateTime", item.lastModifiedDateTime));
                        parameters.Add(new SQLiteParameter("@name", item.name));
                        parameters.Add(new SQLiteParameter("@size", item.size));
                        parameters.Add(new SQLiteParameter("@webUrl", item.webUrl));
                        parameters.Add(new SQLiteParameter("@reactions_commentCount", item.reactions.commentCount));
                        parameters.Add(new SQLiteParameter("@createdBy_application_displayName", item.createdBy.application.displayName));
                        parameters.Add(new SQLiteParameter("@createdBy_application_id", item.createdBy.application.id));
                        parameters.Add(new SQLiteParameter("@createdBy_user_displayName", item.createdBy.user.displayName));
                        parameters.Add(new SQLiteParameter("@createdBy_user_id", item.createdBy.user.id));
                        parameters.Add(new SQLiteParameter("@lastModifiedBy_application_displayName", item.lastModifiedBy.application.displayName));
                        parameters.Add(new SQLiteParameter("@lastModifiedBy_application_id", item.lastModifiedBy.application.id));
                        parameters.Add(new SQLiteParameter("@lastModifiedBy_user_displayName", item.lastModifiedBy.user.displayName));
                        parameters.Add(new SQLiteParameter("@lastModifiedBy_user_id", item.lastModifiedBy.user.id));
                        parameters.Add(new SQLiteParameter("@parentReference_driveId", item.parentReference.driveId));
                        parameters.Add(new SQLiteParameter("@parentReference_driveType", item.parentReference.driveType));
                        parameters.Add(new SQLiteParameter("@parentReference_id", item.parentReference.id));
                        parameters.Add(new SQLiteParameter("@parentReference_path", item.parentReference.path));
                        parameters.Add(new SQLiteParameter("@fileSystemInfo_createdDateTime", item.fileSystemInfo.createdDateTime));
                        parameters.Add(new SQLiteParameter("@fileSystemInfo_lastModifiedDateTime", item.fileSystemInfo.lastModifiedDateTime));
                        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int rms_external_repo_table_pk, string item_id)
        {
            string sql = @"SELECT 
                                *
                        FROM
                            OneDriveFileCommon
                        WHERE
                            rms_external_repo_table_pk=@rms_external_repo_table_pk AND item_id =@item_id;
                    ";

            SQLiteParameter[] parameters = {
                        new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                        new SQLiteParameter("@item_id",item_id)
                    };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_By_Table_PK_SQL(int rms_external_repo_table_pk, int pk)
        {
            string sql = @"SELECT 
                                *
                        FROM
                            OneDriveFileCommon
                        WHERE
                            rms_external_repo_table_pk=@rms_external_repo_table_pk
                            AND 
                            id=@id;
                    ";

            SQLiteParameter[] parameters = {
                        new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                        new SQLiteParameter("@id",pk)
                    };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_Children_SQL(int rms_external_repo_table_pk, string parent_item_id)
        {
            string sql = @"SELECT 
                                *
                        FROM
                            OneDriveFileCommon
                        WHERE
                            rms_external_repo_table_pk=@rms_external_repo_table_pk AND parentReference_id =@parent_item_id;
                    ";

            SQLiteParameter[] parameters = {
                        new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                        new SQLiteParameter("@parent_item_id",parent_item_id)
                    };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_Children_SQL(int rms_external_repo_table_pk, List<string> parent_item_id_list, bool isOnlyFile)
        {
            List<string> tempValues = parent_item_id_list.ConvertAll(x =>"'"+ x +"'");
            var values = string.Join(",", tempValues.ToArray());
            string sql = string.Empty;
            if (isOnlyFile)
            {
                sql = string.Format(@"SELECT * FROM OneDriveFileCommon WHERE rms_external_repo_table_pk=@rms_external_repo_table_pk AND isFolder=0 AND parentReference_id IN ({0})", values);
            }
            else
            {
                sql = string.Format(@"SELECT * FROM OneDriveFileCommon WHERE rms_external_repo_table_pk=@rms_external_repo_table_pk AND parentReference_id IN ({0})", values);
            }
            SQLiteParameter[] parameters = {
                        new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk)
                       };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        //var ids = string.Join(",", lstids.Select(x => x.ToString()).ToArray());
        //string query = string.Format("SELECT * FROM Products WHERE ID in ({0})", ids)

        public static KeyValuePair<String, SQLiteParameter[]> Delete_SQL(int rms_external_repo_table_pk, List<string> item_id_list)
        {
            List<string> tempValues = item_id_list.ConvertAll(x => "'" + x + "'");
            var values = string.Join(",", tempValues.ToArray());
            string sql = string.Format(@"DELETE FROM OneDriveFileCommon WHERE rms_external_repo_table_pk=@rms_external_repo_table_pk AND item_id IN ({0})", values);
            SQLiteParameter[] parameters = {
                        new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk)
                       };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Insert_SQL(int rms_external_repo_table_pk, ValueItem item)
        {
            string sql = @"INSERT INTO 
                            OneDriveFileCommon(
                                                rms_external_repo_table_pk,
                                                createdDateTime,
                                                cTag,
                                                eTag,
                                                item_id,
                                                lastModifiedDateTime,
                                                name,
                                                size,
                                                webUrl,
                                                reactions_commentCount,
                                                createdBy_application_displayName,
                                                createdBy_application_id,
                                                createdBy_user_displayName,
                                                createdBy_user_id,
                                                lastModifiedBy_application_displayName,
                                                lastModifiedBy_application_id,
                                                lastModifiedBy_user_displayName,
                                                lastModifiedBy_user_id,
                                                parentReference_driveId,
                                                parentReference_driveType,
                                                parentReference_id,
                                                parentReference_path,
                                                fileSystemInfo_createdDateTime,
                                                fileSystemInfo_lastModifiedDateTime,
                                                isFolder
                                               )
                            VALUES(
                                    @rms_external_repo_table_pk,
                                    @createdDateTime,
                                    @cTag,
                                    @eTag,
                                    @item_id,
                                    @lastModifiedDateTime,
                                    @name,
                                    @size,
                                    @webUrl,
                                    @reactions_commentCount,
                                    @createdBy_application_displayName,
                                    @createdBy_application_id,
                                    @createdBy_user_displayName,
                                    @createdBy_user_id,
                                    @lastModifiedBy_application_displayName,
                                    @lastModifiedBy_application_id,
                                    @lastModifiedBy_user_displayName,
                                    @lastModifiedBy_user_id,
                                    @parentReference_driveId,
                                    @parentReference_driveType,
                                    @parentReference_id,
                                    @parentReference_path,
                                    @fileSystemInfo_createdDateTime,
                                    @fileSystemInfo_lastModifiedDateTime,
                                    @isFolder
                                  );
                        ";
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                parameters.Add(new SQLiteParameter("@createdDateTime", item.createdDateTime));
                parameters.Add(new SQLiteParameter("@cTag", item.cTag));
                parameters.Add(new SQLiteParameter("@eTag", item.eTag));
                parameters.Add(new SQLiteParameter("@item_id", item.id));
                parameters.Add(new SQLiteParameter("@lastModifiedDateTime", item.lastModifiedDateTime));
                parameters.Add(new SQLiteParameter("@name", item.name));
                parameters.Add(new SQLiteParameter("@size", item.size));
                parameters.Add(new SQLiteParameter("@webUrl", item.webUrl));
                parameters.Add(new SQLiteParameter("@reactions_commentCount", item.reactions.commentCount));
                parameters.Add(new SQLiteParameter("@createdBy_application_displayName", item.createdBy.application.displayName));
                parameters.Add(new SQLiteParameter("@createdBy_application_id", item.createdBy.application.id));
                parameters.Add(new SQLiteParameter("@createdBy_user_displayName", item.createdBy.user.displayName));
                parameters.Add(new SQLiteParameter("@createdBy_user_id", item.createdBy.user.id));
                parameters.Add(new SQLiteParameter("@lastModifiedBy_application_displayName", item.lastModifiedBy.application.displayName));
                parameters.Add(new SQLiteParameter("@lastModifiedBy_application_id", item.lastModifiedBy.application.id));
                parameters.Add(new SQLiteParameter("@lastModifiedBy_user_displayName", item.lastModifiedBy.user.displayName));
                parameters.Add(new SQLiteParameter("@lastModifiedBy_user_id", item.lastModifiedBy.user.id));
                parameters.Add(new SQLiteParameter("@parentReference_driveId", item.parentReference.driveId));
                parameters.Add(new SQLiteParameter("@parentReference_driveType", item.parentReference.driveType));
                parameters.Add(new SQLiteParameter("@parentReference_id", item.parentReference.id));
                parameters.Add(new SQLiteParameter("@parentReference_path", item.parentReference.path));
                parameters.Add(new SQLiteParameter("@fileSystemInfo_createdDateTime", item.fileSystemInfo.createdDateTime));
                parameters.Add(new SQLiteParameter("@fileSystemInfo_lastModifiedDateTime", item.fileSystemInfo.lastModifiedDateTime));
                parameters.Add(new SQLiteParameter("@isFolder", item.isFolder));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_Children_folder_SQL(int rms_external_repo_table_pk, string parent_item_id)
        {
            string sql = @"SELECT 
                                *
                        FROM
                            OneDriveFileCommon
                        WHERE
                            rms_external_repo_table_pk=@rms_external_repo_table_pk 
                        AND 
                            parentReference_id =@parent_item_id
                        AND
                            isFolder=1;
                    ";

            SQLiteParameter[] parameters = {
                        new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                        new SQLiteParameter("@parent_item_id",parent_item_id)
                    };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

    }
}
