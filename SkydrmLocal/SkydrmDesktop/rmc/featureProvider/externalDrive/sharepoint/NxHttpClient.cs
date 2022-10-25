using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    public class NxHttpClient : HttpClient
    {
        private const int BufferSize = 8192;
        private readonly HttpClientHandler mHandler;

        public NxHttpClient()
        {

        }

        public NxHttpClient(HttpClientHandler handler) : base(handler)
        {
            mHandler = handler;
        }

        public string Download(
            Uri requestUri,
            string localPath,
            bool overwrite,
            long totalLenth,
            IProgress<HttpDownloadProgress> callback,
            CancellationToken cancellationToken)
        {
            if (!overwrite && File.Exists(localPath))
            {
                throw new RepoApiException(string.Format("File {0} already exists.", localPath), ErrorCode.FileAlreadyExist);
            }

            using (var response = GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result)
            {
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
                var content = response.Content;
                if (content == null)
                {
                    return "";
                }

                var headers = content.Headers;
                var contentLength = headers.ContentLength;

                using (var responseStream = content.ReadAsStreamAsync().Result)
                {
                    var downloadProgress = new HttpDownloadProgress();
                    if (contentLength.HasValue)
                    {
                        downloadProgress.TotalBytesToReceive = contentLength.Value;
                    }
                    else
                    {
                        // set default value.
                        if (totalLenth == 0)
                        {
                            downloadProgress.TotalBytesToReceive = 0x10000;
                        }
                        else
                        {
                            downloadProgress.TotalBytesToReceive = totalLenth;
                        }
                    }
                    callback?.Report(downloadProgress);

                    using (var fs = Alphaleonis.Win32.Filesystem.File.Open(localPath, FileMode.OpenOrCreate))
                    {
                        var buffer = new byte[BufferSize];
                        int bytesRead;

                        while ((bytesRead = responseStream.ReadAsync(buffer, 0, BufferSize, cancellationToken).Result) > 0)
                        {
                            fs.Write(buffer, 0, bytesRead);

                            downloadProgress.BytesReceived += bytesRead;
                            callback?.Report(downloadProgress);
                        }
                    }
                }
                return localPath;
            }
        }
    }
}
