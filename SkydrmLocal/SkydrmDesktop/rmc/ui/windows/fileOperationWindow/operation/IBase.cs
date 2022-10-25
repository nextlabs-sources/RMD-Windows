using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    /// <summary>
    /// FileOperationWindow base interface
    /// </summary>
    public interface IBase
    {
        FileAction FileAction { get; }
        OperateFileInfo FileInfo { get; set; }
    }
}
