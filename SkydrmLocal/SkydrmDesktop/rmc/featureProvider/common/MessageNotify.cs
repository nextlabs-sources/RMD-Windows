using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.MessageNotify
{
    /// <summary>
    /// Used to notify message to service manager to record log or popup bubble.
    /// </summary>
    public class MessageNotify : IMessageNotify
    {
        private SkydrmApp app;

        public MessageNotify(SkydrmApp app)
        {
            this.app = app;
        }

        public void NotifyMsg(string target, string message, EnumMsgNotifyType msgtype, string operation, EnumMsgNotifyResult result, EnumMsgNotifyIcon fileStatus)
        {
            bool ret = app.Rmsdk.SDWL_RPM_NotifyMessage("SkyDRM", 
                target, 
                message, 
                Convert.ToUInt32(msgtype), 
                operation,
                Convert.ToUInt32(result),
                Convert.ToUInt32(fileStatus));

            if (!ret)
            {
                app.Log.Warn("Notify message failed.");
            } else
            {
                app.Log.Info("Notify message to service manager: \n" + 
                    "\t File name: " + target +
                    "\t message: " + message + 
                    "\t opration: " + operation +
                    "\t mygtype: " + msgtype.ToString() +
                    "\t result: " + result.ToString() + 
                    "\t fileStatus " + fileStatus.ToString());
            }
        }
    }

    public class MsgNotifyOperation
    {
        public static readonly string VIEW = "View";
        public static readonly string EDIT = "Edit";
        public static readonly string PROTECT = "Protect";
        public static readonly string SHARE = "Share";
        public static readonly string UPLOAD = "Upload";
        public static readonly string UPLOAD_Edit = "Upload edited file";
        public static readonly string DOWNLOAD = "Download";
        public static readonly string PRINT = "Print";
        public static readonly string EXTRACT = "Extract";
        public static readonly string SAVE_AS = "Save As";
        public static readonly string REMOVE = "Remove";
        public static readonly string MODIFY_RIGHTS = "Modify rights";
        public static readonly string MARK_OFFLINE = "Make available offline";
        public static readonly string UNMARK_OFFLINE = "Make unavailable offline";
        public static readonly string ADD_NXL_FILE = "Add nxl file";
    }

   public enum EnumMsgNotifyType
    {
        LogMsg = 0,
        PopupBubble = 1
    }

   public enum EnumMsgNotifyResult
    {
        Failed = 0,
        Succeed = 1
    }

    public enum EnumMsgNotifyIcon
    {
        Unknown = 0,
        Online = 1,
        Offline = 2,
        WaitingUpload = 3
    }
}
