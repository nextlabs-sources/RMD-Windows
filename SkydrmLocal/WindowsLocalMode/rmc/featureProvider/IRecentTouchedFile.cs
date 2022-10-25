using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.featureProvider
{
    public interface  IRecentTouchedFiles : INxlFileStatusChangedNotifiable
    {
        IRecentTouchedFile[] List();

        void UpdateOrInsert(EnumNxlFileStatus status, string fileName); 
    }

    public interface IRecentTouchedFile
    {

        string Status { get; }

        DateTime LastModifiedTime { get; }

        string Name { get; }
    }

}
