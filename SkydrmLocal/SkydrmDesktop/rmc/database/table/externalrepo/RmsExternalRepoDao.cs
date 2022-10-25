using SkydrmLocal.rmc.common.helper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo
{
    public class RmsExternalRepo
    {
        public int Id { get; set; }
        // foreign key, table owner
        public int User_table_pk { get; set; }

        public string RepoId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsShared { get; set; }
        public bool IsDefault { get; set; }
        public string AccountName { get; set; }
        public string AccountId { get; set; }
        public string Token { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public string Preference { get; set; }
        public string ProviderClass { get; set; }

        // Reserved
        /// <summary>
        /// Note: for SharePointOnline & SharePointOnPremise need to store other info(serverSite, account, password)
        /// using these reserved fields.
        /// </summary>
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }
        public string Reserved5 { get; set; }
        public string Reserved6 { get; set; }
        public string Reserved7 { get; set; }
        public string Reserved8 { get; set; }
        public string Reserved9 { get; set; }
        public string Reserved10 { get; set; }

        public static RmsExternalRepo NewByReader(SQLiteDataReader reader)
        {
            RmsExternalRepo item = new RmsExternalRepo();
            {
                item.Id = int.Parse(reader["id"].ToString());
                item.User_table_pk = int.Parse(reader["user_table_pk"].ToString());
                // remote
                item.RepoId = reader["rms_repo_id"].ToString();
                item.Name = reader["rms_repo_name"].ToString();
                item.Type = reader["rms_repo_type"].ToString();
                item.IsShared = int.Parse(reader["rms_IsShared"].ToString()) == 1;
                item.IsDefault = int.Parse(reader["rms_IsDefault"].ToString()) == 1;
                item.AccountName = reader["rms_AccountName"].ToString();
                item.AccountId = reader["rms_AccountId"].ToString();
                item.Token = reader["rms_token"].ToString();
                // The following stored data format like myVault's
                item.CreationTime = DateTime.Parse(reader["rms_creation_time"].ToString()).ToUniversalTime(); 
                item.UpdateTime = DateTime.Parse(reader["rms_update_time"].ToString()).ToUniversalTime();
                item.Preference = reader["rms_preference"].ToString();
                item.ProviderClass = reader["rms_providerClass"].ToString();
                // reserved
                item.Reserved1 = "";
                item.Reserved2 = "";
                item.Reserved3 = "";
                item.Reserved4 = "";
            }

            return item;
        }
    }

    public class RmsExternalRepoDao
    {
        public static readonly string SQL_Create_Table_RmsExternalRepo = @"
            CREATE TABLE IF NOT EXISTS RmsExternalRepo (
                id                          integer             NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                user_table_pk               integer             default 0, 

                rms_repo_id                 varchar(255)        NOT NULL default '',
                rms_repo_name               varchar(255)        NOT NULL default '',
                rms_repo_type               varchar(255)        NOT NULL default '',
                rms_IsShared                integer             NOT NULL default 0,
                rms_IsDefault               integer             NOT NULL default 0,
                rms_AccountName             varchar(255)        NOT NULL default '',
                rms_AccountId               varchar(255)        NOT NULL default '',
                rms_token                   varchar(255)        NOT NULL default '',
                rms_creation_time           datetime            NOT NULL default (datetime('now','localtime')),
                rms_update_time             datetime            NOT NULL default (datetime('now','localtime')),
                rms_preference              varchar(255)        NOT NULL default '',
                rms_providerClass           varchar(255)        NOT NULL default '',

                ----- reserved -----------
                reserved1                   text                DEFAULT '',
                reserved2                   text                DEFAULT '',
                reserved3                   text                DEFAULT '',
                reserved4                   text                DEFAULT '',
                reserved5                   text                DEFAULT '',
                reserved6                   text                DEFAULT '',
                reserved7                   text                DEFAULT '',
                reserved8                   text                DEFAULT '',
                reserved9                   text                DEFAULT '',
                reserved10                  text                DEFAULT '',

                UNIQUE(user_table_pk,rms_repo_id),
                foreign key(user_table_pk) references User(id) on delete cascade);
        ";

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(int user_table_pk,
            string rms_repo_id, string rms_repo_name, string rms_repo_type,
            int rms_isShared, int rms_isDefault, string rms_accountname, string rms_accountId, 
            string token, long createTime, long updateTime, string preference, string providerClass)
        {
            // Need to convert
            string createTimeStr = JavaTimeConverter.ToCSDateTime(createTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string updateTimeStr = JavaTimeConverter.ToCSDateTime(updateTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"
                UPDATE RmsExternalRepo
                SET
                    rms_repo_name=@rms_repo_name,
                    rms_IsShared=@rms_IsShared,
                    rms_IsDefault=@rms_IsDefault,
                    rms_token=@rms_token,
                    rms_update_time=@rms_update_time
                WHERE
                    user_table_pk=@user_table_pk AND rms_repo_id=@rms_repo_id;

                --------if no updated happened, then insert one ------

                INSERT INTO
                    RmsExternalRepo(user_table_pk,
                                    rms_repo_id,
                                    rms_repo_name,
                                    rms_repo_type,
                                    rms_IsShared,
                                    rms_IsDefault,
                                    rms_AccountName,
                                    rms_AccountId,
                                    rms_token,
                                    rms_creation_time,
                                    rms_update_time,
                                    rms_preference,
                                    rms_providerClass)
                SELECT
                    @user_table_pk,
                    @rms_repo_id,
                    @rms_repo_name,
                    @rms_repo_type,
                    @rms_isShared,
                    @rms_isDefault,
                    @rms_accountName,
                    @rms_accountId,
                    @rms_token,
                    @rms_creation_time,
                    @rms_update_time,
                    @rms_preference, 
                    @rms_providerClass
                WHERE
                    ( SELECT changes() = 0 ); 
         ";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_table_pk),
                new SQLiteParameter("@rms_repo_id", rms_repo_id),
                new SQLiteParameter("@rms_repo_name", rms_repo_name),
                new SQLiteParameter("@rms_repo_type", rms_repo_type),
                new SQLiteParameter("@rms_isShared", rms_isShared),
                new SQLiteParameter("@rms_isDefault", rms_isDefault),
                new SQLiteParameter("@rms_accountName", rms_accountname),
                new SQLiteParameter("@rms_accountId", rms_accountId),
                new SQLiteParameter("@rms_token", token),
                new SQLiteParameter("@rms_creation_time", createTimeStr),
                new SQLiteParameter("@rms_update_time", updateTimeStr),
                new SQLiteParameter("@rms_preference", preference),
                new SQLiteParameter("@rms_providerClass", providerClass),
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL_And_Query(int user_table_pk,
            string rms_repo_id, string rms_repo_name, string rms_repo_type,
            int rms_isShared, int rms_isDefault, string rms_accountname, string rms_accountId,
            string token, long createTime, long updateTime, string preference, string providerClass)
        {
            // Need to convert
            string createTimeStr = JavaTimeConverter.ToCSDateTime(createTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string updateTimeStr = JavaTimeConverter.ToCSDateTime(updateTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            string sql = @"
                UPDATE RmsExternalRepo
                SET
                    rms_repo_name=@rms_repo_name,
                    rms_IsShared=@rms_IsShared,
                    rms_IsDefault=@rms_IsDefault,
                    rms_token=@rms_token,
                    rms_update_time=@rms_update_time
                WHERE
                    user_table_pk=@user_table_pk AND rms_repo_id=@rms_repo_id;

                --------if no updated happened, then insert one ------

                INSERT INTO
                    RmsExternalRepo(user_table_pk,
                                    rms_repo_id,
                                    rms_repo_name,
                                    rms_repo_type,
                                    rms_IsShared,
                                    rms_IsDefault,
                                    rms_AccountName,
                                    rms_AccountId,
                                    rms_token,
                                    rms_creation_time,
                                    rms_update_time,
                                    rms_preference,
                                    rms_providerClass)
                SELECT
                    @user_table_pk,
                    @rms_repo_id,
                    @rms_repo_name,
                    @rms_repo_type,
                    @rms_isShared,
                    @rms_isDefault,
                    @rms_accountName,
                    @rms_accountId,
                    @rms_token,
                    @rms_creation_time,
                    @rms_update_time,
                    @rms_preference,
                    @rms_providerClass
                WHERE
                    ( SELECT changes() = 0 ); 

               -- get this itme's primary id--
                SELECT id FROM RmsExternalRepo 
                WHERE user_table_pk=@user_table_pk AND rms_repo_id=@rms_repo_id;
         ";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_table_pk),
                new SQLiteParameter("@rms_repo_id", rms_repo_id),
                new SQLiteParameter("@rms_repo_name", rms_repo_name),
                new SQLiteParameter("@rms_repo_type", rms_repo_type),
                new SQLiteParameter("@rms_isShared", rms_isShared),
                new SQLiteParameter("@rms_isDefault", rms_isDefault),
                new SQLiteParameter("@rms_accountName", rms_accountname),
                new SQLiteParameter("@rms_accountId", rms_accountId),
                new SQLiteParameter("@rms_token", token),
                new SQLiteParameter("@rms_creation_time", createTimeStr),
                new SQLiteParameter("@rms_update_time", updateTimeStr),
                new SQLiteParameter("@rms_preference", preference),
                new SQLiteParameter("@rms_providerClass", providerClass),
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Delete_SQL(int user_table_pk, string repoId)
        {
            string sql = @"
                DELETE FROM 
                    RmsExternalRepo
                WHERE 
                    user_table_pk=@user_table_pk AND 
                    rms_repo_id =@rms_repo_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk),
                new SQLiteParameter("@rms_repo_id",repoId),
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        // Query all records of RmsExternalRepo table which current user added repositories.
        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_tb_pk)
        {
            // sql statement -- '*' means acquire all colunms\fields of each record\row.
            string sql = @"SELECT * FROM
                                RmsExternalRepo
                           WHERE
                                user_table_pk = @user_table_pk
                           ORDER BY rms_repo_name ASC;
                            ";
            // sql query parameter
            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_tb_pk)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        // Query token by user_tb_pk and repoId
        public static KeyValuePair<String, SQLiteParameter[]> Query_Token_SQL(int user_tb_pk, string repoId)
        {
            string sql = @"SELECT rms_token 
                           FROM
                                RmsExternalRepo
                           WHERE
                                user_table_pk = @user_table_pk AND rms_repo_id=@rms_repo_id;
                            ";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_tb_pk),
                new SQLiteParameter("@rms_repo_id", repoId)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        // Query token by table id.
        public static KeyValuePair<String, SQLiteParameter[]> Query_Token_SQL(int table_RmsExternalRepo_pk)
        {
            string sql = @"SELECT rms_token 
                           FROM
                                RmsExternalRepo
                           WHERE
                                id = @table_pk;
                            ";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@table_pk", table_RmsExternalRepo_pk)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Query_PK_RepoId_SQL(int user_table_pk)
        {
            string sql = @"
                SELECT id,rms_repo_id
                FROM RmsExternalRepo
                WHERE user_table_pk=@user_table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Repo_Name(int table_RmsExternalRepo_pk, string name)
        {
            string sql = @"UPDATE 
                                RmsExternalRepo
                           SET 
                                rms_repo_name=@rms_repo_name
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_RmsExternalRepo_pk),
                new SQLiteParameter("@rms_repo_name",name)
            };
            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Token(int table_RmsExternalRepo_pk, string token)
        {
            string sql = @"UPDATE 
                                RmsExternalRepo
                           SET 
                                rms_token=@rms_token
                           WHERE
                                id=@table_pk;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@table_pk",table_RmsExternalRepo_pk),
                new SQLiteParameter("@rms_token", token)
            };
            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Update_Token_By_RepoId(int user_table_pk, string repoid, string token)
        {
            string sql = @"UPDATE 
                                RmsExternalRepo
                           SET 
                                rms_token=@rms_token
                           WHERE
                                user_table_pk = @user_table_pk AND rms_repo_id=@rms_repo_id;
                        ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk),
                new SQLiteParameter("@rms_repo_id",repoid),
                new SQLiteParameter("@rms_token", token)
            };
            return new KeyValuePair<String, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Query_Reserved1(int user_tb_pk, string repoId)
        {
            string sql = @"SELECT reserved1 
                           FROM
                                RmsExternalRepo
                           WHERE
                                user_table_pk = @user_table_pk AND rms_repo_id=@rms_repo_id;
                            ";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_tb_pk),
                new SQLiteParameter("@rms_repo_id", repoId)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Query_Reserved2(int user_tb_pk, string repoId)
        {
            string sql = @"SELECT reserved2 
                           FROM
                                RmsExternalRepo
                           WHERE
                                user_table_pk = @user_table_pk AND rms_repo_id=@rms_repo_id;
                            ";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_tb_pk),
                new SQLiteParameter("@rms_repo_id", repoId)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }

        public static KeyValuePair<string, SQLiteParameter[]> Query_Reserved3(int user_tb_pk, string repoId)
        {
            string sql = @"SELECT reserved3 
                           FROM
                                RmsExternalRepo
                           WHERE
                                user_table_pk = @user_table_pk AND rms_repo_id=@rms_repo_id;
                            ";

            SQLiteParameter[] paras =
            {
                new SQLiteParameter("@user_table_pk", user_tb_pk),
                new SQLiteParameter("@rms_repo_id", repoId)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, paras);
        }
    }
}
