using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WordAddIn.winApi
{
    class Win32
    {
        public enum ShowWindowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1, 
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_MAX = 10
        }

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, EntryPoint = "ShellExecuteW")]
        public static extern IntPtr ShellExecuteW(
        IntPtr hwnd,
        [MarshalAs(UnmanagedType.LPWStr)]string lpszOp,
        [MarshalAs(UnmanagedType.LPWStr)]string lpszFile,
        [MarshalAs(UnmanagedType.LPWStr)]string lpszParams,
        [MarshalAs(UnmanagedType.LPWStr)]string lpszDir,
        ShowWindowCommands FsShowCmd
        );
    }
}
