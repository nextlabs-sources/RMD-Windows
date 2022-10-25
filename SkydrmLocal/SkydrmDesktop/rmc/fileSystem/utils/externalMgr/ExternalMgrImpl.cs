using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.sdk;
using static SkydrmLocal.rmc.fileSystem.external.Helper;

namespace SkydrmLocal.rmc.fileSystem.external
{
    /// <summary>
    /// Used to handle the external nxl file.
    /// </summary>
    public class ExternalMgrImpl : IExternalMgr
    {
        public bool IsNxlFileCanEdit(NxlFileFingerPrint info)
        {    
            EnumOfficeVer ver;
            return IsOfficeFile(info.name)
                && IsOfficeInstalled(out ver) && (ver == EnumOfficeVer.Office_2016 || ver == EnumOfficeVer.Office_2013)
                && IsExistOfficeAddin(info.name)
                && info.HasRight(FileRights.RIGHT_EDIT);
        }

        public IntPtr OpenFile(string filePath)
        {
            return NxlFileHandle._lopen(filePath, NxlFileHandle.OF_READWRITE);
        }

        public void CloseFile(IntPtr handle)
        {
            NxlFileHandle.CloseHandle(handle);
        }

        public bool IsOpen(string filePath)
        {
            return NxlFileHandle.IsOpen(filePath);
        }
    }

    /// <summary>
    /// This supply operate external file with Handle.
    /// </summary>
    class NxlFileHandle
    {
        #region File access mode
        public const int OF_READ = 0;
        public const int OF_WRITE = 1;
        public const int OF_READWRITE = 2;
        #endregion // File access mode

        #region // File share mode, can refrence OpenFile
        // File can be opened many times by multiple processes.
        public const int OF_SHARE_COMPAT = 0x0;
        // Other any process can't open the file again.
        public const int OF_SHARE_EXCLUSIVE = 0x10;
        // Other processes can open and read, deny write. 
        public const int OF_SHARE_DENY_WRITE = 0x20;
        // Other processed can open and write, deny read.
        public const int OF_SHARE_DENY_READ = 0x30;
        // Can open the file, then read and write
        public const int OF_SHARE_DENY_NONE = 0x40;
        #endregion // File share mode

        public static readonly IntPtr HFile_ERROR = new IntPtr(-1);

        /// <summary>
        /// Get current thread file handle.
        /// </summary>
        /// <param name="lpPathName">File full path</param>
        /// <param name="iReadWrite">File access mode</param>
        [DllImport("kernel32.dll")]
        public static extern IntPtr _lopen(string lpPathName, int iReadWrite);


        /// <summary>
        /// Close the file handle.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Check file if has been opened
        /// </summary>
        public static bool IsOpen(string filePath)
        {
            IntPtr h = _lopen(filePath, OF_READWRITE | OF_SHARE_EXCLUSIVE);
            if (h == HFile_ERROR)
            {
                return true;
            }
            else
            {
                CloseHandle(h); // must close.
                return false;
            }
        }

    }

}
