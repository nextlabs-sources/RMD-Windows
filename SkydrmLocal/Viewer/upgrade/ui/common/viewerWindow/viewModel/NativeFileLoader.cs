using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Viewer.upgrade.application;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.avPage.view;
using Viewer.upgrade.ui.common.hoops.view;
using Viewer.upgrade.ui.common.htmlPage.view;
using Viewer.upgrade.ui.common.imagePage.view;
using Viewer.upgrade.ui.common.loadingBarPage.view;
using Viewer.upgrade.ui.common.richTextPage.view;
using Viewer.upgrade.ui.common.vdsPage.view;
using Viewer.upgrade.ui.normal.page.toolbar.view;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.ui.common.viewerWindow.viewModel
{
    public class NativeFileLoader : IFileLoader
    {
        private ViewerApp mApplication;
        private ViewModel mViewModel;
        private IFile mNativeFile;
        private string mTempFile = string.Empty;

        public NativeFileLoader(ViewModel viewModel, IFile nativeFile)
        {
            mApplication = (ViewerApp)Application.Current;
            mViewModel = viewModel;
            mNativeFile = nativeFile;
            InitializePage(nativeFile);
        }

        private string CopyFileIfPathToLong(string longFilePath)
        {
            string result = longFilePath;
            if (longFilePath.StartsWith(@"\\?\", StringComparison.CurrentCultureIgnoreCase))
            {
                //  string destination = Path.GetTempPath() + Path.GetFileName(longFilePath);
                string destination = Path.GetTempPath() + Guid.NewGuid().ToString() + Path.GetExtension(longFilePath);
                File.Copy(longFilePath, destination, true);
                mTempFile = destination;
                result = destination;
            }
            return result;
        }

        public void InitializePage(IFile nativeFile)
        {
            if (EnumFileType.FILE_TYPE_NOT_SUPPORT == nativeFile.FileType)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("VIEW_DLGBOX_DETAILS_NOTSUPPORT").ToString());
                return;
            }

            switch (nativeFile.FileType)
            {
                case EnumFileType.FILE_TYPE_HOOPS_3D:
                case EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D:
                    Load3DFile(nativeFile);
                    break;

                case EnumFileType.FILE_TYPE_OFFICE:
                    LoadOfficeFile(nativeFile);
                    break;

                case EnumFileType.FILE_TYPE_PDF:
                    LoadPdfFile(nativeFile);
                    break;

                case EnumFileType.FILE_TYPE_IMAGE:
                    LoadImageFile(nativeFile);
                    break;


                case EnumFileType.FILE_TYPE_PLAIN_TEXT:
                    LoadTextFile(nativeFile);
                    break;

                case EnumFileType.FILE_TYPE_VIDEO:
                case EnumFileType.FILE_TYPE_AUDIO:
                    LoadAVFile(nativeFile);
                    break;

                case EnumFileType.FILE_TYPE_SAP_VDS:
                    LoadVDSFile(nativeFile);
                    break;

                case EnumFileType.FILE_TYPE_HYPERTEXT_MARKUP:
                    LoadHypertextMarkupFile(nativeFile);
                    break;
            }
        }

        public void LoadHypertextMarkupFile(IFile nativeFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                HtmlPage htmlPage = new HtmlPage(CopyFileIfPathToLong(nativeFile.FilePath));

                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                mViewModel.Viewer = new Frame
                {
                    Content = htmlPage
                };

                htmlPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }
        }

        public void LoadVDSFile(IFile nativeFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                VdsPage vdsPage = new VdsPage(CopyFileIfPathToLong(nativeFile.FilePath));
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                mViewModel.Viewer = new Frame
                {
                    Content = vdsPage
                };

                vdsPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }
        }


        public void LoadAVFile(IFile nativeFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                AVPage aVPage = new AVPage(CopyFileIfPathToLong(nativeFile.FilePath));

                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                mViewModel.Viewer = new Frame
                {
                    Content = aVPage
                };

                aVPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }
        }


        public void LoadTextFile(IFile nativeFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                RichTextPage richTextPage = new RichTextPage(CopyFileIfPathToLong(nativeFile.FilePath));

                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                mViewModel.Viewer = new Frame
                {
                    Content = richTextPage
                };

                richTextPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }
        }


        private void LoadImageFile(IFile nativeFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                ImagePage imagePage = new ImagePage(CopyFileIfPathToLong(nativeFile.FilePath));

                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                mViewModel.Viewer = new Frame
                {
                    Content = imagePage
                };

                imagePage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception  ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }
        }

        private void LoadPdfFile3(IFile nativeFile)
        {
            try
            {
                mApplication.Log.Info("\t\t is 2D Pdf file. \r\n");
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                Viewer.upgrade.ui.common.edgeWebView2Page.view.EdgeWebView2Page edgeWebView2Page = new Viewer.upgrade.ui.common.edgeWebView2Page.view.EdgeWebView2Page(nativeFile.FilePath);
                mViewModel.Viewer = new Frame
                {
                    Content = edgeWebView2Page
                };

                edgeWebView2Page.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mApplication.Log.Error(ex.Message, ex);
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mApplication.Log.Error(ex.Message, ex);
            }
        }


        private void LoadPdfFile(IFile nativeFile)
        {
            if (FileUtils.Is3DPdf(CopyFileIfPathToLong(nativeFile.FilePath)))
            {
                Load3DFile(nativeFile);
            }
            else
            {
               // LoadPdfFile2(nativeFile);
                LoadPdfFile3(nativeFile);
            }
        }

        private void LoadPdfFile2(IFile nativeFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                PreviewHandler previewHandler = new PreviewHandler(nativeFile.Extention);
                Viewer.upgrade.ui.common.previewer2.view.PreviewerPage previewerPage = new Viewer.upgrade.ui.common.previewer2.view.PreviewerPage(
                                                                          nativeFile.FilePath,
                                                                          previewHandler
                                                                          );
                mViewModel.Viewer = new Frame
                {
                    Content = previewerPage
                };

                previewerPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }
        }


        private void LoadOfficeFile(IFile nativeFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                PreviewHandler previewHandler = new PreviewHandler(nativeFile.Extention);
                Viewer.upgrade.ui.common.previewer2.view.PreviewerPage previewerPage = new Viewer.upgrade.ui.common.previewer2.view.PreviewerPage(
                                                                          CopyFileIfPathToLong(nativeFile.FilePath),
                                                                          previewHandler
                                                                          );
                mViewModel.Viewer = new Frame
                {
                    Content = previewerPage
                };

                previewerPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }

            //  UInt64 res = ToolKit.InitializeComObj(nativeFile.Extention, out previewHandler);
            //if (ErrorCode.SUCCEEDED == res)
            //{
            //    Viewer.upgrade.ui.common.previewer2.view.PreviewerPage previewerPage = new Viewer.upgrade.ui.common.previewer2.view.PreviewerPage(
            //                                                                            nativeFile.FilePath,
            //                                                                            previewHandler
            //                                                                            );
            //    mViewModel.Viewer = new Frame
            //    {
            //        Content = previewerPage
            //    };

            //    previewerPage.Sensor.OnUnhandledExceptionOccurrence += delegate (UInt64 errorCode)
            //    {
            //        mViewModel.LoadErrorPage(nativeFile.FileName, errorCode);
            //    };
            //}
            //else
            //{
            //    mViewModel.LoadErrorPage(nativeFile.FileName, res);
            //}
        }


        private void Load3DFile(IFile nativeFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nativeFile.FileName);
                ThreeDViewer threeDViewer = new ThreeDViewer(CopyFileIfPathToLong(nativeFile.FilePath));

                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                mViewModel.Viewer = new Frame
                {
                    Content = threeDViewer
                };

                threeDViewer.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nativeFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }
        }

        public void Close()
        {
            ClearTempFile();
        }

        private void ClearTempFile()
        {
            try
            {
                if ((!string.IsNullOrEmpty(mTempFile)) && (File.Exists(mTempFile)))
                {
                    mApplication.SdkSession.RPM_DeleteFile(mTempFile);
                    //File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
