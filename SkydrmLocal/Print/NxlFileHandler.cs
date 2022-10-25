using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace Print
{
    public class NxlFileHandler
    {
        private string mNxlFilePath;
        private PrintApplication mPrintApplication;
        private Session mSession;
        private string mDecryptedFilePath;

        public string NxlFilePath
        {
            get
            {
                return mNxlFilePath;
            }
        }

        public NxlFileHandler(string NxlFilePath)
        {
            mPrintApplication = (PrintApplication)PrintApplication.Current;
            mNxlFilePath = NxlFilePath;
            mSession = mPrintApplication.Session;
        }

        public bool Decrypt(string decryptedFilePath)
        {
            bool result = true;

            try
            {
                //  log.Info("\t\t CacheRPMFileToken NXLFilePath :" + nxlfilePath + "\r\n");

               // mSession.User.CacheRPMFileToken(mNxlFilePath);

                //  System.Threading.Thread.Sleep(200);
                //log.Info("\t\t ForceCloseFile_NoThrow NXlFilePath :" + nxlfilePath + "\r\n");
                // user.ForceCloseFile_NoThrow(nxlfilePath);

                FileInfo file = new FileInfo(mNxlFilePath);

                file.CopyTo(decryptedFilePath + ".nxl", false);

                WIN32_FIND_DATA pNextInfo;

                FindFirstFile(decryptedFilePath, out pNextInfo);

                mDecryptedFilePath = decryptedFilePath;

            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool GetFingerPrint(out NxlFileFingerPrint nxlFileFingerPrint)
        {
            bool result = true;
            nxlFileFingerPrint = new NxlFileFingerPrint();
            try
            {
                nxlFileFingerPrint = mSession.User.GetNxlFileFingerPrint(mNxlFilePath);

                if (nxlFileFingerPrint.isByCentrolPolicy)
                {
                    Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;
                    try
                    {
                        string mWatermarkStr = string.Empty;
                        mSession.User.EvaulateNxlFileRights(mNxlFilePath, out rightsAndWatermarks);
                        foreach (var v in rightsAndWatermarks)
                        {
                            List<WaterMarkInfo> waterMarkInfoList = v.Value;
                            if (waterMarkInfoList == null)
                            {
                                continue;
                            }
                            foreach (var w in waterMarkInfoList)
                            {
                                mWatermarkStr = w.text;
                                if (!string.IsNullOrEmpty(mWatermarkStr))
                                {
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(mWatermarkStr))
                            {
                                break;
                            }
                        }
                        nxlFileFingerPrint.adhocWatermark = mWatermarkStr;
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            catch (RmSdkException e)
            {
                result = false;
            }
            catch (Exception e)
            {
                result = false;
            }

            return result;
        }

        public void DeleteFile()
        {
            try
            {
                if (string.IsNullOrEmpty(mDecryptedFilePath))
                {
                    return;
                }

                if (!File.Exists(mDecryptedFilePath))
                {
                    return;
                }

                string directoryPath = Path.GetDirectoryName(mDecryptedFilePath);
                mSession.RPM_DeleteFile(mDecryptedFilePath);
                PrintApplication printApplication = (PrintApplication)PrintApplication.Current;

                if (string.Equals(printApplication.Appconfig.RPM_FolderPath, directoryPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }

                // Directory.Delete(directoryPath);
                mSession.RPM_DeleteFolder(directoryPath);
            }
            catch (Exception ex)
            {

            }
        }

        //notification RPM folder for a file copyed in
        private bool FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData)
        {
            bool result = false;
            try
            {
                IntPtr h = FindFirstFileW(lpFileName, out lpFindFileData);
                if (h.ToInt64() != -1)
                {
                    result = FindClose(h);
                }
            }
            catch (Exception ex)
            {
                lpFindFileData = default(WIN32_FIND_DATA);
                //ignore all can catched exception
            }
            return result;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileW(
        string lpFileName,
        out WIN32_FIND_DATA lpFindFileData
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool FindClose(IntPtr hFindFile);


        // The CharSet must match the CharSet of the corresponding PInvoke signature
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }
    }
}
