using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrive.pipe
{
    public class RegisterSyncRootResponse : Response
    {
        public Results results;

        public RegisterSyncRootResponse(int statusCode, string message, Results results) : base(statusCode, message)
        {
            this.results = results;
        }

        public class Results
        {
            public string syncRootId;

            public Results(string syncRootId)
            {
                this.syncRootId = syncRootId;
            }
        }
    }
}
