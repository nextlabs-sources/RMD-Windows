using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.utils
{
    public class ToolKits
    {
        public static bool IsSystemFolderPath(string path)
        {
            bool result = false;
            foreach (var item in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                bool isSucceed = Enum.TryParse(item.ToString(), out Environment.SpecialFolder value);
                if (isSucceed)
                {
                    result = path.Equals(Environment.GetFolderPath(value), StringComparison.OrdinalIgnoreCase);
                    if (result)
                    {
                        break;
                    }
                }
            }
            if (!result)
            {
                result = path.Equals(@"c:\", StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }

        public static bool IsSpecialFolderPath(string path)
        {
            bool result = false;
            if (path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nextlabs\SkyDRM", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), StringComparison.OrdinalIgnoreCase))
            {
                result = true;
            }
            return result;
        }
    }
}
