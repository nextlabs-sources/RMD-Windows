using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.exception
{
    // designed for handling skydrmexception and all its subexceptions 
    public class GeneralHandler
    {
        public static void Handle(SkydrmException skydrmException, bool isNoticeUser=false)
        {
            try
            {
                // whatever exception occured, add log first
                SkydrmApp.Singleton.Log.Error(skydrmException.LogUsedMessage(), skydrmException);
                
                if (isNoticeUser)
                {
                    // add UI code to notice user
                    SkydrmApp.Singleton.ShowBalloonTip(skydrmException.DisplayMessage(), false);
                }

            }
            catch
            {
                // ignore anything
            }
        }


        public static void HandleUploadFailed(SkydrmException skydrmException, string msg)
        {
            try
            {
                // whatever exception occured, add log first
                SkydrmApp.Singleton.Log.Error(skydrmException.LogUsedMessage(), skydrmException);

                // add UI code to notice user
                SkydrmApp.Singleton.ShowBalloonTip(msg, false);
            }
            catch
            {
                // ignore anything
            }
        }

        public static void TryHandleSessionExpiration(Exception e)
        {
            if (e is RmRestApiException)
            {
                var ex = e as RmRestApiException;
                if (!SkydrmApp.Singleton.IsPopUpSessionExpirateDlg && ex.ErrorCode == 401)
                {
                    SkydrmApp.Singleton.Dispatcher.Invoke((Action)delegate
                    {
                        GeneralHandler.HandleSessionExpiration();
                    });
                }
            }
        }

        public static void HandleSessionExpiration()
        {
            SkydrmApp.Singleton.IsPopUpSessionExpirateDlg = true;

            if (CustomMessageBoxWindow.CustomMessageBoxResult.Positive == CustomMessageBoxWindow.Show(
                CultureStringInfo.RemoveFile_DlgBox_Title,
                "",
                CultureStringInfo.ApplicationFindResource("Exception_Sdk_Rest_401_Authentication_Failed"),
                CustomMessageBoxWindow.CustomMessageBoxIcon.None,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_OK,
                null,
                null,
                16,
                false
                 ))
            {
                SkydrmApp.Singleton.RequestLogout(RequestLogoutOps.ExecuteLogout);
                SkydrmApp.Singleton.IsPopUpSessionExpirateDlg = false;
            }
        }

    }
}
