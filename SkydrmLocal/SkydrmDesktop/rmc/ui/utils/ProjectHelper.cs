using SkydrmDesktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.utils
{
    static class ProjectHelper
    {
        public static string GetProjectNameById(int id)
        {
            foreach (var one in SkydrmApp.Singleton.MyProjects.List())
            {
                if (one.Id == id)
                {
                    return one.DisplayName;
                }
            }
            return "";
        }
    }
}
