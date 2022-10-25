using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Viewer.upgrade.application;
using Viewer.upgrade.communication.namedPipe.client;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.statusCode;
using Viewer.upgrade.ui.nxl.page.toolbar.view;
using Viewer.upgrade.utils;
using static Viewer.upgrade.communication.namedPipe.client.NamedPipeClient;
using static Viewer.upgrade.utils.NetworkStatus;

namespace Viewer.upgrade.ui.nxl.page.toolbar.viewModel
{
    public class ViewModel : INotifyPropertyChanged, ISensor
    {
        private INxlFile mNxlFile;
        private ViewerApp mApplication;
        private log4net.ILog mLog;
        private string mFileName;
        private UInt64 mStatusCode;
        private bool mIsNetworkAvailable;
        private ToolBarPage mToolBarPage;
        private Window mParentWindow;
        private Window mMainWindow;

        // private DelegateCommand mCloseCommand;
        private DelegateCommand mLeftRotateCommand;
        private DelegateCommand mRightRotateCommand;
        private DelegateCommand mResetCommand;
        private DelegateCommand mExtractCommand;
        private DelegateCommand mExportCommand;
        private DelegateCommand mEditCommand;
        private DelegateCommand mPrintCommand;
        private DelegateCommand mFileInfoCommand;
        private DelegateCommand mShareCommand;
        private DelegateCommand mProtectCommand;

        private bool mEnableEditBt = true;

        //public DelegateCommand CloseCommand
        //{
        //    get
        //    {
        //        return mCloseCommand;
        //    }
        //    set
        //    {
        //        mCloseCommand = value;
        //        OnPropertyChanged("CloseCommand");
        //    }
        //}
        public DelegateCommand LeftRotateCommand
        {
            get
            {
                return mLeftRotateCommand;
            }
            set
            {
                mLeftRotateCommand = value;
                OnPropertyChanged("LeftRotateCommand");
            }
        }
        public DelegateCommand RightRotateCommand
        {
            get
            {
                return mRightRotateCommand;
            }
            set
            {
                mRightRotateCommand = value;
                OnPropertyChanged("RightRotateCommand");
            }
        }

        public DelegateCommand ResetCommand
        {
            get
            {
                return mResetCommand;
            }
            set
            {
                mResetCommand = value;
                OnPropertyChanged("ResetCommand");
            }
        }

        public DelegateCommand ExtractCommand
        {
            get
            {
                return mExtractCommand;
            }
            set
            {
                mExtractCommand = value;
                OnPropertyChanged("ExtractCommand");
            }
        }
        public DelegateCommand ExportCommand
        {
            get
            {
                return mExportCommand;
            }
            set
            {
                mExportCommand = value;
                OnPropertyChanged("ExportCommand");
            }
        }
        public DelegateCommand EditCommand
        {
            get
            {
                return mEditCommand;
            }
            set
            {
                mEditCommand = value;
                OnPropertyChanged("EditCommand");
            }
        }
        public DelegateCommand PrintCommand
        {
            get
            {
                return mPrintCommand;
            }
            set
            {
                mPrintCommand = value;
                OnPropertyChanged("PrintCommand");
            }
        }
        public DelegateCommand FileInfoCommand
        {
            get
            {
                return mFileInfoCommand;
            }
            set
            {
                mFileInfoCommand = value;
                OnPropertyChanged("FileInfoCommand");
            }
        }
        public DelegateCommand ShareCommand
        {
            get
            {
                return mShareCommand;
            }
            set
            {
                mShareCommand = value;
                OnPropertyChanged("ShareCommand");
            }
        }
        public DelegateCommand ProtectCommand
        {
            get
            {
                return mProtectCommand;
            }
            set
            {
                mProtectCommand = value;
                OnPropertyChanged("ProtectCommand");
            }
        }
        public Window ParentWindow
        {
            set { mParentWindow = value; }
            get { return mParentWindow; }
        }
        public string FileName
        {
            get
            {
                return mFileName;
            }
            set
            {
                mFileName = value;
                OnPropertyChanged("FileName");
            }
        }
        public UInt64 StatusCode
        {
            get
            {
                return mStatusCode;
            }
            set
            {
                mStatusCode = value;
                OnPropertyChanged("StatusCode");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action OnClickFileInfo;
        public event Action OnClickPrint;
        public event Action OnClickEdit;
        public event Action OnClickShare;
        public event Action OnClickExport;
        public event Action OnClickExtract;
        public event Action OnClickRightRotate;
        public event Action OnClickLeftRotate;
        public event Action OnClickProtect;
        public event Action OnClickReset;


        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ViewModel(INxlFile nxlFile, ToolBarPage toolBarPage)
        {
            mApplication = (ViewerApp)ViewerApp.Current;
            mLog = mApplication.Log;
            mNxlFile = nxlFile;
            mToolBarPage = toolBarPage;
            _Initialize();
        }

        public void _Initialize()
        {
            MonitorNetworkSituation();
            InitializeTitle();
            InitializeBtn();
            InitiaCommands();
        }

        public void Page_Loaded()
        {
            mMainWindow = Window.GetWindow(mToolBarPage);
        }

        private void MonitorNetworkSituation()
        {
            mIsNetworkAvailable = NetworkStatus.IsAvailable;
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
        }

        private void InitializeTitle()
        {
            FileName = mNxlFile.FileName;
        }

        private void InitiaCommands()
        {
           // CloseCommand = new DelegateCommand(ExecuteClose, CanExecuteClose);
            LeftRotateCommand = new DelegateCommand(ExecuteLeftRotate, CanExecuteLeftRotate);
            RightRotateCommand = new DelegateCommand(ExecuteRightRotate, CanExecuteRightRotate);
            ResetCommand = new DelegateCommand(ExecuteReset, CanExecuteReset);
            ExtractCommand = new DelegateCommand(ExecuteExtract, CanExecuteExtract);
            ExportCommand = new DelegateCommand(ExecuteExport, CanExecuteExport);
            EditCommand = new DelegateCommand(ExecuteEdit, CanExecuteEdit);
            PrintCommand = new DelegateCommand(ExecutePrint, CanExecutePrint);
            FileInfoCommand = new DelegateCommand(ExecuteFileInfo, CanExecuteFileInfo);
            ShareCommand = new DelegateCommand(ExecuteShare, CanExecuteShare);
            ProtectCommand = new DelegateCommand(ExecuteProtect, CanExecuteProtect);
        }

        public void RaiseCanExecute()
        {
            LeftRotateCommand.RaiseCanExecuteChanged();
            RightRotateCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();
            ExtractCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
            EditCommand.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
            FileInfoCommand.RaiseCanExecuteChanged();
            ShareCommand.RaiseCanExecuteChanged();
            ProtectCommand.RaiseCanExecuteChanged();
        }

        private bool CanExecuteProtect(object arg)
        {
            return GeneralCheck();
        }

        private bool CanExecuteShare(object arg)
        {
            return GeneralCheck();
        }

        private bool CanExecuteFileInfo(object arg)
        {
            return GeneralCheck();
        }

        private bool CanExecutePrint(object arg)
        {
            if (GeneralCheck())
            {
                if (mNxlFile.FileType != EnumFileType.FILE_TYPE_AUDIO && mNxlFile.FileType != EnumFileType.FILE_TYPE_VIDEO)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        private bool CanExecuteEdit(object arg)
        {
            if (GeneralCheck())
            {
                if (!mEnableEditBt)
                {
                    return false;
                }

                if (mNxlFile.FileType == EnumFileType.FILE_TYPE_OFFICE)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        private bool CanExecuteExport(object arg)
        {
            return GeneralCheck();
        }

        private bool CanExecuteExtract(object arg)
        {
            return GeneralCheck();
        }

        private bool CanExecuteRightRotate(object arg)
        {
            if (GeneralCheck() && mNxlFile.FileType == EnumFileType.FILE_TYPE_IMAGE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CanExecuteLeftRotate(object arg)
        {
            if (GeneralCheck() && mNxlFile.FileType == EnumFileType.FILE_TYPE_IMAGE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool GeneralCheck()
        {
            bool result = false;
            if (null != ParentWindow)
            {
                result = true;
            }
            return result;
        }

        public void ExecuteShare(object obj)
        {
            OnClickShare?.Invoke();
            mNxlFile.Share(ParentWindow);
        }
        public void ExecuteFileInfo(object obj)
        {
            OnClickFileInfo?.Invoke();
            mNxlFile.FileInfo(ParentWindow);
        }
        public void ExecutePrint(object obj)
        {
            OnClickPrint?.Invoke();
        }
        public void ExecuteEdit(object obj)
        {
            //try
            //{
            //  mEnableEditBt = false;
            //  mNxlFile.Edit(EditSaved);
            //  OnClickEdit?.Invoke();
            //}
            //catch (Exception ex)
            //{
            //    mLog.Error(ex.Message);
            //}
            //finally
            //{
            //    mEnableEditBt = true;
            //}

            try
            {
                mEnableEditBt = false;
                OnClickEdit?.Invoke();
            }
            catch (Exception ex)
            {
                mLog.Error(ex.Message);
            }
            finally
            {
                mEnableEditBt = true;
            }
        }
  
        //public void EditSaved(bool b)
        //{
        //    // Notify RMD to update file status and do sync.
        //    try
        //    {
        //        Bundle<EditCallBack> bundle = new Bundle<EditCallBack>()
        //        {
        //            Intent = Intent.SyncFileAfterEdit,
        //            obj = new EditCallBack(b, mNxlFile.FilePath)

        //        };
        //        string json = JsonConvert.SerializeObject(bundle);
        //        NamedPipeClient.Start(json);
        //    }
        //    catch (Exception ex)
        //    {
        //        mLog.Error(ex.ToString());
        //    }

        //    ShutdownApplication();
        //}

        //private void ShutdownApplication()
        //{
        //    mApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
        //    {
        //        if (ShutdownMode.OnExplicitShutdown == mApplication?.ShutdownMode)
        //        {
        //            mApplication?.Shutdown();
        //        }
        //        else
        //        {
        //            mMainWindow.Close();
        //        }
        //    }));
        //}

        public void ExecuteExport(object obj)
        {
            OnClickExport?.Invoke();
            mNxlFile.Export(ParentWindow);
        }
        public void ExecuteExtract(object obj)
        {
            OnClickExtract?.Invoke();
            mNxlFile.Extract(ParentWindow);
        }
        public void ExecuteRightRotate(object obj)
        {
             OnClickRightRotate?.Invoke();
        }
        public void ExecuteLeftRotate(object obj)
        {
            OnClickLeftRotate?.Invoke();
        }

        public void ExecuteReset(object obj)
        {
            OnClickReset?.Invoke();
        }

        private bool CanExecuteReset(object arg)
        {
            if (GeneralCheck() && mNxlFile.FileType == EnumFileType.FILE_TYPE_IMAGE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //public bool CanExecuteClose(object arg)
        //{
        //    return true;
        //}
        //public void ExecuteClose(object obj)
        //{
        //  //  mWindow?.Close();
        //}

        public void ExecuteProtect(object obj)
        {
            //currently should never excute to here 
            OnClickProtect?.Invoke();
            throw new NotImplementedException();
        }

        public void InitializeBtn()
        {
            AsyncInitializeFileInfoBtn();
            AsyncInitializeEditBtn();
            AsyncInitializePrintBtn();
            AsyncInitializeShareBtn();
            AsyncInitializeExportBtn();
            AsyncInitializeExtractBtn();

            if (mNxlFile.FileType == EnumFileType.FILE_TYPE_IMAGE)
            {
                StatusCode |= UIStatusCode.ROTATE_BTN_VISIBLE;
            }
            else
            {
                if ((StatusCode & UIStatusCode.ROTATE_BTN_VISIBLE) == UIStatusCode.ROTATE_BTN_VISIBLE)
                {
                    StatusCode ^= UIStatusCode.ROTATE_BTN_VISIBLE;
                }
            }
        }

        private async void AsyncInitializeFileInfoBtn()
        {
         bool res = await Task.Factory.StartNew<bool>(()=> {
              bool result = false;
              try
              {
                result = mNxlFile.CanFileInfo();
              }
              catch (Exception ex)
              {
              }
              return result;

            });

            if (res)
            {
                StatusCode |= UIStatusCode.FILE_INFO_BTN_VISIBLE;
            }
            else
            {
                if ((StatusCode & UIStatusCode.FILE_INFO_BTN_VISIBLE) == UIStatusCode.FILE_INFO_BTN_VISIBLE)
                {
                    StatusCode ^= UIStatusCode.FILE_INFO_BTN_VISIBLE;
                }
            }
        }

        private async void AsyncInitializeEditBtn()
        {
            bool res = await Task.Factory.StartNew<bool>(()=> 
            {
                bool result = false;
                try
                {
                    result = mNxlFile.CanEdit();
                }
                catch (Exception ex)
                {
                }
                return result;
            });

            if (res)
            {
                StatusCode |= UIStatusCode.EDIT_BTN_VISIBLE;
            }
            else
            {
                if ((StatusCode & UIStatusCode.EDIT_BTN_VISIBLE) == UIStatusCode.EDIT_BTN_VISIBLE)
                {
                    StatusCode ^= UIStatusCode.EDIT_BTN_VISIBLE;
                }
            }
        }

        private async void AsyncInitializePrintBtn()
        {
            bool res = await Task.Factory.StartNew<bool>(()=> 
            {
                bool result = false;
                try
                {
                    result = mNxlFile.CanPrint();
                }
                catch (Exception ex)
                {
                }
                return result;
            });

            if (res)
            {
                StatusCode |= UIStatusCode.PRINT_BTN_VISIBLE;
            }
            else
            {
                if ((StatusCode & UIStatusCode.PRINT_BTN_VISIBLE) == UIStatusCode.PRINT_BTN_VISIBLE)
                {
                    StatusCode ^= UIStatusCode.PRINT_BTN_VISIBLE;
                }
            }
        }

        private async void AsyncInitializeShareBtn()
        {
            bool res = await Task.Factory.StartNew<bool>(()=> 
            {
                bool result = false;
                try
                {
                    result = mNxlFile.CanShare();
                }
                catch (Exception ex)
                {
                }
                return result;
            });

            if (res)
            {
                StatusCode |= UIStatusCode.SHARE_BTN_VISIBLE;
            }
            else
            {
                if ((StatusCode & UIStatusCode.SHARE_BTN_VISIBLE) == UIStatusCode.SHARE_BTN_VISIBLE)
                {
                    StatusCode ^= UIStatusCode.SHARE_BTN_VISIBLE;
                }
            }
        }

        private async void AsyncInitializeExportBtn()
        {
            bool res = await Task.Factory.StartNew<bool>(()=> 
            {
                bool result = false;
                try
                {
                    result = mNxlFile.CanExport();
                }
                catch (Exception ex)
                {
                }
                return result;
            });

            if (res)
            {
                StatusCode |= UIStatusCode.SAVE_AS_BTN_VISIBLE;
            }
            else
            {
                if ((StatusCode & UIStatusCode.SAVE_AS_BTN_VISIBLE) == UIStatusCode.SAVE_AS_BTN_VISIBLE)
                {
                    StatusCode ^= UIStatusCode.SAVE_AS_BTN_VISIBLE;
                }
            }
        }

        private async void AsyncInitializeExtractBtn()
        {
            bool res = await Task.Factory.StartNew<bool>(()=> 
            {
                bool result = false;
                try
                {
                    result = mNxlFile.CanExtract();
                }
                catch (Exception ex)
                {
                }
                return result;
            });

            if (res)
            {
                StatusCode |= UIStatusCode.EXTRACT_BTN_VISIBLE;
            }
            else
            {
                if ((StatusCode & UIStatusCode.EXTRACT_BTN_VISIBLE) == UIStatusCode.EXTRACT_BTN_VISIBLE)
                {
                    StatusCode ^= UIStatusCode.EXTRACT_BTN_VISIBLE;
                }
                  
            }
        }

        public void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            mIsNetworkAvailable = e.IsAvailable;
            List<Task<bool>> list = new List<Task<bool>>();
            list.Add(new TaskFactory<bool>().StartNew(() =>
            {
                bool result = false;
                try
                {
                    // dependence network and database
                    result = mNxlFile.CanShare();
                }
                catch (Exception ex)
                {
                }
                return result;
            }));

            list.Add(new TaskFactory<bool>().StartNew(() =>
            {
                bool result = false;
                try
                {
                    // dependence network and database
                    result = mNxlFile.CanExport();
                }
                catch (Exception ex)
                {
                }
                return result;
            }));

            Task.Factory.ContinueWhenAll(list.ToArray(), (x) =>
            {
                // Was cancellation already requested?
                if (mApplication.Token.IsCancellationRequested)
                {
                    mApplication.Token.ThrowIfCancellationRequested();
                }

                Task<bool>[] array = x as Task<bool>[];
                List<UInt64> statusCodes = new List<UInt64>();
                statusCodes.Add(UIStatusCode.SHARE_BTN_VISIBLE);
                statusCodes.Add(UIStatusCode.SAVE_AS_BTN_VISIBLE);
                mApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i].Result)
                        {
                            StatusCode |= statusCodes[i];
                        }
                        else
                        {
                            if ((StatusCode & statusCodes[i]) == statusCodes[i])
                            {
                                StatusCode ^= statusCodes[i];
                            }
                        }
                    }
                }));
            }, mApplication.Token);
        }
    }
}
