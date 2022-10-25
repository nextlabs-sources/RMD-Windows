using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Viewer.extractContent;
using Viewer.utils;
using Viewer.viewer;
using Viewer.viewer.model;
using static Viewer.ViewerWindow;

namespace Viewer.communication
{
    public class CommunicationWithSkydrmLocal
    {
        private static Process SkydrmProcess;

        static CommunicationWithSkydrmLocal()
        {
            SkydrmProcess = CreateProcess("Skydrm.exe");
        }

        public static Process CreateProcess(string exeFileName)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = exeFileName;
            proc.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            proc.StartInfo.CreateNoWindow = true;

            return proc;
        }
  
        public static void SendLog(Log log)
        {
            try
            {               
                if (SkydrmProcess == null)
                {
                    return;
                }
                string data = JsonConvert.SerializeObject(log);
                data = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
                SkydrmProcess.StartInfo.Arguments += "-addLog";
                SkydrmProcess.StartInfo.Arguments += " ";
                SkydrmProcess.StartInfo.Arguments += data;
                SkydrmProcess.Start();
                SkydrmProcess.StartInfo.Arguments = string.Empty;
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }

        public static void FileInfo(NxlConverterResult nxlConverterResult, string ResultJson, IPCManager Ipc, IntPtr HMainWin)
        {
            //ExternFileInfo.Builder EFBuilder = BuildExternFileInfo(nxlConverterResult);
            string passData;

            // set action
            //EFBuilder.SetActionType(ActionType.ViewFileInfo);
            //passData = JsonConvert.SerializeObject(EFBuilder.Build());

            // encapuslate passData into base64
            passData = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(ResultJson));
            StartSkydrmLocalProcess(new IntentParam()
            {
                isShareFile = false,
                isShowFileInfo = true,
                param = passData
            });
        }

        public static void Share(NxlConverterResult nxlConverterResult, string ResultJson, IPCManager Ipc, IntPtr HMainWin)
        {          
            string passData;
     
            // encapuslate passData into base64
            passData = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(nxlConverterResult.LocalDiskPath));
            
            StartSkydrmLocalProcess(new IntentParam()
            {
                isShareFile = true,
                isShowFileInfo = false,
                param = passData
            });          
        }

        public static void Edit(NxlConverterResult nxlConverterResult)
        {
            try
            {
                if (SkydrmProcess == null)
                {
                    return;
                }

                EditInfo editInfo = new EditInfo.Builder()
               .IsNeedUpload(nxlConverterResult.EnumFileRepo == EnumFileRepo.REPO_PROJECT)
               .SetNXlFileLocalPath(nxlConverterResult.LocalDiskPath)
               .Build();

                string json = JsonConvert.SerializeObject(editInfo);

                string data = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

                SkydrmProcess.StartInfo.Arguments += "-edit";
                SkydrmProcess.StartInfo.Arguments += " ";
                SkydrmProcess.StartInfo.Arguments += data;
                SkydrmProcess.Start();
                SkydrmProcess.StartInfo.Arguments = string.Empty;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message.ToString());
            }
        }

        public static void ExtractContent(string nxlfileLocalPath , ViewerWindow viewerWindow)
        {
            string destinationPath = string.Empty;

            if (ExtractContentHelper.ShowSaveAsDialog(out destinationPath, nxlfileLocalPath, viewerWindow))
            {

                ExtractContentParameter extractContentParameter = new ExtractContentParameter
                {
                    NxlFileLocalPath = nxlfileLocalPath,
                    DestinationPath = destinationPath
                };

                string json = JsonConvert.SerializeObject(extractContentParameter);

                string  passData = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                StartSkydrmLocalProcess(new IntentParam()
                {
                    isExtractContent = true,
                    param = passData
                });
            }
        }


        private static bool ShowSaveFileDialog(out string destinationPath, string fileName, ViewerWindow viewerWindow)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog(); 
            dlg.CheckFileExists = false;
            dlg.FileName = fileName; // Default file name
            dlg.DefaultExt = Path.GetExtension(fileName); // .nxl Default file extension
            dlg.Filter = "NextLabs Protected Documents (*.nxl)|*.nxl"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog(viewerWindow);
            // Process save file dialog box results
            if (result == true)
            {
                destinationPath = dlg.FileName;
         
                if (Path.HasExtension(destinationPath))
                {
                    if (!string.Equals(Path.GetExtension(destinationPath), ".nxl", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // destinationPath = destinationPath.Substring(0, destinationPath.LastIndexOf(".")) + ".nxl";
                        destinationPath += ".nxl";
                    }
                }
             
            }
            else
            {
                destinationPath = string.Empty;
            }

            return result.Value;
        }


        private static string ModifyExportedFileNameReplacedWithLatestTimestamp(string fname)
        {
            // like log-2019-01-24-07-04-28.txt
            // pattern-match "-2019-01-24-07-04-28" replaced with latest lcoal timestamp
            string pattern = @"-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}";
            // new stime string
            string newTimeStamp = DateTime.Now.ToLocalTime().ToString("-yyyy-MM-dd-HH-mm-ss");
            Regex r = new Regex(pattern);
            string newName = fname;
            if (r.IsMatch(fname))
            {
                newName = r.Replace(fname, newTimeStamp);
            }
            return newName;
        }

        public static void Export(NxlConverterResult nxlConverterResult,ViewerWindow viewerWindow)
        {
            try
            {
                string destinationPath = string.Empty;
                //fix bug 53134 add new feature, 
                // extract timestamp in target.Name and replaced it as local lastest one
                if (!ShowSaveFileDialog(out destinationPath,
                ModifyExportedFileNameReplacedWithLatestTimestamp(nxlConverterResult.FileName),
                viewerWindow))
                {
                    return;
                }

                if (null == SkydrmProcess)
                {
                    return;
                }

                ExportInfo.Builder builder = new ExportInfo.Builder();

                builder.SetEnumFileRepo(nxlConverterResult.EnumFileRepo)
                       .SetRmsRemotePath(nxlConverterResult.RmsRemotePath)
                       .SetFileName(nxlConverterResult.FileName)
                       .SetDestinationPath(destinationPath);

                string data = JsonConvert.SerializeObject(builder.Build());

                data = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(data));

                SkydrmProcess.StartInfo.Arguments += "-export";
                SkydrmProcess.StartInfo.Arguments += " ";
                SkydrmProcess.StartInfo.Arguments += data;
                SkydrmProcess.Start();
                SkydrmProcess.StartInfo.Arguments = string.Empty;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message.ToString());
            }
        }

        private static void StartSkydrmLocalProcess(IntentParam param)
        {
            try
            {                 
                if (SkydrmProcess == null)
                {
                    return;
                }
                string general_param = "";
                if (param.isShowFileInfo)
                {
                    general_param = "-showfileinfo";

                }
                else if (param.isShareFile)
                {
                    general_param = "-share";
                }
                else if (param.isExtractContent)
                {
                    general_param = "-extractContent";
                }

                SkydrmProcess.StartInfo.Arguments += general_param;
                SkydrmProcess.StartInfo.Arguments += " ";
                SkydrmProcess.StartInfo.Arguments += param.param;
                SkydrmProcess.Start();
                SkydrmProcess.StartInfo.Arguments = string.Empty;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }

  
        [Serializable]
        public class ExportInfo
        {
            public EnumFileRepo EnumFileRepo { get; }

            public string RmsRemotePath { get; }

            public string FileName { get; }

            public string DestinationPath { get; }

            private ExportInfo(EnumFileRepo EnumFileRepo, string RmsRemotePath, string FileName, string DestinationPath)
            {
                this.EnumFileRepo = EnumFileRepo;
                this.RmsRemotePath = RmsRemotePath;
                this.FileName = FileName;
                this.DestinationPath = DestinationPath;
            }

            public class Builder
            {
                private EnumFileRepo EnumFileRepo { get; set; }
                private string RmsRemotePath { get; set; }
                private string FileName { get; set; }
                private string DestinationPath { get; set; }

                public Builder SetEnumFileRepo(EnumFileRepo EnumFileRepo)
                {
                    this.EnumFileRepo = EnumFileRepo;
                    return this;
                }

                public Builder SetRmsRemotePath(string RmsRemotePath)
                {
              
                    this.RmsRemotePath = RmsRemotePath;
                  
                    return this;
                }

                public Builder SetFileName(string FileName)
                {
                                
                    this.FileName = FileName;
                    
                    return this;
                }

                public Builder SetDestinationPath(string DestinationPath)
                {
           
                    this.DestinationPath = DestinationPath;
                   
                    return this;
                }

                public ExportInfo Build()
                {
                    return new ExportInfo(EnumFileRepo, RmsRemotePath, FileName, DestinationPath);
                }

            }
        }

        public class IntentParam
        {
            public  bool isShowFileInfo;
            public  bool isShareFile;
            public  bool isExtractContent;
            public  string param;
        }

        [Serializable]
        public class EditInfo
        {
            public bool IsNeedUpload{ get; }

            public string NXlFileLocalPath { get; }

            private EditInfo(bool IsNeedUpload , string NXlFileLocalPath)
            {
                this.IsNeedUpload = IsNeedUpload;
                this.NXlFileLocalPath = NXlFileLocalPath;
            }

            public class Builder
            {
                private bool M_IsNeedUpload { get; set; }

                private string NXlFileLocalPath { get; set; }

                public Builder IsNeedUpload(bool IsNeedUpload)
                {

                    this.M_IsNeedUpload = IsNeedUpload;
                    return this;
                }

                public Builder SetNXlFileLocalPath(string NXlFileLocalPath)
                {

                    this.NXlFileLocalPath = NXlFileLocalPath;

                    return this;
                }

                public EditInfo Build()
                {
                    return new EditInfo(M_IsNeedUpload, NXlFileLocalPath);
                }

            }
        }

    }
}
