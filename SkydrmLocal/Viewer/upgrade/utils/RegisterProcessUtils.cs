using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.cookie;

namespace Viewer.upgrade.utils
{
    public class RegisterProcessUtils
    {
        public enum ProcessType
        {
            WINWORD,
            POWERPNT,
            EXCEL,
            AcroRd32,
            Unknown
        }

        public class RegisterInfo
        {
            public int ProcessId { get; set; }
            public bool IsNeedRegisterApp { get; set; }
            public RegisterInfo(int processId, bool isNeedRegisterApp)
            {
                this.ProcessId = processId;
                this.IsNeedRegisterApp = isNeedRegisterApp;
            }
        }

        private static ProcessType GetProcessTypeByFileExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext))
            {
                return ProcessType.Unknown;
            }

            if (ToolKit.WORD_EXTENSIONS.Contains(ext))
            {
                return ProcessType.WINWORD;
            }

            if (ToolKit.POWERPOINT_EXTENSIONS.Contains(ext))
            {
                return ProcessType.POWERPNT;
            }

            if (ToolKit.EXCEL_EXTENSIONS.Contains(ext))
            {
                return ProcessType.EXCEL;
            }

            if (ToolKit.PDF_EXTENSIONS.Contains(ext))
            {
                return ProcessType.AcroRd32;
            }
            return ProcessType.Unknown;
        }

        public static List<RegisterInfo> GetNeedRegisterProcess(ProcessType processType)
        {
            List<RegisterInfo> registerInfos = new List<RegisterInfo>();
            if (ProcessType.Unknown == processType)
            {
                return registerInfos;
            }
            Process[] allProcess = Process.GetProcesses();
            foreach (Process proc in allProcess)
            {
                if (proc.ProcessName.Equals(processType.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    switch (processType)
                    {
                        case ProcessType.WINWORD:
                        case ProcessType.POWERPNT:
                        case ProcessType.EXCEL:
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

                        case ProcessType.AcroRd32:
                            registerInfos.Add(new RegisterInfo(proc.Id, false));
                            break;
                    }
                }
            }
            return registerInfos;
        }

        public static void ProcessRegister(Session session, string extention)
        {
            try
            {
                List<RegisterInfo> registerInfos = GetNeedRegisterProcess(GetProcessTypeByFileExtension(extention));
                foreach (RegisterInfo registerInfo in registerInfos)
                {
                    Process process = Process.GetProcessById(registerInfo.ProcessId);
                    if (registerInfo.IsNeedRegisterApp)
                    {
                        string fullPath = Path.GetFullPath(process.MainModule.FileName);
                        session.RPM_RegisterApp(fullPath);
                    }
                    bool result = session.RMP_AddTrustedProcess(process.Id);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
