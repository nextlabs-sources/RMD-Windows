using Alphaleonis.Win32.Filesystem;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.helper
{
    public class NxlHelper
    {
        public static FileRights[] FromRightStrings(string[] rights, bool isProtectFile = false)
        {
            var rt = new List<FileRights>();
            foreach (var s in rights)
            {
                switch (s.ToUpper())
                {
                    case "VIEW":
                        rt.Add(FileRights.RIGHT_VIEW);
                        break;
                    case "EDIT":
                        rt.Add(FileRights.RIGHT_EDIT);
                        break;
                    case "PRINT":
                        rt.Add(FileRights.RIGHT_PRINT);
                        break;
                    case "CLIPBOARD":
                        rt.Add(FileRights.RIGHT_CLIPBOARD);
                        break;
                    case "SAVEAS":
                        if (isProtectFile)
                        {
                            // Should write "Download" rights for "Save As", but the ui should display "Save As". -- fix bug 52176
                            rt.Add(FileRights.RIGHT_DOWNLOAD);
                        }
                        else
                        {
                            rt.Add(FileRights.RIGHT_SAVEAS);
                        }
                        break;
                    case "DECRYPT":
                        rt.Add(FileRights.RIGHT_DECRYPT);
                        break;
                    case "SCREENCAP":
                        rt.Add(FileRights.RIGHT_SCREENCAPTURE);
                        break;
                    case "SEND":
                        rt.Add(FileRights.RIGHT_SEND);
                        break;
                    case "CLASSIFY":
                        rt.Add(FileRights.RIGHT_CLASSIFY);
                        break;
                    case "SHARE":
                        rt.Add(FileRights.RIGHT_SHARE);
                        break;
                    case "DOWNLOAD":
                        rt.Add(FileRights.RIGHT_DOWNLOAD);
                        break;
                    case "WATERMARK":
                        rt.Add(FileRights.RIGHT_WATERMARK);
                        break;
                    default:
                        break;
                }
            }
            return rt.ToArray();
        }

        public static List<string> Helper_GetRightsStr(List<FileRights> rights, bool bAddIfHasWatermark = false, bool bForceAddValidity = true)
        {
            var rt = new List<string>();
            if (rights == null || rights.Count == 0)
            {
                return rt;
            }
            foreach (FileRights f in rights)
            {
                switch (f)
                {
                    case FileRights.RIGHT_VIEW:
                        rt.Add("View");
                        break;
                    case FileRights.RIGHT_EDIT:
                        rt.Add("Edit");
                        break;
                    case FileRights.RIGHT_PRINT:
                        rt.Add("Print");
                        break;
                    case FileRights.RIGHT_CLIPBOARD:
                        rt.Add("Clipboard");
                        break;
                    case FileRights.RIGHT_SAVEAS:
                        rt.Add("SaveAs");
                        break;
                    case FileRights.RIGHT_DECRYPT:
                        rt.Add("Decrypt");
                        break;
                    case FileRights.RIGHT_SCREENCAPTURE:
                        rt.Add("ScreenCapture");
                        break;
                    case FileRights.RIGHT_SEND:
                        rt.Add("Send");
                        break;
                    case FileRights.RIGHT_CLASSIFY:
                        rt.Add("Classify");
                        break;
                    case FileRights.RIGHT_SHARE:
                        rt.Add("Share");
                        break;
                    case FileRights.RIGHT_DOWNLOAD:
                        // as PM required Windows platform must regard download as SaveAS
                        rt.Add("SaveAs");
                        break;
                }
            }


            if (bAddIfHasWatermark)
            {
                rt.Add("Watermark");
            }
            if (bForceAddValidity)
            {
                //
                // comments, by current design, requreied to add the riths "Validity" compulsorily 
                //
                rt.Add("Validity");
            }

            return rt;
        }

        // not safe, should not used , this is a work around forced by Product Requirement
        // other feature MUST NOT use it.
        public static bool PeekHasValidAdhocSection(string path)
        {
            try
            {
                using (var s = File.OpenRead(path))
                {
                    s.Seek(0x2000, System.IO.SeekOrigin.Begin);
                    byte[] buf = new byte[3] { 0, 0, 0 };
                    s.Read(buf, 0, 2);
                    if (buf[0] == 0x7B && buf[1] == 0x7D)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return true;
        }

    }
}
