using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Sdk.helper
{
    /// <summary>
    /// Note: now hard code the string first in order to impl the sdk share link conviniently.
    /// </summary>
    class CultureStringInfo
    {
        public static string Exception_Sdk_General = "There is an internal system error, contact your system administrator.";
        public static string Exception_Sdk_General_UploadFailed = "Failed to upload.";
        public static string Exception_Sdk_Network_IO = "Can't connect to the network.";
        public static string Exception_Sdk_Insufficient_Rights = "You have no permission to access the file.";
        public static string Exception_Sdk_Rest_General = "There is an internal system error, contact your system administrator.";
        public static string Exception_Sdk_Resource_Is_In_Use = "The requested resource is in use.";
        public static string Exception_Sdk_Rest_400_InvalidParam = "Invalid request, contact your system administrator.";
        public static string Exception_Sdk_Rest_401_Authentication_Failed = "Your session has expired or become invalid, please login again.";
        public static string Exception_Sdk_Rest_403_AccessForbidden = "You are not authorized to perform the operation.";
        public static string Exception_Sdk_Rest_404_NotFound = "Unable to perform your request, resource not found.";
        public static string Exception_Sdk_Rest_500_ServerInternal = "There is an internal system error, contact your system administrator.";
        public static string Exception_Sdk_Rest_6001_StorageExceeded = "Your have exceeded the maximum storage limit.";

        // For status error code {4000(65440-61440):Unverified metadata for duid}, we still display the below info
        public static string Exception_Sdk_Rest_4000_UnverifiedMetadata = "You have no permission to access the file.";
        public static string Exception_Sdk_Rest_4001_FileExists = "A file with the same name exists in destination space.";
        public static string Exception_Sdk_Rest_4005_InvalidFileName = "Invalid file name or incorrect file tags as per saved metadata";
        //
        //for MyVault 
        //
        public static string Exception_Sdk_Rest_MyVault_304_RevokedFile = "Permission to access the file has been revoked.";
        public static string Exception_Sdk_Rest_MyVault_4003_ExpiredFile = "Permission to access the file has expired.";
        // myvault upload
        public static string Exception_Sdk_Rest_MyVault_4001_FileExists = "File already exists.";
        public static string Exception_Sdk_Rest_MyVault_5001_InvalidNxl = "Invalid NXL file.";
        public static string Exception_Sdk_Rest_MyVault_5002_InvalidRepoMetadata = "Invalid repository metadata.";
        public static string Exception_Sdk_Rest_MyVault_5003_InvalidFileName = "Invalid file name.";
        public static string Exception_Sdk_Rest_MyVault_5004_InvalidFileExtension = "Invalid file extension.";
        // myvault 5005 invalid duid is mean File already exists.
        public static string Exception_Sdk_Rest_MyVault_5005_InvalidDUID = "Invalid duid.";

        //
        //for MyDrive
        //
        // mydrive upload
        public static string Exception_Sdk_Rest_MyDrive_Upload_4001_InvalidName = "Invalid filename.";
        public static string Exception_Sdk_Rest_MyDrive_Upload_4002_FileExists = "File already exists.";
        public static string Exception_Sdk_Rest_MyDrive_Upload_4005_FileNameExceed = "File name can't exceed 128 characters.";

        //
        //for WorkSpace
        //
        // workspace upload
        public static string Exception_Sdk_Rest_WorkSpace_Upload_4001_FileExists = "File already exists.";
        public static string Exception_Sdk_Rest_WorkSpace_Upload_5003_NotBelong = "The nxl file does not belong to this workspace.";

        //
        //for Project
        //
        // project upload
        public static string Exception_Sdk_Rest_Project_Upload_4001_FileExists = "File already exists.";
        public static string Exception_Sdk_Rest_Project_Upload_4002_InvalidProject = "Invalid project.";
        public static string Exception_Sdk_Rest_Project_Upload_4003_FolderNotF = "Project folder not found.";
        public static string Exception_Sdk_Rest_Project_Upload_4004_InvalidExpiry = "Invalid expiry.";
        public static string Exception_Sdk_Rest_Project_Upload_4005_InvalidName = "Invalid file name.";
        public static string Exception_Sdk_Rest_Project_Upload_4007_IsEditing = "Another User is editing this file.";
        public static string Exception_Sdk_Rest_Project_Upload_5001_EmptyFile = "Empty files are not allowed to be uploaded.";
        public static string Exception_Sdk_Rest_Project_Upload_5002_NotBelong = "The nxl file does not belong to this project.";
        public static string Exception_Sdk_Rest_Project_Upload_5003_Unsupport = "Unsupported NXL version.";
        public static string Exception_Sdk_Rest_Project_Upload_5004_InvalidNxl = "Invalid NXL format.";
        public static string Exception_Sdk_Rest_Project_Upload_5005_RightsRequired = "Classification or Rights required.";

        //
        //for SharedWorkSpace
        //
        // SharedWorkSpace upload
        public static string Exception_Sdk_Rest_SharedSp_Upload_4001_FileExists = "File already exists.";
        public static string Exception_Sdk_Rest_SharedSp_Upload_4002_IsEditing = "Another User is editing this file.";
        public static string Exception_Sdk_Rest_SharedSp_Upload_5001_InvalidNxl = "Invalid NXL format.";
        public static string Exception_Sdk_Rest_SharedSp_Upload_5003_NotBelong = "The nxl file does not belong to this workspace.";
        public static string Exception_Sdk_Rest_SharedSp_Upload_5005_EmptyFile = "Empty files are not allowed to be uploaded.";
        public static string Exception_Sdk_Rest_SharedSp_Upload_5006_UnSupported = "Unsupported NXL version.";
        public static string Exception_Sdk_Rest_SharedSp_Upload_5008_NotValid = "The nxl file does not have valid metadata.";
        public static string Exception_Sdk_Rest_SharedSp_Upload_5009_FileExistsInAnother = "The nxl file already exists in another location in this workspace.";


    }
}
