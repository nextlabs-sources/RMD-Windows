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
    public class MoveToFile : CfExecutor, IMoveToFile
    {
        public bool Confirmed
        {
            get => m_confirmed;
        }

        private bool m_confirmed = false;

        public MoveToFile(IEngine engine, CF_CALLBACK_INFO callbackInfo) : base(engine, callbackInfo)
        {

        }

        public void OnConfirm()
        {
            if (!m_confirmed)
            {
                Execute(NTStatus.STATUS_SUCCESS);

                m_confirmed = true;
            }
        }

        public void OnCancel(int? errorCode = null)
        {
            if (!m_confirmed)
            {
                NTStatus status = errorCode ?? NTStatus.STATUS_IO_DEVICE_ERROR;
                Execute(status);
                m_confirmed = true;
            }
        }

        public override void ReportStatus(string message, uint code)
        {
            OnCancel(null);
        }

        private void Execute(NTStatus status)
        {
            CF_OPERATION_PARAMETERS.ACKRENAME rename = new CF_OPERATION_PARAMETERS.ACKRENAME
            {
                Flags = CF_OPERATION_ACK_RENAME_FLAGS.CF_OPERATION_ACK_RENAME_FLAG_NONE,
                CompletionStatus = status
            };
            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(rename);
            Execute(ref opParams, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_RENAME);
        }
    }
}
