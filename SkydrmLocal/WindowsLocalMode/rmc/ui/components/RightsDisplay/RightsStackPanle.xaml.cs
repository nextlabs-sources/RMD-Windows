using SkydrmLocal.rmc.ui.components.RightsDisplay.model;
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

namespace SkydrmLocal.rmc.ui.components.RightsDisplay
{
    /// <summary>
    /// Interaction logic for RightsStackPanle.xaml
    /// </summary>
    public partial class RightsStackPanle : UserControl
    {
        private RightsStPanViewModel rightsStPanViewModel = new RightsStPanViewModel();

        public RightsStackPanle()
        {
            InitializeComponent();
            this.DataContext = rightsStPanViewModel;
        }

        public void SetRightsList(List<RightsItem> rights)
        {
            rightsStPanViewModel.RightsList.Clear();
            if (rights == null || rights.Count == 0)
            {
                return;
            }
            foreach (var item in rights)
            {
                rightsStPanViewModel.RightsList.Add(item);
            }
        }

        public void SetWatermarkValue(string value)
        {
            rightsStPanViewModel.WatermarkValue = value;
        }

        public void SetValidityValue(string value)
        {
            rightsStPanViewModel.ValidityValue = value;
        }

        public void SetWaterPanlVisibility(Visibility value)
        {
            rightsStPanViewModel.WaterPanlVisibility = value;
        }

        public void SetValidityPanlVisibility(Visibility value)
        {
            rightsStPanViewModel.ValidityPanlVisibility = value;
        }

    }
}
