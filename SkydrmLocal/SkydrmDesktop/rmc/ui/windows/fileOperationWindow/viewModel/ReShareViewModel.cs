using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using CustomControls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SkydrmLocal.rmc.common.component;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    public class ReShareViewModel : BaseViewModel
    {
        private readonly FileDestReSharePage fileDestReSharePage = new FileDestReSharePage();
        private readonly FileReShareResultPage fileReShareResultPage = new FileReShareResultPage();

        private readonly BackgroundWorker DoReShare_BgWorker = new BackgroundWorker();

        private IReShare ReShareOpert { get; }

        public ReShareViewModel(IReShare operation, FileOperationWin win) : base(win)
        {
            this.ReShareOpert = operation;

            InitFDReSharePageViewModel();
            InitFDReSharePageCommand();

            Host.frm.Content = fileDestReSharePage;

            DoReShare_BgWorker.DoWork += DoReShare_BgWorker_DoWork; ;
            DoReShare_BgWorker.RunWorkerCompleted += DoReShare_BgWorker_RunWorkerCompleted;
        }

        private void InitFDReSharePageViewModel()
        {
            fileDestReSharePage.ViewModel = new FileDestReShareViewModel(fileDestReSharePage);
            fileDestReSharePage.ViewModel.CaptionViewMode.Title = CultureStringInfo.ApplicationFindResource("ReShareOperation_Win_Title");
            fileDestReSharePage.ViewModel.CaptionViewMode.Description = "";
            fileDestReSharePage.ViewModel.CaptionViewMode.ChangeBtnVisible = System.Windows.Visibility.Collapsed;
            fileDestReSharePage.ViewModel.CaptionViewMode.FileName = ReShareOpert.FileInfo.FileName[0];

            foreach (var item in DataTypeConvertHelper.ProjectInfo2CustomControlProjects(ReShareOpert.ProjectDatas))
            {
                fileDestReSharePage.ViewModel.ProjectList.Add(item);
            }
        }

        private void InitFDReSharePageCommand()
        {
            // Create bindings.
            CommandBinding binding;

            binding = new CommandBinding(FDReShare_DataCommands.Share);
            binding.CanExecute += ReShareCommand_CanExecute;
            binding.Executed += ReShareCommand;
            Host.CommandBindings.Add(binding);

            binding = new CommandBinding(FDReShare_DataCommands.Cancel);
            binding.Executed += CancelCommand_Executed;
            Host.CommandBindings.Add(binding);
        }
        private void ReShareCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (fileDestReSharePage.ViewModel.ProjectList.All(x => !x.IsChecked))
            {
                e.CanExecute = false;
            }
            else
            {
                e.CanExecute = true;
            }
        }
        private void ReShareCommand(object sender, ExecutedRoutedEventArgs e)
        {
            // do re-share
            StartBackgroundWorker();
        }

        private void StartBackgroundWorker()
        {
            if (!DoReShare_BgWorker.IsBusy)
            {
                DoReShare_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsSharing = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }

        private void DoReShare_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;
            List<string> projectList = new List<string>();
            foreach (var item in fileDestReSharePage.ViewModel.ProjectList)
            {
                if (item.IsChecked)
                {
                    projectList.Add(item.Id.ToString());
                }
            }
            if (projectList.Count > 0)
            {
                result = ReShareOpert.ReShareFile(projectList, "");
            }
            
            e.Result = result;
        }

        private void DoReShare_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // collapsed progress bar UI
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            bool result = (bool)e.Result;

            MenuDisableMgr.GetSingleton().IsSharing = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
            }

            if (!result)
            {
                return;
            }

            // send log to nxrmtray
            StringBuilder builder = new StringBuilder();
            builder.Append(CultureStringInfo.ApplicationFindResource("ReShareUpOperation_Win_AddNotify"));
            builder.Append(" ");
            builder.Append(GetRepoName(fileDestReSharePage.ViewModel.ProjectList.ToList()));
            SkydrmApp.Singleton.MessageNotify.NotifyMsg(ReShareOpert.FileInfo.FileName[0], builder.ToString(),
                    featureProvider.MessageNotify.EnumMsgNotifyType.LogMsg,
                    "Update share with project", featureProvider.MessageNotify.EnumMsgNotifyResult.Succeed,
                    ReShareOpert.IsMarkOffline ? featureProvider.MessageNotify.EnumMsgNotifyIcon.Offline : featureProvider.MessageNotify.EnumMsgNotifyIcon.Online);

            // init FileReShareResultPage
            InitFReSResultPageViewModel();
            InitFReSResultPageCommand();
            Host.frm.Content = fileReShareResultPage;
        }

        private void InitFReSResultPageViewModel()
        {
            fileReShareResultPage.ViewModel = new FileReShareResultViewModel(fileReShareResultPage);
            fileReShareResultPage.ViewModel.CaptionDesc2ViewMode.FileName = ReShareOpert.FileInfo.FileName[0];
            fileReShareResultPage.ViewModel.RepoName = GetRepoName(fileDestReSharePage.ViewModel.ProjectList.ToList());

            if (ReShareOpert.NxlType == NxlFileType.Adhoc)
            {
                // file is Adhoc
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;
                bool isAddWaterMark = !string.IsNullOrEmpty(ReShareOpert.WaterMark);
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(ReShareOpert.Rights, isAddWaterMark);
                //fileReShareResultPage.ViewModel.RightsDisplayViewModel.RightsList = DataTypeConvert.SDKRights2RightsDisplayModel(ReShareOpert.Rights, isAddWaterMark);
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = ReShareOpert.WaterMark;
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = isAddWaterMark ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(ReShareOpert.Expiration, out string expiryDate);
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = expiryDate;
            }
            else
            {
                // file is CentralPolicy
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.CentralPolicy;
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = ReShareOpert.Tags;
                bool isAddWaterMark = !string.IsNullOrEmpty(ReShareOpert.WaterMark);
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(ReShareOpert.Rights, isAddWaterMark, false);
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = ReShareOpert.WaterMark;
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = isAddWaterMark ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                fileReShareResultPage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = System.Windows.Visibility.Collapsed;
            }
            
        }
        private string GetRepoName(List<Project>projects)
        {
            bool isAppend = false;
            StringBuilder builder = new StringBuilder();
            foreach (var item in projects)
            {
                if (item.IsChecked)
                {
                    builder.Append(item.Name);
                    builder.Append(", ");
                    isAppend = true;
                }
            }
            if (isAppend)
            {
                builder.Remove(builder.Length - 2, 2);//remove last ", "
            }
            return builder.ToString();
        }

        private void InitFReSResultPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FReSResult_DataCommands.Close);
            binding.Executed += CancelCommand_Executed;
            Host.CommandBindings.Add(binding);
        }

        private void CancelCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Host.Close();
        }
    }
}
