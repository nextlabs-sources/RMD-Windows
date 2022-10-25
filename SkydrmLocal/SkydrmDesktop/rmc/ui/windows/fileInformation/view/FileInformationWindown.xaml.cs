using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using static SkydrmLocal.rmc.fileSystem.project.ProjectRepo;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.common.helper;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;

namespace SkydrmLocal.rmc.ui.windows
{
    /// <summary>
    /// Interaction logic for FileInformationWindown.xaml
    /// </summary>
    public partial class FileInformationWindow : Window, INotifyPropertyChanged
    {
        // Application
        private SkydrmApp App = (SkydrmApp)SkydrmApp.Current;

        public event PropertyChangedEventHandler PropertyChanged;

        private const string DATE_FORMATTER = "dd MMMM yyyy";

        private ObservableCollection<string> sharedWith = new ObservableCollection<string>();

        public ObservableCollection<string> SharedWith
        {
            get { return sharedWith; }
            set { sharedWith = value; }
        }


        public string fileName = string.Empty;

        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileName"));

            }
        }

        public string filePath = string.Empty;

        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePath"));

            }
        }

        public string displayLastModified = string.Empty;

        public string DisplayLastModified
        {
            get { return displayLastModified; }
            set
            {
                displayLastModified = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayLastModified"));
            }
        }

        public string fileSize = string.Empty;

        public string FileSize
        {
            get { return fileSize; }
            set
            {
                fileSize = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileSize"));
            }
        }

        public string displayExpiration = string.Empty;

        public string DisplayExpiration
        {
            get { return displayExpiration; }
            set
            {
                displayExpiration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayExpiration"));

            }
        }

        public string displayWaterMark = string.Empty;

        public string DisplayWaterMark
        {
            get { return displayWaterMark; }
            set
            {
                SetWaterMark(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayWaterMark"));
            }
        }
        private const string DOLLAR_USER = "$(User)";
        private const string DOLLAR_BREAK = "$(Break)";
        private const string DOLLAR_DATE = "$(Date)";
        private const string DOLLAR_TIME = "$(Time)";
        private const string PRESET_VALUE_EMAIL_ID="User ID";
        private const string PRESET_VALUE_DATE="Date";
        private const string PRESET_VALUE_TIME="Time";
        private const string PRESET_VALUE_LINE_BREAK="Line break";
        private void SetWaterMark(string value)
        {
            if (value == null)
            {
                return;
            }

            string initWatermark = value;
            if (initWatermark.Contains("\n"))
            {
                initWatermark = initWatermark.Replace("\n", DOLLAR_BREAK);
            }
            displayWaterMark = initWatermark;

            this.tbWaterMark.Inlines.Clear();
            
            ConvertString2PresetValue(initWatermark);
        }

        private void ConvertString2PresetValue(string initValue)
        {
            if (string.IsNullOrEmpty(initValue))
            {
                return;
            }

            char[] array = initValue.ToCharArray();
            // record preset value begin index
            int beginIndex = -1;
            // record preset value end index
            int endIndex = -1;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == '$')
                {
                    beginIndex = i;
                }
                else if (array[i] == ')')
                {
                    endIndex = i;
                }

                if (beginIndex != -1 && endIndex != -1 && beginIndex < endIndex)
                {

                    // append text before preset value
                    Run run = new Run(initValue.Substring(0, beginIndex));
                    this.tbWaterMark.Inlines.Add(run);

                    // judge if is preset
                    string subStr = initValue.Substring(beginIndex, endIndex - beginIndex + 1);

                    if (subStr.Equals(DOLLAR_USER))
                    {
                        AddPreset(DOLLAR_USER);
                    }
                    else if (subStr.Equals(DOLLAR_BREAK))
                    {
                        AddPreset(DOLLAR_BREAK);
                    }
                    else if (subStr.Equals(DOLLAR_DATE))
                    {
                        AddPreset(DOLLAR_DATE);
                    }
                    else if (subStr.Equals(DOLLAR_TIME))
                    {
                        AddPreset(DOLLAR_TIME);
                    }
                    else
                    {
                        Run r = new Run(subStr);
                        this.tbWaterMark.Inlines.Add(r);
                    }

                    // quit
                    break;
                }
            }

            if (beginIndex == -1 || endIndex == -1 || beginIndex > endIndex) // have not preset
            {
                Run run = new Run(initValue);
                this.tbWaterMark.Inlines.Add(run);
            }
            else if (beginIndex < endIndex)
            {
                if (endIndex + 1 < initValue.Length)
                {
                    // Converter the remaining by recursive
                    ConvertString2PresetValue(initValue.Substring(endIndex + 1));
                }
            }
        }
        private void AddPreset(string preset)
        {
            Run run = new Run();

            Run space = new Run(" ");
            this.tbWaterMark.Inlines.Add(space);

            switch (preset)
            {
                case DOLLAR_USER:
                    run.Text = PRESET_VALUE_EMAIL_ID;
                    run.Background = new SolidColorBrush(Color.FromRgb(0XD4, 0XEF, 0XDF));
                    break;
                case DOLLAR_BREAK:
                    run.Text = PRESET_VALUE_LINE_BREAK;
                    run.Background = new SolidColorBrush(Color.FromRgb(0XFA, 0XD7, 0XB8));
                    break;
                case DOLLAR_DATE:
                    run.Text = PRESET_VALUE_DATE;
                    run.Background = new SolidColorBrush(Color.FromRgb(0XD4, 0XEF, 0XDF));
                    break;
                case DOLLAR_TIME:
                    run.Text = PRESET_VALUE_TIME;
                    run.Background = new SolidColorBrush(Color.FromRgb(0XD4, 0XEF, 0XDF));
                    break;
                default:
                    break;
            }

            this.tbWaterMark.Inlines.Add(run);

            Run space2 = new Run(" ");
            this.tbWaterMark.Inlines.Add(space2);
        }

        public ObservableCollection<WrapperFileRights> displayFileRights = new ObservableCollection<WrapperFileRights>();

        public ObservableCollection<WrapperFileRights> DisplayFileRights
        {
            get { return displayFileRights; }
            set { displayFileRights = value; }
        }

        private bool hidenValidity = false;

        public bool HidenValidity
        {
            get
            {
                return hidenValidity;
            }
            set
            {
                hidenValidity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HidenValidity"));
            }
        }

        // For file owner, display document steward text.
        // And if the file is ByCentrolPolicy, isOwnerVisibility = Visibility.Collapsed; 
        // Here, according tag, to determine whether isOwnerVisibility = Visibility.Collapsed
        private Visibility isOwnerVisibility = Visibility.Collapsed;
        public Visibility IsOwnerVisibility
        {
            get
            {
                return isOwnerVisibility;
            }
            set
            {
                isOwnerVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsOwnerVisibility"));
            }
        }

        // Fix bug 53734.
        // For share with me file. If  EnumFileRepo is REPO_SHARED_WITH_ME,should display 'Shared by' not 'Shared with'.
        private Visibility isShareWithVisibility = Visibility.Visible;
        public Visibility IsShareWithVisibility
        {
            get
            {
                return isShareWithVisibility;
            }
            set
            {
                isShareWithVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsShareWithVisibility"));
            }
        }

        public FileInformationWindow(IFileInfo info)
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Window_Load);
            this.Activated += new EventHandler(delegate (object sender, EventArgs e)
            {
                this.Topmost = false;
                this.Focus();
            });

            try
            {
                InitData(info);
            }
            catch (Exception ex)
            {
                Denied_PromptInfo.Text = CultureStringInfo.ApplicationFindResource("Exception_Sdk_Insufficient_Rights");
                Access_Denied_Containe.Visibility = Visibility.Visible;
                Console.WriteLine(ex.ToString());
            }
        }


        private void InitData(IFileInfo info)
        {

            FileName = info.Name;

            FileSize = FormatFileSize(info.Size);

            // DisplayLastModified = info.LastModified.ToLocalTime().ToString();
            try
            {
                NxlFileFingerPrint nxlFileFingerPrint = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(info.LocalDiskPath);
                DateTime dateTime = JavaTimeConverter.ToCSDateTime(nxlFileFingerPrint.modified).ToLocalTime();
                DisplayLastModified = dateTime.ToLocalTime().ToString();
            }
            catch (Exception ex)
            {
      
            }
         
            if (info.IsCreatedLocal)
            {
                // FilePath = info.LocalDiskPath;
                FilePath = FileName;
            }
            else
            {
                FilePath = info.RmsRemotePath;
            }

            IsOwnerVisibility = Visibility.Collapsed;
            DisplayWaterMark = "";
            rights = new FileRights[0];
            Expiration e = new Expiration() { type = ExpiryType.NEVER_EXPIRE };
            try
            {
                IsOwnerVisibility = info.IsOwner ? Visibility.Visible : Visibility.Collapsed;
                DisplayWaterMark = info.WaterMark;
                rights = info.Rights;
                e = info.Expiration;
            }
            catch(Exception)
            {

            }
            WrapperRights(rights);
            UpdateValidity(e);

            switch (info.FileRepo)
            {
                case EnumFileRepo.EXTERN:

                    break;
                case EnumFileRepo.REPO_MYVAULT:
                    HandShareWithByInfo(info);
                    break;
                case EnumFileRepo.REPO_SHARED_WITH_ME:
                    IsShareWithVisibility = Visibility.Collapsed;
                    HandShareWithByInfo(info);
                    break;
                case EnumFileRepo.REPO_WORKSPACE:
                case EnumFileRepo.REPO_PROJECT:
                case EnumFileRepo.REPO_EXTERNAL_DRIVE:
                    bool isByCentrolPolicy = false;
                    try
                    {
                        isByCentrolPolicy = info.IsByCentrolPolicy;
                    }catch(Exception)
                    {
                        isByCentrolPolicy = !NxlHelper.PeekHasValidAdhocSection(info.LocalDiskPath);
                    }

                    if(isByCentrolPolicy)
                    {
                        UIProjectContainer.Visibility = Visibility.Visible;
                        IsOwnerVisibility = Visibility.Collapsed;
                        HidenValidity = true;

                        Dictionary<string, List<string>> t = null;
                        try
                        {
                            t = info.Tags;
                        }
                        catch(Exception)
                        {
                            t = SkydrmApp.Singleton.Rmsdk.User.GetNxlTagsWithoutToken(info.LocalDiskPath);
                        }

                        TagView.InitializeTags(t);

                        // Fix bug 54218
                        RemoveValidityRight();
                    }

                    break;
                case EnumFileRepo.UNKNOWN:
                    break;
            }
        }

        public FileInformationWindow(string jsonstr)
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Window_Load);
            this.Activated += new EventHandler(delegate (object sender, EventArgs e)
            {
                this.Topmost = false;
                this.Focus();
            });

            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonstr);

            // tmp path
            if (jo.ContainsKey("IsConverterSucceed"))
            {
                bool isSucceed = (bool)jo.GetValue("IsConverterSucceed");

                if (!isSucceed)
                {
                    return;
                }
            }

            // FileName 
            if (jo.ContainsKey("FileName"))
            {
                FileName = jo["FileName"].ToString();
            }

            // FileName 
            if (jo.ContainsKey("Size"))
            {
                Int64 tempSize = 0;
                Int64.TryParse(jo["Size"].ToString(), out tempSize);
                FileSize = FormatFileSize(tempSize);
            }

            if (jo.ContainsKey("Expiration"))
            {
                Expiration expiration = new Expiration();
                JObject jObject = (JObject)jo.GetValue("Expiration");
                expiration.type = (ExpiryType)((int)jObject.GetValue("type"));
                expiration.Start = (Int64)jObject.GetValue("Start");
                expiration.End = (Int64)jObject.GetValue("End");

                UpdateValidity(expiration);
            }

            if (jo.ContainsKey("AdhocWaterMark"))
            {
                DisplayWaterMark = jo["AdhocWaterMark"].ToString();
                //StringBuilder wmText = new StringBuilder();
                //CommonUtils.ConvertWatermark2DisplayStyle(AdhocWatermark, ref wmText);                                               
            }

            // rights ---- need to test.
            if (jo.ContainsKey("Rights"))
            {
                JArray ja = (JArray)jo.GetValue("Rights");
                if (ja != null && ja.Count > 0)
                {
                    IList<FileRights> rights = new List<FileRights>();
                    foreach (var one in ja)
                    {
                        int value = (int)one;
                        rights.Add((FileRights)value);
                    }
                    //If policy has watermark value 
                    //then should diplay watermark icon in rights icon column.
                    FileRights[] r = rights.ToArray();
                    WrapperRights(r);
                }
            }

            bool IsCreatedLocal = true;
            string RmsRemotePath = string.Empty;
            string LocalDiskPath = string.Empty;
            if (jo.ContainsKey("IsCreatedLocal"))
            {
                IsCreatedLocal = (bool)jo.GetValue("IsCreatedLocal");
            }

            if (jo.ContainsKey("RmsRemotePath"))
            {
                RmsRemotePath = jo["RmsRemotePath"].ToString();
            }

            if (jo.ContainsKey("LocalDiskPath"))
            {
                LocalDiskPath = jo["LocalDiskPath"].ToString();
            }

            if (IsCreatedLocal)
            {
                FilePath = FileName;
            }
            else
            {
                FilePath = RmsRemotePath;
            }

            // judge isOwner
            bool isOwner = false;
            if (jo.ContainsKey("IsOwner"))
            {
                isOwner = (bool)jo.GetValue("IsOwner");
            }
            IsOwnerVisibility = isOwner ? Visibility.Visible : Visibility.Collapsed;

            if (jo.ContainsKey("DateModified"))
            {
                DisplayLastModified = jo["DateModified"].ToString();
            }

            if (jo.ContainsKey("EnumFileRepo"))
            {
                Int32 temp = -1;
                string FileRepo = jo["EnumFileRepo"].ToString();
                bool b = int.TryParse(FileRepo, out temp);

                if (!b)
                {
                    return;
                }

                // Judge file is Adhoc or CentrolPolicy 
                bool isByAdHoc = false;
                bool isByCentrolPolicy = false;
                if (jo.ContainsKey("IsByAdHoc"))
                {
                    isByAdHoc = (bool)jo.GetValue("IsByAdHoc");
                }
                if (jo.ContainsKey("IsByCentrolPolicy"))
                {
                    isByCentrolPolicy = (bool)jo.GetValue("IsByCentrolPolicy");
                }

                EnumFileRepo enumFileRepo = (EnumFileRepo)Enum.ToObject(typeof(EnumFileRepo), temp);             

                switch (enumFileRepo)
                {
                    case EnumFileRepo.UNKNOWN:
                        break;
                    case EnumFileRepo.EXTERN:
                        // add feature, external support tags
                        HandleCentralPolicy(jo, isByCentrolPolicy);
                        break;
                    case EnumFileRepo.REPO_SHARED_WITH_ME:
                        IsShareWithVisibility = Visibility.Collapsed;
                        HandShareWithByJson(jo);
                        break;
                    case EnumFileRepo.REPO_MYVAULT:
                        HandShareWithByJson(jo);
                        break;
                    case EnumFileRepo.REPO_PROJECT:
                        HandleCentralPolicy(jo, isByCentrolPolicy);
                        break;
                }
            }

        }

        private void HandUIMyVaultContainer()
        {
            UIMyVaultContainer.Visibility = Visibility.Visible;
        }
        private void HandShareWithByInfo(IFileInfo info)
        {
            HandUIMyVaultContainer();

            foreach (string one in info.Emails)
            {
                if (!String.IsNullOrEmpty(one))
                {
                    SharedWith.Add(one);
                }
            }
        }
        private void HandShareWithByJson(JObject jo)
        {
            HandUIMyVaultContainer();

            if (jo.ContainsKey("SharedWith"))
            {
                JArray ja = (JArray)jo.GetValue("SharedWith");
                if (ja != null && ja.Count > 0)
                {
                    string[] Emails = new string[ja.Count];

                    for (int i = 0; i < ja.Count; i++)
                    {
                        Emails[i] = ja[i].ToString();
                    }

                    foreach (string one in Emails)
                    {
                        if (!String.IsNullOrEmpty(one))
                        {
                            SharedWith.Add(one);
                        }
                    }

                }
            }
        }

        private void HandleCentralPolicy(JObject jo, bool isByCentrolPolicy)
        {
            if (jo.ContainsKey("Tags"))
            {
                string Tags = jo["Tags"].ToString();

                if (isByCentrolPolicy)
                {
                    HidenValidity = true;
                    UIProjectContainer.Visibility = Visibility.Visible;
                    IsOwnerVisibility = Visibility.Collapsed;

                    if (Tags.Length > 2)  // at least "{}"
                    {
                        try
                        {
                            Dictionary<string, List<string>> jarr = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(Tags);
                            TagView.InitializeTags(jarr);
                        }
                        catch (Exception e)
                        {
                            SkydrmApp.Singleton.Log.Error("Error in FileInformationWindown:", e);
                        }
                    }
                    // Fix bug 54218
                    RemoveValidityRight();
                }

            }
        }

        public String FormatFileSize(Int64 fileSize)
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

        private void WrapperRights(FileRights[] rights)
        {
            if (null == rights || rights.Length == 0)
            {
                Denied_PromptInfo.Text = CultureStringInfo.ApplicationFindResource("Exception_Sdk_Insufficient_Rights");
                Access_Denied_Containe.Visibility = Visibility.Visible;
                return;
            }

            HashSet<WrapperFileRights> right_set = new HashSet<WrapperFileRights>();

            foreach (FileRights f in rights)
            {
                switch (f)
                {
                    case FileRights.RIGHT_VIEW:
                        right_set.Add(WrapperFileRights.RIGHT_VIEW);
                        break;
                    case FileRights.RIGHT_EDIT:
                        right_set.Add(WrapperFileRights.RIGHT_EDIT);
                        break;
                    case FileRights.RIGHT_PRINT:
                        right_set.Add(WrapperFileRights.RIGHT_PRINT);
                        break;
                    case FileRights.RIGHT_CLIPBOARD:
                        right_set.Add(WrapperFileRights.RIGHT_CLIPBOARD);
                        break;
                    case FileRights.RIGHT_SAVEAS:
                        right_set.Add(WrapperFileRights.RIGHT_SAVEAS);
                        break;
                    case FileRights.RIGHT_DECRYPT:
                        right_set.Add(WrapperFileRights.RIGHT_DECRYPT);
                        break;
                    case FileRights.RIGHT_SCREENCAPTURE:
                        right_set.Add(WrapperFileRights.RIGHT_SCREENCAPTURE);
                        break;
                    case FileRights.RIGHT_SEND:
                        right_set.Add(WrapperFileRights.RIGHT_SEND);
                        break;
                    case FileRights.RIGHT_CLASSIFY:
                        right_set.Add(WrapperFileRights.RIGHT_CLASSIFY);
                        break;
                    case FileRights.RIGHT_SHARE:
                        right_set.Add(WrapperFileRights.RIGHT_SHARE);
                        break;
                    // as PM required Windows platform must regard download as SaveAS
                    case FileRights.RIGHT_DOWNLOAD:
                        right_set.Add(WrapperFileRights.RIGHT_SAVEAS);
                        break;
                    case FileRights.RIGHT_WATERMARK:
                        right_set.Add(WrapperFileRights.RIGHT_WATERMARK);
                        break;
                }
            }

            foreach (var i in right_set)
            {
                DisplayFileRights.Add(i);
            }

            if (!string.IsNullOrEmpty(DisplayWaterMark))
            {
                if (!DisplayFileRights.Contains(WrapperFileRights.RIGHT_WATERMARK))
                {
                    DisplayFileRights.Add(WrapperFileRights.RIGHT_WATERMARK);
                }
            }

            DisplayFileRights.Add(WrapperFileRights.RIGHT_VALIDITY);

            // Fix bug 58676
            RemoveCurrentUnsupportRights();
        }

        private void RemoveCurrentUnsupportRights()
        {
            DisplayFileRights.Remove(WrapperFileRights.RIGHT_CLIPBOARD);
            DisplayFileRights.Remove(WrapperFileRights.RIGHT_SCREENCAPTURE);
            DisplayFileRights.Remove(WrapperFileRights.RIGHT_SEND);
            DisplayFileRights.Remove(WrapperFileRights.RIGHT_CLASSIFY);
        }

        private void RemoveValidityRight()
        {
            DisplayFileRights.Remove(WrapperFileRights.RIGHT_VALIDITY);
        }

        private void ShowMessageBox(string msg)
        {
            MessageBox.Show(this, "Application internal error. Please contact your system administrator for further help.", "SkyDRM DESKTOP");
        }

        private FileRights[] rights;
        private StringBuilder sb;

        public void UpdateValidity(Expiration expiration)
        {
            ExpiryType operationType = expiration.type;
            if (operationType != ExpiryType.NEVER_EXPIRE && DateTimeHelper.DateTimeToTimestamp(DateTime.Now) > expiration.End)
            {
                DisplayExpiration = "Expired";
                return;
            }
            switch (operationType)
            {
                case ExpiryType.NEVER_EXPIRE:
                    DisplayExpiration = CultureStringInfo.ApplicationFindResource("NeverExpire");

                    break;
                case ExpiryType.RELATIVE_EXPIRE:
                    string dateRelativeS = DateTimeHelper.TimestampToDateTime(expiration.Start);
                    string dateRelativeE = DateTimeHelper.TimestampToDateTime(expiration.End);
                    DisplayExpiration = "Until " + dateRelativeE;
                    break;
                case ExpiryType.ABSOLUTE_EXPIRE:
                    string dateAbsoluteS = DateTimeHelper.TimestampToDateTime(expiration.Start);
                    string dateAbsoluteE = DateTimeHelper.TimestampToDateTime(expiration.End);
                    DisplayExpiration = "Until " + dateAbsoluteE;
                    break;
                case ExpiryType.RANGE_EXPIRE:
                    string dateRangeS = DateTimeHelper.TimestampToDateTime(expiration.Start);
                    string dateRangeE = DateTimeHelper.TimestampToDateTime(expiration.End);
                    DisplayExpiration = dateRangeS + " To " + dateRangeE;
                    break;
            }
        }

        public enum WrapperFileRights
        {
            RIGHT_VIEW = 0x1,
            RIGHT_EDIT = 0x2,
            RIGHT_PRINT = 0x4,
            RIGHT_CLIPBOARD = 0x8,
            RIGHT_SAVEAS = 0x10,
            RIGHT_DECRYPT = 0x20,
            RIGHT_SCREENCAPTURE = 0x40,
            RIGHT_SEND = 0x80,
            RIGHT_CLASSIFY = 0x100,
            RIGHT_SHARE = 0x200,
            RIGHT_DOWNLOAD = 0x400,
            RIGHT_WATERMARK = 0x40000000,
            RIGHT_VALIDITY = 0x600
        }

        // Used to handle window display issue across processse(open the window from viewer process)
        private void Window_Load(object sender, RoutedEventArgs args)
        {
            this.Topmost = false;
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Escape || e.Key == Key.Enter)
            {
                this.Close();
            }
        }
    }
}
