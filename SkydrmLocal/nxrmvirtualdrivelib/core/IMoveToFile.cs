using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IMoveToFile
    {
        void OnConfirm();

        void OnCancel(int? errorCode = null);
    }
}
