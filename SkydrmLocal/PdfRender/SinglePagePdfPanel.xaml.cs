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
using PdfRender.Helper;
using PdfRender.model;

namespace PdfRender
{
    /// <summary>
    /// Interaction logic for SinglePagePdfPanel.xaml
    /// </summary>
    public partial class SinglePagePdfPanel : UserControl, IPdfPanel
    {
        private PdfPanel parent;
        private ScrollViewer scrollViewer;
        private PdfImageProvider imageProvider;
        private int currentPageIndex = 0; // starting at 0

        public SinglePagePdfPanel(PdfPanel parent)
        {
            InitializeComponent();
            this.parent = parent;
            this.SizeChanged += SinglePageMoonPdfPanel_SizeChanged;
        }

        void SinglePageMoonPdfPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.scrollViewer = VisualTreeHelperEx.FindChild<ScrollViewer>(this);
        }

        public ScrollViewer ScrollViewer
        {
            get { return this.scrollViewer; }
        }

        public UserControl Instance
        {
            get { return this; }
        }

        public void Load(IPdfSource source)
        {
            this.scrollViewer = VisualTreeHelperEx.FindChild<ScrollViewer>(this);
            this.imageProvider = new PdfImageProvider(source, this.parent.TotalPages,
                new PageDisplaySettings(this.parent.GetPagesPerRow(), this.parent.ViewType, this.parent.HorizontalMargin, this.parent.Rotation), false);

            currentPageIndex = 0;

            if (this.parent.ZoomType == ZoomType.Fixed)
                this.SetItemsSource();
            else if (this.parent.ZoomType == ZoomType.FitToHeight)
                this.ZoomToHeight();
            else if (this.parent.ZoomType == ZoomType.FitToWidth)
                this.ZoomToWidth();
        }

        private void SetItemsSource()
        {
            var startIndex = PageHelper.GetVisibleIndexFromPageIndex(this.currentPageIndex, this.parent.ViewType);
            this.itemsControl.ItemsSource = this.imageProvider.FetchRange(startIndex, this.parent.GetPagesPerRow()).FirstOrDefault();
        }

        public int GetCurrentPageIndex(ViewType viewType)
        {
            return this.currentPageIndex;
        }

        public void GotoNextPage()
        {
            var nextPageIndex = PageHelper.GetNextPageIndex(this.currentPageIndex, this.parent.TotalPages, this.parent.ViewType);

            if (nextPageIndex == -1)
                return;

            this.currentPageIndex = nextPageIndex;

            this.SetItemsSource();

            if (this.scrollViewer != null)
                this.scrollViewer.ScrollToTop();
        }

        public void GotoPage(int pageNumber)
        {
            currentPageIndex = pageNumber - 1;
            this.SetItemsSource();

            if (this.scrollViewer != null)
                this.scrollViewer.ScrollToTop();
        }

        public void GotoPreviousPage()
        {
            var prevPageIndex = PageHelper.GetPreviousPageIndex(this.currentPageIndex, this.parent.ViewType);

            if (prevPageIndex == -1)
                return;

            this.currentPageIndex = prevPageIndex;

            this.SetItemsSource();

            if (this.scrollViewer != null)
                this.scrollViewer.ScrollToTop();
        }

        #region Zoom specific code
        public float CurrentZoom
        {
            get
            {
                if (this.imageProvider != null)
                    return this.imageProvider.Settings.ZoomFactor;

                return 1.0f;
            }
        }

        public void ZoomToWidth()
        {
            var scrollBarWidth = this.scrollViewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible ? SystemParameters.VerticalScrollBarWidth : 0;
            var zoomFactor = (this.parent.ActualWidth - scrollBarWidth) / this.parent.PageRowBounds[this.currentPageIndex].SizeIncludingOffset.Width;
            var pageBound = this.parent.PageRowBounds[this.currentPageIndex];

            if (scrollBarWidth == 0 && ((pageBound.Size.Height * zoomFactor) + pageBound.VerticalOffset) >= this.parent.ActualHeight)
                scrollBarWidth += SystemParameters.VerticalScrollBarWidth;

            scrollBarWidth += 2; // Magic number, sorry :)
            zoomFactor = (this.parent.ActualWidth - scrollBarWidth) / this.parent.PageRowBounds[this.currentPageIndex].SizeIncludingOffset.Width;

            ZoomInternal(zoomFactor);
        }

        public void ZoomToHeight()
        {
            var scrollBarHeight = this.scrollViewer.ComputedHorizontalScrollBarVisibility == System.Windows.Visibility.Visible ? SystemParameters.HorizontalScrollBarHeight : 0;
            var zoomFactor = (this.parent.ActualHeight - scrollBarHeight) / this.parent.PageRowBounds[this.currentPageIndex].SizeIncludingOffset.Height;
            var pageBound = this.parent.PageRowBounds[this.currentPageIndex];

            if (scrollBarHeight == 0 && ((pageBound.Size.Width * zoomFactor) + pageBound.HorizontalOffset) >= this.parent.ActualWidth)
                scrollBarHeight += SystemParameters.HorizontalScrollBarHeight;

            zoomFactor = (this.parent.ActualHeight - scrollBarHeight) / this.parent.PageRowBounds[this.currentPageIndex].SizeIncludingOffset.Height;

            ZoomInternal(zoomFactor);
        }

        public void ZoomIn()
        {
            ZoomInternal(this.CurrentZoom + this.parent.ZoomStep);
        }

        public void ZoomOut()
        {
            ZoomInternal(this.CurrentZoom - this.parent.ZoomStep);
        }

        public void Zoom(double zoomFactor)
        {
            this.ZoomInternal(zoomFactor);
        }

        private void ZoomInternal(double zoomFactor)
        {
            if (zoomFactor > this.parent.MaxZoomFactor)
                zoomFactor = this.parent.MaxZoomFactor;
            else if (zoomFactor < this.parent.MinZoomFactor)
                zoomFactor = this.parent.MinZoomFactor;

            this.imageProvider.Settings.ZoomFactor = (float)zoomFactor;

            this.SetItemsSource();
        }
        #endregion

    }
}
