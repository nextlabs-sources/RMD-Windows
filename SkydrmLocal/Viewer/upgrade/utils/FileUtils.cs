using PdfFileAnalyzer;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.utils
{
    public class FileUtils
    {
        public static bool CheckFileAttributeHasReadOnly(string path)
        {
            bool result = false;

            // Create the file if it does not exist.
            if (File.Exists(path))
            {
                System.IO.FileAttributes attributes = File.GetAttributes(path);

                if ((attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    result = true;
                }
            }
            return result;
        }

        public static bool RemoveAttributeOfReadOnly(string path)
        {
            bool result = false;
            // Create the file if it does not exist.
            if (File.Exists(path))
            {
                System.IO.FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    //// Show the file.
                    attributes = RemoveAttribute(attributes, System.IO.FileAttributes.ReadOnly);
                    File.SetAttributes(path, attributes);
                    result = true;
                }
            }
            return result;
        }

        public static System.IO.FileAttributes RemoveAttribute(System.IO.FileAttributes attributes, System.IO.FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

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
            catch (Exception ex)
            {
                lpFindFileData = default(WIN32_FIND_DATA);
                //ignore all can catched exception
            }
            return result;
        }

        public static void CreateFolder(string dir, System.IO.FileAttributes fileAttributes)
        {
            Directory.CreateDirectory(dir);
            DirectoryInfo wd_di = new DirectoryInfo(dir);
            wd_di.Attributes |= fileAttributes;
        }

        /// <summary>
        /// Judge pdf file if contains 3D element.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool Is3DPdf(string filePath)
        {
            PdfDocument document = new PdfDocument();
            if (document.ReadPdfFile(filePath))
            {
                document = null; // read failed
                return false;
            }

            for (int row = 0; row < document.ObjectArray.Count; row++)
            {
                PdfIndirectObject obj = document.ObjectArray[row];

                string type = obj.ObjectType;
                string subType = obj.ObjectSubtype;

                if (!string.IsNullOrEmpty(type))
                {
                    if (type == "/3D" || type == "/3DNode" || type == "/3DRenderMode" || type == "/3DView")
                    {
                        return true;
                    }
                }

                if (!string.IsNullOrEmpty(subType))
                {
                    if (subType == "/U3D" || subType == "/CAD" || subType == "/3D")
                    {
                        return true;
                    }
                }

            }

            return false;
        }

        public static bool DelFileNoThrow(string FullPath)
        {
            if (FullPath == null || FullPath.Length == 0)
            {
                return false;
            }
            try
            {
                File.Delete(FullPath);
                return true;
            }
            catch (Exception e)
            {
                Console.Write("Nxl File can not be deleted, " + FullPath + "\t Unexception: " + e, e);
            }
            return false;
        }

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

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileW(
        string lpFileName,
        out WIN32_FIND_DATA lpFindFileData
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool FindClose(IntPtr hFindFile);

    }
}
