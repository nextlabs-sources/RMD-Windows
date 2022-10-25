#define USING_EXCHANGE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Viewer.render.hoops.ThreeDView;
using Viewer.render.hoops.ThreeDViewer;
using Viewer.utils;

namespace Viewer.hoops.Commands
{

    /// <summary>
    /// File loading command handler --- Now only for test.
    /// </summary>
    class FileOpenCommand : BaseCommand
    {
        private ViewerWindow viewWin;
        public ViewerWindow ViewWin
        {
            get
            {
                return viewWin;
            }

            set
            {
                viewWin = value;
            }
        }

        public FileOpenCommand(ThreeDViewer viewPage) : base(viewPage) { }

        // File load delegate for loading file on separate thread.
        private delegate void FileLoadDelegate(string fileName, SprocketsWPFControl control);

        public override void Execute(object parameter)
        {
            //            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //            string dialog_filter = "HSF File (.hsf)|*.hsf|StereoLithography File (.stl)|*.stl|Wavefront File (.obj)|*.obj|Point Cloud Files (*.ptx, *.pts, *.xyz)|*.ptx;*.pts;*.xyz|";

            //#if USING_EXCHANGE
            //                        dialog_filter += "All CAD Files (*.3ds, *.3dxml, *.sat, *.sab, *_pd, *.model, *.dlv, *.exp, *.session, *.CATPart, *.CATProduct, *.CATShape, *.CATDrawing" +
            //                         ", *.cgr, *.dae, *.prt, *.prt.*, *.neu, *.neu.*, *.asm, *.asm.*, *.xas, *.xpr, *.arc, *.unv, *.mf1, *.prt, *.pkg, *.ifc, *.ifczip, *.igs, *.iges, *.ipt, *.iam" +
            //                         ", *.jt, *.kmz, *.prt, *.pdf, *.prc, *.x_t, *.xmt, *.x_b, *.xmt_txt, *.3dm, *.stp, *.step, *.stpz, *.stp.z, *.stl, *.par, *.asm, *.pwd, *.psm" +
            //                         ", *.sldprt, *.sldasm, *.sldfpp, *.asm, *.u3d, *.vda, *.wrl, *.vml, *.xv3, *.xv0)|" +
            //                         "*.3ds;*.3dxml;*.sat;*.sab;*_pd;*.model;*.dlv;*.exp;*.session;*.catpart;*.catproduct;*.catshape;*.catdrawing" +
            //                         ";*.cgr;*.dae;*.prt;*.prt.*;*.neu;*.neu.*;*.asm;*.asm.*;*.xas;*.xpr;*.arc;*.unv;*.mf1;*.prt;*.pkg;*.ifc;*.ifczip;*.igs;*.iges;*.ipt;*.iam" +
            //                         ";*.jt;*.kmz;*.prt;*.pdf;*.prc;*.x_t;*.xmt;*.x_b;*.xmt_txt;*.3dm;*.stp;*.step;*.stpz;*.stp.z;*.stl;*.par;*.asm;*.pwd;*.psm" +
            //                         ";*.sldprt;*.sldasm;*.sldfpp;*.asm;*.u3d;*.vda;*.wrl;*.vml;*.obj;*.xv3;*.xv0;*.hsf|" +
            //                         "3D Studio Files (*.3ds)|*.3ds|" +
            //                         "3DXML Files (*.3dxml)|*.3dxml|" +
            //                         "ACIS SAT Files (*.sat, *.sab)|*.sat;*.sab|" +
            //                         "CADDS Files (*_pd)|*_pd|" +
            //                         "CATIA V4 Files (*.model, *.dlv, *.exp, *.session)|*.model;*.dlv;*.exp;*.session|" +
            //                         "CATIA V5 Files (*.CATPart, *.CATProduct, *.CATShape, *.CATDrawing)|*.catpart;*.catproduct;*.catshape;*.catdrawing|" +
            //                         "CGR Files (*.cgr)|*.cgr|" +
            //                         "Collada Files (*.dae)|*.dae|" +
            //                         "Creo (ProE) Files (*.prt, *.prt.*, *.neu, *.neu.*, *.asm, *.asm.*, *.xas, *.xpr)|*.prt;*.prt.*;*.neu;*.neu.*;*.asm;*.asm.*;*.xas;*.xpr|" +
            //                         "I-DEAS Files (*.arc, *.unv, *.mf1, *.prt, *.pkg)|*.arc;*.unv;*.mf1;*.prt;*.pkg|" +
            //                         "IFC Files (*.ifc, *.ifczip)|*.ifc;*.ifczip|" +
            //                         "IGES Files (*.igs, *.iges)|*.igs;*.iges|" +
            //                         "Inventor Files (*.ipt, *.iam)|*.ipt;*.iam|" +
            //                         "JT Files (*.jt)|*.jt|" +
            //                         "KMZ Files (*.kmz)|*.kmz|" +
            //                         "NX (Unigraphics) Files (*.prt)|*.prt|" +
            //                         "PDF Files (*.pdf)|*.pdf|" +
            //                         "PRC Files (*.prc)|*.prc|" +
            //                         "Parasolid Files (*.x_t, *.xmt, *.x_b, *.xmt_txt)|*.x_t;*.xmt;*.x_b;*.xmt_txt|" +
            //                         "Rhino Files (*.3dm)|*.3dm|" +
            //                         "STEP Files (*.stp, *.step, *.stpz, *.stp.z)|*.stp;*.step;*.stpz;*.stp.z|" +
            //                         "STL Files (*.stl)|*.stl|" +
            //                         "SolidEdge Files (*.par, *.asm, *.pwd, *.psm)|*.par;*.asm;*.pwd;*.psm|" +
            //                         "SolidWorks Files (*.sldprt, *.sldasm, *.sldfpp, *.asm)|*.sldprt;*.sldasm;*.sldfpp;*.asm|" +
            //                         "Universal 3D Files (*.u3d)|*.u3d|" +
            //                         "VDA Files (*.vda)|*.vda|" +
            //                         "VRML Files (*.wrl, *.vrml)|*.wrl;*.vrml|" +
            //                         "XVL Files (*.xv3, *.xv0)|*.xv0;*.xv3|";
            //#endif

            //#if USING_PARASOLID
            //             dialog_filter += "Parasolid File (*.x_t, *.x_b, *.xmt_txt, *.xmt_bin)|*.x_t;*.x_b;*.xmt_txt;*.xmt_bin|";
            //#endif

            //#if USING_DWG
            //#if !DEBUG
            //#if (VS2012 || VC2015)
            //             dialog_filter += "DWG File (*.dwg, *.dxf)|*.dwg;*.dxf|";
            //#endif
            //#endif
            //#endif
            //            dialog_filter += "All Files (*.*)|*.*";
            //            dlg.Filter = dialog_filter;
            //            dlg.DefaultExt = ".hsf";
            //            //dlg.InitialDirectory = Path.GetFullPath(_win.samplesDir);
            //            Nullable<bool> result = dlg.ShowDialog();
            //            if (result == true)
            //            {
            //                OpenDatasetFile(dlg.FileName);
            //            }


            OpenDatasetFile(parameter.ToString());
        }

        /// <summary>
        /// Loads dataset model and attaches to main view
        /// </summary>
        private void OpenDatasetFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            FileLoadDelegate load;
            load = new FileLoadDelegate(ThreadedFileLoad);

            // Perform asynchronous file load on a separate thread.
            load.BeginInvoke(filePath, _win.GetSprocketsControl(), null, null);
        }

        /// <summary>
        /// Method for loading the model on a separate thread.
        /// </summary>
        public void ThreadedFileLoad(string fileName, SprocketsWPFControl control)
        {
            _win.CreateNewModel();

            // perform file load
            bool loaded = false;
            string ext = Path.GetExtension(fileName);

            HPS.Stream.ImportResultsKit importResults = null;
            if (ext == ".hsf")
            {
                loaded = ImportHSFFile(fileName, ref importResults);
            }
            else if (ext == ".stl")
            {
                loaded = ImportSTLFile(fileName);
            }
            else if (ext == ".obj") 
            {
                loaded = ImportOBJFile(fileName);
            }

#if USING_PARASOLID
            else if (ext == ".x_t" || ext == ".xmt_txt" || ext == ".x_b" || ext == ".xmt_bin")
                loaded = ImportParasolidFile(filename, null);
#endif
#if USING_DWG
#if !DEBUG
#if (VS2012 || VS2015)
            else if (ext == ".dwg" || ext == ".dxf")
                loaded = ImportDWGFile(filename, null);
#endif
#endif
#endif
#if USING_EXCHANGE
            else
                loaded = ImportExchangeFile(fileName, null);
#endif
            InvokeUIAction(delegate ()
            {
                // make sure the segment browser has a valid root
                //_win.SegmentBrowserRootCommand.Execute(null);
                //_win.ConfigurationBrowser.Init();

                if (loaded)
                {
                    if (_win.CADModel == null)
                    {
                        HPS.CameraKit defaultCamera = null;
                        if (importResults == null || !importResults.ShowDefaultCamera(out defaultCamera))
                            control.Canvas.GetFrontView().ComputeFitWorldCamera(out defaultCamera);
                        _win.DefaultCamera = defaultCamera;
                    }

                    // Set new Window title
                   // _win.SetTitle("- " + Path.GetFileName(fileName));

                    //AddToRecentFiles(fileName);
                }

                _win.GetSprocketsControl().IsEnabled = true;
              //  _win.AttachOverlay();
            }, false);

        }

        private bool ImportHSFFile(string filename, ref HPS.Stream.ImportResultsKit importResults)
        {
            HPS.Stream.ImportOptionsKit importOptions = new HPS.Stream.ImportOptionsKit();
            
            importOptions.SetSegment(_win.Model.GetSegmentKey());
            importOptions.SetAlternateRoot(_win.Model.GetLibraryKey());
            importOptions.SetPortfolio(_win.Model.GetPortfolioKey());

            HPS.Stream.ImportNotifier importNotifier = new HPS.Stream.ImportNotifier();
            HPS.IOResult status;
            string exceptionMessage = "";
            try
            {
                importNotifier = HPS.Stream.File.Import(filename, importOptions);
                
                DisplayImportProgress(importNotifier);
                status = importNotifier.Status();

                if (status == HPS.IOResult.Success)
                    importResults = importNotifier.GetResults();
            }
            catch (HPS.IOException ex)
            {
                exceptionMessage = ex.Message;
                status = ex.result;
            }

            if (status != HPS.IOResult.Success && status != HPS.IOResult.Canceled)
                MessageBox.Show(GetErrorString(status, filename, exceptionMessage), CultureStringInfo.View_DlgBox_Title);

            return status == HPS.IOResult.Success;
        }

        private bool ImportSTLFile(string filename)
        {
            HPS.STL.ImportOptionsKit importOptions = HPS.STL.ImportOptionsKit.GetDefault();
            importOptions.SetSegment(_win.Model.GetSegmentKey());

            HPS.STL.ImportNotifier importNotifier = new HPS.STL.ImportNotifier();
            HPS.IOResult status;
            string exceptionMessage = "";
            try
            {
                importNotifier = HPS.STL.File.Import(filename, importOptions);
                DisplayImportProgress(importNotifier);
                status = importNotifier.Status();
            }
            catch (HPS.IOException ex)
            {
                exceptionMessage = ex.Message;
                status = ex.result;
            }

            if (status != HPS.IOResult.Success)
                MessageBox.Show(GetErrorString(status, filename, exceptionMessage), CultureStringInfo.View_DlgBox_Title);

            return status == HPS.IOResult.Success;
        }

        private bool ImportOBJFile(string filename)
        {
            HPS.OBJ.ImportOptionsKit importOptions = new HPS.OBJ.ImportOptionsKit();
            importOptions.SetSegment(_win.Model.GetSegmentKey());
            importOptions.SetPortfolio(_win.Model.GetPortfolioKey());

            HPS.OBJ.ImportNotifier importNotifier = new HPS.OBJ.ImportNotifier();
            HPS.IOResult status;
            string exceptionMessage = "";
            try
            {
                importNotifier = HPS.OBJ.File.Import(filename, importOptions);
                DisplayImportProgress(importNotifier);
                status = importNotifier.Status();
            }
            catch (HPS.IOException ex)
            {
                exceptionMessage = ex.Message;
                status = ex.result;
            }

            if (status != HPS.IOResult.Success)
                MessageBox.Show(GetErrorString(status, filename, exceptionMessage), CultureStringInfo.View_DlgBox_Title);

            return status == HPS.IOResult.Success;
        }

#if USING_EXCHANGE
        private bool ImportExchangeFile(string filename, HPS.Exchange.ImportOptionsKit importOptions)
        {
            HPS.Exchange.ImportNotifier importNotifier = null;
            HPS.IOResult status;
            string exceptionMessage = "";
            try
            {
                HPS.Exchange.ImportOptionsKit modifiedImportOptions;
                if (importOptions != null)
                    modifiedImportOptions = new HPS.Exchange.ImportOptionsKit(importOptions);
                else
                    modifiedImportOptions = new HPS.Exchange.ImportOptionsKit();
                modifiedImportOptions.SetBRepMode(HPS.Exchange.BRepMode.BRepAndTessellation);

                HPS.Exchange.File.Format format = HPS.Exchange.File.GetFormat(filename);
                string[] configuration;
                HPS.Exchange.Configuration[] allConfigurations;
                if (format == HPS.Exchange.File.Format.CATIAV4
                    && !modifiedImportOptions.ShowConfiguration(out configuration)
                    && (allConfigurations = HPS.Exchange.File.GetConfigurations(filename)).Length > 0)
                {
                    // CATIA V4 files w/ configurations must specify a configuration otherwise nothing will be loaded
                    // So if this file has configurations and no configuration was specified, load the first configuration
                    modifiedImportOptions.SetConfiguration(GetFirstConfiguration(allConfigurations));
                }

                importNotifier = HPS.Exchange.File.Import(filename, modifiedImportOptions);

                bool success = false;

                InvokeUIAction(delegate()
                {
                    //show the progress dialog
                    _win.GetSprocketsControl().IsEnabled = false;
                    var dlg = new ExchangeImportDialog(_win, importNotifier);
                    dlg.Owner = viewWin;
                    dlg.ShowDialog();

                    success = dlg.WasSuccessful();
                }, true);

                status = importNotifier.Status();
            }
            catch (HPS.IOException ex)
            {
                exceptionMessage = ex.Message;
                status = ex.result;
            }

            if (status != HPS.IOResult.Success && status != HPS.IOResult.Canceled)
            {
                if (!File.Exists(filename))
                {
                    MessageBox.Show(string.Format("Failed to copy the protected file {0} to NextLabs secure folder. We can't render this file, and please contact system administrator for further help.",Path.GetFileName(filename)), CultureStringInfo.View_DlgBox_Title);
                }
                else
                {
                    MessageBox.Show(GetErrorString(status, filename, exceptionMessage), CultureStringInfo.View_DlgBox_Title);
                }
            }

            return status == HPS.IOResult.Success;
        }

        private string[] GetFirstConfiguration(HPS.Exchange.Configuration[] configurations)
        {
            if (configurations == null || configurations.Length == 0)
                return null;

            var firstConfiguration = new List<string>();
            firstConfiguration.Add(configurations[0].GetName());
            var subconfiguration = GetFirstConfiguration(configurations[0].GetSubconfigurations());
            if (subconfiguration != null)
                firstConfiguration.AddRange(subconfiguration);
            return firstConfiguration.ToArray();
        }

        //public void ImportConfiguration(string[] configuration)
        //{
        //    if (_win.CADModel != null)
        //    {
        //        string filename = new HPS.StringMetadata(_win.CADModel.GetMetadata("Filename")).GetValue();

        //        _win.CADModel.Delete();
        //        _win.CADModel = null;

        //        var importOptions = new HPS.Exchange.ImportOptionsKit();
        //        importOptions.SetConfiguration(configuration);

        //        ImportExchangeFile(filename, importOptions);


        //        InvokeUIAction(delegate()
        //        {
        //            // make sure the segment browser has a valid root
        //            _win.SegmentBrowserRootCommand.Execute(null);

        //            _win.ModelBrowser.Init();
        //            _win.ConfigurationBrowser.Init();

        //            _win.GetSprocketsControl().IsEnabled = true;
        //        }, false);
        //    }
        //}
#endif

#if USING_PARASOLID
        private bool ImportParasolidFile(string filename, HPS.Parasolid.ImportOptionsKit importOptions)
        {
            HPS.Parasolid.ImportNotifier importNotifier = null;
            HPS.IOResult status;
            string exceptionMessage = "";
            try
            {
                HPS.Parasolid.ImportOptionsKit modifiedImportOptions;
                if (importOptions != null)
                    modifiedImportOptions = new HPS.Parasolid.ImportOptionsKit(importOptions);
                else
                    modifiedImportOptions = new HPS.Parasolid.ImportOptionsKit();
                HPS.Parasolid.Format format;
                if (!modifiedImportOptions.ShowFormat(out format))
                {
                    string ext = System.IO.Path.GetExtension(filename);
                    if (ext == ".x_t" || ext == ".xmt_txt")
                        format = HPS.Parasolid.Format.Text;
                    else // assuming not a neutral binary format
                        format = HPS.Parasolid.Format.Binary;
                    modifiedImportOptions.SetFormat(format);
                }

                importNotifier = HPS.Parasolid.File.Import(filename, modifiedImportOptions);
                DisplayImportProgress(importNotifier);
                status = importNotifier.Status();

                if (status == HPS.IOResult.Success)
                    _win.CADModel = importNotifier.GetCADModel();
            }
            catch (HPS.IOException ex)
            {
                exceptionMessage = ex.Message;
                status = ex.result;
            }

            if (status != HPS.IOResult.Success)
                MessageBox.Show(GetErrorString(status, filename, exceptionMessage));

            return status == HPS.IOResult.Success;
        }
#endif

#if USING_DWG
        private bool ImportDWGFile(string filename, HPS.DWG.ImportOptionsKit importOptions)
        {
            HPS.DWG.ImportNotifier importNotifier = null;
            HPS.IOResult status;
            string exceptionMessage = "";
            try
            {
                HPS.DWG.ImportOptionsKit modifiedImportOptions;
                if (importOptions != null)
                    modifiedImportOptions = new HPS.DWG.ImportOptionsKit(importOptions);
                else
                    modifiedImportOptions = new HPS.DWG.ImportOptionsKit();
                
                importNotifier = HPS.DWG.File.Import(filename, modifiedImportOptions);
                DisplayImportProgress(importNotifier);
                status = importNotifier.Status();

                if (status == HPS.IOResult.Success)
                    _win.CADModel = importNotifier.GetCADModel();
            }
            catch (HPS.IOException ex)
            {
                exceptionMessage = ex.Message;
                status = ex.result;
            }

            if (status != HPS.IOResult.Success)
                MessageBox.Show(GetErrorString(status, filename, exceptionMessage));

            return status == HPS.IOResult.Success;
        }
#endif
        // Now hide the progress
        private bool DisplayImportProgress(HPS.IONotifier importNotifier)
        {
            bool success = false;
            InvokeUIAction(delegate ()
            {
                //show the progress dialog
                _win.GetSprocketsControl().IsEnabled = false;
                var dlg = new ProgressBar(_win, importNotifier);
                dlg.Owner = viewWin;
                dlg.ShowDialog();

                success = dlg.WasSuccessful();
            }, true);

            //return success;

            return true;
        }

        // Now hide the progress
        private bool DisplayImportProgress(ProgressBar dlg)
        {
            bool success = false;
            InvokeUIAction(delegate ()
            {
                _win.GetSprocketsControl().IsEnabled = false;
                dlg.Owner = viewWin;
                dlg.ShowDialog();

                success = dlg.WasSuccessful();
            }, true);

            //return success;
            return true;
        }

        /// <summary>
        /// Helper method to perform the UI actions in main thread.
        /// </summary>
        private void InvokeUIAction(Action action, bool wait)
        {
            if (wait)
            {
                _win.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(action)); // synch
            }
            else
            {
                _win.Dispatcher.BeginInvoke(new Action(action)); // asynch
            }
        }

        private string GetErrorString(HPS.IOResult status, string filename, string exceptionMessage)
        {
            string errorString = "";
            switch (status)
            {
                case HPS.IOResult.FileNotFound:
                    errorString = "Could not locate file " + filename;
                    break;

                case HPS.IOResult.UnableToOpenFile:
                    errorString = "Unable to open file " + filename;
                    break;

                case HPS.IOResult.InvalidOptions:
                    errorString = "Invalid options: " + exceptionMessage;
                    break;

                case HPS.IOResult.InvalidSegment:
                    errorString = "Invalid segment: " + exceptionMessage;
                    break;

                case HPS.IOResult.UnableToLoadLibraries:
                    errorString = "Unable to load libraries: " + exceptionMessage;
                    break;

                case HPS.IOResult.VersionIncompatibility:
                    errorString = "Version incompatability: " + exceptionMessage;
                    break;

                case HPS.IOResult.InitializationFailed:
                    errorString = "Initialization failed: " + exceptionMessage;
                    break;

                case HPS.IOResult.UnsupportedFormat:
                    errorString = "Unsupported format.";
                    break;

                case HPS.IOResult.Canceled:
                    errorString = "IO canceled.";
                    break;

                case HPS.IOResult.Failure:
                default:
                    errorString = "Error loading file " + filename + ":\n\t" + exceptionMessage;
                    break;
            }
            return errorString;
        }

    }
}
