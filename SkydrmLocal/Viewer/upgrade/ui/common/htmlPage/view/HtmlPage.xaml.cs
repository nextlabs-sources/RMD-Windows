using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.htmlPage.viewModel;

namespace Viewer.upgrade.ui.common.htmlPage.view
{
    /// <summary>
    /// Interaction logic for HtmlPage.xaml
    /// </summary>
    public partial class HtmlPage : Page
    {
        private ViewModel mViewModel;
        public HtmlPage(string filePath)
        {
            InitializeComponent();
            mViewModel = new ViewModel(filePath, this);
        }

        public ISensor Sensor
        {
            get { return mViewModel; }
        }

        public void Watermark(WatermarkInfo watermarkInfo)
        {
            mViewModel.Watermark(watermarkInfo);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WebBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(WebBrowserDocumentCompletedEventHandler);
            WebBrowser.IsWebBrowserContextMenuEnabled = false;
            WebBrowser.WebBrowserShortcutsEnabled = false;
            WebBrowser.ScriptErrorsSuppressed = true;
            mViewModel.Page_Loaded();
        }

        public void WebBrowserDocumentCompletedEventHandler(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
             WebBrowser.Document.Body.Drag += new HtmlElementEventHandler(Body_Drag);
        }

        private void Body_Drag(object sender, HtmlElementEventArgs e)
        {
            e.ReturnValue = false;
        }

        public void Print()
        {
            mViewModel.Print();
        }
    }
}
