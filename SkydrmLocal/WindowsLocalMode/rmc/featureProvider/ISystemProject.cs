using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.sdk;

namespace SkydrmLocal.rmc.featureProvider
{
    // A new added idea in 01/22/2019
    // each RMS have only one SystemProject
    // by current, if user want to protect file at local(in-place protection), use ISystemProject as defualt data-source
    public interface ISystemProject : IHeartBeat
    {
        // RMS may do not have this feature, so out UI must hide it.
        bool IsFeatureEnabled { get; }
        int Id { get; }
        bool IsEnableAdHoc { get; }
        string TenantId { get; }
        // once after portection file to nxl, does it need to delete the plain file 
        bool IsNeedDeleteSourceFile { get; }

        ProjectClassification[] GetClassifications();
        string ProtectFileAdhoc(string PlainFilePath, string DestFolder, List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration);
        string ProtectFileCentrolPolicy(string PlainFilePath, string DestFolder, UserSelectTags tags);
    }
}
