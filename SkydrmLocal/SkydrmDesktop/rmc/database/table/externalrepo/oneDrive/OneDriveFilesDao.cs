using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.oneDrive
{
    public class OneDriveFilesDao
    {
        public static readonly string SQL_Create_Table_OneDriveFiles = @"
                CREATE TABLE IF NOT EXISTS OneDriveFiles(
                                id                             integer                 NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                                rms_external_repo_table_pk     integer                 default 0,
                                file_common_table_pk           integer                 default 0,
                                downloadUrl                varchar(500)            NOT NULL default '',
                                file_mimeType              varchar(255)            NOT NULL default '',
                                hashes_quickXorHash        varchar(255)            NOT NULL default '',
                                hashes_sha1Hash            varchar(255)            NOT NULL default '',
                                item_id                    varchar(255)            NOT NULL default '',
                                UNIQUE(rms_external_repo_table_pk, file_common_table_pk, item_id),
                                foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade,
                                foreign key(file_common_table_pk) references OneDriveFileCommon(id) on delete cascade);
        ";

       // foreign key(folder_table_pk) references OneDriveFolders(id) on delete cascade);

        //public static KeyValuePair<String, SQLiteParameter[]> Upsert_OneDriveFile_SQL(int rms_external_repo_table_pk, 
        //                                                                              int file_common_table_pk,
        //                                                                              FileItem fileItem)
        //{
        //    string sql = @"UPDATE
        //                     OneDriveFiles
        //                   SET
        //                        downloadUrl=@downloadUrl,
        //                        file_mimeType=@file_mimeType,
        //                        hashes_quickXorHash=@hashes_quickXorHash,
        //                        hashes_sha1Hash=@hashes_sha1Hash
        //                   WHERE
        //                      rms_external_repo_table_pk =@rms_external_repo_table_pk AND item_id=@item_id;

        //                   INSERT INTO OneDriveFiles(
        //                                            rms_external_repo_table_pk,
        //                                            file_common_table_pk,
        //                                            downloadUrl,
        //                                            file_mimeType,
        //                                            hashes_quickXorHash,
        //                                            hashes_sha1Hash,
        //                                            item_id
        //                                            )
        //                   SELECT
        //                            @rms_external_repo_table_pk,
        //                            @file_common_table_pk,
        //                            @downloadUrl,
        //                            @file_mimeType,
        //                            @hashes_quickXorHash,
        //                            @hashes_sha1Hash,
        //                            @item_id
        //                   WHERE
        //                            (SELECT changes() =0);
                        
        //        ";
        //    List<SQLiteParameter> parameters = new List<SQLiteParameter>();
        //    try
        //    {
        //        parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
        //        parameters.Add(new SQLiteParameter("@file_common_table_pk", file_common_table_pk));
        //        parameters.Add(new SQLiteParameter("@downloadUrl", fileItem.downloadUrl));
        //        parameters.Add(new SQLiteParameter("@file_mimeType", fileItem.file.mimeType));
        //        parameters.Add(new SQLiteParameter("@hashes_quickXorHash", fileItem.file.hashes.quickXorHash));
        //        parameters.Add(new SQLiteParameter("@hashes_sha1Hash", fileItem.file.hashes.sha1Hash));
        //        parameters.Add(new SQLiteParameter("@item_id", fileItem.id));
        //        return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public static KeyValuePair<String, SQLiteParameter[]> Insert_SQL(int rms_external_repo_table_pk, string file_common_table_pk, FileItem fileItem)
        {
            string sql = @"INSERT INTO 
                            OneDriveFiles(rms_external_repo_table_pk,
                                          file_common_table_pk,
                                          downloadUrl,
                                          file_mimeType,
                                          hashes_quickXorHash,
                                          hashes_sha1Hash,
                                          item_id
                                         )
                             VALUES(@rms_external_repo_table_pk,
                                    @file_common_table_pk,
                                    @downloadUrl,
                                    @file_mimeType,
                                    @hashes_quickXorHash,
                                    @hashes_sha1Hash,
                                    @item_id
                                   );
                ";
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                parameters.Add(new SQLiteParameter("@file_common_table_pk", file_common_table_pk));
                parameters.Add(new SQLiteParameter("@downloadUrl", fileItem.downloadUrl));
                parameters.Add(new SQLiteParameter("@file_mimeType", fileItem.file.mimeType));
                parameters.Add(new SQLiteParameter("@hashes_quickXorHash", fileItem.file.hashes.quickXorHash));
                parameters.Add(new SQLiteParameter("@hashes_sha1Hash", fileItem.file.hashes.sha1Hash));
                parameters.Add(new SQLiteParameter("@item_id", fileItem.id));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_SQL(int rms_external_repo_table_pk, FileItem fileItem)
        {
            string sql = @"UPDATE 
                            OneDriveFiles
                           SET  
                                downloadUrl=@downloadUrl,
                                file_mimeType=@file_mimeType,
                                hashes_quickXorHash=@hashes_quickXorHash,
                                hashes_sha1Hash=@hashes_sha1Hash
                           WHERE
                                rms_external_repo_table_pk =@rms_external_repo_table_pk AND item_id=@item_id;
                        ";

            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                parameters.Add(new SQLiteParameter("@downloadUrl", fileItem.downloadUrl));
                parameters.Add(new SQLiteParameter("@file_mimeType", fileItem.file.mimeType));
                parameters.Add(new SQLiteParameter("@hashes_quickXorHash", fileItem.file.hashes.quickXorHash));
                parameters.Add(new SQLiteParameter("@hashes_sha1Hash", fileItem.file.hashes.sha1Hash));
                parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                parameters.Add(new SQLiteParameter("@item_id", fileItem.id));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
