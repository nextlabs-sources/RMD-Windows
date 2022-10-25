using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database.memcache.project
{
    class Utils
    {
        public static readonly Func<string, string, bool> IsDirectChild = (f, Parent) =>
        {
            // find direclt child, i.e path= /a/b
            // return /a/b/c.txt  /a/b/d/aaa/
            if (f.Length == 0)
            {
                return false;
            }
            if (f.Length <= Parent.Length)
            {
                return false;
            }
            if (!f.StartsWith(Parent, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            var idx = f.IndexOf('/', Parent.Length);

            if (idx == -1)
            {
                // a direct doc found
                return true;
            }
            if (idx == f.Length - 1)
            {
                // a direct foldr found
                return true;
            }
            return false;
        };
    }
}
