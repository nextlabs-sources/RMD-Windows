using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model
{
     public enum FileAction
    {
        /// <summary>
        /// Protect a normal file
        /// </summary>
        Protect,
        /// <summary>
        /// Protect a normal file and share to person
        /// </summary>
        Share,
        /// <summary>
        /// View nxl file info
        /// </summary>
        ViewFileInfo,
        /// <summary>
        /// MyVault nxl file to share 
        /// </summary>
        UpdateRecipients,
        /// <summary>
        /// Share nxl file, Project to Project (Now only project file support)
        /// </summary>
        ReShare,
        /// <summary>
        /// Update shared with project list (Now only project file support)
        /// </summary>
        ReShareUpdate,
        /// <summary>
        /// Add nxl file to other repo
        /// </summary>
        AddFileTo,
        /// <summary>
        /// Modify nxl file rights
        /// </summary>
        ModifyRights,
        /// <summary>
        /// Upload normal file, or protect and upload file.  (Now only MyDrive support)
        /// </summary>
        UploadFile,
        /// <summary>
        /// Use the specified tag to protect the file
        /// </summary>
        SpecialProtect
    }
}
