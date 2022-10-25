using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.impl
{
    public class TransferData : CfExecutor, ITransferData
    {
        private CF_CALLBACK_PARAMETERS CallbackParams;

        public TransferData(IEngine engine, CF_CALLBACK_INFO callbackInfo,
            CF_CALLBACK_PARAMETERS callbackParameters) : base(engine, callbackInfo)
        {
            this.CallbackParams = callbackParameters;
        }

        public void Transfer(byte[] buffer, long offset, long length)
        {
            IntPtr bufferPtr = IntPtr.Zero;
            try
            {
                bufferPtr = buffer.AllocHGlobal();
                CF_OPERATION_PARAMETERS.TRANSFERDATA data = new CF_OPERATION_PARAMETERS.TRANSFERDATA
                {
                    Buffer = bufferPtr,
                    Offset = offset,
                    Length = length,
                    CompletionStatus = NTStatus.STATUS_SUCCESS
                };
                Execute(data);
            }
            finally
            {
                bufferPtr.FreeHGlobal();
            }
        }

        public override void ReportStatus(string message, uint code)
        {
            IntPtr intPtr = IntPtr.Zero;
            try
            {
                byte[] array = new byte[4096];
                intPtr = array.AllocHGlobal();

                CF_OPERATION_PARAMETERS.TRANSFERDATA data = new CF_OPERATION_PARAMETERS.TRANSFERDATA
                {
                    Buffer = IntPtr.Zero,
                    Offset = 0L,
                    Length = array.Length,
                    CompletionStatus = NTStatus.STATUS_UNSUCCESSFUL,
                };
                CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(data);
                Execute(opParams, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA, message, code);
            }
            finally
            {
                intPtr.FreeHGlobal();
            }
        }

        private void Execute(CF_OPERATION_PARAMETERS.TRANSFERDATA data)
        {
            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(data);
            Execute(ref opParams, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA);
        }
    }
}
