using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    public interface IProtectAndShare : IBase
    {
        /// <summary>
        /// Use for display and save dest path
        /// </summary>
        CurrentSelectedSavePath CurrentSelectedSavePath { get; }

        List<INxlFile> ProtectAndShareFile(List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, List<string>emails, string message);
    }
}
