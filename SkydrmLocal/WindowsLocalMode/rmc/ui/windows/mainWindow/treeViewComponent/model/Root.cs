using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model
{
    public class Root
    {
        // myVault and sharedWith
        public Root(string repoName, IList<INxlFile> myVaults, IList<INxlFile> sharedWithFiles)
        {
            this.RepoName = repoName;
            this.MyVaultFiles = myVaults;
            this.ShareWithFiles = sharedWithFiles;
            this.Projects = null;
        }

        // project
        public Root(string repoName, IList<ProjectData> projects)
        {
            this.RepoName = repoName;
            this.Projects = projects;
            this.MyVaultFiles = null;
            this.ShareWithFiles = null;
        }

        public string RepoName { get; private set; }

        public IList<INxlFile> MyVaultFiles { get; set; }

        public IList<INxlFile> ShareWithFiles { get; set; }

        public IList<ProjectData> Projects { get;}

    }

}
