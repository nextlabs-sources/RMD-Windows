using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database.table.systembucket
{

    public class SystemBucket
    {
        private int id;
        private int user_pk_id;

        private int systembucket_rms_id;
        private string systembucket_rms_tenant_id;
        private string classification_raw;
        private bool is_enable_adhoc;

        private string reserved1;
        private string reserved2;
        private string reserved3;
        private string reserved4;

        public SystemBucket()
        {
        }

        public int Id { get => id;  }
        public int UserPKId { get => user_pk_id; }
        public int SystemBucketRMSId { get => systembucket_rms_id; }
        public string SystemBucketRMSTenantId { get => systembucket_rms_tenant_id;  }
        public string ClassificationJson{ get => classification_raw;}
        public bool IsEnableAdhoc { get => is_enable_adhoc;}



        public static SystemBucket NewByReader(SQLiteDataReader reader)
        {
            return new SystemBucket()
            {
                id = int.Parse(reader["id"].ToString()),
                user_pk_id = int.Parse(reader["user_table_pk"].ToString()),
                systembucket_rms_id = int.Parse(reader["rms_systembucket_id"].ToString()),
                systembucket_rms_tenant_id = reader["rms_systembucket_tenant_id"].ToString(),
                classification_raw = reader["rms_systembucket_classification"].ToString(),
                is_enable_adhoc = int.Parse(reader["rms_isEnableAdhoc"].ToString())==1?true:false,
                reserved1 = "",
                reserved2 = "",
                reserved3 = "",
                reserved4 = "",
            };
        }

        public static SystemBucket NewDefault()
        {
            return new SystemBucket()
            {
                id=-1,
                user_pk_id =-1,
                classification_raw ="[]",
                is_enable_adhoc =false,
                systembucket_rms_id =-1,
                systembucket_rms_tenant_id="",
                reserved1="",
                reserved2 = "",
                reserved3 = "",
                reserved4 = "",
            };
        }

    }

    class SystemBucketDao
    {
        public static readonly string SQL_Create_Table_SystemBucket = @"
            CREATE TABLE IF NOT EXISTS SystemBucket(
                   id                               integer   NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                   user_table_pk                    integer   NOT NULL,
       
                   rms_systembucket_id              integer   NOT NULL default 0,
                   rms_systembucket_tenant_id       text      NOT NULL default '',
                   rms_systembucket_classification  text      NOT NULL default '',
                   rms_isEnableAdhoc                integer   NOT NULL default 0,
       
                   reserved1                        text      default '',
                   reserved2                        text      default '',
                   reserved3                        text      default '',
                   reserved4                        text      default '',     
       
                   unique(user_table_pk),
                   foreign key(user_table_pk) references User(id) on delete cascade
            );
        ";

        static public KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(
            int user_table_pk,
            int rms_systembucket_id,
            string rms_systembucket_tenant_id,
            string rms_systembucket_classification,
            bool rms_isEnableAdhoc)
        {
            string sql = @"
                INSERT INTO 
                    SystemBucket(user_table_pk,
                            rms_systembucket_id,rms_systembucket_tenant_id,rms_systembucket_classification,rms_isEnableAdhoc)
                    VALUES(@user_table_pk,
                            @rms_systembucket_id,@rms_systembucket_tenant_id,@rms_systembucket_classification,@rms_isEnableAdhoc)
                ON CONFLICT(user_table_pk)
                   DO UPDATE SET
                      rms_systembucket_id=excluded.rms_systembucket_id,
                      rms_systembucket_tenant_id=excluded.rms_systembucket_tenant_id,
                      rms_systembucket_classification=excluded.rms_systembucket_classification,
                      rms_isEnableAdhoc=excluded.rms_isEnableAdhoc;
            ";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk),
                new SQLiteParameter("@rms_systembucket_id",rms_systembucket_id),
                new SQLiteParameter("@rms_systembucket_tenant_id",rms_systembucket_tenant_id),
                new SQLiteParameter("@rms_systembucket_classification",rms_systembucket_classification),
                new SQLiteParameter("@rms_isEnableAdhoc",rms_isEnableAdhoc?1:0)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(
                                    int user_table_pk)
        {
            string sql = @"
            SELECT 
                id,user_table_pk,

                rms_systembucket_id,rms_systembucket_tenant_id,rms_systembucket_classification,rms_isEnableAdhoc
            FROM 
                SystemBucket
            WHERE
                user_table_pk=@user_table_pk;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


    }
}
