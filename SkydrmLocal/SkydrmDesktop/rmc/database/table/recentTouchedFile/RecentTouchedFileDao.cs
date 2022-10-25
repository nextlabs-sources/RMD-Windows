using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database.table.recentTouchedFile
{
    public class RecentTouchedFile
    {
        private int id;

        private int user_table_pk;

        private string status;

        private DateTime last_modified_time= DateTime.Now;

        private string name;

        private string message;

        public RecentTouchedFile()
        {

        }

        public int Id { get => id; set => id = value; }
        public int User_Table_Pk { get => user_table_pk; set => user_table_pk = value; }
        public string Status { get => status; set => status = value; }
        public DateTime Last_modified_time { get => last_modified_time; set => last_modified_time = value; }
        public string Name { get => name; set => name = value; }
        public string Message { get => message; set => message = value; }

        public static RecentTouchedFile NewByReader(SQLiteDataReader reader)
        {           
            var rt= new RecentTouchedFile()
            {
                Id = Int32.Parse(reader["id"].ToString()),
                User_Table_Pk = Int32.Parse(reader["user_table_pk"].ToString()),
                Status = reader["status"].ToString(),
                // last_modify need convert
                Name=reader["name"].ToString(),
                Message =reader["msg"].ToString(),
                Last_modified_time=DateTime.Parse(reader["last_modified_time"].ToString())
            };
            //DateTime.TryParse(reader["last_modified_time"].ToString(), out rt.last_modified_time);

            return rt;
        }

    }

    public class RecentTouchedFileDao
    {
        public static readonly string SQL_Create_Table_RecentTouchedFile = @"
                CREATE TABLE IF NOT EXISTS RecentTouchedFile (
                   id                  integer          NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   user_table_pk       integer          default 0,         
                   status              varchar(255)     NOT NULL default '', 
                   last_modified_time  DateTime         NOT NULL default (datetime('now','localtime')),
                   name                varchar(255)     NOT NULL default '',                     
                   msg                 varchar(255)     NOT NULL default '',                     
                       
                   UNIQUE(user_table_pk,name)
                   foreign key(user_table_pk) references User(id) on delete cascade);
        ";


        public static KeyValuePair<String, SQLiteParameter[]> Update_Or_Insert_SQL(
               int user_table_pk, string status, string name)
        {

            string sql = @"
                   UPDATE RecentTouchedFile 
                        SET 
                            status=@status,
                            last_modified_time=datetime('now','localtime')
                                         
                        WHERE
                            user_table_pk = @user_table_pk AND name=@name;

                       ---------if no updated happeded, then insert one--------------------------

                        INSERT INTO  
                                RecentTouchedFile(user_table_pk,status,name)
                        SELECT 
                                @user_table_pk,@status,@name
                        WHERE
                            ( SELECT changes() = 0 );
                ";

            SQLiteParameter[] parameters = {
                   new SQLiteParameter("@user_table_pk" , user_table_pk),
                   new SQLiteParameter("@status" , status),
                   new SQLiteParameter("@name" , name)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_SQL(int user_table_pk)
        {
            string sql = @"
              SELECT   
                *
            FROM
                RecentTouchedFile
            WHERE
                 user_table_pk=@user_table_pk 
            AND 
              ( julianday('now','localtime') - julianday(last_modified_time) <= 7
            OR
                status='WaitingUpload'
              )
            ;";

            SQLiteParameter[] parameters = {
                new SQLiteParameter("@user_table_pk",user_table_pk)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }
    }
}
//}
