using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.sdk;
using static SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint.ListFileResult;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    class NxSharePointOnPremise : NxSharePointBase
    {
        private readonly string mUserName;
        private readonly string mPassWord;

        public NxSharePointOnPremise(IRmsRepo repo) : base(repo)
        {
            var username = QueryUserName(repo.RepoId);
            var password = QueryPassWord(repo.RepoId);
            mUserName = string.IsNullOrEmpty(username) ? "john.tyler" : username;
            mPassWord = string.IsNullOrEmpty(password) ? "john.tyler" : password;

            WorkingPath = SkydrmApp.Singleton.User.WorkingFolder + "\\SharePointOnPremise\\" + repo.RepoId;
            Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(WorkingPath);
        }

        public static bool TryAuth(string serversite, string username, string password)
        {
            var rt = GetCurrentUsrInfo(serversite, username, password);
            return !string.IsNullOrEmpty(rt);
        }

        protected override IExternalDriveFile NewByDBItem(SharePointDriveFile file)
        {
            return new SharePointOnPremiseFile(this, file);
        }

        protected override IExternalDriveLocalFile NewByDBLocalItem(SharePointDriveLocalFile file)
        {
            return new SharePointOnPremiseLocalFile(this, file);
        }

        public override IExternalDriveFile[] SyncInternal(string pathId, bool site)
        {
            var remotes = ListRemoteFile(GetCloudPathIdByPathId(pathId), site);
            if (remotes == null)
            {
                return new IExternalDriveFile[0];
            }
            var children = remotes.GetChildren();
            if (children == null)
            {
                return new IExternalDriveFile[0];
            }
            var rt = new List<IExternalDriveFile>();
            foreach (var item in children)
            {
                if (item == null)
                {
                    continue;
                }
                SharePointOnPremiseFile file = new SharePointOnPremiseFile(this);
                {
                    file.FileId = item.CloudPathId;
                    file.Name = item.Name;
                    file.IsFolder = item is FolderResult;
                    file.IsSite = item is SiteResult;
                    file.ModifiedTme = item.LastModifiedTime;
                    file.Size = item.Size;
                    file.LocalPath = "";

                    if (string.IsNullOrEmpty(item.Name))
                    {
                        file.IsNxlFile = false;
                    }
                    else
                    {
                        file.IsNxlFile = item.Name.EndsWith(".nxl") ? true : false;
                    }
                    if (item is FolderResult || item is SiteResult)
                    {
                        file.DisplayPath = pathId + item.Name + "/";
                        file.CloudPathId = (pathId + item.Name + "/").ToLower();
                        SetCloudPathId(file.CloudPathId, item.CloudPathId);
                        if (item is SiteResult)
                        {
                            SetSite(file.CloudPathId, true);
                        }
                    }
                    else
                    {
                        file.DisplayPath = pathId + item.Name;
                        file.CloudPathId = (pathId + item.Name).ToLower();
                    }
                };

                rt.Add(file);
            }
            return rt.ToArray();
        }

        protected override string UploadFile(string pathId, string fileName, string localpath, bool overwrite)
        {
            return UploadRemoteFile(pathId, mUserName, mPassWord, fileName, localpath, overwrite);
        }

        protected override string DownloadFile(string pathId, string localPath, bool overwrite,
            int start, long totalLength, bool bPartialDownload,
            IProgress<HttpDownloadProgress> callback, CancellationToken cancellation)
        {
            return DownloadRemoteFile(pathId, mUserName, mPassWord, localPath, overwrite, totalLength, callback, cancellation);
        }

        public ListFileResult ListRemoteFile(string pathId, bool site)
        {
            try
            {
                if ("/".Equals(pathId))
                {
                    return LoadRoot(mServerSite, mUserName, mPassWord);
                }
                if (site)
                {
                    return LoadChildSiteAndLists(pathId, mUserName, mPassWord);
                }
                return LoadChildFoldersAndLists(pathId, mUserName, mPassWord);
            }
            catch (RepoApiException e)
            {
                if (e.ErrorCode == ErrorCode.NotFound)
                {
                    //SkydrmApp.Singleton.Log.Error(e.Message, e);
                    return null;
                }
                else
                {
                    throw e;
                }
            }
        }

        public void DeleteFile(string pathId)
        {
            DeleteRemoteFileAsync(pathId, mUserName, mPassWord);
        }

        private string QueryUserName(string repoId)
        {
            return SkydrmApp.Singleton.DBFunctionProvider
                .QueryExternalRepoReserved2(repoId);
        }

        private string QueryPassWord(string repoId)
        {
            return SkydrmApp.Singleton.DBFunctionProvider
                .QueryExternalRepoReserved3(repoId);
        }

        private ListFileResult LoadRoot(string serverSite, string username, string password)
        {
            string rootFolder = GetResources(GetRootUrl(serverSite, false), username, password);
            string rootSites = GetResources(GetRootUrl(serverSite, true), username, password);

            ListFileResult folderResult = ListFileResult.ParseRoots(rootFolder, false);
            ListFileResult sitesResult = ListFileResult.ParseRoots(rootSites, true);

            return ListFileResult.MergeResults(folderResult, sitesResult);
        }

        private ListFileResult LoadChildSiteAndLists(string cloudPahId, string username, string password)
        {
            string childSites = GetResources(GetChildSitesUrl(cloudPahId), username, password);
            string fileLists = GetResources(GetFileListsUrl(cloudPahId), username, password);
            ListFileResult sitesResult = ListFileResult.ParseRoots(childSites, true);
            ListFileResult fileListsResult = ListFileResult.ParseRoots(fileLists, false);
            return ListFileResult.MergeResults(sitesResult, fileListsResult);
        }

        private ListFileResult LoadChildFoldersAndLists(string cloudPahId, string username, string password)
        {
            string folders = GetResources(GetFoldersUrl(cloudPahId), username, password);
            string files = GetResources(GetFilesUrl(cloudPahId), username, password);

            ListFileResult folderResult = ListFileResult.ParseChildFolders(folders);
            ListFileResult listFileResult = ListFileResult.ParseChildFiles(files);
            return ListFileResult.MergeResults(folderResult, listFileResult);
        }

        public string DownloadRemoteFile(string url, string username, string password,
            string localPath, bool overwrite, long totalLength,
            IProgress<HttpDownloadProgress> callback,
            CancellationToken cancellation)
        {
            using (NTLMHttpClient client = new NTLMHttpClient(url, username, password))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                client.DefaultRequestHeaders.Add("ContentType", "application/json;odata=verbose;charset=utf-8");

                return client.Download(new Uri(url), localPath, overwrite, totalLength, callback, cancellation);
            }
        }

        private string UploadRemoteFile(string pathId, string username, string password, string fileName, string localPath, bool overwrite)
        {
            using (NTLMHttpClient client = new NTLMHttpClient(pathId, username, password))
            {
                string digest = CreateDigest(mServerSite, username, password);

                var files = File.ReadAllBytes(localPath);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                client.DefaultRequestHeaders.Add("X-RequestDigest", digest);

                var url = GetUploadFileUrl(pathId, fileName, overwrite);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                using (ByteArrayContent content = new ByteArrayContent(files))
                {
                    request.Content = content;
                    var response = client.SendAsync(request).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            throw new RepoApiException("You are not authorized to perform this aciton.",
                                ErrorCode.AccessTokenExpired);
                        }
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            throw new RepoApiException("Not found.",
                                ErrorCode.NotFound);
                        }
                        if (response.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            throw new RepoApiException("Internal sever error.",
                                ErrorCode.InternalServerError);
                        }
                        throw new RepoApiException(response.RequestMessage.Content.ToString());
                    }
                    using (Stream responseStream = response.Content.ReadAsStreamAsync().Result)
                    {
                        using (StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }

        private static string GetCurrentUsrInfo(string serversite, string usrname, string password)
        {
            return GetResources(GetCurrentUserUrl(serversite), usrname, password);
        }

        private static string GetResources(string url, string username, string password)
        {
            using (NTLMHttpClient client = new NTLMHttpClient(url, username, password))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                client.DefaultRequestHeaders.Add("ContentType", "application/json;odata=verbose;charset=utf-8");

                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new RepoApiException("You are not authorized to perform this aciton.",
                                ErrorCode.AccessTokenExpired);
                    }
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new RepoApiException("Not found.",
                                ErrorCode.NotFound);
                    }
                    if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        throw new RepoApiException("Internal sever error.",
                            ErrorCode.InternalServerError);
                    }
                    throw new RepoApiException(response.RequestMessage.Content.ToString());
                }

                using (Stream responseStream = response.Content.ReadAsStreamAsync().Result)
                {
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")))
                    {
                        return reader.ReadToEnd();
                    }
                }

            }
        }

        private string DeleteRemoteFileAsync(string pathId, string username, string password)
        {
            using (NTLMHttpClient client = new NTLMHttpClient(pathId, username, password))
            {
                string digest = CreateDigest(mServerSite, username, password);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                client.DefaultRequestHeaders.Add("ContentType", "application/json;odata=verbose;charset=utf-8");
                client.DefaultRequestHeaders.Add("X-HTTP-Method", "DELETE");
                client.DefaultRequestHeaders.Add("IF-MATCH", "*");
                client.DefaultRequestHeaders.Add("X-RequestDigest", digest);


                var response = client.SendAsync(new HttpRequestMessage(HttpMethod.Post, pathId)).Result;
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new RepoApiException("You are not authorized to perform this aciton.",
                                ErrorCode.AccessTokenExpired);
                    }
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new RepoApiException("Not found.",
                                ErrorCode.NotFound);
                    }
                    if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        throw new RepoApiException("Internal sever error.",
                            ErrorCode.InternalServerError);
                    }
                    throw new RepoApiException(response.RequestMessage.Content.ToString());
                }
                using (Stream responseStream = response.Content.ReadAsStreamAsync().Result)
                {
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        private string CreateDigest(string serverSite, string username, string password)
        {
            using (NTLMHttpClient client = new NTLMHttpClient(serverSite, username, password))
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                client.DefaultRequestHeaders.Add("ContentType", "application/json");
                client.DefaultRequestHeaders.Add("ContentLength", "0");
                string cmd = "/_api/contextinfo";
                StringContent httpContent = new StringContent("");

                var response = client.PostAsync(serverSite + cmd, httpContent).Result;
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new RepoApiException("You are not authorized to perform this aciton.",
                                ErrorCode.AccessTokenExpired);
                    }
                    if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        throw new RepoApiException("Internal sever error.",
                            ErrorCode.InternalServerError);
                    }
                    throw new RepoApiException(response.RequestMessage.Content.ToString());
                }
                string results = response.Content.ReadAsStringAsync().Result;
                JObject responseObj = JObject.Parse(results);
                if (!(responseObj["d"] is JObject dObj))
                {
                    return "";
                }
                if (!(dObj["GetContextWebInformation"] is JObject webInforObj))
                {
                    return "";
                }
                return webInforObj["FormDigestValue"].ToString();
            }
        }

        public override string GetDefaultServerSite()
        {
            return @"http://rms-sp2013.qapf1.qalab01.nextlabs.com/sites/iosdev/";
        }

        class NTLMHttpClient : NxHttpClient
        {

            public NTLMHttpClient(string url, string username, string password) : base(new HttpClientHandler()
            {
                Credentials = new CredentialCache()
                {
                    {
                        new Uri(url),
                        "NTLM",
                        new NetworkCredential(username,password)
                    }
                }
            })
            {

            }
        }
    }
}
