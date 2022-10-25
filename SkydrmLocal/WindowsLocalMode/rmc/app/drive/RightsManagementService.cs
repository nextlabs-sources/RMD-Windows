using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.sdk;
using System;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


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

    public enum DecryptIntent
    {
        View,
        Print,
        Share,
        ExtractContent
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
        
        // may throw RmSdkException
        public static void CheckRPMDriverExist(Session session)
        {
            if (!session.RPM_IsDriverExist())
            {
                throw new RmSdkException("RMP Driver does not exist, denied the following operations",
                    RmSdkExceptionDomain.RMP_Driver,
                    exception.ExceptionComponent.RMSDK);
            }
        }


        public static string GenerateDecryptFilePath(string RPMFolder, string NxlFilePath, DecryptIntent decryptIntent)
        {
            string result = string.Empty;

            string filePathWithoutNXlExtension = Path.GetFileNameWithoutExtension(NxlFilePath);

            // Should handle the system automatically rename for the postfix, like: Allen-2018-10-22-07-40-13.txt(1)
            // Fix bug 55300
            //  StringHelper.Replace(filePathWithoutNXlExtension, out filePathWithoutNXlExtension, StringHelper.REMOVE_SYSTEM_ATUO_RENAME_POSTFIX, RegexOptions.IgnoreCase);
            // filePathWithoutNXlExtension = filePathWithoutNXlExtension.Trim();

            // Should handle the Team center automatically rename for the postfix, like: Jack.prt - 2019 - 01 - 24 - 07 - 04 - 28.1
            if (!StringHelper.Replace(filePathWithoutNXlExtension,
                                    out filePathWithoutNXlExtension,
                                    StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase))
            {
                // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                StringHelper.Replace(filePathWithoutNXlExtension,
                                    out filePathWithoutNXlExtension,
                                    StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase);
            }

            string GuidDirectory = RPMFolder + "\\" + System.Guid.NewGuid().ToString();

            Directory.CreateDirectory(GuidDirectory);

            switch (decryptIntent)
            {
                case DecryptIntent.View:
                    result = GuidDirectory + "\\" + "For_View_" + Path.GetExtension(filePathWithoutNXlExtension);

                    break;
                case DecryptIntent.Print:

                    result = GuidDirectory + "\\" + "For_Print_" + Path.GetExtension(filePathWithoutNXlExtension);

                    break;
                case DecryptIntent.Share:

                    string originalFileName;

                    StringHelper.Replace(filePathWithoutNXlExtension, out originalFileName, StringHelper.TIMESTAMP_PATTERN, RegexOptions.IgnoreCase);

                    result = GuidDirectory + "\\" + originalFileName;

                    break;
                case DecryptIntent.ExtractContent:

                    result = GuidDirectory + "\\" + filePathWithoutNXlExtension;

                    break;
            }

            return result;
        }

        public static System.IO.FileAttributes RemoveAttribute(System.IO.FileAttributes attributes, System.IO.FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public static bool CheckFileAttributeHasReadOnly(string path)
        {
            log4net.ILog log = SkydrmLocalApp.Singleton.Log;

            log.Info("\t\t\t\t CheckFileAttributeIsReadOnly path :" + path + "\r\n");

            bool result = false;

            // Create the file if it does not exist.
            if (System.IO.File.Exists(path))
            {
                System.IO.FileAttributes attributes = System.IO.File.GetAttributes(path);

                if ((attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    // Show the file.
                    attributes = RemoveAttribute(attributes, System.IO.FileAttributes.ReadOnly);
                    File.SetAttributes(path, attributes);
                    log.InfoFormat("\t\t\t\t The {0} file is no longer hidden \r\n.", path);
                    Console.WriteLine("The {0} file is no longer hidden.", path);
                }
            }

            return result;
        }


        // Summary:
        //    This function will create Directory with GUID name include decrypted file 

        // Parameters:
        //    skydrmLocalApp:
        //              Instance of SkydrmLocalApp 

        //    nxlfileLocalPath:
        //              nxl File full path

        //    decryptedFilePath
        //               Decrypted file with original file name and extension       

        //    isNeedTimestamp
        //           false Withiout Timestamp else has 
        public static void DecryptNXLFile (SkydrmLocalApp skydrmLocalApp, string nxlfileLocalPath, string decryptedFilePath)
        {
            // decryptedFilePath = string.Empty;

            CheckFileAttributeHasReadOnly(nxlfileLocalPath);

            skydrmLocalApp.Log.Info("\t\t\t\t CacheRPMFileToken NXLFilePath :"+ nxlfileLocalPath+"\r\n");
            skydrmLocalApp.Rmsdk.User.CacheRPMFileToken(nxlfileLocalPath);

            System.Threading.Thread.Sleep(200);

            skydrmLocalApp.Log.Info("\t\t\t\t ForceCloseFile_NoThrow NXlFilePath :" + nxlfileLocalPath + "\r\n");
            skydrmLocalApp.Rmsdk.User.ForceCloseFile_NoThrow(nxlfileLocalPath);
    
            string filePathWithoutNXlExtension = Path.GetFileNameWithoutExtension(nxlfileLocalPath);

            // Should handle the system automatically rename for the postfix, like: Allen-2018-10-22-07-40-13.txt(1)
            // Fix bug 55300
          //  StringHelper.Replace(filePathWithoutNXlExtension, out filePathWithoutNXlExtension, StringHelper.REMOVE_SYSTEM_ATUO_RENAME_POSTFIX, RegexOptions.IgnoreCase);
          //  filePathWithoutNXlExtension = filePathWithoutNXlExtension.Trim();
                
            // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
            if (!StringHelper.Replace(filePathWithoutNXlExtension,
                                     out filePathWithoutNXlExtension,
                                     StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                     RegexOptions.IgnoreCase))
            {
                // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                StringHelper.Replace(filePathWithoutNXlExtension,
                                    out filePathWithoutNXlExtension, 
                                    StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase);
            }

            FileInfo file = new FileInfo(nxlfileLocalPath);

            file.CopyTo(decryptedFilePath + ".nxl", false);

            WIN32_FIND_DATA pNextInfo;

            FindFirstFile(decryptedFilePath + ".nxl", out pNextInfo);

            SanityCheck(20, 100, decryptedFilePath + ".nxl", file.Length);

            CheckFileAttributeHasReadOnly(decryptedFilePath);
        }

        // Asynchronous function that does not block the current thread and does not wait for the end of decryption 
        public static void AsynDecryptNXLFile(SkydrmLocalApp skydrmLocalApp, string nxlfileLocalPath, string decryptedFilePath)
        {
            new Thread(new ThreadStart(()=>
            {
                CheckFileAttributeHasReadOnly(nxlfileLocalPath);

                skydrmLocalApp.Log.Info("\t\t\t\t CacheRPMFileToken NXLFilePath :" + nxlfileLocalPath + "\r\n");
                skydrmLocalApp.Rmsdk.User.CacheRPMFileToken(nxlfileLocalPath);

                System.Threading.Thread.Sleep(200);

                skydrmLocalApp.Log.Info("\t\t\t\t ForceCloseFile_NoThrow NXlFilePath :" + nxlfileLocalPath + "\r\n");
                skydrmLocalApp.Rmsdk.User.ForceCloseFile_NoThrow(nxlfileLocalPath);

                string filePathWithoutNXlExtension = Path.GetFileNameWithoutExtension(nxlfileLocalPath);

                // Should handle the system automatically rename for the postfix, like: Allen-2018-10-22-07-40-13.txt(1)
                // Fix bug 55300
              //  StringHelper.Replace(filePathWithoutNXlExtension, out filePathWithoutNXlExtension, StringHelper.REMOVE_SYSTEM_ATUO_RENAME_POSTFIX, RegexOptions.IgnoreCase);
               // filePathWithoutNXlExtension = filePathWithoutNXlExtension.Trim();

                // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
                if (!StringHelper.Replace(filePathWithoutNXlExtension,
                                         out filePathWithoutNXlExtension,
                                         StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                         RegexOptions.IgnoreCase))
                {
                    // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                    StringHelper.Replace(filePathWithoutNXlExtension,
                                        out filePathWithoutNXlExtension,
                                        StringHelper.POSTFIX_1_249,
                                        RegexOptions.IgnoreCase);
                }

                File.Copy(nxlfileLocalPath, decryptedFilePath + ".nxl", false);

                WIN32_FIND_DATA pNextInfo;

                FindFirstFile(decryptedFilePath + ".nxl", out pNextInfo);

                CheckFileAttributeHasReadOnly(decryptedFilePath);

            }))
            {
                Name = "AsynchronousDecryptNXLFile", IsBackground = true, Priority = ThreadPriority.Normal
            }.Start();
        }

        public static void RPMDeleteDirectory(SkydrmLocalApp skydrmLocalApp, string directoryPath)
        {
            SkydrmLocalApp.Singleton.Log.Info("\t\t Enter:  RPMDeleteDirectory \r\n");

            try
            {
                if (null == skydrmLocalApp)
                {
                    return;
                }

                if (string.IsNullOrEmpty(directoryPath))
                {
                    return;
                }

                Session Rmsdk = skydrmLocalApp.Rmsdk;

                if (null == Rmsdk)
                {
                    return;
                }

                if ( string.Equals(skydrmLocalApp.User.RPMFolder, directoryPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }

                string[] allFilePath = Directory.GetFiles(directoryPath);

                foreach (string filePath in allFilePath)
                {
                    SkydrmLocalApp.Singleton.Log.Info("\t\t Begin: RPM_DeleteFile \r\n");
                    Rmsdk.RPM_DeleteFile(filePath);
                    SkydrmLocalApp.Singleton.Log.Info("\t\t End: RPM_DeleteFile \r\n");
                }

                string[] allSubdirectory = Directory.GetDirectories(directoryPath);

                foreach (string subDirectoryPath in allSubdirectory)
                {
                    RPMDeleteDirectory(skydrmLocalApp, subDirectoryPath);
                }

                Directory.Delete(directoryPath);

                SkydrmLocalApp.Singleton.Log.Info("\t\t Leave:  RPMDeleteDirectory \r\n");
            }
            catch (Exception ex)
            {
                skydrmLocalApp.Log.Info(ex.Message);
            }
        }

        public static void RPMDeleteFile(SkydrmLocalApp skydrmLocalApp, string filePath)
        {
            if (null == skydrmLocalApp)
            {
                return;
            }

            Session Rmsdk = skydrmLocalApp.Rmsdk;

            if (null == Rmsdk)
            {
                return;
            }

            Rmsdk.RPM_DeleteFile(filePath);
        }

        //notification RPM folder for a file copyed in
        private static bool FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData)
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
                long length = new FileInfo(TmpPath).Length;

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

    }
}
