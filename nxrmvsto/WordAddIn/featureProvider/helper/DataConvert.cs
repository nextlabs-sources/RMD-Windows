using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinFormControlLibrary;
using WordAddIn.sdk;

namespace WordAddIn.featureProvider.helper
{
    public class DataConvert
    {
        #region Error string
        public static string MandatoryTagString = "Please select one value for mandatory classification.";
        public static string ExpiryString = "The validity time is invalid. Please choose a validity time again.";
        public static string OpenNxlString = "Failed to open NXL file, error code:";
        public static string ProtectSucString = "File has been protected successfully.";
        public static string SystemInnerError = "There is an internal system error, contact your system administrator.";
        public static string Protect_Failed = "Failed to protect the file.";
        public static string DenyOp_InRMP = "You can't protect the file from NextLabs secured folder.";
        public static string DenyProtectString = "This file type is not supported to protect.";
        public static string UingRawBuiltinSaveAs = "This file type is not supported to protect. Please continue to save as native file type.";
        public static string ProtectChanged = "Any changes to this file will now be save to the protected file.";
        public static string RPM_Protect_Failed = "You cannot protect this file in the current folder because it is set as an NextLabs secured folder by your administrator.Try to protect the file by placing it in another folder.";
        #endregion

        public static string ReplaceNxlFileTimestamp(string fname)
        {
            // like log-2019-01-24-07-04-28.txt
            // pattern-match "-2019-01-24-07-04-28" replaced with latest lcoal timestamp
            string pattern = @"-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}";
            // new stime string
            string newTimeStamp = DateTime.Now.ToLocalTime().ToString("-yyyy-MM-dd-HH-mm-ss");
            Regex r = new Regex(pattern);
            string newName = fname;
            if (r.IsMatch(fname))
            {
                newName = r.Replace(fname, newTimeStamp);
            }
            return newName;
        }

        public static bool IsExpiry(WinFormControlLibrary.Expiration frmExpiration)
        {
            bool result = false;
            if (frmExpiration.type != WinFormControlLibrary.ExpiryType.NEVER_EXPIRE 
                && DataConvertHelp.DateTimeToTimestamp(DateTime.Now) > frmExpiration.End)
            {
                result = true;
            }
            return result;
        }
      

        /// <summary>
        /// SDK 'ProjectClassification' type convert to WinFrmControlLibrary 'Classification' type
        /// </summary>
        /// <param name="sdkTag"></param>
        /// <returns></returns>
        public static Classification[] SdkTag2FrmTag(ProjectClassification[] sdkTag)
        {
            if(sdkTag == null || sdkTag.Length == 0)
            {
                return new Classification[0];
            }

            Classification[] tags = new Classification[sdkTag.Length];
            for (int i = 0; i < sdkTag.Length; i++)
            {
                tags[i].name = sdkTag[i].name;
                tags[i].isMultiSelect = sdkTag[i].isMultiSelect;
                tags[i].isMandatory = sdkTag[i].isMandatory;
                tags[i].labels = sdkTag[i].labels;
            }
            return tags;
        }

        /// <summary>
        /// WinFrmControlLibrary 'Rights' type convert to SDK 'FileRights' type
        /// </summary>
        /// <param name="rights"></param>
        /// <returns></returns>
        public static List<FileRights> FrmRights2SdkRights(HashSet<Rights> rights)
        {
            List<FileRights> fileRights = new List<FileRights>();
            foreach (var item in rights)
            {
                switch (item)
                {
                    case Rights.RIGHT_VIEW:
                        fileRights.Add(FileRights.RIGHT_VIEW);
                        break;
                    case Rights.RIGHT_EDIT:
                        fileRights.Add(FileRights.RIGHT_EDIT);
                        break;
                    case Rights.RIGHT_PRINT:
                        fileRights.Add(FileRights.RIGHT_PRINT);
                        break;
                    case Rights.RIGHT_CLIPBOARD:
                        fileRights.Add(FileRights.RIGHT_CLIPBOARD);
                        break;
                    case Rights.RIGHT_SAVEAS:
                        fileRights.Add(FileRights.RIGHT_DOWNLOAD);//PM required
                        break;
                    case Rights.RIGHT_DECRYPT:
                        fileRights.Add(FileRights.RIGHT_DECRYPT);
                        break;
                    case Rights.RIGHT_SCREENCAPTURE:
                        fileRights.Add(FileRights.RIGHT_SCREENCAPTURE);
                        break;
                    case Rights.RIGHT_SEND:
                        fileRights.Add(FileRights.RIGHT_SEND);
                        break;
                    case Rights.RIGHT_CLASSIFY:
                        fileRights.Add(FileRights.RIGHT_CLASSIFY);
                        break;
                    case Rights.RIGHT_SHARE:
                        fileRights.Add(FileRights.RIGHT_SHARE);
                        break;
                    case Rights.RIGHT_DOWNLOAD:
                        fileRights.Add(FileRights.RIGHT_DOWNLOAD);
                        break;
                    case Rights.RIGHT_WATERMARK:
                        fileRights.Add(FileRights.RIGHT_WATERMARK);
                        break;
                    case Rights.RIGHT_VALIDITY:
                        break;
                    default:
                        break;
                }
            }
            return fileRights;
        }

        /// <summary>
        /// SDK 'FileRights' type convert to WinFrmControlLibrary 'Rights' type
        /// </summary>
        /// <param name="rights"></param>
        /// <returns></returns>
        public static HashSet<Rights> SdkRights2FrmRights(FileRights[] rights)
        {
            HashSet<Rights> fileRights = new HashSet<Rights>();
            fileRights.Add(Rights.RIGHT_VIEW);// defult rights

            foreach (var item in rights)
            {
                switch (item)
                {
                    case FileRights.RIGHT_VIEW:
                        fileRights.Add(Rights.RIGHT_VIEW);
                        break;
                    case FileRights.RIGHT_EDIT:
                        fileRights.Add(Rights.RIGHT_EDIT);
                        break;
                    case FileRights.RIGHT_PRINT:
                        fileRights.Add(Rights.RIGHT_PRINT);
                        break;
                    case FileRights.RIGHT_CLIPBOARD:
                        fileRights.Add(Rights.RIGHT_CLIPBOARD);
                        break;
                    case FileRights.RIGHT_SAVEAS:
                        fileRights.Add(Rights.RIGHT_SAVEAS);
                        break;
                    case FileRights.RIGHT_DECRYPT:
                        fileRights.Add(Rights.RIGHT_DECRYPT);
                        break;
                    case FileRights.RIGHT_SCREENCAPTURE:
                        fileRights.Add(Rights.RIGHT_SCREENCAPTURE);
                        break;
                    case FileRights.RIGHT_SEND:
                        fileRights.Add(Rights.RIGHT_SEND);
                        break;
                    case FileRights.RIGHT_CLASSIFY:
                        fileRights.Add(Rights.RIGHT_CLASSIFY);
                        break;
                    case FileRights.RIGHT_SHARE:
                        fileRights.Add(Rights.RIGHT_SHARE);
                        break;
                    case FileRights.RIGHT_DOWNLOAD:
                        fileRights.Add(Rights.RIGHT_SAVEAS);
                        break;
                    case FileRights.RIGHT_WATERMARK:
                        fileRights.Add(Rights.RIGHT_WATERMARK);
                        break;
                    //case FileRights.RIGHT_VALIDITY:
                    //    break;
                    default:
                        break;
                }
            }
            return fileRights;
        }

        /// <summary>
        /// WinFormControlLibrary 'Expiration' type convert to SDK 'Expiration' type
        /// </summary>
        /// <param name="frmExpiration"></param>
        /// <returns></returns>
        public static sdk.Expiration FrmExpt2SdkExpt(WinFormControlLibrary.Expiration frmExpiration)
        {
            sdk.Expiration expiry = new sdk.Expiration();
            switch (frmExpiration.type)
            {
                case WinFormControlLibrary.ExpiryType.NEVER_EXPIRE:
                    expiry.type = sdk.ExpiryType.NEVER_EXPIRE;
                    break;
                case WinFormControlLibrary.ExpiryType.RELATIVE_EXPIRE:
                    expiry.type = sdk.ExpiryType.RELATIVE_EXPIRE;
                    break;
                case WinFormControlLibrary.ExpiryType.ABSOLUTE_EXPIRE:
                    expiry.type = sdk.ExpiryType.ABSOLUTE_EXPIRE;
                    break;
                case WinFormControlLibrary.ExpiryType.RANGE_EXPIRE:
                    expiry.type = sdk.ExpiryType.RANGE_EXPIRE;
                    break;
                default:
                    break;
            }
            expiry.Start = frmExpiration.Start;
            expiry.End = frmExpiration.End;

            return expiry;
        }

        /// <summary>
        /// SDK 'Expiration' type convert to WinFrmControlLibrary 'Expiration' type
        /// </summary>
        /// <param name="sdkExpiration"></param>
        /// <returns></returns>
        public static WinFormControlLibrary.Expiration SdkExpt2FrmExpt(sdk.Expiration sdkExpiration)
        {
            WinFormControlLibrary.Expiration expiry = new WinFormControlLibrary.Expiration();
            switch (sdkExpiration.type)
            {
                case sdk.ExpiryType.NEVER_EXPIRE:
                    expiry.type = WinFormControlLibrary.ExpiryType.NEVER_EXPIRE;
                    break;
                case sdk.ExpiryType.RELATIVE_EXPIRE:
                    expiry.type = WinFormControlLibrary.ExpiryType.RELATIVE_EXPIRE;
                    break;
                case sdk.ExpiryType.ABSOLUTE_EXPIRE:
                    expiry.type = WinFormControlLibrary.ExpiryType.ABSOLUTE_EXPIRE;
                    break;
                case sdk.ExpiryType.RANGE_EXPIRE:
                    expiry.type = WinFormControlLibrary.ExpiryType.RANGE_EXPIRE;
                    break;
                default:
                    break;
            }
            expiry.Start = sdkExpiration.Start;
            expiry.End = sdkExpiration.End;

            return expiry;
        }

    }
}
