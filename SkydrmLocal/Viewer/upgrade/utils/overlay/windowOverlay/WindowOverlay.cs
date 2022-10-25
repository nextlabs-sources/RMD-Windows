using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SAP.VE.DVL;
using System.Drawing;
using Viewer.upgrade.file.basic.utils;
using System.Globalization;

namespace Viewer.upgrade.utils.overlay.windowOverlay
{
    public class WindowOverlay : Decorator
    {
        private double m_y, m_x;
        private Window m_parent;
        private Window m_overlay;
        private Object m_content;
        private int mChildrenCount = 0;
        public int ChildrenCount { get { return mChildrenCount; } set { mChildrenCount = value; } }

        public WindowOverlay()
        {
            CreateOverlayWindow();
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return mChildrenCount;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException("index");

            return (Visual)m_overlay.Content;
        }

        public void overlay_MouseWheelEventHandler(object sender, MouseWheelEventArgs e){
            this.RaiseEvent(e);
        }

        private void CreateOverlayWindow()
        {
            m_overlay = new Window();

            m_overlay.WindowStyle = WindowStyle.None;
            m_overlay.ShowInTaskbar = false;
            m_overlay.Focusable = false;
            m_overlay.Background = System.Windows.Media.Brushes.Transparent;
            m_overlay.AllowsTransparency = true;

            m_overlay.PreviewMouseDown += new MouseButtonEventHandler(M_overlay_PreviewMouseDown);
            m_overlay.MouseWheel += new MouseWheelEventHandler(overlay_MouseWheelEventHandler);
        }

        public void Show(bool show)
        {
            if (show)
            {
                CreateOverlayWindow();
                m_overlay.Content = m_content;
                m_overlay.Show();
                m_overlay.Owner = m_parent;
            }
            else
            {
                m_overlay.Hide();
                m_overlay.Close();
            }

            m_parent.Focus();
        }

        public void Close()
        {
            m_overlay?.Hide();
            m_overlay?.Close();
            if (null != m_parent)
            {
                m_parent.LocationChanged -= Parent_LocationChanged;
                m_parent.Focus();
            }
        }

        public object OverlayContent
        {
            get
            {
                return m_overlay == null ? null : m_overlay.Content;
            }

            set
            {
                if (m_overlay != null)
                {
                    m_content = value;
                    m_overlay.Content = value;
                }
            }
        }

        private void M_overlay_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            m_parent.Focus();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdatePosition(false);
          // UpdatePosition(true);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (m_overlay.Visibility != Visibility.Visible)
            {
                UpdatePosition(true);
                m_overlay.Show();
                m_parent = GetParent(this);
                m_overlay.Owner = m_parent;
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
                m_overlay.Hide();
                return;
            }
            m_overlay.Show();

            var windowContent = ((m_parent.Content) as FrameworkElement);
            var r = LayoutInformation.GetLayoutSlot(this);
            var p = TransformToAncestor(m_parent).Transform(new System.Windows.Point(0, 0));

            m_x = (m_parent.ActualWidth - windowContent.ActualWidth) / 2 + p.X;
            m_y = (m_parent.ActualHeight - windowContent.ActualHeight) - m_x + p.Y;

            if (updatexy)
            {
                m_overlay.Left = m_parent.Left + r.Left + m_x;
                m_overlay.Top = m_parent.Top + r.Top + m_y;
            }
            m_overlay.Width = r.Width;
            m_overlay.Height = r.Height;
        }
    }
}
