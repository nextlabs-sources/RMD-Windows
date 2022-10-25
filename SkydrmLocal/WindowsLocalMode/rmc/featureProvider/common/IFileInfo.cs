using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.ui.windows.FileInformationWindow;

namespace SkydrmLocal.rmc.featureProvider
{
    /**
    All nxl file must support this get-type attribute for UI to display,include:
        ProjectRMS/Local 
        MyVualtRMS/Local 
        SharedWithMe
    12/14/2018 by current rm-sdk design, if a user is NOT allowed to have access for nxl file,he cannot 
    open nxl file in sdk, for this case any info peculiar to Nxl i.e.
        isOwner,
        rights, 
        expiration, 
        watermartk,
        tag
    will throw out  InsufficientRights excpetion
    
    **/
    // 
    public interface IFileInfo
    {
        string Name{ get;}

        long Size { get;}

        bool IsOwner { get; }

        bool IsByAdHoc { get; }

        bool IsByCentrolPolicy { get; }

        DateTime LastModified { get;}  // windows-UTC

        string LocalDiskPath { get;}  // file stored at local disk

        string RmsRemotePath { get;}  // file in RMS remote, like /aaa/bbb/ccc/ddd.txt.nxl

        bool IsCreatedLocal { get;}   // ture for local added file, flase RMS remote file 
      
        string[] Emails { get;}     // shared to who, or who share this file to you

        FileRights[] Rights { get;}

        string WaterMark { get;}

        Expiration Expiration { get;}

        Dictionary<string, List<string>> Tags { get;}

        string RawTags { get; }

        EnumFileRepo FileRepo { get;}

        IFileInfo Update();
    }
        

}
