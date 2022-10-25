using SkydrmDesktop.rmc.fileSystem.utils;
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
        // For project
        public Root(string repoName, IList<ProjectData> projects)
        {
            this.RepoName = repoName;
            this.Projects = projects;
            this.RepoType = repoName;
        }

        // For other repo
        public Root(string repoName, IList<INxlFile> files, string repoType, RepositoryProviderClass repoClass = RepositoryProviderClass.UNKNOWN)
        {
            this.RepoName = repoName;
            this.RepoFiles = files;
            this.RepoType = repoType;
            this.RepoClass = repoClass;
        }

        public string RepoName { get; set; }

        // Mainly used for distinguish external repository.
        public string RepoType { get; }

        // Use for external repo account type(personal, business, application)
        public RepositoryProviderClass RepoClass { get; }

        public IList<INxlFile> RepoFiles { get; } // Contains Folder.

        public IList<ProjectData> Projects { get;}
    }

}
