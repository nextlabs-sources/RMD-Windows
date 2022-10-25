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
using Viewer.upgrade.ui.common.errorPage.viewModel;

namespace Viewer.upgrade.ui.common.errorPage.view
{
    /// <summary>
    /// Interaction logic for ErrorPage.xaml
    /// </summary>
    public partial class ErrorPage : Page
    {
        private Window mWindow;
        private ViewModel mViewModel;
        public ErrorPage(string errorMessage)
        {
            InitializeComponent();
            this.mViewModel =new ViewModel(errorMessage);
            this.DataContext = mViewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mWindow = Window.GetWindow(this);
        }

        public void Close_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        public void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mWindow?.Close();
            e.Handled = true;
        }
    }
}
