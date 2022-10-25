using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public class WinFileSystemItemHandler : IItemHandler
    {
        private readonly string m_syncRootPath;
        private bool m_checkMagicNum;
        private readonly int m_initialCount;

        private readonly ConcurrentDictionary<ulong, Item<ulong>> m_ItemCache;

        public bool CheckMaigcNum
        {
            get => m_checkMagicNum;
            set
            {
                m_checkMagicNum = value;
            }
        }

        public WinFileSystemItemHandler(string syncRootPath, bool checkMagicNum, int initialCount)
        {
            this.m_syncRootPath = syncRootPath;
            CheckMaigcNum = checkMagicNum;
            this.m_initialCount = initialCount;
            this.m_ItemCache = new ConcurrentDictionary<ulong, Item<ulong>>();
        }

        public async Task DeleteDirectory(ulong fileId, CancellationToken token = default)
        {
            if (token.IsCancellationRequested || !CheckMaigcNum)
            {
                return;
            }
            var fullPath = GetPath(m_syncRootPath, fileId);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                //Task.Run(() => OnDelete(token));
            }
        }

        //public async Task<long> OnDelete(CancellationToken token = default)
        //{

        //}

        private static string GetPath(string parent, ulong fileId)
        {
            return Path.Combine(parent, fileId.ToString());
        }
    }
}
