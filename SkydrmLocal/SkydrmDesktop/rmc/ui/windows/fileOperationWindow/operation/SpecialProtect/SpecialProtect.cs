using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    class SpecialProtect : ISpecialProtect
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        private OperateFileInfo fileInfo;
        private CurrentSelectedSavePath currentSelectedSavePath;
        private Dictionary<string, List<string>> selectedTags;
        public SpecialProtect(OperateFileInfo fileInfo,  CurrentSelectedSavePath selectedSavePath, Dictionary<string, List<string>> tags)
        {
            this.fileInfo = fileInfo;
            this.currentSelectedSavePath = selectedSavePath;
            this.selectedTags = tags;
        }

        public FileAction FileAction => FileAction.SpecialProtect;

        public OperateFileInfo FileInfo { get => fileInfo; set => fileInfo = value; }

        public CurrentSelectedSavePath CurrentSelectedSavePath { get => currentSelectedSavePath; }

        public Dictionary<string, List<string>> SelectedTags { get => selectedTags; }

        public List<FileRights> PreviewRightsByCentralPolicy(int id, Dictionary<string, List<string>> selectedTags, out string mWatermarkStr)
        {
            List<FileRights> fileRights = new List<FileRights>();
            mWatermarkStr = string.Empty;
            try
            {
                UserSelectTags tags = new UserSelectTags();
                foreach (var item in selectedTags)
                {
                    tags.AddTag(item.Key, item.Value);
                }
                // Inoke sdk api, get rights
                Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;

                app.Rmsdk.User.GetFileRightsFromCentalPolicyByProjectId(id, tags,
                    out rightsAndWatermarks);

                foreach (var item in rightsAndWatermarks.Keys)
                {
                    fileRights.Add(item);
                }

                foreach (var v in rightsAndWatermarks)
                {
                    List<WaterMarkInfo> waterMarkInfoList = v.Value;
                    if (waterMarkInfoList == null)
                    {
                        continue;
                    }
                    foreach (var w in waterMarkInfoList)
                    {
                        mWatermarkStr = w.text;
                        if (!string.IsNullOrEmpty(mWatermarkStr))
                        {
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(mWatermarkStr))
                    {
                        break;
                    }
                }
                return fileRights;
            }
            catch (SkydrmLocal.rmc.sdk.SkydrmException e)
            {
                GeneralHandler.Handle(e, true);
                return fileRights;
            }
        }

        public List<INxlFile> ProtectFile(Dictionary<string, List<string>> selectedTags)
        {
            // Reset user-selected actions
            app.User.ApplyAllSelectedOption = false;
            app.User.SelectedOption = 0;

            List<INxlFile> createdNxlFiles = new List<INxlFile>();

            // init WarterMarkInfo
            WaterMarkInfo waterMarkInfo = new WaterMarkInfo()
            {
                fontColor = "",
                fontName = "",
                text = "",
                fontSize = 0,
                repeat = 0,
                rotation = 0,
                transparency = 0
            };

            // init UserSelectTags, if protect to central policy file, Rights should clear.
            UserSelectTags userSelectTags = new UserSelectTags();
            List<FileRights> rights = new List<FileRights>();
            Expiration expiration = new Expiration();

            foreach (var item in selectedTags)
            {
                userSelectTags.AddTag(item.Key, item.Value);
            }


            var selectedSavePath = CurrentSelectedSavePath;

            // protect files
            List<string> nxlFileName = new List<string>();
            Dictionary<string, string> failedFileName = new Dictionary<string, string>();
            INxlFile doc = null;

            for (int i = 0; i < FileInfo.FilePath.Length; i++)
            {
                doc= ProtectFileHelper.SystemBucketAddLocalFile(true, FileInfo.FilePath[i], selectedSavePath.DestPathId,
                        rights, waterMarkInfo, expiration, userSelectTags, out string msg);
                if (doc != null)
                {
                    nxlFileName.Add(doc.Name);
                    createdNxlFiles.Add(doc);
                }
                else
                {
                    if (app.User.SelectedOption != 3)
                    {
                        failedFileName.Add(FileInfo.FileName[i], msg);
                    }
                }
            }

            // update fileName to NxlFileName
            if (nxlFileName.Count > 0)
            {
                FileInfo.FileName = nxlFileName.ToArray();
            }

            // update failed fileName
            FileInfo.FailedFileName = failedFileName;

            return createdNxlFiles;
        }
    }
}
