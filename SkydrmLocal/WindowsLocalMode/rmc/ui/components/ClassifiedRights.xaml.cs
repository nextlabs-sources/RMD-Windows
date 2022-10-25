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

namespace SkydrmLocal.rmc.ui.components
{
    /// <summary>
    /// Interaction logic for ClassifiedRights.xaml
    /// </summary>
    public partial class ClassifiedRights : UserControl
    {
        public ClassifiedRights()
        {
            InitializeComponent();
        }

        public void InitTagView(Dictionary<string, List<string>> tags)
        {
            this.TagView.InitializeTags(tags);
        }
        public void InitRightsDisplay(List<RightsDisplay.model.RightsItem> rights, string waterMark, string validity,
            Visibility waterMarkVisible, Visibility validityVisible)
        {
            if (rights == null || rights.Count == 0)
            {
                this.RightsSp.Visibility = Visibility.Collapsed;
                this.AccessDenied.Visibility = Visibility.Visible;
                this.AccessDenied.tb_right.Text = CultureStringInfo.ClassifiedRight_No_Permission;
            }
            else
            {
                this.RightsSp.Visibility = Visibility.Visible;
                this.AccessDenied.Visibility = Visibility.Collapsed;

                this.RightsSp.SetRightsList(rights);
                this.RightsSp.SetWatermarkValue(waterMark);
                this.RightsSp.SetValidityValue(validity);
                this.RightsSp.SetWaterPanlVisibility(waterMarkVisible);
                this.RightsSp.SetValidityPanlVisibility(validityVisible);
            }
        }
    }
}
