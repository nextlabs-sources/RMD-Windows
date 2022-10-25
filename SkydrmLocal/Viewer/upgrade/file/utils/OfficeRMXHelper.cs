using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.file.basic.utils
{
    public class OfficeRMXHelper
{
    private static List<string> CurrentUserSubKeys = new List<string>();
    private static List<string> LocalMachineSubKeys = new List<string>();
    private static UIntPtr HKEY_CURRENT_USER = (UIntPtr)((long)0x80000001);
    private static UIntPtr HKEY_LOCAL_MACHINE = (UIntPtr)((long)0x80000002);

    static OfficeRMXHelper()
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

        //LocalMachine clickToRun
        LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Microsoft\Office\Excel\Addins\NxlRmAddin");
        LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Microsoft\Office\Word\Addins\NxlRmAddin");
        LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Microsoft\Office\PowerPoint\Addins\NxlRmAddin");
        LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Wow6432Node\Microsoft\Office\Excel\Addins\NxlRmAddin");
        LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Wow6432Node\Microsoft\Office\Word\Addins\NxlRmAddin");
        LocalMachineSubKeys.Add(@"SOFTWARE\MICROSOFT\Office\ClickToRun\REGISTRY\MACHINE\SOFTWARE\Wow6432Node\Microsoft\Office\PowerPoint\Addins\NxlRmAddin");
    }

    /// <summary>
    /// Try to delete bad office Addin key-values -- fix bug 52100.
    /// -- when user try to edit office file multiple times(try to open, edit, close...), the add-in will disable in current user(only some machine).
    ///    Namely, "\HKEY_CURRENT_USER\Software\Microsoft\Office\PowerPoint\Addins\NxlRMAddin" -- LoadBehavior is 0, 
    ///    so we'll try to delete the key if exist when user edit.
    /// </summary>
    public static void ChangeRegeditOfOfficeAddin(Session session)
    {
        string name = "LoadBehavior";
        uint value = 3;

        foreach (string key in CurrentUserSubKeys)
        {
            bool rt = session.SDWL_Register_SetValue(HKEY_CURRENT_USER, key, name, value);
        }

        foreach (string key in LocalMachineSubKeys)
        {
            bool rt = session.SDWL_Register_SetValue(HKEY_LOCAL_MACHINE, key, name, value);
        }
    }
}
}
