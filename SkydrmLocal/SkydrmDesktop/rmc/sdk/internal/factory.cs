using Sdk.helper;
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
            RmSdkRestMethodKind methodKind = RmSdkRestMethodKind.Genernal,
            string message = ""
            )
        {
            // dispatch by error_code
            string msg = message;

            //
            // Intercept error by errorcode and transfer it to exception, then throw out
            //

            // rights not granrted intercept
            if (errorCode == InsufficientRightsException.SdkErrorCode)
            {
                msg = String.Format("Exception for {0}, Special-defined Error:{1}",
                    funcName, InsufficientRightsException.DefaultMsg);
                throw new InsufficientRightsException();

            }
            else if (errorCode == ResourceIsInUseException.SdkErrorCode)
            {
                throw new ResourceIsInUseException(msg);
            }
            // network_io intercept
            else if (errorCode >= Config.SDK_NETWORK_ERROR_BASE && errorCode <= Config.SDK_NETWORK_ERROR_MAX)
            {
                // this if SKD Network IO excpeiton
                msg = String.Format("Exception for {0}, Special-defined Error:{1}",
                    funcName, RmSdkNetworkIoException.DefaultMsg);
                throw new RmSdkNetworkIoException();
            }
            // rmsdk rest api intercept
            else if (errorCode >= Config.RMS_ERROR_BASE)
            {
                // write log first
                int restError = (int)(errorCode - Config.RMS_ERROR_BASE);
                msg = String.Format("Exception for {0}, RMS Rest API Http Error:{1}", funcName, restError);

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
                else if(restError == 4005)
                {
                    throw new InvalidFileNameException();
                }
                
                // give each known domain a chance to throw 
                if(domain == RmSdkExceptionDomain.Sdk_Common)
                {
                    ThrowForSdkCommon(methodKind, restError);
                }
                else if (domain == RmSdkExceptionDomain.Rest_MyDrive)
                {
                    ThrowForRestMyDrive(methodKind, restError);
                }
                else if (domain == RmSdkExceptionDomain.Rest_MyVault)
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
                else if (domain == RmSdkExceptionDomain.Rest_WorkSpace)
                {
                    ThrowForRestWorkSpace(methodKind, restError);
                }
                else if (domain == RmSdkExceptionDomain.Rest_SharedWorkSpace)
                {
                    ThrowForRestSharedWorkSpace(methodKind, restError);
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
            else if(errorCode >= Config.NXL_BASE)
            {
                int restError = (int)(errorCode - Config.NXL_BASE);
                msg = String.Format("Exception for {0}, SDK NXL Base Error:{1}", funcName, restError);
                if (restError == 2)
                {
                    throw new AccessForbiddenException();
                }

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
                throw new RmSdkException();
            }
        }

        static private void ThrowForSdkCommon(RmSdkRestMethodKind methodKind, int RestError)
        {
            if (methodKind == RmSdkRestMethodKind.Genernal)
            {
                string msg = "";
                switch (RestError)
                {
                    case 4000:
                        msg = CultureStringInfo.Exception_Sdk_Rest_4000_UnverifiedMetadata;
                        break;
                    case 4001:
                        msg = CultureStringInfo.Exception_Sdk_Rest_4001_FileExists;
                        break;
                    default:
                        msg = CultureStringInfo.Exception_Sdk_General;
                        break;
                }
                throw new RmSdkException(msg);
            }

            throw new RmSdkException();
        }

        // for mydrive, it may has its own special exceptions
        static private void ThrowForRestMyDrive(RmSdkRestMethodKind methodKind, int RestError)
        {
            if (methodKind == RmSdkRestMethodKind.Upload)
            {
                string msg = "";
                switch (RestError)
                {
                    case 4001:
                        msg = CultureStringInfo.Exception_Sdk_Rest_MyDrive_Upload_4001_InvalidName;
                        break;
                    case 4002:
                        msg = CultureStringInfo.Exception_Sdk_Rest_MyDrive_Upload_4002_FileExists;
                        break;
                    case 4005:
                        msg = CultureStringInfo.Exception_Sdk_Rest_MyDrive_Upload_4005_FileNameExceed;
                        break;
                    default:
                        msg = CultureStringInfo.Exception_Sdk_General_UploadFailed;
                        break;
                }
                throw new RmRestApiException(msg, RmSdkExceptionDomain.Rest_MyDrive, methodKind, RestError);
            }

            throw new RmRestApiException(RestError);
        }

        // for myvault, it may has its own special exceptions
        static private void ThrowForRestMyVault(RmSdkRestMethodKind methodKind, int RestError)
        {
            if (methodKind == RmSdkRestMethodKind.Upload)
            {
                string msg = "";
                switch (RestError)
                {
                    case 4001:
                        msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_4001_FileExists;
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
                        msg = CultureStringInfo.Exception_Sdk_Rest_MyVault_5005_InvalidDUID;
                        break;
                    default:
                        msg = CultureStringInfo.Exception_Sdk_General_UploadFailed;
                        break;
                }
                throw new RmRestApiException(msg, RmSdkExceptionDomain.Rest_MyVault, methodKind, RestError);
            }

            throw new RmRestApiException(RestError);
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
            if (methodKind == RmSdkRestMethodKind.Upload)
            {
                string msg = "";
                switch (RestError)
                {
                    case 4001:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_4001_FileExists;
                        break;
                    case 4002:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_4002_InvalidProject;
                        break;
                    case 4003:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_4003_FolderNotF;
                        break;
                    case 4004:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_4004_InvalidExpiry;
                        break;
                    case 4005:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_4005_InvalidName;
                        break;
                    case 4007:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_4007_IsEditing;
                        break;
                    case 5001:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_5001_EmptyFile;
                        break;
                    case 5002:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_5002_NotBelong;
                        break;
                    case 5003:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_5003_Unsupport;
                        break;
                    case 5004:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_5004_InvalidNxl;
                        break;
                    case 5005:
                        msg = CultureStringInfo.Exception_Sdk_Rest_Project_Upload_5005_RightsRequired;
                        break;
                    default:
                        msg = CultureStringInfo.Exception_Sdk_General_UploadFailed;
                        break;
                }
                throw new RmRestApiException(msg, RmSdkExceptionDomain.Rest_MyProject, methodKind, RestError);
            }

            throw new RmRestApiException(RestError);
        }

        static private void ThrowForRestWorkSpace(RmSdkRestMethodKind methodKind, int RestError)
        {
            if (methodKind == RmSdkRestMethodKind.Upload)
            {
                string msg = "";
                switch (RestError)
                {
                    case 4001:
                        msg = CultureStringInfo.Exception_Sdk_Rest_WorkSpace_Upload_4001_FileExists;
                        break;
                    case 5003:
                        msg = CultureStringInfo.Exception_Sdk_Rest_WorkSpace_Upload_5003_NotBelong;
                        break;
                    default:
                        msg = CultureStringInfo.Exception_Sdk_General_UploadFailed;
                        break;
                }
                throw new RmRestApiException(msg, RmSdkExceptionDomain.Rest_WorkSpace, methodKind, RestError);
            }

            throw new RmRestApiException(RestError);
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

        static private void ThrowForRestSharedWorkSpace(RmSdkRestMethodKind methodKind, int RestError)
        {
            if (methodKind == RmSdkRestMethodKind.Upload)
            {
                string msg = "";
                switch (RestError)
                {
                    case 4001:
                        msg = CultureStringInfo.Exception_Sdk_Rest_SharedSp_Upload_4001_FileExists;
                        break;
                    case 4002:
                        msg = CultureStringInfo.Exception_Sdk_Rest_SharedSp_Upload_4002_IsEditing;
                        break;
                    case 5001:
                        msg = CultureStringInfo.Exception_Sdk_Rest_SharedSp_Upload_5001_InvalidNxl;
                        break;
                    case 5003:
                        msg = CultureStringInfo.Exception_Sdk_Rest_SharedSp_Upload_5003_NotBelong;
                        break;
                    case 5005:
                        msg = CultureStringInfo.Exception_Sdk_Rest_SharedSp_Upload_5005_EmptyFile;
                        break;
                    case 5006:
                        msg = CultureStringInfo.Exception_Sdk_Rest_SharedSp_Upload_5006_UnSupported;
                        break;
                    case 5008:
                        msg = CultureStringInfo.Exception_Sdk_Rest_SharedSp_Upload_5008_NotValid;
                        break;
                    case 5009:
                        msg = CultureStringInfo.Exception_Sdk_Rest_SharedSp_Upload_5009_FileExistsInAnother;
                        break;
                    default:
                        msg = CultureStringInfo.Exception_Sdk_General_UploadFailed;
                        break;
                }
                throw new RmRestApiException(msg, RmSdkExceptionDomain.Rest_SharedWorkSpace, methodKind, RestError);
            }

            throw new RmRestApiException(RestError);
        }

    }

}
