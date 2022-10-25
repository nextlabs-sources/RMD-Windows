using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr
{
    public enum ExternalRepoType
    {
        DROPBOX = 0,
        ONEDRIVE = 1,
        GOOGLEDRIVE = 2,
        BOX = 3,
        SHAREPOINT = 4,
        SHAREPOINT_ONLINE = 5,
        SHAREPOINT_ONPREMISE = 6,

        // Other
        LOCAL_DRIVE = 20,
        UNKNOWN 
    }
}
