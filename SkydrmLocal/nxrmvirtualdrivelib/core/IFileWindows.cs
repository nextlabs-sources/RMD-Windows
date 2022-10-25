using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IFileWindows : IVirtualFile
    {
        Task OpenCompletionAsync(CancellationToken token = default);

        Task CloseCompletionAsync(CancellationToken token = default);

        Task ValidateDataAsync(long offset, long length, bool explicitHydration, IValidateData validateData);
    }
}
