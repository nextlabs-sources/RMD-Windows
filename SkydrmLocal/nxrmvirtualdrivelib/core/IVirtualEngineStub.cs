using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IVirtualEngineStub
    {
        Task<uint> CreateAsync(IFileSystemItemMetadata[] items, bool allowConflict = true);

        Task<bool> UpdateAsync(IFileSystemItemMetadata item, bool autoHydration = true);

        Task<bool> DeleteAsync();

        Task<bool> MoveToAsync(string moveToPath);
    }
}
