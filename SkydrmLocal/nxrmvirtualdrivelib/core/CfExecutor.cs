using nxrmvirtualdrivelib.native;
using nxrmvirtualdrivelib.ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.core
{
    public class CfExecutor : IResultContext
    {
        protected IEngine m_virtualEngine;
        protected CF_CALLBACK_INFO CallbackInfo;
        protected CORRELATION_VECTOR? CorrelationVector;
        protected CF_CONNECTION_KEY ConnectionKey;
        protected CF_TRANSFER_KEY TransferKey;

        protected ILogger Logger;

        public CfExecutor(IEngine engine, CF_CALLBACK_INFO callbackInfo)
        {
            this.m_virtualEngine = engine;
            this.CallbackInfo = callbackInfo;
            this.ConnectionKey = callbackInfo.ConnectionKey;
            this.TransferKey = callbackInfo.TransferKey;
            this.CorrelationVector = (callbackInfo.CorrelationVector == IntPtr.Zero) ? new CORRELATION_VECTOR()
                : Marshal.PtrToStructure<CORRELATION_VECTOR>(callbackInfo.CorrelationVector);

            this.Logger = engine.Logger;
        }

        public virtual void ReportProgress(long total, long completed)
        {
            WinNative.ReportProviderProgess(ConnectionKey, TransferKey, total, completed).CheckResult();
        }

        public virtual void ReportStatus(string message, uint code)
        {
            throw new NotImplementedException();
        }

        protected void Execute(ref CF_OPERATION_PARAMETERS opParams, CF_OPERATION_TYPE type)
        {
            CF_OPERATION_INFO opInfo = new CF_OPERATION_INFO
            {
                Type = type,
                ConnectionKey = ConnectionKey,
                TransferKey = TransferKey,
                StructSize = CF_OPERATION_PARAMETERS.CF_SIZE_OF_OP_PARAM<CF_OPERATION_INFO>()
            };
            CfExecute(opInfo, ref opParams).CheckResult();
        }

        protected void Execute(CF_OPERATION_PARAMETERS opParams, CF_OPERATION_TYPE type, string message, uint code)
        {
            IntPtr syncStatusPtr = IntPtr.Zero;
            IntPtr correlationVectorPtr = IntPtr.Zero;
            try
            {
                //Prepare sync status
                CF_SYNC_STATUS syncStatus = new CF_SYNC_STATUS
                {
                    Code = code,
                    StructSize = (uint)Marshal.SizeOf(typeof(CF_SYNC_STATUS))
                };
                syncStatusPtr = Marshal.AllocHGlobal(Marshal.SizeOf(syncStatus));
                Marshal.StructureToPtr(syncStatus, syncStatusPtr, false);

                // Prepare correlation vector.
                correlationVectorPtr = CorrelationVector.HasValue ? Marshal.AllocHGlobal(Marshal.SizeOf(CorrelationVector)) : IntPtr.Zero;
                if (correlationVectorPtr != IntPtr.Zero)
                {
                    Marshal.StructureToPtr(CorrelationVector.Value, correlationVectorPtr, false);
                }

                CF_OPERATION_INFO opInfo = new CF_OPERATION_INFO
                {
                    Type = type,
                    ConnectionKey = ConnectionKey,
                    TransferKey = TransferKey,
                    StructSize = CF_OPERATION_PARAMETERS.CF_SIZE_OF_OP_PARAM<CF_OPERATION_INFO>()
                };
                opInfo.SyncStatus = syncStatusPtr;
                opInfo.CorrelationVector = correlationVectorPtr;

                CfExecute(opInfo, ref opParams).CheckResult();
            }
            finally
            {
                syncStatusPtr.FreeHGlobal();
                correlationVectorPtr.FreeHGlobal();
            }
        }
    }
}
