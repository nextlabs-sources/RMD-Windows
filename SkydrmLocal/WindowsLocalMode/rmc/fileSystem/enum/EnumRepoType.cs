using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.fileSystem.basemodel
{
    public enum EnumFileRepo
    {
        UNKNOWN = 0,
        EXTERN = 1, // extern nxl file.
        REPO_MYVAULT = 2,
        REPO_PROJECT = 3,
        REPO_SHARED_WITH_ME
    }
}
