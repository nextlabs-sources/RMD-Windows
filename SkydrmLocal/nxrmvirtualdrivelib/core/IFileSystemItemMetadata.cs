using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IFileSystemItemMetadata
    {
        byte[] RemoteStorageItemId { get; set; }

        string Name { get; set; }

        FileAttributes Attributes { get; set; }

        DateTimeOffset CreationTime { get; set; }

        DateTimeOffset LastWriteTime { get; set; }

        DateTimeOffset LastAccessTime { get; set; }

        DateTimeOffset ChangeTime { get; set; }

        long Length { get; set; }

        string PathId { get; set; }
    }
}
