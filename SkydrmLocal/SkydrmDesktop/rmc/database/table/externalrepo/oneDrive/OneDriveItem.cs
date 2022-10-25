using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.oneDrive
{
    public class OneDriveItem
    {
        public string context { get; set; } = string.Empty;
        public int count { get; set; } = 0;
        public List<ValueItem> value { get; set; } = new List<ValueItem>();
    }

    public class Reactions : IEquatable<Reactions>
    {
        public int commentCount { get; set; } = 0;

        public override bool Equals(object obj)
        {
            return Equals(obj as Reactions);
        }

        public bool Equals(Reactions other)
        {
            return other != null &&
                   commentCount == other.commentCount;
        }

        public override int GetHashCode()
        {
            return -168406171 + commentCount.GetHashCode();
        }
    }

    public class User : IEquatable<User>
    {
        public string displayName { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            return Equals(obj as User);
        }

        public bool Equals(User other)
        {
            return other != null &&
                   displayName == other.displayName &&
                   id == other.id;
        }

        public override int GetHashCode()
        {
            var hashCode = 1727683750;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(displayName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            return hashCode;
        }
    }

    public class CreatedBy : IEquatable<CreatedBy>
    {
        public Application application { get; set; } = new Application();
        public User user { get; set; } = new User();

        public override bool Equals(object obj)
        {
            return Equals(obj as CreatedBy);
        }

        public bool Equals(CreatedBy other)
        {
            return other != null &&
                   EqualityComparer<Application>.Default.Equals(application, other.application) &&
                   EqualityComparer<User>.Default.Equals(user, other.user);
        }

        public override int GetHashCode()
        {
            var hashCode = -2051324673;
            hashCode = hashCode * -1521134295 + EqualityComparer<Application>.Default.GetHashCode(application);
            hashCode = hashCode * -1521134295 + EqualityComparer<User>.Default.GetHashCode(user);
            return hashCode;
        }
    }

    public class Application : IEquatable<Application>
    {
        public string displayName { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            return Equals(obj as Application);
        }

        public bool Equals(Application other)
        {
            return other != null &&
                   displayName == other.displayName &&
                   id == other.id;
        }

        public override int GetHashCode()
        {
            var hashCode = 1727683750;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(displayName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            return hashCode;
        }
    }

    public class LastModifiedBy : IEquatable<LastModifiedBy>
    {
        public Application application { get; set; } = new Application();
        public User user { get; set; } = new User();

        public override bool Equals(object obj)
        {
            return Equals(obj as LastModifiedBy);
        }

        public bool Equals(LastModifiedBy other)
        {
            return other != null &&
                   EqualityComparer<Application>.Default.Equals(application, other.application) &&
                   EqualityComparer<User>.Default.Equals(user, other.user);
        }

        public override int GetHashCode()
        {
            var hashCode = -2051324673;
            hashCode = hashCode * -1521134295 + EqualityComparer<Application>.Default.GetHashCode(application);
            hashCode = hashCode * -1521134295 + EqualityComparer<User>.Default.GetHashCode(user);
            return hashCode;
        }
    }

    public class ParentReference : IEquatable<ParentReference>
    {
        public string driveId { get; set; } = string.Empty;
        public string driveType { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            return Equals(obj as ParentReference);
        }

        public bool Equals(ParentReference other)
        {
            return other != null &&
                   driveId == other.driveId &&
                   driveType == other.driveType &&
                   id == other.id &&
                   path == other.path;
        }

        public override int GetHashCode()
        {
            var hashCode = 737816479;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(driveId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(driveType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(path);
            return hashCode;
        }
    }

    public class FileSystemInfo : IEquatable<FileSystemInfo>
    {
        public string createdDateTime { get; set; } = string.Empty;
        public string lastModifiedDateTime { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            return Equals(obj as FileSystemInfo);
        }

        public bool Equals(FileSystemInfo other)
        {
            return other != null &&
                   createdDateTime == other.createdDateTime &&
                   lastModifiedDateTime == other.lastModifiedDateTime;
        }

        public override int GetHashCode()
        {
            var hashCode = -2046643295;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(createdDateTime);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(lastModifiedDateTime);
            return hashCode;
        }
    }

    public class View : IEquatable<View>
    {
        public string viewType { get; set; } = string.Empty;
        public string sortBy { get; set; } = string.Empty;
        public string sortOrder { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            return Equals(obj as View);
        }

        public bool Equals(View other)
        {
            return other != null &&
                   viewType == other.viewType &&
                   sortBy == other.sortBy &&
                   sortOrder == other.sortOrder;
        }

        public override int GetHashCode()
        {
            var hashCode = -1018381085;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(viewType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(sortBy);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(sortOrder);
            return hashCode;
        }
    }

    public class SpecialFolder : IEquatable<SpecialFolder>
    {
        public string name { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            return Equals(obj as SpecialFolder);
        }

        public bool Equals(SpecialFolder other)
        {
            return other != null &&
                   name == other.name;
        }

        public override int GetHashCode()
        {
            return 363513814 + EqualityComparer<string>.Default.GetHashCode(name);
        }
    }

    public class Hashes : IEquatable<Hashes>
    {
        public string quickXorHash { get; set; } = string.Empty;
        public string sha1Hash { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            return Equals(obj as Hashes);
        }

        public bool Equals(Hashes other)
        {
            return other != null &&
                   quickXorHash == other.quickXorHash &&
                   sha1Hash == other.sha1Hash;
        }

        public override int GetHashCode()
        {
            var hashCode = -1146387783;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(quickXorHash);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(sha1Hash);
            return hashCode;
        }
    }

    public class ValueItem : IEquatable<ValueItem>
    {
        public string createdDateTime { get; set; } = string.Empty;
        public string cTag { get; set; } = string.Empty;
        public string eTag { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public string lastModifiedDateTime { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public long size { get; set; } = 0;
        public string webUrl { get; set; } = string.Empty;
        public Reactions reactions { get; set; } = new Reactions();
        public CreatedBy createdBy { get; set; } = new CreatedBy();
        public LastModifiedBy lastModifiedBy { get; set; } = new LastModifiedBy();
        public ParentReference parentReference { get; set; } = new ParentReference();
        public FileSystemInfo fileSystemInfo { get; set; } = new FileSystemInfo();
        public int isFolder { get; set; } = 0;

        public ValueItem()
        {

        }

        public ValueItem(SQLiteDataReader reader)
        {
            this.createdDateTime = reader["createdDateTime"].ToString();
            this.cTag = reader["cTag"].ToString();
            this.eTag = reader["eTag"].ToString();
            this.id = reader["item_id"].ToString();
            this.lastModifiedDateTime = reader["lastModifiedDateTime"].ToString();
            this.name = reader["name"].ToString();
            this.size = long.Parse(reader["size"].ToString());
            this.webUrl = reader["webUrl"].ToString();
            this.reactions = new Reactions();
            {
                this.reactions.commentCount = int.Parse(reader["reactions_commentCount"].ToString());
            }
            this.createdBy = new CreatedBy();
            {
                this.createdBy.application.displayName = reader["createdBy_application_displayName"].ToString();
                this.createdBy.application.id = reader["createdBy_application_id"].ToString();
                this.createdBy.user.displayName = reader["createdBy_user_displayName"].ToString();
                this.createdBy.user.id = reader["createdBy_user_id"].ToString();
            }
            this.lastModifiedBy = new LastModifiedBy();
            {
                this.lastModifiedBy.application.displayName = reader["lastModifiedBy_application_displayName"].ToString();
                this.lastModifiedBy.application.id = reader["lastModifiedBy_application_id"].ToString();
                this.lastModifiedBy.user.displayName = reader["lastModifiedBy_user_displayName"].ToString();
                this.lastModifiedBy.user.id = reader["lastModifiedBy_user_id"].ToString();
            }
            this.parentReference = new ParentReference();
            {
                this.parentReference.driveId = reader["parentReference_driveId"].ToString();
                this.parentReference.driveType = reader["parentReference_driveType"].ToString();
                this.parentReference.id = reader["parentReference_id"].ToString();
                this.parentReference.path = reader["parentReference_path"].ToString();
            }
            this.fileSystemInfo = new FileSystemInfo();
            {
                this.fileSystemInfo.createdDateTime = reader["fileSystemInfo_createdDateTime"].ToString();
                this.fileSystemInfo.lastModifiedDateTime = reader["fileSystemInfo_lastModifiedDateTime"].ToString();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ValueItem);
        }

        public bool Equals(ValueItem other)
        {
            return other != null &&
                   createdDateTime == other.createdDateTime &&
                   cTag == other.cTag &&
                   eTag == other.eTag &&
                   id == other.id &&
                   lastModifiedDateTime == other.lastModifiedDateTime &&
                   name == other.name &&
                   size == other.size &&
                   webUrl == other.webUrl &&
                   EqualityComparer<Reactions>.Default.Equals(reactions, other.reactions) &&
                   EqualityComparer<CreatedBy>.Default.Equals(createdBy, other.createdBy) &&
                   EqualityComparer<LastModifiedBy>.Default.Equals(lastModifiedBy, other.lastModifiedBy) &&
                   EqualityComparer<ParentReference>.Default.Equals(parentReference, other.parentReference) &&
                   EqualityComparer<FileSystemInfo>.Default.Equals(fileSystemInfo, other.fileSystemInfo) &&
                   isFolder == other.isFolder;
        }

        public override int GetHashCode()
        {
            var hashCode = 90438936;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(createdDateTime);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(cTag);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(eTag);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(lastModifiedDateTime);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
            hashCode = hashCode * -1521134295 + size.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(webUrl);
            hashCode = hashCode * -1521134295 + EqualityComparer<Reactions>.Default.GetHashCode(reactions);
            hashCode = hashCode * -1521134295 + EqualityComparer<CreatedBy>.Default.GetHashCode(createdBy);
            hashCode = hashCode * -1521134295 + EqualityComparer<LastModifiedBy>.Default.GetHashCode(lastModifiedBy);
            hashCode = hashCode * -1521134295 + EqualityComparer<ParentReference>.Default.GetHashCode(parentReference);
            hashCode = hashCode * -1521134295 + EqualityComparer<FileSystemInfo>.Default.GetHashCode(fileSystemInfo);
            hashCode = hashCode * -1521134295 + isFolder.GetHashCode();
            return hashCode;
        }
    }

    public class File : IEquatable<File>
    {
        public string mimeType { get; set; } = string.Empty;
        public Hashes hashes { get; set; } = new Hashes();

        public override bool Equals(object obj)
        {
            return Equals(obj as File);
        }

        public bool Equals(File other)
        {
            return other != null &&
                   mimeType == other.mimeType &&
                   EqualityComparer<Hashes>.Default.Equals(hashes, other.hashes);
        }

        public override int GetHashCode()
        {
            var hashCode = 292531674;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(mimeType);
            hashCode = hashCode * -1521134295 + EqualityComparer<Hashes>.Default.GetHashCode(hashes);
            return hashCode;
        }
    }

    public class Folder : IEquatable<Folder>
    {
        public int childCount { get; set; } = 0;
        public View view { get; set; } = new View();

        public override bool Equals(object obj)
        {
            return Equals(obj as Folder);
        }

        public bool Equals(Folder other)
        {
            return other != null &&
                   childCount == other.childCount &&
                   EqualityComparer<View>.Default.Equals(view, other.view);
        }

        public override int GetHashCode()
        {
            var hashCode = 1767670868;
            hashCode = hashCode * -1521134295 + childCount.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<View>.Default.GetHashCode(view);
            return hashCode;
        }
    }

    public class FolderItem : ValueItem, IEquatable<FolderItem>
    {
        public Folder folder { get; set; } = new Folder();
        public SpecialFolder specialFolder { get; set; } = new SpecialFolder();
        public int isRootFolder { get; set; } = 0;

        public FolderItem()
        {
           this.isFolder = 1;
        }

        public FolderItem(SQLiteDataReader reader) : base(reader)
        {
            this.isFolder = 1;
            this.folder = new Folder();
            {
                this.folder.childCount = int.Parse(reader["folder_childCount"].ToString());
                this.folder.view.viewType = reader["folder_view_viewType"].ToString();
                this.folder.view.sortBy = reader["folder_view_sortBy"].ToString();
                this.folder.view.sortOrder = reader["folder_view_sortOrder"].ToString();
            }

            this.specialFolder = new SpecialFolder();
            {
                this.specialFolder.name = reader["specialFolder_name"].ToString();
            }
            this.isRootFolder = int.Parse(reader["isRootFolder"].ToString());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FolderItem);
        }

        public bool Equals(FolderItem other)
        {
            return other != null &&
                   base.Equals(other) &&
                   EqualityComparer<Folder>.Default.Equals(folder, other.folder) &&
                   EqualityComparer<SpecialFolder>.Default.Equals(specialFolder, other.specialFolder) &&
                   isRootFolder == other.isRootFolder;
        }

        public override int GetHashCode()
        {
            var hashCode = -373463548;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Folder>.Default.GetHashCode(folder);
            hashCode = hashCode * -1521134295 + EqualityComparer<SpecialFolder>.Default.GetHashCode(specialFolder);
            hashCode = hashCode * -1521134295 + isRootFolder.GetHashCode();
            return hashCode;
        }
    }

    public class FileItem : ValueItem, IEquatable<FileItem>
    {
        public File file { get; set; } = new File();
        public string downloadUrl { get; set; } = string.Empty;

        public FileItem()
        {
            this.isFolder = 0;
        }
        public FileItem(SQLiteDataReader reader) : base(reader)
        {
            this.isFolder = 0;
            this.downloadUrl = reader["downloadUrl"].ToString();
            this.file = new File();
            {
                this.file.mimeType = reader["file_mimeType"].ToString();
                this.file.hashes = new Hashes();
                {
                    this.file.hashes.quickXorHash = reader["hashes_quickXorHash"].ToString();
                    this.file.hashes.sha1Hash = reader["hashes_sha1Hash"].ToString();
                }
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileItem);
        }

        public bool Equals(FileItem other)
        {
            return other != null &&
                   base.Equals(other) &&
                   EqualityComparer<File>.Default.Equals(file, other.file) &&
                   downloadUrl == other.downloadUrl;
        }

        public override int GetHashCode()
        {
            var hashCode = 1342742635;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<File>.Default.GetHashCode(file);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(downloadUrl);
            return hashCode;
        }
    }

    public class ValueItemConverter : CustomCreationConverter<ValueItem>
    {
        private bool mIsFolder;

        public ValueItemConverter(bool isFolder)
        {
            this.mIsFolder = isFolder;
        }

        public override ValueItem Create(Type objectType)
        {
            if (mIsFolder)
            {
                return new FolderItem();
            }
            else
            {
                return new FileItem();
            }
        }
    }

}
