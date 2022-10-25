using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.application;
using Viewer.upgrade.cookie;
using Viewer.upgrade.file.basic;

namespace Viewer.upgrade.session
{
    public interface ISession
    {
        //IApplication Application { get; }
        string Id { get; }
       // string[] CmdArgs { get; }
       // UInt64 StatusCode { get; }
        Cookie Cookie { get; }
        // List<Cookie> Cookies { get; }
        // List<VTask> VTasks { get; }
       // List<System.Windows.Window> ViewerWindows { get; }
       // Task<UInt64> Create();
       // UInt64 Delete();
    }
}
