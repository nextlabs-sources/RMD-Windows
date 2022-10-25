using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.oneDrive
{
    public class OneDriveFoldersDao
    {
        public static readonly string SQL_Create_Table_OneDriveFolders = @"
                     CREATE TABLE IF NOT EXISTS OneDriveFolders(
                                   id                                 integer                 NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, 
                                   rms_external_repo_table_pk         integer                 default 0,
                                   file_common_table_pk               integer                 default 0,
                                   folder_childCount                  integer                 NOT NULL default 0,
                                   folder_view_viewType               varchar(255)            NOT NULL default '',
                                   folder_view_sortBy                 varchar(255)            NOT NULL default '',
                                   folder_view_sortOrder              varchar(255)            NOT NULL default '',
                                   specialFolder_name                 varchar(255)            NOT NULL default '',
                                   isRootFolder                       integer                 NOT NULL default 0,
                                   item_id                            varchar(255)            NOT NULL default '',
                                   UNIQUE(rms_external_repo_table_pk, file_common_table_pk, item_id),
                                   foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade,
                                   foreign key(file_common_table_pk) references OneDriveFileCommon(id) on delete cascade);
                    ";

        //public static KeyValuePair<String, SQLiteParameter[]> Upsert_OneDriveFolder_SQL(int rms_external_repo_table_pk,
        //                                                                                string file_common_table_pk,
        //                                                                                FolderItem folderItem)
        //{
        //    string sql = @"UPDATE 
        //                        OneDriveFolders 
        //                   SET
        //                        folder_childCount=@folder_childCount,
        //                        folder_view_viewType=@folder_view_viewType,
        //                        folder_view_sortBy=@folder_view_sortBy,
        //                        folder_view_sortOrder=@folder_view_sortOrder,
        //                        specialFolder_name=@specialFolder_name
        //                   WHERE
        //                      rms_external_repo_table_pk =@rms_external_repo_table_pk 
        //                    AND 
        //                      file_common_table_pk=@file_common_table_pk 
        //                    AND
        //                      item_id=@item_id  
        //                    ;

        //                INSERT INTO OneDriveFolders(
        //                                            rms_external_repo_table_pk,
        //                                            file_common_table_pk,
        //                                            folder_childCount,
        //                                            folder_view_viewType,
        //                                            folder_view_sortBy,
        //                                            folder_view_sortOrder,
        //                                            specialFolder_name,
        //                                            isRootFolder,
        //                                            item_id
        //                                           )
        //                            SELECT
        //                                    @rms_external_repo_table_pk,
        //                                    @file_common_table_pk,
        //                                    @folder_childCount,
        //                                    @folder_view_viewType,
        //                                    @folder_view_sortBy,
        //                                    @folder_view_sortOrder,
        //                                    @specialFolder_name,
        //                                    @isRootFolder,
        //                                    @item_id
        //                            WHERE
        //                                (SELECT changes() =0);
        //                 ";
        //    List<SQLiteParameter> parameters = new List<SQLiteParameter>();
        //    try
        //    {
        //        parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
        //        parameters.Add(new SQLiteParameter("@file_common_table_pk", file_common_table_pk));
        //        parameters.Add(new SQLiteParameter("@folder_childCount", folderItem.folder.childCount));
        //        parameters.Add(new SQLiteParameter("@folder_view_viewType", folderItem.folder.view.viewType));
        //        parameters.Add(new SQLiteParameter("@folder_view_sortBy", folderItem.folder.view.sortBy));
        //        parameters.Add(new SQLiteParameter("@folder_view_sortOrder", folderItem.folder.view.sortOrder));
        //        parameters.Add(new SQLiteParameter("@specialFolder_name", folderItem.specialFolder.name));
        //        parameters.Add(new SQLiteParameter("@isRootFolder", folderItem.isRootFolder));
        //        parameters.Add(new SQLiteParameter("@item_id", folderItem.id));
        //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public static KeyValuePair<String, SQLiteParameter[]> Update_Folder_SQL(int rms_external_repo_table_pk, FolderItem folderItem)
        {
            string sql = @"
                      UPDATE   
                        OneDriveFolders
                      SET
                        folder_childCount=@folder_childCount,
                        folder_view_viewType=@folder_view_viewType,
                        folder_view_sortBy=@folder_view_sortBy,
                        folder_view_sortOrder=@folder_view_sortOrder,
                        specialFolder_name=@specialFolder_name
                      WHERE
                        rms_external_repo_table_pk=@rms_external_repo_table_pk AND item_id=@item_id;
                   ";

            SQLiteParameter[] parameters = {
                      new SQLiteParameter("@folder_childCount",folderItem.folder.childCount),
                      new SQLiteParameter("@folder_view_viewType",folderItem.folder.view.viewType),
                      new SQLiteParameter("@folder_view_sortBy",folderItem.folder.view.sortBy),
                      new SQLiteParameter("@folder_view_sortOrder",folderItem.folder.view.sortOrder),
                      new SQLiteParameter("@specialFolder_name",folderItem.specialFolder.name),
                      new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                      new SQLiteParameter("@item_id",folderItem.id)
                    };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Query_OneDriveRootFolder_SQL(int rms_external_repo_table_pk)
        {
            string sql = @"
                      SELECT   
                        *
                      FROM
                         OneDriveFolders
                      WHERE
                         rms_external_repo_table_pk=@rms_external_repo_table_pk AND isRootFolder=1;                   
                    ";
            SQLiteParameter[] parameters = {
                      new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk)
                    };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Insert_SQL(int rms_external_repo_table_pk, string file_common_table_pk, FolderItem folderItem)
        {
            string sql = @"INSERT INTO
                             OneDriveFolders(rms_external_repo_table_pk,
                                             file_common_table_pk,
                                             folder_childCount,
                                             folder_view_viewType,
                                             folder_view_sortBy,
                                             folder_view_sortOrder,
                                             specialFolder_name,
                                             isRootFolder,
                                             item_id
                                             )
                             VALUES(@rms_external_repo_table_pk,
                                    @file_common_table_pk,
                                    @folder_childCount,
                                    @folder_view_viewType,
                                    @folder_view_sortBy,
                                    @folder_view_sortOrder,
                                    @specialFolder_name,
                                    @isRootFolder,
                                    @item_id);
                    ";
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                parameters.Add(new SQLiteParameter("@file_common_table_pk", file_common_table_pk));
                parameters.Add(new SQLiteParameter("@folder_childCount", folderItem.folder.childCount));
                parameters.Add(new SQLiteParameter("@folder_view_viewType", folderItem.folder.view.viewType));
                parameters.Add(new SQLiteParameter("@folder_view_sortBy", folderItem.folder.view.sortBy));
                parameters.Add(new SQLiteParameter("@folder_view_sortOrder", folderItem.folder.view.sortOrder));
                parameters.Add(new SQLiteParameter("@specialFolder_name", folderItem.specialFolder.name));
                parameters.Add(new SQLiteParameter("@isRootFolder", folderItem.isRootFolder));
                parameters.Add(new SQLiteParameter("@item_id", folderItem.id));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
