using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.utils.overlay.window;

namespace Viewer.upgrade.utils.overlay.decorator
{
    public class OverlayDecorator : Decorator
    {
        private OverlayWindow mOverlayWindow;
        private double m_y, m_x;
        private Window m_parent;

        public OverlayDecorator(WatermarkInfo watermarkInfo)
        {
            mOverlayWindow = new OverlayWindow(watermarkInfo);
        }

        private void M_overlay_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            m_parent.Focus();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdatePosition(false);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (mOverlayWindow.Visibility != Visibility.Visible)
            {
                UpdatePosition(true);
                mOverlayWindow.Show();
                mOverlayWindow.PreviewMouseDown += new MouseButtonEventHandler(M_overlay_PreviewMouseDown);
                m_parent = GetParent(this);
                mOverlayWindow.Owner = m_parent;
                m_parent.LocationChanged += new EventHandler(Parent_LocationChanged);
            }
        }

        private void Parent_LocationChanged(object sender, EventArgs e)
        {
            UpdatePosition(true);
        }

        private Window GetParent(DependencyObject obj)
        {
            var parent = VisualTreeHelper.GetParent(obj);

            if (parent != null)
                return GetParent(parent);

            var o = obj as FrameworkElement;

            if (o != null)
            {
                if (o is Window)
                    return (o as Window);
                if (o.Parent != null)
                    return GetParent(o.Parent);
            }

            return null;
        }

        private void UpdatePosition(bool updatexy)
        {
            if (m_parent == null)
            {
                m_parent = GetParent(this);
            }

            if (!m_parent.IsVisible)
            {
                mOverlayWindow.Hide();
                return;
            }
            mOverlayWindow.Show();

            var windowContent = ((m_parent.Content) as FrameworkElement);
            var r = LayoutInformation.GetLayoutSlot(this);
            var p = TransformToAncestor(m_parent).Transform(new System.Windows.Point(0, 0));

            m_x = (m_parent.ActualWidth - windowContent.ActualWidth) / 2 + p.X;
            m_y = (m_parent.ActualHeight - windowContent.ActualHeight) - m_x + p.Y;

            if (updatexy)
            {
                mOverlayWindow.Left = m_parent.Left + r.Left + m_x;
                mOverlayWindow.Top = m_parent.Top + r.Top + m_y;
            }
            mOverlayWindow.Width = r.Width;
            mOverlayWindow.Height = r.Height;
        }


    }
}
