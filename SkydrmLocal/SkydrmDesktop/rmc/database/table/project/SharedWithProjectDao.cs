using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.project
{
    public class SharedWithProjectFile
    {
        public int Id { get; set; }
        public int ProjectTablePK { get; set; }

        // rms returned fields
        public string RmsDuid { get; set; }
        public string RmsName { get; set; }
        public string RmsFileType { get; set; }
        public long RmsFileSize { get; set; }
        public DateTime RmsSharedDate { get; set; }
        public string RmsSharedBy { get; set; } 
        public string RmsTransactionId { get; set; }
        public string RmsTransactionCode { get; set; }
        public string RmsSharedUrl { get; set; }
        public string RmsRightsJson { get; set; }
        public bool RmsIsOwner { get; set; }
        public int RmsProtectionType { get; set; }
        public string RmsSharedByProject { get; set; }

        // local 
        public bool IsOffline { get; set; } 
        public string LocalPath { get; set; }
        public int OperationStatus { get; set; }

        public static SharedWithProjectFile NewByReader(SQLiteDataReader reader)
        {
            var f = new SharedWithProjectFile
            {
                Id = int.Parse(reader["id"].ToString()),
                ProjectTablePK = int.Parse(reader["project_table_pk"].ToString()),
                RmsDuid = reader["rms_duid"].ToString(),
                RmsName = reader["rms_name"].ToString(),
                RmsFileType = reader["rms_file_type"].ToString(),
                RmsFileSize = long.Parse(reader["rms_file_size"].ToString()),
                RmsSharedDate = new DateTime(long.Parse(reader["rms_shared_date"].ToString())),
                RmsSharedBy = reader["rms_shared_by"].ToString(),
                RmsTransactionId = reader["rms_transaction_id"].ToString(),
                RmsTransactionCode = reader["rms_transaction_code"].ToString(),
                RmsSharedUrl = reader["rms_shared_link_url"].ToString(),
                RmsRightsJson = reader["rms_rights_json"].ToString(),
                RmsIsOwner = int.Parse(reader["rms_is_owner"].ToString()) == 1 ? true : false,
                RmsProtectionType = int.Parse(reader["rms_protection_type"].ToString()),
                RmsSharedByProject = reader["rms_shared_by_project"].ToString(),
                //  local
                IsOffline = int.Parse(reader["is_offline"].ToString()) == 1 ? true : false,
                LocalPath = reader["local_path"].ToString(),
                OperationStatus = int.Parse(reader["operation_status"].ToString())
            };

            return f;
        }
    }

    public class SharedWithProjectDao
    {
        // operation_status will refer to SkydrmLocal.rmc.fileSystem.basemodel.EnumNxlFileStatus
        // 4 means Online;
        public static readonly string SQL_Create_Table_SharedWithProjectFile = @"
                CREATE TABLE IF NOT EXISTS SharedWithProjectFile(
                   id                        integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   project_table_pk          integer NOT NULL, 

                   -- rms returned (13)--
                   rms_duid                  varchar(255) NOT NULL DEFAULT '', 
                   rms_name                  varchar(255) DEFAULT '', 
                   rms_file_type             varchar(255) DEFAULT '',
                   rms_file_size             integer DEFAULT 0,
                   rms_shared_date           integer DEFAULT 0,
                   rms_shared_by             varchar(255) DEFAULT '',
                   rms_transaction_id        varchar(255) DEFAULT '',
                   rms_transaction_code      varchar(255) DEFAULT '',
                   rms_shared_link_url       varchar(255) DEFAULT '',
                   rms_rights_json           varchar(255) DEFAULT '',
                   rms_is_owner              integer DEFAULT 0,
                   rms_protection_type       integer DEFAULT -1,
                   rms_shared_by_project     varchar(255) DEFAULT '',

                   -- local feature --
                   is_offline                integer DEFAULT 0, 
                   local_path                varchar(255) DEFAULT '', 
                   operation_status          integer   DEFAULT 4,
                   
                   -- table restrictions --
                   unique(project_table_pk,rms_duid)
                   foreign key(project_table_pk) references Project(id) on delete cascade);
        ";

        public static KeyValuePair<string, SQLiteParameter[]> Upsert_SQL(
            int project_table_pk, string duid, string name,
            string type, Int64 size, Int64 shared_date, string shared_by,
            string transaction_id, string strnsaction_code, string shared_link_url,
            string rights_json, bool is_owner, int protection_type, string shared_by_project)
        {
            string sql = @"
                UPDATE SharedWithProjectFile 
                SET 
                    project_table_pk=@project_table_pk,
                    rms_duid=@duid,
                    rms_name=@name,
                    rms_file_type=@type,
                    rms_file_size=@size,
                    rms_shared_date=@shared_date,
                    rms_shared_by=@shared_by,
                    rms_transaction_id=@transcation_id,
                    rms_transaction_code=@transcation_code,
                    rms_shared_link_url=@shared_link_url,
                    rms_rights_json=@rights_json,
                    rms_is_owner=@is_owner,
                    rms_protection_type=@protection_type,
                    rms_shared_by_project=@shared_by_project
                WHERE
                    project_table_pk = @project_table_pk AND rms_duid=@duid;
                ---------if no updated happeded, then insert one--------------------------
                    INSERT INTO  
                            SharedWithProjectFile(
                                        project_table_pk,
                                        rms_duid,
                                        rms_name,
                                        rms_file_type,
                                        rms_file_size,
                                        rms_shared_date,
                                        rms_shared_by,
                                        rms_transaction_id,
                                        rms_transaction_code,
                                        rms_shared_link_url,
                                        rms_rights_json,
                                        rms_is_owner,
                                        rms_protection_type,
                                        rms_shared_by_project
                                        )
                    SELECT 
                            @project_table_pk,
                            @duid,
                            @name,
                            @type,
                            @size,
                            @shared_date,
                            @shared_by,
                            @transcation_id,
                            @transcation_code,
                            @shared_link_url,
                            @rights_json,
                            @is_owner,
                            @protection_type,
                            @shared_by_project
                    WHERE
                        ( SELECT changes() = 0 );
                
            ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@duid",duid),
                new SQLiteParameter("@name",name),
                new SQLiteParameter("@type",type),
                new SQLiteParameter("@size",size),
                new SQLiteParameter("@shared_date",shared_date),
                new SQLiteParameter("@shared_by",shared_by),
                new SQLiteParameter("@transcation_id",transaction_id),
                new SQLiteParameter("@transcation_code",strnsaction_code),
                new SQLiteParameter("@shared_link_url",shared_link_url),
                new SQLiteParameter("@rights_json",rights_json),
                new SQLiteParameter("@is_owner",is_owner?1:0),
                new SQLiteParameter("@protection_type",protection_type),
                new SQLiteParameter("@shared_by_project",shared_by_project),
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Query_SQL(int project_table_pk)
        {
            string sql = @"
               SELECT *
               FROM  SharedWithProjectFile
               WHERE project_table_pk=@project_table_pk
            ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_IsOffline_SQL(
            int sharedWithProjectFile_table_pk,
            bool isOffline)
        {
            string sql = @"
                UPDATE 
                    SharedWithProjectFile
                SET
                    is_offline=@isOffline
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",sharedWithProjectFile_table_pk),
                new SQLiteParameter("@isOffline",isOffline?1:0)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_LocalPath_SQL(
             int sharedWithProjectFile_table_pk, string newPath)
        {
            string sql = @"
                UPDATE 
                    SharedWithProjectFile
                SET
                    local_path=@newPath
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",sharedWithProjectFile_table_pk),
                new SQLiteParameter("@newPath",newPath),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
            int sharedWithProjectFile_table_pk,
            int newStatus)
        {
            string sql = @"
                UPDATE 
                    SharedWithProjectFile
                SET
                    operation_status=@newStatus
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",sharedWithProjectFile_table_pk),
                new SQLiteParameter("@newStatus",newStatus)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(
           string duid)
        {
            string sql = @"DELETE FROM 
                                SharedWithProjectFile
                           WHERE
                                rms_duid=@duid;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@duid",duid)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

    }
}
