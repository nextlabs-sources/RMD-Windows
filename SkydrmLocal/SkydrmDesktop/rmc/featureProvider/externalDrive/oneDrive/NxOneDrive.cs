using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.database.table.externalrepo.oneDrive;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmDesktop.rmc.featureProvider.OneDrive;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using static SkydrmDesktop.rmc.database.table.externalrepo.oneDrive.OneDriveItem;
using static Skydrmlocal.rmc.database2.FunctionProvider;


namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    //// Below are the clientId (Application Id) of your app registration and the tenant information. 
    //// You have to replace:
    //// - the content of ClientID with the Application Id for your app registration
    //// - The content of Tenant by the information about the accounts allowed to sign-in in your application:
    ////   - For Work or School account in your org, use your tenant ID, or domain
    ////   - for any Work or School accounts, use organizations
    ////   - for any Work or School accounts, or Microsoft personal account, use common
    ////   - for Microsoft Personal account, use consumers
    //private static readonly string ClientId = "8ea12ff6-4f3f-4f4d-a727-d02e2be5e15e";
    //// Note: Tenant is important for the quickstart. We'd need to check with Andre/Portal if we
    //// want to change to the AadAuthorityAudience.
    //private static readonly string Tenant = "common";
    //private static readonly string Instance = "https://login.microsoftonline.com/";
    //// Set the API Endpoint to Graph 'me' endpoint. 
    //// To change from Microsoft public cloud to a national cloud, use another value of graphAPIEndpoint.
    //// Reference with Graph endpoints here: https://docs.microsoft.com/graph/deployments#microsoft-graph-and-graph-explorer-service-root-endpoints
    ////Set the scope for API call to user.read
    ////private static readonly string[] Scopes = new string[] { "Files.ReadWrite.AppFolder", "Files.ReadWrite.All" , "Sites.ReadWrite.All" };
    //private static IPublicClientApplication ClientApp;
    //private AuthenticationResult mAuthenticationResult;
    //public AuthenticationResult AuthenticationResult { get => mAuthenticationResult; }
    public class NxOneDrive : IExternalDrive
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;
        private readonly log4net.ILog log;
        private IRmsRepo rmsRepo;
        public string WorkingPath { get; }
        public OneDriveRestApiSdk sdk { get; }
        public ExternalRepoType Type => rmsRepo.Type;
        public string DisplayName { get => rmsRepo.DisplayName; set => rmsRepo.DisplayName = value; }
        public string AccessToken { get => rmsRepo.Token; set => rmsRepo.Token = value; }
        public string RepoId => rmsRepo.RepoId;

        private CancellationTokenSource mCancellationTokenSource;

        static NxOneDrive()
        {
            //ServicePointManager.DefaultConnectionLimit = 30;
            //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            //ClientApp = PublicClientApplicationBuilder.Create(ClientId)
            //    .WithAuthority($"{Instance}{Tenant}")
            //    .WithDefaultRedirectUri()
            //    .Build();
            //TokenCacheHelper.EnableSerialization(ClientApp.UserTokenCache);
        }

        public NxOneDrive(IRmsRepo repo)
        {
            this.rmsRepo = repo;
            this.log = app.Log;
            WorkingPath = app.User.WorkingFolder + "\\OneDrive\\" + repo.RepoId;
            Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(WorkingPath);
            sdk = new OneDriveRestApiSdk(repo.Token);
            mCancellationTokenSource = new CancellationTokenSource();
        }

        //public async Task<string> Login(Window window)
        //{
        //    var accounts = await ClientApp.GetAccountsAsync();
        //    var firstAccount = accounts.FirstOrDefault();

        //    try
        //    {
        //        mAuthenticationResult = await ClientApp.AcquireTokenSilent(Scopes, firstAccount)
        //            .ExecuteAsync();
        //    }
        //    catch (MsalUiRequiredException ex)
        //    {
        //        // A MsalUiRequiredException happened on AcquireTokenSilent. 
        //        // This indicates you need to call AcquireTokenInteractive to acquire a token
        //        try
        //        {
        //            mAuthenticationResult = await ClientApp.AcquireTokenInteractive(Scopes)
        //                .WithAccount(accounts.FirstOrDefault())
        //                .WithParentActivityOrWindow(new WindowInteropHelper(window).Handle) // optional, used to center the browser on the window
        //                .WithPrompt(Prompt.SelectAccount)
        //                .ExecuteAsync();
        //        }
        //        catch (MsalException msalex)
        //        {
        //            throw msalex;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //    if (mAuthenticationResult != null)
        //    {
        //        return await GetHttpContentWithToken(mAuthenticationResult.AccessToken);
        //    }
        //    return "";
        //}
        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        //private async Task<string> GetHttpContentWithToken(string token)
        //{
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        string url = GraphAPIEndpoint + "/me";
        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
        //        //Add the token in Authorization header
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        //        response = await mHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, mCancellationTokenSource.Token);
        //        var content = await response.Content.ReadAsStringAsync();
        //        return content;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        ///// <summary>
        ///// Sign out the current user
        ///// </summary>
        //public async Task SignOut()
        //{
        //    var accounts = await ClientApp.GetAccountsAsync();
        //    if (accounts.Any())
        //    {
        //        try
        //        {
        //            await ClientApp.RemoveAsync(accounts.FirstOrDefault());
        //        }
        //        catch (MsalException ex)
        //        {
        //            throw ex;
        //        }
        //    }
        //}

        //public async Task<string> Information()
        //{
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        string url = GraphAPIEndpoint + "/me/drives";
        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
        //        //Add the token in Authorization header
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mAccessToken);
        //        response = await mHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, mCancellationTokenSource.Token);
        //        return await response.Content.ReadAsStringAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public async Task<string> ListChildren(string id)
        //{
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        string url = GraphAPIEndpoint + "/me/drive/items/{item-id}/children";
        //        url = url.Replace("{item-id}", string.Equals(id, "/", StringComparison.CurrentCultureIgnoreCase) ? "root" : id);
        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
        //        //Add the token in Authorization header
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mAccessToken);
        //        response = await mHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, mCancellationTokenSource.Token);
        //        return await response.Content.ReadAsStringAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}


        //public async Task<string> Delete(string id)
        //{
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        string url = GraphAPIEndpoint + "/me/drive/items/{item-id}";
        //        url = url.Replace("{item-id}", id);
        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, url);
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mAccessToken);
        //        response = await mHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, mCancellationTokenSource.Token);
        //        return await response.Content.ReadAsStringAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public async Task<string> Upload(string parentId, string filePath)
        //{
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        string fileName = Path.GetFileName(filePath);
        //        string url = GraphAPIEndpoint + "/me/drive/items/{parent-id}:/{filename}:/content";
        //        url = url.Replace("{parent-id}", parentId);
        //        url = url.Replace("{filename}", fileName);
        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Put, url);
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mAccessToken);
        //        request.Content = new StreamContent(File.OpenRead(filePath));
        //        response = await mHttpClient.SendAsync(request, mCancellationTokenSource.Token);
        //        return await response.Content.ReadAsStringAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public async Task<string> CreateFolder(string parentId, string json)
        //{
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        string url = GraphAPIEndpoint + "/me/drive/items/{parent-item-id}/children";
        //        url = url.Replace("{parent-item-id}", parentId);
        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
        //        //Add the token in Authorization header
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mAccessToken);
        //        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        //        response = await mHttpClient.SendAsync(request, mCancellationTokenSource.Token);
        //        return await response.Content.ReadAsStringAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public async Task DownloadFile(string destinationDir, string itemId)
        //{
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        string url = GraphAPIEndpoint + "/me/drive/items/{item-id}/content";
        //        url = url.Replace("{item-id}", itemId);
        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mAccessToken);
        //        response = await mHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, mCancellationTokenSource.Token);
        //        response.EnsureSuccessStatusCode();
        //        mCancellationTokenSource.Token.ThrowIfCancellationRequested();
        //        using (var stream = await response.Content.ReadAsStreamAsync())
        //        {
        //            using (FileStream fs = new FileStream(Path.Combine(destinationDir, response.Content.Headers.ContentDisposition.FileNameStar), FileMode.OpenOrCreate))
        //            {
        //                stream.CopyTo(fs);
        //                fs.Flush();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}


        //public async Task DownloadLargFile(string destinationDir, string itemId)
        //{
        //    MultipleThreadDownload multipleThreadDownload = new MultipleThreadDownload(destinationDir);
        //    //string url = GraphAPIEndpoint + "/me/drives/c9fa40e83525749e/items/C9FA40E83525749E!120/content";
        //    string url = GraphAPIEndpoint + "/me/drive/items/{item-id}/content";
        //    url = url.Replace("{item-id}", itemId);
        //    var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
        //    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mAccessToken);
        //    multipleThreadDownload.Progress += delegate (object state, long pb)
        //    {

        //    };
        //    await multipleThreadDownload.DownLoad(request , "Jack");
        //}


        //public async Task UploadLargFile(string parentId, string filePath)
        //{
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        //string url = GraphAPIEndpoint + "/me/drive/root:/{item-path}:/createUploadSession";
        //        //url = url.Replace("{item-path}", Path.GetFileName(filePath));

        //        string url = GraphAPIEndpoint + "/me/drive/items/{itemId}:/{fileName}:/createUploadSession";
        //        url = url.Replace("{itemId}", parentId);
        //        url = url.Replace("{fileName}", Path.GetFileName(filePath));

        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mAccessToken);
        //        //  request.Headers.IfMatch.Add(new EntityTagHeaderValue("aQzlGQTQwRTgzNTI1NzQ5RSExMDQuMA"));
        //        // request.Headers.IfMatch.TryParseAdd("aQzlGQTQwRTgzNTI1NzQ5RSExMDQuMA");
        //        string json = @"{
        //                          '@microsoft.graph.conflictBehavior' : 'rename',
        //                          'description': 'description',
        //                          'fileSystemInfo': { '@odata.type': 'microsoft.graph.fileSystemInfo'},
        //                          'name': '{fileName}'
        //                       }";

        //        json = json.Replace("{fileName}", Path.GetFileName(filePath));
        //        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        //        response = await mHttpClient.SendAsync(request);
        //        response.EnsureSuccessStatusCode();
        //        var content = await response.Content.ReadAsStringAsync();
        //        JObject jo = JObject.Parse(content);
        //        if (!jo.ContainsKey("uploadUrl") || !jo.ContainsKey("nextExpectedRanges"))
        //        {
        //            return;
        //        }
        //        string[] nextExpectedRanges = jo.SelectToken("nextExpectedRanges").Select(s => (string)s).ToArray();
        //        long from = long.Parse(nextExpectedRanges[0].Split('-')[0]);
        //        string uploadUri = (string)jo.SelectToken("['uploadUrl']");
        //        //Note: If your app splits a file into multiple byte ranges, the size of each byte range MUST be a multiple of 320 KiB (327,680 bytes). Using a fragment size that does not divide evenly by 320 KiB will result in errors committing some files.
        //        long range = 327680;
        //        HttpResponseMessage nextResponse = null;
        //        do
        //        {
        //            nextResponse = await UploadPortionFile(uploadUri, from, range, filePath, mCancellationTokenSource.Token);
        //            if (null == nextResponse || !nextResponse.IsSuccessStatusCode || nextResponse.StatusCode != HttpStatusCode.Accepted)
        //            {
        //                break;
        //            }
        //            var nextContent = await nextResponse.Content.ReadAsStringAsync();
        //            JObject nextJo = JObject.Parse(nextContent);
        //            if (!nextJo.ContainsKey("nextExpectedRanges"))
        //            {
        //                break;
        //            }
        //            from = long.Parse(((string)nextJo.SelectToken("nextExpectedRanges[0]")).Split('-')[0]);
        //        } while (null != nextResponse && nextResponse.IsSuccessStatusCode && nextResponse.StatusCode == HttpStatusCode.Accepted);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private async Task<HttpResponseMessage> UploadPortionFile(string uploadUri, long from, long range, string localFilePath, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        using (FileStream fileStream = File.Open(localFilePath, FileMode.Open, FileAccess.ReadWrite))
        //        {
        //            if (fileStream.Length <= 0)
        //            {
        //                new Exception();
        //            }
        //            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Put, uploadUri);
        //            long to = (from + range - 1) > (fileStream.Length - 1) ? (fileStream.Length - 1) : (from + range - 1);
        //            fileStream.Seek(from, SeekOrigin.Begin);
        //            var buffer = new byte[to - from + 1];
        //            var read = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        //            if (read != 0)
        //            {
        //                request.Content = new ByteArrayContent(buffer);
        //                request.Content.Headers.ContentLength = to - from + 1;
        //                request.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, to, fileStream.Length);
        //                var response = await mHttpClient.SendAsync(request);
        //                response.EnsureSuccessStatusCode();
        //                Console.WriteLine(to + "/" + fileStream.Length);
        //                return response;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    return null;
        //}

       // DateTime dateTime = DateTime.Parse(reader["ser_lastModifiedDateTime"].ToString());
       // rt.ModifiedTime = new DateTime(Int64.Parse(JavaTimeConverter.ToCSLongTicks(SkydrmLocal.rmc.common.helper.DateTimeHelper.DateTimeToTimestamp(dateTime)).ToString()));

        //private void Convert(ValueItem item, out database.table.externalrepo.ExternalDriveFile raw)
        //{
        //    database.table.externalrepo.ExternalDriveFile rt = new database.table.externalrepo.ExternalDriveFile();
        //    {
        //        rt.Id = 0;
        //        rt.FileId = item.id;
        //        rt.IsFolder = item.isFolder == 1;
        //        rt.Name = item.name;
        //        rt.Size = item.size;
        //        rt.DisplayPath = item.name;
        //        rt.CloudPathId = item.id;
        //        DateTime dateTime = DateTime.Parse(item.lastModifiedDateTime);
        //        rt.ModifiedTime = new DateTime(Int64.Parse(JavaTimeConverter.ToCSLongTicks(SkydrmLocal.rmc.common.helper.DateTimeHelper.DateTimeToTimestamp(dateTime)).ToString()));
        //        // loal
        //        rt.IsOffline = int.Parse(reader["is_offline"].ToString()) == 1;
        //        rt.IsFavorite = int.Parse(reader["is_favorite"].ToString()) == 1;
        //        rt.LocalPath = reader["local_path"].ToString();
        //        rt.IsNxlFile = int.Parse(reader["is_nxl_file"].ToString()) == 1;
        //        rt.Status = int.Parse(reader["status"].ToString());
        //        rt.CustomString = reader["custom_string"].ToString();
        //        rt.Edit_Status = int.Parse(reader["edit_status"].ToString());
        //        rt.ModifyRightsStatus = int.Parse(reader["modify_rights_status"].ToString());
        //        // resereved
        //        rt.Reserved1 = "";
        //        rt.Reserved2 = "";
        //        rt.Reserved3 = "";
        //        rt.Reserved4 = "";
        //    }
        //    return rt;
        //}

        public IExternalDriveFile[] ListAllFiles()
        {
            try
            {
                var rt = new List<OneDriveFile>();
                foreach (var i in app.DBFunctionProvider.ListOneDriveAllFilesForUI(RepoId))
                {
                    rt.Add(new OneDriveFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveFile[] ListFiles(string cloudPathId)
        {
            try
            {
                var rt = new List<OneDriveFile>();
                foreach (var i in app.DBFunctionProvider.ListOneDriveFileForUI(RepoId, cloudPathId))
                {
                    rt.Add(new OneDriveFile(this, i));// required each new fill do auto-fix 
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveFile[] SyncFiles(string cloudPathId)
        {

            if (string.Equals("/", cloudPathId, StringComparison.CurrentCultureIgnoreCase))
            {
                FolderItem remote_rootFolderItem = sdk.GetRootFolder();
                FolderItem local_rootFolderItem = app.DBFunctionProvider.GetOneDriveRootFolder(RepoId);
                if (null == local_rootFolderItem)
                {
                    // bool res = app.DBFunctionProvider.InsertOneDriveRootFolder(RepoId, remote_rootFolderItem);
                    app.DBFunctionProvider.InsertOneDriveFileBatchEx(RepoId, new List<ValueItem> { remote_rootFolderItem });
                }
                else
                {
                    if (!remote_rootFolderItem.Equals(local_rootFolderItem))
                    {
                        // bool res = app.DBFunctionProvider.UpdateOneDriveFolder(RepoId, remote_rootFolderItem);
                        app.DBFunctionProvider.UpdateOneDriveFileBatchEx(RepoId, new List<ValueItem> { remote_rootFolderItem });
                    }
                    //ValueItem remote_root_item = remote_rootFolderItem;
                    //ValueItem local_root_item = local_rootFolderItem;
                    //if (!remote_root_item.Equals(local_root_item))
                    //{
                    //    bool res = app.DBFunctionProvider.UpdateOneDriveFileCommon(RepoId, remote_rootFolderItem);
                    //}
                }
            }

            OneDriveItem oneDriveItem = sdk.ListChildren(cloudPathId);
            List<ValueItem> remote = oneDriveItem.value;
            List<ValueItem> local = app.DBFunctionProvider.ListOneDriveFile(RepoId, cloudPathId);

            // routine: delete file that had been del on remote but still in local
            var diffset = from i in local
                          let rIds = from j in remote select j.id
                          where !rIds.Contains(i.id)
                          select i;

            foreach (var i in diffset)
            {
                if (i.isFolder == 1)
                {
                    app.DBFunctionProvider.DeleteOneDriveFolderAndAllSubFiles(RepoId, i.id);
                }
                else
                {
                    app.DBFunctionProvider.DeleteOneDriveFile(RepoId,i.id);
                }
            }

            List<ValueItem> newFiles = null;
            List<ValueItem> updateFiles = null;
            FilterOutNotModified(local, remote, out newFiles,out updateFiles);
            app.DBFunctionProvider.InsertOneDriveFileBatchEx(RepoId, newFiles);
            app.DBFunctionProvider.UpdateOneDriveFileBatchEx(RepoId, updateFiles);

            return ListFiles(cloudPathId);
        }

        public IExternalDriveLocalFile[] ListAllLocalFiles()
        {
            try
            {
                var rt = new List<OneDriveLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListOneDriveAllLocalFile(RepoId))
                {
                    rt.Add(new OneDriveLocalFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveLocalFile[] ListLocalFiles(string cloudPathId)
        {
            try
            {
                var rt = new List<OneDriveLocalFile>();
                foreach (var i in app.DBFunctionProvider.ListOneDriveLocalFile(RepoId, cloudPathId))
                {
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteOneDriveLocalFile(i.Id);
                        continue;
                    }
                    rt.Add(new OneDriveLocalFile(this, i));
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IExternalDriveLocalFile AddLocalFile(string cloudPathId, string filePath, List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {

            string newAddedName = string.Empty;
            try
            {
                // tell api to convert this this
                app.Log.Info("protect the file to external repo: " + filePath);

                string outPath = string.Empty;
                if (app.IsPersonRouter)
                {
                    // using myVault defult token.
                    outPath = app.Rmsdk.User.ProtectFile(filePath, rights,
                        waterMark, expiration, new UserSelectTags());
                }
                else
                {
                    // using system bucket token group.
                    outPath = app.Rmsdk.User.ProtectFileToSystemProject(app.SystemProject.Id, filePath,
                    rights, waterMark, expiration, tags);
                }


                newAddedName = Alphaleonis.Win32.Filesystem.Path.GetFileName(outPath);
                var newAddedFileSize = new Alphaleonis.Win32.Filesystem.FileInfo(outPath).Length;

                // store this file into db;
                app.Log.Info("store the new protected file into database");
                app.DBFunctionProvider.InertLocalFileToOneDrive(RepoId, cloudPathId,
                    newAddedName, outPath, (int)newAddedFileSize, Alphaleonis.Win32.Filesystem.File.GetLastAccessTime(outPath));

                // tell service mgr
                app.MessageNotify.NotifyMsg(newAddedName, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Protect_Succeed"),
                    EnumMsgNotifyType.LogMsg, MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.WaitingUpload);

                // return this local file to caller
                return ListLocalFiles(cloudPathId).First((i) =>
                {
                    return i.LocalDiskPath.Equals(outPath);
                });
            }
            catch (Exception e)
            {
                app.Log.Error("Failed to Protect the file" + e.Message, e);

                throw;
            }
        }

        public void Upload(string localPath, string name, string cloudPathId, bool isOverwrite = false, IUploadProgressCallback callback = null)
        {
            string parentItemId = string.Empty;
            if (string.Equals("/", cloudPathId, StringComparison.CurrentCultureIgnoreCase))
            {
                parentItemId = app.DBFunctionProvider.GetOneDriveRootFolder(RepoId)?.id;
            }
            else
            {
                parentItemId = cloudPathId;
            }
            sdk.ResumableUpload(localPath, name, parentItemId, isOverwrite, callback);
        }

        public void OnHeartBeat()
        {
            throw new NotImplementedException();
        }

        public IOfflineFile[] GetOfflines()
        {
            try
            {
                var rt = new List<IOfflineFile>();
                foreach (var i in app.DBFunctionProvider.ListOneDriveOfflineFile(RepoId))
                {
                    if (i.IsOffline && Alphaleonis.Win32.Filesystem.File.Exists(i.LocalPath))
                    {
                        rt.Add(new OneDriveFile(this, i));
                    }
                }
                return rt.ToArray();
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        public IPendingUploadFile[] GetPendingUploads()
        {
            try
            {
                IList<IPendingUploadFile> rt = new List<IPendingUploadFile>();
                foreach (var i in app.DBFunctionProvider.ListOneDriveAllLocalFile(RepoId))
                {
                    // auto fix
                    if (!FileHelper.Exist(i.LocalPath))
                    {
                        app.DBFunctionProvider.DeleteOneDriveLocalFile(i.Id);
                        continue;
                    }
                    if (IsMatchPendingUpload((EnumNxlFileStatus)i.OperationStatus))
                    {
                        rt.Add(new OneDriveLocalFile(this, i));
                    }
                }
                return rt.ToArray();

            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                throw;
            }
        }

        private bool IsMatchPendingUpload(EnumNxlFileStatus status)
        {
            if (status == EnumNxlFileStatus.WaitingUpload ||
                status == EnumNxlFileStatus.Uploading ||
                status == EnumNxlFileStatus.UploadFailed ||
                status == EnumNxlFileStatus.UploadSucceed
                )
            {
                return true;
            }
            return false;
        }

        private void FilterOutNotModified(List<ValueItem> locals, List<ValueItem> remotes,out List<ValueItem> newFiles,out List<ValueItem> updateFiles)
        {
            newFiles = new List<ValueItem>();
            updateFiles = new List<ValueItem>();
            if (locals.Count == 0)
            {
                newFiles = remotes;
                return;
            }
        
            foreach (var i in remotes)
            {
                try
                {
                    // If use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                    {
                        if (i.id != j.id)
                        {
                            return false;
                        }
                        return true;
                    });

                    // If no matching element, will return null.
                    if (l == null)
                    {
                        // remote added node, should add into local
                        newFiles.Add(i);
                        continue;
                    }

                    if (i.isFolder == 1)
                    {
                        if (!((i as FolderItem).Equals(l as FolderItem)))
                        {
                            updateFiles.Add(i);
                        }
                    }else if (i.isFolder == 0)
                    {
                        if (!((i as FileItem).Equals(l as FileItem)))
                        {
                            updateFiles.Add(i);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
    }


    public sealed class OneDriveFile : ExternalDriveFile
    {
        private string repoId;
        private string cacheFolder;
        private string accessToken;
        private OneDriveRestApiSdk sdk;

        public OneDriveFile()
        {
        }

        public OneDriveFile(NxOneDrive host) : base()
        {
            this.repoId = host.RepoId;
            this.accessToken = host.AccessToken;
            InitCacheFolder(host.WorkingPath);
        }

        // Construct from db data(Used for ListFile from db)
        public OneDriveFile(NxOneDrive host, database.table.externalrepo.ExternalDriveFile raw) : base(raw)
        {
            this.repoId = host.RepoId;
            this.accessToken = host.AccessToken;
            this.sdk = host.sdk;
            InitCacheFolder(host.WorkingPath);
        }

        public override void DeleteItem()
        {
        }

        public override void Download()
        {
            if (IsFolder)
            {
                return;
            }

            // Since OneDrive have the same name folder, so we directly download the file
            // into the local directory which is created by its fId.
            var parentFolder = this.cacheFolder + @"\" + FileId;
            FileHelper.CreateDir_NoThrow(parentFolder);
            var localPath = parentFolder + @"\" + Name;

            UpdateStatus(EnumNxlFileStatus.Downloading);
            // delete previous file
            FileHelper.Delete_NoThrow(localPath, true);
            try
            {
                if (Size > 4*1024*1024)
                {
                    sdk.DownloadLargFile(localPath, FileId);
                }
                else
                {
                    sdk.DownloadFile(localPath, FileId);
                }
                // update local path into db
                this.LocalPath = localPath;
                UpdateStatus(EnumNxlFileStatus.DownLoadedSucceed);
            }
            catch (Exception e)
            {
                UpdateStatus(EnumNxlFileStatus.DownLoadedFailed);
                // del 
                FileHelper.Delete_NoThrow(localPath);
                throw e;
            }
        }

        public override void Export(string destFolder)
        {
        }

        public override void RemoveFromLocal()
        {
            try
            {
                if (IsFolder)
                {
                    return;
                }

                // delete local
                if (!Alphaleonis.Win32.Filesystem.File.Exists(this.LocalPath))
                {
                    return;
                }
                try
                {
                    Alphaleonis.Win32.Filesystem.File.Delete(this.LocalPath);
                }
                catch (Exception e)
                {
                    SkydrmApp.Singleton.Log.Error(e.ToString());
                }

                // update file status -- also will update db
                Status = EnumNxlFileStatus.RemovedFromLocal;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e);
                throw;
            }
        }

        protected override void UpdateLocalPath(string localPath)
        {
            if (Raw == null || Raw.LocalPath == localPath)
            {
                return;
            }
            // update db
            SkydrmApp.Singleton.DBFunctionProvider.UpdateOneDriveFileLocalPath(repoId, Raw.FileId, localPath);
            // update cache
            Raw.LocalPath = localPath;
        }

        protected override void UpdateOffline(bool offline)
        {
            if (Raw.IsOffline == offline)
            {
                return;
            }

            SkydrmApp.Singleton.DBFunctionProvider.UpdateOneDriveFileLocalStatusIsOffline(repoId ,Raw.FileId, offline);
            Raw.IsOffline = offline;
        }

        protected override void UpdateStatus(EnumNxlFileStatus newValue)
        {
            // Update vaultfile status in db.
            var app = SkydrmApp.Singleton;
           // app.DBFunctionProvider.UpdateOneDriveFileStatus(Raw.Id, (int)newValue);
            app.DBFunctionProvider.UpdateOneDriveFileStatus(repoId, Raw.FileId, (int)newValue);
            Raw.Status = (int)newValue;
            if (Status == EnumNxlFileStatus.Online)
            {
                IsOffline = false;
            }
            if (Status == EnumNxlFileStatus.AvailableOffline)
            {
                IsOffline = true;
            }
        }

        private void InitCacheFolder(string homePath)
        {
            this.cacheFolder = homePath;
            FileHelper.CreateDir_NoThrow(cacheFolder);
        }
    }

    public sealed class OneDriveLocalFile : ExternalDriveLocalFile
    {
        private SkydrmApp app = SkydrmApp.Singleton;
        private string repoId;
        private string accessToken;
        private OneDriveRestApiSdk sdk;
        private NxOneDrive NxOneDrive;
        public override EnumFileRepo FileRepo => EnumFileRepo.REPO_EXTERNAL_DRIVE;

        public OneDriveLocalFile(NxOneDrive host, database.table.externalrepo.ExternalDriveLocalFile raw) : base(raw)
        {
            this.NxOneDrive = host;
            this.sdk = host.sdk;
        }

        public override void RemoveFromLocal()
        {
            // delete at local disk
            if (FileHelper.Exist(raw.LocalPath))
            {
                FileHelper.Delete_NoThrow(raw.LocalPath);
            }
            else
            {
                app.Log.Warn("file to be del,but not in local, " + raw.LocalPath);
            }

            // remove from database
            app.DBFunctionProvider.DeleteOneDriveLocalFile(raw.Id);
        }

        public override void Upload(bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            if (string.IsNullOrEmpty(PathId))
            {
                throw new Exception("The parent pathId is empty");
            }
            try
            {
                // Invoke
                sdk.ResumableUpload(LocalDiskPath, Name, PathId, isOverWrite, callback);
                // delete from local file db
                app.DBFunctionProvider.DeleteOneDriveLocalFile(raw.Id);

                if (app.User.LeaveCopy)
                {
                    app.User.LeaveCopy_Feature.AddFile(LocalDiskPath);
                    FileHelper.Delete_NoThrow(LocalDiskPath);
                }
            }
            catch (Exception e)
            {
                app.MessageNotify.NotifyMsg(raw.Name, e.Message, EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.WaitingUpload);

                throw e;
            }
        }

        protected override void ChangeOperationStaus(EnumNxlFileStatus status)
        {
            if (raw.OperationStatus == (int)status)
            {
                return;
            }
            // change db
            app.DBFunctionProvider.UpdateOneDriveLocalFileStatus(raw.Id, (int)status);
            // update cache
            raw.OperationStatus = (int)status;
        }

        protected override string GetFileDisplayPath()
        {
            // var folder = app.DBFunctionProvider.QueryOneDriveLocalFileRMSParentFolder(NxOneDrive.RepoId, raw.ExternalDriveFileTablePk);
            return Name;
        }
        protected override string GetFileCloudPathId()
        {
            //string res = string.Empty;
            //if (string.IsNullOrEmpty(PathId))
            //{
            //    res = app.DBFunctionProvider.GetOneDriveItemId(repoId, raw.ExternalDriveFileTablePk);
            //}
            return raw.Reserved1;
        }
    }

    public sealed class OneDriveRestApiSdk
    {
        private string token;
        //private readonly string baseUrl = @"https://graph.microsoft.com/v1.0";
        private readonly string baseUrl = @"https://api.onedrive.com/v1.0";
        private HttpClient httpClient;
        private CancellationTokenSource cancellationTokenSource;

        public OneDriveRestApiSdk(string token)
        {
            this.token = token;
            httpClient = new HttpClient();
            cancellationTokenSource = new CancellationTokenSource();
        }

        public FolderItem GetRootFolder()
        {
            System.Net.Http.HttpResponseMessage response = null;
            FolderItem rootItem = null;
            try
            {
                string url = baseUrl + "/drive/root";
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
                response.EnsureSuccessStatusCode();
                var str = response.Content.ReadAsStringAsync().Result;
                // parse json
                ParseJson(str, out rootItem);
                rootItem.isRootFolder = 1;
                return rootItem;
            }
            catch (Exception ex)
            {
                if (null != response)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new RepoApiException(response.ReasonPhrase, ErrorCode.AccessTokenExpired);
                    }
                    else
                    {
                        throw new RepoApiException(response.ReasonPhrase, ErrorCode.Common);
                    }
                }
                else
                {
                    throw ex;
                }
            }
        }

        public OneDriveItem ListChildren(string itemId)
        {
            System.Net.Http.HttpResponseMessage response = null;
            OneDriveItem children = new OneDriveItem();
            children.value = new List<ValueItem>();
            try
            {
                string url = baseUrl + "/drive/items/{item-id}/children";
                url = url.Replace("{item-id}", string.Equals(itemId, "/", StringComparison.CurrentCultureIgnoreCase) ? "root" : itemId);
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
                response.EnsureSuccessStatusCode();
                var jsonStr = response.Content.ReadAsStringAsync().Result;
                ParseJson(jsonStr, ref children);
                return children;
            }
            catch (Exception ex)
            {
                if (null != response)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new RepoApiException(response.ReasonPhrase, ErrorCode.AccessTokenExpired);
                    }
                    else
                    {
                        throw new RepoApiException(response.ReasonPhrase, ErrorCode.Common);
                    }
                }
                else
                {
                    throw ex;
                }
            }
        }

        private void ParseJson(string jonsContent, out FolderItem folderItem)
        {
            try
            {
                folderItem = JsonConvert.DeserializeObject<FolderItem>(jonsContent);
            }
            catch (Exception ex)
            {
                throw new Exception("Parse json failed.");
            }
        }

        private void ParseJson(string jonsContent, ref OneDriveItem children)
        {
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(jonsContent);
                if (jo.ContainsKey("@odata.context"))
                {
                    children.context = jo["@odata.context"].ToString();
                }
                if (jo.ContainsKey("@odata.count"))
                {
                    children.count = int.Parse(jo["@odata.count"].ToString());
                }
                if (jo.ContainsKey("value"))
                {
                    var ja = jo["value"].ToArray();
                    foreach (var one in ja)
                    {
                        JObject j = (JObject)JsonConvert.DeserializeObject(one.ToString());
                        ValueItemConverter itemConverter = new ValueItemConverter(j.ContainsKey("folder") ? true : false);
                        ValueItem valueItem = JsonConvert.DeserializeObject<ValueItem>(one.ToString(), itemConverter);
                        children.value.Add(valueItem);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Parse json failed.");
            }
        }

        public void DownloadFile(string destinationPath, string itemId)
        {
            System.Net.Http.HttpResponseMessage response;
            try
            {
                string url = baseUrl + "/drive/items/{item-id}/content";
                url = url.Replace("{item-id}", itemId);
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token).Result;
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new RepoApiException(response.ReasonPhrase, ErrorCode.AccessTokenExpired);
                }

                if (response.StatusCode == HttpStatusCode.OK) { 
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    using (var stream =  response.Content.ReadAsStreamAsync().Result)
                    {
                        using (FileStream fs = new FileStream(destinationPath, FileMode.OpenOrCreate))
                        {
                            stream.CopyTo(fs);
                            fs.Flush();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DownloadLargFile(string destinationPath, string itemId)
        {
            MultipleThreadDownload multipleThreadDownload = new MultipleThreadDownload(destinationPath);
            //string url = GraphAPIEndpoint + "/me/drives/c9fa40e83525749e/items/C9FA40E83525749E!120/content";
            string url = baseUrl + "/drive/items/{item-id}/content";
            url = url.Replace("{item-id}", itemId);
            multipleThreadDownload.DownLoad(url,token,itemId);
        }

        private string AutoRename(string fullPath)
        {
            int count = 1;

            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string path = Path.GetDirectoryName(fullPath);
            string newFullPath = fullPath;
            while (System.IO.File.Exists(newFullPath))
            {
                string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                newFullPath = Path.Combine(path, tempFileName + extension);
            }
            return newFullPath;
        }

        public void ResumableUpload(string filePath, string name, string parentId, bool isOverwrite = false, IUploadProgressCallback callback = null)
        {
            System.Net.Http.HttpResponseMessage response = null;
            try
            {
                string url = baseUrl + "/drive/items/{itemId}:/{fileName}:/createUploadSession";
                url = url.Replace("{itemId}", parentId);
                url = url.Replace("{fileName}", name);

                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                //  request.Headers.IfMatch.Add(new EntityTagHeaderValue("aQzlGQTQwRTgzNTI1NzQ5RSExMDQuMA"));
                // request.Headers.IfMatch.TryParseAdd("aQzlGQTQwRTgzNTI1NzQ5RSExMDQuMA");
                string json = @"{
                                'item':{
                                        '@microsoft.graph.conflictBehavior': 'rename'
                                       }
                                }";
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                response =  httpClient.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().Result;
                JObject jo = JObject.Parse(content);
                if (!jo.ContainsKey("uploadUrl") || !jo.ContainsKey("nextExpectedRanges"))
                {
                    return;
                }
                string[] nextExpectedRanges = jo.SelectToken("nextExpectedRanges").Select(s => (string)s).ToArray();
                long from = long.Parse(nextExpectedRanges[0].Split('-')[0]);
                string uploadUri = (string)jo.SelectToken("['uploadUrl']");
                //Note: If your app splits a file into multiple byte ranges, the size of each byte range MUST be a multiple of 320 KiB (327,680 bytes). Using a fragment size that does not divide evenly by 320 KiB will result in errors committing some files.
                long range = 327680;
                HttpResponseMessage nextResponse = null;
                do
                {
                    nextResponse = UploadFileChunk(uploadUri, from, range, filePath, cancellationTokenSource.Token, callback);
                    if (null == nextResponse || !nextResponse.IsSuccessStatusCode || nextResponse.StatusCode != HttpStatusCode.Accepted)
                    {
                        break;
                    }
                    var nextContent = nextResponse.Content.ReadAsStringAsync().Result;
                    JObject nextJo = JObject.Parse(nextContent);
                    if (!nextJo.ContainsKey("nextExpectedRanges"))
                    {
                        break;
                    }
                    from = long.Parse(((string)nextJo.SelectToken("nextExpectedRanges[0]")).Split('-')[0]);
                } while (null != nextResponse && nextResponse.IsSuccessStatusCode && nextResponse.StatusCode == HttpStatusCode.Accepted);
                callback?.OnComplete(true, filePath, null);
            }
            catch (Exception ex)
            {
                if (null != response)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        callback?.OnComplete(false, filePath, new RepoApiException("OneDrive upload failed.", ErrorCode.AccessTokenExpired));
                    }
                    else
                    {
                        callback?.OnComplete(false, filePath, new RepoApiException("OneDrive upload failed.", ErrorCode.Common));
                    }
                }
                else
                {
                    callback?.OnComplete(false, filePath, new RepoApiException("OneDrive upload failed.", ErrorCode.Common));
                }
            }
        }

        private HttpResponseMessage UploadFileChunk(string uploadUri, long from, long range, string localFilePath, CancellationToken cancellationToken, IUploadProgressCallback callback = null)
        {
            try
            {
                using (FileStream fileStream = System.IO.File.Open(localFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    if (fileStream.Length <= 0)
                    {
                        new Exception();
                    }
                    var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Put, uploadUri);
                    long to = (from + range - 1) > (fileStream.Length - 1) ? (fileStream.Length - 1) : (from + range - 1);
                    fileStream.Seek(from, SeekOrigin.Begin);
                    var buffer = new byte[to - from + 1];
                    var read =  fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).Result;
                    if (read != 0)
                    {
                        request.Content = new ByteArrayContent(buffer);
                        request.Content.Headers.ContentLength = to - from + 1;
                        request.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, to, fileStream.Length);
                        var response = httpClient.SendAsync(request).Result;
                        response.EnsureSuccessStatusCode();
                        callback?.OnProgress(to, fileStream.Length);
                        Console.WriteLine(to + "/" + fileStream.Length);
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

    }
}
