using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.file.basic.utils;

namespace Viewer.upgrade.file.basic
{
    public interface INxlFile : IFile
    {
        string Duid { get; }
        bool Expired { get; }
        Int32 Dirstatus { get; }
        WatermarkInfo WatermarkInfo { get; }
        NxlFileFingerPrint NxlFileFingerPrint { get; }
        void Share(System.Windows.Window Owner);
        void Print(System.Windows.Window Owner);
        void Export(System.Windows.Window Owner);
        void Extract(System.Windows.Window Owner);
        void FileInfo(System.Windows.Window Owner);
        string Decrypt(string outputFileName = "", bool removeTimestamp = true);
        void Edit(Action<bool> EditSaved, Action ProcessExited);
        bool CanFileInfo();
        bool CanShare();
        bool CanPrint();
        bool CanExport();
        bool CanExtract();
        bool CanEdit();
        void ClearTempFiles();
    }
}
