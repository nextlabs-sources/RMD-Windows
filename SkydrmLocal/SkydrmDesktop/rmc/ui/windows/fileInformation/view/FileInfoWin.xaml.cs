using CustomControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SkydrmDesktop.rmc.ui.windows.fileInformation.view
{
    /// <summary>
    /// Interaction logic for FileInfoWin.xaml
    /// </summary>
    public partial class FileInfoWin : Window
    {
        private SkydrmApp app = (SkydrmApp)SkydrmApp.Current;

        private readonly FileInfoPage fileInfoPage = new FileInfoPage();

        #region Nxl file attribute
        private string FileName { get; set; }
        private string FilePath { get; set; }
        private long FileSize { get; set; }
        private string LastModifiedTime { get; set; }
        private bool IsAdhoc { get; set; }
        private bool IsCentralPolicy { get; set; }
        private string[] Emails { get; set; } = new string[] { };
        private List<FileRights> NxlFileRights { get; set; } = new List<FileRights>();
        private Dictionary<string, List<string>> NxlTags { get; set; } = new Dictionary<string, List<string>>();
        private string NxlWaterMark { get; set; }
        private Expiration NxlExpiration { get; set; } = new Expiration();
        private EnumFileRepo NxlFileRepo { get; set; } = EnumFileRepo.UNKNOWN;
        private bool IsSwitchServer { get; set; }
        #endregion

        public FileInfoWin(IFileInfo info)
        {
            InitializeComponent();

            InitData(info);

            InitFileInfoPageViewModel();
            InitFileInfoPageCommand();
            this.frm.Content = fileInfoPage;
        }

        public FileInfoWin(string jsonstr)
        {
            InitializeComponent();

            InitData(jsonstr);

            InitFileInfoPageViewModel();
            InitFileInfoPageCommand();
            this.frm.Content = fileInfoPage;
        }

        private void InitData(IFileInfo info)
        {
            FileName = info.Name;
            FilePath = info.LocalDiskPath;
            FileSize = info.Size;
            LastModifiedTime= info.LastModified.ToLocalTime().ToString();
            IsAdhoc = info.IsByAdHoc;
            IsCentralPolicy = info.IsByCentrolPolicy;
            Emails = info.Emails;
            NxlFileRights = info.Rights.ToList();
            NxlTags = info.Tags;
            NxlWaterMark = info.WaterMark;
            NxlExpiration = info.Expiration;
            NxlFileRepo = info.FileRepo;
        }

        private void InitData(string jsonstr)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonstr);
            if (jo.ContainsKey("FileName"))
            {
                FileName = jo["FileName"].ToString();
            }
            if (jo.ContainsKey("LocalDiskPath"))
            {
                FilePath = jo["LocalDiskPath"].ToString();
            }
            if (jo.ContainsKey("Size"))
            {
                Int64 tempSize = 0;
                Int64.TryParse(jo["Size"].ToString(), out tempSize);
                FileSize = tempSize;
            }
            if (jo.ContainsKey("DateModified"))
            {
                LastModifiedTime = jo["DateModified"].ToString();
            }
            if (jo.ContainsKey("IsByAdHoc"))
            {
                IsAdhoc = (bool)jo.GetValue("IsByAdHoc");
            }
            if(jo.ContainsKey("IsSwitchServer"))
            {
                IsSwitchServer = (bool)jo.GetValue("IsSwitchServer");
            }
            if (jo.ContainsKey("IsByCentrolPolicy"))
            {
                IsCentralPolicy = (bool)jo.GetValue("IsByCentrolPolicy");
            }
            if (jo.ContainsKey("SharedWith"))
            {
                JArray ja = (JArray)jo.GetValue("SharedWith");
                if (ja != null && ja.Count > 0)
                {
                    string[] emails = new string[ja.Count];

                    for (int i = 0; i < ja.Count; i++)
                    {
                        emails[i] = ja[i].ToString();
                    }
                    Emails = emails;
                }
            }
            if (jo.ContainsKey("Rights"))
            {
                JArray ja = (JArray)jo.GetValue("Rights");
                if (ja != null && ja.Count > 0)
                {
                    List<FileRights> rights = new List<FileRights>();
                    foreach (var one in ja)
                    {
                        int value = (int)one;
                        rights.Add((FileRights)value);
                    }
                    NxlFileRights = rights;
                }
            }
            if (jo.ContainsKey("Tags"))
            {
                string Tags = jo["Tags"].ToString();

                if (Tags.Length > 2)  // at least "{}"
                {
                    try
                    {
                        Dictionary<string, List<string>> jarr = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(Tags);
                        NxlTags = jarr;
                    }
                    catch (Exception e)
                    {
                        SkydrmApp.Singleton.Log.Error("Error in FileInfoWin:", e);
                    }
                }
            }
            if (jo.ContainsKey("AdhocWaterMark"))
            {
                NxlWaterMark = jo["AdhocWaterMark"].ToString();                                          
            }
            if (jo.ContainsKey("Expiration"))
            {
                Expiration expiration = new Expiration();
                JObject jObject = (JObject)jo.GetValue("Expiration");
                expiration.type = (ExpiryType)((int)jObject.GetValue("type"));
                expiration.Start = (Int64)jObject.GetValue("Start");
                expiration.End = (Int64)jObject.GetValue("End");

                NxlExpiration = expiration;
            }
            if (jo.ContainsKey("EnumFileRepo"))
            {
                Int32 temp = -1;
                string FileRepo = jo["EnumFileRepo"].ToString();
                bool b = int.TryParse(FileRepo, out temp);
                if (!b)
                {
                    NxlFileRepo = EnumFileRepo.UNKNOWN;
                }
                else
                {
                    EnumFileRepo enumFileRepo = (EnumFileRepo)Enum.ToObject(typeof(EnumFileRepo), temp);
                    NxlFileRepo = enumFileRepo;
                }
            }
        }

        #region InitFileInfoPage
        private void InitFileInfoPageViewModel()
        {
            fileInfoPage.ViewModel.FileName = FileName;

            fileInfoPage.ViewModel.FileSize = FormatFileSize(FileSize);
            
            fileInfoPage.ViewModel.LastModifiedTime = LastModifiedTime;

            if (!string.IsNullOrWhiteSpace(NxlWaterMark) && !NxlFileRights.Contains(FileRights.RIGHT_WATERMARK))
            {
                NxlFileRights.Add(FileRights.RIGHT_WATERMARK);
            }

            if (!IsAdhoc && !IsCentralPolicy)// not have any permissions, check whether file is centralpolicy
            {
                try
                {
                    var tag = app.Rmsdk.User.GetNxlTagsWithoutToken(FilePath);
                    if (tag.Count > 0)
                    {
                        IsCentralPolicy = true;
                        NxlTags = tag;
                    }
                }
                catch (Exception e)
                {
                    app.Log.Error(e);
                }
            }

            if (!IsCentralPolicy)
            {
                fileInfoPage.ViewModel.FileType = CustomControls.components.FileType.Adhoc;

                foreach (string one in Emails)
                {
                    if (!string.IsNullOrEmpty(one))
                    {
                        fileInfoPage.ViewModel.Emails.Add(one);
                    }
                }
                fileInfoPage.ViewModel.SharedDescMargin = new Thickness(155,0,0,0);
                fileInfoPage.ViewModel.EmailListMargin = new Thickness(155, 9, 0, 0);

                if (NxlFileRights.Count > 0)
                {
                    fileInfoPage.ViewModel.AdhocAccessDenyVisibility = Visibility.Collapsed;
                    bool isAddWarterMark = !string.IsNullOrWhiteSpace(NxlWaterMark);
                    fileInfoPage.ViewModel.AdhocRightsVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(NxlFileRights.ToArray(), isAddWarterMark);
                    fileInfoPage.ViewModel.AdhocRightsVM.WatermarkValue = NxlWaterMark;
                    fileInfoPage.ViewModel.AdhocRightsVM.WaterPanlVisibility = isAddWarterMark ?
                         Visibility.Visible : Visibility.Collapsed;
                    DataTypeConvertHelper.SdkExpiry2CustomCtrExpiry(NxlExpiration, out string expiryDate, false);

                    ExpiryType operationType = NxlExpiration.type;
                    if (operationType != ExpiryType.NEVER_EXPIRE && DateTimeHelper.DateTimeToTimestamp(DateTime.Now) > NxlExpiration.End)
                    {
                        expiryDate = "Expired";
                    }
                    fileInfoPage.ViewModel.AdhocRightsVM.ValidityValue = expiryDate;
                }
                else
                {
                    fileInfoPage.ViewModel.AdhocAccessDenyVisibility = Visibility.Visible;
                }
            }
            else
            {
                fileInfoPage.ViewModel.FileType = CustomControls.components.FileType.CentralPolicy;

                fileInfoPage.ViewModel.ClassifiedRightsVM.CentralTag = NxlTags;
                fileInfoPage.ViewModel.ClassifiedRightsVM.CentralTagScrollViewMargin = new Thickness(200,0,0,0);

                if (NxlFileRights.Count > 0)
                {
                    fileInfoPage.ViewModel.ClassifiedRightsVM.AccessDenyVisibility = Visibility.Collapsed;
                    bool isAddWarterMark = !string.IsNullOrWhiteSpace(NxlWaterMark);
                    fileInfoPage.ViewModel.ClassifiedRightsVM.RightsDisplayVM.FileRights = DataTypeConvertHelper.SDKRights2CustomControlRights(NxlFileRights.ToArray(), isAddWarterMark, false);
                    fileInfoPage.ViewModel.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue =NxlWaterMark;
                    fileInfoPage.ViewModel.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = isAddWarterMark ? Visibility.Visible : Visibility.Collapsed;
                    fileInfoPage.ViewModel.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = Visibility.Collapsed;
                }
                else
                {
                    fileInfoPage.ViewModel.ClassifiedRightsVM.AccessDenyVisibility = Visibility.Visible;
                    if (IsSwitchServer)
                    {
                        fileInfoPage.ViewModel.ClassifiedRightsVM.AccessDenyText = "You are not authorized to view this document.";
                    }
                }
            }

            switch (NxlFileRepo)
            {
                case EnumFileRepo.REPO_MYVAULT:
                    fileInfoPage.ViewModel.SharedDesc=$"Shared with {fileInfoPage.ViewModel.Emails.Count} members";
                    break;
                case EnumFileRepo.REPO_SHARED_WITH_ME:
                    fileInfoPage.ViewModel.SharedDesc = "Shared by";
                    break;
            }
        }
        private void InitFileInfoPageCommand()
        {
            CommandBinding binding;
            binding = new CommandBinding(FileInfo_DataCommands.Close);
            binding.Executed += CloseCommand;
            this.CommandBindings.Add(binding);
        }
        private void CloseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private string FormatFileSize(Int64 fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00} GB", ((Double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00} MB", ((Double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00} KB", ((Double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0} bytes", fileSize);
            }
        }
        #endregion

        /// <summary>
        ///  When set window SizeToContent(attribute),the WindowStartupLocation will failure
        ///  Use this method to display UI.
        /// </summary>
        /// <param name="sizeInfo"></param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            //Calculate half of the offset to move the form

            if (sizeInfo.HeightChanged)
                this.Top += (sizeInfo.PreviousSize.Height - sizeInfo.NewSize.Height) / 2;

            if (sizeInfo.WidthChanged)
                this.Left += (sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width) / 2;
        }

    }
}
