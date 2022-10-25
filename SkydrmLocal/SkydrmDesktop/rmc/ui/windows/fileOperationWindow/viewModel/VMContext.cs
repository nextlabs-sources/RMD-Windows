using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    class VMContext
    {
        private BaseViewModel viewModel;

        public VMContext(IBase operation, FileOperationWin win)
        {
            switch (operation.FileAction)
            {
                case FileAction.Protect:
                    viewModel = new ProtectViewModel((IProtect)operation, win);
                    break;
                case FileAction.Share:
                    viewModel = new ProtectAndShareViewModel((IProtectAndShare)operation, win);
                    break;
                case FileAction.ViewFileInfo:
                    break;
                case FileAction.UpdateRecipients:
                    viewModel = new UpdateRecipientViewModel((IUpdateRecipients)operation, win);
                    break;
                case FileAction.ReShare:
                    viewModel = new ReShareViewModel((IReShare)operation, win);
                    break;
                case FileAction.ReShareUpdate:
                    viewModel = new ReShareUpdateViewModel((IReShareUpdate)operation, win);
                    break;
                case FileAction.AddFileTo:
                    viewModel = new AddNxlFileViewModel((IAddNxlFile)operation, win);
                    break;
                case FileAction.ModifyRights:
                    viewModel = new ModifyNxlFileRightViewModel((IModifyNxlFileRight)operation, win);
                    break;
                case FileAction.UploadFile:
                    viewModel = new UploadFileViewModel((IUpload)operation, win);
                    break;
                case FileAction.SpecialProtect:
                    viewModel = new SpecialProtectViewModel((ISpecialProtect)operation, win);
                    break;
            }
        }

        public BaseViewModel GetViewModel()
        {
            return viewModel;
        }
    }
}
