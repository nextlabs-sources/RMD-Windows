using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Text;

namespace PdfiumViewer
{
    public interface IOnPrintSinglePageListener
    {
      void OnPrintSinglePageListener(PrintPageEventArgs e, Int32 Width, Int32 Height);
    }
}
