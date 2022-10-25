#include "helper.h"
#include "nxrmshellglobal.h"
#include <vector>
#include <sstream>
#include <process.h>
#include <thread>
#include <iostream>
#include <array>
#include <fstream>

#pragma warning(disable:4267)

extern "C" SHELL_GLOBAL_DATA Global;

static const std::wstring TAG_OFFICE_LIB = L"TagOffice.dll";
static const unsigned int NXL_LENGTH = 7;

HRESULT create_key_with_default_value(
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
		LONG lRet;

		if (ERROR_SUCCESS != (lRet = RegOpenKeyExW(root,
			parent,
			0,
			KEY_WRITE,
			&hParent)))
		{
			nRet = (lRet == ERROR_ACCESS_DENIED ? E_ACCESSDENIED : E_UNEXPECTED);
			break;
		}

		if (ERROR_SUCCESS != (lRet = RegCreateKey(hParent,
			key,
			&hKey)))
		{
			nRet = (lRet == ERROR_ACCESS_DENIED ? E_ACCESSDENIED : E_UNEXPECTED);
			break;
		}

		if (!default_value)
		{
			break;
		}

		if (ERROR_SUCCESS != (lRet = RegSetValueExW(hKey,
			NULL,
			0,
			REG_SZ,
			(const BYTE*)default_value,
			(DWORD)(wcslen(default_value) + 1) * sizeof(WCHAR))))
		{
			nRet = (lRet == ERROR_ACCESS_DENIED ? E_ACCESSDENIED : E_UNEXPECTED);
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

HRESULT set_value_content(
	const HKEY  root,
	const WCHAR *key,
	const WCHAR *valuename,
	const WCHAR *content)
{
	HRESULT nRet = S_OK;

	HKEY hKey = NULL;

	do
	{
		if (ERROR_SUCCESS != RegOpenKeyExW(root,
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

HRESULT delete_key(const HKEY root, const WCHAR *parent, const WCHAR *key)
{
	HRESULT nRet = S_OK;

	HKEY hKey = NULL;

	do
	{
		LONG lRet;

		if (ERROR_SUCCESS != (lRet = RegOpenKeyExW(root,
			parent,
			0,
			DELETE,
			&hKey)))
		{
			nRet = (lRet == ERROR_FILE_NOT_FOUND || lRet == ERROR_ACCESS_DENIED ? HRESULT_FROM_WIN32(lRet) : E_UNEXPECTED);
			break;
		}

		if (ERROR_SUCCESS != (lRet = RegDeleteKeyW(hKey, key)))
		{
			nRet = (lRet == ERROR_FILE_NOT_FOUND || lRet == ERROR_ACCESS_DENIED ? HRESULT_FROM_WIN32(lRet) : E_UNEXPECTED);
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


HBITMAP BitmapFromIcon(HICON hIcon)
{
	ICONINFO IconInfo = { 0 };
	HBITMAP hBitmap = NULL;

	do
	{
		if (!hIcon)
		{
			break;
		}

		if (!GetIconInfo(hIcon, &IconInfo))
		{
			break;
		}

		hBitmap = (HBITMAP)CopyImage(IconInfo.hbmColor, IMAGE_BITMAP, GetSystemMetrics(SM_CXSMICON), GetSystemMetrics(SM_CYSMICON), LR_CREATEDIBSECTION);

		::DeleteObject(IconInfo.hbmColor);
		::DeleteObject(IconInfo.hbmMask);

	} while (FALSE);

	return hBitmap;
}

int GetCurrentDPI()
{
	int nDPI = 0;
	HDC hdc = GetDC(0);

	nDPI = GetDeviceCaps(hdc, LOGPIXELSY);

	ReleaseDC(0, hdc);
	return nDPI;
}



std::vector<std::wstring> query_selected_file(IDataObject *pdtobj)
{
	std::vector<std::wstring> files;
	FORMATETC   FmtEtc = { CF_HDROP, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
	STGMEDIUM   Stg = { 0 };
	HDROP       hDrop = NULL;

	memset(&Stg, 0, sizeof(Stg));
	Stg.tymed = CF_HDROP;

	// Find CF_HDROP data in pDataObj
	long rt = pdtobj->GetData(&FmtEtc, &Stg);

	if (rt) { // FAILED(pdtobj->GetData(&FmtEtc, &Stg))
		return files;
	}

	// Get the pointer pointing to real data
	hDrop = (HDROP)GlobalLock(Stg.hGlobal);
	if (NULL == hDrop) {
		ReleaseStgMedium(&Stg);
		return files;
	}

	// How many files are selected?
	const int nFiles = DragQueryFileW(hDrop, 0xFFFFFFFF, NULL, 0);
	if (0 == nFiles) {
		return files;
	}

	for (int i = 0; i < nFiles; i++) {
		wchar_t s[MAX_PATH];
		if (0 != DragQueryFileW(hDrop, i, s, MAX_PATH)) {
			//push all files in MenuFilter will be checked later
			files.push_back(s);
		}
	}

	GlobalUnlock(Stg.hGlobal);
	ReleaseStgMedium(&Stg);

	return files;
}

bool get_viewer_exe_path(std::wstring & path)
{
	HKEY root = HKEY_LOCAL_MACHINE;
	const wchar_t* parent = L"Software\\NextLabs\\SkyDRM\\LocalApp";
	HKEY hParent;

	if (ERROR_SUCCESS != RegOpenKeyExW(root, parent, 0, KEY_READ, &hParent))
	{
		return NULL;
	}

	DWORD value_type;
	BYTE* value_buffer;
	DWORD value_length;

	// get length first
	if (ERROR_SUCCESS != RegQueryValueExW(hParent, L"Viewer", NULL, &value_type, NULL, &value_length)) {
		RegCloseKey(hParent);
		return NULL;
	}

	value_buffer = new BYTE[value_length + 2];
	// get value;
	if (ERROR_SUCCESS != RegQueryValueExW(hParent, L"Viewer", NULL, &value_type, value_buffer, &value_length)) {
		RegCloseKey(hParent);
		return NULL;
	}
	// close 
	if (ERROR_SUCCESS != RegCloseKey(hParent)) {
		return NULL;
	}

	// set value to out param
	path.assign((wchar_t*)value_buffer);
	delete[] value_buffer;

	return true;
}


bool get_skydrm_exe_path(std::wstring & path)
{
	HKEY root = HKEY_LOCAL_MACHINE;
	const wchar_t* parent = L"Software\\NextLabs\\SkyDRM\\LocalApp";
	HKEY hParent;

	if (ERROR_SUCCESS != RegOpenKeyExW(root, parent, 0, KEY_READ, &hParent))
	{
		return NULL;
	}

	DWORD value_type;
	BYTE* value_buffer;
	DWORD value_length;

	// get length first
	if (ERROR_SUCCESS != RegQueryValueExW(hParent, L"Executable", NULL, &value_type, NULL, &value_length)) {
		RegCloseKey(hParent);
		return NULL;
	}

	value_buffer = new BYTE[value_length + 2];
	// get value;
	if (ERROR_SUCCESS != RegQueryValueExW(hParent, L"Executable", NULL, &value_type, value_buffer, &value_length)) {
		RegCloseKey(hParent);
		return NULL;
	}
	// close 
	if (ERROR_SUCCESS != RegCloseKey(hParent)) {
		return NULL;
	}

	// set value to out param
	path.assign((wchar_t*)value_buffer);
	delete[] value_buffer;

	return true;
}

bool is_dir(const std::wstring & path)
{
	DWORD dwAttrs = GetFileAttributesW(path.c_str());

	if (dwAttrs == INVALID_FILE_ATTRIBUTES) {
		OutputDebugString(L"GetFileAttributesW return INVALID_FILE_ATTRIBUTES, error code: ");
		OutputDebugStringA(std::to_string(GetLastError()).c_str());
		return false;
	}
	
	if (FILE_ATTRIBUTE_DIRECTORY & dwAttrs) {
		return true;
	}

	return false;
}

bool is_nxl_suffix(const std::wstring & path)
{
  /*char data[NXL_LENGTH];
  std::ifstream ifs(path, std::ifstream::in);
  ifs.getline(data, NXL_LENGTH);
  bool result = false;
  if (data[0] == 'N' && data[1] == 'X' && data[2] == 'L' && data[3] == 'F' && data[4] == 'M' && data[5] == 'T')
  {
    result = true;
  }
  ifs.close();
  return result;*/

	auto pos = path.find_last_of(L".");
	if (pos != -1)
	{
		if (!lstrcmpi(L".nxl", path.substr(pos, 4).c_str()))
		{
			return true;
		}
	}
	return false;
}

bool isOfficeFormatFile(const std::wstring& ext)
{
	bool bRet = false;
	static std::array<std::wstring, 21> arrOffice =
	{
		L".doc", L".docx", L".dot", L".dotx", L"docm", L"dotm", L"rtf"
		L".xls", L".xlsx", L".xlt", L".xltx", L"xlsm", L"xltm", L"xlsb",
		L".ppt", L".pptx", L".ppsx", L".potx", L"pot", L"potm", L"pptm"
	};

	for (auto item : arrOffice)
	{
		if (str_iends_with(ext, item)) {
			bRet = true;
			break;
		}
	}

	return bRet;
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

std::wstring GetTagLibPath()
{
	std::wstring ret(L"");

	WCHAR module_path[MAX_PATH] = { 0 };
	if (!GetModuleFileNameW(Global.hModule, module_path, sizeof(module_path) / sizeof(WCHAR))) {
		return ret;
	}
	std::wstring path(module_path);
	auto index = path.rfind(L"\\");
	if (index != std::wstring::npos) {
		std::wstring modulDir = path.substr(0, index);
		ret = modulDir + L"\\" + TAG_OFFICE_LIB;
	}

	return ret;
}

std::wstring string2WString(const std::string& str)
{
	std::wstring result;

	int len = MultiByteToWideChar(CP_ACP, 0, str.c_str(), str.size(), NULL, 0);
	TCHAR* buffer = new TCHAR[len + 1];
 
	MultiByteToWideChar(CP_ACP, 0, str.c_str(), str.size(), buffer, len);
	buffer[len] = '\0';    

	result.append(buffer);
	delete[] buffer;

	return result;
}

std::string wstring2String(const std::wstring& wstr)
{
	std::string result;
 
	int len = WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), wstr.size(), NULL, 0, NULL, NULL);
	char* buffer = new char[len + 1];
 
	WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), wstr.size(), buffer, len, NULL, NULL);
	buffer[len] = '\0';
 
	result.append(buffer);
	delete[] buffer;

	return result;
}

void SplitString(const std::wstring& ws, std::vector<std::wstring>& v, const std::wstring& c)
{
	std::wstring::size_type pos1, pos2;
	pos2 = ws.find(c);
	pos1 = 0;

	while (std::wstring ::npos != pos2)
	{
		v.push_back(ws.substr(pos1, pos2 - pos1));

		pos1 = pos2 + c.size();
		pos2 = ws.find(c, pos1);
	}

	if (pos1 != ws.length())
	{
		v.push_back(ws.substr(pos1));
	}
}

bool hasRights(const std::wstring& wstrFingerPrint, FileRights specifyRights)
{
	bool ret = false;

	if (wstrFingerPrint.empty())
	{
		return ret;
	}

	// FingerPrint: the format like following:
	// "RIGHT_VIEW=true|RIGHT_EDIT=true|RIGHT_SAVEAS=false;isByAdHoc=true|isByCentrolPolicy=fale"  

	std::vector<std::wstring> vecFp;
	SplitString(wstrFingerPrint, vecFp, L";");

	// parse rights
	std::vector<std::wstring> vecRights;
	SplitString(vecFp[0], vecRights, L"|");

	for (auto one : vecRights)
	{
		std::wstring k = one.substr(0, one.find('='));
		std::wstring v = one.substr(one.find('=')+1);

		auto s = rightsEnum2Str(specifyRights);
		if (s == k)
		{
			std::transform(v.begin(), v.end(), v.begin(), ::tolower);
			std::istringstream(wstring2String(v)) >> std::boolalpha >> ret;
			break;
		}
	}

	return ret;
}

bool isAdhoc(const std::wstring& wstrFingerPrint)
{
	bool ret = false;
	if (wstrFingerPrint.empty())
	{
		return ret;
	}

	std::vector<std::wstring> vecFp;
	SplitString(wstrFingerPrint, vecFp, L";");

	// parse other fields
	std::vector<std::wstring> vecOtherFields;
	SplitString(vecFp[1], vecOtherFields, L"|");

	for (auto one : vecOtherFields)
	{
		std::wstring k = one.substr(0, one.find('='));
		std::wstring v = one.substr(one.find('=') + 1);

		if (k == L"isByAdHoc")
		{
			std::transform(v.begin(), v.end(), v.begin(), ::tolower);
			std::istringstream(wstring2String(v)) >> std::boolalpha >> ret;
		}
	}

	return ret;
}

bool hasAdminRights(const std::wstring& wstrFingerPrint)
{
	bool ret = false;
	if (wstrFingerPrint.empty())
	{
		return ret;
	}

	std::vector<std::wstring> vecFp;
	SplitString(wstrFingerPrint, vecFp, L";");

	// parse other fields
	std::vector<std::wstring> vecOtherFields;
	SplitString(vecFp[1], vecOtherFields, L"|");

	for (auto one : vecOtherFields)
	{
		std::wstring k = one.substr(0, one.find('='));
		std::wstring v = one.substr(one.find('=') + 1);

		if (k == L"hasAdminRights")
		{
			std::transform(v.begin(), v.end(), v.begin(), ::tolower);
			std::istringstream(wstring2String(v)) >> std::boolalpha >> ret;
		}
	}

	return ret;
}

bool isSaaSRouter(const std::wstring& wstrFingerPrint)
{
	bool ret = false;
	if (wstrFingerPrint.empty())
	{
		return ret;
	}

	std::vector<std::wstring> vecFp;
	SplitString(wstrFingerPrint, vecFp, L";");

	// parse other fields
	std::vector<std::wstring> vecOtherFields;
	SplitString(vecFp[1], vecOtherFields, L"|");

	for (auto one : vecOtherFields)
	{
		std::wstring k = one.substr(0, one.find('='));
		std::wstring v = one.substr(one.find('=') + 1);

		if (k == L"isSaaSRouter")
		{
			std::transform(v.begin(), v.end(), v.begin(), ::tolower);
			std::istringstream(wstring2String(v)) >> std::boolalpha >> ret;
		}
	}

	return ret;
}

bool isFromProject(const std::wstring& wstrFingerPrint)
{
	bool ret = false;
	if (wstrFingerPrint.empty())
	{
		return ret;
	}

	std::vector<std::wstring> vecFp;
	SplitString(wstrFingerPrint, vecFp, L";");

	// parse other fields
	std::vector<std::wstring> vecOtherFields;
	SplitString(vecFp[1], vecOtherFields, L"|");

	for (auto one : vecOtherFields)
	{
		std::wstring k = one.substr(0, one.find('='));
		std::wstring v = one.substr(one.find('=') + 1);

		if (k == L"isFromPorject")
		{
			std::transform(v.begin(), v.end(), v.begin(), ::tolower);
			std::istringstream(wstring2String(v)) >> std::boolalpha >> ret;
		}
	}

	return ret;
}

bool isFromSystemBucket(const std::wstring& wstrFingerPrint)
{
	bool ret = false;
	if (wstrFingerPrint.empty())
	{
		return ret;
	}

	std::vector<std::wstring> vecFp;
	SplitString(wstrFingerPrint, vecFp, L";");

	// parse other fields
	std::vector<std::wstring> vecOtherFields;
	SplitString(vecFp[1], vecOtherFields, L"|");

	for (auto one : vecOtherFields)
	{
		std::wstring k = one.substr(0, one.find('='));
		std::wstring v = one.substr(one.find('=') + 1);

		if (k == L"isFromSystemBucket")
		{
			std::transform(v.begin(), v.end(), v.begin(), ::tolower);
			std::istringstream(wstring2String(v)) >> std::boolalpha >> ret;
		}
	}

	return ret;
}

bool isFromMyVault(const std::wstring& wstrFingerPrint)
{
	bool ret = false;
	if (wstrFingerPrint.empty())
	{
		return ret;
	}

	std::vector<std::wstring> vecFp;
	SplitString(wstrFingerPrint, vecFp, L";");

	// parse other fields
	std::vector<std::wstring> vecOtherFields;
	SplitString(vecFp[1], vecOtherFields, L"|");

	for (auto one : vecOtherFields)
	{
		std::wstring k = one.substr(0, one.find('='));
		std::wstring v = one.substr(one.find('=') + 1);

		if (k == L"isFromMyVault")
		{
			std::transform(v.begin(), v.end(), v.begin(), ::tolower);
			std::istringstream(wstring2String(v)) >> std::boolalpha >> ret;
		}
	}

	return ret;
}



const unsigned int BUFFER_SIZE = 4096;

bool NamedPipeClient::OnConnectPipe()
{
	// Judge whether have named pipe instance.
	if (!WaitNamedPipe(m_pipeName.c_str(), NMPWAIT_USE_DEFAULT_WAIT))
	{
		OutputDebugString(L"Not have any named pipe instance.");
		m_hPipe = NULL;
		return false;
	}

	// Open available named pipe
	m_hPipe = CreateFile(
		m_pipeName.c_str(),
		GENERIC_READ | GENERIC_WRITE,
		0,
		NULL,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL,
		NULL);

	if (m_hPipe == INVALID_HANDLE_VALUE)
	{
		OutputDebugString(L"Open named pipe failed.");
		m_hPipe = NULL;
		return false;
	}

	return true;
}

void NamedPipeClient::OnWritePipe()
{
	if (m_hPipe == NULL)
	{
		return;
	}

	char buf[BUFFER_SIZE] = { 0 };
	DWORD dwWrite;
	strcpy_s(buf, wstring2String(m_FilePath).c_str());

	if (!WriteFile(m_hPipe, buf, BUFFER_SIZE, &dwWrite, NULL))
	{
		OutputDebugString(L"Write named pipe failed.");
	}
	FlushFileBuffers(m_hPipe);
}

void NamedPipeClient::OnReadPipe()
{
	if (m_hPipe == NULL)
	{
		return;
	}

	CHAR buf[BUFFER_SIZE];
	DWORD dwRead;
	unsigned int len = 0;
	memset(buf, 0, sizeof(buf));

	while (1)
	{
		if (ReadFile(m_hPipe, buf, BUFFER_SIZE, &dwRead, NULL))
		{
			if (dwRead != 0)
			{
				// Append received data.
				std::string data(buf);
				m_receivedData += string2WString(data);
				dwRead = 0;
				memset(buf, 0, sizeof(buf));

				break;
			}
			else
			{
				break;
			}
		}
		else
		{
			int ret = ::GetLastError();
			std::cout << ret << std::endl;

			break;
		}
	}
}

bool fs::is_dos_path(const std::wstring& s) 
{
	return isalpha(s[0]) && L':' == s[1] && L'\\' == s[2];
}

std::wstring fs::convert_dos_full_file_path(const std::wstring& s)
{
	std::wstring input_path(s);
	std::wstring final_path;

	auto find = input_path.find(L"/");
	if (find != std::wstring::npos) {
		std::replace(input_path.begin(), input_path.end(), '/', '\\');
	}

	auto found = input_path.rfind(L"\\");
	if (found == input_path.size() - 1) {
		input_path.replace(found, 2, L"");
	}
		
	// convert to long path
	long     length = 0;
	TCHAR*   buffer = NULL;
	// First obtain the size needed by passing NULL and 0.
	length = GetLongPathName(input_path.c_str(), NULL, 0);
	if (length > (long)(input_path.size() + 1))
	{
		buffer = new TCHAR[length];
		length = GetLongPathName(input_path.c_str(), buffer, length);
		if (length > 0)
			input_path = buffer;
		delete[] buffer;
	}

	// for junction folder or sybolic link file, GetFinalPathNameByHandleW to get exact file path
	BOOL input_has_nxl = false;
	if (is_nxl_suffix(input_path)) {
		input_has_nxl = true;
	}
		
		

	std::wstring global_input_path = input_path;
	if (fs::is_dos_path(input_path))
	{
		// if file path is too long > 260, convert it to global dos path
		global_input_path = L"\\\\?\\" + input_path;
	}

	TCHAR  existingTarget[2048];
	HANDLE hFile = ::CreateFileW(global_input_path.c_str(), 0, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (INVALID_HANDLE_VALUE == hFile)
	{
		// in case of directory
		hFile = ::CreateFileW(global_input_path.c_str(), 0, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_BACKUP_SEMANTICS, NULL);
	}

	if (INVALID_HANDLE_VALUE != hFile)
	{
		// following call will redirect to exact NXL or normal file if current process is trusted/untrusted
		// we shall append or remove .NXL back
		// Note: the string that is returned by this fucntion uses the "\\?\" syntax.
		DWORD dwret = ::GetFinalPathNameByHandleW(hFile, existingTarget, 2048, FILE_NAME_OPENED);

		if (dwret == 0)
		{
			DWORD ret = GetLastError();
			if (ret == ERROR_PATH_NOT_FOUND)
			{
				// not a dos path, let's try with GUID path
				dwret = ::GetFinalPathNameByHandleW(hFile, existingTarget, 2048, FILE_NAME_OPENED | VOLUME_NAME_GUID);
			}
		}

		if (dwret < 2048)
		{
			CloseHandle(hFile);
			input_path = existingTarget;
		}
		else
		{
			//
			// too long file path > 2048
			// allocate more buffer 
			TCHAR  *_2existingTarget = new WCHAR[dwret + 1];
			DWORD _2dwret = ::GetFinalPathNameByHandleW(hFile, _2existingTarget, (dwret + 1), FILE_NAME_OPENED);
			CloseHandle(hFile);

			if (_2dwret < (dwret + 1))
			{
				input_path = _2existingTarget;
			}
			else
			{
				// error, do nothing
			}
			delete _2existingTarget;
		}
	}

	BOOL output_has_nxl = false;
	if (is_nxl_suffix(input_path)) {
		output_has_nxl = true;
	}
		
	// append NXL, trusted process want to access a NXL file
	if (input_has_nxl && output_has_nxl == false) {
		input_path += L".nxl";
	}
		
	// remove .nxl, untrsuted process want to access a NXL file
	if (input_has_nxl == false && output_has_nxl && input_path.size() > 4) {
		input_path = input_path.substr(0, input_path.size() - 4);
	}
		
	found = input_path.rfind(L"\\");
	if (found == input_path.size() - 1) {
		input_path.replace(found, 2, L"");
	}
	
	if (str_istarts_with(input_path, L"\\\\?\\")) {
		input_path = input_path.substr(4);

		if (str_istarts_with(input_path, L"UNC\\")) {
			final_path = L"\\";
			final_path += input_path.substr(3);
		}
		else {
			final_path = input_path;
		}
	}
	else {
		final_path = input_path;
	}

	return std::move(final_path);
}
