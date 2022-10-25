using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public class CancellationTokenWrapper : IDisposable
    {
        private CancellationTokenPool cancellationTokenPool;
        private CancellationToken token;

        public CancellationTokenWrapper(CancellationTokenPool pool, CancellationToken token)
        {
            this.cancellationTokenPool = pool;
            this.token = token;
        }

        public void Dispose()
        {
            cancellationTokenPool.TakeOne(token);
        }
    }
}
