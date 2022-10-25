
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Viewer;
using Viewer.utils;

namespace SkydrmLocal.rmc.drive
{
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

    public class RegisterInfo
    {
        public int ProcessId { get; set; }
        public bool IsNeedRegisterApp { get; set; }
        public RegisterInfo(int processId, bool isNeedRegisterApp)
        {
            this.ProcessId = processId;
            this.IsNeedRegisterApp = isNeedRegisterApp;
        }
    }


    public class RightsManagementService 
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileW(
        string lpFileName,
        out WIN32_FIND_DATA lpFindFileData
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool FindClose(IntPtr hFindFile);

        public RightsManagementService(){ }

        public static bool AddRPMDir(Session session, string distination)
        {
            bool result = true;
            try
            {
                session.RPM_AddDir(distination);
            }
            catch (Exception e)
            {
                result = false;          
            }
            return result;
        }


        //// may throw RmSdkException
        //public static void CheckRPMDriverExist(Session session)
        //{
        //    if (!session.RPM_IsDriverExist())
        //    {
        //        throw new RmSdkException("RMP Driver does not exist, denied the following operations",
        //            RmSdkExceptionDomain.RMP_Driver,
        //            exception.ExceptionComponent.RMSDK);
        //    }
        //}

        public static System.IO.FileAttributes RemoveAttribute(System.IO.FileAttributes attributes, System.IO.FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public static bool CheckFileAttributeHasReadOnly(string path,log4net.ILog log)
        {
         
            log.Info("\t\t CheckFileAttributeIsReadOnly path :" + path + "\r\n");

            bool result = false;
            try
            {
                // Create the file if it does not exist.
                if (System.IO.File.Exists(path))
                {
                    System.IO.FileAttributes attributes = System.IO.File.GetAttributes(path);

                    if ((attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                    {
                        // Show the file.
                        attributes = RemoveAttribute(attributes, System.IO.FileAttributes.ReadOnly);
                        File.SetAttributes(path, attributes);
                        result = true;
                        log.InfoFormat("\t\t The {0} file is no longer hidden \r\n.", path);
                        Console.WriteLine("The {0} file is no longer hidden.", path);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        public static void DecryptNXLFile(User user, log4net.ILog log, string nxlfilePath, string decryptedFilePath)
        {
            log.Info("\t\t CacheRPMFileToken NXLFilePath :" + nxlfilePath + "\r\n");

            try
            {
                CheckFileAttributeHasReadOnly(nxlfilePath,log);

                user.CacheRPMFileToken(nxlfilePath);

                // System.Threading.Thread.Sleep(200);
                // log.Info("\t\t ForceCloseFile_NoThrow NXlFilePath :" + nxlfilePath + "\r\n");
                // user.ForceCloseFile_NoThrow(nxlfilePath);

                FileInfo file = new FileInfo(nxlfilePath);

                file.CopyTo(decryptedFilePath + ".nxl", false);

                WIN32_FIND_DATA pNextInfo;

                FindFirstFile(decryptedFilePath + ".nxl", out pNextInfo);

                CheckFileAttributeHasReadOnly(decryptedFilePath, log);
            }catch(Exception ex)
            {

            }
        }
    
        public static string GenerateDecryptFilePath(string RPMFolder, string NxlFilePath , bool isNeedTimestamp)
        {
            string result = string.Empty;

            string fileNameWithoutNXlExtension = Path.GetFileNameWithoutExtension(NxlFilePath);

            // Should handle the system automatically rename for the postfix, like: Allen-2018-10-22-07-40-13.txt(1)
            // Fix bug 55300
            //StringHelper.Replace(fileNameWithoutNXlExtension, out fileNameWithoutNXlExtension, StringHelper.REMOVE_SYSTEM_ATUO_RENAME_POSTFIX, RegexOptions.IgnoreCase);
            //fileNameWithoutNXlExtension = fileNameWithoutNXlExtension.Trim();

            // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
            if (!StringHelper.Replace(fileNameWithoutNXlExtension,
                                     out fileNameWithoutNXlExtension,
                                     StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                     RegexOptions.IgnoreCase))
            {
                // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                StringHelper.Replace(fileNameWithoutNXlExtension,
                                    out fileNameWithoutNXlExtension,
                                    StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase);
            }

            //StringHelper.Replace(fileNameWithoutNXlExtension,
            //              out fileNameWithoutNXlExtension,
            //              StringHelper.REMOVE_NXL_IN_FILE_NAME,
            //              RegexOptions.IgnoreCase);


            string GuidDirectory = RPMFolder + "\\" + System.Guid.NewGuid().ToString();

            Directory.CreateDirectory(GuidDirectory);

            if (isNeedTimestamp)
            {
                result = GuidDirectory + "\\" + fileNameWithoutNXlExtension;
            }
            else
            {
                //WithoutTimestamp
                string originalFileName;

                StringHelper.Replace(fileNameWithoutNXlExtension, out originalFileName, StringHelper.TIMESTAMP_PATTERN, RegexOptions.IgnoreCase);

                result = GuidDirectory + "\\" + originalFileName;

            }
            return result;
        }

        public static void RPMDeleteDirectory(Session session, log4net.ILog log, string RPMFolder, string directoryPath)
        {
            try
            {
                log.Info("\t\t Delete Directory In RPM Folder");
                if (string.IsNullOrEmpty(directoryPath))
                {
                    return;
                }

                if (null == session)
                {
                    return;
                }

                if ( string.Equals(RPMFolder, directoryPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }

                string[] allFilePath = Directory.GetFiles(directoryPath);

                foreach (string filePath in allFilePath)
                {
                    session.RPM_DeleteFile(filePath);
                }

                string[] allSubdirectory = Directory.GetDirectories(directoryPath);

                foreach (string subDirectoryPath in allSubdirectory)
                {
                    RPMDeleteDirectory(session, log, RPMFolder, subDirectoryPath);
                }
                session.RPM_DeleteFolder(directoryPath);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }

        public static void RPMDeleteFile(Session session, string filePath)
        {
            if (null == session)
            {
                return;
            }

            session.RPM_DeleteFile(filePath);
        }

        //notification RPM folder for a file copyed in
        public static bool FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData)
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
            catch(Exception ex) 
            {
                lpFindFileData = default(WIN32_FIND_DATA);
                //ignore all can catched exception
            }
            return result;
        }

        //for ensure the file decrypt compeletely
        private static bool SanityCheck(int count, int millisecondsTimeout, string tmpPath, Int64 size)
        {
            bool result = false;
            try
            {
                int current = 1;

                while (current <= count && !result)
                {
                    current++;

                    result = CheckDecryptedBySize(tmpPath, size);
                    //if (result)
                    //{
                    //    // double check
                    //    System.Threading.Thread.Sleep(millisecondsTimeout);
                    //    result = CheckDecryptedBySize(tmpPath, size);
                    //}

                    if (!result)
                    {
                        System.Threading.Thread.Sleep(millisecondsTimeout);
                    }
                }

            }
            catch(Exception ex)
            {
                //ignore all can catched exception
            }

            return result;
        }

        private static bool CheckDecryptedBySize(string TmpPath, Int64 nxlFileSize)
        {
            bool result = false;

            try
            {
                long length = new System.IO.FileInfo(TmpPath).Length;

                if ( (nxlFileSize - 16384 - 512) <= length
                    &&
                    length <= (nxlFileSize - 16384)
                   )
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
    
            }

            return result;
        }

        private bool TryOPenFile(string TmpPath)
        {
            bool result = false;
            FileStream fileStream = null;
            try
            {
                byte[] bt = new byte[8];
                fileStream = File.Open(TmpPath, FileMode.Open, FileAccess.Read);
                fileStream.Read(bt, 0, 8);
                string st = System.Text.Encoding.UTF8.GetString(bt);
                if (!String.Equals(st, "NXLFMT@", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (null != fileStream)
                {
                    fileStream.Close();
                }
            }

            return result;
        }
    }
}
