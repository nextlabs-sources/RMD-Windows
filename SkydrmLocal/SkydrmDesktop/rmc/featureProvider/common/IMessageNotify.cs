using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider
{
   public interface IMessageNotify
    {
        /// <summary>
        /// Notify messages to RPM service manager
        /// </summary>
        /// <param name="target">the target of this activity (example, a new protected file name)</param>
        /// <param name="message">the message needs to be logged or notified (example, "You are not authorized to view the file.")</param>
        /// <param name="msgtype">message type: record log or popup bubble</param>
        /// <param name="operation">the operation of the activity, @see {MsgNotifyOperation}</param>
        /// <param name="result">the operation result</param>
        /// <param name="fileStatus">Used to display file icon by file status</param>
        void NotifyMsg(string target, string message, EnumMsgNotifyType msgtype, string operation, EnumMsgNotifyResult result, EnumMsgNotifyIcon fileStatus = EnumMsgNotifyIcon.Unknown);
    }
}
