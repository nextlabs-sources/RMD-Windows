using Alphaleonis.Win32.Filesystem;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.renameFileWindow.rename
{
    class RenameFile : IRenameFile
    {
        private INxlFile nxlFile;
        private string adviceName;
        private bool renameResult;
        public RenameFile(INxlFile nxl, string newName)
        {
            nxlFile = nxl;
            adviceName = newName;
        }

        public string AdviceName => adviceName;

        public bool RenameResult => renameResult;

        public bool Rename(string newName)
        {
            bool result = false;
            try
            {
                if (newName.Equals(nxlFile.Name))
                {
                    throw new Exception(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Rename"));
                }

                string originalNxlPath = nxlFile.LocalPath;
                string folderPath = Path.GetDirectoryName(originalNxlPath);
                string newNxlPath = Path.Combine(folderPath, newName);

                string tempNxlPath= Path.Combine(Path.GetTempPath(), newName);
                File.Copy(originalNxlPath, tempNxlPath, true);
                // decrypt
                string decryptPath = RightsManagementService.GenerateDecryptFilePath(SkydrmApp.Singleton.User.RPMFolder, tempNxlPath, DecryptIntent.ExtractContent);
                RightsManagementService.DecryptNXLFile(SkydrmApp.Singleton, tempNxlPath, decryptPath);
                if (File.Exists(decryptPath))
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(decryptPath));
                    File.Copy(decryptPath, tempPath, true);
                    RightsManagementService.RPMDeleteDirectory(SkydrmApp.Singleton, Path.GetDirectoryName(decryptPath));

                    SkydrmApp.Singleton.Rmsdk.User.ProtectFileFrom(tempPath, tempNxlPath, out string outPutNxlPath);

                    //File.Move(outPutNxlPath, newNxlPath, MoveOptions.ReplaceExisting);
                    File.Copy(outPutNxlPath, newNxlPath, true);
                    FileHelper.Delete_NoThrow(outPutNxlPath);

                    //update local cache
                    nxlFile.Name = newName;
                    nxlFile.LocalPath = newNxlPath;
                    result = true;

                    FileHelper.Delete_NoThrow(originalNxlPath);
                    FileHelper.Delete_NoThrow(tempNxlPath);
                    FileHelper.Delete_NoThrow(tempPath);
                }
                else
                {
                    FileHelper.Delete_NoThrow(tempNxlPath);
                    throw new Exception(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Rename"));
                }

            }
            catch (Exception e)
            {
                result = false;
                SkydrmApp.Singleton.Log.Error(e);
                // notify user
                SkydrmApp.Singleton.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_RecordLog_File_Rename"), false, nxlFile.Name);
            }
            renameResult = result;
            return result;
        }
    }
}
