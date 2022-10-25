using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Viewer.utils;

namespace Viewer.render.preview
{
    public class AcceleratePreview
    {
        public static object CurrentPreviewHandler = null;

        public static Guid CurrentPreviewHandlerGUID = Guid.Empty;

        public static string PreInitializePreviewError = string.Empty;

        private static Task<bool> TaskPreInitializePreview = null;

        public async static void OpenOfficeFileAsync(PreviewHandlerHost previewHandlerHost, string filePath)
        {
            if (null == TaskPreInitializePreview)
            {
                previewHandlerHost.Open(filePath);
            }
            else
            {
                await Task.WhenAny(TaskPreInitializePreview);
                previewHandlerHost.SpeedUpOpenOfficeFile(filePath,
                                                         CurrentPreviewHandler,
                                                         CurrentPreviewHandlerGUID,
                                                         PreInitializePreviewError);
            }
        }

        public static void PreInitializePreviewCaller(string extension)
        {
            TaskPreInitializePreview =  PreInitializePreviewAsync(extension);
        }

        private static Task<bool> PreInitializePreviewAsync(string extension)
        {
            return Task.Run<bool>(() =>
            {
                return PreInitializePreview(extension);
            });
        }


        private static bool PreInitializePreview(string extension)
        {
            bool result = false;
            try
            {
                Guid guid = GetPreviewHandlerGUID(extension);
                if (guid != Guid.Empty)
                {
                    AcceleratePreview.CurrentPreviewHandlerGUID = guid;
                    Type comType = Type.GetTypeFromCLSID(guid);
                    AcceleratePreview.CurrentPreviewHandler = Activator.CreateInstance(comType);
                    CommonUtils.RegisterProcess();
                    result = true;
                }
                else
                {
                    AcceleratePreview.PreInitializePreviewError = "No preview available.";
                }
            }
            catch (Exception ex)
            {
                ViewerApp.Log.Error("\t\t Some error happend on PreInitializePreview {0} \r\n", ex);
            }
            return result;
        }

        public static Guid GetPreviewHandlerGUID(string extension)
        {
            // open the registry key corresponding to the file extension
            RegistryKey ext = Registry.ClassesRoot.OpenSubKey(extension);
            if (ext != null)
            {
                // open the key that indicates the GUID of the preview handler type
                RegistryKey test = ext.OpenSubKey("shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
                if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));

                // sometimes preview handlers are declared on key for the class
                string className = Convert.ToString(ext.GetValue(null));
                if (className != null)
                {
                    test = Registry.ClassesRoot.OpenSubKey(className + "\\shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
                    if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));
                }
            }

            return Guid.Empty;
        }
       
        public static void Dispose()
        {
            if (CurrentPreviewHandler != null)
            {
                Marshal.FinalReleaseComObject(CurrentPreviewHandler);
            }

            if (CurrentPreviewHandler is IPreviewHandler)
            {
                try
                {
                    // explicitly unload the content
                    ((IPreviewHandler)CurrentPreviewHandler).Unload();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }

            if (CurrentPreviewHandler != null)
            {
                Marshal.FinalReleaseComObject(CurrentPreviewHandler);
                CurrentPreviewHandler = null;
                GC.Collect();
            }

        }
    }
}
