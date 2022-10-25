using CustomControls;
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
    class ModifyNxlFileRightViewModel : BaseViewModel
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private readonly FileRightsSelectPage fileRightsSelectPage = new FileRightsSelectPage();
        private readonly FileRightsPreviewPage fileRightsPreviewPage = new FileRightsPreviewPage();
        private readonly FileRightsResultPage fileRightsResultPage = new FileRightsResultPage();

        private readonly BackgroundWorker DoModifyRight_BgWorker = new BackgroundWorker();

        // use for optimize page load
        private bool HasInitedFileRightsPreviewPageVM { get; set; }

        private IModifyNxlFileRight ModifyRightOpert { get; }

        // for selected tags changed
        private bool tagIsValid { get; set; } = true;
        private Dictionary<string, List<string>> tags { get; set; } = new Dictionary<string, List<string>>();
        // for preview tag rights wartemark
        private SkydrmLocal.rmc.sdk.WaterMarkInfo tagPreivewWarterMark { get; set; } = new SkydrmLocal.rmc.sdk.WaterMarkInfo() { text = "", fontName = "", fontColor = "" };

        public ModifyNxlFileRightViewModel(IModifyNxlFileRight operation, FileOperationWin win) : base(win)
        {
            this.ModifyRightOpert = operation;

            InitFRSelectPageViewModel();
            InitFRSelectPageCommand();
            Host.frm.Content = fileRightsSelectPage;

            DoModifyRight_BgWorker.DoWork += DoModifyRight_BgWorker_DoWork;
            DoModifyRight_BgWorker.RunWorkerCompleted += DoModifyRight_BgWorker_RunWorkerCompleted;
        }

        #region Init FileRightsSelectPage
        private void InitFRSelectPageViewModel()
        {
            // caption
            int fileCount = ModifyRightOpert.FileInfo.FileName.Length;
            fileRightsSelectPage.ViewMode.CaptionVM.Title = CultureStringInfo.ApplicationFindResource("ModifyRightOperation_Win_Title");
            fileRightsSelectPage.ViewMode.CaptionVM.DescriptionVisibility = System.Windows.Visibility.Hidden;
            fileRightsSelectPage.ViewMode.CaptionVM.FileCount = fileCount;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileCount; i++)
            {
                sb.Append(ModifyRightOpert.FileInfo.FileName[i]);
                if (i != fileCount - 1)
                {
                    sb.Append(";\n");
                }
            }
            fileRightsSelectPage.ViewMode.CaptionVM.FileName = sb.ToString();
            fileRightsSelectPage.ViewMode.CaptionVM.ChangeBtnVisible = System.Windows.Visibility.Collapsed;
            if (ModifyRightOpert.NxlType == model.NxlFileType.Adhoc)
            {
                fileRightsSelectPage.ViewMode.CaptionVM.PermissionDescribe = CultureStringInfo.ApplicationFindResource("ModifyRightOperation_Caption_UserDefined");

                List<string> adhocRightsStr = DataTypeConvertHelper.SDKRights2RightsString(ModifyRightOpert.NxlRights);

                Dictionary<string, List<string>> keyValues = new Dictionary<string, List<string>>();
                keyValues.Add("", adhocRightsStr);

                fileRightsSelectPage.ViewMode.CaptionVM.FilePermissions = keyValues;
            }
            else
            {
                fileRightsSelectPage.ViewMode.CaptionVM.PermissionDescribe = CultureStringInfo.ApplicationFindResource("ModifyRightOperation_Caption_CompanyDefined");
                fileRightsSelectPage.ViewMode.CaptionVM.FilePermissions = ModifyRightOpert.NxlTags;
            }

            // display save path
            fileRightsSelectPage.ViewMode.SavePath = ModifyRightOpert.CurrentSelectedSavePath.DestDisplayPath;
            // change destination button visible
            fileRightsSelectPage.ViewMode.ChangDestBtnVisible = System.Windows.Visibility.Collapsed;

            if (ModifyRightOpert.NxlType == model.NxlFileType.Adhoc)
            {
                // adhoc
                fileRightsSelectPage.ViewMode.ProtectType = ProtectType.Adhoc;
                fileRightsSelectPage.ViewMode.CentralRadioIsEnable = false;

                // rights
                bool isAddWarterMark = !string.IsNullOrWhiteSpace(ModifyRightOpert.NxlAdhocWaterMark.text);
                fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Rights = DataTypeConvertHelper.SDKRights2CustomControlRights(ModifyRightOpert.NxlRights.ToArray(), isAddWarterMark);

                //wartermark, expiration
                if (isAddWarterMark)
                {
                    fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Watermarkvalue = ModifyRightOpert.NxlAdhocWaterMark.text;
                }
                fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry = DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(ModifyRightOpert.NxlExpiration,
                    out string expiryDate, false);
            }
            else
            {
                // centralPolicy
                fileRightsSelectPage.ViewMode.ProtectType = ProtectType.CentralPolicy;
                fileRightsSelectPage.ViewMode.AdhocRadioIsEnable = false;

                // if event handler's binding not in the constructor, should bind handler before set value
                // when set fileRightsSelectPage.ViewMode.CtP_Classifications will excute this handler
                fileRightsSelectPage.ViewMode.OnClassificationChanged += (ss, ee) =>
                {
                    Console.WriteLine($"Handler SelectClassificationChanged in ProtectViewModel: isValid({ee.NewValue.IsValid}),select count({ee.NewValue.KeyValues.Count})");
                    tagIsValid = ee.NewValue.IsValid;
                    tags = ee.NewValue.KeyValues;
                };

                // classifications
                CustomControls.components.CentralPolicy.model.Classification[] classifications = new CustomControls.components.CentralPolicy.model.Classification[0];
                if (ModifyRightOpert.NxlRepoId == App.SystemProject.Id)
                {
                    classifications = DataTypeConvertHelper.SdkTag2CustomControlTag(App.SystemProject.GetClassifications());
                }
                else
                {
                    foreach (var item in ModifyRightOpert.RepoList)
                    {
                        if (item is SkydrmLocal.rmc.fileSystem.project.ProjectRepo)
                        {
                            IList<SkydrmLocal.rmc.fileSystem.project.ProjectData> projects = (item as SkydrmLocal.rmc.fileSystem.project.ProjectRepo).FilePool;
                            foreach (var project in projects)
                            {
                                if (ModifyRightOpert.NxlRepoId == project.ProjectInfo.ProjectId)
                                {
                                    classifications = DataTypeConvertHelper.SdkTag2CustomControlTag(project.ProjectInfo.Raw.ListClassifications());
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }

                fileRightsSelectPage.ViewMode.CtP_Classifications = classifications;

                ///  ModifyRightOpert has same name in Classifications, that should show AddInheritedClassification
                Dictionary<string, List<string>> needCtP_AddInheritedClassification = new Dictionary<string, List<string>>();
                foreach (var item in ModifyRightOpert.NxlTags)
                {
                    var itemClassificastion = classifications.ToList().FirstOrDefault(x => x.name == item.Key);
                    if (!string.IsNullOrWhiteSpace(itemClassificastion.name))
                    {
                        needCtP_AddInheritedClassification.Add(item.Key, item.Value);
                    }
                }

                fileRightsSelectPage.ViewMode.CtP_AddInheritedClassification = needCtP_AddInheritedClassification;

                if (fileRightsSelectPage.ViewMode.CtP_Classifications.Length == 0
                    && ModifyRightOpert.NxlTags.Count == 0)
                {
                    fileRightsSelectPage.ViewMode.CpWarnDesText = CultureStringInfo.ApplicationFindResource("ModifyRightOperation_NoTag_Warning");
                    fileRightsSelectPage.ViewMode.CpWarnDesVisible = System.Windows.Visibility.Visible;
                }
                else
                {
                    fileRightsSelectPage.ViewMode.CpWarnDesVisible = System.Windows.Visibility.Collapsed;
                }
            }

            // positive button content
            fileRightsSelectPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ModifyRightOperation_FRSelectBtn_Next");
        }
        private void InitFRSelectPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRS_DataCommands.Positive);
            binding.CanExecute += FRightsSelectPositiveCommand_CanExecute;
            binding.Executed += FRightsSelectPositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRS_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }
        private void FRightsSelectPositiveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (fileRightsSelectPage.ViewMode.ProtectType == ProtectType.Adhoc)
            {
                e.CanExecute = true;
            }
            else
            {
                if (tagIsValid)
                {
                    e.CanExecute = true;
                }
            }
        }
        private void FRightsSelectPositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (ModifyRightOpert.NxlType == model.NxlFileType.CentralPolicy)
            {
                //centralPolicy
                // init FileRightsPreviewPage
                if (!HasInitedFileRightsPreviewPageVM)
                {
                    HasInitedFileRightsPreviewPageVM = true;
                    InitFRPreviewPageViewModel();
                    InitFRPreviewPageCommand();
                }
                else
                {
                    InitFRPreviewPageSubViewModel();
                }
                Host.frm.Content = fileRightsPreviewPage;
            }
            else
            {
                // adhoc
                var sdkExpiry = DataTypeConvertHelper.CustomCtrExpiry2SdkExpiry(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry);
                if (sdkExpiry.type != SkydrmLocal.rmc.sdk.ExpiryType.NEVER_EXPIRE && DateTimeHelper.DateTimeToTimestamp(DateTime.Now) > sdkExpiry.End)
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ProtectOperation_InValidExpiry"), false);
                    return;
                }
                // do modify
                StartDoModify_BgWorker();
            }
        }
        #endregion

        #region Init FileRightsPreviewPage
        private void InitFRPreviewPageViewModel()
        {
            // caption
            int fileCount = ModifyRightOpert.FileInfo.FileName.Length;
            fileRightsPreviewPage.ViewMode.CaptionVM.Title = CultureStringInfo.ApplicationFindResource("ModifyRightOperation_Win_Title");
            fileRightsPreviewPage.ViewMode.CaptionVM.DescriptionVisibility = System.Windows.Visibility.Hidden;
            fileRightsPreviewPage.ViewMode.CaptionVM.FileCount = fileCount;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileCount; i++)
            {
                sb.Append(ModifyRightOpert.FileInfo.FileName[i]);
                if (i != fileCount - 1)
                {
                    sb.Append(";\n");
                }
            }
            fileRightsPreviewPage.ViewMode.CaptionVM.FileName = sb.ToString();
            fileRightsPreviewPage.ViewMode.CaptionVM.ChangeBtnVisible = System.Windows.Visibility.Collapsed;

            fileRightsPreviewPage.ViewMode.PositiveBtnContent = CultureStringInfo.ApplicationFindResource("ModifyRightOperation_FRPreviewBtn_Add");

            InitFRPreviewPageSubViewModel();
        }
        private void InitFRPreviewPageSubViewModel()
        {
            // think about do this in background, use task await
            Host.Cursor = Cursors.Wait;
            var listRights = ModifyRightOpert.PreviewRightsByCentralPolicy(tags, out SkydrmLocal.rmc.sdk.WaterMarkInfo warterMark);
            Host.Cursor = Cursors.Arrow;

            tagPreivewWarterMark = warterMark;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.CentralPolicy;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = tags;
            fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTagScrollViewMargin = new System.Windows.Thickness(78, 0, 0, 0);

            if (listRights.Count > 0)
            {
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.AccessDenyVisibility = System.Windows.Visibility.Collapsed;
                bool isAddWarterMark = !string.IsNullOrEmpty(warterMark.text);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(listRights.ToArray(), isAddWarterMark, false);
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = warterMark.text;
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = isAddWarterMark ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.AccessDenyVisibility = System.Windows.Visibility.Visible;
            }
        }
        private void InitFRPreviewPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FRPreview_DataCommands.Positive);
            binding.Executed += FRPreviewPositiveCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRPreview_DataCommands.Back);
            binding.Executed += FRPreviewBackCommand;
            Host.CommandBindings.Add(binding);
            binding = new CommandBinding(FRPreview_DataCommands.Cancel);
            binding.Executed += CancelCommand;
            Host.CommandBindings.Add(binding);
        }

        private void FRPreviewPositiveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            StartDoModify_BgWorker();
        }
        private void FRPreviewBackCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Host.frm.Content = fileRightsSelectPage;
        }
        #endregion

        #region Init FileRightsResultPage
        private void InitFRResultPageViewModel()
        {
            // caption
            int fileCount = ModifyRightOpert.FileInfo.FileName.Length;

            fileRightsResultPage.ViewMode.Caption3VM.Title = CultureStringInfo.ApplicationFindResource("ModifyRightOperation_Win_Title");
            fileRightsResultPage.ViewMode.Caption3VM.PromptText = fileCount > 1 ?
                CultureStringInfo.ApplicationFindResource("ProtectOperation_PromptHave") : //For multiple files protect title display.
                CultureStringInfo.ApplicationFindResource("ProtectOperation_PromptHas");  //For single file protect title display.;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileCount; i++)
            {
                sb.Append(ModifyRightOpert.FileInfo.FileName[i]);
                if (i != fileCount - 1)
                {
                    sb.Append(";\n");
                }
            }
            fileRightsResultPage.ViewMode.Caption3VM.FileName = sb.ToString();
            fileRightsResultPage.ViewMode.Caption3VM.Desitination = ModifyRightOpert.CurrentSelectedSavePath.DestDisplayPath;

            if (ModifyRightOpert.NxlType == model.NxlFileType.CentralPolicy)
            {
               
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.CentralPolicy;

                var frPreClassifiedRightsVM = fileRightsPreviewPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTag = frPreClassifiedRightsVM.CentralTag;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.CentralTagScrollViewMargin = frPreClassifiedRightsVM.CentralTagScrollViewMargin;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.AccessDenyVisibility = frPreClassifiedRightsVM.AccessDenyVisibility;

                if (frPreClassifiedRightsVM.AccessDenyVisibility == System.Windows.Visibility.Visible)
                {
                    return;
                }

                ModifyRightOpert.PreviewRightsByCentralPolicy(tags, out SkydrmLocal.rmc.sdk.WaterMarkInfo water);
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.RightsList = frPreClassifiedRightsVM.RightsDisplayVM.RightsList;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = water.text;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = frPreClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = frPreClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility;
            }
            else
            {
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.FileType = CustomControls.components.FileType.Adhoc;

                var frSelectAdhocVM = fileRightsSelectPage.ViewMode.AdhocPage_ViewModel;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.FileRights = frSelectAdhocVM.Rights;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WatermarkValue = frSelectAdhocVM.Watermarkvalue;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.WaterPanlVisibility = frSelectAdhocVM.Rights.Contains(CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK) ?
                     System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                fileRightsResultPage.ViewMode.AdhocAndClassifiedRightsVM.AdhocRightsVM.ValidityValue = frSelectAdhocVM.ExpireDateValue;
            }

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

        private void StartDoModify_BgWorker()
        {
            if (!DoModifyRight_BgWorker.IsBusy)
            {
                DoModifyRight_BgWorker.RunWorkerAsync();

                // display progrss bar UI
                GridProBarVisible = System.Windows.Visibility.Visible;

                MenuDisableMgr.GetSingleton().IsProtecting = true;
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
            }
        }
        private void DoModifyRight_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool result = false;

            // fix bug 58690
            if (!App.MainWin.viewModel.IsNetworkAvailable)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_NxLFile_Rights_Cannot_Modified"), false);
                e.Result = false;
                return;
            }

            List<SkydrmLocal.rmc.sdk.FileRights> fileRights = new List<SkydrmLocal.rmc.sdk.FileRights>();
            SkydrmLocal.rmc.sdk.WaterMarkInfo warterMark = new SkydrmLocal.rmc.sdk.WaterMarkInfo();

            if (ModifyRightOpert.NxlType == model.NxlFileType.Adhoc)
            {
                fileRights = DataTypeConvertHelper.CustomCtrRights2SDKRights(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Rights);
                warterMark = ModifyRightOpert.NxlAdhocWaterMark;
            }
            else
            {
                fileRights.Clear();
                warterMark = tagPreivewWarterMark;
                // fix Bug 63450 - local app crash when add file to main window
                // Some attributes of the watermark obtained from the SDK are null,
                // In sdkwrapper not judge string is null, need re-set string is empty.
                if (string.IsNullOrEmpty(warterMark.text))
                {
                    warterMark.text = "";
                }
                warterMark.fontColor = "";
                warterMark.fontName = "";
            }

            var sdkExpiry = DataTypeConvertHelper.CustomCtrExpiry2SdkExpiry(fileRightsSelectPage.ViewMode.AdhocPage_ViewModel.Expiry);
            var selectTags = tags;

            result = ModifyRightOpert.ModifyRights(fileRights, warterMark, sdkExpiry, selectTags);

            e.Result = result;
        }
        private void DoModifyRight_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result = (bool)e.Result;
            GridProBarVisible = System.Windows.Visibility.Collapsed;

            MenuDisableMgr.GetSingleton().IsProtecting = false;
            if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
            {
                MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
            }

            if (!result)
            {
                return;
            }

            ModifyRightOpert.AddLog();

            // init FileRightsResultPage
            InitFRResultPageViewModel();
            Host.CommandBindings.Clear();
            InitFRResultPageCommand();

            Host.Closed += (ss, ee) =>
            {
                if (ModifyRightOpert.FileInfo.FromSource != model.FileFromSource.SkyDRM_PlugIn)
                {
                    ModifyRightOpert.UpdateNxlFile();
                }
            };

            Host.frm.Content = fileRightsResultPage;
        }

    }
}
