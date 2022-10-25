using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SkydrmDesktop.rmc.featureProvider.OneDrive
{
    public class MultipleThreadDownload
    {
        private const int mThreadSize = 3;
        private long mFileTotalContentLenght = 0;
        private long mHaveDownloadedContentLenght = 0;
        private Task<long>[] mSubTask = new Task<long>[mThreadSize];
        private object mState;
        private System.Net.Http.HttpClient mHttpClient;
        private CancellationTokenSource mCancellationTokenSource;
        private string mCacheDir = Environment.CurrentDirectory + "\\DownloadCache";
        private bool mDownloadCache = false;
        private string mDestinationPath;
        private string mUrl;
        private string mAccessToken;
        private UInt64 _Flag = 0x00;
        private UInt64 _Pause = 0x01;
        private UInt64 _Cancle = 0x02;
        public string CacheDir { get { return mCacheDir; } set { mCacheDir = value; } }
        public bool DownloadCache { get { return mDownloadCache; } set { mDownloadCache = value; } }
        public event Action<object, long> Progress;

        public MultipleThreadDownload(string destinationPath)
        {
            mDestinationPath = destinationPath;
            ServicePointManager.DefaultConnectionLimit = 3;
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            mHttpClient = new System.Net.Http.HttpClient();
            mCancellationTokenSource = new CancellationTokenSource();
            mHttpClient.Timeout = new TimeSpan(TimeSpan.TicksPerDay);
        }

        public void Pause()
        {
            _Flag |= _Pause;
            if (mCancellationTokenSource.Token.CanBeCanceled)
            {
                mCancellationTokenSource.Cancel();
            }
        }

        public void Resume()
        {
            if (((_Flag & _Pause) == _Pause) || ((_Flag & _Cancle) == _Cancle))
            {
                _Flag ^= _Flag;
                mCancellationTokenSource = new CancellationTokenSource();
                DownLoad(mUrl, mAccessToken, mState);
            }
        }

        public void Cancle()
        {
            _Flag |= _Cancle;
            if (mCancellationTokenSource.Token.CanBeCanceled)
            {
                mCancellationTokenSource.Cancel();
            }
        }

        public string DownLoad(string url,string accessToken, object state)
        {
            mUrl = url;
            mAccessToken = accessToken;
            mState = state;
            System.Net.Http.HttpResponseMessage response;
            try
            {
                HttpRequestMessage httpRequestMessage = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                httpRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                response = mHttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, mCancellationTokenSource.Token).Result;
                response.EnsureSuccessStatusCode();
                if (mCancellationTokenSource.Token.IsCancellationRequested)
                {
                    return string.Empty;
                }
                mFileTotalContentLenght = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : -1;
                if (mFileTotalContentLenght <= 0)
                {
                    throw new RepoApiException("File lenght cannot be less zero",ErrorCode.IllegalOperation);
                }
                // string destinationPath = Path.Combine(mDestinationDir, response.Content.Headers.ContentDisposition.FileNameStar);
                FileStream localFile = File.Open(mDestinationPath, FileMode.OpenOrCreate);
                localFile.SetLength(mFileTotalContentLenght);
                localFile.Close();
                long block = mFileTotalContentLenght % mThreadSize == 0 ? mFileTotalContentLenght / mThreadSize : mFileTotalContentLenght / mThreadSize + 1;
                if (mDownloadCache)
                {
                    mCacheDir += "\\" + Path.GetFileNameWithoutExtension(response.Content.Headers.ContentDisposition.FileNameStar);
                    if (!Directory.Exists(mCacheDir))
                    {
                        Directory.CreateDirectory(mCacheDir);
                    }
                    for (int threadId = 0; threadId < mThreadSize; threadId++)
                    {
                        if (!File.Exists(Path.Combine(mCacheDir, threadId + ".txt")))
                        {
                            StreamWriter streamWriter = File.CreateText(Path.Combine(mCacheDir, threadId + ".txt"));
                            streamWriter.Close();
                        }
                    }
                }
                for (int threadId = 0; threadId < mThreadSize; threadId++)
                {
                    mSubTask[threadId] = DownLoadBlock(mDestinationPath, httpRequestMessage.RequestUri, block, threadId);
                }
                // Task.WaitAll(mSubTask);
                long[] vs = Task.WhenAll<long>(mSubTask).Result;
                return mDestinationPath;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private Task<long> DownLoadBlock(string file, Uri uri, long block, int threadId)
        {
           return new TaskFactory<long>().StartNew(()=> {
               long totalRead = 0;
               try
                {
                   if (mDownloadCache)
                   {
                       totalRead = ReadDownloadedSize(threadId);
                       Interlocked.Add(ref mHaveDownloadedContentLenght, totalRead);
                   }
                    var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, uri);
                    long start = threadId * block + totalRead;
                    long end = (threadId + 1) * block - 1;
                    if (start > end)
                    {
                       throw new RepoApiException("Index start cannot be greater-than index end", ErrorCode.IllegalOperation);
                    }
                    string aa = string.Format("File name:{0}, thread Id:{1}, index start:{2}, index end:{3}", Path.GetFileName(file), threadId, start, end);
                    Console.WriteLine(string.Format("File name:{0}, thread Id:{1}, index start:{2}, index end:{3}", Path.GetFileName(file), threadId, start, end));
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
                    HttpResponseMessage response = null;
                    response = mHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, mCancellationTokenSource.Token).Result;
                    response.EnsureSuccessStatusCode();
                    if (mCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return totalRead;
                    }
                    using (var stream = response.Content.ReadAsStreamAsync().Result)
                    {
                        using (FileStream destinationFile = File.Open(file, FileMode.Open, FileAccess.Write, FileShare.Write))
                        {
                            destinationFile.Seek(start, SeekOrigin.Begin);
                            var buffer = new byte[1024*8*10];
                            int readLenght = 0;
                            while ((readLenght = stream.ReadAsync(buffer, 0, buffer.Length).Result) != 0)
                            {
                                destinationFile.Write(buffer, 0, readLenght);
                                destinationFile.Flush();
                                totalRead += readLenght;
                                if (mDownloadCache)
                                {
                                    WriteDownFilesize(totalRead, threadId);
                                }
                                Interlocked.Add(ref mHaveDownloadedContentLenght, readLenght);
                                readLenght = 0;
                                Console.WriteLine("test");
                                string tt = string.Format(string.Format("File name:{0}, thread Id:{1}, haveDownloadedContentLenght:{2}--->FileTotalContentLenght:{3}", Path.GetFileName(file), threadId, mHaveDownloadedContentLenght, mFileTotalContentLenght));
                                Console.WriteLine(string.Format(string.Format("File name:{0}, thread Id:{1}, haveDownloadedContentLenght:{2}--->FileTotalContentLenght:{3}", Path.GetFileName(file), threadId, mHaveDownloadedContentLenght, mFileTotalContentLenght)));
                                Progress?.Invoke(mState, mHaveDownloadedContentLenght);
           
                                if (mCancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    if ((_Flag & _Pause) == _Pause)
                                    {
                                        break;
                                    }
                                    else if ((_Flag & _Cancle) == _Cancle)
                                    {
                                        if (mDownloadCache)
                                        {
                                            CleanCache();
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return totalRead;
            }, TaskCreationOptions.LongRunning);

        }

        private void CleanCache()
        {
            if (Directory.Exists(mCacheDir))
            {
                DeleteDirectory(mCacheDir);
            }
        }

        private void DeleteDirectory(string directoryPath)
        {
            try
            {
                string[] allFilePath = Directory.GetFiles(directoryPath);

                foreach (string filePath in allFilePath)
                {
                    File.Delete(filePath);
                }

                string[] allSubdirectory = Directory.GetDirectories(directoryPath);

                foreach (string subDirectoryPath in allSubdirectory)
                {
                    DeleteDirectory(subDirectoryPath);
                }

                Directory.Delete(directoryPath);
            }
            catch (Exception ex)
            {
               
            }
        }

        public void WriteDownFilesize(long i, int threadId)
        {
            using (StreamWriter streamWriter = File.CreateText(Path.Combine(mCacheDir, threadId + ".txt")))
            {
                 streamWriter.Write(i + "");
            }
        }

        public long ReadDownloadedSize(int threadId)
        {
            long downloadedSize = 0;
            using (StreamReader streamReader = File.OpenText(Path.Combine(mCacheDir, threadId + ".txt")))
            {
                String line = streamReader.ReadLine();
                streamReader.Close();
                if (!string.IsNullOrEmpty(line))
                {
                    downloadedSize = long.Parse(line);
                }
                return downloadedSize;
            }
        }
    }
}
