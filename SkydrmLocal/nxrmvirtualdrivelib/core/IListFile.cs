using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IListFile
    {
        Task ListAsync(IFileSystemItemMetadata[] children, long childrenTotalCount, bool disableOnDemandPopulation = true, bool allowConflict = true);
    }
}
