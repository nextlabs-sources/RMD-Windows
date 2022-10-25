using PdfFileAnalyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Viewer.overlay;
using Viewer.utils;
using Viewer.viewer;

namespace Viewer.render
{
    public static class RenderHelper
    {
        /// <summary>
        /// Get the file type by file extension
        /// </summary>
        public static EnumFileType GetFileTypeByExtension(string fileName, log4net.ILog log)
        {
            log.Info("\t\t GetFileTypeByExtension \r\n");
            if (string.IsNullOrEmpty(fileName))
            {
                return EnumFileType.FILE_TYPE_NOT_SUPPORT;
            }

            if (!fileName.Contains("."))
            {
                return EnumFileType.FILE_TYPE_NOT_SUPPORT;
            }

            fileName = CommonUtils.OriginalFileNamePreprocess(fileName);

            // Hoops
            string ext = fileName.Substring(fileName.LastIndexOf('.'));
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

            if (string.Equals(ext, ".pptx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".potx", StringComparison.CurrentCultureIgnoreCase))
            {     
                return EnumFileType.FILE_TYPE_OFFICE;
            }

            if (string.Equals(ext, ".xlsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xls", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xltx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlsb", StringComparison.CurrentCultureIgnoreCase)
             
                )
            {   
                return EnumFileType.FILE_TYPE_OFFICE;
            }

            if (string.Equals(ext, ".pdf", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumFileType.FILE_TYPE_PDF;
            }

            // Plain text.
            if (string.Equals(ext, ".cpp", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".htm", StringComparison.CurrentCultureIgnoreCase)
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

        public static EnumPlainTextRender DispatchPlainTextRender(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath);  
            if (string.IsNullOrEmpty(ext))
            {
                return EnumPlainTextRender.FILE_TYPE_NOT_SUPPORT;
            }

            if (string.Equals(ext, ".cpp", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xml", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".h", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".js", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".err", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".swift", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".txt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".c", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".py", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumPlainTextRender.PLAIN_TEXT_WB_RENDER;
            }
            else if (string.Equals(ext, ".htm", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(ext, ".json", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(ext, ".java", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(ext, ".m", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(ext, ".log", StringComparison.CurrentCultureIgnoreCase)
                 || string.Equals(ext, ".sql", StringComparison.CurrentCultureIgnoreCase))
            {
                return EnumPlainTextRender.PLAIN_TEXT_RTB_RENDER;
            }
            else
            {
                return EnumPlainTextRender.FILE_TYPE_NOT_SUPPORT;
            }
        }

        public static bool IsAttachOverlay(WatermarkInfo waterMark)
        {
            bool result = false;

            if (null == waterMark)
            {
                result = false;
            }
            else
            {
                if (string.IsNullOrEmpty(waterMark.Text))
                {
                    result = false;
                }
                else
                {
                    result = true;
                }
            }

            //if (NxlFileInfo.IsOwner)
            //{
            //    return false;
            //}

            //// is not owner & don't have watermark
            //if (!NxlFileInfo.Rights.Contains(viewer.model.FileRights.RIGHT_WATERMARK))
            //{
            //    return false;
            //}

            return result;
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
        FILE_TYPE_SAP_VDS,
        FILE_TYPE_ASSEMBLY
    }

    public enum EnumPlainTextRender
    {
        // Using WebBrowser render
        PLAIN_TEXT_WB_RENDER,

        // Using RichTexBox render
        PLAIN_TEXT_RTB_RENDER,

        // Not support
        FILE_TYPE_NOT_SUPPORT
    }

    public enum StatusOfView
    {
        NORMAL = 0,
        ERROR_DECRYPTFAILED = 1,
        ERROR_NOT_AUTHORIZED = 2,
        SYSTEM_INTERNAL_ERROR = 3,
        DOWNLOAD_FAILED=4,
        FILE_TYPE_NOT_SUPPORT=5,
        FILE_HAS_EXPIRED=6
    }

}
