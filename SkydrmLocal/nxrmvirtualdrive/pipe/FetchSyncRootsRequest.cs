using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using VirtualDrive;

namespace nxrmvirtualdrive.pipe
{
    public class FetchSyncRootsRequest : Request
    {
        public override async Task<Response> Process()
        {
            var syncRoots = await ProviderRegister.GetSyncRoots();
            List<string> rt = new List<string>();
            if (syncRoots != null && syncRoots.Count != 0)
            {
                rt.AddRange(syncRoots.Keys);
            }
            return new FetchSyncRootsResponse(STATUS_CODE_OK, STATUS_OK, new FetchSyncRootsResponse.Results(rt));
        }
    }
}
