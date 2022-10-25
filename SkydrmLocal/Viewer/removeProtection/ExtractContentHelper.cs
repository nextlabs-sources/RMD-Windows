using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Viewer.extractContent
{
    public class ExtractContentHelper
    {
        public static string ExtractOriginalFileName(string fileName)
        {
            // like log-2019-01-24-07-04-28.txt
            // pattern-match "-2019-01-24-07-04-28" replaced with latest lcoal timestamp
            string pattern = @"-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}";
            // new string
            string newString = "";
            Regex r = new Regex(pattern);
            string newName = fileName;
            if (r.IsMatch(fileName))
            {
                newName = r.Replace(fileName, newString);
            }
            return newName;
        }

        public static bool ShowSaveAsDialog(out string destinationPath, string nxlFileLocalPath, ViewerWindow viewerWindow)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            string originalFileName = ExtractOriginalFileName(Path.GetFileNameWithoutExtension(nxlFileLocalPath));
            string originalExtension = Path.GetExtension(originalFileName);

            dlg.CheckFileExists = false;
            dlg.FileName = originalFileName; // Default file name
            dlg.DefaultExt = originalExtension; // .nxl Default file extension
            dlg.Filter = "Documents (*" + originalExtension + ")|*" + originalExtension;
            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog(viewerWindow);
            // Process save file dialog box results
            if (result == true)
            {
                destinationPath = dlg.FileName;
            }
            else
            {
                destinationPath = string.Empty;
            }
            return result.Value;
        }

        public static void CopyFile(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath, true);
        }
    }

    public class ExtractContentParameter
    {
        public string NxlFileLocalPath { get; set; }

        public string DestinationPath { get; set; }

    }


}
