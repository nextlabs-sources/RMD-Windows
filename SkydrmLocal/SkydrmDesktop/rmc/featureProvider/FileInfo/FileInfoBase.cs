using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop;
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
        public SkydrmException exception;
        protected NxlFileFingerPrint fingerpring;
        string watermarkStr = string.Empty;
        private bool isNormalFile = false;

        public virtual bool IsNormalFile { get => isNormalFile; } 

        public abstract string Name { get; }  // waiting for derivated class

        public abstract long Size { get; }    // waiting for derivated class

        // Note: for normal file, since fingerprint is null, we should overwrite these fields in sub-class.
        public virtual bool IsOwner
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.isOwner;
                else
                    return false;
            }
        }

        public virtual bool HasAdminRights
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.hasAdminRights;
                else
                    return false;
            }
        }

        public virtual bool IsByAdHoc
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.isByAdHoc;
                else
                    return false;
            }
        }

        public virtual bool IsByCentrolPolicy
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.isByCentrolPolicy;
                else
                    return false;
            }
        }

        public abstract DateTime LastModified { get; }    // waiting for derivated class 

        public virtual string LocalDiskPath => localDiskPath;

        public abstract string RmsRemotePath { get; }   // waiting for derivated class 

        public abstract bool IsCreatedLocal { get; }  // waiting for derivated class 

        public abstract string[] Emails { get; }  // waiting for derivated class 

        public virtual FileRights[] Rights
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.rights;
                else
                    return new FileRights[0];
            }
        }

        public virtual string WaterMark
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.isByCentrolPolicy ? watermarkStr : fingerpring.adhocWatermark;
                else
                    return "";
            }
        }

        public virtual Expiration Expiration
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.expiration;
                else
                    return new Expiration();
            }
        }

        public virtual Dictionary<string, List<string>> Tags
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.tags;
                else
                    return new Dictionary<string, List<string>>();
            }
        }

        public virtual string RawTags
        {
            get
            {
                if (bHasFingerPrint)
                    return fingerpring.rawtags;
                else
                    return "";
            }
        }

        public abstract EnumFileRepo FileRepo { get; }    // waiting for derivated class 

        public FileInfoBaseImpl(string path, bool isNormalFile = false)
        {
            this.localDiskPath = path;
            this.isNormalFile = isNormalFile;
            this.exception = null;  // by default it null
            try
            {
                if (path.Length == 0 || !FileHelper.Exist(path))
                {
                    this.exception = new SkydrmException("Path is invalid", ExceptionComponent.FEATURE_PROVIDER);
                }
                else
                {
                    if (isNormalFile)
                    {
                        return;
                    }

                    // protected file.
                    this.fingerpring = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(localDiskPath,true);
                    if (this.fingerpring.isByCentrolPolicy)
                    {
                        Dictionary<FileRights, List<WaterMarkInfo>> rightsAndWatermarks;
                        SkydrmApp.Singleton.Rmsdk.User.EvaulateNxlFileRights(path, out rightsAndWatermarks,true);
                        foreach (var v in rightsAndWatermarks)
                        {
                            List<WaterMarkInfo> waterMarkInfoList = v.Value;
                            if (waterMarkInfoList == null)
                            {
                                continue;
                            }
                            foreach (var w in waterMarkInfoList)
                            {
                                watermarkStr = w.text;
                                if (!string.IsNullOrEmpty(watermarkStr))
                                {
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(watermarkStr))
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (SkydrmException e)
            {
                this.exception = e;
                Console.WriteLine("Get fingerPrint failed: " + e.ToString());
                SkydrmApp.Singleton.Log.Warn("Get fingerPrint failed: " + e.ToString());
            }
            catch (Exception e)
            {
                this.exception = new SkydrmException("Unexpected exception occurs."+e.Message);
            }
        }


        public virtual IFileInfo Update()
        {
            this.exception = null;  // by default it null
            try
            {
                string path = LocalDiskPath;    // give dervied a chance to modify localdiskpath
                if (!isNormalFile)
                {
                    this.fingerpring = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(path);  
                }
            }
            catch (SkydrmException e)
            {
                SkydrmApp.Singleton.Log.Warn("In Update(),Can not get nxl file finger print" + e.LogUsedMessage(), e);
                this.exception = e;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Warn("Unexpected exception occurs" + e.Message, e);
                this.exception = new SkydrmException("Unexpected exception occurs" + e.Message);
            }
            return this;
        }

        private bool bHasFingerPrint { get => !isNormalFile && this.exception == null; }

    }
}
