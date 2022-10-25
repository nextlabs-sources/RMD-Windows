using nxrmvirtualdrivelib.filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IEngine : IFileFilter
    {
        ILogger Logger { get; }

        bool IsThisDrivePath(string path);

        bool IsThisDriveAsRPMDir();

        bool IsRPMDir(string path);

        Task<IFileSystemItem> GetFileSystemItemAsync(string userFileSystemPath, FileSystemItemType itemType, byte[] itemId);
    }
}
