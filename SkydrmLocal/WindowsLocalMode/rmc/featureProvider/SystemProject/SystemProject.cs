using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.sdk;
using Newtonsoft.Json;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database.table.systembucket;

namespace SkydrmLocal.rmc.featureProvider.SystemProject
{
    public class SystemProject : ISystemProject
    {

        static SkydrmLocalApp App = SkydrmLocalApp.Singleton;
        SystemBucket raw;

        public SystemProject(SystemBucket raw)
        {
            this.raw = raw;
        }

        public int Id => raw.SystemBucketRMSId;

        public bool IsEnableAdHoc => raw.IsEnableAdhoc;

        public string TenantId => raw.SystemBucketRMSTenantId;

        // by comments from Raymond, if RMS not support systemProject TenantId is null or empty. 
        // for fix bug 57266 , if server is Personal account, not support systemProject
        public bool IsFeatureEnabled => IsEnableSystemBucket();

        public bool IsNeedDeleteSourceFile => App.Rmsdk.User.GetIsDeleteSource();


        public void OnHeartBeat()
        {
            // get params
            int sbId = App.Rmsdk.User.GetSystemProjectId();
            string sbTenant = App.Rmsdk.User.GetSystemProjectTenantId();
            bool sbIsEnableAdhoc = App.Rmsdk.User.IsEnabledAdhocForSystemBucket();
            ProjectClassification[] classifications = App.Rmsdk.User.GetProjectClassification(sbTenant);
            string sbClassification = JsonConvert.SerializeObject(classifications);
            // update db
            App.DBFunctionProvider.UpsertSystemBucket(sbId, sbTenant, sbClassification, sbIsEnableAdhoc);
            // update local
            raw = App.DBFunctionProvider.GetSystemBucket();
        }


        public ProjectClassification[] GetClassifications()
        {
            return JsonConvert.DeserializeObject<ProjectClassification[]>(raw.ClassificationJson);

            //return JsonConvert.DeserializeObject<ProjectClassification[]>(
            //    App.DBFunctionProvider.GetProjectClassification(Id));
            //List<ProjectClassification> rt = new List<ProjectClassification>();

            //ProjectClassification c1 = new ProjectClassification()
            //{
            //    name = "t111111111111111111",
            //    isMandatory = true,
            //    isMultiSelect = true,
            //    labels = new Dictionary<string, bool>()
            //    {
            //        { "aaa",true },
            //        { "bbb",false},
            //        { "ccc",false},
            //        { "ddd",false},
            //    },
            //};

            //ProjectClassification c2 = new ProjectClassification()
            //{
            //    name = "t2222222222222222222222222222222222",
            //    isMandatory = false,
            //    isMultiSelect = false,
            //    labels = new Dictionary<string, bool>()
            //    {
            //        { "aaa",true },
            //        { "bbb",false},
            //        { "ccc",false},
            //        { "ddd",false},
            //    },
            //};

            //ProjectClassification c3 = new ProjectClassification()
            //{
            //    name = "t333333333333333333333333333",
            //    isMandatory = false,
            //    isMultiSelect = true,
            //    labels = new Dictionary<string, bool>()
            //    {
            //        { "aaa",true },
            //        { "bbb",false},
            //        { "ccc",false},
            //        { "ddd",false},
            //    },
            //};

            //rt.Add(c1);rt.Add(c2); rt.Add(c3);

            //return rt.ToArray();
        }

        public string ProtectFileAdhoc(string PlainFilePath, string DestFolder, List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration)
        {
            App.Log.Info("Protect systemBucket adhoc file:"+ PlainFilePath);
            string outpath = SkydrmLocalApp.Singleton.Rmsdk.User.ProtectFileToSystemProject(
                Id, PlainFilePath, DestFolder,
                rights, waterMark, expiration, new UserSelectTags());

            //return FileHelper.DoAfterProtect(PlainFilePath, outpath, DestFolder, IsNeedDeleteSourceFile);
            return FileHelper.DoAfterProtect(PlainFilePath, outpath, DestFolder, false);
        }



        public string ProtectFileCentrolPolicy(string PlainFilePath, string DestFolder, UserSelectTags tags)
        {
            App.Log.Info("Protect systemBucket CentrolPolicy file:" + PlainFilePath);
            List<FileRights> defaultRights;
            WaterMarkInfo defaultWatermark;
            Expiration defaultExpiration;

            GenerateDefaultValue(out defaultRights, out defaultWatermark, out defaultExpiration);


            string outpath = SkydrmLocalApp.Singleton.Rmsdk.User.ProtectFileToSystemProject(
                Id, PlainFilePath, DestFolder,
                defaultRights, defaultWatermark, defaultExpiration,
                tags);

            //return FileHelper.DoAfterProtect(PlainFilePath, outpath, DestFolder, IsNeedDeleteSourceFile);
            return FileHelper.DoAfterProtect(PlainFilePath, outpath, DestFolder, false);
        }

        private void GenerateDefaultValue(out List<FileRights> defaultRights,
            out WaterMarkInfo defaultWatermark,
            out Expiration defaultExpiration)
        {
            defaultRights = new List<FileRights>();

            defaultWatermark = new WaterMarkInfo()
            {
                fontColor = "",
                fontName = "",
                text = "",
                fontSize = 0,
                repeat = 0,
                rotation = 0,
                transparency = 0
            };
            defaultExpiration = new Expiration()
            {
                type = ExpiryType.NEVER_EXPIRE,
                Start = 0,
                End = 0
            };
        }

        private bool CheckIsAdhocEnabled()
        {
            // by current All Project get a single button whether turn on adhoc, so check any proj other than system default
            foreach (var i in App.MyProjects.List())
            {
                return i.IsEnableAdHoc;
            }

            return true; // by default show adhoc
        }

        // fix bug 57266
        private bool IsEnableSystemBucket()
        {
            bool result = true;
            
            string routUrl = App.DBFunctionProvider.GetCurrentRouterUrl();

            if (App.Config.Router.Equals(routUrl,StringComparison.CurrentCultureIgnoreCase))
            {
                result = false;
            }
            return result;
        }


    }
}
