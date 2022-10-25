using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.ui.common.viatalError.viewModel
{
    public class CViewModel : INotifyPropertyChanged
    {
        private string mErrorMessage = string.Empty;
        public string ErrorMessage
        {
            get
            {
                return mErrorMessage;
            }
            set
            {
                mErrorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }
  
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CViewModel(string errorMsg)
        {
            ErrorMessage = errorMsg;
        }
    }
}
