using Microsoft.Office.Interop.PowerPoint;
using Microsoft.Office.Interop.Word;
using Print.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Print
{
    public class OfficeToXPS
    {
        #region Properties & Constants  
        private static List<string> wordExtensions = new List<string>
        {
            ".doc",
            ".docx",
            ".dot",
            ".dotx",
            ".rtf",
            ".vsd",
            ".vsdx"
        };


        private static List<string> excelExtensions = new List<string>
        {
            ".xls",
            ".xlsx",
            ".xlt",
            ".xltx",
            ".xlsb"
        };

        private static List<string> powerpointExtensions = new List<string>
        {
            ".ppt",
            ".pptx",
            ".ppsx",
            ".potx",
        };

        #endregion

        #region Public Methods  
        public static OfficeToXpsConversionResult ConvertToXps(string sourceFilePath, ref string resultFilePath)
        {
            var result = new OfficeToXpsConversionResult(ConversionResult.UnexpectedError);

            // Check to see if it's a valid file  
            if (!IsValidFilePath(sourceFilePath))
            {
                result.Result = ConversionResult.InvalidFilePath;
                result.ResultText = sourceFilePath;
                return result;
            }

            var ext = Path.GetExtension(sourceFilePath).ToLower();

            // Check to see if it's in our list of convertable extensions  
            if (!IsConvertableFilePath(sourceFilePath))
            {
                result.Result = ConversionResult.InvalidFileExtension;
                result.ResultText = ext;
                return result;
            }

            // Convert if Word  
            if (wordExtensions.Contains(ext))
            {
                return ConvertFromWord(sourceFilePath, ref resultFilePath);
            }

            // Convert if Excel  
            if (excelExtensions.Contains(ext))
            {
                return ConvertFromExcel(sourceFilePath, ref resultFilePath);
            }

            // Convert if PowerPoint  
            if (powerpointExtensions.Contains(ext))
            {
                return ConvertFromPowerPoint(sourceFilePath, ref resultFilePath);
            }

            return result;
        }
        #endregion

        #region Private Methods  
        public static bool IsValidFilePath(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
                return false;

            try
            {
                return File.Exists(sourceFilePath);
            }
            catch (Exception)
            {
            }

            return false;
        }

        public static bool IsConvertableFilePath(string sourceFilePath)
        {
            var ext = Path.GetExtension(sourceFilePath).ToLower();

            return IsConvertableExtension(ext);
        }

        public static bool IsConvertableExtension(string extension)
        {
            return wordExtensions.Contains(extension) ||
                   excelExtensions.Contains(extension) ||
                   powerpointExtensions.Contains(extension);
        }

        private static string GetTempXpsFilePath()
        {
            return Path.ChangeExtension(Path.GetTempFileName(), ".xps");
        }

        private static OfficeToXpsConversionResult ConvertFromWord(string sourceFilePath, ref string resultFilePath)
        {
            object pSourceDocPath = sourceFilePath;
            string pExportFilePath = string.IsNullOrEmpty(resultFilePath) ? GetTempXpsFilePath() : resultFilePath;
            var pExportFormat = Microsoft.Office.Interop.Word.WdExportFormat.wdExportFormatXPS;
            bool pOpenAfterExport = false;
            var pExportOptimizeFor = Microsoft.Office.Interop.Word.WdExportOptimizeFor.wdExportOptimizeForOnScreen;
            var pExportRange = Microsoft.Office.Interop.Word.WdExportRange.wdExportAllDocument;
            int pStartPage = 0;
            int pEndPage = 0;
            var pExportItem = Microsoft.Office.Interop.Word.WdExportItem.wdExportDocumentContent;
            var pIncludeDocProps = true;
            var pKeepIRM = true;
            var pCreateBookmarks = Microsoft.Office.Interop.Word.WdExportCreateBookmarks.wdExportCreateWordBookmarks;
            var pDocStructureTags = true;
            var pBitmapMissingFonts = true;
            var pUseISO19005_1 = false;

            Microsoft.Office.Interop.Word.Application wordApplication = null;
            Microsoft.Office.Interop.Word.Document wordDocument = null;
            try
            {
                wordApplication = new Microsoft.Office.Interop.Word.Application();
                wordApplication.DisplayAlerts = WdAlertLevel.wdAlertsNone;
                //==================================================================
                List<RegisterInfo> registerInfos = CommonUtils.GetAllWinwordeProcess();
                foreach (RegisterInfo registerInfo in registerInfos)
                {
                    CommonUtils.Register(registerInfo);
                }
                //==================================================================

                object paramMissing = Type.Missing;
                bool readOnly = true;
                bool addToRecentFiles = false;
                bool visible = false;
                bool revert = true;

                wordDocument = wordApplication.Documents.Open(ref pSourceDocPath,
                    paramMissing,
                    readOnly,
                    addToRecentFiles,
                    paramMissing,
                    paramMissing,
                    revert,
                    paramMissing,
                    paramMissing,
                    paramMissing,
                    paramMissing,
                    visible,
                    paramMissing,
                    paramMissing,
                    paramMissing,
                    paramMissing);

                if (wordDocument != null)
                {
                        wordDocument.ExportAsFixedFormat(
                                            pExportFilePath,
                                            pExportFormat,
                                            pOpenAfterExport,
                                            pExportOptimizeFor,
                                            pExportRange,
                                            pStartPage,
                                            pEndPage,
                                            pExportItem,
                                            pIncludeDocProps,
                                            pKeepIRM,
                                            pCreateBookmarks,
                                            pDocStructureTags,
                                            pBitmapMissingFonts,
                                            pUseISO19005_1
                                        );

                        resultFilePath = pExportFilePath;
                        return new OfficeToXpsConversionResult(ConversionResult.OK, pExportFilePath);
                }
                else
                {
                    return new OfficeToXpsConversionResult(ConversionResult.ErrorUnableToOpenOfficeFile);
                }
            }
            catch (Exception ex)
            {
                return new OfficeToXpsConversionResult(ConversionResult.UnexpectedError, "Word", ex);
            }
            finally
            {
                // Close and release the Document object.  
                if (wordDocument != null)
                {
                    wordDocument.Close(WdSaveOptions.wdDoNotSaveChanges, WdOriginalFormat.wdOriginalDocumentFormat);
                    wordDocument = null;
                }

                // Quit Word and release the ApplicationClass object.  
                if (wordApplication != null)
                {
                    wordApplication.Quit(WdSaveOptions.wdDoNotSaveChanges, WdOriginalFormat.wdOriginalDocumentFormat);
                    wordApplication = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static OfficeToXpsConversionResult ConvertFromPowerPoint(string sourceFilePath, ref string resultFilePath)
        {
            string pSourceDocPath = sourceFilePath;
            string pExportFilePath = string.IsNullOrEmpty(resultFilePath) ? GetTempXpsFilePath() : resultFilePath;
            Microsoft.Office.Interop.PowerPoint.Application pptApplication = null;
            Microsoft.Office.Interop.PowerPoint.Presentation pptPresentation = null;
            try
            {
                pptApplication = new Microsoft.Office.Interop.PowerPoint.Application();
                pptApplication.DisplayAlerts = PpAlertLevel.ppAlertsNone;

                //int processId = Int32.MaxValue;
                //try
                //{
                //    processId = GetWindowThreadProcessId(pptApplication.HWND, out processId);
                //    if (processId == Int32.MaxValue)
                //    {
                //        return new OfficeToXpsConversionResult(ConversionResult.ErrorUnableToOpenOfficeFile, "PowerPoint", new Exception());
                //    }

                //    if(RegisterProcess.Register(new RegisterInfo(processId, false)))
                //    {
                //        return new OfficeToXpsConversionResult(ConversionResult.ErrorUnableToOpenOfficeFile, "PowerPoint", new Exception());
                //    }
                //}
                //catch (Exception exc)
                //{
                //    return new OfficeToXpsConversionResult(ConversionResult.ErrorUnableToOpenOfficeFile, "PowerPoint", exc);
                //}
                  
                // pptApplication.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
                pptPresentation = pptApplication.Presentations.Open(pSourceDocPath,
                                                                    Microsoft.Office.Core.MsoTriState.msoTrue,
                                                                    Microsoft.Office.Core.MsoTriState.msoTrue,
                                                                    Microsoft.Office.Core.MsoTriState.msoFalse);

                if (pptPresentation != null)
                {
                       
                        pptPresentation.ExportAsFixedFormat(
                                            pExportFilePath,
                                            Microsoft.Office.Interop.PowerPoint.PpFixedFormatType.ppFixedFormatTypeXPS,
                                            Microsoft.Office.Interop.PowerPoint.PpFixedFormatIntent.ppFixedFormatIntentScreen,
                                            Microsoft.Office.Core.MsoTriState.msoFalse,
                                            Microsoft.Office.Interop.PowerPoint.PpPrintHandoutOrder.ppPrintHandoutVerticalFirst,
                                            Microsoft.Office.Interop.PowerPoint.PpPrintOutputType.ppPrintOutputSlides,
                                            Microsoft.Office.Core.MsoTriState.msoFalse,
                                            null,
                                            Microsoft.Office.Interop.PowerPoint.PpPrintRangeType.ppPrintAll,
                                            string.Empty,
                                            true,
                                            true,
                                            true,
                                            true,
                                            false
                                        );

                        resultFilePath = pExportFilePath;
                        return new OfficeToXpsConversionResult(ConversionResult.OK, pExportFilePath);
                }
                else
                {
                    return new OfficeToXpsConversionResult(ConversionResult.ErrorUnableToOpenOfficeFile);
                }
            }
            catch (Exception ex)
            {
                return new OfficeToXpsConversionResult(ConversionResult.UnexpectedError, "PowerPoint", ex);
            }
            finally
            {
                // Close and release the Document object.  
                if (pptPresentation != null)
                {
                    pptPresentation.Close();
                    pptPresentation = null;
                }


                if (pptApplication != null)
                {
                    //fix bug 52846
                    // pptApplication.Quit();
                    pptApplication = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static OfficeToXpsConversionResult ConvertFromExcel(string sourceFilePath, ref string resultFilePath)
        {
            string pSourceDocPath = sourceFilePath;
            string pExportFilePath = string.IsNullOrEmpty(resultFilePath) ? GetTempXpsFilePath() : resultFilePath;
            var pExportFormat = Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypeXPS;
            var pExportQuality = Microsoft.Office.Interop.Excel.XlFixedFormatQuality.xlQualityStandard;
            var pOpenAfterPublish = false;
            var pIncludeDocProps = true;
            var pIgnorePrintAreas = true;
            Microsoft.Office.Interop.Excel.Application excelApplication = null;
            Microsoft.Office.Interop.Excel.Workbook excelWorkbook = null;

            try
            {
                excelApplication = new Microsoft.Office.Interop.Excel.Application();
                excelApplication.DisplayAlerts = false;
                excelApplication.EnableEvents = false;

                //=====================================================
                uint processId = 0;
                uint threadID = GetWindowThreadProcessId(new IntPtr(excelApplication.Hwnd), out processId);
                if (processId == 0)
                {
                    return new OfficeToXpsConversionResult(ConversionResult.ErrorUnableToOpenOfficeFile, "Excel", new Exception());
                }

                if (!CommonUtils.Register(new RegisterInfo(Convert.ToInt32(processId), false)))
                {
                    return new OfficeToXpsConversionResult(ConversionResult.ErrorUnableToOpenOfficeFile, "Excel", new Exception());
                }
               //=====================================================
                      
                    object paramMissing = Type.Missing;
                    bool readOnly = true;
                    bool ignoreReadOnlyRecommended = true;
                    bool editable = false;
                    bool notify = false;
                    bool addToMru = false;

                    excelWorkbook = excelApplication.Workbooks.Open(pSourceDocPath,
                        paramMissing,
                        readOnly,
                        paramMissing,
                        paramMissing,
                        paramMissing,
                        ignoreReadOnlyRecommended,
                        paramMissing,
                        paramMissing,
                        editable,
                        notify,
                        paramMissing,
                        addToMru,
                        paramMissing,
                        paramMissing
                        );

                if (excelWorkbook != null)
                {
                        excelWorkbook.ExportAsFixedFormat(
                                            pExportFormat,
                                            pExportFilePath,
                                            pExportQuality,
                                            pIncludeDocProps,
                                            pIgnorePrintAreas,

                                            OpenAfterPublish: pOpenAfterPublish
                                        );

                        resultFilePath = pExportFilePath;
                        return new OfficeToXpsConversionResult(ConversionResult.OK, pExportFilePath);
                }
                else
                {
                    return new OfficeToXpsConversionResult(ConversionResult.ErrorUnableToOpenOfficeFile);
                }
            }
            catch (Exception ex)
            {
                return new OfficeToXpsConversionResult(ConversionResult.UnexpectedError, "Excel",ex);
            }
            finally
            {
                // Close and release the Document object.  
                if (excelWorkbook != null)
                {
                    excelWorkbook.Close(false);
                    excelWorkbook = null;
                }

                if (excelApplication != null)
                {
                    excelApplication.Quit();
                    excelApplication = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        #endregion

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }

    public class OfficeToXpsConversionResult
    {
        #region Properties  
        public ConversionResult Result { get; set; }
        public string ResultText { get; set; }
        public Exception ResultError { get; set; }
        #endregion

        #region Constructors  
        public OfficeToXpsConversionResult()
        {
            Result = ConversionResult.UnexpectedError;
            ResultText = string.Empty;
        }
        public OfficeToXpsConversionResult(ConversionResult result)
            : this()
        {
            Result = result;
        }
        public OfficeToXpsConversionResult(ConversionResult result, string resultText)
            : this(result)
        {
            ResultText = resultText;
        }
        public OfficeToXpsConversionResult(ConversionResult result, string resultText, Exception exc)
            : this(result, resultText)
        {
            ResultError = exc;
        }
        #endregion
    }

    public enum ConversionResult
    {
        OK = 0,
        InvalidFilePath = 1,
        InvalidFileExtension = 2,
        UnexpectedError = 3,
        ErrorUnableToInitializeOfficeApp = 4,
        ErrorUnableToOpenOfficeFile = 5,
        ErrorUnableToAccessOfficeInterop = 6,
        ErrorUnableToExportToXps = 7
    }


}

