using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IVirtualFile : IFileSystemItem
    {
        Task ReadAsync(Stream output, long offset, long length, CancellationToken cancellationToken = default);

        Task WriteAsync(IFileSystemItemMetadata metadata, Stream content, CancellationToken cancellationToken = default);
    }
}
