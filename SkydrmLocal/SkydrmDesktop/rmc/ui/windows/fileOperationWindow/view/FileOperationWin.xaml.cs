using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel;
using SkydrmLocal.rmc.common.component;
using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view
{
    /// <summary>
    /// FileOperationWin.xaml  DataCommands
    /// </summary>
    public class FileOpeWin_DataCommands
    {
        private static RoutedCommand back;
        static FileOpeWin_DataCommands()
        {
            back = new RoutedCommand(
              "Back", typeof(FileOpeWin_DataCommands));
        }
        /// <summary>
        ///  FileOperationWin.xaml back button command
        /// </summary>
        public static RoutedCommand Back
        {
            get { return back; }
        }
    }

    /// <summary>
    /// Interaction logic for FileOperationWin.xaml
    /// </summary>
    public partial class FileOperationWin : Window
    {
        public FileOperationWin(IBase operation)
        {
            InitializeComponent();

            VMContext mContext = new VMContext(operation, this);

            this.DataContext = mContext.GetViewModel();
        }

        #region Used to prepare data, first display the progress bar Window
        public FileOperationWin()
        {
            InitializeComponent();

            BaseViewModel baseVM = new BaseViewModel(this);
            this.DataContext = baseVM;

            baseVM.GridProBarVisible = Visibility.Visible;
        }

        public void ChangeViewModel(IBase operation)
        {
            VMContext mContext = new VMContext(operation, this);
            this.DataContext = mContext.GetViewModel();

            this.frm.Focus();
        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // during file-sharing or protecting, can't  be closed. 
            if (MenuDisableMgr.GetSingleton().IsSharing || MenuDisableMgr.GetSingleton().IsProtecting)
            {
                e.Cancel = true;
                return;
            }
        }

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

        private void Window_CloseBtn(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
