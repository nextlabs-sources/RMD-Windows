using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.modifyRights
{
    public class Helper
    {
        public static bool IsHasRights(string filePath)
        {
            bool ret = false;

            try
            {
                var fp = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(filePath);
                ret = fp.hasAdminRights && fp.isByCentrolPolicy;
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error(e.ToString());
            }
            return ret;
        }
    }
}
