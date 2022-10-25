using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Skydrmlocal.rmc.database2.FunctionProvider;
using static SkydrmLocal.rmc.sdk.User;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr
{
    public class RmsRepoMgr : IRmsRepoMgr
    {
        private SkydrmApp App;
        private log4net.ILog log;
        public RmsRepoMgr(SkydrmApp app)
        {
            this.App = app;
            this.log = app.Log;
        }

        #region Impl IRmsRepoMgr
        public List<IRmsRepo> ListRepositories()
        {
            try
            {
                var rt = new List<IRmsRepo>();
                var retDB = App.DBFunctionProvider.ListRepositories();
                foreach(var i in retDB)
                {
                    rt.Add(new RmsRepo(i));
                }
                return rt;
            }
            catch (Exception e)
            {
                App.Log.Error(e.Message, e);
                throw;
            }
        }

        public List<IRmsRepo> SyncRepositories()
        {
            // remote
            var remotes = App.Rmsdk.User.ListRepositories();
            // local
            var locals = ListRepositories();

            // Will do some merge as following 1 & 2.
            // 1. delete repo that had been deleted on remote but still in local --> local also should delete them.
            var diffset = from i in locals
                          let rIds = from j in remotes select j.repoId
                          where !rIds.Contains(i.RepoId)
                          select i;

            foreach(var i in diffset)
            {
                App.DBFunctionProvider.DeleteRepository(i.RepoId);
            }

            // 2. remote added\modified some repos but local don't ---> local also should added\modified them.
            var ff = new List<InsertRepoInfo>();
            foreach(var f in FilterAddedOrModifiedInRemote(locals.ToArray(), remotes))
            {
                // Now don't support personal account, filter it.
                if (f.providerClass == "PERSONAL")
                {
                    continue;
                }

                ff.Add(new InsertRepoInfo() {
                    isShared = (int)f.isShared,
                    isDefault = (int)f.isDefault,
                    createTime = (long)f.createTime,
                    updateTime = (long)f.updateTime,
                    repoid = f.repoId,
                    name = f.name,
                    type = f.type,
                    providerClass = f.providerClass,
                    accountName = f.accountName,
                    accountId = f.accountId,
                    token = f.token,
                    preference = f.preference
                });
            }

            // Insert\Update to db
            App.DBFunctionProvider.UpsertRepoInfoBatch(ff.ToArray());

            return ListRepositories();
        }

        // The json string like the following:
        /*
        {
            "repoId":"e8eb1c55-c4e8-45e6-a273-5ec140c3cbd2",
            "name":"DBDocs",
            "type":"DROPBOX",
            "isShared":false,
            "accountName":"xxxxxx@nextlabs.com",
            "accountId":"2xxxxxxxxxxxxxxxxxxxxxx",
            "creationTime":1470122537982
         } */
        public IRmsRepo AddRepository(string resultJson)
        {
            // todo.
            throw new NotImplementedException();
        }

        public void RemoveRepository(string repoid)
        {
            if(App.Rmsdk.User.RemoveRepository(repoid))
            {
                App.DBFunctionProvider.DeleteRepository(repoid);
            }
        }

        public string GetAccessToken(string repoid)
        {
            try
            {
                string rt = App.Rmsdk.User.GetRepositoryAccessToken(repoid);

                // update into db if acquired succeed. --- Now don't save token into db for security reason.
                //
                //App.DBFunctionProvider.UpdateRepoToken(repoid, rt);

                return rt;
            }
            catch (Exception e)
            {
                // Todo -- Should special throw 5005(token expired) exception and upper level should handle this.
                // throw e;
            }

            return "";
        }

        public string GetAuthorizationURI(string name, ExternalRepoType type, string authUrl="")
        {
            try
            {
                return App.Rmsdk.User.GetRepositoryAuthorizationUrl(name, ConvertTypeEnum2String(type), authUrl);
            }
            catch (Exception e)
            {
                throw;
            }
        }


        public void UpdateRepoName(string repoid, string name)
        {
            bool rt = App.Rmsdk.User.UpdateRepository(repoid, "", name);
            if (!rt)
            {
                throw new Exception("UpdateRepository name failed.");
            }
        }

        public void UpdateRepoToken(string repoID, string token)
        {
            if (App.Rmsdk.User.UpdateRepository(repoID, token, ""))
            {
                App.DBFunctionProvider.UpdateRepoToken(repoID, token);
            } else
            {
                throw new Exception("UpdateRepository token failed.");
            }
        }
        #endregion // Impl IRmsRepoMgr

        #region private methods
        private RepositoryInfo[] FilterAddedOrModifiedInRemote(IRmsRepo[] locals, RepositoryInfo[] remotes)
        {
            if (locals.Length == 0)
            {
                return remotes;
            }

            var rt = new List<RepositoryInfo>();
            foreach (var i in remotes)
            {
                try
                {
                    // If use Enumerable.First(), will throw exception when no matching element. 
                    // It will cause blocking UI when switch treeView item during uploading many files.
                    var l = locals.FirstOrDefault((j) =>
                    {
                        if (i.repoId != j.RepoId)
                        {
                            return false;
                        }
                        return true;
                    });

                    // If no matching element, will return null.
                    if (l == null)
                    {
                        App.Log.Info("Rms repo local list no matching element");
                        // remote added node, should add into local
                        rt.Add(i);
                        continue;
                    }

                    // Modified in remote, local node should also update.
                    if (i.name != l.DisplayName ||
                        i.token != l.Token)
                    {
                        rt.Add(i);
                    }

                }
                catch (Exception e)
                {
                    App.Log.Error(e);
                    // local find error
                    rt.Add(i);
                }
            }

            return rt.ToArray();
        }

        private RepositoryType ConvertType(ExternalRepoType type)
        {
            switch (type)
            {
                case ExternalRepoType.BOX:
                    return RepositoryType.Box;
                case ExternalRepoType.DROPBOX:
                    return RepositoryType.DROPBOX;
                case ExternalRepoType.GOOGLEDRIVE:
                    return RepositoryType.GOOGLE_DRIVE;
                case ExternalRepoType.SHAREPOINT:
                    return RepositoryType.SHAREPOINT;
                case ExternalRepoType.SHAREPOINT_ONLINE:
                    return RepositoryType.SHAREPOINT_ONLINE;
                case ExternalRepoType.SHAREPOINT_ONPREMISE:
                    return RepositoryType.SHAREPOINT_ONPREEMISE;
                default:
                    break;
            }
            // Can't reach here
            throw new Exception("Occur error");
        }

        private string ConvertTypeEnum2String(ExternalRepoType enumType)
        {
            string ret = string.Empty;
            switch (enumType)
            {
                case ExternalRepoType.BOX:
                    ret = "BOX";
                    break;
                case ExternalRepoType.DROPBOX:
                    ret = "DROPBOX";
                    break;
                case ExternalRepoType.GOOGLEDRIVE:
                    ret = "GOOGLE_DRIVE";
                    break;
                case ExternalRepoType.ONEDRIVE:
                    ret = "ONE_DRIVE";
                    break;
                case ExternalRepoType.SHAREPOINT:
                    ret = "SHAREPOINT";
                    break;
                case ExternalRepoType.SHAREPOINT_ONLINE:
                    ret = "SHAREPOINT_ONLINE";
                    break;
                case ExternalRepoType.SHAREPOINT_ONPREMISE:
                    ret = "SHAREPOINT_ONPREMISE";
                    break;
                case ExternalRepoType.LOCAL_DRIVE:
                    ret = "LOCAL_DRIVE";
                    break;
            }

            return ret;
        }

        #endregion // private methods
    }


    public class RmsRepo : IRmsRepo
    {
        private database.table.externalrepo.RmsExternalRepo raw;

        public RmsRepo(database.table.externalrepo.RmsExternalRepo r)
        {
            this.raw = r;
        }

        public string RepoId => raw.RepoId;

        public ExternalRepoType Type { get => Converter(raw.Type); }

        public string ProviderClass { get => raw.ProviderClass; }

        public string DisplayName { get => raw.Name; set => UpdateDisplayName(value); }

        public bool IsShared => raw.IsShared;

        public bool IsDefault => raw.IsDefault;

        public string AccountName => raw.AccountName;

        public string AccountId => raw.AccountId;

        public string Token { get => raw.Token; set => UpdateToken(value); }

        public DateTime CreationTime => raw.CreationTime;

        public DateTime UpdateTime => raw.UpdateTime;

        public string Preference => raw.Preference;

        #region private
        private void UpdateToken(string newValue)
        {
            if(raw.Token == newValue)
            {
                return;
            }

            raw.Token = newValue;
            // Update into db  --- now don't save token into db.
            //SkydrmApp.Singleton.DBFunctionProvider.UpdateRepoToken(raw.Id, newValue);
        }

        private void UpdateDisplayName(string name)
        {
            if(raw.Name == name)
            {
                return;
            }

            raw.Name = name;
            SkydrmApp.Singleton.DBFunctionProvider.UpdateRepoName(raw.Id, name);
        }

        private ExternalRepoType Converter(string type)
        {
            if (type == "GOOGLE_DRIVE")
                return ExternalRepoType.GOOGLEDRIVE;
            else if (type == "BOX")
                return ExternalRepoType.BOX;
            else if (type == "DROPBOX")
                return ExternalRepoType.DROPBOX;
            else if (type == "ONE_DRIVE")
                return ExternalRepoType.ONEDRIVE;
            else if (type == "SHAREPOINT")
                return ExternalRepoType.SHAREPOINT;
            else if (type == "SHAREPOINT_ONLINE")
                return ExternalRepoType.SHAREPOINT_ONLINE;
            else if (type == "SHAREPOINT_ONPREMISE")
                return ExternalRepoType.SHAREPOINT_ONPREMISE;
            else if (type == "LOCAL_DRIVE")
                return ExternalRepoType.LOCAL_DRIVE;
            else
                return ExternalRepoType.UNKNOWN;
                
        }

        #endregion // private
    }
}
