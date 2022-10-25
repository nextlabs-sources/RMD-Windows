using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.hoops;

namespace Viewer.utils
{
    public class ThreeDConverter
    {
        private BackgroundWorker convertWorker = new BackgroundWorker();

        private ThreeDConvertCompleteDelegate callback;
        private string inputPath;
        private string outputPath;

        public ThreeDConverter(string input, string outPath)
        {
            this.inputPath = input;
            this.outputPath = outPath;

            // init convert background worker
            convertWorker.WorkerReportsProgress = true;
            convertWorker.WorkerSupportsCancellation = true;

            // register
            convertWorker.DoWork += RunConvertWorker;
            convertWorker.RunWorkerCompleted += ConvertWorkerCompleted;
        }
        public void Execute3DConvert(ThreeDConvertCompleteDelegate callback)
        {
            this.callback = callback;

            // run
            if (!convertWorker.IsBusy)
            {
                convertWorker.RunWorkerAsync();
            }
        }

        private void RunConvertWorker(object sender, DoWorkEventArgs args)
        {
            ViewerApp.Log.InfoFormat("\t\t Start 3DConvert, inputPath: {0} ,outputPath: {1}\r\n", inputPath, outputPath);
            bool bResult = true;

            System.Diagnostics.Process p = null;

            try
            {
                p = new System.Diagnostics.Process();
                p.StartInfo.FileName = System.IO.Directory.GetCurrentDirectory() + @"\3DConverter\converter.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;  
                p.StartInfo.Arguments = "--input " + inputPath + " --output_hsf " + outputPath + " --license " + HOOPS_LICENSE.CONVERT_KEY;
                p.Start();

                ViewerApp.Log.InfoFormat("\t\t File Path: {0} , WaitForExit Convert \r\n", inputPath);
                p.WaitForExit();
            }
            catch (Exception e)
            {
                ViewerApp.Log.ErrorFormat("\t\t File Path: {0}, Exception Message{1} \r\n",inputPath, e.Message);
                ViewerApp.Log.Error(e.Message,e);
                bResult = false;
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (p != null)
                {
                    p.Close();
                    p.Dispose();
                    p = null;
                }

                if (!File.Exists(outputPath))
                {
                    bResult = false;
                }

                args.Result = bResult;

            }

        }

        private void ConvertWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            callback?.Invoke((bool)args.Result, outputPath);
        }


    }

    public delegate void ThreeDConvertCompleteDelegate(bool bSucceed, string outPath);
}
