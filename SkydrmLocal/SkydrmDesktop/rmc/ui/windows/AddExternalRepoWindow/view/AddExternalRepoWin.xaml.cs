using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo;
using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.viewModel;
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

namespace SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.view
{
    /// <summary>
    /// Interaction logic for AddExternalRepoWin.xaml
    /// </summary>
    public partial class AddExternalRepoWin : Window
    {
        public AddExternalRepoWin(IAddExternalRepo addExternalRepo)
        {
            InitializeComponent();

            this.DataContext = new AddExternalRepoViewModel(addExternalRepo, this);
        }

        /// <summary>
        ///  When set window SizeToContent(attribute),the WindowStartupLocation will failure
        ///  Use this method to display UI.
        /// </summary>
        /// <param name="sizeInfo"></param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            //Calculate half of the offset to move the form

            if (sizeInfo.HeightChanged)
                this.Top += (sizeInfo.PreviousSize.Height - sizeInfo.NewSize.Height) / 2;

            if (sizeInfo.WidthChanged)
                this.Left += (sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width) / 2;
        }

    }
}
