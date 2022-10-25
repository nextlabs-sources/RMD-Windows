using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.placeholder;
using nxrmvirtualdrivelib.ext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.impl
{
    public class ListFile : CfExecutor, IListFile
    {
        private readonly string userFSPath;
        private long placeholderCount;
        private long remains;

        public ListFile(IEngine engine, CF_CALLBACK_INFO callbackInfo, string userFsPath) : base(engine, callbackInfo)
        {
            this.userFSPath = userFsPath;
        }

        public async Task ListAsync(IFileSystemItemMetadata[] children, long childrenTotalCount, bool disableOnDemandPopulation = true, bool allowConflict = true)
        {
            Logger.Info(string.Format("[ListFile]: ListAsync for path {0}.", userFSPath));
            if (children == null)
            {
                children = new IFileSystemItemMetadata[0];
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            placeholderCount += children.LongLength;
            CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS flags = disableOnDemandPopulation ? CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION
                   : CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_NONE;

            CF_PLACEHOLDER_CREATE_INFO[] phCreateInfos = null;
            GCHandle[] pinnedFileIds = null;
            byte[][] fileIdDatas = null;
            try
            {
                (phCreateInfos, pinnedFileIds, fileIdDatas) = await PlaceholderFolder.Convert(true, (ulong)CallbackInfo.FileId, children.ToArray(), userFSPath, m_virtualEngine, allowConflict);
                remains += childrenTotalCount - (phCreateInfos == null ? 0 : phCreateInfos.LongLength);

                long placeholderTotalCount = childrenTotalCount - remains;
                if (placeholderCount > childrenTotalCount)
                {
                    remains = 0;
                }

                try
                {
                    TransferPlaceholder(phCreateInfos, placeholderTotalCount, flags, NTStatus.STATUS_SUCCESS, out uint entryProcessed);
                    stopwatch.Stop();

                    if (entryProcessed > 0)
                    {
                        Logger.Info(string.Format("[ListFile]: ListAsync processed with entries {0}, total {1}, consume time {2}", entryProcessed, placeholderCount, stopwatch.Elapsed.ToString()));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(string.Format("[ListFile]: ListAsync failed with error {0}.", e));
                }
            }
            finally
            {
                pinnedFileIds.Free();
            }

            if (WinFileSystemItem.TryGetAttributes(userFSPath, out var attributes)
                && ((uint)attributes.Value & (uint)FileFlagsAndAttributes.FILE_ATTRIBUTE_PINNED) != 0)
            {
                foreach (var item in phCreateInfos)
                {
                    var fullPath = Path.Combine(userFSPath, item.RelativeFileName);
                    ((VirtualEngine)m_virtualEngine).m_fswService.OnEvent(WatcherChangeTypes.Changed, fullPath);
                }
            }
        }

        public override void ReportStatus(string message, uint code)
        {
            CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS data = new CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS
            {
                Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_NONE,
                PlaceholderTotalCount = 0L,
                PlaceholderArray = IntPtr.Zero,
                PlaceholderCount = 0u,
                CompletionStatus = NTStatus.STATUS_IO_DEVICE_ERROR,
            };

            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(data);
            Execute(opParams, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS, message, code);
        }

        private void TransferPlaceholder(CF_PLACEHOLDER_CREATE_INFO[] phCreateInfo, long placeholderTotalCount, CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS flags,
            NTStatus completionStatus, out uint entryProcessed)
        {
            IntPtr hGlobal = IntPtr.Zero;
            try
            {
                if (phCreateInfo == null)
                {
                    phCreateInfo = new CF_PLACEHOLDER_CREATE_INFO[0];
                }
                hGlobal = phCreateInfo.StructuresToPtr(Marshal.AllocHGlobal);
                var placehodlerCount = (uint)phCreateInfo.Length;

                CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS data = new CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS
                {
                    Flags = flags,
                    PlaceholderTotalCount = placeholderTotalCount,
                    PlaceholderArray = hGlobal,
                    PlaceholderCount = placehodlerCount,
                    CompletionStatus = completionStatus,
                };

                CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(data);
                Execute(ref opParams, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS);
                entryProcessed = opParams.TransferPlaceholders.EntriesProcessed;
            }
            finally
            {
                hGlobal.FreeHGlobal();
            }
        }

    }
}
