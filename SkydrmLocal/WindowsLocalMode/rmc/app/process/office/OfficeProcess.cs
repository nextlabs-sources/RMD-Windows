using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SkydrmLocal.rmc.process
{
    public class OfficeProcess :IEditProcess
    {
        protected int ERROR_CODE_REJECTED_BY_CALLEE = System.Convert.ToInt32("0x80010001", 16);

        public virtual void Launch() { }

        public virtual void OpenFile(string filePath) { }

        public virtual int GetPid() { return Int32.MaxValue; }

        public virtual void FinalReleaseComObject() { }
    
        protected int TryGetPid(string AppId)
        {
            int pid = Int32.MaxValue;

            int count = 10;
            while (count > 0) //Loop till u get
            {
                count--;
                pid = GetProcessIdByWindowTitle(AppId);
                if (pid == Int32.MaxValue)
                {
                    Thread.Sleep(500);
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
        protected int GetProcessIdByWindowTitle(string AppId)
        {
            Process[] P_CESSES = Process.GetProcesses();
            for (int p_count = 0; p_count < P_CESSES.Length; p_count++)
            {
                if (P_CESSES[p_count].MainWindowTitle.Equals(AppId,StringComparison.CurrentCultureIgnoreCase))
                {
                    return P_CESSES[p_count].Id;
                }
            }

            return Int32.MaxValue;
        }

        public virtual void Close(){}

        public virtual IntPtr MainWindowHandle()
        {
            return IntPtr.Zero;
        }

        public virtual Process GetProcess()
        {
            return null;
        }

  
    }
}
