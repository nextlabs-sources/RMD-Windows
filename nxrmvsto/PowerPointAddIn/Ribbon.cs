using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using PowerPointAddIn.featureProvider;
using Office = Microsoft.Office.Core;

// TODO:  Follow these steps to enable the Ribbon (XML) item:

// 1: Copy the following code block into the ThisAddin, ThisWorkbook, or ThisDocument class.

//  protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
//  {
//      return new Ribbon();
//  }

// 2. Create callback methods in the "Ribbon Callbacks" region of this class to handle user
//    actions, such as clicking a button. Note: if you have exported this Ribbon from the Ribbon designer,
//    move your code from the event handlers to the callback methods and modify the code to work with the
//    Ribbon extensibility (RibbonX) programming model.

// 3. Assign attributes to the control tags in the Ribbon XML file to identify the appropriate callback methods in your code.  

// For more information, see the Ribbon XML documentation in the Visual Studio Tools for Office Help.


namespace PowerPointAddIn
{
    [ComVisible(true)]
    public class Ribbon : Office.IRibbonExtensibility
    {
        private Office.IRibbonUI ribbon;

        public Ribbon()
        {
        }

        #region IRibbonExtensibility Members

        public string GetCustomUI(string ribbonID)
        {
            return GetResourceText("PowerPointAddIn.Ribbon.xml");
        }

        #endregion

        #region Ribbon Callbacks
        //Create callback methods here. For more information about adding callback methods, visit https://go.microsoft.com/fwlink/?LinkID=271226

        public void Ribbon_Load(Office.IRibbonUI ribbonUI)
        {
            this.ribbon = ribbonUI;
        }

        public Image GetImage(string imgName)
        {
            Image image = null;
            switch (imgName)
            {
                case "Icon_classify":
                    image = Properties.Resources.Icon_Classify_3x;
                    break;
                case "Icon_protect":
                    image = Properties.Resources.Icon_Protect_3x;
                    break;
                case "Icon_modify":
                    image = Properties.Resources.Icon_Modify_3x;
                    break;
                case "Icon_permission":
                    image = Properties.Resources.Icon_Permission_3x;
                    break;
                default:
                    break;
            }
            return image;
        }

        /// <summary>
        /// Causes all of your custom controls to re-initialize.
        /// </summary>
        public void InvalidateCustomControl()
        {
            Debug.WriteLine("Ribbon: InvalidateCustomControl");
            ribbon.InvalidateControl("bt_Protect");
            ribbon.InvalidateControl("bt_Permission");
            //ribbon.Invalidate();
        }

        public void FileSave_Action(Office.IRibbonControl control, bool cancelDefault)
        {
            try
            {
                string path = Globals.ThisAddIn.Application.ActivePresentation.Path;

                bool saved = Globals.ThisAddIn.Application.ActivePresentation.Saved == Microsoft.Office.Core.MsoTriState.msoTrue;

                if (string.IsNullOrEmpty(path))
                {
                    cancelDefault = true;

                    //ribbon.ActivateTabMso("TabSave");

                    Globals.ThisAddIn.Application.CommandBars.ExecuteMso("FileSaveAs"); //will trigger BeforeSave event
                }
                else
                {
                    cancelDefault = true;
                    Globals.ThisAddIn.SaveAsUI = false;
                    Globals.ThisAddIn.Application.ActivePresentation.Save();
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                Globals.ThisAddIn.SaveAsUI = true;
            }
        }

        public bool Btn_GetEnable(Office.IRibbonControl control)
        {
            Debug.WriteLine("Ribbon: invoke Btn_GetEnable()," + Globals.ThisAddIn.Application.ActivePresentation.FullName);

            if (!Globals.ThisAddIn.SdkHandler.bRecoverSucceed)
            {
                Debug.WriteLine("Ribbon: bRecoverSucceed is false");
                return false;
            }

            bool result = false;
            bool isNxl = Globals.ThisAddIn.SdkHandler.IsNxlFile(Globals.ThisAddIn.Application.ActivePresentation.FullName);

            Debug.WriteLine("Is nxl file:" + isNxl);
            
            string id = control.Id;
            switch (id)
            {
                case "bt_Protect":
                    if (isNxl)
                    { result = false; }
                    else
                    { result = true; }
                    break;
                case "bt_Permission":
                    if (isNxl)
                    { result = true; }
                    else
                    { result = false; }
                    break;
            }
            return result;
        }

        public void Btn_Action(Office.IRibbonControl control)
        {
            string id = control.Id;
            switch (id)
            {
                case "bt_Classify":
                    break;
                case "bt_Protect":
                    FileProtect fileProtect = new FileProtect();
                    fileProtect.ProtectShowDialog();
                    break;
                case "bt_Modify":
                    break;
                case "bt_Permission":
                    NxlFilePermission filePermission = new NxlFilePermission();
                    filePermission.FileInfoShowFrm();
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Helpers

        private static string GetResourceText(string resourceName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] resourceNames = asm.GetManifestResourceNames();
            for (int i = 0; i < resourceNames.Length; ++i)
            {
                if (string.Compare(resourceName, resourceNames[i], StringComparison.OrdinalIgnoreCase) == 0)
                {
                    using (StreamReader resourceReader = new StreamReader(asm.GetManifestResourceStream(resourceNames[i])))
                    {
                        if (resourceReader != null)
                        {
                            return resourceReader.ReadToEnd();
                        }
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
