#include "stdafx.h"
#include "nxrmaddin.h"
#include "nxrmext2.h"
#include "SDWL\SDLAPI.h"
#include <array>


#define NXRMADDIN_NAME							L"NxlRMAddin"
#define NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY	L"Software\\Microsoft\\Office\\PowerPoint\\Addins"
#define NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY		L"Software\\Microsoft\\Office\\Word\\Addins"
#define NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY		L"Software\\Microsoft\\Office\\Excel\\Addins"
#define NXRMADDIN_INSTALL_FRIENDLYNAME_VALUE	L"FriendlyName"
#define NXRMADDIN_INSTALL_LOADBEHAVIOR_VALUE	L"LoadBehavior"
#define NXRMADDIN_INSTALL_DESCRIPTION_VALUE		L"Description"

#define NXRMWORDADDIN_NAME						L"nxrmWordAddIn"
#define NXRMEXCELADDIN_NAME						L"nxrmExcelAddIn"
#define NXRMPOWERPOINTADDIN_NAME				L"nxrmPowerPointAddIn"
#define NXRMADDIN_INSTALL_MANIFEST_VALUE		L"Manifest"


const GUID CLSID_nxrmAddin = { 0xcca3189, 0xf325, 0x4d58,{ 0xab, 0x6d, 0x21, 0x2c, 0xd7, 0x6c, 0x33, 0x22 } };
extern HMODULE g_hModule;

LONG	g_unxrmaddinInstanceCount = 0;
LONG	g_unxrmext2InstanceCount = 0;

static HRESULT install_com_component(WCHAR *addin_path, const WCHAR* wszClsGuidString);
static HRESULT install_powerpoint_addin(void);
static HRESULT install_excel_addin(void);
static HRESULT install_word_addin(void);


static HRESULT uninstall_com_component(void);
static HRESULT uninstall_powerpoint_addin(void);
static HRESULT uninstall_excel_addin(void);
static HRESULT uninstall_word_addin(void);
static HRESULT ModifyWordOpenCommand(std::wstring doctype);
static HRESULT RevertWordOpenCommand(std::wstring doctype);

// RPM security token
static const std::string gRPM_Security = "{6829b159-b9bb-42fc-af19-4a6af3c9fcf6}";

std::wstring GetClassPath(const std::wstring &classid)
{
	std::wstring apppath;
	std::wstring appclassid = L"CLSID\\" + classid + L"\\LocalServer32";
	std::wstring appclassid32 = L"WOW6432Node\\CLSID\\" + classid + L"\\LocalServer32";

	// Computer\HKEY_CLASSES_ROOT\WOW6432Node\CLSID\{91493441-5A91-11CF-8700-00AA0060263B}\LocalServer32
	HKEY hKey = NULL;
	DWORD dwErrorCode = 0;
	WCHAR wstrData[MAX_PATH + 1] = { 0 };
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
			apppath = wstrData;
		}

		RegCloseKey(hKey);
	}

	if (apppath.size() <= 0)
	{
		HKEY hKey = NULL;
		DWORD dwErrorCode = 0;
		WCHAR wstrData[MAX_PATH + 1] = { 0 };
		if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CLASSES_ROOT,
			appclassid32.c_str(),
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
				apppath = wstrData;
			}

			RegCloseKey(hKey);
		}
	}

	if (apppath.size() > 0)
	{
		std::size_t npos = apppath.find(L".EXE");
		if (npos != std::wstring::npos)
			apppath = apppath.substr(0, npos + 4);
		else
		{
			std::size_t npos = apppath.find(L".exe");
			if (npos != std::wstring::npos)
				apppath = apppath.substr(0, npos + 4);
		}

		//trim
		apppath.erase(0, apppath.find_first_not_of(L" "));
		apppath.erase(apppath.find_last_not_of(L" ") + 1);
		if ((apppath.at(0) == L'\"') && (apppath.at(apppath.length()-1) != L'\"'))
		{
			apppath += L"\"";
		}
	}
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

BOOL Is32bitsWord()
{
	// Assume Windows version is 64bits
	std::wstring classid;
	std::wstring _appclassid = L"Word.Application\\CLSID";
	HKEY hKey = NULL;
	DWORD dwErrorCode = 0;
	WCHAR wstrData[MAX_PATH + 1] = { 0 };

	// [HKEY_CLASSES_ROOT\Word.Application\CLSID]
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CLASSES_ROOT,
		_appclassid.c_str(),
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

	std::wstring apppath;
	std::wstring appclassid = L"CLSID\\" + classid + L"\\LocalServer32";
	std::wstring appclassid32 = L"WOW6432Node\\CLSID\\" + classid + L"\\LocalServer32";

	// Computer\HKEY_CLASSES_ROOT\CLSID\{91493441-5A91-11CF-8700-00AA0060263B}\LocalServer32
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
			apppath = wstrData;
		}

		RegCloseKey(hKey);
	}

	if (apppath.size() <= 0)
	{
		HKEY hKey = NULL;
		DWORD dwErrorCode = 0;
		WCHAR wstrData[MAX_PATH + 1] = { 0 };
		if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CLASSES_ROOT,
			appclassid32.c_str(),
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
				apppath = wstrData;
			}

			RegCloseKey(hKey);
		}
	}

	if (apppath.size() > 0)
	{
		std::transform(apppath.begin(), apppath.end(), apppath.begin(), ::tolower);

		if (apppath.find(L"program files (x86)") == std::wstring::npos && apppath.find(L"progra~2") == std::wstring::npos)
			return false;
	}

	return true;
}

BOOL Is32bitsExcel()
{
	// Assume Windows version is 64bits
	std::wstring classid;
	std::wstring _appclassid = L"Excel.Application\\CLSID";
	HKEY hKey = NULL;
	DWORD dwErrorCode = 0;
	WCHAR wstrData[MAX_PATH + 1] = { 0 };

	// [HKEY_CLASSES_ROOT\Word.Application\CLSID]
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CLASSES_ROOT,
		_appclassid.c_str(),
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

	std::wstring apppath;
	std::wstring appclassid = L"CLSID\\" + classid + L"\\LocalServer32";
	std::wstring appclassid32 = L"WOW6432Node\\CLSID\\" + classid + L"\\LocalServer32";

	// Computer\HKEY_CLASSES_ROOT\CLSID\{91493441-5A91-11CF-8700-00AA0060263B}\LocalServer32
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
			apppath = wstrData;
		}

		RegCloseKey(hKey);
	}

	if (apppath.size() <= 0)
	{
		HKEY hKey = NULL;
		DWORD dwErrorCode = 0;
		WCHAR wstrData[MAX_PATH + 1] = { 0 };
		if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CLASSES_ROOT,
			appclassid32.c_str(),
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
				apppath = wstrData;
			}

			RegCloseKey(hKey);
		}
	}

	if (apppath.size() > 0)
	{
		std::transform(apppath.begin(), apppath.end(), apppath.begin(), ::tolower);

		if (apppath.find(L"program files (x86)") == std::wstring::npos && apppath.find(L"progra~2") == std::wstring::npos)
			return false;
	}

	return true;
}

BOOL Is32bitsPowerPoint()
{
	// Assume Windows version is 64bits
	std::wstring classid;
	std::wstring _appclassid = L"PowerPoint.Application\\CLSID";
	HKEY hKey = NULL;
	DWORD dwErrorCode = 0;
	WCHAR wstrData[MAX_PATH + 1] = { 0 };

	// [HKEY_CLASSES_ROOT\Word.Application\CLSID]
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CLASSES_ROOT,
		_appclassid.c_str(),
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

	std::wstring apppath;
	std::wstring appclassid = L"CLSID\\" + classid + L"\\LocalServer32";
	std::wstring appclassid32 = L"WOW6432Node\\CLSID\\" + classid + L"\\LocalServer32";

	// Computer\HKEY_CLASSES_ROOT\CLSID\{91493441-5A91-11CF-8700-00AA0060263B}\LocalServer32
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
			apppath = wstrData;
		}

		RegCloseKey(hKey);
	}

	if (apppath.size() <= 0)
	{
		HKEY hKey = NULL;
		DWORD dwErrorCode = 0;
		WCHAR wstrData[MAX_PATH + 1] = { 0 };
		if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CLASSES_ROOT,
			appclassid32.c_str(),
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
				apppath = wstrData;
			}

			RegCloseKey(hKey);
		}
	}

	if (apppath.size() > 0)
	{
		std::transform(apppath.begin(), apppath.end(), apppath.begin(), ::tolower);

		if (apppath.find(L"program files (x86)") == std::wstring::npos && apppath.find(L"progra~2") == std::wstring::npos)
			return false;
	}
	return true;
}

std::wstring GetNextLabsInstallDir()
{
	OutputDebugStringW(L"GetNextLabsInstallDir");
	std::wstring installdir = L"";
	HKEY root = HKEY_LOCAL_MACHINE;
	const wchar_t* parent = L"Software\\NextLabs\\SkyDRM";
	HKEY hParent;
	DWORD value_type;
	BYTE* value_buffer = NULL;
	DWORD value_length;

	if (ERROR_SUCCESS != RegOpenKeyExW(root, parent, 0, KEY_READ | KEY_WOW64_64KEY, &hParent))
	{
		OutputDebugStringW(L"Failed RegOpenKeyExW");
		goto Cleanup;
	}

	// get length first
	if (ERROR_SUCCESS != RegQueryValueExW(hParent, L"InstallPath", NULL, &value_type, NULL, &value_length)) {
		OutputDebugStringW(L"RegQueryValueExW failed to get value length");
		goto Cleanup;
	}

	value_buffer = new BYTE[value_length + 2];
	if (ERROR_SUCCESS != RegQueryValueExW(hParent, L"InstallPath", NULL, &value_type, value_buffer, &value_length)) {
		OutputDebugStringW(L"RegQueryValueExW failed to get value");
		goto Cleanup;
	}

	// set value to out param
	installdir.assign((wchar_t*)value_buffer);

Cleanup:
	if (value_buffer!=NULL) {
		delete[] value_buffer;
	}
	if (hParent!=NULL) {
		RegCloseKey(hParent);
		hParent = NULL;
	}

	return installdir;
}

//Word: .doc, .docx, .dot, .dotx, .rtf, .docm, .dotm, .odt
//Powerpoint : .xls, .xlsx, .xlt, .xltx, .xlsb, .csv, .ods, .xlm, .xltm,  .xlsm
//Excel : .ppt, .pptx, .ppsx, .potx, .odp, .pptm, .potm, .pps, .ppsm, .pot
void RegisterFileAssociation(
	ISDRmcInstance& rmInstance,
	const std::wstring& strWord,
	const std::wstring& strExcel,
	const std::wstring& strPowerPoint)
{
	std::array<std::wstring, 8> arrWord = { L".doc", L".docx", L".dot", L".dotx", L".rtf", L".docm", L".dotm", L".odt" };
	std::array<std::wstring, 10> arrExcel = { L".xls", L".xlsx", L".xlt", L".xltx", L".xlsb", L".csv", L".ods", L".xlm", L".xltm",  L".xlsm" };
	std::array<std::wstring, 10> arrPowerPoint = { L".ppt", L".pptx", L".ppsx", L".potx", L".odp", L".pptm", L".potm", L".pps", L".ppsm", L".pot" };

	for (auto item : arrWord)
	{
		rmInstance.RPMRegisterFileAssociation(item, strWord, gRPM_Security);
		//ModifyWordOpenCommand(item);
	}

	for (auto item : arrExcel)
	{
		rmInstance.RPMRegisterFileAssociation(item, strExcel, gRPM_Security);
	}

	for (auto item : arrPowerPoint)
	{
		rmInstance.RPMRegisterFileAssociation(item, strPowerPoint, gRPM_Security);
	}
}

void UnRegisterFileAssociation(ISDRmcInstance& rmInstance)
{
	std::array<std::wstring, 8> arrWord = { L".doc", L".docx", L".dot", L".dotx", L".rtf", L".docm", L".dotm", L".odt" };
	std::array<std::wstring, 10> arrExcel = { L".xls", L".xlsx", L".xlt", L".xltx", L".xlsb", L".csv", L".ods", L".xlm", L".xltm",  L".xlsm" };
	std::array<std::wstring, 10> arrPowerPoint = { L".ppt", L".pptx", L".ppsx", L".potx", L".odp", L".pptm", L".potm", L".pps", L".ppsm", L".pot" };

	for (auto item : arrWord)
	{
		rmInstance.RPMUnRegisterFileAssociation(item, gRPM_Security);
		//RevertWordOpenCommand(item);
	}

	for (auto item : arrExcel)
	{
		rmInstance.RPMUnRegisterFileAssociation(item, gRPM_Security);
	}

	for (auto item : arrPowerPoint)
	{
		rmInstance.RPMUnRegisterFileAssociation(item, gRPM_Security);
	}
}


std::wstring NXGetLongPathName(const std::wstring &strPath)
{
	wchar_t szBuf[MAX_PATH] = { 0 };
	std::wstring strFilePath(strPath);
	strFilePath.erase(0, strFilePath.find_first_not_of(L" "));
	strFilePath.erase(strFilePath.find_last_not_of(L" ") + 1);

	bool bHasQuote = false;
	if (strFilePath.size() <= 0) return strFilePath;

	if (strFilePath.at(0) == L'\"')
	{
		strFilePath = strFilePath.substr(1, strFilePath.length() - 2);
		bHasQuote = true;
	}

	::GetLongPathNameW(strFilePath.c_str(), szBuf, MAX_PATH);
	std::wstring strLongPath(szBuf);

	strLongPath.erase(0, strLongPath.find_first_not_of(L" "));
	strLongPath.erase(strLongPath.find_last_not_of(L" ") + 1);
	if (strLongPath.empty())
	{
		::OutputDebugStringW(L"###########Enter NXGetLongPathName#############");
		::OutputDebugStringW(strPath.c_str());
		::OutputDebugStringW(L"###########Leave NXGetLongPathName#############");

		strLongPath = strFilePath;
	}

	if (bHasQuote)
	{
		return std::move(std::wstring(L"\"" + strLongPath + L"\""));
	}
	return std::move(strLongPath);
}


UINT RegisterOfficeRMX()
{
	ISDRmcInstance* _RmsIns = NULL;
	SDWLResult res = SDWLibCreateInstance(&_RmsIns);
	if (!res)
	{
		return ERROR_INSTALL_FAILURE;
	}

	std::wstring wordapp = GetApplicationPath(L"Word.Application");
	std::wstring excelapp = GetApplicationPath(L"Excel.Application");
	std::wstring powerpointapp = GetApplicationPath(L"PowerPoint.Application");

	if (wordapp.size() <= 0 && excelapp.size() <= 0 && powerpointapp.size() <= 0)
	{
		return ERROR_INSTALL_FAILURE;
	}

	_RmsIns->RPMRegisterApp(wordapp, gRPM_Security);
	_RmsIns->RPMRegisterApp(excelapp, gRPM_Security);
	_RmsIns->RPMRegisterApp(powerpointapp, gRPM_Security);

	::OutputDebugStringW(L"###########Enter Register Office RMX File Association#############");
	::OutputDebugStringW(wordapp.c_str());
	::OutputDebugStringW(excelapp.c_str());
	::OutputDebugStringW(powerpointapp.c_str());

	std::wstring strWordApp = NXGetLongPathName(wordapp);
	std::wstring strExcelApp = NXGetLongPathName(excelapp);
	std::wstring strPowerPointApp = NXGetLongPathName(powerpointapp);

	::OutputDebugStringW(strWordApp.c_str());
	::OutputDebugStringW(strExcelApp.c_str());
	::OutputDebugStringW(strPowerPointApp.c_str());
	::OutputDebugStringW(L"###########Leave Register Office RMX File Association#############");

	RegisterFileAssociation(*_RmsIns, strWordApp, strExcelApp, strPowerPointApp);

	delete _RmsIns;

	return ERROR_SUCCESS;
}

UINT UnRegisterOfficeRMX()
{
	ISDRmcInstance* _RmsIns = NULL;
	SDWLResult res = SDWLibCreateInstance(&_RmsIns);
	if (!res)
	{
		return ERROR_INSTALL_FAILURE;
	}

	std::wstring wordapp = GetApplicationPath(L"Word.Application");
	std::wstring excelapp = GetApplicationPath(L"Excel.Application");
	std::wstring powerpointapp = GetApplicationPath(L"PowerPoint.Application");

	if (wordapp.size() <= 0 && excelapp.size() <= 0 && powerpointapp.size() <= 0)
	{
		return ERROR_INSTALL_FAILURE;
	}
	_RmsIns->RPMUnregisterApp(wordapp, gRPM_Security);
	_RmsIns->RPMUnregisterApp(excelapp, gRPM_Security);
	_RmsIns->RPMUnregisterApp(powerpointapp, gRPM_Security);

	UnRegisterFileAssociation(*_RmsIns);

	delete _RmsIns;

	return ERROR_SUCCESS;
}

std::wstring reg_get_value(const std::wstring &strSubKey, const std::wstring &strValueName, DWORD dwType, HKEY hRoot)
{
	HKEY hKey = NULL;
	std::wstring strValue;
	LSTATUS lStatus = ::RegOpenKeyExW(hRoot, strSubKey.c_str(), 0, KEY_QUERY_VALUE, &hKey); //| KEY_WOW64_64KEY
	if (lStatus == ERROR_SUCCESS)
	{
		std::vector<unsigned char> buf;
		unsigned long value_size = 1;

		lStatus = ::RegQueryValueExW(hKey, strValueName.c_str(), NULL, &dwType, (LPBYTE)buf.data(), &value_size);
		if (ERROR_SUCCESS == lStatus)
		{
			buf.resize(value_size, 0);
			lStatus = ::RegQueryValueExW(hKey, strValueName.c_str(), NULL, &dwType, (LPBYTE)buf.data(), &value_size);
			if (ERROR_SUCCESS == lStatus)
			{
				strValue = (const wchar_t*)buf.data();
			}
		}
	}

	if (hKey != NULL)
	{
		RegCloseKey(hKey);
		hKey = NULL;
	}

	return std::move(strValue);
}

HRESULT reg_set_value(const std::wstring &strSubKey, const std::wstring &strValueName, const std::wstring &strValue, DWORD dwType, HKEY hRoot)
{
	HRESULT hr = S_OK;

	HKEY hKey = NULL;
	do
	{
		if (ERROR_SUCCESS != RegOpenKeyExW(hRoot, strSubKey.c_str(), 0, KEY_WRITE, &hKey)) // | KEY_WOW64_64KEY
		{
			hr = E_UNEXPECTED;
			break;
		}

		DWORD dwData = (DWORD)strValue.length() * sizeof(WCHAR);
		if (ERROR_SUCCESS != RegSetValueExW(hKey, strValueName.c_str(), 0, dwType, (const BYTE*)strValue.c_str(), dwData)) //REG_MULTI_SZ
		{
			hr = E_UNEXPECTED;
			break;
		}

	} while (FALSE);

	if (hKey)
	{
		RegCloseKey(hKey);
		hKey = NULL;
	}

	return hr;
}

static HRESULT ModifyWordOpenCommand(std::wstring doctype)
{
	std::wstring strItemDefault = L"";
	std::wstring strItemName = L"command";
	std::wstring strDoc = reg_get_value(L"SOFTWARE\\Classes\\" + doctype, L"", REG_SZ, HKEY_LOCAL_MACHINE);
	std::wstring strCurVer = reg_get_value(L"SOFTWARE\\Classes\\" + strDoc, L"CurVer", REG_SZ, HKEY_LOCAL_MACHINE);
	if (strCurVer.size() <= 0)
		strCurVer = strDoc;
	std::wstring strSubkey = L"SOFTWARE\\Classes\\" + strCurVer + L"\\shell\\Open\\command";
	// change "command", REG_MULTI_SZ
	{
		std::wstring strCommand = reg_get_value(strSubkey, strItemName, REG_MULTI_SZ, HKEY_LOCAL_MACHINE);
		if (strCommand.size() > 0)
		{
			std::wstring strNewCommand = strCommand;
			std::wstring strFind(L"/n");
			std::wstring strTarget(L"/w");
			auto pos = strNewCommand.rfind(strFind);
			if (std::wstring::npos != pos)
			{
				strNewCommand.replace(pos, strFind.length(), strTarget);
			}
			HRESULT hRet = reg_set_value(strSubkey, strItemName, strNewCommand, REG_MULTI_SZ, HKEY_LOCAL_MACHINE);

			::OutputDebugStringW(L"###########Enter ModifyWordOpenCommand#############");
			::OutputDebugStringW(strDoc.c_str());
			::OutputDebugStringW(strCurVer.c_str());
			::OutputDebugStringW(strSubkey.c_str());
			::OutputDebugStringW(strCommand.c_str());
			::OutputDebugStringW(strNewCommand.c_str());
			::OutputDebugStringW(L"###########Leave ModifyWordOpenCommand#############");
		}
	}
	// change default "", REG_SZ
	{
		std::wstring strCommand = reg_get_value(strSubkey, strItemDefault, REG_SZ, HKEY_LOCAL_MACHINE);
		if (strCommand.size() > 0)
		{
			std::wstring strNewCommand = strCommand;
			std::wstring strFind(L"/n");
			std::wstring strTarget(L"/w");
			auto pos = strNewCommand.rfind(strFind);
			if (std::wstring::npos != pos)
			{
				strNewCommand.replace(pos, strFind.length(), strTarget);
			}
			HRESULT hRet = reg_set_value(strSubkey, strItemDefault, strNewCommand, REG_SZ, HKEY_LOCAL_MACHINE);

			::OutputDebugStringW(L"###########Enter ModifyWordOpenCommand#############");
			::OutputDebugStringW(strDoc.c_str());
			::OutputDebugStringW(strCurVer.c_str());
			::OutputDebugStringW(strSubkey.c_str());
			::OutputDebugStringW(strCommand.c_str());
			::OutputDebugStringW(strNewCommand.c_str());
			::OutputDebugStringW(L"###########Leave ModifyWordOpenCommand#############");
		}
	}

	return 0;
}

static HRESULT RevertWordOpenCommand(std::wstring doctype)
{
	std::wstring strItemDefault = L"";
	std::wstring strItemName = L"command";
	std::wstring strDoc = reg_get_value(L"SOFTWARE\\Classes\\" + doctype, L"", REG_SZ, HKEY_LOCAL_MACHINE);
	std::wstring strCurVer = reg_get_value(L"SOFTWARE\\Classes\\" + strDoc, L"CurVer", REG_SZ, HKEY_LOCAL_MACHINE);
	if (strCurVer.size() <= 0)
		strCurVer = strDoc;
	std::wstring strSubkey = L"SOFTWARE\\Classes\\" + strCurVer + L"\\shell\\Open\\command";
	// change "command", REG_MULTI_SZ
	{
		std::wstring strCommand = reg_get_value(strSubkey, strItemName, REG_MULTI_SZ, HKEY_LOCAL_MACHINE);
		if (strCommand.size() > 0)
		{
			std::wstring strNewCommand = strCommand;
			std::wstring strFind(L"/w");
			std::wstring strTarget(L"/n");
			auto pos = strNewCommand.rfind(strFind);
			if (std::wstring::npos != pos)
			{
				strNewCommand.replace(pos, strFind.length(), strTarget);
			}
			HRESULT hRet = reg_set_value(strSubkey, strItemName, strNewCommand, REG_MULTI_SZ, HKEY_LOCAL_MACHINE);

			::OutputDebugStringW(L"###########Enter ModifyWordOpenCommand#############");
			::OutputDebugStringW(strDoc.c_str());
			::OutputDebugStringW(strCurVer.c_str());
			::OutputDebugStringW(strSubkey.c_str());
			::OutputDebugStringW(strCommand.c_str());
			::OutputDebugStringW(strNewCommand.c_str());
			::OutputDebugStringW(L"###########Leave ModifyWordOpenCommand#############");
		}
	}
	// change default "", REG_SZ
	{
		std::wstring strCommand = reg_get_value(strSubkey, strItemDefault, REG_SZ, HKEY_LOCAL_MACHINE);
		if (strCommand.size() > 0)
		{
			std::wstring strNewCommand = strCommand;
			std::wstring strFind(L"/w");
			std::wstring strTarget(L"/n");
			auto pos = strNewCommand.rfind(strFind);
			if (std::wstring::npos != pos)
			{
				strNewCommand.replace(pos, strFind.length(), strTarget);
			}
			HRESULT hRet = reg_set_value(strSubkey, strItemDefault, strNewCommand, REG_SZ, HKEY_LOCAL_MACHINE);

			::OutputDebugStringW(L"###########Enter ModifyWordOpenCommand#############");
			::OutputDebugStringW(strDoc.c_str());
			::OutputDebugStringW(strCurVer.c_str());
			::OutputDebugStringW(strSubkey.c_str());
			::OutputDebugStringW(strCommand.c_str());
			::OutputDebugStringW(strNewCommand.c_str());
			::OutputDebugStringW(L"###########Leave ModifyWordOpenCommand#############");
		}
	}

	return 0;
}

STDMETHODIMP Inxrmaddin::QueryInterface(REFIID riid, void **ppobj)
{
	HRESULT hRet = S_OK;

	IUnknown *punk = NULL;

	*ppobj = NULL;

	do
	{
		if ((IID_IUnknown == riid) || (IID_IClassFactory == riid))
		{
			punk = (IUnknown *)this;
		}
		else
		{
			hRet = E_NOINTERFACE;
			break;
		}

		AddRef();

		*ppobj = punk;

	} while (FALSE);

	return hRet;
}

STDMETHODIMP Inxrmaddin::CreateInstance(IUnknown * pUnkOuter, REFIID riid, void ** ppvObject)
{
	HRESULT hr = S_OK;

	nxrmExt2 *p = NULL;

	do
	{
		if (pUnkOuter)
		{
			*ppvObject = NULL;
			hr = CLASS_E_NOAGGREGATION;
			break;
		}

		p = new nxrmExt2;

		if (!p)
		{
			*ppvObject = NULL;
			hr = E_OUTOFMEMORY;
			break;
		}

		InterlockedIncrement(&g_unxrmext2InstanceCount);

		hr = p->QueryInterface(riid, ppvObject);

		p->Release();

	} while (FALSE);

	return hr;
}

STDMETHODIMP Inxrmaddin::LockServer(BOOL fLock)
{
	if (fLock)
	{
		m_uLockCount++;
	}
	else
	{
		if (m_uLockCount > 0)
			m_uLockCount--;
	}

	return m_uLockCount;
}

STDMETHODIMP_(ULONG) Inxrmaddin::AddRef()
{
	m_uRefCount++;

	return m_uRefCount;
}

STDMETHODIMP_(ULONG) Inxrmaddin::Release()
{
	ULONG uCount = 0;

	if (m_uRefCount)
		m_uRefCount--;

	uCount = m_uRefCount;

	if (!uCount && (m_uLockCount == 0))
	{
		delete this;
		InterlockedDecrement(&g_unxrmaddinInstanceCount);
	}

	return uCount;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
	HRESULT  hr = E_OUTOFMEMORY;

	Inxrmaddin *InxrmInstance = NULL;

	if (IsEqualCLSID(rclsid, CLSID_nxrmAddin))
	{
		InxrmInstance = new Inxrmaddin;

		if (InxrmInstance)
		{
			InterlockedIncrement(&g_unxrmaddinInstanceCount);
			hr = InxrmInstance->QueryInterface(riid, ppv);
			InxrmInstance->Release();
		}
	}
	else
	{
		hr = CLASS_E_CLASSNOTAVAILABLE;
	}

	return(hr);
}

STDAPI DllCanUnloadNow(void)
{
	if (g_unxrmaddinInstanceCount == 0 && g_unxrmext2InstanceCount == 0)
	{
		return S_OK;
	}
	else
	{
		return S_FALSE;
	}
}

STDAPI DllUnregisterServer(void)
{
	uninstall_powerpoint_addin();

	uninstall_excel_addin();

	uninstall_word_addin();

	uninstall_com_component();

	return S_OK;
}

STDAPI DllRegisterServer(void)
{
	HRESULT nRet = S_OK;

	WCHAR module_path[MAX_PATH] = { 0 };
	OLECHAR* ClsGuidString = NULL;

	do
	{
		if (!GetModuleFileNameW(g_hModule, module_path, sizeof(module_path) / sizeof(WCHAR)))
		{
			nRet = E_UNEXPECTED;
			break;
		}

		//create guid string	
		StringFromCLSID(CLSID_nxrmAddin, &ClsGuidString);

		nRet = install_com_component(module_path, ClsGuidString);
		if (S_OK != nRet)
		{
			break;
		}

		nRet = install_powerpoint_addin();
		if (S_OK != nRet)
		{
			break;
		}

		nRet = install_excel_addin();
		if (S_OK != nRet)
		{
			break;
		}

		nRet = install_word_addin();
		if (S_OK != nRet)
		{
			break;
		}

	} while (FALSE);

	if (ClsGuidString!=NULL)
	{
		::CoTaskMemFree(ClsGuidString);
		ClsGuidString = NULL;
	}

	if (nRet != S_OK)
	{
		DllUnregisterServer();
	}

	return nRet;
}


static HRESULT create_key_with_default_value(
	const HKEY	root,
	const WCHAR *parent,
	const WCHAR *key,
	const WCHAR *default_value)
{
	HRESULT nRet = S_OK;

	HKEY hParent = NULL;
	HKEY hKey = NULL;

	do
	{
		if (ERROR_SUCCESS != RegOpenKeyExW(root,
			parent,
			0,
			KEY_WRITE,
			&hParent))
		{
			nRet = E_UNEXPECTED;
			break;
		}

		if (ERROR_SUCCESS != RegCreateKey(hParent,
			key,
			&hKey))
		{
			nRet = E_UNEXPECTED;
			break;
		}

		if (!default_value)
		{
			break;
		}

		if (ERROR_SUCCESS != RegSetValueExW(hKey,
			NULL,
			0,
			REG_SZ,
			(const BYTE*)default_value,
			(DWORD)(wcslen(default_value) + 1) * sizeof(WCHAR)))
		{
			nRet = E_UNEXPECTED;
			break;
		}

	} while (FALSE);

	if (hKey)
	{
		RegCloseKey(hKey);
		hKey = NULL;
	}

	if (hParent)
	{
		RegCloseKey(hParent);
		hParent = NULL;
	}

	return nRet;
}

static HRESULT set_value_content(
	const WCHAR *key,
	const WCHAR *valuename,
	const WCHAR *content)
{
	HRESULT nRet = S_OK;

	HKEY hKey = NULL;

	do
	{
		if (ERROR_SUCCESS != RegOpenKeyExW(HKEY_CLASSES_ROOT,
			key,
			0,
			KEY_SET_VALUE,
			&hKey))
		{
			nRet = E_UNEXPECTED;
			break;
		}

		if (ERROR_SUCCESS != RegSetValueExW(hKey,
			valuename,
			0,
			REG_SZ,
			(const BYTE*)content,
			(DWORD)(wcslen(content) + 1) * sizeof(WCHAR)))
		{
			nRet = E_UNEXPECTED;
			break;
		}

	} while (FALSE);

	if (hKey)
	{
		RegCloseKey(hKey);
		hKey = NULL;
	}

	return nRet;
}

static HRESULT create_dword_value(
	const HKEY	root,
	const WCHAR	*key,
	const WCHAR	*value_name,
	const DWORD	value)
{
	HRESULT hr = S_OK;

	HKEY hKey = NULL;

	do
	{
		//
		// the key must has been created in advance
		//
		if (ERROR_SUCCESS != RegOpenKeyExW(root,
			key,
			0,
			KEY_WRITE,
			&hKey))
		{
			hr = E_UNEXPECTED;
			break;
		}

		if (ERROR_SUCCESS != RegSetValueExW(hKey,
			value_name,
			0,
			REG_DWORD,
			(const BYTE*)&value,
			sizeof(value)))
		{
			hr = E_UNEXPECTED;
			break;
		}

	} while (FALSE);

	if (hKey)
	{
		RegCloseKey(hKey);
		hKey = NULL;
	}

	return hr;
}

static HRESULT create_sz_value(
	const HKEY	root,
	const WCHAR	*key,
	const WCHAR	*value_name,
	const WCHAR	*value)
{
	HRESULT hr = S_OK;

	HKEY hKey = NULL;

	do
	{
		//
		// the key must has been created in advance
		//
		if (ERROR_SUCCESS != RegOpenKeyExW(root,
			key,
			0,
			KEY_WRITE,
			&hKey))
		{
			hr = E_UNEXPECTED;
			break;
		}

		if (ERROR_SUCCESS != RegSetValueExW(hKey,
			value_name,
			0,
			REG_SZ,
			(const BYTE*)value,
			(DWORD)(wcslen(value) + 1) * sizeof(WCHAR)))
		{
			hr = E_UNEXPECTED;
			break;
		}

	} while (FALSE);

	if (hKey)
	{
		RegCloseKey(hKey);
		hKey = NULL;
	}

	return hr;

}

static HRESULT delete_key(const HKEY root, const WCHAR *parent, const WCHAR *key)
{
	HRESULT nRet = S_OK;

	HKEY hKey = NULL;

	do
	{
		if (ERROR_SUCCESS != RegOpenKeyExW(root,
			parent,
			0,
			DELETE | KEY_SET_VALUE,
			&hKey))
		{
			nRet = E_UNEXPECTED;
			break;
		}

		if (ERROR_SUCCESS != RegDeleteTreeW(hKey, key))
		{
			nRet = E_UNEXPECTED;
			break;
		}

	} while (FALSE);

	if (hKey)
	{
		RegCloseKey(hKey);
		hKey = NULL;
	}

	return nRet;
}


static HRESULT install_com_component(WCHAR *addin_path, const WCHAR* wszClsGuid)
{
	HRESULT hr = S_OK;

	do
	{
		//
		// install component under CLSID 
		//
		std::wstring wstrClsIDKey = L"CLSID";
		hr = create_key_with_default_value(HKEY_CLASSES_ROOT,
			wstrClsIDKey.c_str(),
			wszClsGuid,
			NXRMADDIN_NAME);

		if (S_OK != hr)
		{
			break;
		}

		std::wstring wstrInstallClsIDKey = wstrClsIDKey + L"\\" + wszClsGuid;
		hr = create_key_with_default_value(HKEY_CLASSES_ROOT,
			wstrInstallClsIDKey.c_str(),
			L"InprocServer32",
			addin_path);

		if (S_OK != hr)
		{
			break;
		}

		std::wstring wstrInstallInproc32Key = wstrInstallClsIDKey + L"\\InprocServer32";
		hr = set_value_content(wstrInstallInproc32Key.c_str(),
			L"ThreadingModel",
			L"Apartment");

		if (S_OK != hr)
		{
			break;
		}

		//
		// install component under HKEY_CLASSES_ROOT
		//
		hr = create_key_with_default_value(HKEY_CLASSES_ROOT,
			NULL,
			NXRMADDIN_NAME,
			L"nxlrmaddin Class");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_key_with_default_value(HKEY_CLASSES_ROOT,
			NXRMADDIN_NAME,
			L"CLSID",
			wszClsGuid);

		if (S_OK != hr)
		{
			break;
		}

	} while (FALSE);

	RegisterOfficeRMX();
	return hr;
}

static HRESULT install_powerpoint_addin(void)
{
	HRESULT hr = S_OK;

	WCHAR addin_key[260] = { 0 };

	do
	{
		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY);

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			L"\\");

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_NAME);

		//
		// create powerpoint addin first in case office is not installed
		//
		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NULL,
			NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY,
			NXRMADDIN_NAME,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_DESCRIPTION_VALUE,
			L"Enable NextLabs Rights Management service for PowerPoint");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_FRIENDLYNAME_VALUE,
			L"NextLabs Rights Management for PowerPoint");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_dword_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_LOADBEHAVIOR_VALUE,
			3);

		if (S_OK != hr)
		{
			break;
		}

	} while (FALSE);

	do
	{
		WCHAR addin_key[260] = { 0 };

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY);

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			L"\\");

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMPOWERPOINTADDIN_NAME);

		//
		// create word addin first in case office is not installed
		//
		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NULL,
			NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY,
			NXRMPOWERPOINTADDIN_NAME,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_DESCRIPTION_VALUE,
			L"NextLabs Rights Management VSTO Addin for PowerPoint");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_FRIENDLYNAME_VALUE,
			L"NextLabs Rights Management Addin for PowerPoint");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_dword_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_LOADBEHAVIOR_VALUE,
			3);

		if (S_OK != hr)
		{
			break;
		}

		std::wstring nxrmwordaddinmanifest;
		if (Is32bitsPowerPoint())
			nxrmwordaddinmanifest = GetNextLabsInstallDir() + L"\\bin32\\nxrmPowerPointAddIn.vsto|vstolocal";
		else
			nxrmwordaddinmanifest = GetNextLabsInstallDir() + L"\\bin\\nxrmPowerPointAddIn.vsto|vstolocal";

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_MANIFEST_VALUE,
			nxrmwordaddinmanifest.c_str());

		if (S_OK != hr)
		{
			break;
		}
	} while (FALSE);


	return hr;
}

static HRESULT install_excel_addin(void)
{
	HRESULT hr = S_OK;

	WCHAR addin_key[260] = { 0 };

	do
	{
		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY);

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			L"\\");

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_NAME);

		//
		// create excel addin first in case office is not installed
		//
		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NULL,
			NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY,
			NXRMADDIN_NAME,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_DESCRIPTION_VALUE,
			L"Enable NextLabs Rights Management service for Excel");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_FRIENDLYNAME_VALUE,
			L"NextLabs Rights Management for Excel");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_dword_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_LOADBEHAVIOR_VALUE,
			3);

		if (S_OK != hr)
		{
			break;
		}

	} while (FALSE);

	do
	{
		WCHAR addin_key[260] = { 0 };

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY);

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			L"\\");

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMEXCELADDIN_NAME);

		//
		// create word addin first in case office is not installed
		//
		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NULL,
			NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY,
			NXRMEXCELADDIN_NAME,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_DESCRIPTION_VALUE,
			L"NextLabs Rights Management VSTO Addin for Excel");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_FRIENDLYNAME_VALUE,
			L"NextLabs Rights Management Addin for Excel");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_dword_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_LOADBEHAVIOR_VALUE,
			3);

		if (S_OK != hr)
		{
			break;
		}

		std::wstring nxrmwordaddinmanifest;
		if (Is32bitsExcel())
			nxrmwordaddinmanifest = GetNextLabsInstallDir() + L"\\bin32\\nxrmExcelAddIn.vsto|vstolocal";
		else
			nxrmwordaddinmanifest = GetNextLabsInstallDir() + L"\\bin\\nxrmExcelAddIn.vsto|vstolocal";

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_MANIFEST_VALUE,
			nxrmwordaddinmanifest.c_str());

		if (S_OK != hr)
		{
			break;
		}
	} while (FALSE);


	return hr;

}
static HRESULT install_word_addin(void)
{
	HRESULT hr = S_OK;

	do
	{
		WCHAR addin_key[260] = { 0 };

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY);

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			L"\\");

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_NAME);

		//
		// create word addin first in case office is not installed
		//
		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NULL,
			NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY,
			NXRMADDIN_NAME,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_DESCRIPTION_VALUE,
			L"Enable NextLabs Rights Management service for Word");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_FRIENDLYNAME_VALUE,
			L"NextLabs Rights Management for Word");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_dword_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_LOADBEHAVIOR_VALUE,
			3);

		if (S_OK != hr)
		{
			break;
		}

	} while (FALSE);

	do
	{
		WCHAR addin_key[260] = { 0 };

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY);

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			L"\\");

		wcscat_s((WCHAR*)addin_key,
			sizeof(addin_key) / sizeof(WCHAR),
			NXRMWORDADDIN_NAME);

		//
		// create word addin first in case office is not installed
		//
		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NULL,
			NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_key_with_default_value(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY,
			NXRMWORDADDIN_NAME,
			NULL);

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_DESCRIPTION_VALUE,
			L"NextLabs Rights Management VSTO Addin for Word");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_FRIENDLYNAME_VALUE,
			L"NextLabs Rights Management Addin for Word");

		if (S_OK != hr)
		{
			break;
		}

		hr = create_dword_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_LOADBEHAVIOR_VALUE,
			3);

		if (S_OK != hr)
		{
			break;
		}

		std::wstring nxrmwordaddinmanifest;
		if (Is32bitsWord())
			nxrmwordaddinmanifest = GetNextLabsInstallDir() + L"\\bin32\\nxrmWordAddIn.vsto|vstolocal";
		else
			nxrmwordaddinmanifest = GetNextLabsInstallDir() + L"\\bin\\nxrmWordAddIn.vsto|vstolocal";

		hr = create_sz_value(HKEY_LOCAL_MACHINE,
			(const WCHAR*)addin_key,
			NXRMADDIN_INSTALL_MANIFEST_VALUE,
			nxrmwordaddinmanifest.c_str());

		if (S_OK != hr)
		{
			break;
		}
	} while (FALSE);

	return hr;

}

static HRESULT uninstall_com_component(void)
{
	HRESULT hr = S_OK;

	do
	{
		OLECHAR* ClsGuidString = NULL;
		StringFromCLSID(CLSID_nxrmAddin, &ClsGuidString);

		hr = delete_key(HKEY_CLASSES_ROOT,
			L"CLSID",
			ClsGuidString);

		hr = delete_key(HKEY_CLASSES_ROOT,
			NULL,
			NXRMADDIN_NAME);

	} while (FALSE);

	UnRegisterOfficeRMX();

	return hr;
}

static HRESULT uninstall_powerpoint_addin(void)
{
	HRESULT hr = S_OK;

	do
	{
		hr = delete_key(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY,
			NXRMADDIN_NAME);

	} while (FALSE);

	do
	{
		hr = delete_key(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_POWERPOINT_ADDIN_KEY,
			NXRMPOWERPOINTADDIN_NAME);

	} while (FALSE);

	return hr;
}

static HRESULT uninstall_excel_addin(void)
{
	HRESULT hr = S_OK;

	do
	{
		hr = delete_key(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY,
			NXRMADDIN_NAME);

	} while (FALSE);

	do
	{
		hr = delete_key(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_EXCEL_ADDIN_KEY,
			NXRMEXCELADDIN_NAME);

	} while (FALSE);

	return hr;

}

static HRESULT uninstall_word_addin(void)
{
	HRESULT hr = S_OK;

	do
	{
		hr = delete_key(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY,
			NXRMADDIN_NAME);

	} while (FALSE);

	do
	{
		hr = delete_key(HKEY_LOCAL_MACHINE,
			NXRMADDIN_INSTALL_WINWORD_ADDIN_KEY,
			NXRMWORDADDIN_NAME);

	} while (FALSE);

	return hr;

}
