using SkydrmLocal.rmc.common.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper
{
    class FileTypeHelper
    {
        private const string NXL = ".nxl";

        // Judge the nxl file if is supported.
        public static EnumFileType GetFileTypeByExtension(string fileName)
        {

            if (string.IsNullOrEmpty(fileName))
            {
                return EnumFileType.FILE_TYPE_NOT_SUPPORT;
            }

            if (fileName.EndsWith(".nxl"))
            {
                fileName = fileName.Substring(0, fileName.Length - NXL.Length);
            }

            if (!fileName.Contains("."))
            {
                return EnumFileType.FILE_TYPE_NOT_SUPPORT;
            }

            // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
            if (!StringHelper.Replace(fileName,
                                     out fileName,
                                     StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                     RegexOptions.IgnoreCase))
            {
                // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                StringHelper.Replace(fileName,
                                    out fileName,
                                    StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase);
            }

            string ext = fileName.Substring(fileName.LastIndexOf('.'));

            // Hoops
            if (string.Equals(ext, ".hsf", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_HOOPS_3D;
            }

            // Audio & Vedio
            if (string.Equals(ext, ".mp3", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_AUDIO;
            }

            if (string.Equals(ext, ".mp4", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_VIDEO;
            }

            // Image
            if (string.Equals(ext, ".png", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".gif", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".jpg", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".bmp", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".tif", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".tiff", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".jpe", StringComparison.CurrentCultureIgnoreCase)
                )
            {
                return EnumFileType.FILE_TYPE_IMAGE;
            }

            // Note: for other office file: .docm, .xltm, .xlsm, .potm, .dotm --- preview don't support currently.

            // word
            if (string.Equals(ext, ".docx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".doc", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dot", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dotx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".rtf", StringComparison.CurrentCultureIgnoreCase)
                /* || string.Equals(ext, ".vsd", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(ext, ".vsdx", StringComparison.CurrentCultureIgnoreCase)*/)
            {
                return EnumFileType.FILE_TYPE_OFFICE;
            }

            // ppt
            if (string.Equals(ext, ".pptx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".potx", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_OFFICE;
            }

            // Excel
            if (string.Equals(ext, ".xlsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xls", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xltx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlsb", StringComparison.CurrentCultureIgnoreCase)

                )
            {
                return EnumFileType.FILE_TYPE_OFFICE;
            }

            // pdf
            if (string.Equals(ext, ".pdf", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_PDF;
            }

            // Plain text.
            if (string.Equals(ext, ".cpp", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".htm", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".html", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xml", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".json", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".h", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".js", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".java", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".err", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".m", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".swift", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".txt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".log", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".sql", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".c", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".py", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".csv", StringComparison.CurrentCultureIgnoreCase)
                )
            {
                return EnumFileType.FILE_TYPE_PLAIN_TEXT;
            }

            // Using exchange to view directly
            if ( // Common
                string.Equals(ext, ".jt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".igs", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".stp", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".stl", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".step", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".iges", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".rh", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".vsd", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".vsdx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dgn", StringComparison.CurrentCultureIgnoreCase)
                // Solid Edge
                || string.Equals(ext, ".par", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".psm", StringComparison.CurrentCultureIgnoreCase)
                // Parasolid
                || string.Equals(ext, ".x_t", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".x_b", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xmt_txt", StringComparison.CurrentCultureIgnoreCase)
                // CREO, Pro/Engineer
                || string.Equals(ext, ".prt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".neu", StringComparison.CurrentCultureIgnoreCase)
                // CATIA
                || string.Equals(ext, ".model", StringComparison.CurrentCultureIgnoreCase)
                // CATIA V6
                || string.Equals(ext, ".3dxml", StringComparison.CurrentCultureIgnoreCase)
                // CATIA V5
                || string.Equals(ext, ".catpart", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".cgr", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".catshape", StringComparison.CurrentCultureIgnoreCase)
                // Solid works
                || string.Equals(ext, ".prt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".sldprt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".sldasm", StringComparison.CurrentCultureIgnoreCase)
                // AutoCAD,Inventor,TrueView
                || string.Equals(ext, ".dwg", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dxf", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ipt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".asm", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".iam", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".catproduct", StringComparison.CurrentCultureIgnoreCase)
                )
            {
                return EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D;
            }

            if (string.Equals(ext, ".vds", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_SAP_VDS;
            }

            return EnumFileType.FILE_TYPE_NOT_SUPPORT;
        }

        public enum EnumFileType
        {
            FILE_TYPE_HOOPS_3D,
            FILE_TYPE_HPS_EXCHANGE_3D,
            FILE_TYPE_OFFICE,
            FILE_TYPE_PDF,
            FILE_TYPE_3D_PDF,
            FILE_TYPE_IMAGE,
            FILE_TYPE_PLAIN_TEXT,
            FILE_TYPE_VIDEO,
            FILE_TYPE_AUDIO,
            FILE_TYPE_NOT_SUPPORT,
            FILE_TYPE_SAP_VDS
        }
    }
}
