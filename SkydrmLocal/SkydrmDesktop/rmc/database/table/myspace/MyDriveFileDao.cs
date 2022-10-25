using SkydrmLocal.rmc.common.helper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.myspace
{
    public class MyDriveFile
    {
        public int Id { get; set; }
        // foreign key, table owner
        public int User_table_pk { get; set; }

        public string RmsPathDisplay { get; set; }
        public string RmsPathId { get; set; }
        public string RmsNxlName { get; set; }
        public DateTime RmsLastModified { get; set; }
        public int RmsSize { get; set; }
        public bool RmsIsFolder { get; set; }

        // all local fields
        public string LocalPath { get; set; }
        public bool IsOffline { get; set; }
        public DateTime OfflineTime { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime FavoriteTime { get; set; }
        public int Status { get; set; }

        // reserved
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }

        public static MyDriveFile NewByReader(SQLiteDataReader reader)
        {
            MyDriveFile item = new MyDriveFile();
            {
                item.Id = int.Parse(reader["id"].ToString());
                item.User_table_pk = int.Parse(reader["user_table_pk"].ToString());
                // remote
                item.RmsPathId = reader["rms_path_id"].ToString();
                item.RmsPathDisplay = reader["rms_display_path"].ToString();
                item.RmsNxlName = reader["rms_name"].ToString();
                item.RmsLastModified = DateTime.Parse(reader["rms_last_modified_time"].ToString()).ToUniversalTime(); // like myVault
                item.RmsSize = int.Parse(reader["rms_size"].ToString());
                item.RmsIsFolder = int.Parse(reader["rms_is_folder"].ToString()) == 1;
                // local
                item.LocalPath = reader["local_path"].ToString();
                item.IsOffline = int.Parse(reader["is_offline"].ToString()) == 1;
                item.OfflineTime = DateTime.Parse(reader["offline_time"].ToString()).ToUniversalTime();
                item.IsFavorite = int.Parse(reader["is_favorite"].ToString()) == 1;
                item.FavoriteTime = DateTime.Parse(reader["favorite_time"].ToString()).ToUniversalTime();
                item.Status = int.Parse(reader["operation_status"].ToString());
                // reserved
                item.Reserved1 = "";
                item.Reserved2 = "";
                item.Reserved3 = "";
                item.Reserved4 = "";
            }

            return item;
        }
    }

    public class MyDriveFileDao
    {
        public static readonly string SQL_Create_Table_MyDriveFile = @"
            CREATE TABLE IF NOT EXISTS MyDriveFile (
                id                          integer             NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                user_table_pk               integer             default 0, 
                
                ------ rms returned -----
                rms_path_id                 varchar(255)        NOT NULL default '',
                rms_display_path            varchar(255)        NOT NULL default '',   
                rms_repo_id                 varchar(255)        NOT NULL default '',
                rms_name                    varchar(255)        NOT NULL default '',
                rms_last_modified_time      datetime            NOT NULL default (datetime('now','localtime')),
                rms_is_folder               integer             NOT NULL default 0,
                rms_size                    integer             NOT NULL default 0,
                
                ----- local added--------
                local_path                  varchar(255)        NOT NULL default '',
                is_offline                  integer             NOT NULL default 0,
                offline_time                DateTime            NOT NULL default (datetime('now','localtime')), 
                is_favorite                 integer             NOT NULL default 0,
                favorite_time               DateTime            NOT NULL default (datetime('now','localtime')),
                operation_status            integer             NOT NULL DEFAULT 4,
                
                ----- reserved -----------
                reserved1                   text                DEFAULT '',
                reserved2                   text                DEFAULT '',
                reserved3                   text                DEFAULT '',
                reserved4                   text                DEFAULT '',

                UNIQUE(user_table_pk,rms_path_id),
                foreign key(user_table_pk) references User(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> UPSERT_SQL(
            int user_tb_pk,
            string pathId, string pathDisplay, string name, string repoId,
            Int64 lastModified, Int64 size,
            Int32 isFolder)
        {
            // Need convert, like myVault. (or like workspace file table stored way.)
            var csTime = JavaTimeConverter.ToCSDateTime(lastModified);
            string convertedLastModified = csTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"     
               UPDATE 
                    MyDriveFile
                SET
                    rms_last_modified_time=@last_modified_time,
                    rms_size=@size,
                    rms_name=@name,
                    rms_display_path=@path_display
                WHERE
                    user_table_pk=@user_table_pk AND rms_path_id=@path_id;
                
                ---------if no updated happeded, then insert one--------------------------
                INSERT INTO
                    MyDriveFile(user_table_pk,
                                rms_path_id,
                                rms_display_path,
                                rms_repo_id,
                                rms_name,
                                rms_last_modified_time,
                                rms_size,                               
                                rms_is_folder)
                SELECT
                    @user_table_pk,
                    @path_id,
                    @path_display,
                    @repo_id,
                    @name,
                    @last_modified_time,
                    @size,
                    @is_folder
                    
                WHERE
                       ( SELECT changes() = 0 );
            ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_tb_pk),
                new SQLiteParameter("@path_id",pathId),
                new SQLiteParameter("@path_display",pathDisplay),
                new SQLiteParameter("@repo_id",repoId),
                new SQLiteParameter("@name",name),

                new SQLiteParameter("@last_modified_time",convertedLastModified),
                new SQLiteParameter("@size",size),
                new SQLiteParameter("@is_folder",isFolder)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> InsertFakedRoot_SQL(
             int user_tb_pk)
        {
            string sql = @"
                INSERT OR IGNORE INTO 
                    MyDriveFile(user_table_pk, rms_path_id, rms_display_path)
                    VALUES(@user_table_pk, '/', '/');
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_tb_pk),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk, string folderPath)
        {
            string sql = @"SELECT * FROM
                                MyDriveFile 
                           WHERE
                                user_table_pk = @user_table_pk AND rms_path_id like @path
                           ORDER BY 
                                rms_path_id ASC";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_tb_pk),
                new SQLiteParameter("@path", folderPath+'%')
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        /// <summary>
        /// Query the pathId as the parentFolder when upload myDrive local file.
        /// </summary>
        /// <param name="user_tb_pk"></param>
        /// <param name="this_table_pk"></param>
        /// <returns></returns>
        public static KeyValuePair<String, SQLiteParameter[]> Query_DisplayPath_SQL(int user_tb_pk, int this_table_pk)
        {
            string sql = @"
                SELECT 
                    rms_display_path
                FROM
                    MyDriveFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_File_Status_SQL(
            int table_pk, int status)
        {
            string sql = @"UPDATE MyDriveFile
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

        public static KeyValuePair<String, SQLiteParameter[]> Update_Offline_Mark_SQL(
          int table_pk, bool is_offline)
        {
            string sql = @"UPDATE MyDriveFile
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
                                MyDriveFile
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
          int user_table_pk, string rms_path_id)
        {
            string sql = @"DELETE FROM 
                                MyDriveFile
                           WHERE
                                user_table_pk=@user_tb_pk AND rms_path_id=@rms_path_id;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_tb_pk",user_table_pk),
                new SQLiteParameter("@rms_path_id",rms_path_id)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Delete specified folder and its all children nodes.
        public static KeyValuePair<String, SQLiteParameter[]> Delete_Folder_And_SubChildren_SQL(
            int user_table_pk, string rms_path_id)
        {
            string sql = @"DELETE FROM 
                                MyDriveFile
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
