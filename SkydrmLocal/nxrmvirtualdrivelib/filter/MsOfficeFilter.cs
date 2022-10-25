using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.filter
{
    public class MsOfficeFilter : IFileFilter
    {
        private bool m_sendContentOnUnlock;

        public bool SendContentOnUnlock
        {
            get => m_sendContentOnUnlock;
            set
            {
                m_sendContentOnUnlock = value;
            }
        }

        public async Task<bool> FilterAsync(string pathName, SourceFrom sourceFrom, SourceAction sourceAction, FileSystemItemType itemType, string newPath = null)
        {
            if (itemType == FileSystemItemType.Folder)
            {
                return false;
            }
            if (!string.IsNullOrWhiteSpace(pathName) && IsBlackList(pathName))
            {
                return true;
            }

            if (sourceAction != SourceAction.MOVE)
            {
                if (!string.IsNullOrWhiteSpace(newPath) && IsBlackList(newPath))
                {
                    return true;
                }
            }

            switch (sourceAction)
            {
                case SourceAction.CREATE:
                case SourceAction.UPDATE:
                    if (SendContentOnUnlock && IsInvalidPath(pathName))
                    {
                        return true;
                    }
                    break;
                case SourceAction.MOVE:
                case SourceAction.MOVE_COMPLETION:
                    if (FileHelper.IsRecycleBinPath(pathName))
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        private static bool IsBlackList(string path)
        {
            return IsInvalidPrefix(path) || IsInvalidExtension(path);
        }

        private static bool IsInvalidPrefix(string path)
        {
            return Path.GetFileName(path).StartsWith("~$");
        }

        private static bool IsInvalidExtension(string path)
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            int len = fileNameWithoutExt.Length;

            if ((fileNameWithoutExt.StartsWith("~") && extension.Equals(".tmp", StringComparison.InvariantCultureIgnoreCase))
                || (fileNameWithoutExt.StartsWith("ppt") && extension.Equals(".tmp", StringComparison.InvariantCultureIgnoreCase))
                || (string.IsNullOrEmpty(extension) && fileNameWithoutExt.Length == 8 && fileNameWithoutExt.ToUpper() == fileNameWithoutExt)
                || (len > 5 && len < 9 && extension.Equals(".tmp", StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            else
            {
                if (extension.Equals(".tmp", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (fileNameWithoutExt.IndexOf(".docx~", StringComparison.InvariantCultureIgnoreCase) > -1)
                    {
                        return true;
                    }
                    if (fileNameWithoutExt.IndexOf(".xlsx~", StringComparison.InvariantCultureIgnoreCase) > -1)
                    {
                        return true;
                    }
                    if (fileNameWithoutExt.IndexOf(".pptx~", StringComparison.InvariantCultureIgnoreCase) > -1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsInvalidPath(string path)
        {
            return GetInvalidPath(path) != null;
        }

        private static string GetInvalidPath(string path)
        {
            int num = path.LastIndexOf(Path.DirectorySeparatorChar);
            if (num != -1 && !string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                string text = path.Insert(num + 1, "~$");
                if (File.Exists(text))
                {
                    return text;
                }
                int length = Path.GetFileNameWithoutExtension(path).Length;
                if (length > 6)
                {
                    int count = (length == 7) ? 1 : 2;
                    text = text.Remove(num + 1 + "~$".Length, count);
                    if (File.Exists(text))
                    {
                        return text;
                    }
                }
            }
            return null;
        }
    }
}
