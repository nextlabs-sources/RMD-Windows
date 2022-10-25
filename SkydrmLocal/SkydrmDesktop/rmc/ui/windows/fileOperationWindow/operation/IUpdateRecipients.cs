using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    public interface IUpdateRecipients : IBase
    {
        /// <summary>
        /// Use for display and save dest path
        /// </summary>
        CurrentSelectedSavePath CurrentSelectedSavePath { get; }

        NxlFileType NxlType { get; }
        //Nxl File info
        List<FileRights> NxlRights { get; }
        WaterMarkInfo NxlAdhocWaterMark { get; }
        Expiration NxlExpiration { get; }
        bool IsOwner { get; }

        bool IsRevoked(out List<string> sharedEmail);

        bool UpdateRecipients(List<string> addEmails, List<string> removeEmails, string message);
    }
}
