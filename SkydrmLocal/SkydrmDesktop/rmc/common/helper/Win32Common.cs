using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.helper
{
    public class Win32Common
    {
        // Win shell requried, if you want more ditinct processes looked at one group, 
        // set a same appID string for each one
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        #region forbid window close button
        public const int GWL_STYLE = -16;
        public const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        #endregion

        #region User32.dll functions

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        #endregion

        #region Kernel32.dll functions
        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern uint GetLastError();
        #endregion

        public static bool BringWindowToTop(IntPtr hWnd, Process p)
        {
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            if (null == p)
            {
                return false;
            }

            IntPtr hFrgWnd = GetForegroundWindow();

            Console.WriteLine("******GetForegroundWindow:***** " + hFrgWnd.ToString("x"));
            Console.WriteLine("******GetWindowThreadProcessId:***** " + GetWindowThreadProcessId(hFrgWnd, (IntPtr)0));
            Console.WriteLine("******GetCurrentThreadId():***** " + GetCurrentThreadId());

            AttachThreadInput(GetWindowThreadProcessId(hFrgWnd, (IntPtr)0), GetCurrentThreadId(), true);

            SetForegroundWindow(hWnd);
            BringWindowToTop(hWnd);

            if (!BringWindowToTop(hWnd))
            {
                Console.WriteLine("BringWindowToTop Error %d/n", GetLastError());
            }
            else
            {
                Console.WriteLine("BringWindowToTop OK/n");
            }
            if (SetForegroundWindow(hWnd))
            {
                Console.WriteLine("SetForegroundWindow Error %d/n", GetLastError());
            }
            else
            {
                Console.WriteLine("SetForegroundWindow OK/n");
            }

            SwitchToThisWindow(hWnd, true);

            AttachThreadInput(GetWindowThreadProcessId(hFrgWnd, (IntPtr)0), GetCurrentThreadId(), false);

            return true;
        }

        public class WindowHandleInfo
        {
            public struct WindowInfo
            {
                public IntPtr hWnd;
                public string szWinName;
                public string szClassName;
            }

            [DllImport("shell32.dll")]
            public static extern int ShellExecute(IntPtr hwnd, StringBuilder lpszOp, StringBuilder lpszFile, StringBuilder lpszParams, StringBuilder lpszDir, int FsShowCmd);
            [DllImport("user32.dll")]
            private static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, int lParam);
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, string lparam);
            [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
            private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [DllImport("user32.dll")]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
            [DllImport("user32.dll")]
            private static extern int GetWindowTextW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder lpString, int nMaxCount);
            [DllImport("user32.dll")]
            private static extern int GetClassNameW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder lpString, int nMaxCount);

            private delegate bool WNDENUMPROC(IntPtr hWnd, int lParam);

            public IntPtr Result;

            public WindowHandleInfo(string className, string windowName)
            {
                WindowInfo[] winArray = GetAllSystemWindows();
                int i = 0;
                int index = 0;
                for (i = 0; i < winArray.Length; ++i)
                {
                    if (winArray[i].szClassName.ToString().Contains(className)
                        && winArray[i].szWinName.ToString().Contains(windowName))
                    {
                        Console.WriteLine("**********Get Window Handle:************  " + winArray[i].szWinName.ToString());
                        index = i;
                    }

                }
                Result = winArray[index].hWnd;
                Console.WriteLine("**********Result Handle:************ " + "Hex:" + Result.ToString("x") + ";" + "Dec:" + Result.ToString());
            }

            // Gets all Windows of the system
            static WindowInfo[] GetAllSystemWindows()
            {
                List<WindowInfo> wndList = new List<WindowInfo>();
                EnumWindows(delegate (IntPtr hWnd, int lParam)
                {
                    WindowInfo wnd = new WindowInfo();
                    StringBuilder sb = new StringBuilder(256);
                    //get hwnd
                    wnd.hWnd = hWnd;
                    //get window name
                    GetWindowTextW(hWnd, sb, sb.Capacity);
                    wnd.szWinName = sb.ToString();
                    //get window class
                    GetClassNameW(hWnd, sb, sb.Capacity);
                    wnd.szClassName = sb.ToString();
                    Console.WriteLine("Window handle=" + wnd.hWnd.ToString().PadRight(20) + " szClassName=" + wnd.szClassName.PadRight(20) + " szWindowName=" + wnd.szWinName);
                    //add it into list
                    wndList.Add(wnd);
                    return true;
                }, 0);
                return wndList.ToArray();
            }

        }

        public class FileStatus
        {
            [DllImport("kernel32.dll")]
            private static extern IntPtr _lopen(string lpPathName, int iReadWrite);

            [DllImport("kernel32.dll")]
            private static extern bool CloseHandle(IntPtr hObject);

            private const int OF_READWRITE = 2;

            private const int OF_SHARE_DENY_NONE = 0x40;

            private static readonly IntPtr HFILE_ERROR = new IntPtr(-1);

            public static int FileIsOpen(string fileFullName)
            {
                if (!Alphaleonis.Win32.Filesystem.File.Exists(fileFullName))
                {
                    return -1;
                }

                IntPtr handle = _lopen(fileFullName, OF_READWRITE | OF_SHARE_DENY_NONE);

                if (handle == HFILE_ERROR)
                {
                    return 1;
                }

                CloseHandle(handle);

                return 0;
            }
        }

        public class FileSizeOnDisk
        {
            public static long GetFileSizeOnDisk(string file)
            {
                Alphaleonis.Win32.Filesystem.FileInfo info = new Alphaleonis.Win32.Filesystem.FileInfo(file);
                uint dummy, sectorsPerCluster, bytesPerSector;
                int result = GetDiskFreeSpaceW(info.Directory.Root.FullName, out sectorsPerCluster, out bytesPerSector, out dummy, out dummy);
                if (result == 0)
                    return 0;
                uint clusterSize = sectorsPerCluster * bytesPerSector;
                uint hosize;
                uint losize = GetCompressedFileSizeW(file, out hosize);
                long size;
                size = (long)hosize << 32 | losize;
                return ((size + clusterSize - 1) / clusterSize) * clusterSize;
            }

            [DllImport("kernel32.dll")]
            static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
               [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

            [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
            static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
               out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
               out uint lpTotalNumberOfClusters);
        }
    }
}
