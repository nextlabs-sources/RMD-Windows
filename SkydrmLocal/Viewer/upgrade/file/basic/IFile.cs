using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.application;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.session;

namespace Viewer.upgrade.file.basic
{
    public interface IFile
    {
        string FileName { get; }
        string Extention { get; }
        string FilePath { get; }
     
        EnumFileType FileType { get; }
        //UInt64 StatusCode { get; }
        void Delete();
    }
}
