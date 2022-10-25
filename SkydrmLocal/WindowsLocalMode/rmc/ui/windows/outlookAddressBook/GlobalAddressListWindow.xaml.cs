using SkydrmLocal.rmc.ui.windows.outlookAddressBook.viewModel;
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

namespace SkydrmLocal.rmc.ui.windows.outlookAddressBook
{
    /// <summary>
    /// Interaction logic for GlobalAddressListWindow.xaml
    /// </summary>
    public partial class GlobalAddressListWindow : Window
    {
        private GlobalAddressViewModel viewModel;

        public event EventHandler<EmailEventArgs> EmailListUpdateEvent;

        public GlobalAddressListWindow()
        {
            InitializeComponent();

            viewModel = new GlobalAddressViewModel();
            this.DataContext = viewModel;
        }

        private void Do_Search(object sender, components.CustomSearchBox.SearchEventArgs e)
        {
            viewModel.Search(e.SearchText);
        }

        private void On_Add_Btn(object sender, RoutedEventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var item in fileList.SelectedItems)
            {
                if (item is GlobalAddressViewModel.AddressData)
                {
                    GlobalAddressViewModel.AddressData drv = item as GlobalAddressViewModel.AddressData;
                    builder.Append(drv.EmailAddress + ";");
                }
            }

            var eventArgs = new EmailEventArgs();
            eventArgs.EmailList = builder.ToString();

            EmailListUpdateEvent.Invoke(this, eventArgs);
            this.Close();
        }

        private void On_Cacle_Btn(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public class EmailEventArgs : EventArgs
        {
            public string EmailList { get; set; }
        }

    }
}
