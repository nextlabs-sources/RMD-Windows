using SkydrmLocal.rmc.database2.manager;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database2.table.operation
{
    public class OperationDao
    {
        private SQLiteDBHelper sQLiteDBHelper = null;

        public OperationDao(SQLiteDBHelper sQLiteDBHelper)
        {
            this.sQLiteDBHelper = sQLiteDBHelper;
        }

     

        public void Insert(Int32 MyVault_file_id, string Operation,DateTime Operation_time, string Result, string User, Int32 Project_file_id, Int32 Project_local_file_id)
        {
            string sql = "INSERT INTO Operation(MyVault_file_id,Operation,Operation_time,Result,User,Project_file_id,Project_local_file_id) values (@MyVault_file_id,@Operation,@Operation_time,@Result,User,@Project_file_id,@Project_local_file_id);";

            SQLiteParameter[] parameters = {
                                     new SQLiteParameter("@MyVault_file_id" , MyVault_file_id),
                                     new SQLiteParameter("@Operation" , Operation),
                                     new SQLiteParameter("@Operation_time", Operation_time),
                                     new SQLiteParameter("@Result", Result),
                                     new SQLiteParameter("@User", User),
                                     new SQLiteParameter("@Project_file_id", Project_file_id),
                                     new SQLiteParameter("@Project_local_file_id", Project_local_file_id)                         
                                };

            sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        }
        public void Delete(Int32 Id)
        {
            string sql = "DELETE FROM Operation WHERE Id = @Id;";
            SQLiteParameter[] parameters = {
                                     new SQLiteParameter("@Id" , Id) };

            sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        }

        public void Update(Int32 Id,Int32 MyVault_file_id, string Operation, DateTime Operation_time, string Result, string User, Int32 Project_file_id, Int32 Project_local_file_id)
        {
            string sql = "UPDATE Operation SET MyVault_file_id = @MyVault_file_id ,Operation=@Operation, Operation_time=@Operation_time ,Result=@Result,User=@User,Project_file_id=@Project_file_id,Project_local_file_id=@Project_local_file_id WHERE Id=@Id;";
            SQLiteParameter[] parameters = {
                                     new SQLiteParameter("@MyVault_file_id" , MyVault_file_id),
                                     new SQLiteParameter("@Operation" , Operation),
                                     new SQLiteParameter("@Operation_time", Operation_time),
                                     new SQLiteParameter("@Result", Result),
                                     new SQLiteParameter("@User", User),
                                     new SQLiteParameter("@Project_file_id", Project_file_id),
                                     new SQLiteParameter("@Project_local_file_id", Project_local_file_id),        
                                     new SQLiteParameter("@Id",Id)};

            sQLiteDBHelper.ExecuteNonQuery(sql, parameters);
        }

        public List<Operation> QueryByMyVaultFileId(Int32 parameter_MyVault_file_id)
        {
            List<Operation> result = new List<Operation>();

            string sql = "SELECT * FROM Operation WHERE MyVault_file_id=@MyVault_file_id;";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                 new SQLiteParameter("@MyVault_file_id", parameter_MyVault_file_id)
            };

            SQLiteDataReader sQLiteDataReader = sQLiteDBHelper.ExecuteReader(sql, parameters);

            Int32 Id = -1;
            Int32 MyVault_file_id = -1;
            DateTime Operation_time = DateTime.Now;
            Int32 Project_file_id = -1;
            Int32 Project_local_file_id = -1;  
            
            if (sQLiteDataReader.Read())
            {
                Id = -1;
                MyVault_file_id = -1;
                Operation_time = DateTime.Now;
                Project_file_id = -1;
                Project_local_file_id = -1;       

                Int32.TryParse(sQLiteDataReader["Id"].ToString(), out Id);
                Int32.TryParse(sQLiteDataReader["MyVault_file_id"].ToString(), out MyVault_file_id);
                string Operation = sQLiteDataReader["Operation"].ToString();
                bool b_Operation_time = DateTime.TryParse(sQLiteDataReader["Operation_time"].ToString(), out Operation_time);
                string Result = sQLiteDataReader["Result"].ToString();
                string User = sQLiteDataReader["User"].ToString();
                Int32.TryParse(sQLiteDataReader["Project_file_id"].ToString(), out Project_file_id);
                Int32.TryParse(sQLiteDataReader["Project_local_file_id"].ToString(), out Project_local_file_id);

                Operation operation = new Operation();
                operation.Id = Id;
                operation.MyVault_file_id = MyVault_file_id;
                operation.S_operation = Operation;
                operation.Operation_time = Operation_time;
                operation.Result = Result;
                operation.User = User;
                operation.Project_file_id = Project_file_id;
                operation.Project_local_file_id = Project_local_file_id;
                result.Add(operation);
            }
            return result;
        }

        public List<Operation> QueryByProjectFileId(Int32 parameter_project_file_id)
        {
            List<Operation> result = new List<Operation>();

            string sql = "SELECT * FROM Operation WHERE Project_file_id=@Project_file_id;";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                 new SQLiteParameter("@Project_file_id", parameter_project_file_id)
            };

            SQLiteDataReader sQLiteDataReader = sQLiteDBHelper.ExecuteReader(sql, parameters);

            Int32 Id = -1;
            Int32 MyVault_file_id = -1;
            DateTime Operation_time = DateTime.Now;
            Int32 Project_file_id = -1;
            Int32 Project_local_file_id = -1;

            if (sQLiteDataReader.Read())
            {
                Id = -1;
                MyVault_file_id = -1;
                Operation_time = DateTime.Now;
                Project_file_id = -1;
                Project_local_file_id = -1;

                Int32.TryParse(sQLiteDataReader["Id"].ToString(), out Id);
                Int32.TryParse(sQLiteDataReader["MyVault_file_id"].ToString(), out MyVault_file_id);
                string Operation = sQLiteDataReader["Operation"].ToString();
                bool b_Operation_time = DateTime.TryParse(sQLiteDataReader["Operation_time"].ToString(), out Operation_time);
                string Result = sQLiteDataReader["Result"].ToString();
                string User = sQLiteDataReader["User"].ToString();
                Int32.TryParse(sQLiteDataReader["Project_file_id"].ToString(), out Project_file_id);
                Int32.TryParse(sQLiteDataReader["Project_local_file_id"].ToString(), out Project_local_file_id);

                Operation operation = new Operation();
                operation.Id = Id;
                operation.MyVault_file_id = MyVault_file_id;
                operation.S_operation = Operation;
                operation.Operation_time = Operation_time;
                operation.Result = Result;
                operation.User = User;
                operation.Project_file_id = Project_file_id;
                operation.Project_local_file_id = Project_local_file_id;
                result.Add(operation);
            }
            return result;
        }

        public List<Operation> QueryByProjectLocalFileId(Int32 parameter_project_local_file_id)
        {
            List<Operation> result = new List<Operation>();

            string sql = "SELECT * FROM Operation WHERE Project_local_file_id=@Project_local_file_id;";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                 new SQLiteParameter("@Project_local_file_id", parameter_project_local_file_id)
            };

            SQLiteDataReader sQLiteDataReader = sQLiteDBHelper.ExecuteReader(sql, parameters);

            Int32 Id = -1;
            Int32 MyVault_file_id = -1;
            DateTime Operation_time = DateTime.Now;
            Int32 Project_file_id = -1;
            Int32 Project_local_file_id = -1;

            if (sQLiteDataReader.Read())
            {
                Id = -1;
                MyVault_file_id = -1;
                Operation_time = DateTime.Now;
                Project_file_id = -1;
                Project_local_file_id = -1;

                Int32.TryParse(sQLiteDataReader["Id"].ToString(), out Id);
                Int32.TryParse(sQLiteDataReader["MyVault_file_id"].ToString(), out MyVault_file_id);
                string Operation = sQLiteDataReader["Operation"].ToString();
                bool b_Operation_time = DateTime.TryParse(sQLiteDataReader["Operation_time"].ToString(), out Operation_time);
                string Result = sQLiteDataReader["Result"].ToString();
                string User = sQLiteDataReader["User"].ToString();
                Int32.TryParse(sQLiteDataReader["Project_file_id"].ToString(), out Project_file_id);
                Int32.TryParse(sQLiteDataReader["Project_local_file_id"].ToString(), out Project_local_file_id);

                Operation operation = new Operation();
                operation.Id = Id;
                operation.MyVault_file_id = MyVault_file_id;
                operation.S_operation = Operation;
                operation.Operation_time = Operation_time;
                operation.Result = Result;
                operation.User = User;
                operation.Project_file_id = Project_file_id;
                operation.Project_local_file_id = Project_local_file_id;
                result.Add(operation);
            }
            return result;
        }


    }
}
