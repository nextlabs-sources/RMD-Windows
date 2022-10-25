using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.ui.windows.renameFileWindow.rename;
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

namespace SkydrmDesktop.rmc.ui.windows.renameFileWindow.view
{
    /// <summary>
    /// RenameFileWin.xaml  DataCommands
    /// </summary>
    public class RenameFile_DataCommands
    {
        private static RoutedCommand positive;
        private static RoutedCommand cancel;
        static RenameFile_DataCommands()
        {
            positive = new RoutedCommand(
              "Positive", typeof(RenameFile_DataCommands));

            InputGestureCollection input = new InputGestureCollection();
            input.Add(new KeyGesture(Key.Escape));
            cancel = new RoutedCommand(
              "Cancel", typeof(RenameFile_DataCommands), input);
        }
        /// <summary>
        ///  RenameFileWin.xaml positive button command
        /// </summary>
        public static RoutedCommand Positive
        {
            get { return positive; }
        }
        /// <summary>
        /// RenameFileWin.xaml cancel button command
        /// </summary>
        public static RoutedCommand Cancel
        {
            get { return cancel; }
        }
    }

    /// <summary>
    /// Interaction logic for RenameFileWin.xaml
    /// </summary>
    public partial class RenameFileWin : Window
    {
        private bool IsRenaming { get; set; }

        private IRenameFile Opert { get; }

        public RenameFileWin(IRenameFile renameFile)
        {
            InitializeComponent();

            Opert = renameFile;
            this.fileName.Text = Opert.AdviceName;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PositiveCmdBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.gridProBar.Visibility = Visibility.Visible;
            IsRenaming = true;

            string newName = this.fileName.Text;

            AsyncHelper.RunAsync((para) =>
            {
                // invoke api to rename
                return Opert.Rename(para);
            },
            newName,
            (rt) =>
            {
                // re-set
                this.gridProBar.Visibility = Visibility.Collapsed;
                IsRenaming = false;

                if (rt)
                {
                    this.Close();
                }
            });
        }

        private void PositiveCmdBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            string fileName = this.fileName.Text;
            if (!string.IsNullOrEmpty(fileName))
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsRenaming)
            {
                e.Cancel = true;
            }
        }
    }
}
