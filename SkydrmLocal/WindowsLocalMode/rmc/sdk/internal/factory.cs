using Newtonsoft.Json;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.sdk
{
    // find a good way to dispath proper exceptions by  domain, method,errorcode
    // the more detailed the better
    // plagiarize the idea from VISITOR/RESPONSIBILITY CHAIN in design-pattern
    class ExceptionFactory
    {
        static public void BuildThenThrow(string funcName, uint errorCode,
            RmSdkExceptionDomain domain = RmSdkExceptionDomain.Sdk_Common,
            RmSdkRestMethodKind methodKind = RmSdkRestMethodKind.Genernal
            )
        {
            // dispatch by error_code
            string msg = "";

            //
            // Intercept error by errorcode and transfer it to exception, then throw out
            //

            // rights not granrted intercept
            if (errorCode == InsufficientRightsException.SdkErrorCode)
            {
                msg = String.Format("Exception for {0}, Special-defined Error:{1}",
                    funcName, InsufficientRightsException.DefaultMsg);
                SkydrmLocalApp.Singleton.Log.Info(msg);
                throw new InsufficientRightsException();

            }
            // network_io intercept
            else if (errorCode >= Config.SDK_NETWORK_ERROR_BASE && errorCode <= Config.SDK_NETWORK_ERROR_MAX)
            {
                // this if SKD Network IO excpeiton
                msg = String.Format("Exception for {0}, Special-defined Error:{1}",
                    funcName, RmSdkNetworkIoException.DefaultMsg);
                SkydrmLocalApp.Singleton.Log.Info(msg);
                throw new RmSdkNetworkIoException();
            }
            // rmsdk rest api intercept
            else if (errorCode >= Config.RMS_ERROR_BASE)
            {
                // write log first
                int restError = (int)(errorCode - Config.RMS_ERROR_BASE);
                msg = String.Format("Exception for {0}, RMS Rest API Http Error:{1}", funcName, restError);
                SkydrmLocalApp.Singleton.Log.Info(msg);

                // intercept common rt
                if (restError == 400)
                {
                    throw new InvalidMalFormedParamException();
                }
                else if (restError == 401)
                {
                    throw new SessionAuthenticationException();
                }
                else if (restError == 403)
                {
                    throw new AccessForbiddenException();
                }
                else if (restError == 404)
                {
                    throw new NotFoundException();
                }
                else if (restError == 500)
                {
                    throw new ServerInternalException();
                }
                else if (restError == 6001 || restError == 6002)
                {
                    throw new StorageExceededException();
                }

                // give each known domain a chance to throw 
                if (domain == RmSdkExceptionDomain.Rest_MyVault)
                {
                    ThrowForRestMyVault(methodKind, restError);
                }
                else if (domain == RmSdkExceptionDomain.Rest_MyProject)
                {
                    ThrowForRestMyProject(methodKind, restError);
                }
                else if (domain == RmSdkExceptionDomain.Rest_MySharedWithMe)
                {
                    ThrowForRestSharedWithMe(methodKind, restError);
                }
                else if (domain == RmSdkExceptionDomain.Rest_MyDrive)
                {
                    // not defined this release
                }
                else if (domain == RmSdkExceptionDomain.Rest_Base)
                {
                    // for rest other common feature like 
                    //      token, heartbeat, user, tenent, sharing log, membership, repository
                    // if need, to produce new domain
                }
                // by last throw the common exp for rest api section
                throw new RmRestApiException(restError);
            }
            // sdk rest download rest api -- why the error code is different from upload.
            else if (errorCode == 401)
            {
                throw new SessionAuthenticationException();
            } else if (errorCode == 403)
            {
                throw new AccessForbiddenException();
            }

            // sdk general error
            else
            {
                msg = String.Format("Exception for {0},err={1} - {2}", funcName, errorCode, Config.GetSDKCommonError(errorCode));
                SkydrmLocalApp.Singleton.Log.Info(msg);
                throw new RmSdkException();
            }
        }


        // for myvault, it may has its own special exceptions
        static private void ThrowForRestMyVault(RmSdkRestMethodKind methodKind, int RestError)
        {
            /*
             4003   File is Expired
             5001	Invalid NXL format.
             5002	Invalid repository metadata.
             5003	Invalid filename.
             5004	Invalid file extension.
             5005	Invalid DUID.

            for delete method, 
              304 File has been revoked.
              5001	File is deleted.
             */
            string msg = "";
            switch (RestError)
            {
                case 304:
                    msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_304_RevokedFile;
                    break;
                case 4003:
                    msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_4003_ExpiredFile;
                    break;
                case 5001:
                    msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_5001_InvalidNxl;
                    break;
                case 5002:
                    msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_5002_InvalidRepoMetadata;
                    break;
                case 5003:
                    msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_5003_InvalidFileName;
                    break;
                case 5004:
                    msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_5004_InvalidFileExtension;
                    break;
                case 5005:
                    msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_5005_InvalidFileExtension;
                    break;
                default:
                    break;
            }

            throw new RmRestApiException(msg, RmSdkExceptionDomain.Rest_MyVault, methodKind, RestError);
        }

        static private void ThrowForRestMyProject(RmSdkRestMethodKind methodKind, int RestError)
        {
            /*
             List_Project, Get_Project_Metadata, Create Project,Update Project, Uplaod File
             4001   Project Name Too Long.
             4002	Project Description Too Long.
             4003	Project Name containing illegal special characters.
             4009	Invalid email.
             5005	Project name already exists.

             Create Folder
             4001	Invalid folder name.
             4002	Folder already exists.
             4003	Folder name length limit of 127 characters exceeded.

             Send Invitation Reminder / Revoke Invitation  / Accept Invitation / Decline Invitation
             4001	Invitation expired
             4002	Invitation already declined
             4003	logged in email does not match with invitee email
             4005	Invitation already accepted
             4006	Invitation already revoked
             4007	Decline reason too long

             Remove Member
             5001	Only Project owner can remove a member.
             5002	Project owner cannot be removed .

             Folder MetaData
             304	Folder not modified.
             */
        }


        static private void ThrowForRestSharedWithMe(RmSdkRestMethodKind methodKind, int RestError)
        {
            /*
             Reshare
             403    File is not allowed to share.
             4001   File has been revoked.

             Download
             403    You are not allowed to download the file.

            */
        }

    }

    class Helper
    {
        static public List<FileRights> ParseRights(Int64 v)
        {
            var rt = new List<FileRights>();

            if ((v & (Int64)FileRights.RIGHT_VIEW) > 0)
            {
                rt.Add(FileRights.RIGHT_VIEW);
            }
            if ((v & (Int64)FileRights.RIGHT_EDIT) > 0)
            {
                rt.Add(FileRights.RIGHT_EDIT);
            }
            if ((v & (Int64)FileRights.RIGHT_PRINT) > 0)
            {
                rt.Add(FileRights.RIGHT_PRINT);
            }
            if ((v & (Int64)FileRights.RIGHT_CLIPBOARD) > 0)
            {
                rt.Add(FileRights.RIGHT_CLIPBOARD);
            }
            if ((v & (Int64)FileRights.RIGHT_SAVEAS) > 0)
            {
                rt.Add(FileRights.RIGHT_SAVEAS);
            }
            if ((v & (Int64)FileRights.RIGHT_DECRYPT) > 0)
            {
                rt.Add(FileRights.RIGHT_DECRYPT);
            }
            if ((v & (Int64)FileRights.RIGHT_SCREENCAPTURE) > 0)
            {
                rt.Add(FileRights.RIGHT_SCREENCAPTURE);
            }
            if ((v & (Int64)FileRights.RIGHT_SEND) > 0)
            {
                rt.Add(FileRights.RIGHT_SEND);
            }
            if ((v & (Int64)FileRights.RIGHT_CLASSIFY) > 0)
            {
                rt.Add(FileRights.RIGHT_CLASSIFY);
            }
            if ((v & (Int64)FileRights.RIGHT_SHARE) > 0)
            {
                rt.Add(FileRights.RIGHT_SHARE);
            }
            if ((v & (Int64)FileRights.RIGHT_DOWNLOAD) > 0)
            {
                rt.Add(FileRights.RIGHT_DOWNLOAD);
            }
            if ((v & (Int64)FileRights.RIGHT_WATERMARK) > 0)
            {
                rt.Add(FileRights.RIGHT_WATERMARK);
            }

            return rt;
        }

        static public Dictionary<string, List<string>> ParseClassificationTag(string value)
        {
            var rt = new Dictionary<string, List<string>>();
            if (value.Length == 0)
            {
                return rt;
            }
            if (!StringHelper.IsValidJsonStr_Fast(value))
            {
                return rt;
            }

            return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(value);
        }

        static public NxlFileFingerPrint Convert(User.InternalFingerPrint fp)
        {
            NxlFileFingerPrint v = new NxlFileFingerPrint()
            {
                name = fp.name,
                created = fp.created,
                modified = fp.modified,
                localPath = fp.localPath,
                size = fp.size,
                isOwner = fp.isOwner == 1 ? true : false,
                isFromMyVault = fp.isFromMyVault == 1 ? true : false,
                isFromPorject = fp.isFromPorject == 1 ? true : false,
                isFromSystemBucket = fp.isFromSystemBucket == 1 ? true : false,
                projectId = (int)fp.projectId,
                isByAdHoc = fp.isByAdHoc == 1 ? true : false,
                isByCentrolPolicy = fp.isByCentrolPolicy == 1 ? true : false,
                tags = Helper.ParseClassificationTag(fp.tags),
                rawtags = fp.tags,
                expiration = fp.expiration,
                adhocWatermark = fp.adhocWatermark,
                rights = Helper.ParseRights(fp.rights).ToArray(),
                hasAdminRights = fp.hasAdminRights == 1 ? true : false,
                duid = fp.duid
            };
            return v;
        }

        public static MyVaultMetaData Convert(User.InternalMyVaultMetaData md)
        {
            MyVaultMetaData metaData = new MyVaultMetaData
            {
                name = md.name,
                fileLink = md.fileLink,

                lastModified = md.lastModified,
                protectedOn = md.protectedOn,
                sharedOn = md.sharedOn,
                protectionType = md.protectionType,

                isShared = md.isShared == 1 ? true : false,
                isDeleted = md.isDeleted == 1 ? true : false,
                isRevoked = md.isRevoked == 1 ? true : false,
                isOwner = md.isOwner == 1 ? true : false,
                isNxl = md.isNxl == 1 ? true : false,
                
                recipents = ParseRecipents(md.recipents),
                pathDisplay = md.pathDisplay,
                pathId = md.pathId,
                tags = Helper.ParseClassificationTag(md.tags),
                rawTags = md.tags,
                expiration = md.expiration
            };
            return metaData;
        }

        public static List<string> ParseRecipents(string recipents)
        {
            List<string> ret = new List<string>();

            if (!string.IsNullOrEmpty(recipents))
            {
                foreach (var i in recipents.Split(new char[] { ';' }))
                {
                    if (!string.IsNullOrEmpty(i))
                    {
                        ret.Add(i);
                    }
                }
            }

            return ret;
        }
    }
}
