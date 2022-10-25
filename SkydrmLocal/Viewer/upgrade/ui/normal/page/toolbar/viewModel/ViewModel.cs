using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.application;
using Viewer.upgrade.file.basic;

namespace Viewer.upgrade.ui.normal.page.toolbar.viewModel
{
    public class ViewModel: INotifyPropertyChanged
    {
        private string mFileName;

        public string FileName
        {
            get
            {
                return mFileName;
            }
            set
            {
                mFileName = value;
                OnPropertyChanged("FileName");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ViewModel(string fileName)
        {
            FileName = fileName;
        }
    }
}
