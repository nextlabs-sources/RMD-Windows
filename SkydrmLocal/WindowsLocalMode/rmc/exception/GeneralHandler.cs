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
                SkydrmLocalApp.Singleton.Log.Error(skydrmException.LogUsedMessage(), skydrmException);
                
                if (isNoticeUser)
                {
                    // add UI code to notice user
                    SkydrmLocalApp.Singleton.ShowBalloonTip(skydrmException.DisplayMessage());
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
                SkydrmLocalApp.Singleton.Log.Error(skydrmException.LogUsedMessage(), skydrmException);

                // add UI code to notice user
                SkydrmLocalApp.Singleton.ShowBalloonTip(msg);
            }
            catch
            {
                // ignore anything
            }
        }

        public static void HandleSessionExpiration()
        {
            SkydrmLocalApp.Singleton.IsPopUpSessionExpirateDlg = true;

            if (CustomMessageBoxWindow.CustomMessageBoxResult.Positive == CustomMessageBoxWindow.Show(
                CultureStringInfo.RemoveFile_DlgBox_Title,
                "",
                CultureStringInfo.Exception_Sdk_Rest_401_Authentication_Failed,
                CustomMessageBoxWindow.CustomMessageBoxIcon.None,
                CustomMessageBoxWindow.CustomMessageBoxButton.BTN_OK,
                null,
                null,
                16,
                 false))
            {
                SkydrmLocalApp.Singleton.Logout(null,true);
                SkydrmLocalApp.Singleton.IsPopUpSessionExpirateDlg = false;
            }
        }

    }
}
