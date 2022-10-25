//using SkydrmLocal.rmc.sdk;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows;
//using Viewer.upgrade.database;
//using Viewer.upgrade.file.basic;
//using Viewer.upgrade.session;

//namespace Viewer.upgrade.application
//{
//    public interface IApplication 
//    {
//        Application SystemApplication { get; }
//        ConcurrentDictionary<string, ISession> Sessions { get; }
//        log4net.ILog Log { get; }
//        SkydrmLocal.rmc.sdk.Session SdkSession { get; }
//        string RmSdkFolder { get; }
//        string WorkingFolder { get; }
//        string Def_RPM_Folder { get; }
//        UInt64 StatusCode { get; }
//        string DataBaseFolder { get; }
//        FunctionProvider FunctionProvider { get; }
//        CancellationToken Token { get; }
//       // System.Windows.Window ActiveWindow { get; set; }
//       // IFile ActiveFiles { get; set; }
//        ConcurrentDictionary<string, string> Temp_RPM_Folders { get; }
//    }
//}
