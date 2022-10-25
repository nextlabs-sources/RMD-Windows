using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.filter
{
    public interface IFileFilter
    {
        Task<bool> FilterAsync(string pathName, SourceFrom sourceFrom, SourceAction sourceAction, FileSystemItemType itemType, string newPath = null);
    }
}
