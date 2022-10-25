using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.database
{
    public class ServerDao
    {
        public static readonly string SQL_Create_Table_Server = @"
             CREATE TABLE IF NOT EXISTS Server(
                id                  integer         NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                router_url          varchar(255)    NOT NULL default '', 
                url                 varchar(255)    NOT NULL default '', 
                tenand_id           varchar(255)    NOT NULL default '', 
                last_access         DateTime        default current_timestamp,
                last_logout         DateTime        default current_timestamp ,
                access_count        integer         default 1,
                is_onpremise        integer         default 1,                       

                UNIQUE(router_url,tenand_id)
                );
        ";

        // Using router and teandId as union primary key insteading of url -- fix bug 52730
        public static KeyValuePair<String, SQLiteParameter[]> Upsert_SQL(string router, string url, string tenand, bool isOnPremise)
        {
            string sql = @"
                INSERT INTO 
                    Server( url, router_url, tenand_id, is_onpremise)
                    Values(@url,@router_url, @tenand_id, @is_onpremise)
                 ON CONFLICT(tenand_id,router_url)
                    DO UPDATE SET 
                        access_count = access_count+1,
                        last_access  = current_timestamp,
                        last_logout  = current_timestamp;          
                -- find id--
                Select id from Server
                where tenand_id=@tenand_id and router_url=@router_url;   
                
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@url",url),
                new SQLiteParameter("@router_url", router),
                new SQLiteParameter("@tenand_id", tenand),
                new SQLiteParameter("@is_onpremise", isOnPremise==true?1:0)
            };

            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);

        }

        public static KeyValuePair<String, SQLiteParameter[]> Query_ID_SQL(string router, string tenant)
        {
            string sql = @"
                select id 
                from server
                where 
                    router_url=@router_url and 
                    tenand_id = @tenand_id
                ;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@router_url",router),
                new SQLiteParameter("@tenand_id",tenant)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }


        public static KeyValuePair<String, SQLiteParameter[]> Update_LastLogout_SQL(int primary_key)
        {
            string sql = @"
                update server
                set  last_logout=current_timestamp
                where id=@id;
            ";
            SQLiteParameter[] parameters = {
                new SQLiteParameter("@id",primary_key)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        //only select 5 dataitem by desc
        public static KeyValuePair<String, SQLiteParameter[]> Query_Router_Url_SQL()
        {
            string sql = @"
            SELECT router_url 
            FROM server 
            order by[Id] desc limit 0,5;";
            SQLiteParameter[] parameters = { };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Get Url
        public static KeyValuePair<String, SQLiteParameter[]> Query_Current_Router_Url_SQL(int primary_key)
        {
            string sql = @"
            SELECT router_url 
            FROM server 
            where id=@id;";
            SQLiteParameter[] parameters = {
             new SQLiteParameter("@id",primary_key)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        // Get Url
        public static KeyValuePair<String, SQLiteParameter[]> Query_Url_SQL(int primary_key)
        {
            string sql = @"
            SELECT url 
            FROM server 
            where id=@id;";
            SQLiteParameter[] parameters = {
             new SQLiteParameter("@id",primary_key)
            };
            return new KeyValuePair<string, SQLiteParameter[]>(sql, parameters);
        }

        //public ServerDao(SQLiteDBHelper sQLiteDBHelper)
        //{
        //    this.sQLiteDBHelper = sQLiteDBHelper;
        //}

        //public void Insert(string Url, string Tenant_id, string Router_url, DateTime Last_access, Int32 Access_count, DateTime Last_logout)
        //{

        //    string sql = "INSERT INTO Server(Url, Tenand_id , Router_url, Last_access, Access_count, Last_logout)values(@Url, @Tenand_id ,@Router_url, @Last_access , @Access_count, @Last_logout);";

        //    SQLiteParameter[] parameters = {
        //                             new SQLiteParameter("@Url" , Url),
        //                             new SQLiteParameter("@Tenand_id" , Tenant_id),
        //                             new SQLiteParameter("@Router_url", Router_url),
        //                             new SQLiteParameter("@Last_access", Last_access),
        //                             new SQLiteParameter("@Access_count" , Access_count),
        //                             new SQLiteParameter("@Last_logout" , Last_logout)};

        //    sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        //}

        //public void Delete(Int32 Id)
        //{
        //    string sql = "DELETE FROM Server WHERE Id = @Id;";
        //    SQLiteParameter[] parameters = {
        //                             new SQLiteParameter("@Id" , Id) };

        //    sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        //}

        //public void Update(Int32 Id, string Url, string Tenant_id, string Router_url, DateTime Last_access, Int32 Access_count, DateTime Last_logout)
        //{

        //    string sql = "UPDATE Server SET Url = @Url ,Tenand_id=@Tenand_id, Router_url=@Router_url ,Last_access=@Last_access ,Access_count=@Access_count, Last_logout=@Last_logout WHERE Id=@Id;";
        //    SQLiteParameter[] parameters = {
        //                            new SQLiteParameter("@Url" , Url),
        //                             new SQLiteParameter("@Tenand_id" , Tenant_id),
        //                             new SQLiteParameter("@Router_url", Router_url),
        //                             new SQLiteParameter("@Last_access", Last_access),
        //                             new SQLiteParameter("@Access_count" , Access_count),
        //                             new SQLiteParameter("@Last_logout" , Last_logout),
        //                             new SQLiteParameter("@Id",Id)};

        //    sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        //}

        //public List<Server> queryAll() {
        //    List<Server> result = new List<Server>();
        //     string sql = "SELECT * FROM Server;";
        //    SQLiteDataReader sQLiteDataReader = sQLiteDBHelper.ExecuteReader(sql, null);

        //    Int32 Id = -1;

        //    while (sQLiteDataReader.Read())
        //    {
        //        Id = -1;
        //        Int32.TryParse(sQLiteDataReader["Id"].ToString(), out Id);
        //        string Url = sQLiteDataReader["Url"].ToString();
        //        string Tenand_id = sQLiteDataReader["Tenand_id"].ToString();
        //        string Router_url = sQLiteDataReader["Router_url"].ToString();
        //        string Last_access = sQLiteDataReader["Last_access"].ToString();
        //        string Access_count = sQLiteDataReader["Access_count"].ToString();
        //        string Last_logout = sQLiteDataReader["Last_logout"].ToString();

        //        Server server = new Server();
        //        server.Id = Id;
        //        server.Url = Url;
        //        server.Tenant_id = Tenand_id;
        //        server.Router_url = Router_url;
        //        server.Last_access = Last_access;
        //        server.Access_count = Access_count;
        //        server.Last_logout = Last_logout;

        //        result.Add(server);
        //    }
        //    return result;
        //}

        //public Server QueryById(Int32 parameter)
        //{
        //    Server result = new Server();
        //    string sql = "SELECT * FROM Server WHERE Id=@Id;";

        //    SQLiteParameter[] parameters = new SQLiteParameter[]
        //    {   
        //         new SQLiteParameter("@Id", parameter)
        //    };

        //    SQLiteDataReader sQLiteDataReader = sQLiteDBHelper.ExecuteReader(sql, parameters);

        //    Int32 Id = -1;

        //    if (sQLiteDataReader.Read())
        //    {
        //        Id = -1;
        //        Int32.TryParse(sQLiteDataReader["Id"].ToString(), out Id);
        //        string Url = sQLiteDataReader["Url"].ToString();
        //        string Tenand_id = sQLiteDataReader["Tenand_id"].ToString();
        //        string Router_url = sQLiteDataReader["Router_url"].ToString();
        //        string Last_access = sQLiteDataReader["Last_access"].ToString();
        //        string Access_count = sQLiteDataReader["Access_count"].ToString();
        //        string Last_logout = sQLiteDataReader["Last_logout"].ToString();


        //        result.Id = Id;
        //        result.Url = Url;
        //        result.Tenant_id = Tenand_id;
        //        result.Router_url = Router_url;
        //        result.Last_access = Last_access;
        //        result.Access_count = Access_count;
        //        result.Last_logout = Last_logout;
        //    }

        //    return result;
        //}

    }
}
