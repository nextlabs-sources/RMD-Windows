using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.impl
{
    public class ValidateData : CfExecutor, IValidateData
    {
        public ValidateData(IEngine engine, CF_CALLBACK_INFO callbackInfo)
            : base(engine, callbackInfo)
        {

        }

        public void Validate(long offset, long length, bool valid)
        {
            CF_OPERATION_PARAMETERS.ACKDATA data = new CF_OPERATION_PARAMETERS.ACKDATA
            {
                Flags = CF_OPERATION_ACK_DATA_FLAGS.CF_OPERATION_ACK_DATA_FLAG_NONE,
                CompletionStatus = valid ? NTStatus.STATUS_SUCCESS : NTStatus.STATUS_FILE_CORRUPT_ERROR,
                Offset = offset,
                Length = length
            };
            Execute(data);
        }

        private void Execute(CF_OPERATION_PARAMETERS.ACKDATA data)
        {
            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(data);
            Execute(ref opParams, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DATA);
        }
    }
}
