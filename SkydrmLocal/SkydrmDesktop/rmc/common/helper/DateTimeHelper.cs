using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.helper
{
    class DateTimeHelper
    {
        //2017-06-09-15-59-59
        public const int NxlFileDatetimeLength = 19;

        public const string DATE_FORMATTER = "M/d/yyyy h:mm tt";

        //This function can be dangerous, 
        //if file name is qwerttyuiopasdfghjklzxvcvbnm.doc.nxl , 
        //it can not distinguish the file has timeStamp
        //it will broke original file name
        public static string DateTimeConvert(string datename)
        {
            string Time = "";
            //2017-06-09-15-59-59
            if (datename.Length > NxlFileDatetimeLength)
            {
                string temp = datename.Substring(0, datename.LastIndexOf('.'));
                if (temp.LastIndexOf('.') > NxlFileDatetimeLength)
                {
                    temp = temp.Substring(0, temp.LastIndexOf('.'));
                    temp = temp.Substring(temp.Length - 19, 19);
                }
                else
                {
                    temp = temp.Substring(temp.Length - 19, 19);
                }
                int year = int.Parse(temp.Substring(0, 4));
                int month = int.Parse(temp.Substring(5, 2));
                int day = int.Parse(temp.Substring(8, 2));
                int hour = int.Parse(temp.Substring(11, 2));
                int minute = int.Parse(temp.Substring(14, 2));
                int second = int.Parse(temp.Substring(17, 2));
                DateTime dt = new DateTime(year, month, day, hour, minute, second);
                Time = dt.ToString(DATE_FORMATTER);
            }
            return Time;
        }

        public static long DateTimeToTimestamp(DateTime time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            return Convert.ToInt64((time - startDateTime).TotalMilliseconds);
        }
        public static string TimestampToDateTime(long time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            DateTime newTime = startDateTime.AddMilliseconds(time);
            return newTime.ToString("MMMM dd, yyyy");
        }

        //for mainWindow
        public static string TimestampToDateTime2(long time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            DateTime newTime = startDateTime.AddMilliseconds(time);
            return newTime.ToString(DATE_FORMATTER);
        }
    }
}
