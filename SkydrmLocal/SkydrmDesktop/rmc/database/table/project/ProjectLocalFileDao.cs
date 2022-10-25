using SkydrmLocal.rmc.database2.manager;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database2.table.project
{
    public class ProjectLocalFile
    {
        private int id;
        private int project_table_pk;
        private int projectfile_table_pk;
        private string nxl_name;
        private string nxl_local_path;
        private DateTime last_modified_time;
        private long file_size;
        private int operation_status;

        // reserved
        private string reserved1;
        private string reserved2;
        private string reserved3;
        private string reserved4;
        private string reserved5;

        public ProjectLocalFile()
        {

        }
        public int Id { get => id; set => id = value; }
        public int ProjectTablePk { get => project_table_pk; set => project_table_pk = value; }
        public int ProjectFileTablePk { get => projectfile_table_pk; set => projectfile_table_pk = value; }
        public string Name { get => nxl_name; set => nxl_name = value; }
        public DateTime Last_modified_time { get => last_modified_time; set => last_modified_time = value; }
        public long Size { get => file_size; set => file_size = value; }
        public int Operation_status { get => operation_status; set => operation_status = value; }
        public string Path { get => nxl_local_path; set => nxl_local_path = value; }

        // Reserved
        /// <summary>
        /// now Reserved1 use for save file already exist in server 
        /// </summary>
        public string Reserved1 { get => reserved1; set => reserved1 = value; }
        public string Reserved2 { get => reserved2; set => reserved2 = value; }
        public string Reserved3 { get => reserved3; set => reserved3 = value; }
        public string Reserved4 { get => reserved4; set => reserved4 = value; }
        public string Reserved5 { get => reserved5; set => reserved5 = value; }

        public static ProjectLocalFile NewByReader(SQLiteDataReader reader)
        {
            ProjectLocalFile p = new ProjectLocalFile()
            {
                Id = int.Parse(reader["id"].ToString()),
                ProjectTablePk = int.Parse(reader["project_table_pk"].ToString()),
                ProjectFileTablePk = int.Parse(reader["projectfile_table_pk"].ToString()),
                Name = reader["nxl_name"].ToString(),
                Path = reader["nxl_local_path"].ToString(),
                Last_modified_time = DateTime.Parse(reader["last_modified_time"].ToString()).ToUniversalTime(),
                Size = long.Parse(reader["file_size"].ToString()),
                Operation_status = int.Parse(reader["operation_status"].ToString()),
                Reserved1 = reader["reserved1"].ToString(),
                Reserved2 = "",
                Reserved3 = "",
                Reserved4 = "",
                Reserved5 = ""
            };
            return p;
        }
    }
    public class ProjectLocalFileDao
    {
        public static readonly string SQL_Create_Table_ProjectLocalFile = @"
            CREATE TABLE IF NOT EXISTS ProjectLocalFile(
                   id                       integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   project_table_pk         integer NOT NULL, 
                   ---- which rms folder will hold this file ---- 
                   projectfile_table_pk     integer NOT NULL, 
                   nxl_name                 varchar(255) NOT NULL DEFAULT '',
                   nxl_local_path           varchar(255) NOT NULL DEFAULT '',   
                   last_modified_time       datetime NOT NULL default (datetime('now','localtime')),
                   file_size                integer   NOT NULL DEFAULT 0,
                   operation_status         integer   NOT NULL DEFAULT 3,

                   ----- V4 added -----------
                   reserved1                text             DEFAULT '',
                   reserved2                text             DEFAULT '',
                   reserved3                text             DEFAULT '',
                   reserved4                text             DEFAULT '',
                   reserved5                text             DEFAULT '',
                   
                   unique(project_table_pk,projectfile_table_pk,nxl_name),
                   foreign key(project_table_pk) references Project(Id) on delete cascade,
                   foreign key(projectfile_table_pk) references ProjectFile(Id) on delete cascade);
        ";


        // Extend db table reserved field (version 4).
        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved1_V4 = @"
                   ALTER TABLE ProjectLocalFile ADD COLUMN 
                        reserved1               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved2_V4 = @"
                   ALTER TABLE ProjectLocalFile ADD COLUMN 
                        reserved2               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved3_V4 = @"
                   ALTER TABLE ProjectLocalFile ADD COLUMN 
                        reserved3               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved4_V4 = @"
                   ALTER TABLE ProjectLocalFile ADD COLUMN 
                        reserved4               text         DEFAULT '';
        ";

        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved5_V4 = @"
                   ALTER TABLE ProjectLocalFile ADD COLUMN 
                        reserved5               text         DEFAULT '';
        ";




        /// <summary>
        ///     Find local files in Project Folder
        /// </summary>
        /// <param name="project_table_pk"> primary key of project </param>
        /// <param name="folderInPorject">folder string in project file</param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
            int project_table_pk, string folderInPorject)
        {
            string sql = @"
                SELECT  *
                FROM ProjectLocalFile
                WHERE
                    project_table_pk=@project_table_pk and
                    projectfile_table_pk=(
                                            select 
                                                ProjectFile.id 
                                            from 
                                                ProjectFile 
                                            where 
                                                ProjectFile.project_table_pk=@project_table_pk AND 
                                                ProjectFile.rms_path_id=@folderInPorject
                                            );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@folderInPorject",folderInPorject),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
            int project_table_pk)
        {
            string sql = @"
                SELECT  *
                FROM ProjectLocalFile
                WHERE
                    project_table_pk=@project_table_pk ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
            int project_table_pk, string folderInPorject, string name,
            string path,int size, DateTime lastModified, string reserved1)
        {
            //
            var cstime = lastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = @"
                UPDATE 
                    ProjectLocalFile
                SET
                    last_modified_time=@timestr,
                    file_size=@size,
                    reserved1=@reserved1
                WHERE
                    nxl_name=@name AND project_table_pk=@project_table_pk
                    AND projectfile_table_pk=
                    ( SELECT 
                                    ProjectFile.id 
                                FROM 
                                    ProjectFile 
                                WHERE 
                                    ProjectFile.project_table_pk=@project_table_pk  
                                AND  
                                    ProjectFile.rms_path_id=@folderInPorject);
                
                ---------if no updated happeded, then insert one--------------------------
                INSERT INTO 
                    ProjectLocalFile(project_table_pk,projectfile_table_pk,
                                     nxl_name,nxl_local_path,last_modified_time,file_size,reserved1)
                SELECT
                    @project_table_pk,
                    ( SELECT 
                             ProjectFile.id 
                         FROM 
                             ProjectFile 
                         WHERE 
                             ProjectFile.project_table_pk=@project_table_pk  
                         AND  
                             ProjectFile.rms_path_id=@folderInPorject
                     ),
                    @name,
                    @path,
                    @timestr,
                    @size,
                    @reserved1
                 WHERE
                       ( SELECT changes() = 0 );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@folderInPorject",folderInPorject),
                new SQLiteParameter("@name",name),
                new SQLiteParameter("@path",path),
                new SQLiteParameter("@timestr",cstime),
                new SQLiteParameter("@size",size),
                new SQLiteParameter("@reserved1",reserved1),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
            int project_table_pk,
            string FolderId, 
            string name, 
            int newStatus
            )
        {
            // for new name has timestamp, so its unique, we can ignore FolderID

            string sql = @"
                UPDATE 
                    ProjectLocalFile
                SET
                    operation_status=@newStatus
                WHERE
                    project_table_pk=@project_table_pk AND nxl_name=@name;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@name",name),
                new SQLiteParameter("@newStatus",newStatus)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_FileStatus_SQL(
            int table_pk,int newStatus)
        {
            // for new name has timestamp, so its unique, we can ignore FolderID

            string sql = @"
                UPDATE 
                    ProjectLocalFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Name_SQL(
      int tb_pk, string name)
        {
            string sql = @"UPDATE 
                                ProjectLocalFile
                           SET 
                                nxl_name=@name
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk),
                new SQLiteParameter("@name",name)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_LocalPath_SQL(
            int tb_pk, string localPath)
        {
            string sql = @"UPDATE 
                                ProjectLocalFile
                           SET 
                                nxl_local_path=@path
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk),
                new SQLiteParameter("@path",localPath)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Reserved1_SQL(
          int tb_pk, string reserved1)
        {
            string sql = @"UPDATE 
                                ProjectLocalFile
                           SET 
                                reserved1=@reserved1
                           WHERE
                                id=@tb_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@tb_pk",tb_pk),
                new SQLiteParameter("@reserved1", reserved1)
            };

            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL
        (
            int project_table_pk,
            string FolderId,
            string name
        )
        {
            string sql = @"
                DELETE FROM
                    ProjectLocalFile
                WHERE
                    project_table_pk=@project_table_pk AND nxl_name=@name;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@project_table_pk",project_table_pk),
                new SQLiteParameter("@name",name),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(int table_pk)
        {
            string sql = @"
                DELETE FROM
                    ProjectLocalFile
                WHERE
                    id=@table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }



    }
}
