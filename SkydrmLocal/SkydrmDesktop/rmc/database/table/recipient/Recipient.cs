using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database2.table.recipient
{
    public class Recipient
    {
        private Int32 id;
        private Int32 file_id;
        private string name;
        private string email;

        public Recipient()
        {

        }

        public int Id { get => id; set => id = value; }
        public int File_id { get => file_id; set => file_id = value; }
        public string Name { get => name; set => name = value; }
        public string Email { get => email; set => email = value; }
    }
}
