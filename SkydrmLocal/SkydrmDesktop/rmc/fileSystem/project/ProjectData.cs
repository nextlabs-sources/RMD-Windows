using SkydrmLocal.rmc.fileSystem.basemodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.fileSystem.project
{
    // UI will bind this data struct as All projects current user owned
    public class ProjectData
    {
        public ProjectData()
        {
        }

        public ProjectData(ProjectInfo info, IList<INxlFile> nodes)
        {
            ProjectInfo = info;
            FileNodes = nodes;
        }

        public ProjectInfo ProjectInfo { get; set; }

        // Including project online nodes and local created nodes.
        public IList<INxlFile> FileNodes { get; set; }
    }
}
