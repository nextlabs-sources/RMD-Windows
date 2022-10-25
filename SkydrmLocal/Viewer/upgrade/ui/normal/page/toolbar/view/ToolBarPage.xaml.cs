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
using Viewer.upgrade.ui.normal.page.toolbar.viewModel;

namespace Viewer.upgrade.ui.normal.page.toolbar.view
{
    /// <summary>
    /// Interaction logic for ToolBarPage.xaml
    /// </summary>
    public partial class ToolBarPage : Page
    {
        private ViewModel mViewModel;
        public ToolBarPage(string fileName)
        {
            InitializeComponent();
            mViewModel = new ViewModel(fileName);
            this.DataContext = mViewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
