using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Viewer.upgrade.ui.common.errorPage.view;
using Viewer.upgrade.ui.common.viewerWindow.view;

namespace Viewer.upgrade.ui.common.viewerWindow.viewModel
{
    public abstract class AbsViewModel : INotifyPropertyChanged, IViewModel
    {
        protected Frame mToolbar;
        protected Frame mViewer;
        protected ViewerWindow mViewerWindow;

        public Frame Toolbar
        {
            get
            {
                return mToolbar;
            }
            set
            {
                mToolbar = value;
                OnPropertyChanged("Toolbar");
            }
        }

        public Frame Viewer
        {
            get
            {
                return mViewer;
            }
            set
            {
                mViewer = value;
                OnPropertyChanged("Viewer");
            }
        }

        public Window Window
        {
            get
            {
                return mViewerWindow;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AbsViewModel(ViewerWindow viewerWindow)
        {
            mViewerWindow = viewerWindow;
        }

        public abstract void Window_Closed();
        public abstract void Window_Loaded();
        public abstract void Window_ContentRendered();

        public void LoadErrorPage(string fileName, string errorMessage)
        {
            Viewer.upgrade.ui.normal.page.toolbar.view.ToolBarPage toolbarPage = new normal.page.toolbar.view.ToolBarPage(fileName);
            ErrorPage errorPage = new ErrorPage(errorMessage);

            Toolbar = new Frame()
            {
                Content = toolbarPage
            };

            Viewer = new Frame
            {
                Content = errorPage
            };
        }

    }
}
