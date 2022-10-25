using SkydrmDesktop.Resources.languages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.mainWindow.model
{
    public class CurrentSelectedSavePath
    {
        /// <summary>
        /// ownerId Id = -1, mean dummy repo like 'MySpace', 'Project';
        /// ownerId Id = 0, mean itself like 'MyDrive', 'MyVault';
        /// ownerId Id > 0, mean this is project id, external repo id
        /// </summary>
        public string OwnerId { get; }

        //
        // Since support protecting file to follow repos, so RepoName can be below:
        // ----MyVault
        // ----WorkSpace
        // ----Project
        // ----System Bucket(Local)
        //
        public string RepoName { get; }

        // Generally is folder pathId (not including repo name), like "/allenTest/1/"
        public string DestPathId { get; }

        // Mainly used for UI display that distinguish between repos (including repo name), 
        // like "WorkSpace:/allentest/1/"; "Project: oneProject/oneFolder/"
        public string DestDisplayPath { get; }
        
        public CurrentSelectedSavePath(string repoName, string pathId = "/", string displayPath="/", string ownerId = "0")
        {
            this.RepoName = repoName;
            this.DestPathId = pathId;
            this.DestDisplayPath = displayPath;
            this.OwnerId = ownerId;
        }
    }
}
