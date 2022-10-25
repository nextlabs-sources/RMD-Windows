using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SkydrmDesktop;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.view
{
    /// <summary>
    /// Interaction logic for TreeViewComponent.xaml
    /// </summary>
    public partial class TreeViewComponent : UserControl
    {

        public ObservableCollection<TreeViewItemViewModel> CopyChildren = new ObservableCollection<TreeViewItemViewModel>();

        // Because we call SelcetItemChanged Event in the external, the TreeViewItemViewModel can not convert to TreeViewItem type.
        // Use TreeViewItem.Selected event to get TreeViewItem type.
        public delegate void TransmitTreeViewItem(TreeViewItem treeViewItem);
        public event TransmitTreeViewItem TransmitItemEvent;

        public TreeViewComponent()
        {
            InitializeComponent();
        }
    
        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            try
            {              
                TreeViewItem tvi = e.OriginalSource as TreeViewItem;

                if (tvi !=null)
                {
                    TransmitItemEvent?.Invoke(tvi);
                }
                
            }
            catch (Exception msg)
            {
                SkydrmApp.Singleton.Log.Error("Exception in TreeviewCompent TreeViewItem_Selected event:", msg);
            }        
        }

    }
}
