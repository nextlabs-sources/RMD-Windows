using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.featureProvider.RecentTouchFile
{
    public sealed class MyRecentTouchedFiles : IRecentTouchedFiles
    {
        readonly private SkydrmLocalApp app;
        readonly private log4net.ILog log;

        public event FileStatusHandler Notification;

        public MyRecentTouchedFiles(SkydrmLocalApp app )
        {
            this.app = app;
            this.log = app.Log;         
        }
    
        public void UpdateOrInsert(EnumNxlFileStatus status, string fileName)
        {
            OnFileStatusChanged(status, fileName);
            app.DBFunctionProvider.UpdateOrInsertRecentTouchedFile(status.ToString(), fileName);
        }


        private void OnFileStatusChanged(EnumNxlFileStatus status, string fileName)
        {
            Notification?.Invoke(status, fileName);
        }

        public IRecentTouchedFile[] List()
        {
            var ps = app.DBFunctionProvider.GetRecentTouchedFile();
            if (ps == null || ps.Length == 0)
            {
                return new IRecentTouchedFile[0];
            }
            MyIRecentTouchedFile[] rt = new MyIRecentTouchedFile[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                rt[i] = new MyIRecentTouchedFile(app, ps[i]);
            }
            return rt;
        }
    }

    public sealed class MyIRecentTouchedFile : IRecentTouchedFile
    {
        readonly private SkydrmLocalApp app;
        readonly private log4net.ILog log;
        private database.table.recentTouchedFile.RecentTouchedFile raw;

        public MyIRecentTouchedFile(SkydrmLocalApp app, database.table.recentTouchedFile.RecentTouchedFile raw)
        {
            this.app = app;
            this.log = app.Log;
            this.raw = raw;
        }

        public string Status { get => raw.Status; }

        public DateTime LastModifiedTime { get => raw.Last_modified_time; }

        public string Name { get => raw.Name; }
    }
}
