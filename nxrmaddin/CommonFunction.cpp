#include "stdafx.h"
#include "SkyDrmSDKMgr.h"
#include "CommonFunction.h"
#include "Log.h"


// declare static vector
std::vector<std::wstring> CommonFunction::nxlFileNameStemURLEncoudered;
std::vector<std::wstring> CommonFunction::nxlFilePath;
std::map<std::wstring, bool> CommonFunction::openedNxlFilePath;
const unsigned int BUFFER_SIZE = 4096;

std::wstring CommonFunction::GetClassPath(const std::wstring &classid)
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
		if ((apppath.at(0) == L'\"') && (apppath.at(apppath.length() - 1) != L'\"'))
		{
			apppath += L"\"";
		}
	}
	return apppath;
}

std::wstring CommonFunction::GetApplicationPath(const std::wstring &appname)
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


std::wstring CommonFunction::GetFileFolder(const std::wstring& filePath)
{
	std::wstring strPath = filePath;
	std::wstring::size_type position = strPath.find_last_of(L'\\');
	if (position != std::wstring::npos)
	{
		strPath.erase(position);
	}
	return strPath;
}

std::wstring CommonFunction::GetFileName(const std::wstring& filePath)
{
	std::wstring wstrFileName = filePath;

	const size_t last_slash_idx = filePath.find_last_of(L"\\/");
	if (std::string::npos != last_slash_idx)
	{
		wstrFileName = filePath.substr(last_slash_idx + 1);
	}
	return wstrFileName;
}

std::wstring CommonFunction::GetLocalTimeString()
{
	SYSTEMTIME st = { 0 };
	GetLocalTime(&st);

	wchar_t szTime[256];
	int ncopy = wnsprintfW(szTime, sizeof(szTime)/sizeof(szTime[0])-1, L"%d%02d%02d-%02d%02d%02d", st.wYear, st.wMonth, st.wDay,
		st.wHour, st.wMinute, st.wSecond);

	szTime[ncopy] = 0;

	return szTime;
}

SYSTEMTIME CommonFunction::LocalTimeStringToSYSTEMTIME(std::wstring str)
{
	SYSTEMTIME st = { 0 };
	int a = 0;
	swscanf_s(str.c_str(), L"%04hd%02hd%02hd-%02hd%02hd%02hd.txt", &st.wYear, &st.wMonth, &st.wDay, &st.wHour, &st.wMinute, &st.wSecond);
	return st;
}

double CommonFunction::IntervalOfSYSTEMTIME(SYSTEMTIME start, SYSTEMTIME end)
{
	ULARGE_INTEGER fTime1;/*FILETIME*/
	ULARGE_INTEGER fTime2;/*FILETIME*/


	SystemTimeToFileTime(&start, (FILETIME*)&fTime1);
	SystemTimeToFileTime(&end, (FILETIME*)&fTime2);
	ULONGLONG dft = fTime2.QuadPart > fTime1.QuadPart ? fTime2.QuadPart - fTime1.QuadPart : fTime1.QuadPart - fTime2.QuadPart;
	return (double)dft / double(10000000);
}


std::wstring CommonFunction::GetProgramDataFolder()
{
	wchar_t* pwszFolder = NULL;
	HRESULT hr = ::SHGetKnownFolderPath(FOLDERID_ProgramData, 0, NULL, &pwszFolder);
	if (SUCCEEDED(hr) && (NULL!=pwszFolder) )
	{
		std::wstring strFolder = pwszFolder;
		CoTaskMemFree(pwszFolder);


		//create sub folder
		strFolder += L"\\Nextlabs\\officeaddin";

		int nRes =SHCreateDirectoryExW(NULL, strFolder.c_str(), NULL);
		if ((nRes!=ERROR_SUCCESS) &&
			(nRes!= ERROR_ALREADY_EXISTS) &&
			(nRes!=ERROR_FILE_EXISTS) )
		{
			strFolder = L"";
		}


		return strFolder;
	}
	return L"";
}

bool CommonFunction::IsNXLFile(const wchar_t* wszFilePath)
{
	bool res = false;

	if (SkyDrmSDKMgr::Instance()->IsRPMFolderFile(wszFilePath)) {
		res = true;
	}

	return res;
	//if (SkyDrmSDKMgr::Instance()->IsRPMFolderFile(wszFilePath))
	//{
	//	std::wstring wstrNXLFile = wszFilePath;
	//	wstrNXLFile += L".nxl";

	//	return FileExist(wstrNXLFile.c_str());
	//}

	//return false;
}


bool CommonFunction::IsNXLSuffix(const wchar_t* wszFilePath)
{
	if (!wszFilePath) {
		return false;
	}
	std::wstring path = wszFilePath;
	if (path.length() < 4) {
		return false;
	}
	std::transform(path.begin(), path.end(), path.begin(),
		[](unsigned char c) { return std::tolower(c); } // correct
	);

	if (0 == path.compare(path.length() - 4, 4, L".nxl")) {
		return true;
	}

	return false;
}


bool CommonFunction::FileExist(const wchar_t* wszFullFilePath)
{
	DWORD dwFileAttr = GetFileAttributesW(wszFullFilePath);
	if (dwFileAttr!=INVALID_FILE_ATTRIBUTES)
	{
		return (dwFileAttr&FILE_ATTRIBUTE_DIRECTORY) == 0;//is not a directory
	}
	return false;
}


bool CommonFunction::IsReadOnlyFile(const wchar_t* wszFullFilePath)
{
	DWORD dwFileAttr = GetFileAttributesW(wszFullFilePath);
	if (dwFileAttr != INVALID_FILE_ATTRIBUTES)
	{
		return (dwFileAttr&FILE_ATTRIBUTE_READONLY);
	}
	return false;
}

//
//void CommonFunction::ClearFolderContents(const std::wstring& path) {
//	using namespace std;
//	using namespace boost::filesystem;
//
//	if (!boost::filesystem::is_directory(path)) {
//		return;
//	}
//	
//	std::vector<directory_entry> v; // To save the file names in a vector.
//	std::copy(recursive_directory_iterator(path), recursive_directory_iterator(), back_inserter(v));
//	for (std::vector<directory_entry>::const_iterator it = v.begin(); it != v.end(); ++it)
//	{
//		try {
//			wstring p = (*it).path().generic_wstring();
//			// remove file readonly and hiden attr;
//			::SetFileAttributesW(p.c_str(), ::GetFileAttributesW(p.c_str()) & (~FILE_ATTRIBUTE_HIDDEN) & (~FILE_ATTRIBUTE_READONLY));
//			//wcout << L"remove file:" << p << endl;
//			if (is_regular_file(*it)) {
//				//wcout << "is file,so using win32 to del" << endl;
//				if (!::DeleteFileW(p.c_str())) {
//					//wcout << L"del failed" << endl;
//				}
//			}
//			if (is_directory(*it)) {
//				if (!::RemoveDirectoryW(p.c_str())) {
//					//wcout << L"remove dir failed" << endl;
//				}
//			}
//			//boost::filesystem::remove_all((*it));
//		}
//		catch (std::exception& ) {
//			//cout << e.what() << endl;
//		}
//	}
//	
//}



BSTR CommonFunction::LoadCustomUIFromFile(WCHAR *FileName)
{
	BSTR CustomUIXML = NULL;

	HANDLE hFile = INVALID_HANDLE_VALUE;

	BOOL bRet = TRUE;

	DWORD dwFileSize = 0;

	BYTE *buf = NULL;
	BYTE *p = NULL;
	DWORD bufLen = 0;

	DWORD BytesRead = 0;
	DWORD TotalBytesRead = 0;

	int BSTRLen = 0;

	do
	{
		hFile = CreateFileW(FileName,
			GENERIC_READ,
			0,
			NULL,
			OPEN_EXISTING,
			FILE_ATTRIBUTE_TEMPORARY | FILE_FLAG_SEQUENTIAL_SCAN,
			NULL);

		if (hFile == INVALID_HANDLE_VALUE)
		{
			break;
		}

		dwFileSize = GetFileSize(hFile, NULL);

		if (!dwFileSize)
		{
			break;
		}

		//
		// CustomUI.xml should not bigger than 1Mb
		//
		if (dwFileSize >= 1024 * 1024)
		{
			break;
		}

		bufLen = (dwFileSize + sizeof('\0') + 4095) & (~4095);

		buf = (BYTE*)malloc(bufLen);

		if (!buf)
		{
			break;
		}

		memset(buf, 0, bufLen);

		p = buf;
		TotalBytesRead = 0;

		do
		{
			bRet = ReadFile(hFile,
				(p - TotalBytesRead),
				(bufLen - TotalBytesRead),
				&BytesRead,
				NULL);

			if (!bRet)
			{
				break;
			}

			if (BytesRead == 0)
			{
				//
				// End of file
				//
				break;
			}

			TotalBytesRead += BytesRead;

		} while (TRUE);

		if (TotalBytesRead != dwFileSize)
		{
			break;
		}

		BSTRLen = MultiByteToWideChar(CP_UTF8,
			0,
			(LPCSTR)buf,
			TotalBytesRead,
			NULL,
			0);

		if (BSTRLen <= 0)
		{
			break;
		}

		CustomUIXML = SysAllocStringLen(NULL, BSTRLen);

		if (!CustomUIXML)
		{
			break;
		}

		BSTRLen = MultiByteToWideChar(CP_UTF8,
			0,
			(LPCSTR)buf,
			TotalBytesRead,
			CustomUIXML,
			BSTRLen);

		if (BSTRLen <= 0)
		{
			SysFreeString(CustomUIXML);
			CustomUIXML = NULL;
		}

	} while (FALSE);

	if (buf)
	{
		free(buf);
		buf = NULL;
	}

	if (hFile != INVALID_HANDLE_VALUE)
	{
		CloseHandle(hFile);
		hFile = INVALID_HANDLE_VALUE;
	}

	return CustomUIXML;
}

//bool CommonFunction::GetAutoSaveDir_PPT(std::wstring & outPath)
//{
//	wchar_t var_env[255] = { 0 };
//	::GetEnvironmentVariableW(L"APPDATA", var_env, 255);
//	boost::filesystem::path p(var_env);
//	p += L"\\Microsoft\\PowerPoint\\";
//	outPath = p.generic_wstring();
//	return true;
//}
//
//
//bool CommonFunction::GetAutoSaveDir_WORD(std::wstring & outPath)
//{
//	wchar_t var_env[255] = { 0 };
//	::GetEnvironmentVariableW(L"APPDATA", var_env, 255);
//	boost::filesystem::path p(var_env);
//	p += L"\\Microsoft\\Word\\";
//	outPath = p.generic_wstring();
//	return true;
//}
//
//bool CommonFunction::GetAutoSaveDir_EXCEL(std::wstring & outPath)
//{
//	wchar_t var_env[255] = { 0 };
//	::GetEnvironmentVariableW(L"APPDATA", var_env, 255);
//	boost::filesystem::path p(var_env);
//	p += L"\\Microsoft\\Excel\\";
//	outPath = p.generic_wstring();
//	return true;
//}

std::wstring gVersion;

std::wstring CommonFunction::GetOfficeVersion()
{
	return gVersion;
}

void CommonFunction::SetOfficeVersion(const std::wstring & version)
{
	gVersion = version;
}

std::wstring gName;

std::wstring CommonFunction::GetOfficeName()
{
	return gName;
}

void CommonFunction::SetOfficeName(const std::wstring & name)
{
	gName = name;
}

//void CommonFunction::TryingDeleteRegKeyInResiliency()
//{
//	// config registry
//	std::wstring path = L"Software\\Microsoft\\Office\\{Version}\\{AppName}\\Resiliency\\DocumentRecovery";
//
//	boost::replace_all(path, L"{Version}", GetOfficeVersion());
//	boost::replace_all(path, L"{AppName}", GetOfficeName());
//
//	theLog.WriteLog(0, NULL, 0, L"RegKey is %s\n", path.c_str());
//
//	// iterating all key's all values if match nxl file folder, remove this key
//	CRegKey key_docRecovery;
//	if (ERROR_SUCCESS !=key_docRecovery.Open(HKEY_CURRENT_USER, path.c_str(), KEY_READ | KEY_WRITE)) {
//		theLog.WriteLog(0, NULL, 0, L"can not open this key");
//		return;
//	}
//
//	DWORD index = 0;
//	std::vector<wchar_t> buf(MAX_PATH);
//	DWORD buf_len= MAX_PATH;
//
//	std::vector <std::wstring> foundKey;
//	// GET EACH KEY
//	while (ERROR_NO_MORE_ITEMS != key_docRecovery.EnumKey(index, buf.data(), &buf_len)) {
//		// FIND EACH VALUE
//		bool isFoundKey = false;
//		std::wstring kname(buf.data(), buf_len);
//		buf.clear(); buf.reserve(MAX_PATH);	buf_len = MAX_PATH;
//		CRegKey k;
//		if (ERROR_SUCCESS != k.Open(key_docRecovery, kname.c_str(), KEY_READ| KEY_WRITE)) {
//			continue;
//		}
//
//		// Enum each values
//		DWORD vindex = 0;
//		std::vector<wchar_t> vname(MAX_PATH);
//		std::vector<BYTE> vdata(0x1000);
//		DWORD vnlen= MAX_PATH;
//		DWORD vndata= 0x1000;
//		DWORD vdtype = REG_BINARY;
//		while (ERROR_NO_MORE_ITEMS != ::RegEnumValueW(k.m_hKey, vindex, vname.data(), &vnlen, NULL, &vdtype, vdata.data(), &vndata)) {
//
//			vdata.resize(vndata);
//			// get the values and to analyze
//			for (auto& nxl : nxlFilePath) {
//				// if nxl's parent can be found in vdata; must delete the key
//				std::wstring parent = boost::filesystem::path(nxl).parent_path().generic_wstring();
//				boost::replace_all(parent, L"/", L"\\");
//				std::vector<BYTE> target;
//				for (auto c : parent) {
//					target.push_back(HIBYTE(c));
//					target.push_back(LOBYTE(c));
//				}
//				if (vdata.end() != std::search(vdata.begin(), vdata.end(), target.begin(), target.end())) {
//					// match, must delete the k
//					foundKey.push_back(kname);
//					isFoundKey = true;
//					break;
//				}
//
//			}
//
//			if (isFoundKey) {
//				break;
//			}
//
//			// wati next iter
//			vindex++;
//			vnlen = MAX_PATH;
//			vname.clear(); vname.reserve(vnlen);
//			vndata = 0x1000;
//			vdata.clear(); vdata.resize(vndata);
//		}
//		
//
//		// wait next iter
//		index++;		
//	}
//
//
//	for (auto fk : foundKey) {
//		//fk.delete
//		key_docRecovery.DeleteSubKey(fk.c_str());
//	}
//
//
//}

std::wstring CommonFunction::String2WString(const std::string& str)
{
	//std::wstring result;
	//int len = MultiByteToWideChar(CP_ACP, 0, str.c_str(), str.size(), NULL, 0);
	//TCHAR* buffer = new TCHAR[len + 1];
	//MultiByteToWideChar(CP_ACP, 0, str.c_str(), str.size(), buffer, len);
	//buffer[len] = '\0';
	//result.append(buffer);
	//delete[] buffer;
	//return result;

	LPCSTR pszSrc = str.c_str();
	int nLen = str.size();

	int nSize = MultiByteToWideChar(CP_UTF8, 0, (LPCSTR)pszSrc, nLen, 0, 0);
	if (nSize <= 0) return NULL;

	WCHAR *pwszDst = new WCHAR[nSize + 1];
	if (NULL == pwszDst) return NULL;

	MultiByteToWideChar(CP_UTF8, 0, (LPCSTR)pszSrc, nLen, pwszDst, nSize);
	pwszDst[nSize] = 0;

	if (pwszDst[0] == 0xFEFF) // skip Oxfeff
		for (int i = 0; i < nSize; i++)
			pwszDst[i] = pwszDst[i + 1];

	std::wstring wcharString(pwszDst);
	delete pwszDst;

	return wcharString;
}

std::string CommonFunction::Wstring2String(const std::wstring& wstr)
{
	//std::string result;
	//int len = WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), wstr.size(), NULL, 0, NULL, NULL);
	//char* buffer = new char[len + 1];
	//WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), wstr.size(), buffer, len, NULL, NULL);
	//buffer[len] = '\0';
	//result.append(buffer);
	//delete[] buffer;
	//return result;

	LPCWSTR pwszSrc = wstr.c_str();

	int nLen = WideCharToMultiByte(CP_UTF8, 0, pwszSrc, -1, NULL, 0, NULL, NULL);

	if (nLen <= 0) return std::string("");

	char* pszDst = new char[nLen];
	if (NULL == pszDst) return std::string("");

	WideCharToMultiByte(CP_UTF8, 0, pwszSrc, -1, pszDst, nLen, NULL, NULL);
	pszDst[nLen - 1] = 0;

	std::string strTemp(pszDst);
	delete[] pszDst;

	return strTemp;
}

bool CommonFunction::IsPathLenghtMoreThan259(const std::wstring& filePath) {
	long     length = 0;
	TCHAR*   buffer = NULL;
	// First obtain the size needed by passing NULL and 0.
	length = GetLongPathName(filePath.c_str(), NULL, 0);
	if (length == 0) {
		return false;
	};
	// Dynamically allocate the correct size 
	// (terminating null char was included in length)
	buffer = new TCHAR[length];
	// Now simply call again using same short path.
	length = GetLongPathName(filePath.c_str(), buffer, length);
	if (length == 0) {
		delete[] buffer;
		return false;
	}
	_tprintf(TEXT("long name = %s shortname = %s"), buffer, filePath.c_str());
	std::wstring innerFilePath(buffer);
	delete[] buffer;

	int max_size = 259;
	if (innerFilePath.length() > max_size) {
		return true;
	}
	return false;
}

std::wstring CommonFunction::GetProcessName(unsigned long processId)
{
	DWORD		dwErr = 0;
	HANDLE		hProcess = NULL;
	std::wstring	strAppImagePath;

	do
	{
		if (processId == 0)
			hProcess = GetCurrentProcess();
		else
			hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, processId);
		if (hProcess == NULL) {
			dwErr = ::GetLastError();

			std::wstring log = std::wstring(L"OpenProcess dwErr is:") + std::to_wstring(dwErr);
			OutputDebugStringW(log.c_str());

			break;
		}

		WCHAR nameBuf[MAX_PATH];
		DWORD dwSize _countof(nameBuf);
		DWORD ret;

		// Discard using GetModuleFileNameExW, which will failed in some machine or scene.
		ret = QueryFullProcessImageName(hProcess, 0, nameBuf, &dwSize);

		if (ret == 0) {

			DWORD dwExitCode = 0;
			BOOL bRet = ::GetExitCodeProcess(hProcess, &dwExitCode);
			dwErr = ::GetLastError();

			std::wstring log = std::wstring(L"QueryFullProcessImageName dwErr is:") + std::to_wstring(dwErr);
			log += L" , GetExitCodeProcess ret = " + std::to_wstring(bRet) + L" exitcode = " + std::to_wstring(dwExitCode) + L"\n";
			OutputDebugStringW(log.c_str());

			break;
		}

		strAppImagePath = std::wstring(nameBuf);

	} while (FALSE);

	::CloseHandle(hProcess);

	return strAppImagePath;
}


std::wstring ConstructJson(std::wstring nxlFilePath) {
	std::wstring result = L"";

	/*
	 {
		"Intent": "0",
		"obj" : {
			"IsEdit": "true",
			"LocalPath" : "c:\aa\bb\c.doc.nxl"
			}
	}
	*/

	nlohmann::json root = nlohmann::json::object();
	//root["Bundle"] = nlohmann::json::object();
	//nlohmann::json& bundle = root["Bundle"];
	root["Intent"] = "1";
	root["obj"] = nlohmann::json::object();
	nlohmann::json& obj = root["obj"];
	obj["IsEdit"] = "true";
	obj["LocalPath"] = CommonFunction::Wstring2String(nxlFilePath);
	result = CommonFunction::String2WString(root.dump());
	return result;
}

bool OnConnectPipe(std::wstring pipeName, HANDLE* hPipe) {
	// Judge whether have named pipe instance.
	if (!WaitNamedPipe(pipeName.c_str(), NMPWAIT_USE_DEFAULT_WAIT))
	{
		OutputDebugString(L"Not have any named pipe instance.");
		*hPipe = NULL;
		return false;
	}

	// Open available named pipe
	*hPipe = CreateFile(
		pipeName.c_str(),
		GENERIC_READ | GENERIC_WRITE,
		0,
		NULL,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL,
		NULL);

	if (*hPipe == INVALID_HANDLE_VALUE)
	{
		OutputDebugString(L"Open named pipe failed.");
		*hPipe = NULL;
		return false;
	}

	return true;
}

//std::wstring ReadPipe(HANDLE hPipe) {
//	std::wstring result = L"";
//
//	if (hPipe == NULL)
//	{
//		return result;
//	}
//
//	CHAR buf[BUFFER_SIZE];
//	DWORD dwRead = 0;
//	memset(buf, 0, sizeof(buf));
//
//	while (1)
//	{
//		if (ReadFile(hPipe, buf, BUFFER_SIZE, &dwRead, NULL))
//		{
//			if (dwRead != 0)
//			{
//				// Append received data.
//				std::string data(buf);
//				result += CommonFunction::String2WString(data);
//				dwRead = 0;
//				memset(buf, 0, sizeof(buf));
//				break;
//			}
//			else
//			{
//				break;
//			}
//		}
//		else
//		{
//			int ret = ::GetLastError();
//			std::cout << ret << std::endl;
//			break;
//		}
//	}
//	return result;
//}

void OnWritePipe(std::wstring jsonData, HANDLE hPipe) {
	if (hPipe == NULL)
	{
		return;
	}
	char buf[BUFFER_SIZE] = { 0 };
	DWORD dwWrite = 0;
	strcpy_s(buf, CommonFunction::Wstring2String(jsonData).c_str());

	if (!WriteFile(hPipe, buf, BUFFER_SIZE, &dwWrite, NULL))
	{
		OutputDebugString(L"Write named pipe failed.");
	}
	FlushFileBuffers(hPipe);
}

void CommonFunction::NotifiyRMDAppToSyncNxlFile(const std::wstring nxlFilePath) 
{
	const std::wstring PIPE_NAME_PREFIX = L"\\\\.\\pipe\\544336d7-9086-4369-a9d0-3691ea290376_sid_";
	const std::wstring RMD_NAMEDPIP_SER_UUID = L"8986207c-5161-436a-abe9-dfc365c89820";


	if ((nxlFilePath.empty()) || (nxlFilePath.size()==0)) {
		return;
	}

	// Get current user session id.
	DWORD pid = GetCurrentProcessId();
	DWORD sid;
	BOOL ret = ProcessIdToSessionId(pid, &sid);
	if (ret) {

		std::wstring pipeName = PIPE_NAME_PREFIX + std::to_wstring(sid);
	
		std::wstring log = L"pipe name is: -->" + pipeName;
		::OutputDebugString(log.c_str());

		std::wstring jsonData = ConstructJson(nxlFilePath);

		HANDLE hPipe;
		if (OnConnectPipe(pipeName, &hPipe)) {
			//std::wstring receivedStr = ReadPipe(hPipe);
			//CommonFunction::trim(receivedStr);
			//if (0 == _wcsicmp(RMD_NAMEDPIP_SER_UUID.c_str(), receivedStr.c_str())) 
			//{
				OnWritePipe(jsonData, hPipe);
			//}

			if (hPipe != NULL)
			{
				CloseHandle(hPipe);
			}
		}
	}
	else {
		::OutputDebugString(L"Call ProcessIdToSessionId failed!");
		::OutputDebugString(std::to_wstring(GetLastError()).c_str());
	}
}


std::wstring CommonFunction::RPMEditFindMap(const std::wstring filePath) 
{
	OutputDebugStringW(L"RPMEditFindMap");
	std::wstring nxlfilepath = L"";
	HKEY root = HKEY_CURRENT_USER;
	const wchar_t* parent = L"Software\\NextLabs\\SkyDRM\\Session";
	DWORD value_type;
	BYTE* value_buffer = NULL;
	DWORD value_length;

	HKEY hParent;
	if (ERROR_SUCCESS != RegOpenKeyExW(root, parent, 0, KEY_READ, &hParent))
	{
		OutputDebugStringW(L"Failed RegOpenKeyExW");
		goto Cleanup;
	}

	// get length first
	if (ERROR_SUCCESS != RegQueryValueExW(hParent, filePath.c_str(), NULL, &value_type, NULL, &value_length)) {
		//RegCloseKey(hParent);
		OutputDebugStringW(L"RegQueryValueExW failed to get value length");
		goto Cleanup;
	}

	value_buffer = new BYTE[value_length + 2];
	// get value;
	if (ERROR_SUCCESS != RegQueryValueExW(hParent, filePath.c_str(), NULL, &value_type, value_buffer, &value_length)) {
		//RegCloseKey(hParent);
		OutputDebugStringW(L"RegQueryValueExW failed to get value");
		goto Cleanup;
	}

	// set value to out param
	nxlfilepath.assign((wchar_t*)value_buffer);

Cleanup:
	if (value_buffer != NULL) {
	  delete[] value_buffer;
	}

	if (hParent != NULL) {
		RegCloseKey(hParent);
		hParent = NULL;
	}

	return nxlfilepath;
}

#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
bool CommonFunction::IsSanctuaryFolder(const std::wstring & path, uint32_t * dirstatus, std::wstring & filetags)
{
	std::wstring log = std::wstring(L"dir is:") + std::wstring(path);
	if (SkyDrmSDKMgr::Instance()->IsSanctuaryFolder(path, dirstatus, filetags)) {
		log += std::wstring(L"this folder is a SanctuaryFolder.\r\n");
		OutputDebugStringW(log.c_str());
		return true;
	}
	log += std::wstring(L"this folder is not a SanctuaryFolder.\r\n");
	OutputDebugStringW(log.c_str());
	return false;
}
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR

std::wstring gAppName = L"Office App";

std::wstring nextlabs::utils::get_app_name()
{
	return gAppName;
}

void nextlabs::utils::set_app_name(const std::wstring& name)
{
	gAppName = name;
}


HRESULT AutoWrap(WORD autoType, VARIANT *pvResult, IDispatch* pDisp, LPOLESTR ptName, int cArgs...)
{
	// Check the parameter
	if (NULL == pDisp)
	{
		return E_FAIL;
	}

	// Variables used...
	DISPPARAMS  dp = { NULL, NULL, 0, 0 };
	DISPID      dispidNamed = DISPID_PROPERTYPUT;
	DISPID      dispID;
	HRESULT     hr = E_FAIL;
	char        szName[256] = { 0 };

	// Convert down to ANSI
	WideCharToMultiByte(CP_ACP, 0, ptName, -1, szName, 256, NULL, NULL);

	// Get DISPID for name passed...
	hr = pDisp->GetIDsOfNames(IID_NULL, &ptName, 1, LOCALE_USER_DEFAULT, &dispID);
	if (FAILED(hr))
	{
		return hr;
	}

	// Allocate memory for arguments...
	VARIANT *pArgs = new VARIANT[cArgs + 1];

	// Extract arguments...
	// Begin variable-argument list...
	va_list marker;
	va_start(marker, cArgs);

	for (int i = 0; i < cArgs; i++) {
		pArgs[i] = va_arg(marker, VARIANT);
	}
	// End variable-argument section...
	va_end(marker);

	// Build DISPPARAMS
	dp.cArgs = cArgs;
	dp.rgvarg = pArgs;

	// Handle special-case for property-puts!
	if (autoType & DISPATCH_PROPERTYPUT)
	{
		dp.cNamedArgs = 1;
		dp.rgdispidNamedArgs = &dispidNamed;
	}

	// Make the call!
	hr = pDisp->Invoke(dispID, IID_NULL, LOCALE_SYSTEM_DEFAULT, autoType, &dp, pvResult, NULL, NULL);
	delete[] pArgs;

	return hr;
}

CComPtr<IDispatch> GetCustomProperties(_In_ IDispatch* pCurDoc)
{
	if (NULL != pCurDoc)
	{
		CComVariant var2;
		HRESULT hr = AutoWrap(DISPATCH_PROPERTYGET, &var2, pCurDoc, L"CustomDocumentProperties", 0);
		if (SUCCEEDED(hr) && NULL != var2.pdispVal)
		{
			return var2.pdispVal;
		}
	}
	return NULL;
}

bool CommonFunction::ReadTag(_In_ IDispatch* pDocObj, _Out_ std::vector<std::pair<std::wstring, std::wstring>>& vecTagPair)
{
	// 1. check parameter & tag library, we use pDocObj to read tags but we also need use tag library to convert the long tags
	if (NULL == pDocObj)
	{
		return false;
	}

	// 2. get custom interface
	CComPtr<IDispatch> pCustomProperties = GetCustomProperties(pDocObj);
	if (NULL == pCustomProperties)
	{
		return false;
	}

	// 3. read tags by pDoc
	CComVariant theResult;
	HRESULT hr = AutoWrap(DISPATCH_PROPERTYGET, &theResult, pCustomProperties, L"Count", 0);
	if (SUCCEEDED(hr))
	{
		if (theResult.lVal <= 0)
		{
			return true;
		}

		long lCount = theResult.lVal;
		for (long i = 0; i < lCount; i++)
		{
			CComVariant varIndex(i + 1);
			hr = AutoWrap(DISPATCH_PROPERTYGET, &theResult, pCustomProperties, L"Item", 1, varIndex);
			if (SUCCEEDED(hr) && theResult.pdispVal)
			{
				IDispatch* pCustomProperty = theResult.pdispVal;	// here if use the CComPtr, the process can't exit when excel open in IE
				hr = AutoWrap(DISPATCH_PROPERTYGET, &theResult, pCustomProperty, L"Name", 0);
				if (SUCCEEDED(hr) && VT_BSTR == theResult.vt && NULL != theResult.bstrVal)
				{
					std::wstring wstrTagName(theResult.bstrVal);
					hr = AutoWrap(DISPATCH_PROPERTYGET, &theResult, pCustomProperty, L"Value", 0);
					if (SUCCEEDED(hr) && VT_BSTR == theResult.vt && NULL != theResult.bstrVal)
					{
						std::wstring wstrTagValue(theResult.bstrVal);
						vecTagPair.push_back(std::pair<std::wstring, std::wstring>(wstrTagName, wstrTagValue));
					}
				}
				pCustomProperty->Release();
			}
		}
	}
	else
	{
		return false;
	}

	return true;
	//// 4. alloc resource for convert the long tags 
	//ResourceAttributeManager* pMgr = NULL;
	//vector<ResourceAttributes*> vecpAttr;

	//if (!NLAlloceResource(pMgr, vecpAttr, 2))
	//{
	//	NLCELOG_RETURN_VAL(false)
	//}

	//NLAddAttributeFromVector(vecpAttr[0], vecTagPair);
	//m_lConvert4GetAttr(vecpAttr[1], vecpAttr[0]);					// this function always return 1, no means
	//vecTagPair.clear();
	//NLGetAttributeToVetor(vecpAttr[1], vecTagPair);

	//NLFreeResource(pMgr, vecpAttr);

	//NLPRINT_TAGPAIRLOG(vecTagPair, L"read tags by pDoc");
	//NLCELOG_RETURN_VAL(true)
}

bool str_istarts_with(const std::wstring& s, const std::wstring& s2)
{
	if (s.length() < s2.length())
		return false;

	return (0 == _wcsnicmp(s.c_str(), s2.c_str(), s2.length()));
}

bool str_iends_with(const std::wstring& s, const std::wstring& s2)
{
	if (s.length() < s2.length())
		return false;

	return (0 == _wcsicmp(s.c_str() + (s.length() - s2.length()), s2.c_str()));
}


std::wstring CommonFunction::ParseTag(const std::vector<std::pair<std::wstring, std::wstring>>& tags)
{
	for (auto i : tags) {
		if (str_istarts_with(i.first, L"MSIP_Label_") && str_iends_with(i.first, L"_Name")) {
			return i.second;
		}
	}

	return L"";
}