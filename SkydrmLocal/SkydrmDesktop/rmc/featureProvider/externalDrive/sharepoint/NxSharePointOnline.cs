using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.sdk;
using static SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint.ListFileResult;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    class NxSharePointOnline : NxSharePointBase
    {
        public NxSharePointOnline(IRmsRepo repo) : base(repo)
        {
            WorkingPath = SkydrmApp.Singleton.User.WorkingFolder + "\\SharePointOnline\\" + repo.RepoId;
            Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(WorkingPath);
        }

        protected override IExternalDriveFile NewByDBItem(SharePointDriveFile file)
        {
            return new SharePointOnlineFile(this, file);
        }

        protected override IExternalDriveLocalFile NewByDBLocalItem(SharePointDriveLocalFile file)
        {
            return new SharePointOnlineLocalFile(this, file);
        }

        public override IExternalDriveFile[] SyncInternal(string pathId, bool site)
        {
            var remotes = ListRemoteFile(AccessToken, GetCloudPathIdByPathId(pathId), site);
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
                SharePointOnlineFile file = new SharePointOnlineFile(this);
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

        protected override string DownloadFile(string pathId, string localPath, bool overwrite,
            int start, long totalLength, bool bPartialDownload,
            IProgress<HttpDownloadProgress> callback,
            CancellationToken cancellation)
        {
            return DownloadRemoteFile(pathId, AccessToken, localPath, overwrite, totalLength, callback, cancellation);
        }

        protected override string UploadFile(string pathId, string fileName, string localpath, bool overwrite)
        {
            return UploadRemoteFile(pathId, AccessToken, fileName, localpath, overwrite);
        }

        public ListFileResult ListRemoteFile(string accessToken, string pathId, bool site)
        {
            try
            {
                if ("/".Equals(pathId))
                {
                    return LoadRoot(mServerSite, accessToken);
                }
                if (site)
                {
                    return LoadChildSiteAndLists(pathId, accessToken);
                }
                return LoadChildFoldersAndLists(pathId, accessToken);
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

        public string DeleteFile(string pathId)
        {
            return DeleteRemoteFileAsync(pathId, AccessToken);
        }

        private ListFileResult LoadRoot(string serverSite, string accessToken)
        {
            string rootFolder = GetResources(GetRootUrl(serverSite, false), accessToken);
            string rootSites = GetResources(GetRootUrl(serverSite, true), accessToken);

            ListFileResult folderResult = ListFileResult.ParseRoots(rootFolder, false);
            ListFileResult sitesResult = ListFileResult.ParseRoots(rootSites, true);
            return ListFileResult.MergeResults(folderResult, sitesResult);
        }

        private ListFileResult LoadChildSiteAndLists(string cloudPahId, string accessToken)
        {
            string childSites = GetResources(GetChildSitesUrl(cloudPahId), accessToken);
            string fileLists = GetResources(GetFileListsUrl(cloudPahId), accessToken);

            ListFileResult sitesResult = ListFileResult.ParseRoots(childSites, true);
            ListFileResult fileListsResult = ListFileResult.ParseRoots(fileLists, false);
            return ListFileResult.MergeResults(sitesResult, fileListsResult);
        }

        private ListFileResult LoadChildFoldersAndLists(string cloudPahId, string accessToken)
        {
            string folders = GetResources(GetFoldersUrl(cloudPahId), accessToken);
            string files = GetResources(GetFilesUrl(cloudPahId), accessToken);

            ListFileResult folderResult = ListFileResult.ParseChildFolders(folders);
            ListFileResult listFileResult = ListFileResult.ParseChildFiles(files);
            return ListFileResult.MergeResults(folderResult, listFileResult);
        }

        private string DownloadRemoteFile(string url, string accessToken,
            string localPath, bool overwrite, long totalLength,
            IProgress<HttpDownloadProgress> progress,
            CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new RepoApiException("Fatal error, param accessToken must not be null.", ErrorCode.ParamInvalid);
            }

            using (NxHttpClient client = new NxHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                client.DefaultRequestHeaders.Add("ContentType", "application/json;odata=verbose;charset=utf-8");

                return client.Download(new Uri(url), localPath, overwrite, totalLength, progress, cancellation);
            }
        }

        private string UploadRemoteFile(string pathId, string accessToken, string fileName, string localPath, bool overwrite)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new RepoApiException("Fatal error, param accessToken must not be null.", ErrorCode.ParamInvalid);
            }

            using (NxHttpClient client = new NxHttpClient())
            {
                string digest = CreateDigest(mServerSite, accessToken);

                var files = File.ReadAllBytes(localPath);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                client.DefaultRequestHeaders.Add("X-RequestDigest", digest);

                var url = "";
                if (string.Equals("/", pathId))
                {
                    url = GetUploadFileUrl(mServerSite, fileName, overwrite);
                }
                else
                {
                    url = GetUploadFileUrl(pathId, fileName, overwrite);
                }

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

        private string GetResources(string url, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new RepoApiException("Fatal error, param accessToken must not be null.", ErrorCode.ParamInvalid);
            }

            using (NxHttpClient client = new NxHttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

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

        private string DeleteRemoteFileAsync(string pathId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new RepoApiException("Fatal error, param accessToken must not be null.", ErrorCode.ParamInvalid);
            }

            using (NxHttpClient client = new NxHttpClient())
            {
                string digest = CreateDigest(mServerSite, accessToken);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
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

        private string CreateDigest(string serverSite, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new RepoApiException("Fatal error, param accessToken must not be null.", ErrorCode.ParamInvalid);
            }

            using (NxHttpClient client = new NxHttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
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
            return @"https://nextlabstest.sharepoint.com/skydrm01/";
        }

    }
}
