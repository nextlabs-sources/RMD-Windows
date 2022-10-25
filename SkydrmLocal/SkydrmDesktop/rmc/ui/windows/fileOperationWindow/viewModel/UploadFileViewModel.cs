using CustomControls;
using CustomControls.components;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    public class UploadFileViewModel : BaseViewModel
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private readonly FileRightsSelectPage fileRightsSelectPage = new FileRightsSelectPage();
        private readonly FileRightsResultPage fileRightsResultPage = new FileRightsResultPage();

        private readonly BackgroundWorker DoProtect_BgWorker = new BackgroundWorker();
        private readonly BackgroundWorker DoJustUpload_BgWorker = new BackgroundWorker();

        private IUpload UploadOpert { get; }

        // use for notify MainWindow
        private List<SkydrmLocal.rmc.fileSystem.basemodel.INxlFile> createdFiles;

        public UploadFileViewModel(IUpload operation, FileOperationWin win) : base(win)
        {
            this.UploadOpert = operation;

            InitFRSelectPageViewModel();
            InitFRSelectPageCommand();
            Host.frm.Content = fileRightsSelectPage;

            DoProtect_BgWorker.DoWork += DoProtect_BgWorker_DoWork;
            DoProtect_BgWorker.RunWorkerCompleted += DoProtect_BgWorker_RunWorkerCompleted;

            DoJustUpload_BgWorker.DoWork += DoJustUpload_BgWorker_DoWork;
            DoJustUpload_BgWorker.RunWorkerCompleted += DoJustUpload_BgWorker_RunWorkerCompleted;
        }

        #region Init FileRightsSelectPage
        private void InitFRSelectPageViewModel()
        {
            // caption
            int fileCount = UploadOpert.FileInfo.FileName.Length;
            fileRightsSelectPage.ViewMode.CaptionDescVisible = System.Windows.Visibility.Collapsed;
            fileRightsSelectPage.ViewMode.Caption4VM.Title = CultureStringInfo.ApplicationFindResource("UploadOperation_Win_Title");
            if (fileCount > 1)
            {
                fileRightsSelectPage.ViewMode.Caption4VM.SelectFileDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_SelectFiles");
                fileRightsSelectPage.ViewMode.SavePathDesc = CultureStringInfo.ApplicationFindResource("ProtectOperation_FilesSaveTo");
                fileRightsSelectPage.ViewMode.FileTypeSelect_Sp_Lable = CultureStringInfo.ApplicationFindResource("ProtectOperation_SpecifyRightsFiles");
            }
            fileRightsSelectPage.ViewMode.Caption4VM.FileCount = fileCount;
            fileRightsSelectPage.ViewMode.Caption4VM.FileNameList.Clear();
            for (int i = 0; i < fileCount; i++)
            {
                fileRightsSelectPage.ViewMode.Caption4VM.FileNameList.Add(new FileModel()
                { Name = UploadOpert.FileInfo.FileName[i], FullName = UploadOpert.FileInfo.FileName[i] });
            }

            // display path
            fileRightsSelectPage.ViewMode.SavePath = UploadOpert.CurrentSelectedSavePath.DestDisplayPath;
            fileRightsSelectPage.ViewMode.ChangDestBtnVisible = System.Windows.Visibility.Collapsed;

            // disenable centralpolicy radio
            fileRightsSelectPage.ViewMode.ProtectType = ProtectType.Adhoc;
            fileRightsSelectPage.ViewMode.CentralRadioIsEnable = false;

            //wartermark, expiration 
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Watermarkvalue = App.User.Watermark.text;
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry = DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(App.User.Expiration,
                out string expiryDate, true);

            // disable extract right
            fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.DisableRights = new HashSet<CustomControls.components.DigitalRights.model.FileRights>()
                    { CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT };

            fileRightsSelectPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("UploadOperation_Btn_Protect");
            fileRightsSelectPage.ViewMode.JustUploadBtnVisibility = System.Windows.Visibility.Visible;
        }

        private void InitFRSelectPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRS_DataCommands.Positive);
            binding.Executed += FRSelectPositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRS_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRS_DataCommands.JustUpload);
            binding.Executed += FRSelectJustUploadCommand; ;
            Host.CommandBindings.Add(binding);
        }

        private void FRSelectPositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            StartDoProtectUpload_BgWorker();
        }
        private void FRSelectJustUploadCommand(object sender, ExecutedRoutedEventArgs e)
        {
            StartDoJustUpload_BgWorker();
        }
        #endregion

        #region Init FileRightsResultPage
        private void InitFRResultPageViewModel()
        {
            // caption
            int fileCount = UploadOpert.FileInfo.FileName.Length;

            fileRightsResultPage.ViewMode.Caption3VM.Title = fileCount > 1 ?
                CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Titles")  : //For multiple files protect title display.
                CultureStringInfo.ApplicationFindResource("ProtectOperation_Win_Title"); //For single file protect title display.;
            fileRightsResultPage.ViewMode.Caption3VM.PromptText = fileCount > 1 ?
                CultureStringInfo.ApplicationFindResource("ProtectOperation_PromptHave") : //For multiple files protect title display.
                CultureStringInfo.ApplicationFindResource("ProtectOperation_PromptHas");  //For single file protect title display.;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileCount; i++)
            {
                sb.Append(UploadOpert.FileInfo.FileName[i]);
                if (i != fileCount - 1)
                {
                    sb.Append(";\n");
                }
            }
            fileRightsResultPage.ViewMode.Caption3VM.FileName = sb.ToString();
            fileRightsResultPage.ViewMode.Caption3VM.Desitination = DataTypeConvertHelper.MY_SPACE;

            // only support adhoc
            fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;

            var frSelectAdhocVM = fileRightsSelectPage.ViewMode.AdhocPage_ViewModel;
            fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = frSelectAdhocVM.Rights;
            fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = frSelectAdhocVM.Watermarkvalue;
            fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = frSelectAdhocVM.Rights.Contains(CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK) ?
                 System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = frSelectAdhocVM.ExpireDateValue;

        }
        private void InitFRResultPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRResult_DataCommands.Close);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        #endregion

        private void CancelCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Host.Close();
        }

        private void StartDoProtectUpload_BgWorker()
        {
            if (!DoProtect_BgWorker.IsBusy)
            {
                DoProtect_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsProtecting = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }
        private void DoProtect_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;

            // protect file
            var sdkRights = DataTypeConvertHelper.CustomCtrRights2SDKRights(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Rights);
            string warterMark = fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Watermarkvalue;
            var sdkExpiry = DataTypeConvertHelper.CustomCtrExpiry2SdkExpiry(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry);

            var listFile = UploadOpert.ProtectFile(sdkRights, warterMark, sdkExpiry);
            if (listFile.Count > 0)
            {
                createdFiles = listFile;
                result = true;
            }

            e.Result = result;
        }
        private void DoProtect_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result = (bool)e.Result;
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsProtecting = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
            }

            if (UploadOpert.FileInfo.FailedFileName.Count > 0)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_Protect_Failed"), false, "", "Protect failed");
            }

            if (!result)
            {
                return;
            }

            // init FileRightsResultPage
            InitFRResultPageViewModel();
            InitFRResultPageCommand();

            Host.Closed += (ss, ee) =>
            {
                if (createdFiles?.Count > 0)
                {
                    App.MainWin.viewModel.GetCreatedFile(createdFiles);
                }
            };

            Host.frm.Content = fileRightsResultPage;
        }

        private void StartDoJustUpload_BgWorker()
        {
            if (!DoJustUpload_BgWorker.IsBusy)
            {
                DoJustUpload_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsProtecting = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }
        private void DoJustUpload_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;

            var listFile = UploadOpert.AddUploadFile();
            if (listFile.Count > 0)
            {
                createdFiles = listFile;
                result = true;
            }

            e.Result = result;
        }
        private void DoJustUpload_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result = (bool)e.Result;
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsProtecting = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
            }

            if (UploadOpert.FileInfo.FailedFileName.Count > 0)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("UploadOperation_Failed_AddUploadFile"), false, "", "Add file to upload list failed");
            }

            if (!result)
            {
                return;
            }
            

            Host.Closed += (ss, ee) =>
            {
                if (createdFiles?.Count > 0)
                {
                    App.MainWin.viewModel.GetCreatedFile(createdFiles);
                }
            };

            Host.Close();
        }

    }
}
