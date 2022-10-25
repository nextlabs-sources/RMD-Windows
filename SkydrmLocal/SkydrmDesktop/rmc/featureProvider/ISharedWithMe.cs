using SkydrmLocal.rmc.common.interfaces;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.featureProvider
{
    public interface ISharedWithMe: IHeartBeat, ILocalFile
    {
        string Dir { get; }
        ISharedWithMeFile[] Sync();
        ISharedWithMeFile[] List();
        
    }

    public interface ISharedWithMeFile
    {
        string Name { get; }
        string Duid { get; }
        string Type { get; } // "txt","mp3"
        long FileSize { get; }
        DateTime SharedDate { get; }
        string SharedBy { get; }  // who share file to you 
        string SharedLinkeUrl { get; } // a url you can view it online
        FileRights[] Rights { get; } 
        string Comments { get; }
        bool IsOwner { get; }
        bool IsOffline { get; set; }
        string LocalDiskPath { get; }   // if offlined
        string PartialLocalPath { get; }
        EnumNxlFileStatus Status { get; set; }
        bool IsEdit { get; set; }
        bool IsModifyRights { get; set; }
        IFileInfo FileInfo { get; }
        string TransactionId { get; }
        void Download(bool isForViewOnly=true);
        void DownloadPartial();
        void Export(string destinationFolder);
        // once the file has been downloaded, user can delete local 
        void Remove();
        bool ShareFile(string nxlLocalPath, string[] addEmails, string[] removeEmails, string comment);
    }
}

