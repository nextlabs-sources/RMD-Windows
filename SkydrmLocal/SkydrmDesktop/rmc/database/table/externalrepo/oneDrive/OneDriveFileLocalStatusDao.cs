using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.oneDrive
{
    public class OneDriveFileLocalStatusDao
    {
        public static readonly string SQL_Create_Table_OneDriveFileLocalStatusDao = @"
                CREATE TABLE IF NOT EXISTS OneDriveFileLocalStatus(
                                id                             integer                 NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                                rms_external_repo_table_pk     integer                 default 0,
                                file_common_table_pk           integer                 default 0,
                                item_id                    varchar(255)                NOT NULL default '',

                                        ------ local added -----
                                is_offline                  integer                    NOT NULL default 0,
                                is_favorite                 integer                    NOT NULL default 0,
                                local_path                  varchar(255)               NOT NULL default '',
                                is_nxl_file                 integer                    NOT NULL default 0,
                                status                      integer                    NOT NULL default 4,
                                custom_string               varchar(255)               NOT NULL default '',
                                edit_status                 integer                    NOT NULL default 0,
                                modify_rights_status        integer                    NOT NULL default 0,

                                         ----- reserved ---- 
                                reserved1                   text                       DEFAULT '',
                                reserved2                   text                       DEFAULT '',
                                reserved3                   text                       DEFAULT '',
                                reserved4                   text                       DEFAULT '',
                                UNIQUE(rms_external_repo_table_pk, item_id),
                                foreign key(file_common_table_pk) references OneDriveFileCommon(id) on delete cascade);
                            ";

        public static KeyValuePair<String, SQLiteParameter[]> Insert_SQL(int rms_external_repo_table_pk, string file_common_table_pk, FileItem fileItem)
        {

            int is_offline = 0;
            int is_favorite = 0;
            string local_path = string.Empty;
            int is_nxl_file = 0;
            int status = 4;
            string custom_string = string.Empty;
            int edit_status = 0;
            int modify_rights_status = 0;

            string sql = @"INSERT INTO 
                            OneDriveFileLocalStatus(rms_external_repo_table_pk,
                                                    file_common_table_pk,
                                                    item_id,
                                                    is_offline,
                                                    is_favorite,
                                                    local_path,
                                                    is_nxl_file,
                                                    status,
                                                    custom_string,
                                                    edit_status,
                                                    modify_rights_status
                                                    )
                             VALUES(@rms_external_repo_table_pk,
                                    @file_common_table_pk,
                                    @item_id,
                                    @is_offline,
                                    @is_favorite,
                                    @local_path,
                                    @is_nxl_file,
                                    @status,
                                    @custom_string,
                                    @edit_status,
                                    @modify_rights_status
                                    );
                  ";
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                is_nxl_file = string.Equals(Path.GetExtension(fileItem.name), ".nxl", StringComparison.CurrentCultureIgnoreCase) ? 1 : 0;
                parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                parameters.Add(new SQLiteParameter("@file_common_table_pk", file_common_table_pk));
                parameters.Add(new SQLiteParameter("@item_id", fileItem.id));
                parameters.Add(new SQLiteParameter("@is_offline", is_offline));
                parameters.Add(new SQLiteParameter("@is_favorite", is_favorite));
                parameters.Add(new SQLiteParameter("@local_path", local_path));
                parameters.Add(new SQLiteParameter("@is_nxl_file", is_nxl_file));
                parameters.Add(new SQLiteParameter("@status", status));
                parameters.Add(new SQLiteParameter("@custom_string", edit_status));
                parameters.Add(new SQLiteParameter("@edit_status", edit_status));
                parameters.Add(new SQLiteParameter("@modify_rights_status", modify_rights_status));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Status_SQL(int rms_external_repo_table_pk, string item_id, int newStatus)
        {
             string sql = @"UPDATE 
                                  OneDriveFileLocalStatus
                            SET 
                                  status=@status
                            WHERE
                                  rms_external_repo_table_pk =@rms_external_repo_table_pk AND item_id=@item_id;
                  ";
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                parameters.Add(new SQLiteParameter("@status", newStatus));
                parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                parameters.Add(new SQLiteParameter("@item_id", item_id));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Local_Path_SQL(int rms_external_repo_table_pk, string item_id, string local_path)
        {
            string sql = @"UPDATE 
                                  OneDriveFileLocalStatus
                            SET 
                                  local_path=@local_path
                            WHERE
                                  rms_external_repo_table_pk =@rms_external_repo_table_pk AND item_id=@item_id;
                  ";
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                parameters.Add(new SQLiteParameter("@local_path", local_path));
                parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                parameters.Add(new SQLiteParameter("@item_id", item_id));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Status_SQL(int table_pk, int newStatus)
        {
            string sql = @"UPDATE 
                                  OneDriveFileLocalStatus
                            SET 
                                  status=@status
                            WHERE
                                  id =@table_pk;
                  ";
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                parameters.Add(new SQLiteParameter("@status", newStatus));
                parameters.Add(new SQLiteParameter("@table_pk", table_pk));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Local_Is_Offline_SQL(int rms_external_repo_table_pk, string item_id, bool is_offline)
        {
            string sql = @"UPDATE 
                                  OneDriveFileLocalStatus
                            SET 
                                  is_offline=@is_offline
                            WHERE
                                  rms_external_repo_table_pk =@rms_external_repo_table_pk AND item_id=@item_id;
                  ";
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            try
            {
                parameters.Add(new SQLiteParameter("@is_offline", is_offline?1:0));
                parameters.Add(new SQLiteParameter("@rms_external_repo_table_pk", rms_external_repo_table_pk));
                parameters.Add(new SQLiteParameter("@item_id", item_id));
                return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
