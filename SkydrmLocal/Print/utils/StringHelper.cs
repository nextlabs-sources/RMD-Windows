using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Print.utils
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

        public static string REMOVE_SYSTEM_ATUO_RENAME_POSTFIX = @"\(\d{1}\)";

        public static string REMOVE_NXL_IN_FILE_NAME = @"\.nxl";

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
    }
}
