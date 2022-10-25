using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.render;

namespace Viewer.utils
{
    public delegate void OnPdfAnalyzerComplete(bool bIs3d);

    public class PdfAnalyzer
    {
        private BackgroundWorker analyzerWorker = new BackgroundWorker();
        private OnPdfAnalyzerComplete callback;
        private string path;
        private bool is3dPdf = false;

        public PdfAnalyzer(string filePath,log4net.ILog log)
        {
            log.Info("\t\t PdfAnalyzer \r\n");
            this.path = filePath;
            // init convert background worker
            analyzerWorker.WorkerReportsProgress = true;
            analyzerWorker.WorkerSupportsCancellation = true;

            // register
            analyzerWorker.DoWork += RunConvertWorker;
            analyzerWorker.RunWorkerCompleted += ConvertWorkerCompleted;
        }

        public void Analyzer(OnPdfAnalyzerComplete callback)
        {
            this.callback = callback;

            // run
            if (!analyzerWorker.IsBusy)
            {
                analyzerWorker.RunWorkerAsync();
            }
        }


        private void RunConvertWorker(object sender, DoWorkEventArgs args)
        {
            try
            {
                is3dPdf = RenderHelper.Is3DPdf(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "3D pdf PdfAnalyzer failed.");
            }
        }

        private void ConvertWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            callback?.Invoke(is3dPdf);
        }

    }


}
