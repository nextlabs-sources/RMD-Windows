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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.ui.nxl.page.toolbar.viewModel;

namespace Viewer.upgrade.ui.nxl.page.toolbar.view
{
    /// <summary>
    /// Interaction logic for ToolBarPage.xaml
    /// </summary>
    public partial class ToolBarPage : Page
    {
       // private Window mParentWindow;
        private ViewModel mViewModel;

        public ISensor Sensor
        {
            get { return mViewModel; }
        }

        //public Window ParentWindow
        //{
        //    set { mParentWindow = value; }
        //    get { return mParentWindow; }
        //}

        public ToolBarPage(INxlFile nxlFile)
        {
            InitializeComponent();
            mViewModel = new ViewModel(nxlFile,this);
            this.DataContext = mViewModel;
        }

        public void SetParentWindow(Window window)
        {
            mViewModel.ParentWindow = window;
            mViewModel.RaiseCanExecute();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.Page_Loaded();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
  
        }
    }
}
