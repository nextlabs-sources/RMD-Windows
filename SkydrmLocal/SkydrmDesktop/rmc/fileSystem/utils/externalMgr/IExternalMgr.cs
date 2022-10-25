using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.fileSystem.external
{
    /// <summary>
    /// Used to handle the external nxl file, which is operated by explorer.
    /// </summary>
    public interface IExternalMgr
    {
        bool IsNxlFileCanEdit(NxlFileFingerPrint info);

        /// <summary>
        /// Open the external file and get file handle.
        /// </summary>
        IntPtr OpenFile(string filePath);

        /// <summary>
        /// Check the specified file if has been opened.
        /// </summary>
        bool IsOpen(string filePath);

        /// <summary>
        /// Close file
        /// </summary>
        void CloseFile(IntPtr handle);

    }
}
