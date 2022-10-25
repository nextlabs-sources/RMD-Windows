using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.edit
{
    public interface IEditProcess
    {
        void Launch();

        void OpenFile(string filePath);

        int GetPid();

        void Close();

        IntPtr MainWindowHandle();

        Process GetProcess();

        void FinalReleaseComObject();

    }
}
