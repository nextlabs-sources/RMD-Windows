using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.model
{
    /// <summary>
    /// Handle file protect & share operation passing paramters.
    /// </summary>
    public class FileOperation
    {
        public FileOperation(string[] filePath, ActionType action,string[] fileName=null)
        {
            FilePath = filePath;
            Action = action;

            if (fileName == null)
            {
                FileName = new string[filePath.Length];
                for (int i = 0; i < filePath.Length; i++)
                {
                    FileName[i] = Path.GetFileName(filePath[i]);
                }
                
            }
            else
            {
                FileName = fileName;
            }
            
        }

        public string[] FilePath { get; set; }

        public string[] FileName { get; set; }

        // Protect failed
        public string FailedFileName { get; set; }

        public ActionType Action { get; }

        public override string ToString()
        {
            return "FilePath: " + FilePath + "\n"
                + "FileName: " + FileName + "\n"
                + "Action: " + Action;
        }

        public enum ActionType
        {
            /// <summary>
            /// Protect file
            /// </summary>
            Protect,
            /// <summary>
            /// Protect file and share
            /// </summary>
            Share, 

            ViewFileInfo,
            /// <summary>
            /// Share .nxl file ( Keep same file)
            /// </summary>
            UpdateRecipients,

            ModifyRights
        }
    }

    
}
