using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmLocal.rmc.fileSystem.basemodel;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    public sealed class SharePointOnPremiseFile : SharePointBaseFile
    {
        public SharePointOnPremiseFile(NxSharePointBase host) : base(host)
        {

        }

        public SharePointOnPremiseFile(NxSharePointBase host, SharePointDriveFile item) : base(host, item)
        {

        }

    }
}
