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
    public class DeleteFile : CfExecutor, IDeleteFile
    {
        public bool Confirmed
        {
            get => m_confirmed;
        }

        private bool m_confirmed = false;

        public DeleteFile(IEngine engine, CF_CALLBACK_INFO callbackInfo)
            : base(engine, callbackInfo)
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
            OnCancel();
        }

        private void Execute(NTStatus status)
        {
            CF_OPERATION_PARAMETERS.ACKDELETE data = new CF_OPERATION_PARAMETERS.ACKDELETE
            {
                Flags = CF_OPERATION_ACK_DELETE_FLAGS.CF_OPERATION_ACK_DELETE_FLAG_NONE,
                CompletionStatus = status
            };
            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(data);
            Execute(ref opParams, CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DELETE);
        }
    }
}
