using SkydrmLocal.rmc.featureProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    /// <summary>
    /// The file that protected from local and will upload into the external drive.
    /// Support offline mode.
    /// </summary>
    public interface IExternalDriveLocalFile: IPendingUploadFile
    {
        // Reserved for extend
    }
}
