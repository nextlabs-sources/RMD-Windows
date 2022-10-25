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
using Viewer.upgrade.application;
using Viewer.upgrade.ui.common.viatalError.viewModel;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.ui.common.viatalError.view
{
    /// <summary>
    /// Interaction logic for ViatalErrorWindow.xaml
    /// </summary>
    public partial class ViatalErrorWindow : Window
    {
        private CViewModel mViewModel;
        private ViewerApp mApplication;

        public ViatalErrorWindow(string message)
        {
            InitializeComponent();
            this.mApplication = (ViewerApp)Application.Current;
            this.mViewModel = new CViewModel(message);
            this.DataContext = mViewModel;
        }

        private void Close_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(mApplication.ShutdownMode == ShutdownMode.OnMainWindowClose)
            {
                ViewerApp.Current.MainWindow = this;
            }

            //Int64 code = ToolKit.RunningMode();
            //if (code == 0)
            //{
            //    ViewerApp.Current.MainWindow = this;
            //}
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
