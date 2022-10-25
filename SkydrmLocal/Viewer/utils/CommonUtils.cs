using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Viewer.viewer;

namespace Viewer.utils
{
    public class CommonUtils
    {
        private const string DOLLAR_USER = "$(User)";
        private const string DOLLAR_BREAK = "$(Break)";
        private const string DOLLAR_DATE = "$(Date)";
        private const string DOLLAR_TIME = "$(Time)";

        // Get the all Office process id.
        //public static List<RegisterInfo> GetAllNeedRegisterProcess()
        //{
        //    List<RegisterInfo> registerInfos = new List<RegisterInfo>();
        //    Process[] allProcess = Process.GetProcesses();
        //    foreach (Process proc in allProcess)
        //    {
        //        if (proc.ProcessName.Equals("WINWORD", StringComparison.CurrentCultureIgnoreCase) ||
        //                proc.ProcessName.Equals("POWERPNT", StringComparison.CurrentCultureIgnoreCase) ||
        //                proc.ProcessName.Equals("EXCEL", StringComparison.CurrentCultureIgnoreCase) ||
        //                proc.ProcessName.Equals("AcroRd32", StringComparison.CurrentCultureIgnoreCase))
        //        {
        //            string fullPath = Path.GetFullPath(proc.MainModule.FileName);
        //            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);
        //            string CompanyName = fileVersionInfo.CompanyName;
        //            int ProductMajor = fileVersionInfo.ProductMajorPart;

        //            if (string.Equals(CompanyName, CultureStringInfo.Common_Microsoft_Corporation, StringComparison.CurrentCultureIgnoreCase))
        //            {
        //                //16 mean Office version 2016
        //                if (ProductMajor == 16)
        //                {
        //                    registerInfos.Add(new RegisterInfo(proc.Id, true));
        //                }
        //                else
        //                {
        //                    registerInfos.Add(new RegisterInfo(proc.Id, false));
        //                }
        //            }
        //            else if (proc.ProcessName.Equals("AcroRd32", StringComparison.CurrentCultureIgnoreCase))
        //            {
        //                registerInfos.Add(new RegisterInfo(proc.Id, false));
        //            }
        //        }
        //    }
        //    return registerInfos;
        //}

        public static void ProcessRegister(Session session, FileType fileType, log4net.ILog log)
        {
            try
            {
                List<RegisterInfo> registerInfos = CommonUtils.GetNeedRegisterProcess(fileType);
                foreach (RegisterInfo registerInfo in registerInfos)
                {
                    Process process = Process.GetProcessById(registerInfo.ProcessId);
                    if (registerInfo.IsNeedRegisterApp)
                    {
                        string fullPath = Path.GetFullPath(process.MainModule.FileName);
                        session.RPM_RegisterApp(fullPath);
                        log.InfoFormat("\t\t RPM_RegisterApp fullPath:{0} \r\n", fullPath);
                    }
                    bool result = session.RMP_AddTrustedProcess(process.Id);
                    log.InfoFormat("\t\t RMP_Add Trusted Process Id:{0} , result:{1} \r\n", process.Id, result);
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("\t\t Some error happend on RegisterProcess, Exception:{0} \r\n", ex);
            }
        }

        public static List<RegisterInfo> GetNeedRegisterProcess(FileType fileType)
        {
            List<RegisterInfo> registerInfos = new List<RegisterInfo>();
            Process[] allProcess = Process.GetProcesses();
            foreach (Process proc in allProcess)
            {
                if (proc.ProcessName.Equals(fileType.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    switch (fileType)
                    {
                        case FileType.WINWORD:
                        case FileType.POWERPNT:
                        case FileType.EXCEL:
                            string fullPath = Path.GetFullPath(proc.MainModule.FileName);
                            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);
                            int ProductMajor = fileVersionInfo.ProductMajorPart;
                            //16 mean Office version 2016
                            if (ProductMajor == 16)
                            {
                                registerInfos.Add(new RegisterInfo(proc.Id, true));
                            }
                            else
                            {
                                registerInfos.Add(new RegisterInfo(proc.Id, false));
                            }
                            break;

                        case FileType.AcroRd32:
                            registerInfos.Add(new RegisterInfo(proc.Id, false));
                            break;
                    }
                }
            }
            return registerInfos;
        }

        public static FileType GetFileTypeByFileExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext))
            {
                return FileType.Unknown;
            }

            if (string.Equals(ext, ".docx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".doc", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dot", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dotx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".rtf", StringComparison.CurrentCultureIgnoreCase)
                 /* || string.Equals(ext, ".vsd", StringComparison.CurrentCultureIgnoreCase)
                  || string.Equals(ext, ".vsdx", StringComparison.CurrentCultureIgnoreCase)*/)
            {
                return FileType.WINWORD;
            }

            if (string.Equals(ext, ".pptx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".potx", StringComparison.CurrentCultureIgnoreCase))
            {
                return FileType.POWERPNT;
            }

            if (string.Equals(ext, ".xlsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xls", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xltx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlsb", StringComparison.CurrentCultureIgnoreCase))
            {
                return FileType.EXCEL;
            }

            if (string.Equals(ext, ".pdf", StringComparison.CurrentCultureIgnoreCase))
            {
                return FileType.AcroRd32;
            }

            return FileType.Unknown;
        }

        //public static void RegisterProcess()
        //{
        //    try
        //    {
        //        List<RegisterInfo> registerInfos = CommonUtils.GetAllNeedRegisterProcess();
        //        foreach (RegisterInfo registerInfo in registerInfos)
        //        {
        //            NamedPipesClient.Register(registerInfo);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewerApp.Log.ErrorFormat("\t\t Some error happend on RegisterProcess, Exception:{0} \r\n", ex);
        //    }
        //}    

        public static void ConvertWatermark2DisplayStyle(string value, string userEmail, ref StringBuilder sb)
        {

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            char[] array = value.ToCharArray();
            // record preset value begin index
            int beginIndex = -1;
            // record preset value end index
            int endIndex = -1;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == '$')
                {
                    beginIndex = i;
                }
                else if (array[i] == ')')
                {
                    endIndex = i;
                }

                if (beginIndex != -1 && endIndex != -1 && beginIndex < endIndex)
                {

                    sb.Append(value.Substring(0, beginIndex));


                    // judge if is preset
                    string subStr = value.Substring(beginIndex, endIndex - beginIndex + 1);

                    if (subStr.Equals(DOLLAR_USER))
                    {
                        sb.Append(" ");
                        sb.Append(Replace(new ReplaceDollarUser(userEmail)));
                        sb.Append(" ");
                    }
                    else if (subStr.Equals(DOLLAR_BREAK))
                    {
                        sb.Append(Replace(new ReplaceDollarBreak()));
                    }
                    else if (subStr.Equals(DOLLAR_DATE))
                    {
                        sb.Append(" ");
                        sb.Append(Replace(new ReplaceDollarDate()));
                        sb.Append(" ");
                    }
                    else if (subStr.Equals(DOLLAR_TIME))
                    {
                        sb.Append(" ");
                        sb.Append(Replace(new ReplaceDollarTime()));
                        sb.Append(" ");
                    }
                    else
                    {
                        sb.Append(subStr);
                    }

                    // quit
                    break;
                }
            }

            if (beginIndex == -1 || endIndex == -1 || beginIndex > endIndex) // have not preset
            {
                sb.Append(value);

            }
            else if (beginIndex < endIndex)
            {
                if (endIndex + 1 < value.Length)
                {
                    // Converter the remaining by recursive
                    ConvertWatermark2DisplayStyle(value.Substring(endIndex + 1), userEmail, ref sb);
                }
            }
        }

        private static string Replace(ReplaceDollar replaceDollar)
        {
           return replaceDollar.Replace();
        }

        //public static void ShowBubble(string info, int timeOut = 2000)
        //{
        //    System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
        //    ni.BalloonTipText = info;
        //    ni.ShowBalloonTip(timeOut);
        //}

        /// <summary>
        /// Easy use func, to show msg in bubble in right-lower corner of windows explorer 
        /// </summary>
        /// <param name="msg">message details</param>
        /// <param name="isPositive">is positive or negative</param>
        /// <param name="fileName">the operated file name, call be empty if not specify some one.</param>
        /// <param name="operation">the detail operation if want to specify, or else can be empty.</param>
        /// <param name="fileStatusIcon">the icon type that indicates the file status.</param>
        public static void ShowBalloonTip(string msg, bool isPositive, string fileName = "", string operation = "",
            EnumMsgNotifyIcon fileStatusIcon = EnumMsgNotifyIcon.Unknown)
        {
            // Send log to service manager.
            if (isPositive)
            {
                MessageNotify.NotifyMsg(fileName, msg, EnumMsgNotifyType.PopupBubble, operation, EnumMsgNotifyResult.Succeed, fileStatusIcon);
            }
            else
            {
                MessageNotify.NotifyMsg(fileName, msg, EnumMsgNotifyType.PopupBubble, operation, EnumMsgNotifyResult.Failed, fileStatusIcon);
            }
        }

        public static long DateTimeToTimestamp(DateTime time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            return Convert.ToInt64((time - startDateTime).TotalMilliseconds);
        }

        public abstract class ReplaceDollar
        {
            public abstract string Replace();

        }

        public class ReplaceDollarUser : ReplaceDollar
        {
            private string UserEmail { get; }

            public ReplaceDollarUser(string userEmail)
            {
                this.UserEmail = userEmail;
            }
            public override string Replace()
            {
                return this.UserEmail;
            }
        }

        public class ReplaceDollarDate : ReplaceDollar
        {
            public override string Replace()
            {
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
        }

        public class ReplaceDollarTime : ReplaceDollar
        {
            public override string Replace()
            {
                return DateTime.Now.ToString("HH:mm:ss");
            }
        }

        public class ReplaceDollarBreak : ReplaceDollar
        {
            public override string Replace()
            {
                return "\n";
            }
        }

        public static bool DelFileNoThrow(string FullPath)
        {
            if (FullPath == null || FullPath.Length == 0)
            {
                return false;
            } 
            try
            {         
                File.Delete(FullPath);
                return true;
            }
            catch (Exception e)
            {
                Console.Write("Nxl File can not be deleted, " + FullPath + "\t Unexception: " + e, e);
            }
            return false;
        }

        public static string OriginalFileNamePreprocess(string OriginalFileName)
        {
            string result = OriginalFileName;
            // Should handle the system automatically rename for the postfix, like: Allen-2018-10-22-07-40-13.txt(1)
            // Fix bug 55300
            StringHelper.Replace(result, out result, StringHelper.REMOVE_SYSTEM_ATUO_RENAME_POSTFIX, RegexOptions.IgnoreCase);
            result = result.Trim();

            // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
            if (!StringHelper.Replace(result,
                                     out result,
                                     StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                     RegexOptions.IgnoreCase))
            {
                // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                StringHelper.Replace(result,
                                    out result,
                                    StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase);
            }
            return result;
        }
    }

    public class Win32Common
    {
        // Win shell requried, if you want more ditinct processes looked at one group, 
        // set a same appID string for each one
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

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

        public static bool BringWindowToTopEx(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
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

    }

    public class StringHelper
    {
        // like log-2019-01-24-07-04-28.txt
        // pattern-match "-2019-01-24-07-04-28" replaced with latest lcoal timestamp
        public static string TIMESTAMP_PATTERN = @"-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}";

        // Should handle the TeamCentral automatically rename for the postfix
        // like: Jack-2019-4-10-07-40-13.prt.x
        // x:[1-249]
        // \.               .
        // [1-9]\d{0,1}     1-99
        // 1\d{2}           100-199
        // 2[0-4]\d         200-249
        public static string POSTFIX_1_249 = @"\.(([1-9]\d{0,1})$|(1\d{2})$|(2[0-4]\d)$)";

        public static string REMOVE_SYSTEM_ATUO_RENAME_POSTFIX = @"\(\d{1}\)";

        //  public static string REMOVE_REMOTE_FOLDER_PREFIX = @"^(\\)*";

        public static string REMOVE_NXL_IN_FILE_NAME = @"\.nxl";

        public static bool IsValidJsonStr_Fast(string jsonStr)
        {
            if (jsonStr == null)
            {
                return false;
            }
            if (jsonStr.Length < 2)
            {
                return false;
            }

            if (!jsonStr.StartsWith("{"))
            {
                return false;
            }

            if (!jsonStr.EndsWith("}"))
            {
                return false;
            }

            return true;
        }

        public static bool IsValidBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
        }

        //if Replace return false , outputStr same with inputStr
        public static bool Replace(string inputStr, out string outputStr, string pattern, RegexOptions regexOptions)
        {
            bool result = false;
            outputStr = inputStr;
            try
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    Regex reg = new Regex(pattern, regexOptions);
                    string newString = string.Empty;
                    if (reg.IsMatch(inputStr))
                    {
                        outputStr = reg.Replace(inputStr, newString);
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }
    }

    public enum FileType
    {
        WINWORD,
        POWERPNT,
        EXCEL,
        AcroRd32,
        Unknown
    }
}
