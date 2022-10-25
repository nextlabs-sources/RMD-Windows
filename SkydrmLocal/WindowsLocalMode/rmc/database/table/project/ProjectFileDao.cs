using SkydrmLocal.rmc.database2.manager;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database2.table.project
{
    public class ProjectFile
    {
        private int id;
        private int project_table_pk;
        
        private string rms_file_id; // no need current
        private string rms_duid; // 3/29/2019 new added support reShare and modify rights feature.

        private string rms_name;
        private string rms_display_path;
        private string rms_path_id;
        private bool rms_is_folder;
        // 10/15/2018 sdk new added
        private DateTime rms_lastModifiedTime;
        private DateTime rms_creationTime;
        private Int64 rms_fileSize;
        private int rms_OwnerId;
        private string rms_OwnerDisplayName;
        private string rms_OwnerEmail;
        // 10/15/2018 end sdk new added
        private bool is_offline;
        private string local_path;
        private int operation_status;
        // 3/27/2019 new added support edit file&modify rights feature.
        private int edit_status;
        private int modify_rights_status;
        private string reserved1;
        private string reserved2;
        private string reserved3;
        private string reserved4;

        public ProjectFile()
        {

        }

        public int Id { get => id; set => id = value; }
        public int ProjectTablePk { get => project_table_pk; set => project_table_pk = value; }

        public string Rms_file_id { get => rms_file_id; set => rms_file_id = value; }
        public string Rms_duid { get => rms_duid; set => rms_duid = value; }
        public string Rms_name { get => rms_name; set => rms_name = value; }
        public string Rms_display_path { get => rms_display_path; set => rms_display_path = value; }
        public string Rms_path_id { get => rms_path_id; set => rms_path_id = value; }
        public bool Rms_is_folder { get => rms_is_folder; set => rms_is_folder = value; }
        public bool Is_offline { get => is_offline; set => is_offline = value; }
        public string Local_path { get => local_path; set => local_path = value; }
        public int Operation_status { get => operation_status; set => operation_status = value; }
        public DateTime Rms_lastModifiedTime { get => rms_lastModifiedTime; set => rms_lastModifiedTime = value; }
        public DateTime Rms_creationTime { get => rms_creationTime; set => rms_creationTime = value; }
        public long Rms_fileSize { get => rms_fileSize; set => rms_fileSize = value; }
        public int Rms_OwnerId { get => rms_OwnerId; set => rms_OwnerId = value; }
        public string Rms_OwnerDisplayName { get => rms_OwnerDisplayName; set => rms_OwnerDisplayName = value; }
        public string Rms_OwnerEmail { get => rms_OwnerEmail; set => rms_OwnerEmail = value; }

        public int Edit_Status { get => edit_status; set => edit_status = value; }
        public int Modify_Rights_Status { get => modify_rights_status; set => modify_rights_status = value; }

        public string Reserved1 { get => reserved1; set => reserved1 = value; }
        public string Reserved2 { get => reserved2; set => reserved2 = value; }
        public string Reserved3 { get => reserved3; set => reserved3 = value; }
        public string Reserved4 { get => reserved4; set => reserved4 = value; }
        

        public static ProjectFile NewByReader(SQLiteDataReader reader)
        {
            var f = new ProjectFile
            {
                Id = int.Parse(reader["id"].ToString()),
                ProjectTablePk = int.Parse(reader["project_table_pk"].ToString()),
                Rms_file_id = reader["rms_file_id"].ToString(),
                Rms_duid=reader["rms_duid"].ToString(),
                Rms_name = reader["rms_name"].ToString(),
                Rms_display_path = reader["rms_display_path"].ToString(),
                Rms_path_id = reader["rms_path_id"].ToString(),
                // 10/15/2018 sdk new added
                Rms_lastModifiedTime = new DateTime(Int64.Parse(reader["rms_lastModifiedTime"].ToString())),
                Rms_creationTime = new DateTime(Int64.Parse(reader["rms_creationTime"].ToString())),
                Rms_fileSize = Int64.Parse(reader["rms_file_size"].ToString()),
                Rms_OwnerId = int.Parse(reader["rms_owner_id"].ToString()),
                Rms_OwnerDisplayName = reader["rms_owner_display_name"].ToString(),
                Rms_OwnerEmail = reader["rms_owner_email"].ToString(),
                // end 10/15/2018 sdk new added
                Is_offline = int.Parse(reader["is_offline"].ToString()) == 1 ? true : false,
                Local_path = reader["local_path"].ToString(),
                Operation_status = int.Parse(reader["operation_status"].ToString()),

                Edit_Status = int.Parse(reader["edit_status"].ToString()),
                Modify_Rights_Status = int.Parse(reader["modify_rights_status"].ToString()),
                Reserved1 = "",
                Reserved2 = "",
                Reserved3 = "",
                Reserved4 = ""
            };

            f.Rms_is_folder = f.Rms_display_path.EndsWith("/");

            return f;
        }   
    }

    public class ProjectFileDao
    {
        // operation_status will refer to SkydrmLocal.rmc.fileSystem.basemodel.EnumNxlFileStatus
        //      4 means Online;
        public static readonly string SQL_Create_Table_ProjectFile = @"
                CREATE TABLE IF NOT EXISTS ProjectFile(
                   id                        integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   project_table_pk          integer NOT NULL, 
                   rms_file_id               varchar(255) NOT NULL, 
                   rms_duid                  varchar(255) NOT NULL DEFAULT '', 
                   rms_name                  varchar(255) DEFAULT '', 
                   rms_display_path          varchar(255) DEFAULT '', 
                   rms_path_id               varchar(255) DEFAULT '', 
                   rms_lastModifiedTime      integer DEFAULT 0,
                   rms_creationTime          integer DEFAULT 0,
                   rms_file_size             integer DEFAULT 0,
                   rms_owner_id              integer DEFAULT -1,
                   rms_owner_display_name    varchar(255) DEFAULT 'unknown',
                   rms_owner_email           varchar(255) DEFAULT 'unknown@unknown.unknown',
                   is_offline                integer DEFAULT 0, 
                   local_path                varchar(255) DEFAULT '', 
                   operation_status          integer   DEFAULT 4,
                   
                   ----- V2 added -----------
                   edit_status               integer   DEFAULT 0,
                   modify_rights_status      integer   DEFAULT 0,
                   reserved1                 text      DEFAULT '',
                   reserved2                 text      DEFAULT '',
                   reserved3                 text      DEFAULT '',
                   reserved4                 text      DEFAULT '',

                   unique(project_table_pk,rms_file_id)
                   foreign key(project_table_pk) references Project(id) on delete cascade);
        ";

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Edit_Status_V2 = @"
                   ALTER TABLE ProjectFile ADD COLUMN 
                        edit_status             integer     DEFAULT 0;
        ";

        public static readonly string SQL_Alter_Table_ProjectFile_Add_ModifyRights_Status_V2 = @"
                   ALTER TABLE ProjectFile ADD COLUMN 
                        modify_rights_status    integer     DEFAULT 0;
        ";

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Reserved1_V2 = @"
                   ALTER TABLE ProjectFile ADD COLUMN 
                        reserved1               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Reserved2_V2 = @"
                   ALTER TABLE ProjectFile ADD COLUMN 
                        reserved2               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Reserved3_V2 = @"
                   ALTER TABLE ProjectFile ADD COLUMN 
                        reserved3               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Reserved4_V2 = @"
                   ALTER TABLE ProjectFile ADD COLUMN 
                        reserved4               text         DEFAULT '';
        ";

        public static KeyValuePair<String, SQLiteParameter[]> Delete_File_SQL(
            int project_table_pk,
            string rms_file_id)
        {
            string sql = @"
                DELETE FROM 
                    ProjectFile
                WHERE 
                    project_table_pk=@project_table_pk AND 
                    rms_file_id=@rms_file_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@rms_file_id",rms_file_id),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Delete_Folder_And_SubChildren_SQL(
            int project_table_pk,
            string rms_path_id)
        {
            string sql = @"
                DELETE FROM 
                    ProjectFile
                WHERE 
                    project_table_pk=@project_table_pk AND 
                    rms_path_id like @rms_path_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@rms_path_id",rms_path_id+'%'),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
                    int project_table_pk, string file_id, string file_duid,
                    string file_display_path, string file_path_id, string file_nxl_name,
            // sdk new added
            Int64 file_lastModifiedTime,
            Int64 file_creationTime,
            Int64 file_size,
            Int32 file_rms_ownerId,
            string file_ownerDisplayName,
            string file_ownerEmail
                    )
        {
            string sql = @"
                    UPDATE ProjectFile 
                    SET 
                        rms_duid=@rms_duid,
                        rms_name=@rms_name,
                        rms_display_path=@rms_display_path,
                        rms_path_id=@rms_path_id,
                        rms_lastModifiedTime=@file_lastModifiedTime,
                        rms_creationTime=@file_creationTime,
                        rms_file_size=@file_size,
                        rms_owner_id=@file_rms_ownerId,
                        rms_owner_display_name=@file_ownerDisplayName,
                        rms_owner_email=@file_ownerEmail
                    WHERE
                        project_table_pk = @project_table_pk AND rms_file_id=@rms_file_id;

                   ---------if no updated happeded, then insert one--------------------------
                    INSERT INTO  
                            ProjectFile(project_table_pk,rms_file_id,
                                        rms_duid,rms_name,rms_display_path,rms_path_id,
                                        rms_lastModifiedTime,rms_creationTime,rms_file_size,
                                        rms_owner_id,rms_owner_display_name,rms_owner_email
                                        )
                    SELECT 
                            @project_table_pk,
                            @rms_file_id,
                            @rms_duid,
                            @rms_name,
                            @rms_display_path,
                            @rms_path_id,
                            @file_lastModifiedTime,
                            @file_creationTime,
                            @file_size,
                            @file_rms_ownerId,
                            @file_ownerDisplayName,
                            @file_ownerEmail
                    WHERE
                        ( SELECT changes() = 0 );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@rms_file_id",file_id),
                new SQLiteParameter("@rms_duid",file_duid),
                new SQLiteParameter("@rms_name",file_nxl_name),
                new SQLiteParameter("@rms_display_path",file_display_path),
                new SQLiteParameter("@rms_path_id",file_path_id),
                // sdk new added
                new SQLiteParameter("@file_lastModifiedTime",file_lastModifiedTime),
                new SQLiteParameter("@file_creationTime",file_creationTime),
                new SQLiteParameter("@file_size",file_size),
                new SQLiteParameter("@file_rms_ownerId",file_rms_ownerId),
                new SQLiteParameter("@file_ownerDisplayName",file_ownerDisplayName),
                new SQLiteParameter("@file_ownerEmail",file_ownerEmail),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> InsertFakedRoot_SQL(
            int project_table_pk)
        {
            string sql = @"
                INSERT OR IGNORE INTO 
                    ProjectFile(project_table_pk,rms_file_id,rms_display_path,rms_path_id)
                    VALUES(@project_table_pk,'00000000-0000-0000-0000-000000000000','/','/');
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
            int project_table_pk, string path)
        {
            string sql = @"
              SELECT   
                *
            FROM
                ProjectFile
            WHERE
                 project_table_pk=@project_table_pk AND rms_display_path like @path
            ORDER BY 
                rms_path_id ASC;                    
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@path",path+'%')
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
            int table_pk,
            int newStatus
            )
        {
            string sql = @"
                UPDATE 
                    ProjectFile
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


        public static KeyValuePair<String, SQLiteParameter[]> Update_IsOffline_SQL(
            int table_pk, bool newMark)
        {
            string sql = @"
                UPDATE 
                    ProjectFile
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
                    ProjectFile
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
            int project_table_pk,
            int this_table_rownumber)
        {
            string sql = @"
                SELECT 
                    rms_display_path
                FROM
                    ProjectFile
                WHERE
                    project_table_pk=@project_table_pk AND
                    id =@this_table_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@this_table_id",this_table_rownumber)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_RowNumberId_SQL(
            int project_table_pk,
            string rms_path_id)
        {
            string sql = @"
                SELECT 
                    id
                FROM
                    ProjectFile
                WHERE
                    project_table_pk=@project_table_pk AND
                    rms_path_id =@rms_path_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@rms_path_id",rms_path_id)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Update_LastModifiedTime_SQL(
            int table_pk,
            DateTime lastModifiedTime)
        {
            string sql = @"
                UPDATE 
                    ProjectFile
                SET
                    rms_lastModifiedTime=@lastModifiedTime
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@lastModifiedTime",lastModifiedTime),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Update_FileSize_SQL(
            int table_pk,
            long file_size)
        {
            string sql = @"
                UPDATE 
                    ProjectFile
                SET
                    rms_file_size=@file_size 
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@file_size",file_size),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Update_EditStatus_SQL(int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    ProjectFile
                SET
                    edit_status=@newStatus
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@newStatus",newStatus)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Update_ModifyRights_Status_SQL(int table_pk, int newStatus)
        {
            string sql = @"
                UPDATE 
                    ProjectFile
                SET
                    modify_rights_status=@newStatus
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
                new SQLiteParameter("@newStatus",newStatus)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }
    }
}
