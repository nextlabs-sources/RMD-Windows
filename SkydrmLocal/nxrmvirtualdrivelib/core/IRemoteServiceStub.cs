using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IRemoteServiceStub
    {
        Task<long> CreateAsync(CancellationToken token = default);

        Task<long> UpdateAsync(CancellationToken token = default);
    }
}
