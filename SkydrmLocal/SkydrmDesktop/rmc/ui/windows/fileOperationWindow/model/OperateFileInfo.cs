using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model
{
    public class OperateFileInfo
    {
        public OperateFileInfo(string[] filePath, string[] fileName = null, FileFromSource fromSource = FileFromSource.SkyDRM_Window_Button)
        {
            FilePath = filePath;

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

            FromSource = fromSource;
        }

        public string[] FilePath { get; }

        public string[] FileName { get; set; }

        public FileFromSource FromSource { get; }

        /// <summary>
        /// Use for protect failed
        /// </summary>
        public Dictionary<string, string> FailedFileName { get; set; } = new Dictionary<string, string>();

        private string AllPath(string[] path)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < path.Length; i++)
            {
                builder.Append(path[i]);
                if (i != path.Length-1)
                {
                    builder.Append("\n");
                }
            }
            return builder.ToString();
        }

        public override string ToString()
        {
            return "FilePath: " + AllPath(FilePath);
        }
    }
}
