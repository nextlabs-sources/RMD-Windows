using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Print.utils
{
    class CommonUtils
    {
        private const string DOLLAR_USER = "$(User)";
        private const string DOLLAR_BREAK = "$(Break)";
        private const string DOLLAR_DATE = "$(Date)";
        private const string DOLLAR_TIME = "$(Time)";

        public static void ShowBubble(string info, int timeOut = 2000)
        {
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.BalloonTipText = info;
            ni.ShowBalloonTip(timeOut);
        }

        private static int TryGetPid(string AppId)
        {
            int pid = Int32.MaxValue;

            int count = 10;
            while (count > 0) //Loop till u get
            {
                count--;
                pid = GetProcessIdByWindowTitle(AppId);
                if (pid == Int32.MaxValue)
                {
                    Thread.Sleep(200);
                    continue;
                }
                else
                {
                    break;
                }
            }

            return pid;
        }

        /// <summary>
        /// Returns the name of that process given by that title
        /// </summary>
        /// <param name="AppId">Int32MaxValue returned if it cant be found.</param>
        /// <returns></returns>
        private static int GetProcessIdByWindowTitle(string AppId)
        {
            Process[] P_CESSES = Process.GetProcesses();
            for (int p_count = 0; p_count < P_CESSES.Length; p_count++)
            {
                if (P_CESSES[p_count].MainWindowTitle.Equals(AppId, StringComparison.CurrentCultureIgnoreCase))
                {
                    return P_CESSES[p_count].Id;
                }
            }

            return Int32.MaxValue;
        }

        public static List<RegisterInfo> GetAllWinwordeProcess()
        {
            List<RegisterInfo> registerInfos = new List<RegisterInfo>();
            Process[] allProcess = Process.GetProcesses();
            foreach (Process proc in allProcess)
            {
                if (proc.ProcessName.Equals("WINWORD", StringComparison.CurrentCultureIgnoreCase))
                {
                    string fullPath = Path.GetFullPath(proc.MainModule.FileName);
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);
                    string CompanyName = fileVersionInfo.CompanyName;
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
                }
            }
            return registerInfos;
        }

        public static bool Register(RegisterInfo registerInfo)
        {
            PrintApplication printApplication = (PrintApplication)PrintApplication.Current;
            Session Session = printApplication.Session;

            bool result = false;
            try
            {
                if (null != Session)
                {
                    Process process = Process.GetProcessById(registerInfo.ProcessId);
                    if (registerInfo.IsNeedRegisterApp)
                    {
                        string fullPath = Path.GetFullPath(process.MainModule.FileName);
                        Session.RPM_RegisterApp(fullPath);

                    }
                    // SkydrmApp.Rmsdk.SDWL_RPM_NotifyRMXStatus(true);
                    result = Session.RMP_AddTrustedProcess(process.Id);
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public static string GenerateDecryptFilePath(string RPMFolder, string NxlFilePath, bool isNeedTimestamp)
        {
            string result = string.Empty;
            // why do ToLower for file name
            // The following is a workaround for a problem in Excel.
            //
            // The problem is this:
            // "If the file is a ".xlsx" or ".xltx" or ".xlsb" file, and the ".xlsx" ".xltx" ".xlsb" filename extension in the file path being passed to
            // the preview handler contains any uppercase characters, the preview would fail."
            // Other details:
            // - The problem only affects Excel.  It doesn't affect Word or PowerPoint.
            // - The problem only affects ".xlsx" ".xltx" ".xlsb" files.  It doesn't affect .xls files.
            // - The problem happens only in preview mode.  It doesn't happen when running Excel as a full
            //   application.
            // - The problem is only related to the letter case in the extension in the file path being passed
            //   to the preview handler.  It is not related to the letter case in the extension in the actual
            //   filename on disk.
            // - The problem exists in Excel 2010, 2013, 2016 and 2019.
            //
            // The workaround is to always convert the extension in the file path to lowercase ".xlsx" ".xltx" ".xlsb" before
            string fileNameWithoutNXlExtension = Path.GetFileNameWithoutExtension(NxlFilePath).ToLower();

            // Should handle the system automatically rename for the postfix, like: Allen-2018-10-22-07-40-13.txt(1)
            // Fix bug 55300
            //StringHelper.Replace(fileNameWithoutNXlExtension, out fileNameWithoutNXlExtension, StringHelper.REMOVE_SYSTEM_ATUO_RENAME_POSTFIX, RegexOptions.IgnoreCase);
            //fileNameWithoutNXlExtension = fileNameWithoutNXlExtension.Trim();

            // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
            if (!StringHelper.Replace(fileNameWithoutNXlExtension,
                                     out fileNameWithoutNXlExtension,
                                     StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                     RegexOptions.IgnoreCase))
            {
                // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                StringHelper.Replace(fileNameWithoutNXlExtension,
                                    out fileNameWithoutNXlExtension,
                                    StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase);
            }

            string GuidDirectory = RPMFolder + "\\" + System.Guid.NewGuid().ToString();

            Directory.CreateDirectory(GuidDirectory);

            if (isNeedTimestamp)
            {

                result = GuidDirectory + "\\" + fileNameWithoutNXlExtension;
            }
            else
            {
                //WithoutTimestamp
                string originalFileName;

                StringHelper.Replace(fileNameWithoutNXlExtension, out originalFileName, StringHelper.TIMESTAMP_PATTERN, RegexOptions.IgnoreCase);

                result = GuidDirectory + "\\" + originalFileName;

            }
            return result;
        }

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

        public static void MessageBox_(string message)
        {
            MessageBox.Show(message);
        }

        private static string Replace(ReplaceDollar replaceDollar)
        {
            return replaceDollar.Replace();
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
    }
}
