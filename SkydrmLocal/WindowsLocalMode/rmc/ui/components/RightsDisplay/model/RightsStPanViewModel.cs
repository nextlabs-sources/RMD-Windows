using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkydrmLocal.rmc.ui.components.RightsDisplay.model
{
    public class RightsStPanViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<RightsItem> rightsList = new ObservableCollection<RightsItem>();
        private string watermarkValue;
        private string validityValue;
        private Visibility waterPanlVisibility;
        private Visibility validityPanlVisibility;

        public ObservableCollection<RightsItem> RightsList
        {
            get { return rightsList; }
            set { rightsList = value; }
        }
        public string WatermarkValue { get => watermarkValue; set { watermarkValue = value; OnPropertyChanged("WatermarkValue"); } }
        public string ValidityValue { get => validityValue; set { validityValue = value; OnPropertyChanged("ValidityValue"); } }
        public Visibility WaterPanlVisibility { get => waterPanlVisibility; set { waterPanlVisibility = value; OnPropertyChanged("WaterPanlVisibility"); } }
        public Visibility ValidityPanlVisibility { get => validityPanlVisibility; set { validityPanlVisibility = value; OnPropertyChanged("ValidityPanlVisibility"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
