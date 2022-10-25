using CustomControls;
using CustomControls.components.ValiditySpecify.model;
using SkydrmLocal.rmc.sdk;
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
using Viewer.upgrade.cookie;
using Viewer.upgrade.database;
using Viewer.upgrade.file.basic;

namespace Viewer.upgrade.ui.common.fileInfoWindow
{
    /// <summary>
    /// Interaction logic for FileInfoWin.xaml
    /// </summary>
    public partial class FileInfoWin : Window
    {
        private readonly FileInfoPage fileInfoPage = new FileInfoPage();

        private List<FileRights> NxlFileRights = new List<FileRights>();

        private string[] Emails = new string[] { };

        public FileInfoWin(INxlFile nxlFile, Cookie cookie)
        {
            InitializeComponent();
            this.Title = "SkyDRM DESKTOP";
            this.frm.Content = fileInfoPage;
            this.Emails = cookie.Emails;
            InitFileInfoPageViewModel(nxlFile);
            InitFileInfoPageCommand();
        }

        #region InitFileInfoPage
        private void InitFileInfoPageViewModel(INxlFile nxlFile)
        {
            fileInfoPage.ViewModel.FileName = nxlFile.FileName;

            fileInfoPage.ViewModel.FileSize = FormatFileSize(nxlFile.NxlFileFingerPrint.size);

            DateTime dateTime = JavaTimeConverter.ToCSDateTime(nxlFile.NxlFileFingerPrint.modified).ToLocalTime();
            fileInfoPage.ViewModel.LastModifiedTime = dateTime.ToLocalTime().ToString(); ;

            NxlFileRights = nxlFile.NxlFileFingerPrint.rights.ToList();

            if (!string.IsNullOrWhiteSpace(nxlFile.WatermarkInfo.WaterMarkRaw) && !NxlFileRights.Contains(FileRights.RIGHT_WATERMARK))
            {
                NxlFileRights.Add(FileRights.RIGHT_WATERMARK);
            }

            if (!nxlFile.NxlFileFingerPrint.isByCentrolPolicy)
            {
                fileInfoPage.ViewModel.FileType = CustomControls.components.FileType.Adhoc;

                foreach (string one in Emails)
                {
                    if (!string.IsNullOrEmpty(one))
                    {
                        fileInfoPage.ViewModel.Emails.Add(one);
                    }
                }
                fileInfoPage.ViewModel.SharedDescMargin = new Thickness(155, 0, 0, 0);
                fileInfoPage.ViewModel.EmailListMargin = new Thickness(155, 9, 0, 0);

                if (NxlFileRights.Count > 0)
                {
                    fileInfoPage.ViewModel.AdhocAccessDenyVisibility = Visibility.Collapsed;
                    bool isAddWarterMark = !string.IsNullOrWhiteSpace(nxlFile.WatermarkInfo.WaterMarkRaw);
                    fileInfoPage.ViewModel.AdhocRightsVM.FileRights = SDKRights2CustomControlRights(NxlFileRights.ToArray(), isAddWarterMark);
                    fileInfoPage.ViewModel.AdhocRightsVM.WatermarkValue = nxlFile.WatermarkInfo.Text;
                    fileInfoPage.ViewModel.AdhocRightsVM.WaterPanlVisibility = isAddWarterMark ? Visibility.Visible : Visibility.Collapsed;
                    SdkExpiry2CustomCtrExpiry(nxlFile.NxlFileFingerPrint.expiration, out string expiryDate, false);

                    ExpiryType operationType = nxlFile.NxlFileFingerPrint.expiration.type;
                    if (operationType != ExpiryType.NEVER_EXPIRE && DateTimeToTimestamp(DateTime.Now) > nxlFile.NxlFileFingerPrint.expiration.End)
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
                fileInfoPage.ViewModel.ClassifiedRightsVM.CentralTag = nxlFile.NxlFileFingerPrint.tags;
                fileInfoPage.ViewModel.ClassifiedRightsVM.CentralTagScrollViewMargin = new Thickness(200, 0, 0, 0);

                if (NxlFileRights.Count > 0)
                {
                    fileInfoPage.ViewModel.ClassifiedRightsVM.AccessDenyVisibility = Visibility.Collapsed;
                    bool isAddWarterMark = !string.IsNullOrWhiteSpace(nxlFile.WatermarkInfo.WaterMarkRaw);
                    fileInfoPage.ViewModel.ClassifiedRightsVM.RightsDisplayVM.FileRights = SDKRights2CustomControlRights(NxlFileRights.ToArray(), isAddWarterMark, false);
                    fileInfoPage.ViewModel.ClassifiedRightsVM.RightsDisplayVM.WatermarkValue = nxlFile.WatermarkInfo.Text;
                    fileInfoPage.ViewModel.ClassifiedRightsVM.RightsDisplayVM.WaterPanlVisibility = isAddWarterMark ? Visibility.Visible : Visibility.Collapsed;
                    fileInfoPage.ViewModel.ClassifiedRightsVM.RightsDisplayVM.ValidityPanlVisibility = Visibility.Collapsed;
                }
                else
                {
                    fileInfoPage.ViewModel.ClassifiedRightsVM.AccessDenyVisibility = Visibility.Visible;
                }
            }

            if (nxlFile.NxlFileFingerPrint.isFromMyVault)
            {
                if (nxlFile.NxlFileFingerPrint.isOwner)
                {
                    fileInfoPage.ViewModel.SharedDesc = $"Shared with {fileInfoPage.ViewModel.Emails.Count} members";
                }
                else
                {
                    fileInfoPage.ViewModel.SharedDesc = "Shared by";
                }
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

        public static HashSet<CustomControls.components.DigitalRights.model.FileRights> SDKRights2CustomControlRights(FileRights[] rights, bool isAddWaterMark = true, bool isAddVilidity = true)
        {
            HashSet<CustomControls.components.DigitalRights.model.FileRights> fileRights = new HashSet<CustomControls.components.DigitalRights.model.FileRights>();
            foreach (var item in rights)
            {
                switch (item)
                {
                    case FileRights.RIGHT_VIEW:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_VIEW);
                        break;
                    case FileRights.RIGHT_EDIT:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_EDIT);
                        break;
                    case FileRights.RIGHT_PRINT:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_PRINT);
                        break;
                    case FileRights.RIGHT_CLIPBOARD:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_CLIPBOARD);
                        break;
                    case FileRights.RIGHT_SAVEAS:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SAVEAS);
                        break;
                    case FileRights.RIGHT_DECRYPT:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT);
                        break;
                    case FileRights.RIGHT_SCREENCAPTURE:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SCREENCAPTURE);
                        break;
                    case FileRights.RIGHT_SEND:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SEND);
                        break;
                    case FileRights.RIGHT_CLASSIFY:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_CLASSIFY);
                        break;
                    case FileRights.RIGHT_SHARE:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SHARE);
                        break;
                    case FileRights.RIGHT_DOWNLOAD:
                        //when protect file will use download right instead of saveAs right
                        // so nxl file download right should display saveAs right
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SAVEAS);
                        break;
                }
            }
            if (isAddWaterMark)
            {
                fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK);
            }
            if (isAddVilidity)
            {
                fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_VALIDITY);
            }
            return fileRights;
        }

        public static IExpiry SdkExpiry2CustomCtrExpiry(Expiration expiration, out string expiryDate, bool isFromUserPreference = false)
        {
            expiryDate = "Never expire";
            IExpiry expiry = new NeverExpireImpl();
            switch (expiration.type)
            {
                case ExpiryType.NEVER_EXPIRE:
                    expiry = new NeverExpireImpl();
                    expiryDate = "Never expire";
                    break;
                case ExpiryType.RELATIVE_EXPIRE:
                    if (isFromUserPreference)
                    {
                        int years = (int)(expiration.Start >> 32);
                        int months = (int)expiration.Start;
                        int weeks = (int)(expiration.End >> 32);
                        int days = (int)expiration.End;
                        expiry = new RelativeImpl(years, months, weeks, days);

                        DateTime dateStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                        string dateRelativeS = dateStart.ToString("MMMM dd, yyyy");
                        if (years == 0 && months == 0 && weeks == 0 && days == 0)
                        {
                            days = 1;
                        }
                        DateTime dateEnd = dateStart.AddYears(years).AddMonths(months).AddDays(7 * weeks + days - 1).AddHours(23).AddMinutes(59).AddSeconds(59);
                        string dateRelativeE = dateEnd.ToString("MMMM dd, yyyy");
                        expiryDate = dateRelativeS + " To " + dateRelativeE;
                    }
                    else
                    {
                        string dateRelativeS = TimestampToDateTime(expiration.Start);
                        string dateRelativeE = TimestampToDateTime(expiration.End);
                        expiry = new RelativeImpl(0, 0, 0, CountDays(Convert.ToDateTime(dateRelativeS).Ticks, Convert.ToDateTime(dateRelativeE).Ticks));
                        expiryDate = "Until " + dateRelativeE;
                    }
                    break;
                case ExpiryType.ABSOLUTE_EXPIRE:
                    string dateAbsoluteE = TimestampToDateTime(expiration.End);
                    expiry = new AbsoluteImpl(expiration.End);
                    expiryDate = "Until " + dateAbsoluteE;
                    break;
                case ExpiryType.RANGE_EXPIRE:
                    string dateRangeS = TimestampToDateTime(expiration.Start);
                    string dateRangeE = TimestampToDateTime(expiration.End);
                    expiry = new RangeImpl(expiration.Start, expiration.End);
                    expiryDate = dateRangeS + " To " + dateRangeE;
                    break;
            }
            return expiry;
        }

        public static string TimestampToDateTime(long time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            DateTime newTime = startDateTime.AddMilliseconds(time);
            return newTime.ToString("MMMM dd, yyyy");
        }

        private static int CountDays(long startMillis, long endMillis)
        {
            long elapsedTicks = endMillis - startMillis;
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
            return elapsedSpan.Days + 1;
        }
        public static long DateTimeToTimestamp(DateTime time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            return Convert.ToInt64((time - startDateTime).TotalMilliseconds);
        }

    }
}
