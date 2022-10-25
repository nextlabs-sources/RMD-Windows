using SkydrmDesktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.fileSystem.project
{
    public class Helper
    {
        public static List<uint> ConvertListString2ListUint(List<string> input)
        {
            var ret = new List<uint>();
            foreach (var i in input)
            {
                ret.Add(uint.Parse(i));
            }

            return ret;
        }

        public static List<string> ConvertListUint2ListString(List<uint> list)
        {
            List<String> ret = new List<string>();

            foreach (var i in list)
            {
                ret.Add(Convert.ToString(i));
            }

            return ret;
        }

        public static string GetProjectNameById(uint id)
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
