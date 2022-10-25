using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Viewer.upgrade.utils
{
    public class StringHelper
    {
        // like log-2019-01-24-07-04-28.txt
        // pattern-match "-2019-01-24-07-04-28" replaced with latest lcoal timestamp
        public static string TIMESTAMP_PATTERN = @"-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}";

        // Should handle the TeamCentral automatically rename for the postfix
        // like: Jack-2019-4-10-07-40-13.prt.x
        // x:[1-249]
        // \.               .
        // [1-9]\d{0,1}     1-99
        // 1\d{2}           100-199
        // 2[0-4]\d         200-249
        public static string POSTFIX_1_249 = @"\.(([1-9]\d{0,1})$|(1\d{2})$|(2[0-4]\d)$)";

        // \\hz-ts03\transfer\helen\windows 
        // pattern-match \\
        public static string REMOVE_SYSTEM_ATUO_RENAME_POSTFIX = @"\(\d{1}\)";

        public static bool IsValidJsonStr_Fast(string jsonStr)
        {
            if (jsonStr == null)
            {
                return false;
            }
            if (jsonStr.Length < 2)
            {
                return false;
            }

            if (!jsonStr.StartsWith("{"))
            {
                return false;
            }

            if (!jsonStr.EndsWith("}"))
            {
                return false;
            }

            return true;
        }

        public static bool IsValidBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
        }

        //if Replace return false , outputStr same with inputStr
        public static bool Replace(string inputStr, out string outputStr, string pattern, RegexOptions regexOptions)
        {
            bool result = false;
            outputStr = inputStr;
            try
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    Regex reg = new Regex(pattern, regexOptions);
                    string newString = string.Empty;
                    if (reg.IsMatch(inputStr))
                    {
                        outputStr = reg.Replace(inputStr, newString);
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        //public static string GenerateDecryptFilePath(string RPMFolder, string NxlFilePath, bool isNeedTimestamp)
        //{
        //    string result = string.Empty;
        //    // why do ToLower for file name
        //    // The following is a workaround for a problem in Excel.
        //    //
        //    // The problem is this:
        //    // "If the file is a ".xlsx" or ".xltx" or ".xlsb" file, and the ".xlsx" ".xltx" ".xlsb" filename extension in the file path being passed to
        //    // the preview handler contains any uppercase characters, the preview would fail."
        //    // Other details:
        //    // - The problem only affects Excel.  It doesn't affect Word or PowerPoint.
        //    // - The problem only affects ".xlsx" ".xltx" ".xlsb" files.  It doesn't affect .xls files.
        //    // - The problem happens only in preview mode.  It doesn't happen when running Excel as a full
        //    //   application.
        //    // - The problem is only related to the letter case in the extension in the file path being passed
        //    //   to the preview handler.  It is not related to the letter case in the extension in the actual
        //    //   filename on disk.
        //    // - The problem exists in Excel 2010, 2013, 2016 and 2019.
        //    //
        //    // The workaround is to always convert the extension in the file path to lowercase ".xlsx" ".xltx" ".xlsb" before
        //    string fileNameWithoutNXlExtension = Path.GetFileNameWithoutExtension(NxlFilePath).ToLower();

        //    // Should handle the system automatically rename for the postfix, like: Allen-2018-10-22-07-40-13.txt(1)
        //    // Fix bug 55300
        //    //StringHelper.Replace(fileNameWithoutNXlExtension, out fileNameWithoutNXlExtension, StringHelper.REMOVE_SYSTEM_ATUO_RENAME_POSTFIX, RegexOptions.IgnoreCase);
        //    //fileNameWithoutNXlExtension = fileNameWithoutNXlExtension.Trim();

        //    // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
        //    if (!Replace(fileNameWithoutNXlExtension,
        //                             out fileNameWithoutNXlExtension,
        //                             TIMESTAMP_PATTERN + POSTFIX_1_249,
        //                             RegexOptions.IgnoreCase))
        //    {
        //        // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
        //        Replace(fileNameWithoutNXlExtension,
        //                            out fileNameWithoutNXlExtension,
        //                            POSTFIX_1_249,
        //                            RegexOptions.IgnoreCase);
        //    }

        //    string GuidDirectory = RPMFolder + "\\" + System.Guid.NewGuid().ToString();

        //    Directory.CreateDirectory(GuidDirectory);

        //    if (isNeedTimestamp)
        //    {
               
        //        result = GuidDirectory + "\\" + fileNameWithoutNXlExtension;
        //    }
        //    else
        //    {
        //        //WithoutTimestamp
        //        string originalFileName;

        //        Replace(fileNameWithoutNXlExtension, out originalFileName, TIMESTAMP_PATTERN, RegexOptions.IgnoreCase);

        //        result = GuidDirectory + "\\" + originalFileName;

        //    }
        //    return result;
        //}

    }
}
