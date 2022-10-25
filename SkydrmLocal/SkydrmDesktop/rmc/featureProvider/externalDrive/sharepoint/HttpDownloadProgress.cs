using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    public struct HttpDownloadProgress
    {
        public long BytesReceived { get; set; }

        public long? TotalBytesToReceive { get; set; }
    }
}
