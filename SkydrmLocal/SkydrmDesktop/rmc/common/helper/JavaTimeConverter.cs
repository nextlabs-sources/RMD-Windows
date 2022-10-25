using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.helper
{
    public class JavaTimeConverter
    {
        static readonly DateTime s1970 = new DateTime(1970, 1, 1, 0, 0, 0);

        public static DateTime ToCSDateTime(long javaTimeMills)
        {
            return new DateTime(s1970.Ticks + javaTimeMills * 10000);
        }

        public static long ToCSLongTicks(long javaTimeMills)
        {
            return ToCSDateTime(javaTimeMills).Ticks;
        }
    }
}
