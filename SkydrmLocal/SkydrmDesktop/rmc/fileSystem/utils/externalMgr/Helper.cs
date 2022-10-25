using Microsoft.Win32;
using SkydrmLocal.rmc.common.helper;
using SkydrmDesktop;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Text.RegularExpressions;

namespace SkydrmLocal.rmc.fileSystem.external
{
   public class Helper
    {
        private const string POSTFIX_NXL = ".nxl";

        private static List<string> CurrentUserSubKeys = new List<string>();
        private static List<string> LocalMachineSubKeys = new List<string>();


        static Helper()
        {
            //CurrentUser
            CurrentUserSubKeys.Add(@"Software\Microsoft\Office\Word\Addins\NxlRmAddin");
            CurrentUserSubKeys.Add(@"Software\Microsoft\Office\Excel\Addins\NxlRmAddin");
            CurrentUserSubKeys.Add(@"Software\Microsoft\Office\PowerPoint\Addins\NxlRmAddin");

            //LocalMachine
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\Excel\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\Word\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\PowerPoint\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\Wow6432Node\Microsoft\Office\Excel\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\Wow6432Node\Microsoft\Office\Word\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\Wow6432Node\Microsoft\Office\PowerPoint\Addins\NxlRmAddin");

            //LocalMachineclickToRun
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Microsoft\Office\Excel\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Microsoft\Office\Word\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Microsoft\Office\PowerPoint\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Wow6432Node\Microsoft\Office\Excel\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Wow6432Node\Microsoft\Office\Word\Addins\NxlRmAddin");
            LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Wow6432Node\Microsoft\Office\PowerPoint\Addins\NxlRmAddin");
        }

        #region Properties & Constants  
        public static List<string> WordExtensions = new List<string>
        {
            ".doc",
            ".docx",
            ".dot",
            ".dotx",
            ".rtf",
            ".vsd",
            ".vsdx"
        };

        public static List<string> ExcelExtensions = new List<string>
        {
            ".xls",
            ".xlsx",
            ".xlt",
            ".xltx",
            ".xlsb",
        };

        public static List<string> PowerpointExtensions = new List<string>
        {
            ".ppt",
            ".pptx",
            ".ppsx",
            ".potx",
        };

        public static List<string> PdfExtensions = new List<string>
        {
            ".pdf"
        };
        #endregion

        /// <summary>
        /// Judge if is office file.
        /// </summary>
        public static bool IsOfficeFile(string fileName)
        {
            bool ret = false;

            // Should handle the system automatically rename for the postfix, like: Allen-2018-10-22-07-40-13.txt(1)
            // Fix bug 55300
            // StringHelper.Replace(fileName, out fileName, StringHelper.REMOVE_SYSTEM_ATUO_RENAME_POSTFIX, RegexOptions.IgnoreCase);


            // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
            if (!StringHelper.Replace(fileName,
                                     out fileName,
                                     StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
                                     RegexOptions.IgnoreCase))
            {
                // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
                StringHelper.Replace(fileName,
                                    out fileName,
                                    StringHelper.POSTFIX_1_249,
                                    RegexOptions.IgnoreCase);
            }


            if (!string.IsNullOrEmpty(fileName))
            {
                if (fileName.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - POSTFIX_NXL.Length);
                    fileName = fileName.Trim();
                }

                if (fileName.Contains("."))
                {
                    string ext = fileName.Substring(fileName.LastIndexOf('.'));

                    // word
                    if (string.Equals(ext, ".docx", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".doc", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".dot", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".dotx", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".rtf", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".vsdx", StringComparison.CurrentCultureIgnoreCase))
                    {
                        ret = true;
                    }

                    // ppt
                    if (string.Equals(ext, ".pptx", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".ppt", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".ppsx", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".potx", StringComparison.CurrentCultureIgnoreCase))
                    {
                        ret = true;
                    }

                    // Excel
                    if (string.Equals(ext, ".xlsx", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".xls", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".xltx", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".xlt", StringComparison.CurrentCultureIgnoreCase)
                        || string.Equals(ext, ".xlsb", StringComparison.CurrentCultureIgnoreCase)
                        )
                    {
                        ret = true;
                    }

                }
            }

            return ret;
        }

        /// <summary>
        /// Detect office app if is installed in local machine, now only consider Office 2013 & Office 2016.
        /// </summary>
        /// <param name="version">returned the office version</param>
        public static bool IsOfficeInstalled(out EnumOfficeVer version)
        {
            bool ret = false;

            version = EnumOfficeVer.Unknown;
            
            try
            {
                // For 32-bit office
                RegistryKey baseKey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32); 
                RegistryKey subKey32_15 = baseKey32.OpenSubKey(@"SOFTWARE\Microsoft\Office\15.0\Common\InstallRoot", false); // Office 2013
                RegistryKey subKey32_16 = baseKey32.OpenSubKey(@"SOFTWARE\Microsoft\Office\16.0\Word\InstallRoot", false); // Office 2016

                // For 64-bit office
                RegistryKey baseKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                RegistryKey subKey64_15 = baseKey64.OpenSubKey(@"SOFTWARE\Microsoft\Office\15.0\Common\InstallRoot", false); // Office 2013
                RegistryKey subKey64_16 = baseKey64.OpenSubKey(@"SOFTWARE\Microsoft\Office\16.0\Word\InstallRoot", false); // Office 2016

                if ( (subKey32_16 != null && subKey32_16.GetValue("Path") != null) 
                    || (subKey64_16 != null && subKey64_16.GetValue("Path") != null))
                {
                    version = EnumOfficeVer.Office_2016;
                    ret = true;
                }
                else if ((subKey32_15 != null && subKey32_15.GetValue("Path") != null) 
                    || (subKey64_15 != null && subKey64_15.GetValue("Path") != null))
                {
                    version = EnumOfficeVer.Office_2013;
                    ret = true;
                }
              
            }
            catch(Exception e)
            {
                Console.WriteLine(" Exception in IsOfficeInstalled.");
                SkydrmApp.Singleton.Log.Error(e.ToString());
            }

            return ret;
        }

        /// <summary>
        /// Try to delete bad office Addin key-values -- fix bug 52100.
        /// -- when user try to edit office file multiple times(try to open, edit, close...), the add-in will disable in current user(only some machine).
        ///    Namely, "\HKEY_CURRENT_USER\Software\Microsoft\Office\PowerPoint\Addins\NxlRMAddin" -- LoadBehavior is 0, 
        ///    so we'll try to delete the key if exist when user edit.
        /// </summary>
        public static void ChangeRegeditOfOfficeAddin()
        {
            int value = 3;


            foreach (string key in CurrentUserSubKeys)
            {
                ChangeBadOfficeAddinKey(Registry.CurrentUser, key, value);
            }


            foreach (string key in LocalMachineSubKeys)
            {
                ChangeBadOfficeAddinKey(Registry.LocalMachine, key, value);
            }

        }

        private static void ChangeBadOfficeAddinKey(RegistryKey registryKey,string key,int value)
        {
            RegistryKey registry = null;

            try
            {
                registry = registryKey.CreateSubKey(key, RegistryKeyPermissionCheck.ReadWriteSubTree);             
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e.Message,e);
            }

            try
            {
                if ( null != registry)
                {
                    registry = registryKey.OpenSubKey(key, true);
                    registry.SetValue("LoadBehavior", value, RegistryValueKind.DWord);
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e.Message, e);
            }

        }

        private static void DeleteSubKey(RegistryKey registryKey, string subkey)
        {
            try
            {
                registryKey = registryKey.OpenSubKey(subkey,true);
                if (null!= registryKey)
                {
                    registryKey.DeleteSubKeyTree(subkey, false);
                    registryKey.Close();
                }                     
            }
            catch (Exception ex)
            {
                SkydrmApp.Singleton.Log.Error(ex.Message, ex);
            }              
        }

        /// <summary>
        /// Judge if exist Office add-in.
        /// </summary>
        public static bool IsExistOfficeAddin(string fileName)
        {

            // Try to delete dirty office addin key if exist. -- fix bug 52100
            fileSystem.external.Helper.ChangeRegeditOfOfficeAddin();

            Session session = SkydrmApp.Singleton.Rmsdk;

            bool ret = false;

            fileName = fileName.Substring(0,fileName.LastIndexOf("."));

            var ext = Path.GetExtension(fileName).ToLower();

            if (WordExtensions.Contains(ext))
            {
                if (session.IsPluginWell("Word", "x64") || session.IsPluginWell("Word", "x86"))
                {
                    ret = true;
                }
            }
            else if (ExcelExtensions.Contains(ext))
            {
                if (session.IsPluginWell("Excel", "x64") || session.IsPluginWell("Excel", "x86"))
                {
                    ret = true;
                }          
            }
            else if (PowerpointExtensions.Contains(ext))
            {
                if (session.IsPluginWell("PowerPoint", "x64") || session.IsPluginWell("PowerPoint", "x86"))
                {
                    ret = true;
                }
            }     
            return ret;
        }

        public static void cleanup_edit_mapping()
        {
            try
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\NextLabs\\SkyDRM", true);
                registry.DeleteSubKeyTree("Session", false);
                registry.Close();
            }
            catch (Exception ex)
            {
                SkydrmApp.Singleton.Log.Error(ex.Message, ex);
            }
        }

        public enum EnumOfficeVer
        {
            Unknown = 0,
            Office_2013 = 1,
            Office_2016 = 2
        }

    }
}
