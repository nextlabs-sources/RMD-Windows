using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IVirtualFolder : IFileSystemItem
    {
        Task<IFileSystemItemMetadata[]> GetChildrenAsync(string pattern, CancellationToken token = default);

        Task<byte[]> CreateFolderAsync(IFolderMetadata metadata, CancellationToken token = default);

        Task<byte[]> CreateFileAsync(IFileMetadata metadata, Stream content, CancellationToken token = default);

        Task WriteAsync(IFolderMetadata metadata, CancellationToken token = default);
    }
}
