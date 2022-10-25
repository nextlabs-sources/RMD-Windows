using Microsoft.Win32;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Viewer.utils;

namespace Viewer.render.preview
{
    public class AcceleratedRenderingDevice
    {
        private static Task mDecrypt;
        private static Task mPriviewInitialization;
        private static PreviewHandler mPreviewHandler;

        public static void Start(string nxlFilePath, string decryptFilePath, User user, Session session, log4net.ILog log)
        {
            mDecrypt = Task.Run(() =>
            {
                RightsManagementService.DecryptNXLFile(user, log , nxlFilePath , decryptFilePath);
            });
        
            mPriviewInitialization = Task.Run(() =>
            {
                try
                {
                    mPreviewHandler = PreviewHandler.Instance();
                    mPreviewHandler.Initialize(decryptFilePath, log);
                    string ext = Path.GetExtension(decryptFilePath);
                    FileType fileType = CommonUtils.GetFileTypeByFileExtension(ext);
                    CommonUtils.ProcessRegister(session, fileType, log);
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                }
            });
        }

        public async static void OpenFileAsync(PreviewHandlerHost previewHandlerHost, string filePath, log4net.ILog log)
        {           
                await Task.WhenAll(mDecrypt, mPriviewInitialization);
                previewHandlerHost.SpeedUpOpenOfficeFile(filePath,
                                                         mPreviewHandler,
                                                         log
                                                         );
        }
    }
}
