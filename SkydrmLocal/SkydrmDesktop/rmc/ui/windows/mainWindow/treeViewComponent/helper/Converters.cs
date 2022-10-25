using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.fileSystem.utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.helper
{
    public class IndentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double colunwidth = 12;
            double left = 0.0;

            UIElement element = value as TreeViewItem;
            while (element.GetType() != typeof(TreeView))
            {
                element = (UIElement)VisualTreeHelper.GetParent(element);
                if (element.GetType() == typeof(TreeViewItem))
                    left += colunwidth;
            }
            return new Thickness(left, 5, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class RootIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string repoType = (string)value;
            if (repoType.Equals(FileSysConstant.HOME, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/home.png";
            }
            if (repoType.Equals(FileSysConstant.MYSPACE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/myspace.png";
            }
            if (repoType.Equals(FileSysConstant.MYDRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/mydrive.png";
            }
            if (repoType.Equals(FileSysConstant.MYVAULT, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/myvault.png";
            }
            if (repoType.Equals(FileSysConstant.WORKSPACE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/workspace.png";
            }
            if (repoType.Equals(FileSysConstant.SHAREDWITHME, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/sharedWithMe.png";
            }
            if (repoType.Equals(FileSysConstant.PROJECT, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/project.png";
            }
            if (repoType.Equals(FileSysConstant.REPOSITORIES, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/repositories.png";
            }
            if (repoType.Equals(FileSysConstant.DROPBOX, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/dropbox.png";
            }
            if (repoType.Equals(FileSysConstant.BOX, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/box.png";
            }
            if (repoType.Equals(FileSysConstant.GOOGLE_DRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/googleDrive.png";
            }
            if (repoType.Equals(FileSysConstant.ONEDRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/oneDrive.png";
            }
            if (repoType.Equals(FileSysConstant.SHAREPOINT, StringComparison.CurrentCultureIgnoreCase)
                || repoType.Equals(FileSysConstant.SHAREPOINT_ONLINE, StringComparison.CurrentCultureIgnoreCase)
                || repoType.Equals(FileSysConstant.SHAREPOINT_ONPREMISE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/sharepoint.png";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
    public class ProjectIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsCreateByMe = (bool)value;
            if (IsCreateByMe)
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByMe.png";
            }
            return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByOthers.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class RepoClassTypeIconConverter : IValueConverter
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
    public class EllipseColorBeforeRepoTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RepositoryProviderClass repoType = (RepositoryProviderClass)value;

            switch (repoType)
            {
                case RepositoryProviderClass.UNKNOWN:
                    break;
                case RepositoryProviderClass.PERSONAL:
                case RepositoryProviderClass.BUSINESS:
                case RepositoryProviderClass.APPLICATION:
                    return new SolidColorBrush(Color.FromRgb(0X8B, 0X8B, 0X8B));
                default:
                    break;
            }

            return new SolidColorBrush(Color.FromArgb(0X00, 0XFF, 0XFF, 0XFF));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
