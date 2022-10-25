using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.utils;

namespace Viewer.share
{
    public class Helper
    {
        public const string DATE_FORMATTER = "M/d/yyyy h:mm tt";

        public static void SdkExpiration2ValiditySpecifyModel(Expiration expiration, out IExpiry expiry, out string expireDateValue, bool isUserPreference)
        {
            expiry = new NeverExpireImpl(); ;
            expireDateValue = CultureStringInfo.ValidityWin_Never_Description2;
            switch (expiration.type)
            {
                case ExpiryType.NEVER_EXPIRE:
                    expiry = new NeverExpireImpl();
                    expireDateValue = CultureStringInfo.ValidityWin_Never_Description2;
                    break;
                case ExpiryType.RELATIVE_EXPIRE:
                    if (isUserPreference)
                    {
                        int years = (int)(expiration.Start >> 32);
                        int months = (int)expiration.Start;
                        int weeks = (int)(expiration.End >> 32);
                        int days = (int)expiration.End;
                        expiry = new RelativeImpl(years, months, weeks, days);

                        DateTime dateStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                        string dateRelativeS = dateStart.ToString("MMMM dd, yyyy");

                        if (years == 0 && months == 0 && weeks == 0 && days == 0)
                        {
                            days = 1;
                        }

                        DateTime dateEnd = dateStart.AddYears(years).AddMonths(months).AddDays(7 * weeks + days - 1).AddHours(23).AddMinutes(59).AddSeconds(59);
                        string dateRelativeE = dateEnd.ToString("MMMM dd, yyyy");

                        expireDateValue = dateRelativeS + " To " + dateRelativeE;
                    }
                    else
                    {
                        string dateRelativeS = TimestampToDateTime(expiration.Start);
                        string dateRelativeE = TimestampToDateTime(expiration.End);
                        expiry = new RelativeImpl(0, 0, 0, CountDays(Convert.ToDateTime(dateRelativeS).Ticks, Convert.ToDateTime(dateRelativeE).Ticks));

                        expireDateValue = "Until " + dateRelativeE;
                    }

                    break;
                case ExpiryType.ABSOLUTE_EXPIRE:
                    string dateAbsoluteS = TimestampToDateTime(expiration.Start);
                    string dateAbsoluteE = TimestampToDateTime(expiration.End);
                    expiry = new AbsoluteImpl(expiration.End);
                    expireDateValue = "Until " + dateAbsoluteE;
                    break;
                case ExpiryType.RANGE_EXPIRE:
                    string dateRangeS = TimestampToDateTime(expiration.Start);
                    string dateRangeE = TimestampToDateTime(expiration.End);
                    expiry = new RangeImpl(expiration.Start, expiration.End);
                    expireDateValue = dateRangeS + " To " + dateRangeE;
                    break;
            }
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

        private static int CountDays(long startMillis, long endMillis)
        {
            long elapsedTicks = endMillis - startMillis;
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
            return elapsedSpan.Days + 1;
        }

    }
}
