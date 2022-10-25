using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VirtualDrive;

namespace nxrmvirtualdrive.pipe
{
    public class UnRegisterSyncRootRequest : Request
    {
        public Parameters parameters;

        public override async Task<Response> Process()
        {
            if (parameters == null)
            {
                return new Response(STATUS_CODE_BAD_REQUEST, STATUS_MALFORMED_REQUEST);
            }
            var displayName = parameters.displayName;

            await ((VirtualDriveApp)Application.Current).StopEngineAsync(displayName);

            if (await ProviderRegister.UnRegisterByNameAsync(displayName))
            {
                return new Response(STATUS_CODE_OK, STATUS_OK);
            }

            return new Response(STATUS_CODE_BAD_REQUEST, STATUS_COMMON);
        }

        public class Parameters
        {
            public string displayName;
        }
    }
}
