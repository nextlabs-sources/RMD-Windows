using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;

namespace SkydrmLocal.rmc.featureProvider.FileInfo
{
    /**
    Since SDK has provide the nxl file fingerprint, so we take this class as a raw wrapper of rmsdk
    Inherited behavior:
        if user is NOT allowed to have accesses, every functions will throw out InsufficientException
            
    */
    abstract class FileInfoBaseImpl : IFileInfo
    {
        protected string localDiskPath;
        protected SkydrmException exception;
        protected NxlFileFingerPrint fingerpring;
        //---------------------------------------------

        public virtual string Name { get { IfThrowOutExcpetion(); return fingerpring.name; } }

        public virtual long Size { get { IfThrowOutExcpetion(); return fingerpring.size; } }

        public virtual bool IsOwner { get { IfThrowOutExcpetion(); return fingerpring.isOwner; } }

        public virtual bool IsByAdHoc { get { IfThrowOutExcpetion(); return fingerpring.isByAdHoc; } }

        public virtual bool IsByCentrolPolicy { get { IfThrowOutExcpetion(); return fingerpring.isByCentrolPolicy; } }

        public abstract DateTime LastModified { get; }    // waiting for derivated class 

        public virtual string LocalDiskPath => localDiskPath;

        public abstract string RmsRemotePath { get; }   // waiting for derivated class 

        public abstract bool IsCreatedLocal { get; }  // waiting for derivated class 

        public abstract string[] Emails { get; }  // waiting for derivated class 

        public virtual FileRights[] Rights { get { IfThrowOutExcpetion(); return fingerpring.rights; } }

        public virtual string WaterMark { get { IfThrowOutExcpetion(); return fingerpring.adhocWatermark; } }

        public virtual Expiration Expiration { get { IfThrowOutExcpetion(); return fingerpring.expiration; } }

        public virtual Dictionary<string, List<string>> Tags { get { IfThrowOutExcpetion(); return fingerpring.tags; } }

        public virtual string RawTags { get { IfThrowOutExcpetion(); return fingerpring.rawtags; } }

        public abstract EnumFileRepo FileRepo { get; }    // waiting for derivated class 

        public FileInfoBaseImpl(string path)
        {
            this.localDiskPath = path;
            this.exception = null;  // by default it null
            try
            {
                if (path.Length == 0 || !FileHelper.Exist(path))
                {
                    this.exception = new SkydrmException("Path is invalid", ExceptionComponent.FEATURE_PROVIDER);
                }
                else
                {
                    this.fingerpring = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(localDiskPath);
                }
            }
            catch (SkydrmException e)
            {
                this.exception = e;
            }
            catch (Exception e)
            {
                this.exception = new SkydrmException("Unexpected exception occurs"+e.Message);
            }
        }


        public virtual IFileInfo Update()
        {
            this.exception = null;  // by default it null
            try
            {
                string path = LocalDiskPath;    // give dervied a chance to modify localdiskpath
                this.fingerpring = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(path);
            }
            catch (SkydrmException e)
            {
                SkydrmLocalApp.Singleton.Log.Warn("In Update(),Can not get nxl file finger print" + e.LogUsedMessage(), e);
                this.exception = e;
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Warn("Unexpected exception occurs" + e.Message, e);
                this.exception = new SkydrmException("Unexpected exception occurs" + e.Message);
            }
            return this;
        }

        protected void IfThrowOutExcpetion()
        {
            if (this.exception != null)
            {
                throw this.exception;
            }
        }
    }
}
