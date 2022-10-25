using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    public interface IUpload : IBase
    {
        /// <summary>
        /// Use for display and save dest path
        /// </summary>
        CurrentSelectedSavePath CurrentSelectedSavePath { get; }

        /// <summary>
        /// MyDrive protect file
        /// </summary>
        /// <param name="rights"></param>
        /// <param name="waterMarkTxt"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        List<INxlFile> ProtectFile(List<FileRights> rights,
            string waterMarkTxt, Expiration expiration);

        /// <summary>
        /// MyDrive add upload file
        /// </summary>
        /// <returns></returns>
        List<INxlFile> AddUploadFile();

    }
}
