using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database2.table.operation
{
    public class Operation
    {
        private Int32 id;
        private Int32 myVault_file_id;
        private string s_operation;
        private DateTime operation_time;
        private string result;
        private string user;
        private Int32 project_file_id;
        private Int32 project_local_file_id;

        public Operation()
        {

        }

        public int Id { get => id; set => id = value; }
        public int MyVault_file_id { get => myVault_file_id; set => myVault_file_id = value; }
        public string S_operation { get => s_operation; set => s_operation = value; }
        public DateTime Operation_time { get => operation_time; set => operation_time = value; }
        public string Result { get => result; set => result = value; }
        public string User { get => user; set => user = value; }
        public int Project_file_id { get => project_file_id; set => project_file_id = value; }
        public int Project_local_file_id { get => project_local_file_id; set => project_local_file_id = value; }
   
    }
}
