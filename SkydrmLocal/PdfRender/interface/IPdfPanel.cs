using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using PdfRender.model;

namespace PdfRender
{
    internal interface IPdfPanel
    {
        ScrollViewer ScrollViewer { get; }
        UserControl Instance { get; }
        float CurrentZoom { get; }
        void Load(IPdfSource source);
        void Zoom(double zoomFactor);
        void ZoomIn();
        void ZoomOut();
        void ZoomToWidth();
        void ZoomToHeight();
        void GotoPage(int pageNumber);
        void GotoPreviousPage();
        void GotoNextPage();
        int GetCurrentPageIndex(ViewType viewType);
    }
}
