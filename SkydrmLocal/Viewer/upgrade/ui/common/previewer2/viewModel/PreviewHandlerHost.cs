using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.upgrade.ui.common.previewer2.viewModel
{
    public partial class PreviewHandlerHost : UserControl
    {
        public PreviewHandlerHost()
        {
            InitializeComponent();
            // try fix Bug 51404 - [doc] word file content not display to top 
            AutoValidate = AutoValidate.Inherit;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
    }
}
