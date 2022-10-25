using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.database
{
    public class Server
    {
        private Int32 id;
        private string url;
        private string tenant_id;
        private string router_url;
        private string last_access;
        private string access_count;
        private string last_logout;

        public Server(Int32 id, string url, string tenant_id, string router_url, string last_access, string access_count, string last_logout)
        {
            this.Id = id;
            this.Url = url;
            this.Tenant_id = tenant_id;
            this.Router_url = router_url;
            this.Last_access = last_access;
            this.Access_count = access_count;
            this.Last_logout = last_logout;
        }

        public Server()
        {

        }

        public int Id { get => id; set => id = value; }
        public string Url { get => url; set => url = value; }
        public string Tenant_id { get => tenant_id; set => tenant_id = value; }
        public string Router_url { get => router_url; set => router_url = value; }
        public string Last_access { get => last_access; set => last_access = value; }
        public string Access_count { get => access_count; set => access_count = value; }
        public string Last_logout { get => last_logout; set => last_logout = value; }

    }
}
