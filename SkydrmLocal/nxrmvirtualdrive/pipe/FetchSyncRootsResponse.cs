using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrive.pipe
{
    public class FetchSyncRootsResponse : Response
    {
        public Results results;

        public FetchSyncRootsResponse(int statusCode, string message, Results results) : base(statusCode, message)
        {
            this.results = results;
        }

        public class Results
        {
            public List<string> syncRoots;

            public Results(List<string> syncRoots)
            {
                this.syncRoots = syncRoots;
            }
        }
    }
}
