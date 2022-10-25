using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IDeleteFile
    {
        void OnConfirm();

        void OnCancel(int? errorCode = null);
    }
}
