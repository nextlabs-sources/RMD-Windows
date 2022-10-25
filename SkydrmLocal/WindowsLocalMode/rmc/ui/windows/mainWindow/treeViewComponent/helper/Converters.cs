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
            string rootName = (string)value;
            if (rootName.Equals(CultureStringInfo.MainWin__TreeView_MyVault, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/myvault-icon@2x.png";
            }
            else if (rootName.Equals(CultureStringInfo.MainWin__TreeView_ShareWithMe, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/sharedWithMe-icon@2x.png";
            }
            return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/proj-icon@2x.png";
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
}
