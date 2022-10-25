using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.utils
{
    class WarterMarkConvertHelper
    {
        private const string DOLLAR_USER = "$(User)";
        private const string DOLLAR_BREAK = "$(Break)";
        private const string DOLLAR_DATE = "$(Date)";
        private const string DOLLAR_TIME = "$(Time)";

        public static void ConvertWatermark2DisplayStyle(string value, ref StringBuilder sb)
        {
            // value = " aa$(user)bb$(tmie)cc$(date)dd$(break)eee"

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            char[] array = value.ToCharArray();
            // record preset value begin index
            int beginIndex = -1;
            // record preset value end index
            int endIndex = -1;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == '$')
                {
                    beginIndex = i;
                }
                else if (array[i] == ')')
                {
                    endIndex = i;
                }

                if (beginIndex != -1 && endIndex != -1 && beginIndex < endIndex)
                {


                    sb.Append(value.Substring(0, beginIndex));


                    // judge if is preset
                    string subStr = value.Substring(beginIndex, endIndex - beginIndex + 1);

                    if (subStr.Equals(DOLLAR_USER))
                    {
                        //sb.Append(" ");
                        sb.Append(ReplaceDollar(DOLLAR_USER));
                        sb.Append(" ");
                    }
                    else if (subStr.Equals(DOLLAR_BREAK))
                    {
                        sb.Append(ReplaceDollar(DOLLAR_BREAK));
                    }
                    else if (subStr.Equals(DOLLAR_DATE))
                    {
                        //sb.Append(" ");
                        sb.Append(ReplaceDollar(DOLLAR_DATE));
                        sb.Append(" ");
                    }
                    else if (subStr.Equals(DOLLAR_TIME))
                    {
                        //sb.Append(" ");
                        sb.Append(ReplaceDollar(DOLLAR_TIME));
                        sb.Append(" ");
                    }
                    else
                    {
                        sb.Append(subStr);
                    }

                    // quit
                    break;
                }
            }

            if (beginIndex == -1 || endIndex == -1 || beginIndex > endIndex) // have not preset
            {
                sb.Append(value);

            }
            else if (beginIndex < endIndex)
            {
                if (endIndex + 1 < value.Length)
                {
                    // Converter the remaining by recursive
                    ConvertWatermark2DisplayStyle(value.Substring(endIndex + 1), ref sb);
                }
            }

        }

        private static string ReplaceDollar(string dollarStr)
        {
            string ret = "";
            switch (dollarStr)
            {
                case DOLLAR_USER:
                    ret = SkydrmDesktop.SkydrmApp.Singleton.Rmsdk.User.Email;
                    break;
                case DOLLAR_DATE:
                    ret = DateTime.Now.ToString("dd MMMM yyyy");
                    break;
                case DOLLAR_TIME:
                    ret = DateTime.Now.ToString("hh:mm");
                    break;
                case DOLLAR_BREAK:
                    ret = " ";
                    break;
                default:
                    break;
            }

            return ret;
        }
    }
}
