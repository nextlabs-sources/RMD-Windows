using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IFileSystemItem
    {
        Task<IFileSystemItemMetadata> GetMetadataAsync();

        Task DeleteAsync(IDeleteFile confirmation, CancellationToken token = default);

        Task<bool> DeleteCompletionAsync(CancellationToken token = default);

        Task<bool> MoveToAsync(string destPath, byte[] remoteStorageFolderId, CancellationToken token = default);

        Task<bool> MoveToCompletionAsync(string destPath, byte[] remoteStorageFolderId, CancellationToken token = default);
    }
}
