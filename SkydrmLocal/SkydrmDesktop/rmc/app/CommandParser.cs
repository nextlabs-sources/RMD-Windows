using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SkydrmLocal.rmc.common.helper;
using SkydrmDesktop.Resources.languages;
using Spire.Pdf;
using Microsoft.Win32;

namespace SkydrmDesktop.rmc.app
{
    public static class CommandParser
    {
        private static bool isProtect = false;
        private static bool isShare = false;
        private static bool isView = false;
        private static bool isShowMain = false;
        private static bool isShowFileInfo = false;
        private static bool isAddNxlLog = false;
        private static bool isEdit = false;
        private static bool isExport = false;
        private static bool isOpenWeb = false;
        private static bool isReShare = false;
        private static bool isAddFileToProject = false;
        private static bool isModifyRights= false;
        private static bool isExtractContent = false;


        private static string file;
        private static string paramdetail;
        //private static string processId;

        public static bool IsProtect { get => isProtect; }
        public static bool IsShare { get => isShare; }
        public static bool IsView { get => isView; }
        public static bool IsShowMain { get => isShowMain; }
        public static string Path { get => file; }
        public static string ParamDetail { get => paramdetail; }
        public static bool IsShowFileInfo { get => isShowFileInfo; }
        public static bool IsAddNxlLog { get => isAddNxlLog;}
        public static bool IsEdit { get => isEdit; }
        public static bool IsExport { get => isExport; }
        //public static string ProcessId { get => processId; }
        public static bool IsOpenWeb { get => isOpenWeb; }
        public static bool IsReShare { get => isReShare; }
        public static bool IsAddFileToProject { get => isAddFileToProject; }
        public static bool IsModifyRights { get => isModifyRights;}
        public static bool IsExtractContent { get => isExtractContent; }
       

        static public bool Parse(string[] ss)
        {
            if (ss == null || ss.Length == 0)
            {
                return true;
            }
            {
                string cmdLine = "";
                ss.All((s) => { cmdLine += (s+" "); return true; });

                SkydrmApp.Singleton.Log.Info("CommandParser.Parse: "+ cmdLine);
                //MessageBox.Show(cmdLine);
            }

            Reset();
            //
            // Begin Parse
            //
            foreach (string item in ss)
            {
                // current want to specify a file
                if (isShare || isProtect ||
                    isView  || isReShare || isAddFileToProject || isModifyRights)
                {
                    file = item;
                    break;
                }
                
                if (isShowMain || isOpenWeb)
                {
                    break;
                }

                if (isShowFileInfo|| isAddNxlLog || isExport || isEdit || isExtractContent)
                {
                    paramdetail = item;
                    break;
                }

                // want share?
                if (0 == String.Compare(item.Substring(1), "share", true))
                {
                    isShare = true;
                    continue;
                }
                // want prtect?
                else if (0 == String.Compare(item.Substring(1), "protect", true))
                {
                    isProtect = true;
                    continue;
                }
                // want view?
                else if (0 == String.Compare(item.Substring(1), "view", true))
                {
                    isView = true;
                    continue;
                }
                // want show main?
                else if (0 == String.Compare(item.Substring(1), "showmain", true))
                {
                    isShowMain = true;
                    continue;
                }
                // want showfileinfo?
                else if (0==String.Compare(item.Substring(1), "showfileinfo", true))
                {
                    isShowFileInfo = true;
                    continue;
                }
                // want addlog?
                else if (0 == String.Compare(item.Substring(1), "addLog", true))
                {
                    isAddNxlLog = true;
                    continue;
                }
                // want edit?
                else if (0 == String.Compare(item.Substring(1), "edit", true))
                {
                    isEdit = true;
                    continue;
                }
                // want export?
                else if (0 == String.Compare(item.Substring(1), "export", true))
                {
                    isExport = true;
                    continue;
                }
                // want reshare nxl file
                else if (0 == String.Compare(item.Substring(1), "reShare", true))
                {
                    isReShare = true;
                    continue;
                }
                // want add nxl file to project
                else if(0 == String.Compare(item.Substring(1), "addNxlToProject", true))
                {
                    isAddFileToProject = true;
                    continue;
                }
                // want modify nxl file rights
                else if(0 == String.Compare(item.Substring(1), "modifyRights", true))
                {
                    isModifyRights = true;
                    continue;
                }
                // want extract nxl file content
                else if(0 == String.Compare(item.Substring(1), "extractContent", true))
                {
                    isExtractContent = true;
                    continue;
                }
                // want open skydrm web?
                else if (0 == String.Compare(item.Substring(1), "openSkyDRMWeb", true))
                {
                    isOpenWeb = true;
                    continue;
                }
            }

            //
            // check all cmd supported, at least match one
            //
            if (!isProtect && !isShare && !isView && !isShowMain && !isShowFileInfo 
                && !isAddNxlLog && !isEdit && !isExport && !isOpenWeb 
                && !isReShare && !isAddFileToProject && !isModifyRights && !isExtractContent)
            {
                // can not find user intend
                return false;
            }

            if (isShowMain || isOpenWeb)
            {
                return true;
            }

            if (isShare)
            {
                // decapsulate base64 into a string;
                if (StringHelper.IsValidBase64String(file))
                {
                    file = Encoding.UTF8.GetString(System.Convert.FromBase64String(file));
                }
            }

            if (isProtect || isShare || isView || isReShare || isAddFileToProject ||isModifyRights)
            {
                try
                {
                    // require file exist and file size >0
                    if (file.Contains('|'))
                    {
                        string[] fpara = file.Split(new char[1] { '|' });
                        file = fpara[0];
                        if(isProtect && !IsSensitiveLabelEnable())
                        {
                            paramdetail = string.Empty;
                        }
                        else
                        {
                            paramdetail = fpara[1];
                        }
                       
                    }
                    else if(IsSensitiveLabelEnable() && isProtect)
                    {
                        string extension = System.IO.Path.GetExtension(file);
                        if (extension.Equals(".pdf", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string label = GetPDFSensitiveLabel(file);
                            if (string.IsNullOrWhiteSpace(label))
                            {
                                paramdetail = string.Empty;
                            }
                            else
                            {
                                paramdetail = label;
                            }
                        }
                    }

                    if (File.Exists(file))
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length > 0)
                        {
                            return true;
                        }
                        else // empty file
                        {
                            SkydrmApp.Singleton.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Notify_EmptyFile_Not_Operate"), false, fileInfo.Name);
                            return false;
                        }
                        
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {                 
                    SkydrmApp.Singleton.Log.Error(ex.Message.ToString(),ex);
                    return false;
                }

            }

            if (isShowFileInfo || isAddNxlLog || isExport || isEdit || isExtractContent)
            {
                if (paramdetail.Length > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }


            // should never reach here
            return false;
        }

        static public void Reset()
        {
            //
            // clear previous 
            //
            isProtect = false;
            isShare = false;
            isView = false;
            isShowMain = false;
            isShowFileInfo = false;
            isAddNxlLog = false;
            isEdit = false;
            isExport = false;
            isOpenWeb = false;
            isReShare = false;
            isAddFileToProject = false;
            isModifyRights = false;
            isExtractContent = false;


            file = "";
            paramdetail = "";
        }

        /// <summary>
        /// this function is getting label in PDF  custom property
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public string GetPDFSensitiveLabel(string path)
        {
            string label = string.Empty;

            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(path);
            Dictionary<string, string> dic = doc.DocumentInformation.GetAllCustomProperties();

            if (dic.Count == 0)
            {
                return string.Empty;
            }

            var first = dic.ToList().FirstOrDefault();
            if (first.Key.StartsWith("MSIP_"))
            {
                if (first.Key.EndsWith("_Enabled"))
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }

            foreach (var item in dic)
            {
                if (item.Key != null && item.Key.EndsWith("_Name"))
                {
                    label = item.Value;
                    break;
                }
            }

            return label;
        }

        static public bool IsSensitiveLabelEnable()
        {
            int status = 0;
            RegistryKey skydrm = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM");
            try
            {
                RegistryKey mip = skydrm.OpenSubKey(@"MIP");
                status = (int)mip.GetValue("Enable_Label", "");
                if (1 == status)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception)
            {
                return false;
            }
            finally
            {
                skydrm?.Close();
            }
        }


    }
}
