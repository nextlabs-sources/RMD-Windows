using SkydrmLocal.rmc.database2.manager;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database2.table.recipient
{
    public class RecipientDao
    {
        public static readonly string SQL_Create_Table_Recipients = @"
                CREATE TABLE IF NOT EXISTS Recipients (
                   id           integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE , 
                   file_id      integer NOT NULL,
                    name         varchar(255) default '', 
                   email        varchar(255) NOT NULL,
                   foreign key(file_id) references MyVaultFile(id) on delete cascade);
        ";

        //private SQLiteDBHelper sQLiteDBHelper = null;

        //public RecipientDao(SQLiteDBHelper sQLiteDBHelper)
        //{
        //    this.sQLiteDBHelper = sQLiteDBHelper;
        //}

 
        //public void Insert(Int32 File_id,string Name,string Email)
        //{
        //    string sql = "INSERT INTO Recipients(File_id,Name,Email) values (@File_id,@Name,@Email);";

        //    SQLiteParameter[] parameters = {
        //                             new SQLiteParameter("@File_id" , File_id),
        //                             new SQLiteParameter("@Name" , Name),
        //                             new SQLiteParameter("@Email", Email)                                  
        //                        };

        //    sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        //}

        //public void Delete(Int32 Id)
        //{
        //    string sql = "DELETE FROM Recipients WHERE Id = @Id;";
        //    SQLiteParameter[] parameters = {
        //                             new SQLiteParameter("@Id" , Id) };

        //    sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        //}

        //public void Update(Int32 Id, Int32 File_id, string Name, string Email)
        //{
        //    string sql = "UPDATE Recipients SET File_id = @File_id ,Name=@Name, Email=@Email WHERE Id=@Id;";
        //    SQLiteParameter[] parameters = {
        //                             new SQLiteParameter("@File_id" , File_id),
        //                             new SQLiteParameter("@Name" , Name),
        //                             new SQLiteParameter("@Email", Email),                    
        //                             new SQLiteParameter("@Id",Id)};

        //    sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        //}

        //public List<Recipient> QueryByFileId(Int32 Paramete_File_id)
        //{
        //    List<Recipient> result = new List<Recipient>();

        //    string sql = "SELECT * FROM Recipients WHERE File_id=@File_id;";

        //    SQLiteParameter[] parameters = new SQLiteParameter[]
        //    {
        //         new SQLiteParameter("@File_id", Paramete_File_id)
        //    };

        //    SQLiteDataReader sQLiteDataReader = sQLiteDBHelper.ExecuteReader(sql, parameters);

        //    Int32 Id = -1;
        //    Int32 File_id = -1;
        //    if (sQLiteDataReader.Read())
        //    {
        //        Id = -1;
        //        File_id = -1;

        //        Int32.TryParse(sQLiteDataReader["Id"].ToString(), out Id);
        //        Int32.TryParse(sQLiteDataReader["File_id"].ToString(), out File_id);
        //        string Name = sQLiteDataReader["Name"].ToString();
        //        string Email = sQLiteDataReader["Email"].ToString();

        //        Recipient recipient = new Recipient();
        //        recipient.Id = Id;
        //        recipient.File_id = File_id;
        //        recipient.Name = Name;
        //        recipient.Email = Email;
        //        result.Add(recipient);
        //    }
        //    return result;
        //}

    }
}
