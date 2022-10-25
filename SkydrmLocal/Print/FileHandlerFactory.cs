using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Print
{
    public interface FileHandlerFactory
    {
        FileHandler CreateInstance(string filePath);
    }

    public class FileHandlerFactoryByExtention : FileHandlerFactory
    {
        private static List<string> wordExtensions = new List<string>
        {
            ".doc",
            ".docx",
            ".dot",
            ".dotx",
            ".rtf",
            ".vsd",
            ".vsdx"
        };

        private static List<string> excelExtensions = new List<string>
        {
            ".xls",
            ".xlsx",
            ".xlt",
            ".xltx",
            ".xlsb"
        };

        private static List<string> powerpointExtensions = new List<string>
        {
            ".ppt",
            ".pptx",
            ".ppsx",
            ".potx",
        };

        private static List<string> pdfExtensions = new List<string>
        {
            ".pdf"
        };

        FileHandler FileHandlerFactory.CreateInstance(string filePath)
        {
            FileHandler result = null;
            // Check to see if it's a valid file  
            if (!IsValidFilePath(filePath))
            {
                return result;
            }

            var ext = Path.GetExtension(filePath).ToLower();

            // Convert if Word  
            if (wordExtensions.Contains(ext))
            {
                result = new OfficeFileHandler(filePath);
            }
            else if (excelExtensions.Contains(ext))
            {
                // Convert if Excel  
                result = new OfficeFileHandler(filePath);
            }
            else if (powerpointExtensions.Contains(ext))
            {
                // Convert if PowerPoint  
                result = new OfficeFileHandler(filePath);
            }
            else if (pdfExtensions.Contains(ext))
            {
                // Convert if PDF
                result = new PDFHandler(filePath);
            }

            return result;
        }

        public bool IsValidFilePath(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
                return false;

            try
            {
                return File.Exists(sourceFilePath);
            }
            catch (Exception)
            {
            }

            return false;
        }

    }
}
