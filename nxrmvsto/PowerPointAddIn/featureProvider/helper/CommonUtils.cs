using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerPointAddIn.featureProvider.helper
{
    public class CommonUtils
    {
        // Deny support to protect the file when save as ppt to these formats.
        // Fix bug 59036
        public static bool IsDenyProtectWhenSaveAsFormat(string filePath)
        {
            bool bRet = false;

            string ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
            {
                return bRet;
            }

            if ( ext.Equals(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                ext.Equals(".png", StringComparison.CurrentCultureIgnoreCase) ||
                ext.Equals(".bmp", StringComparison.CurrentCultureIgnoreCase) ||
                ext.Equals(".gif", StringComparison.CurrentCultureIgnoreCase) ||
                ext.Equals(".wmf", StringComparison.CurrentCultureIgnoreCase))
            {
                bRet = true;
            }

            return bRet;
        }

        // Don't support save as for some format, such as ppam, ppa etc.
        // Fix bug 58986
        public static bool IsNotSupportSaveAs(string filePath)
        {
            bool bRet = false;

            string ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
            {
                return bRet;
            }

            if (ext.Equals(".ppam", StringComparison.CurrentCultureIgnoreCase) ||
                ext.Equals(".ppa", StringComparison.CurrentCultureIgnoreCase))
            {
                bRet = true;
            }

            return bRet;
        }
    }
}
