using SkydrmLocal.rmc.database2.manager;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database2.table.project
{
    public class Project
    {
        private int id;
        private int user_table_pk;

        private int rms_project_id;
        private string rms_name;
        private string rms_display_name;
        private string rms_description;
        private bool rms_is_owner;
        //private string rms_pending_setting; // no need current
        //private string rms_owner_raw_json // no need current


        private string rms_tenant_id;
        private string rms_classifcation_json;
        private bool rms_is_enable_adhoc;

        public Project()
        {

        }
        public int Id { get => id; set => id = value; }
        public int User_table_pk { get => user_table_pk; set => user_table_pk = value; }
        public int Rms_project_id { get => rms_project_id; set => rms_project_id = value; }
        public string Rms_name { get => rms_name; set => rms_name = value; }
        public string Rms_display_name { get => rms_display_name; set => rms_display_name = value; }
        public string Rms_description { get => rms_description; set => rms_description = value; }
        public bool Rms_is_owner { get => rms_is_owner; set => rms_is_owner = value; }
        public string Rms_tenant_id { get => rms_tenant_id; set => rms_tenant_id = value; }
        public string Rms_classifcation_json { get => rms_classifcation_json; set => rms_classifcation_json = value; }
        public bool Rms_is_enable_adhoc { get => rms_is_enable_adhoc; set => rms_is_enable_adhoc = value; }

        public static Project NewByReader(SQLiteDataReader reader)
        {
            Project p = new Project
            {
                Id = int.Parse(reader["id"].ToString()),
                User_table_pk = int.Parse(reader["user_table_pk"].ToString()),
                Rms_project_id = int.Parse(reader["rms_project_id"].ToString()),
                Rms_name = reader["rms_name"].ToString(),
                Rms_display_name = reader["rms_display_name"].ToString(),
                Rms_description = reader["rms_description"].ToString(),
                Rms_is_owner = int.Parse(reader["rms_is_owner"].ToString()) == 1 ? true : false,
                Rms_tenant_id = reader["rms_tenant_id"].ToString(),
                Rms_classifcation_json = reader["rms_classifcation_json"].ToString(),
                Rms_is_enable_adhoc = int.Parse(reader["rms_is_enable_adhoc"].ToString()) == 1 ? true : false
            };
            return p;
        }

        public static Project ConstructItem(int upk, int project_id, string project_name,
            string project_display_name, string project_description,
            bool isOwner, string tenant_id)
        {
            Project item = new Project
            {
                User_table_pk = upk,
                Rms_project_id = project_id,
                Rms_name = project_name,
                Rms_display_name = project_display_name,
                Rms_description = project_description,
                Rms_is_owner = isOwner,
                Rms_tenant_id = tenant_id
            };
            return item;
        }
    }

    public class ProjectDao
    {
        public static readonly string SQL_Create_Table_Project = @"
                CREATE TABLE IF NOT EXISTS Project (
                   id                   integer      NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE ,
                   user_table_pk        integer      NOT NULL , 
                   rms_project_id       varchar(255) NOT NULL ,
                   rms_name             varchar(255) DEFAULT '', 
                   rms_display_name     varchar(255) DEFAULT '', 
                   rms_description      varchar(255) DEFAULT '', 
                   rms_is_owner         integer      DEFAULT 0 , 
                   rms_pending_setting  varchar(255) DEFAULT '', 
                   rms_owner_raw_json   varchar(255) DEFAULT '', 
                   user_membership_id   varchar(255) DEFAULT '', 
                   rms_tenant_name      varchar(255) DEFAULT '', 
                   rms_tenant_id        varchar(255) DEFAULT '',
                   rms_classifcation_json   varchar(255)  DEFAULT '{}',
                   --version1 update--
                   rms_is_enable_adhoc  integer      DEFAULT 1,
                    
                   --talbe restrictions--
                   unique(user_table_pk,rms_project_id), 
                   foreign key(user_table_pk) references User(id) on delete cascade);    
        ";

        public static readonly string SQL_Alter_Table_Project_V1 = @"
                ALTER TABLE Project ADD COLUMN
                    rms_is_enable_adhoc   integer     DEFAULT 1;
        ";

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
                                    int user_table_pk,
                                    int project_id, string project_name, string project_display_name,
                                 string project_description, bool isOnwer, string tenant_id)
        {
            string sql = @"
                INSERT INTO
                    Project( user_table_pk,  rms_project_id,  rms_name,  rms_display_name,  rms_description,  rms_is_owner,  rms_tenant_id)
                    VALUES (  @user_table_pk, @rms_project_id, @rms_name, @rms_display_name, @rms_description, @rms_is_owner, @rms_tenant_id)
                ON CONFLICT(user_table_pk,rms_project_id)
                   DO UPDATE SET
                      rms_name=excluded.rms_name,
                      rms_display_name=excluded.rms_display_name,      
                      rms_description=excluded.rms_description,
                      rms_is_owner=excluded.rms_is_owner,
                      rms_tenant_id=excluded.rms_tenant_id;
                -- get this itme's primary id--
                SELECT id FROM Project 
                WHERE user_table_pk=@user_table_pk AND rms_project_id=@rms_project_id;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk),
                new SQLiteParameter("@rms_project_id",project_id),
                new SQLiteParameter("@rms_name",project_name),
                new SQLiteParameter("@rms_display_name",project_display_name),
                new SQLiteParameter("@rms_description",project_description),
                new SQLiteParameter("@rms_is_owner",isOnwer),
                new SQLiteParameter("@rms_tenant_id",tenant_id)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Delete_SQL(
            int user_table_pk,int rms_project_id)
        {
            string sql = @"
                DELETE FROM 
                    Project
                WHERE 
                    user_table_pk=@user_table_pk AND 
                    rms_project_id =@rms_project_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk),
                new SQLiteParameter("@rms_project_id",rms_project_id),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
                                    int user_table_pk)
        {
            string sql = @"
            SELECT 
                id,user_table_pk,
                rms_project_id,rms_name,rms_display_name,rms_description,
                rms_is_owner,rms_tenant_id,rms_classifcation_json,rms_is_enable_adhoc
            FROM 
                Project
            WHERE
                user_table_pk=@user_table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_PK_PID_SQL(int user_table_pk)
        {
            string sql = @"
                SELECT id,rms_project_id
                FROM Project
                WHERE user_table_pk=@user_table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_Classification_Json(int table_project_id)
        {
            string sql = @"
                SELECT rms_classifcation_json
                FROM    Project
                WHERE  id=@table_project_id;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_project_id",table_project_id)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Classification_Json(int table_project_id,
            string classificationJson)
        {
            string sql = @"
                UPDATE OR IGNORE
                    Project
                SET
                    rms_classifcation_json = @classificationJson
                WHERE  id=@table_project_id;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_project_id",table_project_id),
                new SQLiteParameter("@classificationJson",classificationJson)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_IsEnabledAdhoc(int table_project_id, bool isEnabled)
        {
            string sql = @"
                UPDATE OR IGNORE
                    Project
                SET
                    rms_is_enable_adhoc = @is_enabled
                WHERE
                    id = @table_project_id
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_project_id",table_project_id),
                new SQLiteParameter("@is_enabled",isEnabled?1:0)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }
    }
}
