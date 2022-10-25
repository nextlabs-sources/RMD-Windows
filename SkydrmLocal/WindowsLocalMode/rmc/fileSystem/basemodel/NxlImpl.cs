using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public virtual Int32 ProjectId { get; set; }

        public bool IsFolder => false;

        private string sharedWith;
        public virtual string SharedWith
        {
            get => sharedWith;
            set
            {
                sharedWith = value;
                NotifyPropertyChanged("SharedWith");
            }
        }

        //private EnumNxlFileStatus fileStatus;
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

        public virtual string ParentId { get; set; }

        public virtual IFingerPrint FingerPrint { get; }

        public virtual IFileInfo FileInfo { get; }

        public virtual bool IsCreatedLocal { get; set; }

        public virtual string RmsRemotePath { get; set; }

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

        public virtual string[] Emails { get; set; }

        public virtual bool IsMarkedFileRemoteModified { get; set; }

        public virtual bool IsModifiedRights { get; set; }

        public virtual bool IsEdit { get; set; }

        public virtual string Duid { get; set; }

        public virtual void Remove()
        {
            throw new NotImplementedException();
        }

        public virtual void Upload() { }

        // Property changed notify.
        public virtual event PropertyChangedEventHandler PropertyChanged;

    }

    // Currently only used for project folder.
    public class NxlFolder : INxlFile
    {
        public string Name { get; set; }

        public EnumFileLocation Location { get; set; }

        public virtual string DateModified { get; set; }

        public DateTime RawDateModified { get; set; }

        public long Size { get; set; }

        public virtual Int32 ProjectId { get; set; }

        public bool IsFolder => true;

        public string SharedWith => "";

        public EnumNxlFileStatus FileStatus { get; set; }

        public EnumFileRepo FileRepo { get; set; }

        public string LocalPath { get; set; }
        public string PartialLocalPath { get; }

        public string SourcePath { get => ""; set => throw new NotImplementedException(); }

        public string ParentId { get; set; }

        public IFingerPrint FingerPrint => throw new NotImplementedException();

        public IFileInfo FileInfo => throw new NotImplementedException();

        public bool IsCreatedLocal => throw new NotImplementedException();

        public bool IsEdit { get => false; set => throw new NotImplementedException(); }

        public string RmsRemotePath { get; set; }

        // Mainly used for project folder ui merge.
        public string FileId { get; set; }

        public bool IsMarkedOffline { get; set; }

        public void Remove()
        {
            throw new NotImplementedException();
        }

        // the children nodes of the folder.
        public IList<INxlFile> Children { get; set; }

        public string PathDisplay { get; set; }

        public string PathId { get; set; }

        public virtual string[] Emails => throw new NotImplementedException();

        public bool IsMarkedFileRemoteModified { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Duid { get => throw new NotImplementedException(); }
        public bool IsModifiedRights { get => false; set => throw new NotImplementedException(); }

        public NxlFolder(IProjectFile raw)
        {
            this.Name = raw.Name;
            this.Size = raw.FileSize;
            this.Location = EnumFileLocation.Online;
            this.FileStatus = EnumNxlFileStatus.Online;
            this.FileRepo = EnumFileRepo.REPO_PROJECT;
            this.DateModified = CommonUtils.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
            this.RawDateModified = raw.LastModifiedTime;

            this.LocalPath = raw.LocalDiskPath;
            this.PathDisplay = raw.RMSDisplayPath;
            this.PathId = raw.RsmPathId;
            this.FileId = ""; // need export
        }

        public NxlFolder(IProjectLocalFile raw)
        {
            this.Name = raw.Name;
            this.Size = raw.FileSize;
            this.Location = EnumFileLocation.Online;
            this.FileStatus = raw.Status;
            this.FileRepo = EnumFileRepo.REPO_PROJECT;
            this.DateModified = CommonUtils.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
            this.RawDateModified = raw.LastModifiedTime;

            this.LocalPath = raw.LocalDiskPath;
            this.PathDisplay = raw.RMSRemotePath;
            this.PathId = raw.RMSRemotePath;
            this.FileId = ""; // need export
        }
    }


}
