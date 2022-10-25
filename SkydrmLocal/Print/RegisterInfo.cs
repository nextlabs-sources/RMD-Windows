using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Print
{
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
}
