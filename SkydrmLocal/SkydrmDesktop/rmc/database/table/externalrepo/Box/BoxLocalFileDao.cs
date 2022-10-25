using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.Box
{
    class BoxLocalFileDao
    {
        public static readonly string SQL_Create_Table_BoxLocalFile = @"
            CREATE TABLE IF NOT EXISTS BoxLocalFile(
                   id                                 integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   rms_external_repo_table_pk         integer NOT NULL, 

                   ---- which rms folder will hold this file ---- 
                   external_drive_file_table_pk           integer NOT NULL, 

                   nxl_name                           varchar(255) NOT NULL DEFAULT '',
                   nxl_local_path                     varchar(255) NOT NULL DEFAULT '',   
                   last_modified_time                 datetime NOT NULL default (datetime('now','localtime')),
                   file_size                          integer   NOT NULL DEFAULT 0,
                   operation_status                   integer   NOT NULL DEFAULT 3,

                   ---- For uploading native file, maybe also support it in offline model ---- 
                   is_nxl_file                        integer   NOT NULL DEFAULT 1, 

                   ----- reserved ---- 
                    reserved1                   text                DEFAULT '',
                    reserved2                   text                DEFAULT '',
                    reserved3                   text                DEFAULT '',
                    reserved4                   text                DEFAULT '',
                   
                   unique(rms_external_repo_table_pk,external_drive_file_table_pk,nxl_name),
                   foreign key(rms_external_repo_table_pk) references RmsExternalRepo(id) on delete cascade,
                   foreign key(external_drive_file_table_pk) references BoxFile(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
           int rms_external_repo_table_pk)
        {
            string sql = @"
                SELECT  *
                FROM BoxLocalFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
            int rms_external_repo_table_pk, string folder_cloudPathId, string name,
            string path, int size, DateTime lastModified)
        {
            // Convert
            var cstime = lastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"
                INSERT INTO 
                    BoxLocalFile(rms_external_repo_table_pk,
                                             external_drive_file_table_pk,
                                             nxl_name,
                                             nxl_local_path,
                                             last_modified_time,
                                             file_size)
                    Values(@rms_external_repo_table_pk,
                           ( SELECT 
                                    BoxFile.id 
                                FROM 
                                    BoxFile 
                                WHERE 
                                    BoxFile.rms_external_repo_table_pk=@rms_external_repo_table_pk  
                                AND  
                                    BoxFile.ser_cloud_pathid=@folder_cloudPathId
                            ),
                            @name,
                            @path,
                            @timestr,
                            @size
                            );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@folder_cloudPathId",folder_cloudPathId),
                new SQLiteParameter("@name",name),
                new SQLiteParameter("@path",path),
                new SQLiteParameter("@timestr",cstime),
                new SQLiteParameter("@size",size),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL_Under_TargetFolder(
            int rms_external_repo_table_pk, string folderInRemoteRepo)
        {
            string sql = @"
                SELECT  *
                FROM BoxLocalFile
                WHERE
                    rms_external_repo_table_pk=@rms_external_repo_table_pk and
                    external_drive_file_table_pk=(
                                            select 
                                                BoxFile.id 
                                            from 
                                                BoxFile 
                                            where 
                                                BoxFile.rms_external_repo_table_pk=@rms_external_repo_table_pk AND 
                                                BoxFile.ser_cloud_pathid=@folderInRemoteRepo
                                            );
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@rms_external_repo_table_pk",rms_external_repo_table_pk),
                new SQLiteParameter("@folderInRemoteRepo",folderInRemoteRepo),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Delete_Item_SQL(int table_pk)
        {
            string sql = @"
                DELETE FROM
                    BoxLocalFile
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
