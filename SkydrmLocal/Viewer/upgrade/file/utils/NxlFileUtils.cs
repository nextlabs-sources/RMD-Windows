using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.application;
using Viewer.upgrade.utils;
using System.Text.RegularExpressions;
using Viewer.upgrade.file.basic.utils;

namespace Viewer.upgrade.file.utils
{
    public class NxlFileUtils
    {
        private static string ProcessFileName(string NxlFilePath)
        {
            string result = string.Empty;
            try
            {
                string tempFilePath = string.Copy(NxlFilePath);
                if (tempFilePath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
                {
                    tempFilePath = Path.GetFileNameWithoutExtension(tempFilePath);
                }

                // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
                if (!StringHelper.Replace(tempFilePath,
                                         out tempFilePath,
                                         StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                         RegexOptions.IgnoreCase))
                {
                    // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                    StringHelper.Replace(tempFilePath,
                                        out tempFilePath,
                                        StringHelper.POSTFIX_1_249,
                                        RegexOptions.IgnoreCase);
                }

                tempFilePath = tempFilePath.Trim().ToLower();
                result = tempFilePath;
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string GenerateDecryptFilePath(string RPMFolder, string NxlFilePath, string outputFileName, bool removeTimestamp)
        {
            string result = string.Empty;
            string tpOutputFileName = string.IsNullOrEmpty(outputFileName) ? ProcessFileName(NxlFilePath) : outputFileName;
            string GuidDirectory = RPMFolder + "\\" + System.Guid.NewGuid().ToString();
            Directory.CreateDirectory(GuidDirectory);
            if (removeTimestamp)
            {
                StringHelper.Replace(tpOutputFileName, out tpOutputFileName, StringHelper.TIMESTAMP_PATTERN, RegexOptions.IgnoreCase);
            }
            result = GuidDirectory + "\\" + tpOutputFileName;
            return result;
        }

        public static void CopyFile(string inputFilePath, string outputFilePath)
        {
            int bufferSize = 1024 * 1024;
            using (System.IO.FileStream fileStream = new System.IO.FileStream(outputFilePath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
            //using (FileStream fs = File.Open(<file-path>, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                System.IO.FileStream fs = new System.IO.FileStream(inputFilePath, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
                fileStream.SetLength(fs.Length);
                int bytesRead = -1;
                byte[] bytes = new byte[bufferSize];

                while ((bytesRead = fs.Read(bytes, 0, bufferSize)) > 0)
                {
                    fileStream.Write(bytes, 0, bytesRead);
                }
            }
        }

        public static string Decrypt(string nxlFilePath, string outputFileName , bool removeTimestamp)
        {
            string result = string.Empty;
            try
            {
                ViewerApp application = (ViewerApp)Application.Current;
                application.Log.InfoFormat("\t\t Decrypt nxlFilePath:{0} \r\n", nxlFilePath);

                result = GenerateDecryptFilePath(application.Def_RPM_Folder, nxlFilePath, outputFileName, removeTimestamp);

                application.Log.InfoFormat("\t\t Destination :{0} \r\n", result);

                if (FileUtils.CheckFileAttributeHasReadOnly(nxlFilePath))
                {
                    FileUtils.RemoveAttributeOfReadOnly(nxlFilePath);
                }

                System.IO.FileAttributes attributes = File.GetAttributes(nxlFilePath);

                if ((attributes & System.IO.FileAttributes.Encrypted) == System.IO.FileAttributes.Encrypted)
                {
                    CopyFile(nxlFilePath, result + ".nxl");
                }
                else
                {
                    FileInfo file = new FileInfo(nxlFilePath);
                    application.Log.InfoFormat("\t\t Copy... \r\n");
                    file.CopyTo(result + ".nxl", false);
                }

                FileUtils.WIN32_FIND_DATA pNextInfo;
                FileUtils.FindFirstFile(result + ".nxl", out pNextInfo);

                try
                {
                    System.IO.FileStream fileStream = File.Open(result, System.IO.FileMode.Open);
                    fileStream.Close();
                }
                catch (Exception ex)
                {

                }

                if (File.Exists(result))
                {
                    if (FileUtils.CheckFileAttributeHasReadOnly(result))
                    {
                        FileUtils.RemoveAttributeOfReadOnly(result);
                    }
                }
                else
                {
                    throw new System.IO.FileNotFoundException(application.FindResource("File_does_not_exist").ToString(), result);
                }

                application.Log.InfoFormat("\t\t succeed \r\n");
                return result;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string GetFileExtention(string filePath)
        {
            try
            {
                string result = string.Empty;
                string tempFilePath = ProcessFileName(filePath);
                result = Path.GetExtension(tempFilePath);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static EnumFileType GetFileTypeByExtentionEx(string filePath)
        {
            EnumFileType result = EnumFileType.UNKNOWN;
            try
            {
                string tempFilePath = ProcessFileName(filePath);
                string extention = Path.GetExtension(tempFilePath);
                result = GetFileTypeByExtention(extention);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static EnumFileType GetFileTypeByExtention(string Extention)
        {
            Extention = Extention.ToLower();
            EnumFileType fileType = EnumFileType.UNKNOWN;
            try
            {
                if (ToolKit.EMAIL_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_EMAIL;
                }

                if (ToolKit.VDS_3D_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_SAP_VDS;
                }

                if (ToolKit.EXCHANGE_3D_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D;
                }

                if (ToolKit.HSF_3D_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_HOOPS_3D;
                }

                if (ToolKit.TEXT_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_PLAIN_TEXT;
                }

                if (ToolKit.IMAGE_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_IMAGE;
                }

                if (ToolKit.AUDIO_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_AUDIO;
                }

                if (ToolKit.VIDEO_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_VIDEO;
                }

                if (ToolKit.WORD_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_OFFICE;
                }

                if (ToolKit.EXCEL_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_OFFICE;
                }

                if (ToolKit.POWERPOINT_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_OFFICE;
                }

                if (ToolKit.PDF_EXTENSIONS.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_PDF;
                }

                if (ToolKit.HYPERTEXT_MARKUP.Contains(Extention))
                {
                    fileType = EnumFileType.FILE_TYPE_HYPERTEXT_MARKUP;
                }

                if (EnumFileType.UNKNOWN == fileType)
                {
                    fileType = EnumFileType.FILE_TYPE_NOT_SUPPORT;
                }

                return fileType;
            }
            catch (Exception ex)
            {
                fileType = EnumFileType.UNKNOWN;
                throw ex;
            }
        }
    }
}
