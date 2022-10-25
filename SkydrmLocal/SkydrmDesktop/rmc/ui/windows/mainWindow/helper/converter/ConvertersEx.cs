using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmDesktop.rmc.ui.utils;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper.converter
{

        /// <summary>
        /// Convert local file status to bool to select large context menu or samll context menu.
        /// </summary>
        public class FileStatusEnum2Bool : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                 try
                 {
                     EnumNxlFileStatus status = (EnumNxlFileStatus)value;
                     switch (status)
                     {
                         case EnumNxlFileStatus.AvailableOffline:
                             return true;
                         default: // other file status, will popup small context menu.
                             break;
                     }
                     return false;
                 }
                 catch (Exception)
                 {
                     return false;
                 }

            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

    /// <summary>
    /// For search combox visibility.
    /// </summary>
    public class SearchComboxVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea status = (EnumCurrentWorkingArea)value;
                if (status== EnumCurrentWorkingArea.PROJECT || 
                    status == EnumCurrentWorkingArea.MYSPACE)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Visible;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// List file status icon
    /// </summary>
    public class ListFileStatus2IconConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value[0].GetType() != typeof(EnumNxlFileStatus))
                {
                    return null;
                }
                EnumNxlFileStatus nxlFileStatus = (EnumNxlFileStatus)value[0];
                bool isOffline = (bool)value[1];
                switch (nxlFileStatus)
                {
                    // Now "Leave a copy" can look as the kind of "Offline", so use the same icon.
                    case EnumNxlFileStatus.CachedFile:
                    case EnumNxlFileStatus.AvailableOffline:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileTypeStatus/offline.png", UriKind.Relative));
                    case EnumNxlFileStatus.Uploading:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileTypeStatus/uploading.png", UriKind.Relative));
                    case EnumNxlFileStatus.WaitingUpload:
                        return new BitmapImage(new Uri(@"/rmc/resources/fileTypeStatus/waitupload.png", UriKind.Relative));
                    case EnumNxlFileStatus.UploadFailed:
                        // now file support upload when status is waitingUpload or offline(AvailableOffline,CachedFile) file edited
                        // so these two status file maybe upload failed
                        if (isOffline)
                        {
                            return new BitmapImage(new Uri(@"/rmc/resources/fileTypeStatus/offline.png", UriKind.Relative));
                        }
                        return new BitmapImage(new Uri(@"/rmc/resources/fileTypeStatus/waitupload.png", UriKind.Relative));
                    default:
                        return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// List file type icon
    /// </summary>
    public class ListFile2IconConverterEx : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string name = (string)value[0];
                bool isFolder = (bool)value[1];
                bool isOffline = (bool)value[2];
                bool isNxl = (bool)value[3];

                if (isFolder)
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/Folder.png", UriKind.Relative));
                }

                string originFilename = name;
                if (isNxl)
                {
                    int lastindex = name.LastIndexOf('.');
                    if (lastindex != -1)
                    {
                        originFilename = name.Substring(0, lastindex); // remove .nxl
                    }
                }

                string fileType = System.IO.Path.GetExtension(originFilename); // return .txt or null or empty
                if (string.IsNullOrEmpty(fileType))
                {
                    fileType = "---";
                }
                else if (fileType.IndexOf('.') != -1)
                {
                    fileType = fileType.Substring(fileType.IndexOf('.') + 1).ToLower();
                    if (!FileIconSupportHelper.IsSupportFileTypeEx(fileType))
                    {
                        fileType = "---";
                    }
                }
                else
                {
                    fileType = "---";
                }

                string uritemp = "";
                if (isNxl)
                {
                    uritemp = string.Format(@"/rmc/resources/fileTypeIcons/{0}_G.png", fileType.ToUpper());
                }
                else
                {
                    uritemp = string.Format(@"/rmc/resources/fileTypeIcons/{0}.png", fileType.ToUpper());
                }
                var stream = new Uri(uritemp, UriKind.Relative);
                return new BitmapImage(stream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                return new BitmapImage(new Uri(@"/rmc/resources/fileTypeIcons/---_G.png", UriKind.Relative));
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// For file downloading, upload failed, edited attach extra image and text visibility
    /// </summary>
    public class SpecialFileStatus2SpVisibileConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            //< Binding Path = "FileStatus" />
            //< Binding Path = "IsMarkedOffline" />
            //< Binding Path = "IsEdit" />
            try
            {
                if (value[0].GetType() != typeof(EnumNxlFileStatus))
                {
                    return Visibility.Collapsed;
                }
                EnumNxlFileStatus nxlFileStatus = (EnumNxlFileStatus)value[0];
                bool isOffline = (bool)value[1];
                bool isEdit = (bool)value[2];

                if (nxlFileStatus == EnumNxlFileStatus.Downloading)
                {
                    return Visibility.Visible;
                }
                else if (nxlFileStatus == EnumNxlFileStatus.UploadFailed)
                {
                    return Visibility.Visible;
                }
                else if (isEdit)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return Visibility.Collapsed;
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// For file downloading, upload failed, edited attach extra image
    /// </summary>
    public class SpecialFileStatus2ImageConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            //< Binding Path = "FileStatus" />
            //< Binding Path = "IsMarkedOffline" />
            //< Binding Path = "IsEdit" />
            try
            {
                if (value[0].GetType() != typeof(EnumNxlFileStatus))
                {
                    return null;
                }
                EnumNxlFileStatus nxlFileStatus = (EnumNxlFileStatus)value[0];
                bool isOffline = (bool)value[1];
                bool isEdit = (bool)value[2];

                if (nxlFileStatus == EnumNxlFileStatus.Downloading)
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/icon_down_arrow.png", UriKind.Relative));
                }
                else if (nxlFileStatus == EnumNxlFileStatus.UploadFailed)
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/uploading_sign.png", UriKind.Relative));
                }
                else if (isEdit)
                {
                    return new BitmapImage(new Uri(@"/rmc/resources/icons/icon_yellow_rectangle.png", UriKind.Relative));
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// For file downloading, upload failed, edited attach extra text
    /// </summary>
    public class SpecialFileStatus2TextConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            //< Binding Path = "FileStatus" />
            //< Binding Path = "IsMarkedOffline" />
            //< Binding Path = "IsEdit" />
            try
            {
                if (value[0].GetType() != typeof(EnumNxlFileStatus))
                {
                    return "";
                }
                EnumNxlFileStatus nxlFileStatus = (EnumNxlFileStatus)value[0];
                bool isOffline = (bool)value[1];
                bool isEdit = (bool)value[2];

                if (nxlFileStatus == EnumNxlFileStatus.Downloading)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_List_Updating");
                }
                else if (nxlFileStatus == EnumNxlFileStatus.UploadFailed)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_List_UploadFailed");
                }
                else if (isEdit)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_List_Edited_In_Local");
                }
                else
                {
                    return "";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return "";
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FilterListSourcePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Visibility visibility = (Visibility)value;
                if (visibility==Visibility.Visible)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Visible;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UpdatingIconVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumNxlFileStatus status = (EnumNxlFileStatus)value;
                switch (status)
                {
                    case EnumNxlFileStatus.CachedFile:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.AvailableOffline:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.Uploading:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.WaitingUpload:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.Online:
                        return Visibility.Collapsed;
                    case EnumNxlFileStatus.Downloading:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditedAndModifiedStausConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumNxlFileStatus status = (EnumNxlFileStatus)value;
                switch (status)
                {
                    //case EnumNxlFileStatus.AvailableOffline_Edited:
                    //case EnumNxlFileStatus.CachedFile_Edited: // Need change
                    //    return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditedAndModifiedTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumNxlFileStatus status = (EnumNxlFileStatus)value;
                switch (status)
                {
                    //case EnumNxlFileStatus.AvailableOffline_Edited:
                    //case EnumNxlFileStatus.CachedFile_Edited:
                    //    return "Edited in Local";
                    default:
                        return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileLocationConentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string location = value?.ToString();
                //if(location == "Local")
                //{
                //    return "SkyDRM Folder";
                //} else
                //{
                //    return location;
                //}

                return location;
            }
            catch (Exception)
            {
                //throw;
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsProtectBtnEnableConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isInitial = (bool)value[0];
                if (isInitial)
                {
                    return false;
                }

                return true;
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[1];
                switch (CurrentWorkingArea)
                {
                    // Disable protect btn
                    case EnumCurrentWorkingArea.PROJECT:
                    case EnumCurrentWorkingArea.MYDRIVE:
                    case EnumCurrentWorkingArea.EXTERNAL_REPO:
                        return false;
                    default:
                        return true;
                }
            }
            catch (Exception)
            {
                return true;
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsShareBtnEnableConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isInitial = (bool)value[0];
                if (isInitial)
                {
                    return false;
                }

                return true;
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[1];
                switch (CurrentWorkingArea)
                {
                    // Disable share btn
                    case EnumCurrentWorkingArea.PROJECT:
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.WORKSPACE:
                    case EnumCurrentWorkingArea.MYDRIVE:
                    case EnumCurrentWorkingArea.EXTERNAL_REPO:
                    case EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT:
                        return false;
                    default:
                        return true;
                }
            }
            catch (Exception)
            {
                return true;
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProtectBtnTagConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isInitial = (bool)value[0];
                if (isInitial)
                {
                    string grayImg = @"/rmc/resources/icons/Icon_protect_gray.png";
                    return new BitmapImage(new Uri((string)grayImg, UriKind.Relative));
                }

                return new BitmapImage(new Uri("/rmc/resources/icons/Icon_protect.png", UriKind.Relative));
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[1];
                string imageUrl = "";
                switch (CurrentWorkingArea)
                {
                    // Disable protect btn
                    case EnumCurrentWorkingArea.PROJECT:
                    case EnumCurrentWorkingArea.MYDRIVE:
                    case EnumCurrentWorkingArea.EXTERNAL_REPO:
                        imageUrl = @"/rmc/resources/icons/Icon_protect_gray.png";
                        break;
                    default:
                        imageUrl = @"/rmc/resources/icons/Icon_protect.png";
                        break;
                }
                return new BitmapImage(new Uri((string)imageUrl, UriKind.Relative));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShareBtnTagConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isInitial = (bool)value[0];
                if (isInitial)
                {
                    string grayImg = @"/rmc/resources/icons/Icon_share_gray.png";
                    return new BitmapImage(new Uri((string)grayImg, UriKind.Relative));
                }

                return new BitmapImage(new Uri("/rmc/resources/icons/Icon_share.png", UriKind.Relative));
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[1];
                string imageUrl = "";
                switch (CurrentWorkingArea)
                {
                    // Disable share btn
                    case EnumCurrentWorkingArea.PROJECT:
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.WORKSPACE:
                    case EnumCurrentWorkingArea.MYDRIVE:
                    case EnumCurrentWorkingArea.EXTERNAL_REPO:
                    case EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT:
                        imageUrl = @"/rmc/resources/icons/Icon_share_gray.png";
                        break;
                    default:
                        imageUrl = @"/rmc/resources/icons/Icon_share.png";
                        break;
                }
                return new BitmapImage(new Uri((string)imageUrl, UriKind.Relative));
            }
            catch (Exception)
            {
                return null;
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AddNxlBtnTagConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isInitial = (bool)value[0];
                if (isInitial)
                {
                    string grayImg = @"/rmc/resources/icons/Icon_menu_addfile_gray.png";
                    return new BitmapImage(new Uri((string)grayImg, UriKind.Relative));
                }

                return new BitmapImage(new Uri("/rmc/resources/icons/Icon_menu_addfile.png", UriKind.Relative));
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[1];
                string imageUrl = "";
                switch (CurrentWorkingArea)
                {
                    // Disable addnxl btn
                    case EnumCurrentWorkingArea.PROJECT:
                    case EnumCurrentWorkingArea.MYDRIVE:
                    case EnumCurrentWorkingArea.EXTERNAL_REPO:
                        imageUrl = @"/rmc/resources/icons/Icon_menu_addfile_gray.png";
                        break;
                    default:
                        imageUrl = @"/rmc/resources/icons/Icon_menu_addfile.png";
                        break;
                }
                return new BitmapImage(new Uri((string)imageUrl, UriKind.Relative));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ListViewVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                        return Visibility.Collapsed;
                    default:
                        return Visibility.Visible;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FilterListViewVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabItemSharedWithHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:
                        return CultureStringInfo.ApplicationFindResource("MainWin__Tab_SharedWithMe");
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                        return CultureStringInfo.ApplicationFindResource("MainWin__Tab_SharedWithProject");
                    default:
                        return "Error";
                }
            }
            catch (Exception)
            {
                return "Error";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabItemSharedByHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:
                        return CultureStringInfo.ApplicationFindResource("MainWin__Tab_SharedByMe");
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                        return CultureStringInfo.ApplicationFindResource("MainWin__Tab_SharedByProject");
                    default:
                        return "Error";
                }
            }
            catch (Exception)
            {
                return "Error";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabItemSharedWithVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:
                        //case EnumCurrentWorkingArea.PROJECT_ROOT: // PM required hide project share transaction
                        // fix Bug 64404 , remove 'Share with me' in MyVault.
                        return Visibility.Collapsed;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TabItemSharedByVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:
                    //case EnumCurrentWorkingArea.PROJECT_ROOT: // PM required hide project share transaction
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #region Repo info display convert
    /// <summary>
    /// Repo info visibility
    /// </summary>
    public class RepoInfoVisibility : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[0];
                bool isInitial = (bool)value[1];
                if (isInitial)
                {
                    return Visibility.Collapsed;
                }
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYDRIVE:
                    case EnumCurrentWorkingArea.MYVAULT:
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.WORKSPACE:
                    case EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RepoInfoExternalRepoTypeIconVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RepositoryProviderClass repoType = (RepositoryProviderClass)value;

            switch (repoType)
            {
                case RepositoryProviderClass.UNKNOWN:
                    return Visibility.Collapsed;
                case RepositoryProviderClass.PERSONAL:
                case RepositoryProviderClass.BUSINESS:
                case RepositoryProviderClass.APPLICATION:
                    return Visibility.Visible;
                default:
                    break;
            }

            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RepoDescribeInfoVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string describe = (string)value;
                if (string.IsNullOrEmpty(describe))
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class CreateFolderVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYDRIVE:
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.WORKSPACE:
                        //return Visibility.Visible;
                        // now not support create folder,set Collapsed
                        return Visibility.Collapsed;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class UploadFileVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYDRIVE:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ProjectMemberVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                        //return Visibility.Visible;
                        // now, not support manager project member, set Collapsed
                        return Visibility.Collapsed;
                    default:
                        return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    /// <summary>
    /// Display treeview converter
    /// </summary>
    public class DisplayVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool Isvisibility = (bool)value;
                SkydrmApp.Singleton.Log.Info("Display treeview, Isvisibility value: " + Isvisibility.ToString());

                if (Isvisibility)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                SkydrmApp.Singleton.Log.Info("Convert failed in DisplayVisibilityConverter");
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DisplayGridWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool Isvisibility = (bool)value;
                SkydrmApp.Singleton.Log.Info(" Display grid width, Isvisibility value: " + Isvisibility.ToString());

                if (Isvisibility)
                {
                    //for ColumnDefinition width
                    return @"170";
                }
                return @"auto";
            }
            catch (Exception)
            {
                SkydrmApp.Singleton.Log.Info("Convert failed in DisplayGridWidthConverter");
                return @"auto";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for Loading Page,user first login 
    public class DisplayLoadingPageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isLoading = (bool)value;
                SkydrmApp.Singleton.Log.Info("Display loading page, isLoading value: " + isLoading.ToString());

                if (isLoading)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                SkydrmApp.Singleton.Log.Info("Convert failed in DisplayLoadingPageConverter");
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Home UI visibility
    /// </summary>
    public class HomeVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                if (CurrentWorkingArea == EnumCurrentWorkingArea.HOME)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HomeWorkSpaceVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isSaas = (bool)value;
                if (isSaas)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Visible;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HomeRepositoryVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int externalCount = (int)value;
                if (externalCount > 0)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// MySpace UI visibility
    /// </summary>
    public class MySpaceVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                if (CurrentWorkingArea == EnumCurrentWorkingArea.MYSPACE)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// External repositories manager UI
    /// </summary>
    public class RepositoriesUIVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                if (CurrentWorkingArea == EnumCurrentWorkingArea.EXTERNAL_REPO)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RepositoriesConfigTextVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int externalCount = (int)value;
                if (externalCount > 0)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RepositoriesIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string repoType = (string)value;
            
            if (repoType.Equals(FileSysConstant.DROPBOX, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/resources/icons/externalrepo/dropbox-color.png";
            }
            if (repoType.Equals(FileSysConstant.BOX, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/resources/icons/externalrepo/box-color.png";
            }
            if (repoType.Equals(FileSysConstant.GOOGLE_DRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/resources/icons/externalrepo/googledrive-color.png";
            }
            if (repoType.Equals(FileSysConstant.ONEDRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/resources/icons/externalrepo/onedrive-color.png";
            }
            if (repoType.Equals(FileSysConstant.SHAREPOINT, StringComparison.CurrentCultureIgnoreCase)
                || repoType.Equals(FileSysConstant.SHAREPOINT_ONLINE, StringComparison.CurrentCultureIgnoreCase)
                || repoType.Equals(FileSysConstant.SHAREPOINT_ONPREMISE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/resources/icons/externalrepo/sharepoint-color.png";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class RepositoriesTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RepositoryProviderClass repoType = (RepositoryProviderClass)value;

            switch (repoType)
            {
                case RepositoryProviderClass.UNKNOWN:
                    break;
                case RepositoryProviderClass.PERSONAL:
                    return @"/rmc/resources/icons/externalrepo/type/personal.png";
                case RepositoryProviderClass.BUSINESS:
                    return @"/rmc/resources/icons/externalrepo/type/company.png";
                case RepositoryProviderClass.APPLICATION:
                    return @"/rmc/resources/icons/externalrepo/type/application.png";
                default:
                    break;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class RepositoriesTypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RepositoryProviderClass repoType = (RepositoryProviderClass)value;

            switch (repoType)
            {
                case RepositoryProviderClass.UNKNOWN:
                    break;
                case RepositoryProviderClass.PERSONAL:
                    return new SolidColorBrush(Color.FromRgb(0X7E, 0X57, 0XC2));
                case RepositoryProviderClass.BUSINESS:
                    return new SolidColorBrush(Color.FromRgb(0X66, 0XBB, 0X6A));
                case RepositoryProviderClass.APPLICATION:
                    return new SolidColorBrush(Color.FromRgb(0XFF, 0XA7, 0X26));
                default:
                    break;
            }

            return new SolidColorBrush(Color.FromRgb(0XFF, 0XFF, 0XFF));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    //for display Project  LixBox
    public class DisplayProjectLixBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //for display Project group name in LixBox
    public class ProjectLixBoxGroupNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsCreateByMe = (bool)value;
                if (IsCreateByMe)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin__ProjectListView_GroupByMe");
                }
                return CultureStringInfo.ApplicationFindResource("MainWin__ProjectListView_GroupByOther");
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //for display Project group name in LixBox
    public class ProjectLixBoxGroupName2Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsCreateByMe = (bool)value;
                if (IsCreateByMe)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin__ProjectListView_GroupByMe2");
                }
                return CultureStringInfo.ApplicationFindResource("MainWin__ProjectListView_GroupByOther2");
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display Project group name in LixBox
    public class ProjectPageListNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsCreateByMe = (bool)value;
                if (IsCreateByMe)
                {
                    return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByMe.png";
                }
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByOthers.png";
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display ContentLayout_NotData is or not
    public class DisplayContentNotDataConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[0];
                int nxlFileListCount = (int)value[1];
                int copyFileListCount = (int)value[2];
                if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
                {
                    if (nxlFileListCount > 0 || copyFileListCount > 0)
                    {
                        return Visibility.Collapsed;
                    }
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
            
          
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for ListView Column date modified Header content 
    public class IsDisplayDateColumnContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                        return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Shared_date");
                    default:
                        return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Date_modified");
                }
            }
            catch (Exception)
            {
                return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Date_modified");
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for ListView Column Share with Header content 
    public class IsDisplayShareColumnContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.PROJECT:
                        return @"";
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                        return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Shared_with_project");
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                        return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Shared_with");
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                        return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Shared_by");
                    default:
                        return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Shared_with");
                }
            }
            catch (Exception)
            {
                return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Shared_with");
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // for Tab shared with ListView Column Share from Header content 
    public class IsDisplaySharedFromColuContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value;
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                        return CultureStringInfo.ApplicationFindResource("MainWin__ListView_SharedFrom");
                    default:
                        return @"";
                }
            }
            catch (Exception)
            {
                return CultureStringInfo.ApplicationFindResource("MainWin__FileListView_Shared_with");
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display Search promptText visibility
    public class SearchPromptTextVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isSearch = (bool)value[0];
                int nxlListCount = (int)value[1];

                if (isSearch)
                {
                    if (nxlListCount < 1)
                    {
                        return Visibility.Visible;
                    }
                }
                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }           

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //for display Empty Folder visibility
    public class EmptyFolderVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumCurrentWorkingArea CurrentWorkingArea = (EnumCurrentWorkingArea)value[0];
                bool isLoading = (bool)value[1];
                if (isLoading)
                {
                    return Visibility.Collapsed;
                }
                int nxlFileListCount = (int)value[2];
                int copyFileListCount = (int)value[3];
                int projectCount = (int)value[4];

                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT:
                    case EnumCurrentWorkingArea.WORKSPACE:
                    case EnumCurrentWorkingArea.MYVAULT:
                    case EnumCurrentWorkingArea.MYDRIVE:
                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                        if (nxlFileListCount > 0 || copyFileListCount > 0)
                        {
                            return Visibility.Collapsed;
                        }
                        return Visibility.Visible;//display The folder is empty
                    case EnumCurrentWorkingArea.PROJECT:
                        if (projectCount > 0)
                        {
                            return Visibility.Collapsed;//display SelectProject UI
                        }
                        return Visibility.Visible;//display The folder is empty
                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                    case EnumCurrentWorkingArea.MYSPACE:
                        return Visibility.Collapsed;

                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
           

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // For empty folder visibility when is data loading
    public class EmptyImageVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isDataLoading = (bool)value;
                if (isDataLoading)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Visible;
            }
            
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // For empty folder text when is data loading
    public class EmptyFolderTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isDataLoading = (bool)value;
                if (isDataLoading)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_PromptText_Loading");
                }

                return CultureStringInfo.ApplicationFindResource("MainWin_PromptText_Empty");
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SharedWithConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumFileRepo fileRepo = (EnumFileRepo)value[0];
                List<string> sharedWith = (List<string>)value[1];

                // display SharedWith column
                StringBuilder builder = new StringBuilder();

                if (sharedWith.Count == 0)
                {
                    return "";
                }

                if (fileRepo == EnumFileRepo.REPO_MYVAULT)
                {
                    for (int i = 0; i < sharedWith.Count; i++)
                    {
                        builder.Append(sharedWith[i]);
                        if (i < sharedWith.Count - 1)
                        {
                            builder.Append(", ");
                        }
                    }
                }
                else if (fileRepo == EnumFileRepo.REPO_PROJECT)
                {
                    List<string> prjNameList = new List<string>();

                    var projects = SkydrmApp.Singleton.MainWin.viewModel.projectRepo.FilePool;
                    for (int i = 0; i < sharedWith.Count; i++)
                    {
                        string projectName = "";
                        foreach (var one in projects)
                        {
                            if (one.ProjectInfo.ProjectId == int.Parse(sharedWith[i]))
                            {
                                projectName = one.ProjectInfo.DisplayName;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(projectName))
                        {
                            prjNameList.Add(projectName);
                        }
                    }

                    for (int i = 0; i < prjNameList.Count; i++)
                    {
                        builder.Append(prjNameList[i]);
                        if (i < prjNameList.Count - 1)
                        {
                            builder.Append(", ");
                        }
                    }
                }

                return builder.ToString();
            }
            catch (Exception)
            {
                return null;
            }

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Shared with converter, when file repo is SharedWithMe will use sharedBy field
    /// </summary>
    public class SharedWithOrByConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                EnumFileRepo fileRepo = (EnumFileRepo)value[0];
                List<string> sharedWith = (List<string>)value[1];
                
                // Special handle, use for ShareWithMe repo, display SharedBy column
                string sharedBy = (string)value[2];
                if (fileRepo == EnumFileRepo.REPO_SHARED_WITH_ME)
                {
                    return sharedBy;
                }

                // display SharedWith column
                StringBuilder builder = new StringBuilder();

                if (sharedWith.Count == 0)
                {
                    return "";
                }

                if (fileRepo == EnumFileRepo.REPO_MYVAULT)
                {
                    //if (sharedWith.Count == 1)
                    //{
                    //    return sharedWith[0];
                    //}
                    //if (sharedWith.Count == 2)
                    //{
                    //    ret += sharedWith[0];
                    //    ret += ",";
                    //    ret += sharedWith[1];
                    //    return ret;
                    //}
                    //if (sharedWith.Count > 2)
                    //{
                    //    ret += sharedWith[0];
                    //    ret += ",";
                    //    ret += sharedWith[1];
                    //    ret += "...";
                    //    return ret;
                    //}
                    for (int i = 0; i < sharedWith.Count; i++)
                    {
                        builder.Append(sharedWith[i]);
                        if (i < sharedWith.Count - 1)
                        {
                            builder.Append(", ");
                        }
                    }
                }
                else if (fileRepo == EnumFileRepo.REPO_PROJECT)
                {
                    List<string> prjNameList = new List<string>();

                    var projects = SkydrmApp.Singleton.MainWin.viewModel.projectRepo.FilePool;
                    for (int i = 0; i < sharedWith.Count; i++)
                    {
                        string projectName = "";
                        foreach (var one in projects)
                        {
                            if (one.ProjectInfo.ProjectId == int.Parse(sharedWith[i]))
                            {
                                projectName = one.ProjectInfo.DisplayName;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(projectName))
                        {
                            prjNameList.Add(projectName);
                        }

                    }

                    for (int i = 0; i < prjNameList.Count; i++)
                    {
                        builder.Append(prjNameList[i]);
                        if (i < prjNameList.Count - 1)
                        {
                            builder.Append(", ");
                        }
                    }
                }

                return builder.ToString();
            }
            catch (Exception)
            {
                return null;
            }
           
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Shared with more converter
    /// </summary>
    public class SharedWithMoreConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                List<string> sharedWith = (List<string>)value;

                if (sharedWith.Count > 2)
                {
                    return "+" + (sharedWith.Count - 2).ToString();
                }

                return "";
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Judge the sharedWith count, return true when count is more than 2, means set "More" control visiable.
    /// </summary>
    public class Count2BoolConverter : IValueConverter
    {
        private string[] array;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string emails = value as string;

                if (!string.IsNullOrEmpty(emails) && emails.Contains(","))
                {
                    array = emails.Split(',');
                    if (array.Length == 2 && emails.EndsWith("..."))
                    {
                        return true;
                    }

                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ListCount2BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value > 0 ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

   

    /// <summary>
    /// Using to Converter network status(bool type) to Visibility prompt Title
    /// </summary>
    public class NetworkStatusBool2VisibilityInfo : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Using to Converter network status(bool type) to prompt info(string type)
    /// </summary>
    public class NetworkStatusBool2StringInfo : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_NETWORK_CONNECTED");
                }
                else
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_NETWORK_ERROR");
                }
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Using to Converter network status(bool type) to Image flag
    /// </summary>
    public class NetworkStatusBool2ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                {
                    return @"/rmc/resources/icons/Icon_net_connect.png";
                }
                else
                {
                    return @"/rmc/resources/icons/Icon_net_error.png";
                }
            }
            catch (Exception)
            {
                return @"/rmc/resources/icons/Icon_net_error.png";
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Using to Converter network status(bool type) to line status(online\offline)
    /// </summary>
    public class NetStatusBool2LineStatus : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_STATUS_ON_LINE");
                }
                else
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_STATUS_OFF_LINE");
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Using to Converter network status(bool type) to line status(online\offline) color
    /// </summary>
    public class NetStatusBool2LineColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                {
                    return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                }
                else
                {
                    return new SolidColorBrush(Color.FromRgb(255, 0, 0));
                }
            }
            catch (Exception)
            {
                return new SolidColorBrush(Color.FromRgb(39, 174, 96));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convert file size into readable value (now only use KB as the unit.)
    /// </summary>
    public class ReadableFileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                long size = (long)value;
                //tmp for bug fix, if size==0 ,we take it as a folder 
                if (size == 0)
                {
                    return "";
                }

                return FileSizeHelper.GetSizeString(size);
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #region For Upload Button Converter
    /// <summary>
    /// Convert upload button content
    /// </summary>
    public class ButtonUploadContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsStartUpload = (bool)value;

                if (IsStartUpload == true)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin__Stop_Upload");
                }
                return CultureStringInfo.ApplicationFindResource("MainWin__Start_Upload");
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Convert upload button Tag
    /// </summary>
    public class ButtonUploadTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsStartUpload = (bool)value;
                string imageUrl = "";
                if (IsStartUpload == true)
                {
                    imageUrl = @"/rmc/resources/icons/Icon_stopUpload.png";
                }
                else
                {
                    imageUrl = @"/rmc/resources/icons/Icon_StartUpload.png";
                }
                return new BitmapImage(new Uri((string)imageUrl, UriKind.Relative));
            }
            catch (Exception)
            {
                return null;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Convert upload button  CommandParameter
    /// </summary>
    public class ButtonUploadCommandParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsStartUpload = (bool)value;

                if (IsStartUpload == true)
                {
                    return @"Cmd_StopUpload";
                }
                return @"Cmd_StartUpload";
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region For Menuitem start/stop upload status
    /// <summary>
    /// Convert Menuitem Start Upload Status
    /// </summary>
    public class MenuitemStartUploadStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsStartUpload = (bool)value;

                if (IsStartUpload == true)
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Convert Menuitem Stop Upload Status
    /// </summary>
    public class MenuitemStopUploadStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool IsStartUpload = (bool)value;

                if (IsStartUpload == true)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region For Menuitem status need select File
    /// <summary>
    /// Convert Menuitem Status, if current select File is not null, isEnable is true. or else,isEnable is false
    /// </summary>
    public class MenuitemSelectFileStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                INxlFile CurrentSelectedFile = (INxlFile)value;

                if (CurrentSelectedFile != null && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Uploading &&
                     CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Online)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class MenuitemFileStatusNetworkMultiBindConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                INxlFile CurrentSelectedFile = (INxlFile)values[0];
                bool isNetworkAvailabe = (bool)values[1];

                if (CurrentSelectedFile == null)
                {
                    return false;
                }

                if (CurrentSelectedFile.IsFolder)
                {
                    return false;
                }

                if (CurrentSelectedFile.Location == EnumFileLocation.Local
                    && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Uploading)
                {
                    return true;
                }

                if (CurrentSelectedFile.Location == EnumFileLocation.Online
                    && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Downloading)
                {
                    return isNetworkAvailabe;
                }

                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MenuitemFileInfoStatusNetworkMultiBindConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                INxlFile CurrentSelectedFile = (INxlFile)values[0];
                bool isNetworkAvailabe = (bool)values[1];

                if (CurrentSelectedFile == null)
                {
                    return false;
                }

                if (CurrentSelectedFile.IsFolder || !CurrentSelectedFile.IsNxlFile)
                {
                    return false;
                }

                if (CurrentSelectedFile.Location == EnumFileLocation.Local
                    && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Uploading)
                {
                    return true;
                }

                if (CurrentSelectedFile.Location == EnumFileLocation.Online
                    && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Downloading)
                {
                    return isNetworkAvailabe;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MenuitemRemoveFileStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                INxlFile CurrentSelectedFile = (INxlFile)value;

                // For offline file, we only execute "unmark" instead of "remove".
                if (CurrentSelectedFile != null && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Uploading &&
                     CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Online && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.AvailableOffline)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    #endregion

    public class ItemCountsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int count = (int)value;

                if (count > 1)
                {
                    return CultureStringInfo.ApplicationFindResource("MainWin_List_items");
                }
                return CultureStringInfo.ApplicationFindResource("MainWin_List_item");
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimestampToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                long date = long.Parse(value.ToString());

                return DateTimeHelper.TimestampToDateTime2(date);
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool converse=(bool)value;
                return !converse;
            }
            catch (Exception)
            {
                return  false;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
}

