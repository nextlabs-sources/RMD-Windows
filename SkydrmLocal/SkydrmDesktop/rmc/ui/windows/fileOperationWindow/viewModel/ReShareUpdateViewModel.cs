using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CustomControls;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    public class ReShareUpdateViewModel : BaseViewModel
    {
        private readonly FileDestReShareUpdatePage fileDestReShareUpdatePage = new FileDestReShareUpdatePage();
        private readonly BackgroundWorker DoReShareUpdate_BgWorker = new BackgroundWorker();

        private IReShareUpdate ReShareUpOpert { get; }

        private readonly List<int> originalSharedProjectID = new List<int>();
        private readonly List<string> addProjectID = new List<string>();
        private readonly List<string> removeProjectID = new List<string>();

        public ReShareUpdateViewModel(IReShareUpdate reShareUp, FileOperationWin win) : base(win)
        {
            this.ReShareUpOpert = reShareUp;

            InitFDReShareUpdatePageViewModel();
            InitFDReShareUpdatePageCommand();

            Host.frm.Content = fileDestReShareUpdatePage;

            DoReShareUpdate_BgWorker.DoWork += DoReShareUpdate_BgWorker_DoWork; ;
            DoReShareUpdate_BgWorker.RunWorkerCompleted += DoReShareUpdate_BgWorker_RunWorkerCompleted;
        }
        private void InitFDReShareUpdatePageViewModel()
        {
            fileDestReShareUpdatePage.ViewModel.CaptionViewMode.Title = CultureStringInfo.ApplicationFindResource("ReShareOperation_Win_Title");
            fileDestReShareUpdatePage.ViewModel.CaptionViewMode.Description = "";
            fileDestReShareUpdatePage.ViewModel.CaptionViewMode.ChangeBtnVisible = System.Windows.Visibility.Collapsed;
            fileDestReShareUpdatePage.ViewModel.CaptionViewMode.FileName = ReShareUpOpert.FileInfo.FileName[0];

            if (ReShareUpOpert.NxlType == NxlFileType.Adhoc)
            {
                // file is Adhoc
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;
                bool isAddWaterMark = !string.IsNullOrEmpty(ReShareUpOpert.WaterMark);
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(ReShareUpOpert.Rights, isAddWaterMark);
                //fileDestReShareUpdatePage.ViewModel.RightsDisplayViewModel.RightsList = DataTypeConvert.SDKRights2RightsDisplayModel(ReShareUpOpert.Rights, isAddWaterMark);
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = ReShareUpOpert.WaterMark;
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = isAddWaterMark ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(ReShareUpOpert.Expiration, out string expiryDate);
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = expiryDate;
            }
            else
            {
                // file is CentralPolicy
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.CentralPolicy;
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = ReShareUpOpert.Tags;
                bool isAddWaterMark = !string.IsNullOrEmpty(ReShareUpOpert.WaterMark);
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(ReShareUpOpert.Rights, isAddWaterMark, false);
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = ReShareUpOpert.WaterMark;
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = isAddWaterMark ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                fileDestReShareUpdatePage.ViewModel.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = System.Windows.Visibility.Collapsed;
            }

            foreach (var item in DataTypeConvertHelper.ProjectInfo2CustomControlProjects(ReShareUpOpert.ProjectDatas))
            {
                if (ReShareUpOpert.SharedWithProject.Contains(item.Id))
                {
                    item.IsChecked = true;
                    originalSharedProjectID.Add(item.Id);
                }
                fileDestReShareUpdatePage.ViewModel.ProjectList.Add(item);
            }

            if (ReShareUpOpert.IsAdmin)
            {
                fileDestReShareUpdatePage.ViewModel.RevokeVisibility = System.Windows.Visibility.Visible;
            }
        }

        private void InitFDReShareUpdatePageCommand()
        {
            // Create bindings.
            CommandBinding binding;

            binding = new CommandBinding(FDReShareUpdate_DataCommands.Revoke);
            binding.Executed += ReShareRevokeCommand;
            Host.CommandBindings.Add(binding);

            binding = new CommandBinding(FDReShareUpdate_DataCommands.Update);
            binding.CanExecute += ReShareUpdateCommand_CanExecute;
            binding.Executed += ReShareUpdateCommand;
            Host.CommandBindings.Add(binding);

            binding = new CommandBinding(FDReShareUpdate_DataCommands.Close);
            binding.Executed += CloseCommand_Executed;
            Host.CommandBindings.Add(binding);
        }
        private void ReShareRevokeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            bool result = ReShareUpOpert.RevokeSharing();
            if (result)
            {
                SkydrmApp.Singleton.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ReShareUpOperation_Win_RevokeNotify"), true, 
                    ReShareUpOpert.FileInfo.FileName[0], "Revoke sharing",
                    ReShareUpOpert.IsMarkOffline ? featureProvider.MessageNotify.EnumMsgNotifyIcon.Offline : featureProvider.MessageNotify.EnumMsgNotifyIcon.Online);

                Host.Close();
            }
        }
        private void ReShareUpdateCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            fileDestReShareUpdatePage.ViewModel.PositiveContent = CultureStringInfo.ApplicationFindResource("ReShareOperation_Win_BtnOk");
            foreach (var item in fileDestReShareUpdatePage.ViewModel.ProjectList)
            {
                if (item.IsChecked)
                {
                    if (originalSharedProjectID.Contains(item.Id))
                    { continue; }
                    else
                    { fileDestReShareUpdatePage.ViewModel.PositiveContent= CultureStringInfo.ApplicationFindResource("ReShareOperation_Win_BtnUpdate"); }
                }
                else
                {
                    if (originalSharedProjectID.Contains(item.Id))
                    { fileDestReShareUpdatePage.ViewModel.PositiveContent = CultureStringInfo.ApplicationFindResource("ReShareOperation_Win_BtnUpdate"); }
                    else
                    { continue; }
                }
            }
            e.CanExecute = true;
        }
        private void ReShareUpdateCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (fileDestReShareUpdatePage.ViewModel.PositiveContent == CultureStringInfo.ApplicationFindResource("ReShareOperation_Win_BtnOk"))
            {
                Host.Close();
            }
            else
            {
                // do update re-share 
                StartBackgroundWorker();
            }
        }
        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Host.Close();
        }

        private void StartBackgroundWorker()
        {
            if (!DoReShareUpdate_BgWorker.IsBusy)
            {
                DoReShareUpdate_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsSharing = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }

        private void DoReShareUpdate_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;
            addProjectID.Clear();
            removeProjectID.Clear();
            foreach (var item in fileDestReShareUpdatePage.ViewModel.ProjectList)
            {
                if (item.IsChecked)
                {
                    if (originalSharedProjectID.Contains(item.Id))
                    { continue; }
                    else
                    { addProjectID.Add(item.Id.ToString()); }
                }
                else
                {
                    if (originalSharedProjectID.Contains(item.Id))
                    { removeProjectID.Add(item.Id.ToString()); }
                    else
                    { continue; }
                }
            }

            result = ReShareUpOpert.ReShareUpdateFile(addProjectID, removeProjectID, "");

            e.Result = result;
        }

        private void DoReShareUpdate_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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

            // update originalSharedProjectID List and build string
            // build add project
            StringBuilder builderAdd = new StringBuilder();
            for (int i = 0; i < addProjectID.Count; i++)
            {
                originalSharedProjectID.Add(int.Parse(addProjectID[i]));

                builderAdd.Append(ProjectHelper.GetProjectNameById(int.Parse(addProjectID[i])));
                if (i < addProjectID.Count - 1)
                {
                    builderAdd.Append(", ");
                }
            }
            string addNotify = builderAdd.ToString();

            // build remove project
            StringBuilder builderRemove = new StringBuilder();
            for (int i = 0; i < removeProjectID.Count; i++)
            {
                originalSharedProjectID.Remove(int.Parse(removeProjectID[i]));

                builderRemove.Append(ProjectHelper.GetProjectNameById(int.Parse(removeProjectID[i])));
                if (i < removeProjectID.Count - 1)
                {
                    builderRemove.Append(", ");
                }
            }
            string removeNotify = builderRemove.ToString();

            // build notify string
            StringBuilder buildAll = new StringBuilder();
            if (!string.IsNullOrEmpty(addNotify))
            {
                buildAll.Append(CultureStringInfo.ApplicationFindResource("ReShareUpOperation_Win_AddNotify"));
                buildAll.Append(" ");
                buildAll.Append(addNotify);
                buildAll.Append(".");
            }
            if (!string.IsNullOrEmpty(removeNotify))
            {
                buildAll.Append(CultureStringInfo.ApplicationFindResource("ReShareUpOperation_Win_RemoveNotify"));
                buildAll.Append(" ");
                buildAll.Append(removeNotify);
                buildAll.Append(".");
            }
            string notify = buildAll.ToString();
            if (!string.IsNullOrEmpty(notify))
            {
                fileDestReShareUpdatePage.ViewModel.Notify = notify;

                SkydrmApp.Singleton.MessageNotify.NotifyMsg(ReShareUpOpert.FileInfo.FileName[0], notify, 
                    featureProvider.MessageNotify.EnumMsgNotifyType.LogMsg,
                    "Update share with project", featureProvider.MessageNotify.EnumMsgNotifyResult.Succeed,
                    ReShareUpOpert.IsMarkOffline ? featureProvider.MessageNotify.EnumMsgNotifyIcon.Offline : featureProvider.MessageNotify.EnumMsgNotifyIcon.Online);
            }
            // trigger CanExcute event again.
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
