using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
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


    public class FileHelper
    {
        private static readonly SkydrmLocalApp App = SkydrmLocalApp.Singleton;


        public static bool Exist(string FullPath)
        {
            if (FullPath == null || FullPath.Length == 0)
            {
                return false;
            }
            return File.Exists(FullPath);
        }

        public static void Delete_NoThrow(string FullPath,bool autoDelNxlIfEpt=true)
        {
            // sanity check
            if (FullPath == null || FullPath.Length==0)
            {
                return;
            }
            if (!File.Exists(FullPath))
            {
                App.Log.Warn("File want to delete ,but not exist, " + FullPath);
                return;
            }
            try
            {
                File.Delete(FullPath);
            }
            catch(Exception e)
            {
                App.Log.Warn("File can not be deleted, " + FullPath + "\t Unexception: " + e,e);
                if (autoDelNxlIfEpt)
                {
                    DelNxlByOpenAndClose_NoThrow(FullPath);
                }
            }
            // should never reach here
        }

        // this is a workaround for RMSDK lock the file, when it locked, 
        // we can not delete it, but we open it and then close
        private static bool DelNxlByOpenAndClose_NoThrow(string FullPath)
        {
            if (FullPath == null || FullPath.Length == 0)
            {
                return false;
            }
            if (!FullPath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            try
            {
                App.Rmsdk.User.ForceCloseFile_NoThrow(FullPath);
                File.Delete(FullPath);
                return true;
            }
            catch (Exception e)
            {
                App.Log.Warn("Nxl File can not be deleted, " + FullPath + "\t Unexception: " + e, e);
            }
            return false;
        }


        public static bool RenameAsGarbage_NoThrow(string FullPath)
        {
            if (FullPath == null || FullPath.Length == 0)
            {
                return false;
            }
            try
            {
                // generate random garbage name
                var garbagepath = Path.Combine(GetParentPathWithoutTrailSlash_WorkAround(FullPath), 
                    "garbage_"+Guid.NewGuid().ToString());
                File.Move(FullPath, garbagepath);
                return true;                
            }
            catch (Exception e)
            {
                App.Log.Warn("File can not be renamed, " + FullPath+ "\t Unexception: " + e,e);
            }
            return false;
        }

        public static string GetParentPathWithoutTrailSlash_WorkAround(string path)
        {
            if(path==null)
            {
                path = "";
            }
            path=path.Replace(@"/", @"\");
            if (path.EndsWith(@"\"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            try
            {
                var idx = path.LastIndexOf(@"\");
                if(idx != -1)
                {
                    return path.Substring(0, idx);
                }else
                {
                    //not found
                    throw new NotFoundException("can not find this path's parent");
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("can not get the parent path" + path + "\t Unexception: " + e, e);
                throw;
            }
        }

        public static void CreateDir_NoThrow(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in CreateDirectory,path=" + path, e);
            }
        }

        // As folder owner in NTFS, we can remove the folder's list permission
        // isSet: true - protect, false - unprotect
        public static bool ProtectFolder(string folder,bool isSet)
        {
            bool rt = false;
            try
            {

                NTAccount curUser = (NTAccount)WindowsIdentity.GetCurrent().User.Translate(typeof(NTAccount));

                FileSystemAccessRule DenyListDir = new FileSystemAccessRule(curUser, 
                    FileSystemRights.ListDirectory, AccessControlType.Deny);


                DirectorySecurity dirSecureity = System.IO.Directory.GetAccessControl(folder);
                
                

                if (isSet)
                {
                    dirSecureity.ResetAccessRule(DenyListDir);
                }
                else
                {
                    dirSecureity.RemoveAccessRule(DenyListDir);
                }

                System.IO.Directory.SetAccessControl(folder, dirSecureity);

                return true;
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
            }
            return rt;
        }


        public static string DoAfterProtect(string plainFilePath,
                                                                string nxlProtectedFilePath,
                                                                string userSelectDestFolder,
                                                                bool isNeedDeleteSourceFile)
        {
            // if need delete plainFile, nxl file without time-stamp,
            // if not, nxl file has time-stamp
            string newNxl = String.Empty;
            if (isNeedDeleteSourceFile)
            {
                newNxl = Path.GetFileName(plainFilePath) + ".nxl";
            }
            else
            {
                newNxl = Path.GetFileName(nxlProtectedFilePath);
            }
            // path combination    
            string destPath = Path.Combine(userSelectDestFolder, newNxl);

            try
            {
                File.Copy(nxlProtectedFilePath, destPath, false);
            }
            catch (Exception e) // The file 'C:\Users\aning\Desktop\project.jpg.nxl' already exists.
            {
                Console.WriteLine(e.ToString());

                if (e.Message.EndsWith("already exists."))
                {
                    bool isCancel = false;
                    SkydrmLocalApp.Singleton.Dispatcher.Invoke(() =>
                    {
                        if (ShowReplaceDlg(destPath))
                        {
                            File.Copy(nxlProtectedFilePath, destPath, true);
                        }
                        else
                        {
                            isCancel = true;
                        }
                    });

                    // means user cancel replace into destpath.
                    if (isCancel)
                    {
                        throw e;
                    }

                }
                else
                {
                    throw e;
                }
            }

            // add workaround feature: delete nxlProtectedFilePath in sdk folder 
            try
            {
                SkydrmLocalApp.Singleton.Rmsdk.User.RemoveLocalGeneratedFiles(nxlProtectedFilePath);
            }
            catch (Exception ignore)
            {
            }


            if (isNeedDeleteSourceFile)
            {
                File.Delete(plainFilePath);
            }

            return destPath;
        }


        private static bool ShowReplaceDlg(string destPath)
        {
            string subject = "The destination folder already has a file named \"" + destPath + "\". " + CultureStringInfo.ReplaceFile_DlgBox_Subject;

            CustomMessageBoxWindow.CustomMessageBoxResult ret = CustomMessageBoxWindow.Show(
                CultureStringInfo.ReplaceFile_DlgBox_Title,
                subject,
                "",
                CustomMessageBoxWindow.CustomMessageBoxIcon.None,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_YES,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_NO
            );

            return (ret == CustomMessageBoxWindow.CustomMessageBoxResult.Positive) ? true : false;
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

      //  public static string REMOVE_SYSTEM_ATUO_RENAME_POSTFIX = @"\(\d{1}\)";

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
        public static bool Replace(string inputStr,out string outputStr, string pattern, RegexOptions regexOptions)
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

        /// <summary>
        /// Fix bug 56038, special handle nxl file name
        /// Should handle the Team center automatically rename for the postfix, like: Filename.prt-2019-01-24-07-04-28.1
        /// Change to: Filename-2019-01-24-07-04-28.prt.1
        /// </summary>
        /// <param name="inputStr">nxl file name without .nxl extension</param>
        /// <param name="outputStr">output name without .nxl extension</param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool SpecialReplace(string inputStr, out string outputStr, string pattern)
        {
            bool result = false;
            outputStr = inputStr;
            try
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    var match = Regex.Match(inputStr, pattern, RegexOptions.RightToLeft);
                    if (match.Success)
                    {
                        var value = match.Value;//-2019-01-24-07-04-28.1
                        var index = match.Index;// - index
                        string temp = inputStr.Substring(0, index);//Filename.prt
                        int tempIndex = temp.LastIndexOf(".");
                        if (tempIndex != -1)
                        {
                            string extend = temp.Substring(tempIndex + 1);//prt
                            string replace = value.Substring(0, value.LastIndexOf(".")+1) + extend + value.Substring(value.LastIndexOf("."));//-2019-01-24-07-04-28.prt.1
                            outputStr = temp.Substring(0, tempIndex) + replace;
                            result = true;
                        }
                    }
                
                }
            }
            catch (Exception ex)
            {
                SkydrmLocalApp.Singleton.Log.Error("Error in Special Replace file name:", ex);
            }
            return result;
        }
    }

    public class NxlHelper
    {
        private static readonly SkydrmLocalApp app = SkydrmLocalApp.Singleton;
        public static FileRights[]  FromRightStrings(string[] rights)
        {
            var rt = new List<FileRights>();
            foreach (var s in rights)
            {
                switch (s.ToUpper())
                {
                    case "VIEW":
                        rt.Add(FileRights.RIGHT_VIEW);
                        break;
                    case "EDIT":
                        rt.Add(FileRights.RIGHT_EDIT);
                        break;
                    case "PRINT":
                        rt.Add(FileRights.RIGHT_PRINT);
                        break;
                    case "CLIPBOARD":
                        rt.Add(FileRights.RIGHT_CLIPBOARD);
                        break;
                    case "SAVEAS":
                        //rt.Add(FileRights.RIGHT_SAVEAS);
                        // Should write "Download" rights for "Save As", but the ui should display "Save As". -- fix bug 52176
                        rt.Add(FileRights.RIGHT_DOWNLOAD); 
                        break;
                    case "DECRYPT":
                        rt.Add(FileRights.RIGHT_DECRYPT);
                        break;
                    case "SCREENCAP":
                        rt.Add(FileRights.RIGHT_SCREENCAPTURE);
                        break;
                    case "SEND":
                        rt.Add(FileRights.RIGHT_SEND);
                        break;
                    case "CLASSIFY":
                        rt.Add(FileRights.RIGHT_CLASSIFY);
                        break;
                    case "SHARE":
                        rt.Add(FileRights.RIGHT_SHARE);
                        break;
                    case "DOWNLOAD":
                        rt.Add(FileRights.RIGHT_DOWNLOAD);
                        break;
                    case "WATERMARK":
                        rt.Add(FileRights.RIGHT_WATERMARK);
                        break;
                    default:
                        break;

                }
            }
            return rt.ToArray();
        }

        public static List<string> Helper_GetRightsStr(List<FileRights> rights, bool bAddIfHasWatermark = false, bool bForceAddValidity = true)
        {
            var rt = new List<string>();
            if (rights==null || rights.Count == 0)
            {
                return rt;
            }
            foreach (FileRights f in rights)
            {
                switch (f)
                {
                    case FileRights.RIGHT_VIEW:
                        rt.Add("View");
                        break;
                    case FileRights.RIGHT_EDIT:
                        rt.Add("Edit");
                        break;
                    case FileRights.RIGHT_PRINT:
                        rt.Add("Print");
                        break;
                    case FileRights.RIGHT_CLIPBOARD:
                        rt.Add("Clipboard");
                        break;
                    case FileRights.RIGHT_SAVEAS:
                        rt.Add("SaveAs");
                        break;
                    case FileRights.RIGHT_DECRYPT:
                        rt.Add("Decrypt");
                        break;
                    case FileRights.RIGHT_SCREENCAPTURE:
                        rt.Add("ScreenCapture");
                        break;
                    case FileRights.RIGHT_SEND:
                        rt.Add("Send");
                        break;
                    case FileRights.RIGHT_CLASSIFY:
                        rt.Add("Classify");
                        break;
                    case FileRights.RIGHT_SHARE:
                        rt.Add("Share");
                        break;
                    case FileRights.RIGHT_DOWNLOAD:
                        // as PM required Windows platform must regard download as SaveAS
                        rt.Add("SaveAs");
                        break;
                }
            }


            if (bAddIfHasWatermark)
            {
                rt.Add("Watermark");
            }
            if (bForceAddValidity)
            {
                //
                // comments, by current design, requreied to add the riths "Validity" compulsorily 
                //
                rt.Add("Validity");
            }

            return rt;
        }

        // not safe, should not used , this is a work around forced by Product Requirement
        // other feature MUST NOT use it.
        public static bool PeekHasValidAdhocSection(string path)
        {
            try
            {
                using (var s = File.OpenRead(path))
                {
                    s.Seek(0x2000, System.IO.SeekOrigin.Begin);
                    byte[] buf = new byte[3] { 0, 0, 0 };
                    s.Read(buf, 0, 2);
                    if (buf[0] == 0x7B && buf[1] == 0x7D)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return true;
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

        public static bool BringWindowToTop(IntPtr hWnd, Process p)
        {
            if (hWnd== IntPtr.Zero)
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

            AttachThreadInput(GetWindowThreadProcessId(hFrgWnd, (IntPtr)0),GetCurrentThreadId(), false);

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
                Console.WriteLine("**********Result Handle:************ " + "Hex:"+Result.ToString("x")+";" + "Dec:"+Result.ToString());
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


}
