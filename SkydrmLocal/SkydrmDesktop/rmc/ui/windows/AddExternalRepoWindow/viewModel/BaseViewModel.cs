using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.view;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.viewModel
{
    class BaseViewModel : INotifyPropertyChanged
    {
        private AddExternalRepoWin host;
        private Visibility gridProBarVisible = Visibility.Collapsed;

        public BaseViewModel(AddExternalRepoWin win)
        {
            host = win;
        }

        public AddExternalRepoWin Host { get => host; }
        /// <summary>
        /// Progress bar UI visibility,defult value is Collapsed
        /// </summary>
        public Visibility GridProBarVisible { get => gridProBarVisible; set { gridProBarVisible = value; OnPropertyChanged("GridProBarVisible"); } }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
