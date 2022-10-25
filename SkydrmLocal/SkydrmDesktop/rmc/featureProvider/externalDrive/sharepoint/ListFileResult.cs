using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    public class ListFileResult
    {
        /**
 * {
 * "__metadata": {
 * "id": "https://nextlabsdev.sharepoint.com/sites/rmscentos7303new/_api/Web/Lists(guid'3d8aaa0f-f42e-492e-9421-26d8f7feab61')",
 * "uri": "https://nextlabsdev.sharepoint.com/sites/rmscentos7303new/_api/Web/Lists(guid'3d8aaa0f-f42e-492e-9421-26d8f7feab61')",
 * "etag": "\"4\"",
 * "type": "SP.List"
 * },
 * "RootFolder": {
 * "__deferred": {
 * "uri": "https://nextlabsdev.sharepoint.com/sites/rmscentos7303new/_api/Web/Lists(guid'3d8aaa0f-f42e-492e-9421-26d8f7feab61')/RootFolder"
 * }
 * },
 * "Created": "2018-05-31T08:43:05Z",
 * "Title": "Documents"
 * }
 *
 * @param rt
 * @param file
 * @param results
 * @param sites
 */
        private readonly List<ResultBase> mChildren;

        private ListFileResult(List<ResultBase> children)
        {
            this.mChildren = children;
        }

        public List<ResultBase> GetChildren()
        {
            return mChildren;
        }

        private static void FillParams(ResultBase f,
                               string pathId,
                               string pathDisplay,
                               string cloudPath,
                               string cloudPathId,
                               string name,
                               long size,
                               string timeStr)
        {
            f.PathId = pathId;
            f.PathDisplay = pathDisplay;
            f.CloudPath = cloudPath;
            f.CloudPathId = cloudPathId;
            f.Name = name;
            f.Size = size;
            if (string.IsNullOrEmpty(timeStr))
            {
                f.LastModifiedTime = DateTime.Now;
            }
            else
            {
                f.LastModifiedTime = DateTime.Parse(timeStr);
            }
        }


        public static ListFileResult ParseRoots(string results, bool sites)
        {
            JObject resultsObj = JObject.Parse(results);
            if (resultsObj == null)
            {
                return null;
            }
            if (!(resultsObj["d"] is JObject dObj))
            {
                return null;
            }
            if (!(dObj["results"] is JArray resultsArr))
            {
                return null;
            }
            List<ResultBase> children = new List<ResultBase>();
            foreach (var itemObj in resultsArr)
            {
                if (itemObj == null)
                {
                    continue;
                }

                string created = itemObj["Created"].ToString();
                string title = itemObj["Title"].ToString();
                string pathDisplay = "/" + title + "/";
                string pathId = pathDisplay.ToLower();
                string cloudPathId = "";
                var metadataObj = itemObj["__metadata"];
                if (metadataObj != null)
                {
                    cloudPathId = metadataObj["uri"].ToString();
                }

                if (sites)
                {
                    SiteResult site = new SiteResult();
                    FillParams(site, pathId, pathDisplay, cloudPathId + "/RootFolder",
                        cloudPathId + "/RootFolder", title, 0, created);
                    children.Add(site);
                }
                else
                {
                    FolderResult folder = new FolderResult();
                    FillParams(folder, pathId, pathDisplay, cloudPathId + "/RootFolder",
                        cloudPathId + "/RootFolder", title, 0, created);
                    children.Add(folder);
                }
            }

            return new ListFileResult(children); ;
        }

        public static ListFileResult ParseChildFolders(string results)
        {
            JObject resultsObj = JObject.Parse(results);
            if (resultsObj == null)
            {
                return null;
            }
            if (!(resultsObj["d"] is JObject dObj))
            {
                return null;
            }
            if (!(dObj["results"] is JArray resultsArr))
            {
                return null;
            }
            List<ResultBase> children = new List<ResultBase>();
            foreach (var itemObj in resultsArr)
            {
                if (itemObj == null)
                {
                    continue;
                }
                string cloudPath = "";
                if (itemObj["__metadata"] is JObject metadataObj)
                {
                    var uri = metadataObj["uri"];
                    if (uri != null)
                    {
                        cloudPath = uri.ToString();
                    }
                }

                string name = itemObj["Name"].ToString();
                string pathDisplay = "/" + name + "/";
                string pathId = pathDisplay.ToLower();

                string lastModifiedTime = null;
                var timeObj = itemObj["TimeLastModified"];
                if (timeObj != null)
                {
                    lastModifiedTime = timeObj.ToString();
                }

                string cloudPathId = itemObj["ServerRelativeUrl"].ToString();

                FolderResult folder = new FolderResult();
                FillParams(folder, pathId, pathDisplay, cloudPathId, cloudPath, name, 0, lastModifiedTime);
                children.Add(folder);
            }

            return new ListFileResult(children);
        }

        public static ListFileResult ParseChildFiles(string results)
        {
            JObject resultsObj = JObject.Parse(results);
            if (resultsObj == null)
            {
                return null;
            }
            if (!(resultsObj["d"] is JObject dObj))
            {
                return null;
            }
            if (!(dObj["results"] is JArray resultsArr))
            {
                return null;
            }
            List<ResultBase> children = new List<ResultBase>();
            foreach (var itemObj in resultsArr)
            {
                if (itemObj == null)
                {
                    continue;
                }
                string cloudPath = "";
                if (itemObj["__metadata"] is JObject metadataObj)
                {
                    cloudPath = metadataObj["uri"].ToString();
                }
                string name = itemObj["Name"].ToString();
                string pathDisplay = "/" + name;
                string pathId = pathDisplay.ToLower();
                long.TryParse(itemObj["Length"].ToString(), out long length);
                string timeLastModified = itemObj["TimeLastModified"].ToString();
                string cloudPathId = itemObj["ServerRelativeUrl"].ToString();

                FileResult doc = new FileResult();
                FillParams(doc, pathId, pathDisplay, cloudPathId, cloudPath, name, length, timeLastModified);
                children.Add(doc);
            }

            return new ListFileResult(children);
        }

        public static ListFileResult MergeResults(ListFileResult r1, ListFileResult r2)
        {
            List<ResultBase> children = new List<ResultBase>();
            if (r1 != null)
            {
                children.AddRange(r1.GetChildren());
            }
            if (r2 != null)
            {
                children.AddRange(r2.GetChildren());
            }
            return new ListFileResult(children);
        }

        public abstract class ResultBase
        {
            private string pathId;
            private string pathDisplay;
            private string cloudPath;
            private string cloudPathId;
            private string name;
            private long size;
            private DateTime lastModifiedTime;

            public string PathId { get => pathId; set => pathId = value; }
            public string PathDisplay { get => pathDisplay; set => pathDisplay = value; }
            public string CloudPath { get => cloudPath; set => cloudPath = value; }
            public string CloudPathId { get => cloudPathId; set => cloudPathId = value; }
            public string Name { get => name; set => name = value; }
            public long Size { get => size; set => size = value; }
            public DateTime LastModifiedTime { get => lastModifiedTime; set => lastModifiedTime = value; }
        }

        public class FolderResult : ResultBase
        {

        }

        public sealed class SiteResult : FolderResult
        {

        }

        public sealed class FileResult : ResultBase
        {

        }
    }


}
