// oeinstca.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include <msi.h>
#include <msiquery.h>
#include <stdio.h>
#include <Winreg.h>
#include <Shlwapi.h>
#include <shellapi.h>
#include <fstream>
#include <iostream>
#include <tlhelp32.h>
#include <tuple>
#include <string>
#include <list>
#include <utility>
#include <vector>

using namespace std;

#define MAX_BUFFER 1024
#define PRODUCT_NAME L"SkyDRM Desktop"

// RPM security token

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

//Note:  Messagebox can not use in defered execution since not be able to get UILevel property
UINT _stdcall MessageAndLogging(MSIHANDLE hInstall, BOOL bLogOnly, const WCHAR* wstrMsg )
{
	if(bLogOnly == FALSE && hInstall!= NULL)
	{
		INT nUILevel =0;
		WCHAR wstrTemp[2] = {0};
		DWORD dwBufsize = 0;
		
		dwBufsize = sizeof(wstrTemp)/sizeof(WCHAR);	
		if(ERROR_SUCCESS == MsiGetProperty(hInstall, TEXT("UILevel"), wstrTemp, &dwBufsize))
		{
			nUILevel = _wtoi(wstrTemp);
		}

		if(nUILevel > 2)
		{
			MessageBox(GetForegroundWindow(),(LPCWSTR) wstrMsg, (LPCWSTR)PRODUCT_NAME, MB_OK|MB_ICONWARNING);	
		}
	}

	//add log here
	PMSIHANDLE hRecord = MsiCreateRecord(1);
	if(hRecord !=NULL)
	{
		MsiRecordSetString(hRecord, 0, wstrMsg);
		// send message to running installer
		MsiProcessMessage(hInstall, INSTALLMESSAGE_INFO, hRecord);
		MsiCloseHandle(hRecord);
	}

	
	return ERROR_SUCCESS;
}//return service current status, or return 0 for service not existed


BOOL SHCopy(LPCWSTR from, LPCWSTR to, BOOL bDeleteFrom)
{
	SHFILEOPSTRUCT fileOp = {0};
	WCHAR newFrom[MAX_PATH];
	WCHAR newTo[MAX_PATH];

	if(bDeleteFrom)
		fileOp.wFunc = FO_MOVE;
	else
		fileOp.wFunc = FO_COPY;

	fileOp.fFlags = FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR;

	wcscpy_s(newFrom, from);
	newFrom[wcslen(from) + 1] = NULL;
	fileOp.pFrom = newFrom;

	wcscpy_s(newTo, to);
	newTo[wcslen(to) + 1] = NULL;
	fileOp.pTo = newTo;

	int result = SHFileOperation(&fileOp);

	return result == 0;
}

BOOL SHDelete(LPCWSTR strFile)
{
	SHFILEOPSTRUCT fileOp = { 0 };
	WCHAR newFrom[MAX_PATH] = { 0 };

	fileOp.wFunc = FO_DELETE;
	fileOp.fFlags = FOF_MULTIDESTFILES | FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR;

	wcscpy_s(newFrom, strFile);
	newFrom[wcslen(strFile) + 1] = NULL;
	fileOp.pFrom = newFrom;
	fileOp.pTo = NULL;
	fileOp.fAnyOperationsAborted = true;

	int result = SHFileOperation(&fileOp);

	return result == 0;
}

void ReplaceString(std::wstring &s, const std::wstring &to_find, const std::wstring &replace_with)
{
	std::wstring result;
	std::wstring::size_type pos = 0;
	while(1)
	{
		std::wstring::size_type next = s.find(to_find, pos);
		result.append(s, pos, next-pos);
		if(next != std::wstring::npos)
		{
			result.append(replace_with);
			pos = next + to_find.size();
		}
		else
			break;
	}
	s.swap(result);
	return;	
}


int ReplaceStringInFile(wfstream &inFile, wfstream &outFile, const std::wstring &to_find, const std::wstring &repl_with)
{	
	wchar_t strReplace[MAX_BUFFER];	
	while(!inFile.eof())
	{
		inFile.getline(strReplace,MAX_BUFFER,'\n');
		wstring s;
		s = strReplace;
		if(!s.empty())
		{
			ReplaceString( s, to_find, repl_with) ;	
		}
		outFile <<s <<endl;
	}
	return 1;
}


std::wstring GetClassPath(const std::wstring &classid)
{
	std::wstring apppath;
	DWORD size = 2048;
	INSTALLSTATE installstate;
	WCHAR *sPath;

	sPath = new WCHAR[size];
	installstate = MsiLocateComponent(classid.c_str(), (LPWSTR)sPath, &size);

	if ((installstate == INSTALLSTATE_LOCAL) ||
		(installstate == INSTALLSTATE_SOURCE))
		apppath = sPath;
	delete sPath;
	return apppath;
}

std::wstring GetApplicationPath(const std::wstring &appname)
{
	std::wstring classid;
	std::wstring appclassid = appname + L"\\CLSID";
	HKEY hKey = NULL;
	DWORD dwErrorCode = 0;
	WCHAR wstrData[MAX_PATH + 1] = { 0 };

	// [HKEY_CLASSES_ROOT\Word.Application\CLSID]
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CLASSES_ROOT,
		appclassid.c_str(),
		0,
		KEY_READ,
		&hKey))
	{
		DWORD dwBufsize = sizeof(wstrData) * sizeof(WCHAR);
		dwErrorCode = RegQueryValueEx(hKey,
			TEXT(""),
			NULL,
			NULL,
			(LPBYTE)wstrData,
			&dwBufsize);

		if (ERROR_SUCCESS == dwErrorCode)
		{
			classid = wstrData;
		}

		RegCloseKey(hKey);
	}

	return GetClassPath(classid);
}

//*****************************************************************************************************
//				START MSI CUSTOM ACTION FUNCTION HERE
//*****************************************************************************************************

//////////////////////////////////////////////////////////////////////////
bool DelFolder(const wchar_t *cFilePath, MSIHANDLE hInstall)
{
    bool bDel = true;
    WIN32_FIND_DATA data;
    HANDLE hFind;
    wchar_t cFullPath[512] = { 0 };
    wchar_t cNewPath[512] = { 0 };
    wchar_t wstrMsg[128] = { 0 };
    wsprintfW(cFullPath, L"%s\\*.*", cFilePath);
    hFind = FindFirstFile(cFullPath, &data);
    do
    {
        if ((!wcscmp(L".", data.cFileName)) || (!wcscmp(L"..", data.cFileName)))
        {
            continue;
        }
        if (data.dwFileAttributes == FILE_ATTRIBUTE_DIRECTORY)
        {
            wsprintfW(cNewPath, L"%s\\%s", cFilePath, data.cFileName);
            DelFolder(cNewPath,hInstall);
        }
        else
        {
            wsprintfW(cFullPath, L"%s\\%s", cFilePath, data.cFileName);

			wchar_t wstrFileName[MAX_PATH + 1] = { 0 };
			wcscpy_s(wstrFileName, MAX_PATH + 1, data.cFileName);
			_wcslwr_s(wstrFileName, wcslen(wstrFileName) + 1);
			wchar_t* pStrFind = wcsstr(wstrFileName, L"celog");			if (nullptr == pStrFind)			{
				WCHAR wstrMsg[512] = { 0 };
				swprintf_s(wstrMsg, 512, L"NXPCLOG: delete the file: %s", cFullPath);
				MessageAndLogging(hInstall, TRUE, wstrMsg);

				if (!DeleteFile(cFullPath))
				{
					MessageAndLogging(hInstall, TRUE, L"DeleteFile failed, call MoveFile to delay delete");
					MoveFileEx(cFullPath, 0, MOVEFILE_DELAY_UNTIL_REBOOT);
					bDel = false;
				}
			}
			else
			{
				WCHAR wstrMsg[512] = { 0 };
				swprintf_s(wstrMsg, 512, L"NXPCLOG: Delay delete the file: %s", cFullPath);
				MessageAndLogging(hInstall, TRUE, wstrMsg);
				MoveFileEx(cFullPath, 0, MOVEFILE_DELAY_UNTIL_REBOOT);
				bDel = false;
			}
        }

    } while (FindNextFile(hFind, &data));
    if (!RemoveDirectory(cFilePath))
    {
        MoveFileEx(cFilePath, 0, MOVEFILE_DELAY_UNTIL_REBOOT);
    }
    return bDel;
}

//////////////////////////////////////////////////////////////////////////

UINT __stdcall SetEnv(MSIHANDLE hInstall, const wchar_t *cPath)
{
    MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Start to Set Environment Variable. "));

	WCHAR wstrData[1024] = {0};
	WCHAR wstrMsg[128] = {0};
	DWORD dwErrorCode = 0;

	wcscpy_s(wstrData, MAX_PATH + 1, cPath);
	MessageAndLogging(hInstall, TRUE, (LPCWSTR)wstrData);

	DWORD buffSize = 65535;
	WCHAR buffer[65535] = {0};
    if (GetEnvironmentVariable(L"Path", buffer, buffSize) == NULL)
    {
		dwErrorCode = GetLastError();
		swprintf_s(wstrMsg, 128, L"Failed to get current Environment variable. Error Code: %d", dwErrorCode);
		MessageAndLogging(hInstall, TRUE, (LPCWSTR)wstrMsg);
		return ERROR_INSTALL_FAILURE;
    }
   
	wstring strPath;
	strPath = buffer;
	strPath +=  L";";
	strPath += wstrData;

	MessageAndLogging(hInstall, TRUE, (LPCWSTR)strPath.c_str());

	BOOL bSet = false;
	bSet = SetEnvironmentVariable(L"Path", (LPCWSTR)strPath.c_str());

	if (!bSet) 
    {
        dwErrorCode = GetLastError();
		swprintf_s(wstrMsg, 128, L"Failed to set environment variable. Error Code: %d", dwErrorCode);
		MessageAndLogging(hInstall, TRUE, (LPCWSTR)wstrMsg);
		return ERROR_INSTALL_FAILURE;
    }
	

	 MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Set Environment Variable completed. "));

	return ERROR_SUCCESS;
}

UINT __stdcall RefreshEnv(MSIHANDLE hInstall)
{
	HKEY hKey = NULL;
	WCHAR wstrInstPC[MAX_PATH + 1] = { 0 };
	WCHAR wstrPCBin[MAX_PATH + 1] = { 0 };
	WCHAR wstrCommBin32[MAX_PATH + 1] = { 0 };
	WCHAR wstrCommBin64[MAX_PATH + 1] = { 0 };
	WCHAR wstrMsg[512] = { 0 };
	DWORD dwErrorCode = 0;

	//get PC installed path
	BOOL bFoundInstDir = FALSE;
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE,
		TEXT("SOFTWARE\\NextLabs\\Compliant Enterprise\\Policy Controller\\"),
		0,
		KEY_READ,
		&hKey))
	{
		DWORD dwBufsize = sizeof(wstrInstPC)*sizeof(WCHAR);
		if (ERROR_SUCCESS == RegQueryValueEx(hKey,
			TEXT("InstallDir"),
			NULL,
			NULL,
			(LPBYTE)wstrInstPC,
			&dwBufsize))
		{
			bFoundInstDir = TRUE;
		}
		RegCloseKey(hKey);
	}

	if (bFoundInstDir)
	{
		MessageAndLogging(hInstall, TRUE, TEXT("The install path was found. "));

		wcscpy_s(wstrCommBin32, MAX_PATH + 1, wstrInstPC);
		wcscat_s(wstrCommBin32, L"Common\\bin32\\");

		wcscpy_s(wstrCommBin64, MAX_PATH + 1, wstrInstPC);
		wcscat_s(wstrCommBin64, L"Common\\bin64\\");

		wcscpy_s(wstrPCBin, MAX_PATH + 1, wstrInstPC);
		wcscat_s(wstrPCBin, L"Policy Controller\\bin\\");

		SetEnv(hInstall, wstrPCBin);
		SetEnv(hInstall, wstrCommBin32);
		SetEnv(hInstall, wstrCommBin64);
	}

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Start to Refresh Environment Variable. "));

	DWORD dwReturnValue = 0;
	SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, 0, (LPARAM)TEXT("Environment"), SMTO_ABORTIFHUNG, 5000, (PDWORD_PTR)&dwReturnValue);

	if (dwReturnValue == 0)
	{
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Refresh Environment Variable failed. "));
		return ERROR_SUCCESS;
	}

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Refresh Environment Variable completed. "));

	return ERROR_SUCCESS;
}


std::wstring RegistryQueryValue(HKEY hKey,
	LPCTSTR szName)
{
	std::wstring value;

	DWORD dwType;
	DWORD dwSize = 0;

	if (::RegQueryValueEx(
		hKey,                   // key handle
		szName,                 // item name
		NULL,                   // reserved
		&dwType,                // type of data stored
		NULL,                   // no data buffer
		&dwSize                 // required buffer size
	) == ERROR_SUCCESS && dwSize > 0)
	{
		value.resize(dwSize);

		::RegQueryValueEx(
			hKey,                   // key handle
			szName,                 // item name
			NULL,                   // reserved
			&dwType,                // type of data stored
			(LPBYTE)&value[0],      // data buffer
			&dwSize                 // available buffer size
		);
	}

	return value;
}

std::wstring RegistryEnumProductCode(std::wstring ProductName)
{
	std::wstring ProductCode;
	HKEY hKey;
	LONG ret = ::RegOpenKeyEx(
		HKEY_LOCAL_MACHINE,     // local machine hive
		TEXT("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall"), // uninstall key
		0,                      // reserved
		KEY_READ,               // desired access
		&hKey                   // handle to the open key
	);

	if (ret != ERROR_SUCCESS)
		return ProductCode;

	DWORD dwIndex = 0;
	DWORD cbName = 1024;
	TCHAR szSubKeyName[1024];

	while ((ret = ::RegEnumKeyEx(
		hKey,
		dwIndex,
		szSubKeyName,
		&cbName,
		NULL,
		NULL,
		NULL,
		NULL)) != ERROR_NO_MORE_ITEMS)
	{
		if (ret == ERROR_SUCCESS)
		{
			HKEY hItem;
			if (::RegOpenKeyEx(hKey, szSubKeyName, 0, KEY_READ, &hItem) != ERROR_SUCCESS)
				continue;

			std::wstring name = RegistryQueryValue(hItem, TEXT("DisplayName"));
			if (!name.empty() && name == ProductName)
			{
				ProductCode = szSubKeyName;
				::RegCloseKey(hItem);
				break;
			}

			::RegCloseKey(hItem);
		}
		dwIndex++;
		cbName = 1024;
	}
	::RegCloseKey(hKey);

	return ProductCode;
}

void replaceAll(std::wstring& str, const std::wstring& from, const std::wstring& to) {
	if (from.empty())
		return;
	size_t start_pos = 0;
	while ((start_pos = str.find(from, start_pos)) != std::string::npos) {
		str.replace(start_pos, from.length(), to);
		start_pos += to.length(); // In case 'to' contains 'from', like replacing 'x' with 'yx'
	}
}

std::wstring ReverseCode(std::wstring Code)
{
	std::vector<int> Pattern = { 8, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2 };
	std::wstring inputString = Code;
	std::wstring returnString;
	if (Code.size() <= 0)
		return returnString;

	replaceAll(inputString, TEXT("-"), TEXT(""));

	int index = 0;
	for (int length : Pattern)
	{
		std::wstring substring = inputString.substr(index, length);
		std::reverse(substring.begin(), substring.end());
		returnString += substring;
		index += length;
	}

	return returnString;
}

std::wstring ConvertToRegistryFormat(std::wstring productCode)
{
	return ReverseCode(productCode);
}

std::wstring ConvertFromRegistryFormat(std::wstring upgradeCode)
{
	std::wstring _upgradeCode;
	if (upgradeCode.size() != 32)
		return _upgradeCode;

	_upgradeCode = ReverseCode(upgradeCode);
	_upgradeCode.insert(8, TEXT("-"));
	_upgradeCode.insert(13, TEXT("-"));
	_upgradeCode.insert(18, TEXT("-"));
	_upgradeCode.insert(23, TEXT("-"));
	return _upgradeCode;
}

#define MAX_KEY_LENGTH 255
#define MAX_VALUE_NAME 16383

std::wstring RegistryEnumUpgradeCode(std::wstring ProductCode)
{
	std::wstring UpgradeCode;
	std::wstring rProductCode = ConvertToRegistryFormat(ProductCode);
	HKEY hKey;
	LONG ret = ::RegOpenKeyEx(
		HKEY_LOCAL_MACHINE,     // local machine hive
		TEXT("Software\\Microsoft\\Windows\\CurrentVersion\\Installer\\UpgradeCodes"), // upgradecode key
		0,                      // reserved
		KEY_READ,               // desired access
		&hKey                   // handle to the open key
	);

	if (ret != ERROR_SUCCESS)
		return UpgradeCode;

	DWORD dwIndex = 0;
	DWORD cbName = 1024;
	TCHAR szSubKeyName[1024];
	BOOL bFound = false;

	while ((ret = ::RegEnumKeyEx(
		hKey,
		dwIndex,
		szSubKeyName,
		&cbName,
		NULL,
		NULL,
		NULL,
		NULL)) != ERROR_NO_MORE_ITEMS)
	{
		if (ret == ERROR_SUCCESS)
		{
			HKEY hItem;
			if (::RegOpenKeyEx(hKey, szSubKeyName, 0, KEY_READ, &hItem) != ERROR_SUCCESS)
				continue;

			// Get the class name and the value count. 
			TCHAR  achValue[MAX_VALUE_NAME];
			DWORD cchValue = MAX_VALUE_NAME;
			TCHAR    achClass[MAX_PATH] = TEXT("");  // buffer for class name 
			DWORD    cchClassName = MAX_PATH;  // size of class string 
			DWORD    cSubKeys = 0;               // number of subkeys 
			DWORD    cbMaxSubKey;              // longest subkey size 
			DWORD    cchMaxClass;              // longest class string 
			DWORD    cValues;              // number of values for key 
			DWORD    cchMaxValue;          // longest value name 
			DWORD    cbMaxValueData;       // longest value data 
			DWORD    cbSecurityDescriptor; // size of security descriptor 
			FILETIME ftLastWriteTime;      // last write time 

			ret = ::RegQueryInfoKey(
				hKey,                    // key handle 
				achClass,                // buffer for class name 
				&cchClassName,           // size of class string 
				NULL,                    // reserved 
				&cSubKeys,               // number of subkeys 
				&cbMaxSubKey,            // longest subkey size 
				&cchMaxClass,            // longest class string 
				&cValues,                // number of values for this key 
				&cchMaxValue,            // longest value name 
				&cbMaxValueData,         // longest value data 
				&cbSecurityDescriptor,   // security descriptor 
				&ftLastWriteTime);       // last write time 

			for (DWORD i = 0, ret = ERROR_SUCCESS; i < cValues; i++)
			{
				cchValue = MAX_VALUE_NAME;
				achValue[0] = '\0';
				ret = ::RegEnumValue(hKey, i,
					achValue,
					&cchValue,
					NULL,
					NULL,
					NULL,
					NULL);

				if (ret == ERROR_SUCCESS)
				{
					if (achValue == rProductCode)
					{
						bFound = true;
						UpgradeCode = ConvertFromRegistryFormat(std::wstring(szSubKeyName));
						break;
					}
				}
			}

			::RegCloseKey(hItem);
			if (bFound)
				break;
		}

		dwIndex++;
		cbName = 1024;
	}
	::RegCloseKey(hKey);

	return UpgradeCode;
}

std::vector<std::wstring> split(std::wstring str, std::wstring token) {
	std::vector<std::wstring> result;
	while (str.size()) {
		size_t index = str.find(token);
		if (index != std::wstring::npos) {
			result.push_back(str.substr(0, index));
			str = str.substr(index + token.size());
			if (str.size() == 0)result.push_back(str);
		}
		else {
			result.push_back(str);
			str = L"";
		}
	}
	return result;
}

UINT __stdcall InstallRPM(MSIHANDLE hInstall)
{
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: InstallRPM started. "));

	std::wstring installdir;
	std::wstring RPMUpgradeCode;
	std::wstring RPMProductCode;
	WCHAR wstrTemp[2018] = { 0 };
	DWORD dwBufsize = 0;

	dwBufsize = sizeof(wstrTemp) / sizeof(WCHAR);
	if (ERROR_SUCCESS == MsiGetProperty(hInstall, TEXT("CustomActionData"), wstrTemp, &dwBufsize))
	{
		// C:\Program Files\NextLabs\|{RMDUPGRADECODE-8888-8888-8888-88888888}|{RMDPRODUCTCODE-8888-8888-8888-88888888}
		std::wstring customdata(wstrTemp);
		std::vector<std::wstring> strdatas = split(customdata, L";");
		if (strdatas.size() != 3)
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: failed to get the correct data from installer"));//log only	
			MessageAndLogging(hInstall, TRUE, customdata.c_str());//log only	
			return ERROR_SUCCESS;
		}

		installdir = strdatas[0];
		RPMUpgradeCode = strdatas[1];
		RPMProductCode = strdatas[2];

		if (installdir.size() > 0)
		{
			if (installdir.substr(installdir.size() - 1, installdir.size()) == L"\\")
				installdir.erase(installdir.end() - 1);
		}
	}
	else
	{
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: failed to get the data from installer"));//log only	
		return ERROR_SUCCESS;
	}

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RPMUPGRADECODE + RPMPRODUCTCODE: "));
	MessageAndLogging(hInstall, TRUE, RPMUpgradeCode.c_str());
	MessageAndLogging(hInstall, TRUE, RPMProductCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Install DIR: "));
	MessageAndLogging(hInstall, TRUE, installdir.c_str());

	std::wstring oldRPMUpgradeCode;
	std::wstring oldRPMProductCode;

	oldRPMProductCode = RegistryEnumProductCode(TEXT("SkyDRM Rights Protection Manager"));
	oldRPMUpgradeCode = RegistryEnumUpgradeCode(oldRPMProductCode);
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: EXISTING RPMUPGRADECODE: "));
	MessageAndLogging(hInstall, TRUE, oldRPMUpgradeCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: EXISTING RPMPRODUCTCODE: "));
	MessageAndLogging(hInstall, TRUE, oldRPMProductCode.c_str());

	if (RPMProductCode == oldRPMProductCode)
	{
		// already installed, no need to reinstall
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RPM is already installed. "));
		return ERROR_SUCCESS;
	}
	else if (oldRPMProductCode.size() > 0)
	{
		// old RPM exists, need to remove it
		// 
		// msiexec /qn /x {product code} /noretart REBOOT=ReallySuppress 
		//
		WCHAR cmd[MAX_PATH];
		size_t nSize = sizeof(cmd);
		if (_wgetenv_s(&nSize, cmd, L"COMSPEC") != 0)
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: COMSPEC not found. "));
			return ERROR_SUCCESS;
		}

		std::wstring cmdArgs;
		// cmdArgs += std::wstring(cmd) + L" /c msiexec /x {" + oldRPMProductCode + L"} /qn REBOOT=ReallySuppress";
		cmdArgs += L"msiexec /x {" + oldRPMProductCode + L"} /qn REBOOT=ReallySuppress";
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: cmdArgs: "));
		MessageAndLogging(hInstall, TRUE, cmdArgs.c_str());

		WCHAR cmdArgsBuf[2048];
		wcscpy_s(cmdArgsBuf, cmdArgs.c_str());

		STARTUPINFO si;
		ZeroMemory(&si, sizeof si);
		si.cb = sizeof si;
		si.dwFlags = STARTF_USESHOWWINDOW;
		si.wShowWindow = SW_HIDE;
		PROCESS_INFORMATION pi;
		ZeroMemory(&pi, sizeof pi);

		BOOL processCreated = CreateProcess(NULL, cmdArgsBuf, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);

		if (processCreated)
		{
			DWORD dwWaitResult = WaitForSingleObject(pi.hProcess, INFINITE);

			// Close process and thread handles. 
			CloseHandle(pi.hThread);
			CloseHandle(pi.hProcess);

			// if the process finished, we break out
			if (dwWaitResult == WAIT_OBJECT_0)
			{
			}
			else
			{
				MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: msiexec failed. "));
				return ERROR_SUCCESS;
			}
		}

		// set reboot flag
		// we need to reboot
	}

	// no old RPM or remove it, install it
	// msiexec /i <MSI Path> /qn  /noretart REBOOT=ReallySuppress
	std::wstring msipath = installdir + L"\\SkyDRM\\installer\\SkyDRM Rights Protection Manager.msi";

	WCHAR cmd[MAX_PATH];
	size_t nSize = sizeof(cmd);
	if (_wgetenv_s(&nSize, cmd, L"COMSPEC") != 0)
	{
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: COMSPEC not found. "));
		return ERROR_SUCCESS;
	}

	std::wstring cmdArgs;
	//cmdArgs += std::wstring(cmd) + L" /c msiexec /i \"" + msipath + L"\" /qn REBOOT=ReallySuppress";
	cmdArgs += L"msiexec /i \"" + msipath + L"\" /qn REBOOT=ReallySuppress";
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: cmdArgs: "));
	MessageAndLogging(hInstall, TRUE, cmdArgs.c_str());

	WCHAR cmdArgsBuf[2048];
	wcscpy_s(cmdArgsBuf, cmdArgs.c_str());

	STARTUPINFO si;
	ZeroMemory(&si, sizeof si);
	si.cb = sizeof si;
	si.dwFlags = STARTF_USESHOWWINDOW;
	si.wShowWindow = SW_HIDE;
	PROCESS_INFORMATION pi;
	ZeroMemory(&pi, sizeof pi);

	BOOL processCreated = CreateProcess(NULL, cmdArgsBuf, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);

	if (processCreated)
	{
		DWORD dwWaitResult = WaitForSingleObject(pi.hProcess, INFINITE);

		// Close process and thread handles. 
		CloseHandle(pi.hThread);
		CloseHandle(pi.hProcess);

		// if the process finished, we break out
		if (dwWaitResult == WAIT_OBJECT_0)
		{
		}
		else
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: msiexec failed. "));
			return ERROR_SUCCESS;
		}
	}

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: InstallRPM finished. "));
	return ERROR_SUCCESS;
}

UINT __stdcall UnInstallRPM(MSIHANDLE hInstall)
{
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: UnInstallRPM started. "));

	std::wstring installdir;
	std::wstring RPMUpgradeCode;
	std::wstring RPMProductCode;
	WCHAR wstrTemp[2018] = { 0 };
	DWORD dwBufsize = 0;

	dwBufsize = sizeof(wstrTemp) / sizeof(WCHAR);
	if (ERROR_SUCCESS == MsiGetProperty(hInstall, TEXT("CustomActionData"), wstrTemp, &dwBufsize))
	{
		// C:\Program Files\NextLabs\|{RMDUPGRADECODE-8888-8888-8888-88888888}|{RMDPRODUCTCODE-8888-8888-8888-88888888}
		std::wstring customdata(wstrTemp);
		std::vector<std::wstring> strdatas = split(customdata, L";");
		if (strdatas.size() != 3)
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: failed to get the correct data from installer"));//log only	
			MessageAndLogging(hInstall, TRUE, customdata.c_str());//log only	
			return ERROR_SUCCESS;
		}

		installdir = strdatas[0];
		RPMUpgradeCode = strdatas[1];
		RPMProductCode = strdatas[2];

		if (installdir.size() > 0)
		{
			if (installdir.substr(installdir.size() - 1, installdir.size()) == L"\\")
				installdir.erase(installdir.end() - 1);
		}
	}
	else
	{
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: failed to get the data from installer"));//log only	
		return ERROR_SUCCESS;
	}
	
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RPMUPGRADECODE: "));
	MessageAndLogging(hInstall, TRUE, RPMUpgradeCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RPMPRODUCTCODE: "));
	MessageAndLogging(hInstall, TRUE, RPMProductCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Install DIR: "));
	MessageAndLogging(hInstall, TRUE, installdir.c_str());

	std::wstring oldRPMUpgradeCode;
	std::wstring oldRPMProductCode;

	oldRPMProductCode = RegistryEnumProductCode(TEXT("SkyDRM Rights Protection Manager"));
	oldRPMUpgradeCode = RegistryEnumUpgradeCode(oldRPMProductCode);
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: EXISTING RPMUPGRADECODE: "));
	MessageAndLogging(hInstall, TRUE, oldRPMUpgradeCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: EXISTING RPMPRODUCTCODE: "));
	MessageAndLogging(hInstall, TRUE, oldRPMProductCode.c_str());

	if (oldRPMProductCode.size() > 0)
	{
		// old RPM exists, need to remove it
		// 
		// msiexec /qn /x {product code} /noretart REBOOT=ReallySuppress 
		//
		WCHAR cmd[MAX_PATH];
		size_t nSize = sizeof(cmd);
		if (_wgetenv_s(&nSize, cmd, L"COMSPEC") != 0)
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: COMSPEC not found. "));
			return ERROR_SUCCESS;
		}

		std::wstring cmdArgs;
		// cmdArgs += std::wstring(cmd) + L" /c msiexec /x {" + oldRPMProductCode + L"} /qn REBOOT=ReallySuppress";
		cmdArgs += L"msiexec /x {" + oldRPMProductCode + L"} /qn REBOOT=ReallySuppress";
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: cmdArgs: "));
		MessageAndLogging(hInstall, TRUE, cmdArgs.c_str());

		WCHAR cmdArgsBuf[2048];
		wcscpy_s(cmdArgsBuf, cmdArgs.c_str());

		STARTUPINFO si;
		ZeroMemory(&si, sizeof si);
		si.cb = sizeof si;
		si.dwFlags = STARTF_USESHOWWINDOW;
		si.wShowWindow = SW_HIDE;
		PROCESS_INFORMATION pi;
		ZeroMemory(&pi, sizeof pi);

		BOOL processCreated = CreateProcess(NULL, cmdArgsBuf, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);

		if (processCreated)
		{
			DWORD dwWaitResult = WaitForSingleObject(pi.hProcess, INFINITE);

			// Close process and thread handles. 
			CloseHandle(pi.hThread);
			CloseHandle(pi.hProcess);

			// if the process finished, we break out
			if (dwWaitResult == WAIT_OBJECT_0)
			{
			}
			else
			{
				MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: msiexec failed. "));
				return ERROR_SUCCESS;
			}
		}

		// set reboot flag
		// we need to reboot
	}

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: UnInstallRPM finished. "));
	return ERROR_SUCCESS;
}

UINT __stdcall InstallRMD(MSIHANDLE hInstall)
{
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: InstallRMD started. "));

	std::wstring installdir;
	std::wstring RMDUpgradeCode;
	std::wstring RMDProductCode;
	WCHAR wstrTemp[2018] = { 0 };
	DWORD dwBufsize = 0;

	dwBufsize = sizeof(wstrTemp) / sizeof(WCHAR);
	if (ERROR_SUCCESS == MsiGetProperty(hInstall, TEXT("CustomActionData"), wstrTemp, &dwBufsize))
	{
		// C:\Program Files\NextLabs\|{RMDUPGRADECODE-8888-8888-8888-88888888}|{RMDPRODUCTCODE-8888-8888-8888-88888888}
		std::wstring customdata(wstrTemp);
		std::vector<std::wstring> strdatas = split(customdata, L";");
		if (strdatas.size() != 3)
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: failed to get the correct data from installer"));//log only	
			MessageAndLogging(hInstall, TRUE, customdata.c_str());//log only	
			return ERROR_SUCCESS;
		}

		installdir = strdatas[0];
		RMDUpgradeCode = strdatas[1];
		RMDProductCode = strdatas[2];

		if (installdir.size() > 0)
		{
			if (installdir.substr(installdir.size() - 1, installdir.size()) == L"\\")
				installdir.erase(installdir.end() - 1);
		}
	}
	else
	{
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: failed to get the data from installer"));//log only	
		return ERROR_SUCCESS;
	} 

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RMDUPGRADECODE: "));
	MessageAndLogging(hInstall, TRUE, RMDUpgradeCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RMDPRODUCTCODE: "));
	MessageAndLogging(hInstall, TRUE, RMDProductCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Install DIR: "));
	MessageAndLogging(hInstall, TRUE, installdir.c_str());

	std::wstring oldRMDUpgradeCode;
	std::wstring oldRMDProductCode;

	oldRMDProductCode = RegistryEnumProductCode(TEXT("SkyDRM Desktop for Windows"));
	oldRMDUpgradeCode = RegistryEnumUpgradeCode(oldRMDProductCode);
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: EXISTING RMDUPGRADECODE: "));
	MessageAndLogging(hInstall, TRUE, oldRMDUpgradeCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: EXISTING RMDPRODUCTCODE: "));
	MessageAndLogging(hInstall, TRUE, oldRMDProductCode.c_str());
	
	if (RMDProductCode == oldRMDProductCode)
	{
		// already installed, no need to reinstall
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RMD is already installed. "));
		return ERROR_SUCCESS;
	}
	else if (oldRMDProductCode.size() > 0)
	{
		// old RMD exists, need to remove it
		// 
		// msiexec /qn /x {product code} /noretart REBOOT=ReallySuppress 
		//
		WCHAR cmd[MAX_PATH];
		size_t nSize = sizeof(cmd);
		if (_wgetenv_s(&nSize, cmd, L"COMSPEC") != 0)
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: COMSPEC not found. "));
			return ERROR_SUCCESS;
		}

		std::wstring cmdArgs;
		// cmdArgs += std::wstring(cmd) + L" /c msiexec /x {" + oldRMDProductCode + L"} /qn REBOOT=ReallySuppress";
		cmdArgs += L"msiexec /x {" + oldRMDProductCode + L"} /qn REBOOT=ReallySuppress";
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: cmdArgs: "));
		MessageAndLogging(hInstall, TRUE, cmdArgs.c_str());

		WCHAR cmdArgsBuf[2048];
		wcscpy_s(cmdArgsBuf, cmdArgs.c_str());

		STARTUPINFO si;
		ZeroMemory(&si, sizeof si);
		si.cb = sizeof si;
		si.dwFlags = STARTF_USESHOWWINDOW;
		si.wShowWindow = SW_HIDE;
		PROCESS_INFORMATION pi;
		ZeroMemory(&pi, sizeof pi);

		BOOL processCreated = CreateProcess(NULL, cmdArgsBuf, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);

		if (processCreated)
		{
			DWORD dwWaitResult = WaitForSingleObject(pi.hProcess, INFINITE);

			// Close process and thread handles. 
			CloseHandle(pi.hThread);
			CloseHandle(pi.hProcess);

			// if the process finished, we break out
			if (dwWaitResult == WAIT_OBJECT_0)
			{
			}
			else
			{
				MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: msiexec failed. "));
				return ERROR_SUCCESS;
			}
		}

		// set reboot flag
		// we need to reboot
	}

	// no old RMD or remove it, install it
	// msiexec /i <MSI Path> /qn  /noretart REBOOT=ReallySuppress
	std::wstring msipath = installdir + L"\\SkyDRM\\installer\\SkyDRM Desktop.msi";

	WCHAR cmd[MAX_PATH];
	size_t nSize = sizeof(cmd);
	if (_wgetenv_s(&nSize, cmd, L"COMSPEC") != 0)
	{
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: COMSPEC not found. "));
		return ERROR_SUCCESS;
	}

	std::wstring cmdArgs;
	// cmdArgs += std::wstring(cmd) + L" /c msiexec /i \"" + msipath + L"\" /qn REBOOT=ReallySuppress";
	cmdArgs += L"msiexec /i \"" + msipath + L"\" /qn REBOOT=ReallySuppress";
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: cmdArgs: "));
	MessageAndLogging(hInstall, TRUE, cmdArgs.c_str());

	WCHAR cmdArgsBuf[2048];
	wcscpy_s(cmdArgsBuf, cmdArgs.c_str());

	STARTUPINFO si;
	ZeroMemory(&si, sizeof si);
	si.cb = sizeof si;
	si.dwFlags = STARTF_USESHOWWINDOW;
	si.wShowWindow = SW_HIDE;
	PROCESS_INFORMATION pi;
	ZeroMemory(&pi, sizeof pi);

	BOOL processCreated = CreateProcess(NULL, cmdArgsBuf, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);

	if (processCreated)
	{
		DWORD dwWaitResult = WaitForSingleObject(pi.hProcess, INFINITE);

		// Close process and thread handles. 
		CloseHandle(pi.hThread);
		CloseHandle(pi.hProcess);

		// if the process finished, we break out
		if (dwWaitResult == WAIT_OBJECT_0)
		{
		}
		else
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: msiexec failed. "));
			return ERROR_SUCCESS;
		}
	}

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: InstallRMD finished. "));
	return ERROR_SUCCESS;
}

UINT __stdcall UnInstallRMD(MSIHANDLE hInstall)
{
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: UnInstallRMD started. "));

	std::wstring installdir;
	std::wstring RMDUpgradeCode;
	std::wstring RMDProductCode;
	WCHAR wstrTemp[2018] = { 0 };
	DWORD dwBufsize = 0;

	dwBufsize = sizeof(wstrTemp) / sizeof(WCHAR);
	if (ERROR_SUCCESS == MsiGetProperty(hInstall, TEXT("CustomActionData"), wstrTemp, &dwBufsize))
	{
		// C:\Program Files\NextLabs\|{RMDUPGRADECODE-8888-8888-8888-88888888}|{RMDPRODUCTCODE-8888-8888-8888-88888888}
		std::wstring customdata(wstrTemp);
		std::vector<std::wstring> strdatas = split(customdata, L";");
		if (strdatas.size() != 3)
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: failed to get the correct data from installer"));//log only	
			MessageAndLogging(hInstall, TRUE, customdata.c_str());//log only	
			return ERROR_SUCCESS;
		}

		installdir = strdatas[0];
		RMDUpgradeCode = strdatas[1];
		RMDProductCode = strdatas[2];

		if (installdir.size() > 0)
		{
			if (installdir.substr(installdir.size() - 1, installdir.size()) == L"\\")
				installdir.erase(installdir.end() - 1);
		}
	}
	else
	{
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: failed to get the data from installer"));//log only	
		return ERROR_SUCCESS;
	}

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RMDUPGRADECODE: "));
	MessageAndLogging(hInstall, TRUE, RMDUpgradeCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: RMDPRODUCTCODE: "));
	MessageAndLogging(hInstall, TRUE, RMDProductCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: Install DIR: "));
	MessageAndLogging(hInstall, TRUE, installdir.c_str());

	std::wstring oldRMDUpgradeCode;
	std::wstring oldRMDProductCode;

	oldRMDProductCode = RegistryEnumProductCode(TEXT("SkyDRM Desktop for Windows"));
	oldRMDUpgradeCode = RegistryEnumUpgradeCode(oldRMDProductCode);
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: EXISTING RMDUPGRADECODE: "));
	MessageAndLogging(hInstall, TRUE, oldRMDUpgradeCode.c_str());
	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: EXISTING RMDPRODUCTCODE: "));
	MessageAndLogging(hInstall, TRUE, oldRMDProductCode.c_str());

	if (oldRMDProductCode.size() > 0)
	{
		// old RMD exists, need to remove it
		// 
		// msiexec /qn /x {product code} /noretart REBOOT=ReallySuppress 
		//
		WCHAR cmd[MAX_PATH];
		size_t nSize = sizeof(cmd);
		if (_wgetenv_s(&nSize, cmd, L"COMSPEC") != 0)
		{
			MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: COMSPEC not found. "));
			return ERROR_SUCCESS;
		}

		std::wstring cmdArgs;
		// cmdArgs += std::wstring(cmd) + L" /c msiexec /x {" + oldRMDProductCode + L"} /qn REBOOT=ReallySuppress";
		cmdArgs += L"msiexec /x {" + oldRMDProductCode + L"} /qn REBOOT=ReallySuppress";
		MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: cmdArgs: "));
		MessageAndLogging(hInstall, TRUE, cmdArgs.c_str());

		WCHAR cmdArgsBuf[2048];
		wcscpy_s(cmdArgsBuf, cmdArgs.c_str());

		STARTUPINFO si;
		ZeroMemory(&si, sizeof si);
		si.cb = sizeof si;
		si.dwFlags = STARTF_USESHOWWINDOW;
		si.wShowWindow = SW_HIDE;
		PROCESS_INFORMATION pi;
		ZeroMemory(&pi, sizeof pi);

		BOOL processCreated = CreateProcess(NULL, cmdArgsBuf, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);

		if (processCreated)
		{
			DWORD dwWaitResult = WaitForSingleObject(pi.hProcess, INFINITE);

			// Close process and thread handles. 
			CloseHandle(pi.hThread);
			CloseHandle(pi.hProcess);

			// if the process finished, we break out
			if (dwWaitResult == WAIT_OBJECT_0)
			{
			}
			else
			{
				MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: msiexec failed. "));
				return ERROR_SUCCESS;
			}
		}

		// set reboot flag
		// we need to reboot
	}

	MessageAndLogging(hInstall, TRUE, TEXT("******** NXPCLOG: UnInstallRMD finished. "));
	return ERROR_SUCCESS;
}
