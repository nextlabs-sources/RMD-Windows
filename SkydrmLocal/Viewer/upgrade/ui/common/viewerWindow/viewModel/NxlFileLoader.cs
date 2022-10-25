using Newtonsoft.Json;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Viewer.upgrade.application;
using Viewer.upgrade.communication.message;
using Viewer.upgrade.communication.namedPipe.client;
using Viewer.upgrade.exception;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.file.utils;
using Viewer.upgrade.ui.common.avPage.view;
using Viewer.upgrade.ui.common.hoops.view;
using Viewer.upgrade.ui.common.htmlPage.view;
using Viewer.upgrade.ui.common.imagePage.view;
using Viewer.upgrade.ui.common.richTextPage.view;
using Viewer.upgrade.ui.common.vdsPage.view;
using Viewer.upgrade.ui.nxl.page.toolbar.view;
using Viewer.upgrade.utils;
using static Viewer.upgrade.communication.namedPipe.client.NamedPipeClient;
using static Viewer.upgrade.utils.FileUtils;
using static Viewer.upgrade.utils.ToolKit;
using Viewer.upgrade.ui.common.email.view;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;

namespace Viewer.upgrade.ui.common.viewerWindow.viewModel
{
    public class NxlFileLoader : IFileLoader
    {
        private ViewerApp mApplication;
        private log4net.ILog mLog;
        private ViewModel mViewModel;
        private INxlFile mNxlFile;
        private string mTempRpmFolder;

        public NxlFileLoader(ViewModel viewModel, INxlFile nxlFile)
        {
            mApplication = (ViewerApp)ViewerApp.Current;
            mLog = mApplication.Log;
            mViewModel = viewModel;
            mNxlFile = nxlFile;
            InitializePage(mNxlFile);
        }

        public void Close()
        {
            try
            {
                mNxlFile.ClearTempFiles();
            }
            catch (Exception ex)
            {

            }

            // Will try to remove the temp RPM folder again in "OnExit" before the application exit.
            if (!string.IsNullOrEmpty(mTempRpmFolder))
            {
                int option;
                string tags;
                if (mApplication.SdkSession.RMP_IsSafeFolder(mTempRpmFolder, out option, out tags))
                {
                    mApplication.SdkSession.RPM_RemoveDir(mTempRpmFolder, out string errorMsg);
                    mApplication.Temp_RPM_Folders.TryRemove(mNxlFile.FilePath, out string tempRPMDir);
                }
            }
        }

        private void InitializePage(INxlFile nxlFile)
        {

            if (EnumFileType.FILE_TYPE_NOT_SUPPORT == nxlFile.FileType)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("VIEW_DLGBOX_DETAILS_NOTSUPPORT").ToString());
                return;
            }

            switch (nxlFile.FileType)
            {
                case EnumFileType.FILE_TYPE_EMAIL:
                    LoadEmailFile(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_HOOPS_3D:
                    Load3DFile(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_PDF:
                   // LoadPdfFile(nxlFile);
                    LoadPdfFile2(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D:
                    LoadAssemblyFile(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_OFFICE:
                    LoadOfficeFile(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_IMAGE:
                    LoadImageFile(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_PLAIN_TEXT:
                    LoadTextFile(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_VIDEO:
                case EnumFileType.FILE_TYPE_AUDIO:
                    LoadAVFile(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_SAP_VDS:
                    LoadVDSFile(nxlFile);
                    break;

                case EnumFileType.FILE_TYPE_HYPERTEXT_MARKUP:
                    LoadHypertextMarkupFile(nxlFile);
                    break;
            }
        }


        public void LoadHypertextMarkupFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString()+ nxlFile.Extention);
                HtmlPage htmlPage = new HtmlPage(rpmFilePath);
                htmlPage.Watermark(nxlFile.WatermarkInfo);
                mViewModel.Viewer = new Frame
                {
                    Content = htmlPage
                };

                toolbarPage.Sensor.OnClickPrint += htmlPage.Print;
                htmlPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    toolbarPage.Sensor.OnClickPrint -= htmlPage.Print;
                    toolbarPage.SetParentWindow(null);
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                };

                htmlPage.Sensor.OnLoadFileSucceed += delegate ()
                {
                    toolbarPage.SetParentWindow(mViewModel.Window);
                    mViewModel.SendViewLog(true);
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }


        public void LoadVDSFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);
                VdsPage vdsPage = new VdsPage(rpmFilePath);
                vdsPage.Watermark(nxlFile.WatermarkInfo);
                mViewModel.Viewer = new Frame
                {
                    Content = vdsPage
                };

                toolbarPage.Sensor.OnClickPrint += vdsPage.Print;
                vdsPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    toolbarPage.Sensor.OnClickPrint -= vdsPage.Print;
                    toolbarPage.SetParentWindow(null);
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                };

                vdsPage.Sensor.OnLoadFileSucceed += delegate ()
                {
                    toolbarPage.SetParentWindow(mViewModel.Window);
                    mViewModel.SendViewLog(true);
                };

                vdsPage.Sensor.EndPrint += delegate (bool result)
                {
                    if (result)
                    {
                        mApplication.SdkSession.User.AddLog(nxlFile.FilePath, NxlOpLog.Print, true);
                    }
                    else
                    {
                        mApplication.SdkSession.User.AddLog(nxlFile.FilePath, NxlOpLog.Print, false);
                    }
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }


        public void LoadAVFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);
                AVPage aVPage = new AVPage(rpmFilePath);
                aVPage.Watermark(nxlFile.WatermarkInfo);
                mViewModel.Viewer = new Frame
                {
                    Content = aVPage
                };

                aVPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    toolbarPage.SetParentWindow(null);
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                };

                aVPage.Sensor.OnLoadFileSucceed += delegate ()
                {
                    toolbarPage.SetParentWindow(mViewModel.Window);
                    mViewModel.SendViewLog(true);
                };

            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }


        public void LoadTextFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };
                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);

                RichTextPage richTextPage = new RichTextPage(rpmFilePath);
                richTextPage.Watermark(nxlFile.WatermarkInfo);
                mViewModel.Viewer = new Frame
                {
                    Content = richTextPage
                };

                toolbarPage.Sensor.OnClickPrint += richTextPage.Print;
                richTextPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    toolbarPage.SetParentWindow(null);
                    toolbarPage.Sensor.OnClickPrint -= richTextPage.Print;
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                };

                richTextPage.Sensor.OnLoadFileSucceed += delegate ()
                {
                    toolbarPage.SetParentWindow(mViewModel.Window);
                    mViewModel.SendViewLog(true);
                };

                richTextPage.Sensor.BeforePrint += delegate (System.Windows.Forms.PrintDialog printDialog)
                {
                    printDialog.Document.DocumentName += ":" + nxlFile.FilePath;
                };

                richTextPage.Sensor.EndPrint += delegate (bool result)
                {
                    if (result)
                    {
                        mViewModel.SendPrintLog(true);
                    }
                    else
                    {
                        mViewModel.SendPrintLog(false);
                    }
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }

        private void LoadImageFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };
                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);

                ImagePage imagePage = new ImagePage(rpmFilePath);
                imagePage.Watermark(nxlFile.WatermarkInfo);
                mViewModel.Viewer = new Frame
                {
                    Content = imagePage
                };

                toolbarPage.Sensor.OnClickPrint += imagePage.Print;
                toolbarPage.Sensor.OnClickLeftRotate += imagePage.RotateLeft;
                toolbarPage.Sensor.OnClickRightRotate += imagePage.RotateRight;
                toolbarPage.Sensor.OnClickReset += imagePage.Reset;
                imagePage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    toolbarPage.SetParentWindow(null);
                    toolbarPage.Sensor.OnClickPrint -= imagePage.Print;
                    toolbarPage.Sensor.OnClickLeftRotate -= imagePage.RotateLeft;
                    toolbarPage.Sensor.OnClickRightRotate -= imagePage.RotateRight;
                    toolbarPage.Sensor.OnClickReset -= imagePage.Reset;
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                };

                imagePage.Sensor.OnLoadFileSucceed += delegate ()
                {
                    toolbarPage.SetParentWindow(mViewModel.Window);
                    mViewModel.SendViewLog(true);
                };

                imagePage.Sensor.BeforePrint += delegate (System.Windows.Forms.PrintDialog printDialog)
                {
                    printDialog.Document.DocumentName += ":" + nxlFile.FilePath;
                };

                imagePage.Sensor.EndPrint += delegate (bool result)
                {
                    if (result)
                    {
                        mViewModel.SendPrintLog(true);
                    }
                    else
                    {
                        mViewModel.SendPrintLog(false);
                    }
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }

        private async void LoadOfficeFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                Task<DecryptResult> task_decryptResult = Task.Factory.StartNew<DecryptResult>((x) =>
                {
                    DecryptResult decryptResult = new DecryptResult();
                    try
                    {
                        decryptResult.RpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);
                    }
                    catch (Exception ex)
                    {
                        decryptResult.Exception = ex;
                    }

                    return decryptResult;

                }, nxlFile, mApplication.Token);


                Task<ComObjInitializeResult> task_comObjInitializeResult = Task.Factory.StartNew<ComObjInitializeResult>((x) =>
                {
                    ComObjInitializeResult comObjInitializeResult = new ComObjInitializeResult();
               
                    try
                    {
                       // OfficeRMXHelper.ChangeRegeditOfOfficeAddin(mApplication.SdkSession);
                        comObjInitializeResult.PreviewHandler = new PreviewHandler(nxlFile.Extention);
                        RegisterProcessUtils.ProcessRegister(mApplication.SdkSession, nxlFile.Extention);
                    }
                    catch (Exception ex)
                    {
                        comObjInitializeResult.Exception = ex;
                    }

                    return comObjInitializeResult;

                }, nxlFile, mApplication.Token);


                Task[] tasks = new Task[] { task_decryptResult, task_comObjInitializeResult };
                await Task.WhenAll(tasks);

                if (null == task_decryptResult.Result.Exception && null == task_comObjInitializeResult.Result.Exception)
                {
                    Viewer.upgrade.ui.common.previewer2.view.PreviewerPage previewerPage = new Viewer.upgrade.ui.common.previewer2.view.PreviewerPage(
                                                                            task_decryptResult.Result.RpmFilePath,
                                                                            task_comObjInitializeResult.Result.PreviewHandler);
                    previewerPage.WatermarkInfo = nxlFile.WatermarkInfo;
                    previewerPage.Sensor.OnOverlayWindowLoaded += toolbarPage.SetParentWindow;
                    previewerPage.NxlFile = nxlFile;

                    if (EnumFileType.FILE_TYPE_PDF == nxlFile.FileType)
                    {
                        previewerPage.OverlayOffsetsBottom = 35;
                    }

                    mViewModel.Viewer = new Frame
                    {
                        Content = previewerPage
                    };

                    toolbarPage.Sensor.OnClickEdit += delegate ()
                    {
                        try
                        {
                            mNxlFile.Edit(EditSaved, ProcessExited);
                            toolbarPage.SetParentWindow(null);
                            mViewModel.Window.Hide();
                        }
                        catch (NxlRMAddinUnloadException ex)
                        {
                            mLog.Error(ex.Message);
                            //  MessageNotify.ShowBalloonTip(mApplication.SdkSession, mApplication.FindResource("Not_To_Recognize_Nxrmaddin").ToString(), false);
                            MessageNotify.ShowBalloonTip(mApplication.SdkSession, mApplication.FindResource("Common_System_Internal_Error").ToString(), false);
                        }
                        catch (Exception ex)
                        {
                            mLog.Error(ex.Message);
                            MessageNotify.ShowBalloonTip(mApplication.SdkSession, mApplication.FindResource("Common_System_Internal_Error").ToString(), false);
                        }
    
                        //mApplication.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        //toolbarPage.SetParentWindow(null);
                        //mViewModel.Window.Close();

                        //if (ToolKit.WORD_EXTENSIONS.Contains(nxlFile.Extention) || ToolKit.POWERPOINT_EXTENSIONS.Contains(nxlFile.Extention))
                        //{
                        //    mApplication.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        //    toolbarPage.SetParentWindow(null);
                        //    mViewModel.Window.Close();
                        //}

                        //if (ToolKit.EXCEL_EXTENSIONS.Contains(nxlFile.Extention))
                        //{
                        //    toolbarPage.SetParentWindow(null);
                        //    mViewModel.Window.Hide();
                        //}
                    };

                    toolbarPage.Sensor.OnClickPrint += previewerPage.Print;
                    previewerPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception exception)
                    {
                        previewerPage.Sensor.OnOverlayWindowLoaded -= toolbarPage.SetParentWindow;
                        toolbarPage.Sensor.OnClickPrint -= previewerPage.Print;
                        toolbarPage.SetParentWindow(null);
                        mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                        mViewModel.SendViewLog(false);
                    };

                    previewerPage.Sensor.OnLoadFileSucceed += delegate ()
                    {
                        mViewModel.SendViewLog(true);
                    };
                }
                else
                {
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                }

            }catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }

        public void ProcessExited()
        {
           // ShutdownApplication();
        }

        public void EditSaved(bool b)
        {
            // Notify RMD to update file status and do sync.
            try
            {
                Bundle<EditCallBack> bundle = new Bundle<EditCallBack>()
                {
                    Intent = Intent.SyncFileAfterEdit,
                    obj = new EditCallBack(b, mNxlFile.FilePath)

                };
                string json = JsonConvert.SerializeObject(bundle);
                NamedPipeClient.Start(json);
            }
            catch (Exception ex)
            {
                mLog.Error(ex.ToString());
            }

            ShutdownApplication();
        }

        private void ShutdownApplication()
        {
            mApplication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (ShutdownMode.OnExplicitShutdown == mApplication?.ShutdownMode)
                {
                    mApplication?.Shutdown();
                }
                else
                {
                    mViewModel.Window.Close();
                }
            }));
        }

        public void LoadAssemblyFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };

                switch (nxlFile.Dirstatus)
                {
                    case _NxlFile.RPM_SAFEDIRRELATION_SAFE_DIR:
                    case _NxlFile.RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR:
                    case _NxlFile.RPM_SAFEDIRRELATION_SAFE_DIR + _NxlFile.RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR:
                    case _NxlFile.RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR + _NxlFile.RPM_SAFEDIRRELATION_ANCESTOR_OF_SAFE_DIR:
                    case _NxlFile.RPM_SAFEDIRRELATION_SAFE_DIR + _NxlFile.RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR + _NxlFile.RPM_SAFEDIRRELATION_ANCESTOR_OF_SAFE_DIR:

                        string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);

                        ThreeDViewer threeDViewer = new ThreeDViewer(rpmFilePath);
                        mViewModel.Viewer = new Frame
                        {
                            Content = threeDViewer
                        };

                        threeDViewer.Sensor.OnLoadFileSucceed += delegate ()
                        {
                            mViewModel.SendViewLog(true);
                        };

                        threeDViewer.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                        {
                            mViewModel.LoadErrorPage(mNxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                            mViewModel.SendViewLog(false);
                        };

                        break;

                    case _NxlFile.NORMAL_DIR:
                    case _NxlFile.RPM_SAFEDIRRELATION_ANCESTOR_OF_SAFE_DIR:

                        if (ToolKit.EXCHANGE_3D_ASSEMBLY_ROOT_NODE.Contains(nxlFile.Extention))
                        {
                            string directory = Path.GetDirectoryName(nxlFile.FilePath);
                            if (ToolKit.IsSystemFolderPath(directory) || ToolKit.IsSpecialFolderPath(directory))
                            {
                                MessageNotify.ShowBalloonTip(mApplication.SdkSession, string.Format(mApplication.FindResource("Cannot_Set_RpmFolder_Under_System_Directory").ToString(), directory), false);
                                Load3DFile(nxlFile);
                            }
                            else
                            {
                                LoadAssemblyFileInRPMFolder(nxlFile, toolbarPage);
                            }
                        }
                        else
                        {
                            List<string> paths = new List<string>();
                            List<string> missingpaths = new List<string>();
                            UInt32 missingCounts;
                            string decryptFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);
                            if(mApplication.SdkSession.User.GetAssmblyPathsFromModelFile(decryptFilePath, out paths, out missingpaths, out missingCounts))
                            {
                                if (missingpaths.Count > 0 || paths.Count > 0 || missingCounts > 1)
                                {
                                    if (missingpaths.Count == 0 && missingCounts == 0 && paths.Count == 1 && string.Equals(decryptFilePath, paths[0], StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        Load3DFile(mNxlFile);
                                        return;
                                    }

                                    string directory = Path.GetDirectoryName(nxlFile.FilePath);
                                    if (ToolKit.IsSystemFolderPath(directory) || ToolKit.IsSpecialFolderPath(directory))
                                    {
                                        MessageNotify.ShowBalloonTip(mApplication.SdkSession, string.Format(mApplication.FindResource("Cannot_Set_RpmFolder_Under_System_Directory").ToString(), directory), false);
                                        Load3DFile(toolbarPage, nxlFile, decryptFilePath);
                                    }
                                    else
                                    {
                                        LoadAssemblyFileInRPMFolder(nxlFile, toolbarPage);
                                    }
                                }
                                else
                                {
                                    Load3DFile(mNxlFile);
                                }
                            }
                            else
                            {
                                Load3DFile(mNxlFile);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(mNxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }


        private void Load3DFile(ToolBarPage toolbarPage, INxlFile nxlFile, string rpmFilePath)
        {
            ThreeDViewer threeDViewer = new ThreeDViewer(rpmFilePath);
            threeDViewer.Watermark(nxlFile.WatermarkInfo);
            mViewModel.Viewer = new Frame
            {
                Content = threeDViewer
            };

            toolbarPage.Sensor.OnClickPrint += threeDViewer.Print;
            threeDViewer.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
            {
                toolbarPage.SetParentWindow(null);
                toolbarPage.Sensor.OnClickPrint -= threeDViewer.Print;
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            };

            threeDViewer.Sensor.OnLoadFileSucceed += delegate ()
            {
                toolbarPage.SetParentWindow(mViewModel.Window);
                mViewModel.SendViewLog(true);
            };
        }


        private void LoadAssemblyFileInRPMFolder(INxlFile nxlFile, ToolBarPage toolbarPage)
        {
            try
            {
                string tempRpmFolder = Path.GetDirectoryName(nxlFile.FilePath);
                mApplication.SdkSession.RPM_AddDir(tempRpmFolder);
                mApplication.Temp_RPM_Folders.TryAdd(mNxlFile.FilePath, tempRpmFolder);
                mTempRpmFolder = tempRpmFolder;

                // targetFilePath = Path.Combine(mViewerInstance.TempRpmFolder, Path.GetFileNameWithoutExtension(mFilePath)) + ".nxl";
                string targetFilePath = Path.Combine(mTempRpmFolder, Path.GetFileNameWithoutExtension(nxlFile.FilePath));
                WIN32_FIND_DATA pNextInfo;
                FileUtils.FindFirstFile(mTempRpmFolder, out pNextInfo);

                System.IO.FileStream fileStream = File.Open(targetFilePath + ".nxl", System.IO.FileMode.Open);
                fileStream.Close();

                ThreeDViewer threeDViewer = new ThreeDViewer(targetFilePath);
                threeDViewer.Watermark(nxlFile.WatermarkInfo);
                mViewModel.Viewer = new Frame
                {
                    Content = threeDViewer
                };
             
                toolbarPage.Sensor.OnClickPrint += threeDViewer.Print;
                threeDViewer.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    toolbarPage.SetParentWindow(null);
                    toolbarPage.Sensor.OnClickPrint -= threeDViewer.Print;
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                };

                threeDViewer.Sensor.OnLoadFileSucceed += delegate ()
                {
                    toolbarPage.SetParentWindow(mViewModel.Window);
                    mViewModel.SendViewLog(true);
                };

                threeDViewer.Sensor.EndPrint += delegate (bool result) 
                {
                    if (result)
                    {
                        mViewModel.SendPrintLog(true);
                    }
                    else
                    {
                        mViewModel.SendPrintLog(false);
                    }
                };
            }
            catch (Exception ex)
            {
                MessageNotify.ShowBalloonTip(mApplication.SdkSession, mApplication.FindResource("Common_System_Internal_Error").ToString(), false);
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
            }
        }

        private void LoadPdfFile2(INxlFile nxlFile) {
            try
            {
                mApplication.Log.Info("\t\t LoadPdfFile. \r\n");
                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);
                if (FileUtils.Is3DPdf(rpmFilePath))
                {
                    mApplication.Log.Info("\t\t is 3D Pdf file. \r\n");
                    Load3DFile(nxlFile);
                }
                else
                {
                    mApplication.Log.Info("\t\t is 2D Pdf file. \r\n");
                    ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                    mViewModel.Toolbar = new Frame()
                    {
                        Content = toolbarPage
                    };

                    var process = Process.GetCurrentProcess();
                    string fullPath = process.MainModule.FileName;
                    string appDir = Path.GetDirectoryName(fullPath);
                    string edgeWebView2FullPath = appDir + @"\Microsoft.WebView2.FixedVersionRuntime.104.0.1293.47.x64\msedgewebview2.exe";
                    if (!mApplication.SdkSession.RPM_AddTrustedApp(edgeWebView2FullPath))
                    {
                        mApplication.Log.Info("\t\t Failed to set edgeWebView2 as trusted application. \r\n");
                    }

                    Viewer.upgrade.ui.common.edgeWebView2Page.view.EdgeWebView2Page edgeWebView2Page = new Viewer.upgrade.ui.common.edgeWebView2Page.view.EdgeWebView2Page(rpmFilePath);
                    edgeWebView2Page.Watermark(nxlFile.WatermarkInfo);
                    edgeWebView2Page.NxlFile = nxlFile;

                    mViewModel.Viewer = new Frame
                    {
                        Content = edgeWebView2Page
                    };

                    toolbarPage.Sensor.OnClickEdit += delegate ()
                    {
                        mViewModel.Window.Hide();
                        toolbarPage.SetParentWindow(null);
                    };

                    toolbarPage.Sensor.OnClickPrint += edgeWebView2Page.Print;

                    edgeWebView2Page.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                    {
                        toolbarPage.Sensor.OnClickPrint -= edgeWebView2Page.Print;
                        toolbarPage.SetParentWindow(null);
                        mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                        mViewModel.SendViewLog(false);
                        mApplication.Log.Error(ex.Message, ex);
                    };

                    edgeWebView2Page.Sensor.OnLoadFileSucceed += delegate ()
                    {
                        toolbarPage.SetParentWindow(mViewModel.Window);
                        mViewModel.SendViewLog(true);
                    };
                }
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
                mApplication.Log.Error(ex.Message, ex);
            }
        }

        private void LoadPdfFile(INxlFile nxlFile)
        {
            try
            {
                mApplication.Log.Info("\t\t LoadPdfFile. \r\n");
                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);
                if (FileUtils.Is3DPdf(rpmFilePath))
                {
                    mApplication.Log.Info("\t\t is 3D Pdf file. \r\n");
                    Load3DFile(nxlFile);
                }
                else
                {
                    mApplication.Log.Info("\t\t is 2D Pdf file. \r\n");
                    ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                    mViewModel.Toolbar = new Frame()
                    {
                        Content = toolbarPage
                    };

                        PreviewHandler previewHandler = new PreviewHandler(nxlFile.Extention);
                        RegisterProcessUtils.ProcessRegister(mApplication.SdkSession, nxlFile.Extention);
                        Viewer.upgrade.ui.common.previewer2.view.PreviewerPage previewerPage = new Viewer.upgrade.ui.common.previewer2.view.PreviewerPage(
                                rpmFilePath,
                                previewHandler);

                        previewerPage.WatermarkInfo = nxlFile.WatermarkInfo;
                        previewerPage.Sensor.OnOverlayWindowLoaded += toolbarPage.SetParentWindow;
                        previewerPage.NxlFile = nxlFile;

                        if (EnumFileType.FILE_TYPE_PDF == nxlFile.FileType)
                        {
                            previewerPage.OverlayOffsetsBottom = 35;
                        }

                        mViewModel.Viewer = new Frame
                        {
                            Content = previewerPage
                        };

                        toolbarPage.Sensor.OnClickEdit += delegate ()
                        {
                            mViewModel.Window.Hide();
                            toolbarPage.SetParentWindow(null);
                        };

                        toolbarPage.Sensor.OnClickPrint += previewerPage.Print;
                        previewerPage.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                        {
                            previewerPage.Sensor.OnOverlayWindowLoaded -= toolbarPage.SetParentWindow;
                            toolbarPage.Sensor.OnClickPrint -= previewerPage.Print;
                            toolbarPage.SetParentWindow(null);
                            mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                            mViewModel.SendViewLog(false);
                            mApplication.Log.Error(ex.Message,ex);
                        };

                        previewerPage.Sensor.OnLoadFileSucceed += delegate ()
                        {
                            mViewModel.SendViewLog(true);
                        };
                }
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
                mApplication.Log.Error(ex.Message , ex);
            }
        }

        private void Load3DFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };
                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);
                ThreeDViewer threeDViewer = new ThreeDViewer(rpmFilePath);
                threeDViewer.Watermark(nxlFile.WatermarkInfo);
                mViewModel.Viewer = new Frame
                {
                    Content = threeDViewer
                };
             
                toolbarPage.Sensor.OnClickPrint += threeDViewer.Print;
                threeDViewer.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    toolbarPage.SetParentWindow(null);
                    toolbarPage.Sensor.OnClickPrint -= threeDViewer.Print;
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                    mApplication.Log.Error(ex.Message,ex);
                };

                threeDViewer.Sensor.OnLoadFileSucceed += delegate ()
                {
                    toolbarPage.SetParentWindow(mViewModel.Window);
                    mViewModel.SendViewLog(true);
                };

                threeDViewer.Sensor.BeforePrint += delegate (System.Windows.Forms.PrintDialog printDialog)
                {
                    printDialog.Document.DocumentName += ":" + nxlFile.FilePath;
                };

                threeDViewer.Sensor.EndPrint += delegate (bool result)
                {
                    if (result)
                    {
                        mViewModel.SendPrintLog(true);
                    }
                    else
                    {
                        mViewModel.SendPrintLog(false);
                    }
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }


        private void LoadEmailFile(INxlFile nxlFile)
        {
            try
            {
                ToolBarPage toolbarPage = new ToolBarPage(nxlFile);
                mViewModel.Toolbar = new Frame()
                {
                    Content = toolbarPage
                };
                string rpmFilePath = nxlFile.Decrypt(Guid.NewGuid().ToString() + nxlFile.Extention);
                EmailPage emailViewer = new EmailPage(rpmFilePath);
                emailViewer.Watermark(nxlFile.WatermarkInfo);
                mViewModel.Viewer = new Frame
                {
                    Content = emailViewer
                };

                toolbarPage.Sensor.OnClickPrint += emailViewer.Print;
                emailViewer.Sensor.OnUnhandledExceptionOccurrence += delegate (Exception ex)
                {
                    toolbarPage.SetParentWindow(null);
                    toolbarPage.Sensor.OnClickPrint -= emailViewer.Print;
                    mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                    mViewModel.SendViewLog(false);
                    mApplication.Log.Error(ex.Message, ex);
                };

                emailViewer.Sensor.OnLoadFileSucceed += delegate ()
                {
                    toolbarPage.SetParentWindow(mViewModel.Window);
                    mViewModel.SendViewLog(true);
                };

                emailViewer.Sensor.BeforePrint += delegate (System.Windows.Forms.PrintDialog printDialog)
                {
                    printDialog.Document.DocumentName += ":" + nxlFile.FilePath;
                };

                emailViewer.Sensor.EndPrint += delegate (bool result)
                {
                    if (result)
                    {
                        mViewModel.SendPrintLog(true);
                    }
                    else
                    {
                        mViewModel.SendPrintLog(false);
                    }
                };
            }
            catch (Exception ex)
            {
                mViewModel.LoadErrorPage(nxlFile.FileName, mApplication.FindResource("Common_System_Internal_Error").ToString());
                mViewModel.SendViewLog(false);
            }
        }

    }
}
