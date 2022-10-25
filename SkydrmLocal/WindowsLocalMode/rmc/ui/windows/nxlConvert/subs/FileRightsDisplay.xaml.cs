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

namespace SkydrmLocal.rmc.ui.windows.nxlConvert.subs
{
    /// <summary>
    /// Interaction logic for FileRightsDisplay.xaml
    /// </summary>
    public partial class FileRightsDisplay : UserControl
    {
        public FileRightsDisplay()
        {
            InitializeComponent();
        }

        private List<string> actualRights = new List<string>();
        public List<string> ActualRights { get => actualRights; set => SetActualRights(value); }

        public event RoutedEventHandler OkBtnClicked;
        public event RoutedEventHandler BackBtnClicked;
        public event RoutedEventHandler CancelBtnClicked;

        public void SetOkBtnContent(string value)
        {
            this.okBtn.Content = value;
        }

        public void SetSectionTitle(string value)
        {
            this.tr_Title.Text = value;
        }
        public void SetProjectName(string value)
        {
            this.tr_projectName.Text = value;
        }

        // Init ClassifiedRights component
        public void SetTag(Dictionary<string, List<string>> tags)
        {
            this.ClassifiedRights.InitTagView(tags);
        }
        public void SetTagRights(List<components.RightsDisplay.model.RightsItem> displayRights, string waterMark, string validity,
            Visibility waterMarkVisible, Visibility validityVisible)
        {
            this.ClassifiedRights.InitRightsDisplay(displayRights, waterMark, validity, waterMarkVisible, validityVisible);
        }

        private void SetActualRights(List<string>rights)
        {
            this.actualRights = rights;
        }
        
        private void On_Ok_Btn(object sender, RoutedEventArgs e)
        {
            OkBtnClicked?.Invoke(sender, e);
        }

        private void On_Back_Btn(object sender, RoutedEventArgs e)
        {
            BackBtnClicked?.Invoke(sender, e);
        }

        private void On_Cacle_Btn(object sender, RoutedEventArgs e)
        {
            CancelBtnClicked?.Invoke(sender, e);
        }
    }
}
