using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.featureProvider;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmLocal.rmc.fileSystem.basemodel
{
    // Mainly used to wrapper to api NxlFile class.
    public class NxlDoc : INxlFile, INotifyPropertyChanged
    {
        public virtual string Name { get; set; }

        private EnumFileLocation location;
        public virtual EnumFileLocation Location
        {
            get { return location; }

            set
            {
                location = value;
                // trigger event
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Location"));
            }
        }

        public virtual string DateModified { get; set; }

        public virtual DateTime RawDateModified { get; set; }

        public virtual long Size { get; set; }

        public virtual string RepoId { get; set; }

        public bool IsFolder => false;

        public virtual EnumNxlFileStatus FileStatus {get; set;}

        // Auto refresh ui binding when property changed.
        protected void NotifyPropertyChanged(string boundField)
        {
            // trigger event
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(boundField));
        }

        public virtual EnumFileRepo FileRepo { get; set; }

        public virtual string LocalPath { get; set; }

        public virtual string PartialLocalPath { get; set; }

        public virtual string SourcePath { get; set; }

        public virtual IFileInfo FileInfo { get; }

        public virtual bool IsCreatedLocal { get; set; }

        public virtual string DisplayPath { get; set; }

        public virtual string PathId { get; set; }

        public virtual string FileId { get; set; }

        private bool isMarkedOffline;
        public virtual bool IsMarkedOffline
        {
            get => isMarkedOffline;
            set
            {
                isMarkedOffline = value;
                NotifyPropertyChanged("IsMarkedOffline");
            }
        }

        public virtual bool IsMarkedFileRemoteModified { get; set; }

        public virtual bool IsModifiedRights { get; set; }

        public virtual bool IsEdit { get; set; }

        public virtual string Duid { get; set; }

        // For myDrive or external repository, should overwrite this since can be normal file.
        public virtual bool IsNxlFile { get; set; } = true;

        // Shared by me
        private List<string> sharedWith = new List<string>();
        public virtual List<string> SharedWith
        {
            get => sharedWith;
            set
            {
                sharedWith = value;
                NotifyPropertyChanged("SharedWith");
            }
        }
        public virtual bool IsShared { get; set; }
        public virtual bool IsRevoked { get; set; }

        // Shared with me
        public virtual string SharedDate { get; set; }
        public virtual string SharedBy { get; set; }
        public virtual string SharedByProject { get; set; }


        // Property changed notify.
        public virtual event PropertyChangedEventHandler PropertyChanged;

        //
        // Sink to its derived class to impl by override
        //
        public virtual void Remove() { }

        public virtual bool UnMark() { return false; }

        public virtual void DownloadFile(bool isViewOnly = false) { }

        public virtual void DownloadPartial() { }

        public virtual void UploadEditedFile() { }

        public virtual void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null) { }

        public virtual void Edit(Action<IEditComplete> callback) { }

        public virtual void Export(string destFolder) { }

        // Sharing transaction
        public virtual void UpdateRecipients(List<string> addList, List<string> removedList, string comment) { }
        public virtual void Share(List<string> recipients, string comment) { } // Include reshare
        public virtual bool Revoke() { return false; }


        public override bool Equals(object obj) 
        {
            if (obj == null)
            {
                return false;
            }

            if (obj == this)
            {
                return true;
            }

            // ListView set can contains different types object(PendingUploadFile and RmsDoc{myVaultRmsDoc,WorkSpaceRmsDoc etc.})
            /*
            if (obj.GetType().Equals(this.GetType()) == false)
            {
                return false;
            }*/

            if (!(obj is NxlDoc)){
                return false;
            }

            NxlDoc doc = (NxlDoc)obj;

            /*
            if(this.IsCreatedLocal != doc.IsCreatedLocal)
            {
                return false;
            }*/

            if (!string.IsNullOrEmpty(doc.Duid) && !string.IsNullOrEmpty(this.Duid))
            {
                return doc.Duid == this.Duid;
            }

            if (!string.IsNullOrEmpty(doc.FileId) && !string.IsNullOrEmpty(this.FileId))
            {
                return doc.FileId == this.FileId;
            }

            // RepoId & other feilds
            if (!string.IsNullOrEmpty(doc.RepoId) && !string.IsNullOrEmpty(this.RepoId))
            {
                if (!string.IsNullOrEmpty(doc.PathId) && !string.IsNullOrEmpty(this.PathId))
                {
                    return doc.RepoId == this.RepoId && doc.PathId == this.PathId;
                }

                if (!string.IsNullOrEmpty(doc.DisplayPath) && !string.IsNullOrEmpty(this.DisplayPath))
                {
                    return doc.RepoId == this.RepoId && doc.DisplayPath == this.DisplayPath; 
                }
            }

            // FileRepo & other feilds
            if (!string.IsNullOrEmpty(doc.PathId) && !string.IsNullOrEmpty(this.PathId))
            {
                return doc.FileRepo == this.FileRepo && doc.PathId == this.PathId;
            }

            if (!string.IsNullOrEmpty(doc.DisplayPath) && !string.IsNullOrEmpty(this.DisplayPath))
            {
                return doc.FileRepo == this.FileRepo && doc.DisplayPath == this.DisplayPath;   
            }

            return doc.FileRepo == this.FileRepo && doc.Name == this.Name;

        }

        // Will be used when this type object is stored in Map\Dictionary set.
        public override int GetHashCode()
        {
            if (!string.IsNullOrEmpty(this.Duid))
            {
                return this.Duid.GetHashCode();
            }

            if (!string.IsNullOrEmpty(this.FileId))
            {
                return this.FileId.GetHashCode();
            }

            // RepoId & other feilds
            if (!string.IsNullOrEmpty(this.RepoId))
            {
                if (!string.IsNullOrEmpty(this.PathId))
                {
                    return this.RepoId.GetHashCode() + this.PathId.GetHashCode();
                }

                if (!string.IsNullOrEmpty(this.DisplayPath))
                {
                    return this.RepoId.GetHashCode() + this.DisplayPath.GetHashCode();   
                }
            }

            // FileRepo & other feilds
            if (!string.IsNullOrEmpty(this.PathId))
            {
                return this.FileRepo.GetHashCode() + this.PathId.GetHashCode();
            }

            if (!string.IsNullOrEmpty(this.DisplayPath))
            {
                return this.FileRepo.GetHashCode() + this.DisplayPath.GetHashCode();  
            }

            return this.FileRepo.GetHashCode() + this.Name.GetHashCode();
        }

    }

    public class NxlFolder : INxlFile
    {
        public string Name { get; set; }

        public EnumFileLocation Location { get; set; }

        public virtual string DateModified { get; set; }

        public DateTime RawDateModified { get; set; }

        public long Size { get; set; }

        public virtual string RepoId { get; set; }

        public bool IsFolder => true;

        public EnumNxlFileStatus FileStatus { get; set; }

        public EnumFileRepo FileRepo { get; set; }

        public string LocalPath { get; set; }

        public string PartialLocalPath { get; }

        public string SourcePath { get => ""; set => throw new NotImplementedException(); }

        public IFileInfo FileInfo => throw new NotImplementedException();

        public bool IsCreatedLocal => throw new NotImplementedException();

        public bool IsEdit { get => false; set => throw new NotImplementedException(); }

        public virtual bool IsNxlFile { get => false; set => throw new NotImplementedException(); }

        public string DisplayPath { get; set; }

        public string PathId { get; set; }

        // Mainly used for project folder ui merge.
        public string FileId { get; set; }

        public bool IsMarkedOffline { get; set; }

        // the children nodes of the folder.
        public IList<INxlFile> Children { get; set; }

        public bool IsMarkedFileRemoteModified { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual string Duid { get =>""; }

        public bool IsModifiedRights { get => false; set => throw new NotImplementedException(); }

        // Shared by me
        public virtual bool IsShared { get; set; }
        public virtual bool IsRevoked { get; set; }
        public virtual List<string> SharedWith => new List<string>();

        // Shared with me
        public virtual string SharedDate { get; set; }
        public virtual string SharedBy { get; set; }
        public virtual string SharedByProject { get; set; }


        //
        // Sink to its derived class to impl by override.
        //
        public virtual void Remove() { }

        public virtual bool UnMark() { return false; }

        public virtual void DownloadFile(bool isViewOnly = false) { }

        public virtual void DownloadPartial() { }

        public virtual void UploadEditedFile() { }

        public virtual void Edit(Action<IEditComplete> callback) { }

        public virtual void Export(string destFolder) { }

        // Sharing transaction
        public virtual void UpdateRecipients(List<string> addList, List<string> removedList, string comment) { }
        public virtual void Share(List<string> recipients, string comment) { } 
        public virtual bool Revoke() { return false; }


        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj == this)
            {
                return true;
            }

            if (!(obj is NxlFolder))
            {
                return false;
            }

            NxlFolder doc = (NxlFolder)obj;
            if (!string.IsNullOrEmpty(doc.Duid) && !string.IsNullOrEmpty(this.Duid))
            {
                return doc.Duid == this.Duid;
            }

            if (!string.IsNullOrEmpty(doc.FileId) && !string.IsNullOrEmpty(this.FileId))
            {
                return doc.FileId == this.FileId;
            }

            // RepoId & other feilds
            if (!string.IsNullOrEmpty(doc.RepoId) && !string.IsNullOrEmpty(this.RepoId))
            {
                if (!string.IsNullOrEmpty(doc.PathId) && !string.IsNullOrEmpty(this.PathId))
                {
                    return doc.RepoId == this.RepoId && doc.PathId == this.PathId;
                }

                if (!string.IsNullOrEmpty(doc.DisplayPath) && !string.IsNullOrEmpty(this.DisplayPath))
                {
                    return doc.RepoId == this.RepoId && doc.DisplayPath == this.DisplayPath;
                }

                if (!string.IsNullOrEmpty(doc.LocalPath) && !string.IsNullOrEmpty(this.LocalPath))
                {
                    return doc.RepoId == this.RepoId && doc.LocalPath == this.LocalPath;
                }
            }

            // FileRepo & other feilds
            if (!string.IsNullOrEmpty(doc.PathId) && !string.IsNullOrEmpty(this.PathId))
            {
                return doc.FileRepo == this.FileRepo && doc.PathId == this.PathId;
            }

            if (!string.IsNullOrEmpty(doc.DisplayPath) && !string.IsNullOrEmpty(this.DisplayPath))
            {
                return doc.FileRepo == this.FileRepo && doc.DisplayPath == this.DisplayPath;
            }

            if (!string.IsNullOrEmpty(doc.LocalPath) && !string.IsNullOrEmpty(this.LocalPath))
            {
                return doc.FileRepo == this.FileRepo && doc.LocalPath == this.LocalPath;
            }

            return doc.FileRepo == this.FileRepo && doc.Name == this.Name;

        }

        // Will be used when this type object is stored in Map\Dictionary set.
        public override int GetHashCode()
        {
            if (!string.IsNullOrEmpty(this.Duid))
            {
                return this.Duid.GetHashCode();
            }

            if (!string.IsNullOrEmpty(this.FileId))
            {
                return this.FileId.GetHashCode();
            }

            // RepoId & other feilds
            if (!string.IsNullOrEmpty(this.RepoId))
            {
                if (!string.IsNullOrEmpty(this.PathId))
                {
                    return this.RepoId.GetHashCode() + this.PathId.GetHashCode();
                }

                if (!string.IsNullOrEmpty(this.DisplayPath))
                {
                    return this.RepoId.GetHashCode() + this.DisplayPath.GetHashCode();
                }

                if (!string.IsNullOrEmpty(this.LocalPath))
                {
                    return this.RepoId.GetHashCode() + this.LocalPath.GetHashCode();
                }
            }

            // FileRepo & other feilds
            if (!string.IsNullOrEmpty(this.PathId))
            {
                return this.FileRepo.GetHashCode() + this.PathId.GetHashCode();
            }

            if (!string.IsNullOrEmpty(this.DisplayPath))
            {
                return this.FileRepo.GetHashCode() + this.DisplayPath.GetHashCode();
            }

            if (!string.IsNullOrEmpty(this.LocalPath))
            {
                return this.FileRepo.GetHashCode() + this.LocalPath.GetHashCode();
            }

            return this.FileRepo.GetHashCode() + this.Name.GetHashCode();
        }

        // Means current repo root.
        public NxlFolder()
        {
            this.PathId = "/";
            this.DisplayPath = "/";
        }

    }

}
