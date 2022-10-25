using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sdk.helper
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

        /// <summary>
        /// Fix bug 56038, special handle nxl file name
        /// Should handle the Team center automatically rename for the postfix, like: Filename.prt-2019-01-24-07-04-28.1
        /// Change to: Filename-2019-01-24-07-04-28.prt.1
        /// </summary>
        /// <param name="inputStr">nxl file name without .nxl extension</param>
        /// <param name="outputStr">output name without .nxl extension</param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool SpecialReplace(string inputStr, out string outputStr, string pattern)
        {
            bool result = false;
            outputStr = inputStr;
            try
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    var match = Regex.Match(inputStr, pattern, RegexOptions.RightToLeft);
                    if (match.Success)
                    {
                        var value = match.Value;//-2019-01-24-07-04-28.1
                        var index = match.Index;// - index
                        string temp = inputStr.Substring(0, index);//Filename.prt
                        int tempIndex = temp.LastIndexOf(".");
                        if (tempIndex != -1)
                        {
                            string extend = temp.Substring(tempIndex + 1);//prt
                            string replace = value.Substring(0, value.LastIndexOf(".") + 1) + extend + value.Substring(value.LastIndexOf("."));//-2019-01-24-07-04-28.prt.1
                            outputStr = temp.Substring(0, tempIndex) + replace;
                            result = true;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                //SkydrmApp.Singleton.Log.Error("Error in Special Replace file name:", ex);
            }
            return result;
        }

    }
}
