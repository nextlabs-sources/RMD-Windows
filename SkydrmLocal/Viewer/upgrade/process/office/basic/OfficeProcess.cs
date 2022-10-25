//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Viewer.upgrade.process.basic;

//namespace Viewer.upgrade.process.office.basic
//{

//    public abstract class OfficeProcess : IProcess
//    {
//        protected int ERROR_CODE_REJECTED_BY_CALLEE = System.Convert.ToInt32("0x80010001", 16);
//        public abstract event EventHandler OfficeProcessExited;
//        public abstract event Action<bool> EditSaved;

//        protected int LoopGetPid(string AppId)
//        {
//            int pid = Int32.MaxValue;

//            int count = 10;
//            while (count > 0) //Loop till u get
//            {
//                count--;
//                pid = GetProcessIdByWindowTitle(AppId);
//                if (pid == Int32.MaxValue)
//                {
//                    Thread.Sleep(500);
//                    continue;
//                }
//                else
//                {
//                    break;
//                }
//            }

//            return pid;
//        }

//        /// <summary>
//        /// Returns the id of that process given by that title
//        /// </summary>
//        /// <param name="AppId">Int32MaxValue returned if it cant be found.</param>
//        /// <returns></returns>
//        protected int GetProcessIdByWindowTitle(string AppId)
//        {
//            Process[] P_CESSES = Process.GetProcesses();
//            for (int p_count = 0; p_count < P_CESSES.Length; p_count++)
//            {
//                if (P_CESSES[p_count].MainWindowTitle.Equals(AppId, StringComparison.CurrentCultureIgnoreCase))
//                {
//                    return P_CESSES[p_count].Id;
//                }
//            }

//            return Int32.MaxValue;
//        }

//        public abstract Process Process { get; }
//       // public abstract void FinalReleaseComObject();
//        public abstract void Exit();

//    }
//}
