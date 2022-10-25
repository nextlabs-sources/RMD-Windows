using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.exception;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.cookie
{
    public class Cookie
    {
    //  public string ModuleName { get; }
        public bool IsClickFromNxrmApp { get=> mIsClickFromNxrmApp ; }
        public bool AllowEdit { get=> mAllowEdit; }
        public bool AllowShare { get=> mAllowShare; }
        public string FilePath { get=> mFilePath; }
        public EnumIntent Intent { get=> mIntent; }

        public string FileRepo { get => mFileRepo; }
        public string RepoId { get => mRepoId; }
        public string DisplayPath { get => mDisplayPath; }
        public string[] Emails { get => mEmails; }

        private bool mIsClickFromNxrmApp = false;
        private bool mAllowEdit = false;
        private bool mAllowShare = false;
        private string mFilePath = string.Empty;
        private EnumIntent mIntent = EnumIntent.Unknown;
        private string mFileRepo = string.Empty;
        private string mRepoId = string.Empty;
        private string mDisplayPath = string.Empty;
        private string[] mEmails = new string[] { };

        public Cookie(bool isClickFromNxrmApp, bool allowEdit, bool allowShare, string filePath, EnumIntent intent)
        {
          //  this.ModuleName = moduleName;
            this.mIsClickFromNxrmApp = isClickFromNxrmApp;
            this.mAllowEdit = allowEdit;
            this.mAllowShare = allowShare;
            this.mFilePath = filePath;
            this.mIntent = intent;
        }

        public Cookie(bool isClickFromNxrmApp, 
                      bool allowEdit, 
                      bool allowShare, 
                      string filePath, 
                      EnumIntent intent,
                      string fileRepo,
                      string repoId,
                      string displayPath,
                      string[] emails)
        {
            //  this.ModuleName = moduleName;
            this.mIsClickFromNxrmApp = isClickFromNxrmApp;
            this.mAllowEdit = allowEdit;
            this.mAllowShare = allowShare;
            this.mFilePath = filePath;
            this.mIntent = intent;
            this.mFileRepo = fileRepo;
            this.mRepoId = repoId;
            this.mDisplayPath = displayPath;
            this.mEmails = emails;
        }

        private static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private static bool IsAllowEdit(FileExternalInfo fileInfo)
        {
            bool result = false;
            if (String.Equals(fileInfo.FileRepo, "REPO_PROJECT", StringComparison.CurrentCultureIgnoreCase)
                || String.Equals(fileInfo.FileRepo, "REPO_WORKSPACE", StringComparison.CurrentCultureIgnoreCase)
                || String.Equals(fileInfo.FileRepo, "REPO_EXTERNAL_DRIVE", StringComparison.CurrentCultureIgnoreCase))
            {
                if (String.Equals(fileInfo.FileStatus, "AvailableOffline", StringComparison.CurrentCultureIgnoreCase)
                    || String.Equals(fileInfo.FileStatus, "CachedFile", StringComparison.CurrentCultureIgnoreCase)
                    || fileInfo.IsEdit)
                {
                    result = true;
                }
            }
            return result;
        }

        private static bool IsAllowShare(FileExternalInfo fileInfo)
        {
            bool result = false;
            if (String.Equals(fileInfo.FileStatus, "AvailableOffline", StringComparison.CurrentCultureIgnoreCase)
                || String.Equals(fileInfo.FileStatus, "CachedFile", StringComparison.CurrentCultureIgnoreCase)
                || String.Equals(fileInfo.FileStatus, "Online", StringComparison.CurrentCultureIgnoreCase)
                || String.Equals(fileInfo.FileStatus, "DownLoadedSucceed", StringComparison.CurrentCultureIgnoreCase)
                )
            {
                result = true;
            }
            return result;
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private static bool IsBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0
            || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
                return false;
            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch (Exception exception)
            {
                // Handle the exception
            }
            return false;
        }

        public static Cookie ParseCmdArgs(string[] cmdArgs)
        {
            Cookie result = null;
            bool isClickFromNxrmApp = false;
            bool allowEdit = false;
            bool allowShare = false;
            string filePath = string.Empty;
            string fileRepo = string.Empty;
            string repoId = string.Empty;
            string displayPath = string.Empty;
            string[] emails = new string[] { };
            EnumIntent intent = EnumIntent.Unknown;

            try
            {
                if (null == cmdArgs)
                {
                    throw new ArgumentNullException();
                }

                if (cmdArgs.Length == 1)
                {
                    //if (IsBase64(cmdArgs[0]))
                    //{
                    //    string jsonStr = Base64Decode(cmdArgs[0]);
                    //    if (IsValidJson(jsonStr))
                    //    {
                    //        FileExternalInfo fileInfo = JsonConvert.DeserializeObject<FileExternalInfo>(jsonStr);
                    //        intent = IntentParser.GetIntent(fileInfo.Intent);
                    //        if (intent == EnumIntent.View)
                    //        {
                    //            isClickFromNxrmApp = fileInfo.IsClickFromSkydrmDesktop;
                    //            filePath = IntentParser.VerifyFilePath(fileInfo.FilePath);
                    //            allowEdit = IsAllowEdit(fileInfo);
                    //            allowShare = IsAllowShare(fileInfo);
                    //            result = new Cookie(isClickFromNxrmApp,
                    //                allowEdit,
                    //                allowShare,
                    //                filePath,
                    //                EnumIntent.View,
                    //                fileInfo.FileRepo,
                    //                fileInfo.RepoId,
                    //                fileInfo.DisplayPath
                    //                );
                    //        }
                    //    }
                    //    else
                    //    {
                    //        throw new NotSupportedException("Unsupported command :"+ cmdArgs.ToString());
                    //    }
                    //}
                    //else
                    //{
                    //    filePath = IntentParser.VerifyFilePath(cmdArgs[0]);
                    //    result = new Cookie(isClickFromNxrmApp, allowEdit, allowShare, filePath, EnumIntent.View);
                    //}

                    filePath = IntentParser.VerifyFilePath(cmdArgs[0]);
                    result = new Cookie(isClickFromNxrmApp, allowEdit, allowShare, filePath, EnumIntent.View);
                }
                else if (cmdArgs.Length == 2)
                {
                    intent = IntentParser.GetIntent(cmdArgs[0]);
                    if (intent == EnumIntent.View)
                    {
                        filePath = IntentParser.VerifyFilePath(cmdArgs[1]);
                        result = new Cookie(isClickFromNxrmApp, allowEdit, allowShare, filePath, intent);
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported command :" + cmdArgs.ToString());
                    }
                }
                else if (cmdArgs.Length == 3)
                {
                    intent = IntentParser.GetIntent(cmdArgs[0]);
                    if (intent == EnumIntent.View)
                    {
                       // IntentParser.GetInformation(cmdArgs[1], out allowEdit, out isClickFromNxrmApp, out allowShare);
                        filePath = IntentParser.VerifyFilePath(cmdArgs[1]);
                        if (IsBase64(cmdArgs[2]))
                        {
                            string jsonStr = Base64Decode(cmdArgs[2]);
                            if (IsValidJson(jsonStr))
                            {
                                FileExternalInfo fileInfo = JsonConvert.DeserializeObject<FileExternalInfo>(jsonStr);
                                isClickFromNxrmApp = fileInfo.IsClickFromSkydrmDesktop;
                                allowEdit = IsAllowEdit(fileInfo);
                                allowShare = IsAllowShare(fileInfo);
                                fileRepo = fileInfo.FileRepo;
                                repoId = fileInfo.RepoId;
                                displayPath = fileInfo.DisplayPath;
                                emails = fileInfo.emails;
                            }
                        }
                    
                        result = new Cookie(isClickFromNxrmApp, allowEdit, allowShare, filePath, intent, fileRepo, repoId, displayPath, emails);
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported command :" + cmdArgs.ToString());
                    }
                }

                //if (null == result)
                //{
                //    throw new ParseCmdArgsException(ViewerApp.Current.FindResource("Common_Initialize_failed").ToString());
                //}

                return result;
            }
            catch (NotSupportedException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new ParseCmdArgsException(ViewerApp.Current.FindResource("Common_Initialize_failed").ToString());
            }
        }
    }
}
