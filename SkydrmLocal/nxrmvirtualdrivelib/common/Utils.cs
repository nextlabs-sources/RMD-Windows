using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.common
{
    public class Utils
    {
        public static string PtrToStringAnsi(IntPtr ptr, uint length)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }
            return Marshal.PtrToStringAnsi(ptr, (int)length);
        }

        public static string PtrToStringUni(IntPtr ptr, uint length)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }
            return Marshal.PtrToStringUni(ptr, (int)length);
        }

        public static string PtrToStringAuto(IntPtr ptr, uint length)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }
            return Marshal.PtrToStringAuto(ptr, (int)length);
        }

        public static string GetServerPathFromSyncRootId(string syncRooId)
        {
            var splits = Regex.Split(syncRooId, "->");
            var serverFolder = splits[0] == null ? "" : splits[0];
            return serverFolder;
        }

    }
}
