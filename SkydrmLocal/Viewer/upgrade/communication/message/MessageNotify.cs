using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Viewer.upgrade.communication.message
{
    /// <summary>
    /// Used to notify message to service manager to record log or popup bubble.
    /// </summary>
    public class MessageNotify
    {
        /// <summary>
        /// Easy use func, to show msg in bubble in right-lower corner of windows explorer 
        /// </summary>
        /// <param name="msg">message details</param>
        /// <param name="isPositive">is positive or negative</param>
        /// <param name="fileName">the operated file name, call be empty if not specify some one.</param>
        /// <param name="operation">the detail operation if want to specify, or else can be empty.</param>
        /// <param name="fileStatusIcon">the icon type that indicates the file status.</param>
        public static bool ShowBalloonTip(Session session, string msg, bool isPositive, string fileName = "", string operation = "",
            EnumMsgNotifyIcon fileStatusIcon = EnumMsgNotifyIcon.Unknown)
        {
            // Send log to service manager.
            if (isPositive)
            {
               return MessageNotify.NotifyMsg(session, fileName, msg, EnumMsgNotifyType.PopupBubble, operation, EnumMsgNotifyResult.Succeed, fileStatusIcon);
            }
            else
            {
               return MessageNotify.NotifyMsg(session, fileName, msg, EnumMsgNotifyType.PopupBubble, operation, EnumMsgNotifyResult.Failed, fileStatusIcon);
            }
        }

        public static bool NotifyMsg(Session session, string target, string message, EnumMsgNotifyType msgtype, string operation, EnumMsgNotifyResult result, EnumMsgNotifyIcon fileStatus)
        {
             return session.SDWL_RPM_NotifyMessage("Viewer",
                    target,
                    message,
                    Convert.ToUInt32(msgtype),
                    operation,
                    Convert.ToUInt32(result),
                    Convert.ToUInt32(fileStatus));
        }

        public static void ShowBubble(string info, int timeOut = 2000)
        {
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.BalloonTipText = info;
            ni.ShowBalloonTip(timeOut);
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
