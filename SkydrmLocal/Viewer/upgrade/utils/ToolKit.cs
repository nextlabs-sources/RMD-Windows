using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Viewer.upgrade.application;
using Viewer.upgrade.file.basic.utils;
using Viewer.upgrade.ui.common.previewer2.viewModel;
using Viewer.upgrade.utils.overlay.utils;

namespace Viewer.upgrade.utils
{
    public class ToolKit
    {

        public static readonly List<string> EMAIL_EXTENSIONS = new List<string>
        {
             ".msg"
        };

        public static readonly List<string> VDS_3D_EXTENSIONS = new List<string>
        {
             ".vds"
        };

        public static readonly List<string> EXCHANGE_3D_EXTENSIONS = new List<string>
        {
            ".jt",
            ".igs",
            ".stp",
            ".stl",
            ".step",
            ".iges",
            ".par",
            ".psm",
            ".x_t",
            ".x_b",
            ".xmt_txt",
            ".prt",
            ".neu",
            ".model",
            ".3dxml",
            ".catpart",
            ".cgr",
            ".catshape",
            ".sldprt",
            ".sldasm",
            ".dwg",
            ".dxf",
            ".ipt",
            ".asm",
            ".iam",
            ".catproduct",
            ".3mf",
            ".sat",
            ".sab",
            ".dwf",
            ".dwfx",
            ".session",
            ".dlv",
            ".exp",
            ".catdrawing",
            ".dae",
            ".xas",
            ".xpr",
            ".fbx",
            ".gltf",
            ".glb",
            ".mf1",
            ".arc",
            ".unv",
            ".pkg",
            ".ifc",
            ".ifczip",
            ".xmt",
            ".prc",
            ".rvt",
            ".rfa",
            ".3dm",
            ".pwd",
            ".stpz",
            ".stpx",
            ".stpxz",
            ".u3d",
            ".vda",
            ".wrl",
            ".vrml",
            ".obj",
            ".3ds",
        };

        public static readonly List<string> EXCHANGE_3D_ASSEMBLY = new List<string>
        {
            ".prt",
            ".asm",
            ".sldasm",
            ".jt",
            ".sldprt",
            ".iam",
            ".catproduct",
        };

        public static readonly List<string> EXCHANGE_3D_ASSEMBLY_ROOT_NODE = new List<string>
        {
            ".iam",
            ".catproduct",
        };

        public static readonly List<string> TEXT_EXTENSIONS = new List<string>
        {
            ".cpp",
            ".xml",
            ".json",
            ".h",
            ".js",
            ".java",
            ".err",
            ".m",
            ".swift",
            ".txt",
            ".log",
            ".sql",
            ".c",
            ".py",
        };

        public static readonly List<string> HYPERTEXT_MARKUP = new List<string>
        {
             ".htm",
             ".html",
        };

        public static readonly List<string> IMAGE_EXTENSIONS = new List<string>
        {
            ".png",
            ".gif",
            ".jpg",
            ".bmp",
            ".tif",
            ".tiff",
            ".jpe",
        };

        public static readonly List<string> HSF_3D_EXTENSIONS = new List<string>
        {
            ".hsf",
            ".xyz",
            ".pts",
            ".ptx",
        };

        public static readonly List<string> AUDIO_EXTENSIONS = new List<string>
        {
            ".mp3",
        };

        public static readonly List<string> VIDEO_EXTENSIONS = new List<string>
        {
            ".mp4",
        };

        public static readonly List<string> WORD_EXTENSIONS = new List<string>
        {
            ".doc",
            ".docx",
            ".dot",
            ".dotx",
            ".rtf",
            //".vsd",
            //".vsdx"
        };

        public static readonly List<string> EXCEL_EXTENSIONS = new List<string>
        {
            ".xls",
            ".xlsx",
            ".xlt",
            ".xltx",
            ".xlsb"
        };

        public static readonly List<string> POWERPOINT_EXTENSIONS = new List<string>
        {
            ".ppt",
            ".pptx",
            ".ppsx",
            ".potx",
        };

        public static readonly List<string> PDF_EXTENSIONS = new List<string>
        {
            ".pdf"
        };

        ///// <summary>
        ///// Detect office app if is installed in local machine, now only consider Office 2013 & Office 2016.
        ///// </summary>
        ///// <param name="version">returned the office version</param>
        //public static bool IsOfficeInstalled(out List<OfficeInformation> versions)
        //{
        //    bool ret = false;
        //    versions = new List<OfficeInformation>();
        //   // version = EnumOfficeVer.Unknown;

        //    try
        //    {
        //        // For 32-bit office
        //        RegistryKey baseKey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        //        RegistryKey subKey32_15 = baseKey32.OpenSubKey(@"SOFTWARE\Microsoft\Office\15.0\Common\InstallRoot", false); // Office 2013
        //        RegistryKey subKey32_16 = baseKey32.OpenSubKey(@"SOFTWARE\Microsoft\Office\16.0\Word\InstallRoot", false); // Office 2016

        //        // For 64-bit office
        //        RegistryKey baseKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        //        RegistryKey subKey64_15 = baseKey64.OpenSubKey(@"SOFTWARE\Microsoft\Office\15.0\Common\InstallRoot", false); // Office 2013
        //        RegistryKey subKey64_16 = baseKey64.OpenSubKey(@"SOFTWARE\Microsoft\Office\16.0\Word\InstallRoot", false); // Office 2016

        //        //if ((subKey32_16 != null && subKey32_16.GetValue("Path") != null)
        //        //    || (subKey64_16 != null && subKey64_16.GetValue("Path") != null))
        //        //{
        //        //    version = EnumOfficeVer.Office_2016;
        //        //    ret = true;
        //        //}
        //        //else if ((subKey32_15 != null && subKey32_15.GetValue("Path") != null)
        //        //    || (subKey64_15 != null && subKey64_15.GetValue("Path") != null))
        //        //{
        //        //    version = EnumOfficeVer.Office_2013;
        //        //    ret = true;
        //        //}

        //        if ((subKey32_16 != null && subKey32_16.GetValue("Path") != null))
        //        {
        //            versions.Add(new OfficeInformation() {
        //                EnumOfficeVer = EnumOfficeVer.Office_2016,
        //                Bit = 32
        //            });
        //            ret = true;
        //        }

        //        if ((subKey64_16 != null && subKey64_16.GetValue("Path") != null))
        //        {
        //            versions.Add(new OfficeInformation()
        //            {
        //                EnumOfficeVer = EnumOfficeVer.Office_2016,
        //                Bit = 64
        //            });
        //            ret = true;
        //        }

        //        if ((subKey32_15 != null && subKey32_15.GetValue("Path") != null))
        //        {
        //            versions.Add(new OfficeInformation()
        //            {
        //                EnumOfficeVer = EnumOfficeVer.Office_2013,
        //                Bit = 32
        //            });
        //            ret = true;
        //        }

        //        if ((subKey64_15 != null && subKey64_15.GetValue("Path") != null))
        //        {
        //            versions.Add(new OfficeInformation()
        //            {
        //                EnumOfficeVer = EnumOfficeVer.Office_2013,
        //                Bit = 64
        //            });
        //            ret = true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(" Exception in IsOfficeInstalled.");
        //    }

        //    return ret;
        //}


        public static bool DetectOffice2013(RegistryView registryView)
        {
            bool result = false;
            try
            {
                RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
                RegistryKey subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Office\15.0\Common\InstallRoot", false); // Office 2013
                if (subKey != null)
                {
                    object oValue = subKey.GetValue("Path");
                    if (null != oValue)
                    {
                        string strValue = oValue.ToString();
                        if (!string.IsNullOrWhiteSpace(strValue))
                        {
                            result = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }


        public static bool DetectOffice2016(RegistryView registryView)
        {
            bool result = false;
            try
            {
                RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
                RegistryKey subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Office\16.0\Word\InstallRoot", false); // Office 2016
                if (subKey != null)
                {
                    object oValue = subKey.GetValue("Path");
                    if (null != oValue)
                    {
                        string strValue = oValue.ToString();
                        if (!string.IsNullOrWhiteSpace(strValue))
                        {
                            result = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }


        public static bool DetectOffice2019(RegistryView registryView)
        {
            bool result = false;
            try
            {
                RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
                RegistryKey subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration", false);
                if (null != subKey)
                {
                    object value = subKey.GetValue("ProductReleaseIDs");
                    if (null != value)
                    {
                        string strValue = value.ToString();
                        if (string.Equals(strValue, "ProPlus2019Retail",StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = true;
                        }
                        else if (string.Equals(strValue, "ProPlusRetail", StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }


        public static bool DetectOffice365(RegistryView registryView)
        {
            bool result = false;
            try
            {
                RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
                RegistryKey subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration", false);
                if (null != subKey)
                {
                    object value = subKey.GetValue("ProductReleaseIDs");
                    if (null != value)
                    {
                        string strValue = value.ToString();
                        if (string.Equals(strValue, "O36​​5ProPlusRetail", StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }


        public static long DateTimeToTimestamp(DateTime time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));
            return Convert.ToInt64((time - startDateTime).TotalMilliseconds);
        }

        public static void DebugPurpose_PopupMsg_CheckSpecificRegistryItem()
        {
            // This is a back-door to check [Registy]\Computer\HKEY_CURRENT_USER\Software\Nextlabs\SkyDRM\LocalApp
            //  DebugViewer = ?
            //  we will read the sleep time/option from registry. If it is set to 30, the code will sleep 30s, 
            int count = 0;
            int millisecondsTimeout = 1000;
            RegistryKey localApp = null;
            try
            {
                localApp = Registry.CurrentUser.OpenSubKey(@"Software\Nextlabs\SkyDRM\LocalApp");
                count = (int)localApp.GetValue("DebugViewer", -1);
                SleepThreadForDebug(count, millisecondsTimeout);

                //  isShowMsgBox = (int)localApp.GetValue("DebugViewer", 0) == 1 ? true : false;
                //if (isShowMsgBox)
                //{
                // MessageBox.Show("for debug, good pint to set breakpoint");
                //}
            }
            catch
            {
                // ignroe
            }
            finally
            {
                if (localApp != null)
                {
                    localApp.Close();
                }
            }
        }


        public static void SleepThreadForDebug(int count , int millisecondsTimeout)
        {
            try
            {
                if (count <= 0 || millisecondsTimeout <= 0)
                {
                    return;
                }
                for (int i = count; i > 0; i--)
                {
                    System.Threading.Thread.Sleep(millisecondsTimeout);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static Int64 RunningMode()
        {
            Int64 result = 0;
            RegistryKey registryKey = null;
            try
            {
                registryKey = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\Viewer\DPI");
                result = (Int64)registryKey.GetValue("RunningMode", 0);
            }
            catch
            {
                // ignroe
            }
            finally
            {
                if (registryKey != null)
                {
                    registryKey.Close();
                }
            }

            return result;
        }


        public static void DumpArgsToLog(string[] cmdArgs)
        {
            string l = "";
            foreach (var i in cmdArgs)
            {
                l += i;
                l += " ";
            }
            ViewerApp app =(ViewerApp)ViewerApp.Current;
            app.Log.Info("\t\t cmdArgs:" + l+"\r\n");
        }

        //public enum EnumOfficeVer
        //{
        //    Unknown = 0,
        //    Office_2013 = 1,
        //    Office_2016 = 2
        //}

        //public class OfficeInformation
        //{
        //    public EnumOfficeVer EnumOfficeVer;
        //    public int Bit;
        //}

        //public static EnumFileType GetFileTypeByExtentionEx(string filePath)
        //{
        //    EnumFileType result = EnumFileType.UNKNOWN;
        //    try
        //    {
        //        string tempFilePath = string.Copy(filePath);
        //        if (tempFilePath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
        //        {
        //            tempFilePath = Path.GetFileNameWithoutExtension(tempFilePath);
        //        }

        //        // Should handle the Team center automatically rename for the postfix, like: Jack.prt-2019-01-24-07-04-28.1
        //        if (!StringHelper.Replace(tempFilePath,
        //                                 out tempFilePath,
        //                                 StringHelper.TIMESTAMP_PATTERN + StringHelper.POSTFIX_1_249,
        //                                 RegexOptions.IgnoreCase))
        //        {
        //            // Should handle the Team center automatically rename for the postfix, like: Jack-2019-4-10-07-40-13.prt.1
        //            StringHelper.Replace(tempFilePath,
        //                                out tempFilePath,
        //                                StringHelper.POSTFIX_1_249,
        //                                RegexOptions.IgnoreCase);
        //        }

        //        tempFilePath = tempFilePath.Trim();
        //        string extention = Path.GetExtension(tempFilePath);
        //        result = GetFileTypeByExtention(extention);
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private static EnumFileType GetFileTypeByExtention(string Extention)
        //{
        //    Extention = Extention.ToLower();
        //    EnumFileType fileType = EnumFileType.UNKNOWN;
        //    try
        //    {
        //        if (ToolKit.VDS_3D_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_SAP_VDS;
        //        }

        //        if (ToolKit.EXCHANGE_3D_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_HPS_EXCHANGE_3D;
        //        }

        //        if (ToolKit.HSF_3D_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_HOOPS_3D;
        //        }

        //        if (ToolKit.TEXT_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_PLAIN_TEXT;
        //        }

        //        if (ToolKit.IMAGE_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_IMAGE;
        //        }

        //        if (ToolKit.AUDIO_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_AUDIO;
        //        }

        //        if (ToolKit.VIDEO_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_VIDEO;
        //        }

        //        if (ToolKit.WORD_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_OFFICE;
        //        }

        //        if (ToolKit.EXCEL_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_OFFICE;
        //        }

        //        if (ToolKit.POWERPOINT_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_OFFICE;
        //        }

        //        if (ToolKit.PDF_EXTENSIONS.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_PDF;
        //        }

        //        if (ToolKit.HYPERTEXT_MARKUP.Contains(Extention))
        //        {
        //            fileType = EnumFileType.FILE_TYPE_HYPERTEXT_MARKUP;
        //        }

        //        if (EnumFileType.UNKNOWN == fileType)
        //        {
        //            fileType = EnumFileType.FILE_TYPE_NOT_SUPPORT;
        //        }

        //        return fileType;
        //    }
        //    catch (Exception ex)
        //    {
        //        fileType = EnumFileType.UNKNOWN;
        //        throw ex;
        //    }
        //}

        public static bool IsSupported(string Extention)
        {
            try
            {
                if (ToolKit.VDS_3D_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.EXCHANGE_3D_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.HSF_3D_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.TEXT_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.IMAGE_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.AUDIO_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.VIDEO_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.WORD_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.EXCEL_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.POWERPOINT_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }

                if (ToolKit.PDF_EXTENSIONS.Contains(Extention))
                {
                    return true;
                }
                if (ToolKit.HYPERTEXT_MARKUP.Contains(Extention))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        public static bool IsAttachOverlay(WatermarkInfo waterMark)
        {
            bool result = false;

            if (null == waterMark)
            {
                result = false;
            }
            else
            {
                result = true;
                //if (string.IsNullOrEmpty(waterMark.Text))
                //{
                //    result = false;
                //}
                //else
                //{
                //    result = true;
                //}
            }

            //if (NxlFileInfo.IsOwner)
            //{
            //    return false;
            //}

            //// is not owner & don't have watermark
            //if (!NxlFileInfo.Rights.Contains(viewer.model.FileRights.RIGHT_WATERMARK))
            //{
            //    return false;
            //}

            return result;
        }


        // Function to detect the encoding for UTF-7, UTF-8/16/32 (bom, no bom, little
        // & big endian), and local default codepage, and potentially other codepages.
        // 'taster' = number of bytes to check of the file (to save processing). Higher
        // value is slower, but more reliable (especially UTF-8 with special characters
        // later on may appear to be ASCII initially). If taster = 0, then taster
        // becomes the length of the file (for maximum reliability). 'text' is simply
        // the string with the discovered encoding applied to the file.
        public static Encoding DetectTextEncoding(string filename, out String text, int taster = 1000)
        {
            byte[] b = File.ReadAllBytes(filename);

            //////////////// First check the low hanging fruit by checking if a
            //////////////// BOM/signature exists (sourced from http://www.unicode.org/faq/utf_bom.html#bom4)
            if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0xFE && b[3] == 0xFF) { text = Encoding.GetEncoding("utf-32BE").GetString(b, 4, b.Length - 4); return Encoding.GetEncoding("utf-32BE"); }  // UTF-32, big-endian 
            else if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0x00 && b[3] == 0x00) { text = Encoding.UTF32.GetString(b, 4, b.Length - 4); return Encoding.UTF32; }    // UTF-32, little-endian
            else if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) { text = Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2); return Encoding.BigEndianUnicode; }     // UTF-16, big-endian
            else if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) { text = Encoding.Unicode.GetString(b, 2, b.Length - 2); return Encoding.Unicode; }              // UTF-16, little-endian
            else if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) { text = Encoding.UTF8.GetString(b, 3, b.Length - 3); return Encoding.UTF8; } // UTF-8
            else if (b.Length >= 3 && b[0] == 0x2b && b[1] == 0x2f && b[2] == 0x76) { text = Encoding.UTF7.GetString(b, 3, b.Length - 3); return Encoding.UTF7; } // UTF-7


            //////////// If the code reaches here, no BOM/signature was found, so now
            //////////// we need to 'taste' the file to see if can manually discover
            //////////// the encoding. A high taster value is desired for UTF-8
            if (taster == 0 || taster > b.Length) taster = b.Length;    // Taster size can't be bigger than the filesize obviously.


            // Some text files are encoded in UTF8, but have no BOM/signature. Hence
            // the below manually checks for a UTF8 pattern. This code is based off
            // the top answer at: https://stackoverflow.com/questions/6555015/check-for-invalid-utf8
            // For our purposes, an unnecessarily strict (and terser/slower)
            // implementation is shown at: https://stackoverflow.com/questions/1031645/how-to-detect-utf-8-in-plain-c
            // For the below, false positives should be exceedingly rare (and would
            // be either slightly malformed UTF-8 (which would suit our purposes
            // anyway) or 8-bit extended ASCII/UTF-16/32 at a vanishingly long shot).
            int i = 0;
            bool utf8 = false;
            while (i < taster - 4)
            {
                if (b[i] <= 0x7F) { i += 1; continue; }     // If all characters are below 0x80, then it is valid UTF8, but UTF8 is not 'required' (and therefore the text is more desirable to be treated as the default codepage of the computer). Hence, there's no "utf8 = true;" code unlike the next three checks.
                if (b[i] >= 0xC2 && b[i] <= 0xDF && b[i + 1] >= 0x80 && b[i + 1] < 0xC0) { i += 2; utf8 = true; continue; }
                if (b[i] >= 0xE0 && b[i] <= 0xF0 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0) { i += 3; utf8 = true; continue; }
                if (b[i] >= 0xF0 && b[i] <= 0xF4 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0 && b[i + 3] >= 0x80 && b[i + 3] < 0xC0) { i += 4; utf8 = true; continue; }
                utf8 = false; break;
            }
            if (utf8 == true)
            {
                text = Encoding.UTF8.GetString(b);
                return Encoding.UTF8;
            }


            // The next check is a heuristic attempt to detect UTF-16 without a BOM.
            // We simply look for zeroes in odd or even byte places, and if a certain
            // threshold is reached, the code is 'probably' UF-16.          
            double threshold = 0.1; // proportion of chars step 2 which must be zeroed to be diagnosed as utf-16. 0.1 = 10%
            int count = 0;
            for (int n = 0; n < taster; n += 2) if (b[n] == 0) count++;
            if (((double)count) / taster > threshold) { text = Encoding.BigEndianUnicode.GetString(b); return Encoding.BigEndianUnicode; }
            count = 0;
            for (int n = 1; n < taster; n += 2) if (b[n] == 0) count++;
            if (((double)count) / taster > threshold) { text = Encoding.Unicode.GetString(b); return Encoding.Unicode; } // (little-endian)


            // Finally, a long shot - let's see if we can find "charset=xyz" or
            // "encoding=xyz" to identify the encoding:
            for (int n = 0; n < taster - 9; n++)
            {
                if (
                    ((b[n + 0] == 'c' || b[n + 0] == 'C') && (b[n + 1] == 'h' || b[n + 1] == 'H') && (b[n + 2] == 'a' || b[n + 2] == 'A') && (b[n + 3] == 'r' || b[n + 3] == 'R') && (b[n + 4] == 's' || b[n + 4] == 'S') && (b[n + 5] == 'e' || b[n + 5] == 'E') && (b[n + 6] == 't' || b[n + 6] == 'T') && (b[n + 7] == '=')) ||
                    ((b[n + 0] == 'e' || b[n + 0] == 'E') && (b[n + 1] == 'n' || b[n + 1] == 'N') && (b[n + 2] == 'c' || b[n + 2] == 'C') && (b[n + 3] == 'o' || b[n + 3] == 'O') && (b[n + 4] == 'd' || b[n + 4] == 'D') && (b[n + 5] == 'i' || b[n + 5] == 'I') && (b[n + 6] == 'n' || b[n + 6] == 'N') && (b[n + 7] == 'g' || b[n + 7] == 'G') && (b[n + 8] == '='))
                    )
                {
                    if (b[n + 0] == 'c' || b[n + 0] == 'C') n += 8; else n += 9;
                    if (b[n] == '"' || b[n] == '\'') n++;
                    int oldn = n;
                    while (n < taster && (b[n] == '_' || b[n] == '-' || (b[n] >= '0' && b[n] <= '9') || (b[n] >= 'a' && b[n] <= 'z') || (b[n] >= 'A' && b[n] <= 'Z')))
                    { n++; }
                    byte[] nb = new byte[n - oldn];
                    Array.Copy(b, oldn, nb, 0, n - oldn);
                    try
                    {
                        string internalEnc = Encoding.ASCII.GetString(nb);
                        text = Encoding.GetEncoding(internalEnc).GetString(b);
                        return Encoding.GetEncoding(internalEnc);
                    }
                    catch { break; }    // If C# doesn't recognize the name of the encoding, break.
                }
            }

            // If all else fails, the encoding is probably (though certainly not
            // definitely) the user's local codepage! One might present to the user a
            // list of alternative encodings as shown here: https://stackoverflow.com/questions/8509339/what-is-the-most-common-encoding-of-each-language
            // A full list can be found using Encoding.GetEncodings();
            text = Encoding.Default.GetString(b);
            return Encoding.Default;
        }


        public static void SaveHwndToRegistry(string filePath, IntPtr hWnd)
        {
            RegistryKey OpeningFiles = null;
            try
            {
                OpeningFiles = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\Viewer\OpeningFiles");
                OpeningFiles.SetValue(filePath, hWnd.ToInt64());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static IntPtr GetHwndFromRegistry(string filePath)
        {
            RegistryKey OpeningFiles = null;
            IntPtr result = IntPtr.Zero;
            try
            {
                OpeningFiles = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\Viewer\OpeningFiles");
                object value = OpeningFiles.GetValue(filePath);
                if (null != value)
                {
                    result = new IntPtr(Int64.Parse(value.ToString()));
                }

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void DeleteHwndFromRegistry(string filePath)
        {
            RegistryKey OpeningFiles = null;
            try
            {
                OpeningFiles = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\Viewer\OpeningFiles");
                OpeningFiles.DeleteValue(filePath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static bool IsSystemFolderPath(string path)
        {
            bool result = false;
            foreach (var item in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                bool isSucceed = Enum.TryParse(item.ToString(), out Environment.SpecialFolder value);
                if (isSucceed)
                {
                    result = path.Equals(Environment.GetFolderPath(value), StringComparison.OrdinalIgnoreCase);
                    if (result)
                    {
                        break;
                    }
                }
            }
            if (!result)
            {
                result = path.Equals(@"c:\", StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }

        public static bool IsSpecialFolderPath(string path)
        {
            bool result = false;
            if (path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nextlabs\SkyDRM", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), StringComparison.OrdinalIgnoreCase))
            {
                result = true;
            }
            return result;
        }

        public class ComObjInitializeResult
        {
            public Exception Exception = null;
            public PreviewHandler PreviewHandler = null;
        }

        /// <summary>
        /// Returns the id of that process given by that window title
        /// </summary>
        /// <param name="AppId">Int32MaxValue returned if it cant be found.</param>
        /// <returns></returns>
        public static int GetProcessIdByWindowTitle(string titleString)
        {
            Process[] P_CESSES = Process.GetProcesses();
            for (int p_count = 0; p_count < P_CESSES.Length; p_count++)
            {
                if (P_CESSES[p_count].MainWindowTitle.Equals(titleString, StringComparison.CurrentCultureIgnoreCase))
                {
                    return P_CESSES[p_count].Id;
                }
            }
            return Int32.MaxValue;
        }

    }
}
