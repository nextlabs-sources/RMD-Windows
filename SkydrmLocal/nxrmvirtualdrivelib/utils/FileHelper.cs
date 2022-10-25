using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.utils
{
    public class FileHelper
    {
        public static bool IsDirectory(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        public static bool IsDirectory(FileAttributes attributes)
        {
            return (attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        public static FileSystemItemType GetItemType(string path)
        {
            return IsDirectory(path) ? FileSystemItemType.Folder : FileSystemItemType.File;
        }

        public static bool Exists(string path)
        {
            if (path == null || path.Length == 0)
            {
                return false;
            }
            return File.Exists(path) || Directory.Exists(path);
        }

        public static bool TryGetItemType(string path, out FileSystemItemType? itemType)
        {
            itemType = null;
            if (Exists(path))
            {
                itemType = GetItemType(path);
                return true;
            }
            return false;
        }

        public static IFileSystemItemMetadata GetFileMetadata(string path)
        {
            var info = GetFileInfo(path);
            if (info == null)
            {
                return null;
            }
            return Convert(info);
        }

        public static FileSystemInfo GetFileInfo(string path)
        {
            if (File.Exists(path))
            {
                return new FileInfo(path);
            }
            if (Directory.Exists(path))
            {
                return new DirectoryInfo(path);
            }
            return null;
        }

        public static IFileSystemItemMetadata Convert(FileSystemInfo info)
        {
            IFileSystemItemMetadata fileSystemItemMetadata = (!(info is FileInfo)) ? ((Metadatabase)new FolderMetadata()) : new FileMetadata();
            FileAttributes attributes = info.Attributes & (FileAttributes)(-524289) & (FileAttributes)(-1048577) & ~FileAttributes.Offline;
            fileSystemItemMetadata.Name = info.Name;
            fileSystemItemMetadata.Attributes = attributes;
            fileSystemItemMetadata.CreationTime = info.CreationTime;
            fileSystemItemMetadata.LastWriteTime = info.LastWriteTime;
            fileSystemItemMetadata.LastAccessTime = info.LastAccessTime;
            fileSystemItemMetadata.ChangeTime = info.LastWriteTime;
            if (info is FileInfo)
            {
                ((IFileMetadata)fileSystemItemMetadata).Length = ((FileInfo)info).Length;
            }
            return fileSystemItemMetadata;
        }

        public static async Task<(Item[] creates, Item[] deletes, Dictionary<string, string> moves, Item[] updates)> FilterRemoteAsync<Item>(Dictionary<ulong, Item> remotes,
            Dictionary<ulong, Item> locals) where Item : IFileSystemItemMetadata
        {
            if (remotes == null && locals == null)
            {
                return (null, null, null, null);
            }

            List<Item> creates = new List<Item>();
            List<Item> deletes = new List<Item>();
            Dictionary<string, string> moves = new Dictionary<string, string>();
            List<Item> updates = new List<Item>();
            if (remotes == null || locals == null)
            {
                if (remotes == null)
                {
                    deletes.AddRange(locals.Values);
                }
                if (locals == null)
                {
                    creates.AddRange(remotes.Values);
                }
            }
            else
            {
                foreach (var key in remotes.Keys)
                {
                    if (key == 0)
                    {
                        continue;
                    }
                    if (locals.Keys.Contains(key))
                    {
                        Item remote = remotes[key];
                        Item local = locals[key];
                        //Updates
                        if (remote.LastWriteTime.UtcTicks != local.LastWriteTime.UtcTicks)
                        {
                            updates.Add(remote);
                        }
                        if (!remote.PathId.Equals(local.PathId))
                        {
                            moves.Add(local.PathId, remote.PathId);
                        }
                    }
                    else
                    {
                        creates.Add(remotes[key]);
                    }
                }

                foreach (var key in locals.Keys)
                {
                    if (key == 0)
                    {
                        continue;
                    }
                    if (!remotes.Keys.Contains(key))
                    {
                        deletes.Add(locals[key]);
                    }
                }
            }
            return (creates.ToArray(), deletes.ToArray(), moves, updates.ToArray());
        }

        public static string GetPath(string volume, string childPath)
        {
            return Path.GetFullPath(Path.Combine(volume.TrimStart(new char[1] { Path.DirectorySeparatorChar }), childPath));
        }

        public static bool IsRecycleBinPath(string path)
        {
            return path.IndexOf(@"\$Recycle.Bin", StringComparison.InvariantCultureIgnoreCase) != -1;
        }
    }
}
