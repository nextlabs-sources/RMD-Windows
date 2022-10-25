using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using Viewer.utils;
using Viewer.viewer;
using Size = System.Windows.Size;

namespace Viewer.overlay
{
    class Overlay
    {
        #region Private
        private AdornerLayer m_parentLayer;
        private UIElementAdorner m_adorner;
        private FrameworkElement m_visual;
        private bool m_initialze;
        private Graphics m_graphics;
        private WatermarkInfo m_waterMark;
        private Object locker = new Object();
        #endregion

        #region Public
        public AdornerLayer ParentLayer { get => m_parentLayer; set => m_parentLayer = value; }
        public bool Initialze { get => m_initialze; set => m_initialze = value; }
        public Graphics Graphics {
            get
            {
                lock (locker)
                {
                    return m_graphics;
                }
            }
            set
            {
                lock (locker)
                {
                    m_graphics = value;
                }
            } }
        #endregion

        public void Initialize(FrameworkElement visual,WatermarkInfo waterMark)
        {
            this.m_visual = visual;
            this.ParentLayer = AdornerLayer.GetAdornerLayer(visual);
            this.Initialze = true;
            this.m_waterMark = waterMark;
        }

        public UIElement CreateOverlayInAdornerLayer(double width,double height)
        {
            AdornerLayer parentLayer = AdornerLayer.GetAdornerLayer(m_visual);
            if (parentLayer == null)
            {
                return null;
            }
            if (width == 0 || height == 0)
            {
                return null;
            }
            
            //If invoke Adorner's Add() the child attached on Adorner will intercept touch event.
            //m_adorner.Add();
            m_adorner = new UIElementAdorner(m_visual, GenerateAdornerElement(width, height));
            return m_adorner;
        }

        public Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
                using (HwndSource hwndSource = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = hwndSource.CompositionTarget.TransformToDevice;

            if (element.DesiredSize == new Size())
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return (Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }

        public AdornerLayer GetPrintLayer()
        {
            return ParentLayer;
        }

        public void OnOverlayChange()
        {
            if (m_adorner != null)
            {
                RemoveOverlay(m_adorner);
                m_adorner.Remove();
                m_adorner = null;
            }

            ParentLayer.Add((Adorner)CreateOverlayInAdornerLayer(m_visual.ActualWidth, m_visual.ActualHeight));
        }

        public UIElement CreateOverlay(FixedPage page, GetPageCompletedEventArgs e)
        {
            // Create a new visual host...  toDO: Maybe needs a check here if our VisualHost is already added to the page???
            //VisualHost host = new VisualHost();
            // ...and add our test-visual
            //host.AddVisual(CreateVisual(page.Width, page.Height));
            return GenerateAdornerElement(page.Width, page.Height);
        }

        public UIElement CreateOverlay(double parentWidth,double parentHeight)
        {
            // Create a new visual host...  toDO: Maybe needs a check here if our VisualHost is already added to the page???
            //VisualHost host = new VisualHost();
            // ...and add our test-visual
            //host.AddVisual(CreateVisual(page.Width, page.Height));
            if(parentWidth==0||parentHeight==0)
            {
                return null;
            }
            return GenerateAdornerElement(parentWidth, parentHeight);
        }

        public void DrawOverlayByGraphics(double parentWidth, double parentHeight)
        {
            if (Graphics == null)
            {
                return;
            }
            if(parentWidth==0||parentHeight==0)
            {
                return;
            }
            Console.WriteLine(parentWidth + "---" + parentHeight);
            //This operation is used to get Text's width and height.
            float emSize = (72.0f / 96.0f) * m_waterMark.FontSize;
            SizeF size = Graphics.MeasureString(m_waterMark.Text, new Font(m_waterMark.FontName, emSize, System.Drawing.FontStyle.Regular));
            double startX = 0;
            double startY = 0;
            double hypotenuse = size.Width;
            double height = size.Height;
            int width = (int)(hypotenuse / Math.Sqrt(2));

            //Create Overlay text according to parent's width and height.
            for (double y = startY; y < parentHeight; y = y + width + height)
            {
                for (double x = startX; x <= parentWidth; x = x + hypotenuse)
                {
                    // Create font and brush.
                    Font drawFont = new Font(m_waterMark.FontName, emSize);
                    SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.FromArgb((int)(0xFF*(m_waterMark.TransparentRatio*1.0/100)),
                        ColorTranslator.FromHtml(m_waterMark.FontColor)));
                    // Create rectangle for drawing.
                    RectangleF drawRect = new RectangleF((float)x, (float)y, (float)hypotenuse, (float)height);
                    //Create matrix to rotate rectangle.
                    System.Drawing.Drawing2D.Matrix matrix = new System.Drawing.Drawing2D.Matrix();

                    // test for rotation --- 45
                    matrix.RotateAt(45, 
                        new PointF(drawRect.Left + (drawRect.Width / 2), drawRect.Top + (drawRect.Height / 2)));
                    Graphics.Transform = matrix;

                    Graphics.DrawString(m_waterMark.Text, drawFont, drawBrush, drawRect);
                    Graphics.ResetTransform();
                }
            }
        }

        public Visual CreateVisual(double parentWidth, double parentHeight)
        {
            // create a drawing visual
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext ctx = visual.RenderOpen())
            {
                //This operation is used to get TextBlock's width and height.
                TextBlock tmp = CreateOverlayText();
                tmp.Measure(new System.Windows.Size(Double.PositiveInfinity, Double.PositiveInfinity));
                tmp.Arrange(new Rect(tmp.DesiredSize));
                double startX = 0;
                double startY = 0;
                double hypotenuse = tmp.ActualWidth;
                double height = tmp.ActualHeight;
                int width = (int)(hypotenuse / Math.Sqrt(2));

               // Watermark watermark = ViewerApp.ViewerInstance.NxlFileInfo.Watermark;

                ctx.PushOpacity(m_waterMark.TransparentRatio / 100f > 1.0 ? 1.0 : m_waterMark.TransparentRatio / 100f);
                //Create Overlay text according to parent's width and height.
                for (double y = startY; y <= parentHeight; y = y + width + height)
                {
                    for (double x = startX; x <= parentWidth; x = x + hypotenuse)
                    {
                        int angle = -45;

                        FormattedText element = CreateFormattedText();
                        // test for rotation --- 45
                        ctx.PushTransform(new RotateTransform(-angle));
                        
                        ctx.DrawText(element, new System.Windows.Point(x, y));
                        ctx.Pop();
                        angle = 45;    
                    }
                }
            }
            return visual;
        }

        private void RemoveOverlay(FrameworkElement visual)
        {
            if (ParentLayer == null)
            {
                ParentLayer = AdornerLayer.GetAdornerLayer(visual);
            }
            if (ParentLayer == null)
            {
                return;
            }
            if (m_adorner != null)
            {
                m_adorner.Remove();
                ParentLayer.Remove(m_adorner);
            }
        }

        private UIElement GenerateAdornerElement(double parentWidth, double parentHeight)
        {
            if (parentWidth == 0 || parentHeight == 0)
            {
                return null;
            }

            Console.WriteLine(parentWidth + "---" + parentHeight);
            //Use Canvas to be Overlay texts' root layout which can control childs' position by settingX and settingY.
            Canvas canvas = new Canvas();
            //This operation is used to get TextBlock's width and height.
            TextBlock tmp = CreateOverlayText();
            tmp.Measure(new System.Windows.Size(Double.PositiveInfinity, Double.PositiveInfinity));
            tmp.Arrange(new Rect(tmp.DesiredSize));

            double startX = 0;
            double startY = 0;
            double hypotenuse = tmp.ActualWidth==0? parentWidth: tmp.ActualWidth;
            double height = tmp.ActualHeight==0? parentHeight: tmp.ActualHeight;     
            int width = (int)(hypotenuse / Math.Sqrt(2));
            //Create Overlay text according to parent's width and height.
            //guard against memory leak
            TextBlock element;
            for (double y = startY; y < parentHeight; y = y + width + height)
            {
                for (double x = startX; x <= parentWidth; x = x + hypotenuse)
                {
                    element = CreateOverlayText();
                    Canvas.SetLeft(element, x);
                    Canvas.SetTop(element, y);
                    canvas.Children.Add(element);
                }
            }
            return canvas;
        }

        private TextBlock CreateOverlayText()
        {
            return WrapperTextBlock();
        }

        private FormattedText CreateFormattedText()
        {
            // just draw some text
            System.Windows.Media.Brush brush;
            FormattedText text;
            BrushConverter brushConverter = new BrushConverter();
            brush = (System.Windows.Media.Brush)brushConverter.ConvertFromString(m_waterMark.FontColor);
            text = new FormattedText(m_waterMark.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface("Tahoma"), 12 * 1.5,
                brush);
            text.SetFontSize(m_waterMark.FontSize);
            text.SetForegroundBrush(brush);
            return text;
        }
    
        private TextBlock WrapperTextBlock()
        {
            TextBlock element = new TextBlock();
            BrushConverter brushConverter = new BrushConverter();
            System.Windows.Media.Brush brush = (System.Windows.Media.Brush)brushConverter.ConvertFromString(m_waterMark.FontColor);
            element.Foreground = brush;
            // test for rotation --- 45
            element.LayoutTransform = new RotateTransform(45);
            element.Text = m_waterMark.Text;
            element.FontFamily = new System.Windows.Media.FontFamily(m_waterMark.FontName);
            element.FontSize = m_waterMark.FontSize;
            element.Opacity = m_waterMark.TransparentRatio / 100f > 1.0 ? 1.0 : m_waterMark.TransparentRatio / 100f;
          
            return element;
        }

        #region Class UIElementAdorner
        private class UIElementAdorner : Adorner
        {
            private List<UIElement> m_logicalChildren;
            private UIElement m_element;

            public UIElementAdorner(UIElement adornedElement, UIElement element)
                : base(adornedElement)
            {
                m_element = element;
            }

            public void Add()
            {
                base.AddLogicalChild(m_element);
                base.AddVisualChild(m_element);
            }

            public void Remove()
            {
                base.RemoveLogicalChild(m_element);
                base.RemoveVisualChild(m_element);
            }

            protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
            {
                m_element.Measure(constraint);
                return m_element.DesiredSize;
            }

            protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
            {
                System.Windows.Point location = new System.Windows.Point(0, 0);
                Rect rect = new Rect(location, finalSize);
                m_element.Arrange(rect);
                return finalSize;
            }

            protected override int VisualChildrenCount
            {
                get { return 1; }
            }

            protected override Visual GetVisualChild(int index)
            {
                if (index != 0)
                    throw new ArgumentOutOfRangeException("index");

                return m_element;
            }

            protected override IEnumerator LogicalChildren
            {
                get
                {
                    if (m_logicalChildren == null)
                    {
                        m_logicalChildren = new List<UIElement>();
                        m_logicalChildren.Add(m_element);
                    }
                    return m_logicalChildren.GetEnumerator();
                }
            }
        }
        #endregion

        #region class VisualHost
        public class VisualHost : UIElement
        {
            private List<Visual> fVisuals;

            public VisualHost()
            {
                fVisuals = new List<Visual>();
            }

            protected override Visual GetVisualChild(int index)
            {
                return fVisuals[index];
            }

            protected override int VisualChildrenCount
            {
                get { return fVisuals.Count; }
            }

            public void AddVisual(Visual visual)
            {
                fVisuals.Add(visual);
                base.AddVisualChild(visual);
            }

            public void RemoveVisual(Visual visual)
            {
                fVisuals.Remove(visual);
                base.RemoveVisualChild(visual);
            }
        }
        #endregion
    }
}
