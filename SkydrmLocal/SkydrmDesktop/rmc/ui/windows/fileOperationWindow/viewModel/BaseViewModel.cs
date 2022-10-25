using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.viewModel
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private FileOperationWin host;
        private System.Windows.Visibility gridProBarVisible = System.Windows.Visibility.Collapsed;
        private Stack historyStack = new Stack();
        private Visibility backBtnVisible = Visibility.Collapsed;

        public BaseViewModel(FileOperationWin win)
        {
            host = win;
        }

        public FileOperationWin Host { get => host;}
        /// <summary>
        /// Progress bar UI visibility,defult value is Collapsed
        /// </summary>
        public Visibility GridProBarVisible { get => gridProBarVisible; set { gridProBarVisible = value; OnPropertyChanged("GridProBarVisible"); } }

        /// <summary>
        /// Used to record display page
        /// </summary>
        public Stack HistoryStack { get => historyStack; }

        /// <summary>
        /// Back button visible
        /// </summary>
        public Visibility BackBtnVisible { get => backBtnVisible; set { backBtnVisible = value; OnPropertyChanged("BackBtnVisible"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
