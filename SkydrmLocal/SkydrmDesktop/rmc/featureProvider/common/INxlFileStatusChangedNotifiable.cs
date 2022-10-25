using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.featureProvider.common
{
    
    //on file status changed
    public delegate void FileStatusHandler(EnumNxlFileStatus status, string fileName);

    public interface INxlFileStatusChangedNotifiable
    {
        event FileStatusHandler Notification;
    }
}
