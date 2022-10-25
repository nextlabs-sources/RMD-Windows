// Cnxrmcom.cpp : Implementation of CCnxrmcom

#include "stdafx.h"
#include "Cnxrmcom.h"
#include <comutil.h>
#include <comdef.h>
#include <string>
#include <shellapi.h>
#include <experimental/filesystem>
#include "SDLAPI.h"
#include "checkpluginstatus.h" 
#include "nxrmcomerror.h"
#include "fileutils.h"
#include "SDLNXLFile.h"
#include "SDLUser.h"
#include <ShlObj_core.h>

#include <locale>
#include <codecvt>
//#include "rmccore/utils/json.h"
#include "rmccore/utils/string.h"
#include "rmccore/crypto/aes.h"
#include "rmccore/crypto/sha.h"
//#include "rmccore/restful/rmtoken.h"
//#include "rmccore/restful/rmnxlfile.h"
//#include "rmccore/restful/rmuser.h"
#include "json.hpp"

#include "common/celog2/celog.h" 
#define CELOG_CUR_MODULE "rmcommon"
#define CELOG_CUR_FILE CELOG_FILEPATH_SOURCES_SDWL_SDWRMCLIB_SDRMUSER_CPP 

#define HTTP_STATUS_FORBIDDEN           403
#define HTTP_STATUS_NOT_FOUND           404

using namespace RMCCORE;
using namespace CRYPT;

// CCnxrmcom

typedef basic_string_buffer<char>       string_buffer;
typedef basic_string_buffer<wchar_t>    wstring_buffer;

std::string utf16toutf8(const std::wstring& ws)
{
	std::string s;
	if (!ws.empty()) {
		const int reserved_size = (int)(ws.length() * 3 + 1);
		if (0 == WideCharToMultiByte(CP_UTF8, 0, ws.c_str(), (int)ws.length(), string_buffer(s, reserved_size), (int)reserved_size, nullptr, nullptr)) {
			s.clear();
		}
	}
	return s;
}

std::wstring utf8toutf16(const std::string& s)
{
	std::wstring ws;
	if (!s.empty()) {
		const int reserved_size = (int)s.length();
		if (0 == MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.length(), wstring_buffer(ws, reserved_size), (int)reserved_size)) {
			ws.clear();
		}
	}
	return ws;
}

HRESULT __stdcall CCnxrmcom::GetRights(BSTR NxlFilePath, BSTR * Rights)
{
	std::wstring _nxlFilePath(NxlFilePath);
	wstring log;
	log.append(L"function : GetRights");
	log.append(L" ");
	log.append(std::wstring(L"NxlFilePath : ") + _nxlFilePath);
	OutputDebugStringW(log.c_str());

	CELOG_LOG(CELOG_INFO, L"function=%s, NxlFilePath=%s\n", L"GetRights", _nxlFilePath.c_str());

	std::wstring rights;
	std::wstring json;

	SDWLResult res = RESULT(SDWL_SUCCESS);
	CommonUtils ut;

	std::vector<std::pair<SDRmFileRight, std::vector<SDR_WATERMARK_INFO>>> tmp;

	res = GetCurrentLoggedInUser();

	if (res.GetCode() == 0) {

		res = m_pRmUser->GetRights(_nxlFilePath, tmp);

		if (NULL != m_pRmUser && res.GetCode() == 0) {

			int size = tmp.size();
		
			for (size_t i = 0; i < size; i++)
			{
				rights += ut.SDRmFileRightToString(((SDRmFileRight)(tmp[i].first))).c_str();

				if (i != (size - 1)) {
					rights += L"|";
				}
			}
		}
	}

	std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	std::wstring wMsg = converter.from_bytes(res.GetMsg());

	json = BuildOutputJson(res.GetCode(), rights, wMsg);

	CComBSTR outStr(json.c_str());
	*Rights = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}


SDWLResult __stdcall CCnxrmcom::InnerProtectFile(const std::wstring &filepath, std::wstring& newcreatedfilePath, const std::string& tags) {

	SDWLResult res = RESULT(SDWL_SUCCESS);
	std::wstring tempPath;

	std::vector<SDRmFileRight> r;

	SDR_Expiration e;
	{
		e.type = (IExpiryType)0;
		e.start = 0;
		e.end = 0;
	}

	SDR_WATERMARK_INFO w;
	{
		w.text = std::string().empty();
		w.fontName = std::string().empty();
		w.fontColor = std::string().empty();
		w.fontSize = 0;
		w.transparency = 0;
		w.rotation = (WATERMARK_ROTATION)NOROTATION;
		w.repeat = 0;
	}

	bool adhoc;
	bool workspace;
	int heartbeat;
	int sysprojectid;
	std::string sysprojecttenantid;

	res = m_pRmUser->GetTenantPreference(&adhoc, &workspace, &heartbeat, &sysprojectid, sysprojecttenantid);
	//res = m_pRmUser->GetTenantPreference(&adhoc, &heartbeat, &sysprojectid, sysprojecttenantid);

	if (res == 0) {

		std::string memberid = m_pRmUser->GetMembershipID(sysprojecttenantid);

		if (!newcreatedfilePath.empty()) {
			 tempPath = newcreatedfilePath;
		}

		res = m_pRmUser->ProtectFile(filepath, tempPath, r, w, e, tags, memberid);

		newcreatedfilePath = tempPath;
	}

	return res;
}


SDWLResult CCnxrmcom::RPMCopyFile(const std::wstring &filepath, std::wstring& destpath, const std::wstring hiddenrpmfolder)
{
	CELOG_ENTER;
	SDWLResult res;
	CELOG_LOG(CELOG_INFO, L" filepath = %s, destpath= %s, hiddenrpmfolder= %s, srclen= %d\n", filepath.c_str(), destpath.c_str(), hiddenrpmfolder.c_str(), filepath.length());
	CommonUtils ut;

	std::wstring fpath = ut.to_unlimitedpath(filepath);
	CELOG_LOG(CELOG_INFO, L"to_unlimitedpath: fpath = %s, len= %d\n", fpath.c_str(), filepath.length());

	// check source file exists or not
	if (INVALID_FILE_ATTRIBUTES == GetFileAttributes(fpath.c_str()) && (GetLastError() == ERROR_FILE_NOT_FOUND || GetLastError() == ERROR_PATH_NOT_FOUND))
		CELOG_RETURN_VAL_T(RESULT2(ERROR_PATH_NOT_FOUND, "File not found"));

	// extract file name from source full path
	std::wstring filename = L"";
	std::experimental::filesystem::path _filename(fpath);
	if (_filename.has_extension() == false || icompare((std::wstring)(_filename.extension().c_str()), (std::wstring)(L".nxl")) != 0) //fix bug 56798
		filename = std::wstring(_filename.filename()) + L".nxl";
	else
		filename = _filename.filename();

	// make sure source file is NXL file and can be open
	ISDRmNXLFile *file = NULL;

	// don't need get token
	res = m_pRmUser->OpenFile(fpath, (ISDRmNXLFile**)&file);
	if (!res) {
		CELOG_RETURN_VAL_T(res);
	}

	GUID gidReference;
	HRESULT hCreateGuid = CoCreateGuid(&gidReference);
	std::string sDuid = ut.GuidToString(gidReference);
	std::wstring wDuid(_com_util::ConvertStringToBSTR(sDuid.c_str()));

	m_pRmUser->CloseFile(file);

	// get RPM folder
	std::wstring rpmfolder = L"";
	if (destpath.size() > 0)
		rpmfolder = destpath;
	else
	{
		// use RPM hidden folder for dest RPM folder
		rpmfolder = hiddenrpmfolder;
	}

	// no RPM folder set, return
	if (rpmfolder.size() == 0)
	{
		CELOG_LOG(CELOG_WARNING, L"no RPM folder set");
		CELOG_RETURN_VAL_T(RESULT(SDWL_SUCCESS));
	}

	CELOG_LOG(CELOG_DEBUG, L"source file = %s\n", fpath.c_str());
	// if file is already in a RPM folder, we will not copy
	if (filepath.find(rpmfolder, 0) != std::wstring::npos)
	{
		// file is in RPM folder (passed in RPM folder or hidden RPM folder)
		CELOG_RETURN_VAL_T(RESULT(SDWL_SUCCESS));
	}

	// generate file under RPM folder
	// file path is like "c://RPM folder//DUID-DUID-DUID-DUID//filename.docx
	SYSTEMTIME st = { 0 };
	GetSystemTime(&st);
	// -YYYY-MM-DD-HH-MM-SS
	const std::wstring sTimestamp(ut.FormatString(L"%04d%02d%02d%02d%02d%02d", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond));
	std::wstring dfullpath = ut.to_unlimitedpath(
		std::experimental::filesystem::path(rpmfolder) / std::experimental::filesystem::path(wDuid + sTimestamp));

	CELOG_LOGA(CELOG_DEBUG, "dfullpath = %s\n", ut.to_string(dfullpath).c_str());

	// Create duid directory under RPM folder
	if (INVALID_FILE_ATTRIBUTES == GetFileAttributes(dfullpath.c_str()) && (GetLastError() == ERROR_FILE_NOT_FOUND || GetLastError() == ERROR_PATH_NOT_FOUND))
	{
		if (::CreateDirectoryW(dfullpath.c_str(), NULL) == false)
		{
			// failed, then just use RPM folder
			dfullpath = ut.to_unlimitedpath(std::experimental::filesystem::path(rpmfolder));
		}
	}

	dfullpath =ut.to_unlimitedpath(std::experimental::filesystem::path(dfullpath) / std::experimental::filesystem::path(filename));
	CELOG_LOGA(CELOG_DEBUG, "dfullpath = %s\n", ut.to_string(dfullpath).c_str());

	std::wstring nonnxlfile = ut.remove_extension(dfullpath);
	CELOG_LOGA(CELOG_DEBUG, "nonnxlfile = %s\n", ut.to_string(nonnxlfile).c_str());

	// if both NXL file and decrypted file exists, we will not copy, but directly return the file
	BOOL bExistsNXL = true;
	BOOL bExistsNormal = true;
	if (INVALID_FILE_ATTRIBUTES == GetFileAttributes(dfullpath.c_str()))
		bExistsNXL = false;
	if (INVALID_FILE_ATTRIBUTES == GetFileAttributes(nonnxlfile.c_str()))
		bExistsNormal = false;

	if ((bExistsNormal && false == bExistsNXL) || (false == bExistsNormal))
	{
		// in RPM folder
		if (m_pRmcInstance)
		{
			res = m_pRmcInstance->RPMDeleteFile(nonnxlfile);
			if (!res)
			{
				CELOG_LOGA(CELOG_DEBUG, "RPMDeleteFile failed = %d, %s\n", res.GetCode(), ut.to_string(nonnxlfile).c_str());
			}
		}

		// copy the source file to RPM folder now
		if (CopyFile(fpath.c_str(), dfullpath.c_str(), false))
		{
			// Call FindFirstFile to notify driver there is a new NXL file
			LPCTSTR lpNXLFileName = dfullpath.c_str();
			WIN32_FIND_DATA pNextInfo;
			HANDLE h = FindFirstFile(lpNXLFileName, &pNextInfo);
			if (h != INVALID_HANDLE_VALUE)
			{
				FindClose(h);
			}
		}
		else
		{
			DWORD error = GetLastError();
			CELOG_LOGA(CELOG_DEBUG, "CopyFile failed = %lu, %s\n", error, ut.to_string(dfullpath).c_str());
			res = RESULT2(error, "CopyFile failed");
			CELOG_RETURN_VAL_T(res);
		}
	}

	destpath = nonnxlfile;

	CELOG_RETURN_VAL_T(RESULT(SDWL_SUCCESS));
}


SDWLResult CCnxrmcom::ProjectCreateTempFile(const std::wstring& projectId, const std::wstring& pathId, std::wstring& tmpFilePath)
{
	CommonUtils ut;
	std::wstring s(pathId + L"@" + projectId);
	std::transform(s.begin(), s.end(), s.begin(), ::tolower);
	UCHAR hash[20] = { 0 };
	RetValue ret = RMCCORE::CRYPT::CreateSha1((const unsigned char*)s.c_str(), (ULONG)(s.length() * 2), hash);
	if (!ret)
		return (RESULT2(ERROR_INVALID_DATA, "CreateSha1 failed"));

	tmpFilePath = GetTempDirectory();
	tmpFilePath.append(L"\\");
	tmpFilePath.append(RMCCORE::bintohs<wchar_t>(hash, 20));
	tmpFilePath.append(L".tmp");
	return RESULT(0);
}

std::wstring CCnxrmcom::GetTempDirectory()
{
	PWSTR pszPath = NULL;
	std::wstring root;

	HRESULT hr = SHGetKnownFolderPath(FOLDERID_LocalAppData, KF_FLAG_DEFAULT, NULL, &pszPath);
	if (SUCCEEDED(hr)) {
		root = pszPath;
		root += L"\\NextLabs";
		::CreateDirectoryW(root.c_str(), NULL);
		root += L"\\SkyDRM";
		::CreateDirectoryW(root.c_str(), NULL);
	}
	else
	{
		WCHAR wc[256];
		if (!GetCurrentDirectory(256, wc))
			root = L"c:\\";
		else
			root = wc;
		}
	return root;
}


HRESULT __stdcall CCnxrmcom::ProtectFile(BSTR PlainFilePath, BSTR Tags, BSTR DestinationFolder, BSTR * NxlFilePath)
{
	CommonUtils ut;
	std::wstring _filepath(PlainFilePath);
	std::wstring _tags(Tags);
	std::wstring _destinationFolder(DestinationFolder);

	CELOG_LOG(CELOG_INFO, L"function=%s, PlainFilePath=%s\nTags=%s\nDestinationFolder=%s\n", L"ProtectFile", _filepath.c_str(), _tags.c_str(), _destinationFolder.c_str());
	wstring log;
	log.append(L"function : ProtectFile");
	log.append(L" ");
	log.append(std::wstring(L"PlainFilePath : ") + _filepath);
	log.append(L" ");
	log.append(std::wstring(L"Tags : ") + _tags);
	log.append(L" ");
	log.append(std::wstring(L"DestinationFolder : ")+ _destinationFolder);
	OutputDebugStringW(log.c_str());

	SDWLResult res = RESULT(SDWL_SUCCESS);
	std::wstring json;

	res = GetCurrentLoggedInUser();

	if (res.GetCode() == 0) {

		std::experimental::filesystem::path filePath(_filepath);

		if (filePath.has_extension() == false || icompare((std::wstring)(filePath.extension().c_str()), (std::wstring)(L".nxl")) != 0) {
			// not a nxl file
			// res = RESULT(res);
			res = InnerProtectFile(_filepath, _destinationFolder, _com_util::ConvertBSTRToString(Tags));
		}
		else {
			res = m_pRmUser->ReProtectSystemBucketFile(_filepath);
		}
	}

	std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	std::wstring wMsg = converter.from_bytes(res.GetMsg());
	json = BuildOutputJson(res.GetCode(), _destinationFolder, wMsg);

	CComBSTR outStr(json.c_str());
	*NxlFilePath = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::ViewFile(BSTR NxlFilePath, int Options, BSTR* OutputJson)
{
	std::wstring _nxlFilePath(NxlFilePath);
	CELOG_LOG(CELOG_INFO, L"function=%s, NxlFilePath=%s\nOptions=%d\n", L"ViewFile", _nxlFilePath.c_str(), Options);

	wstring log;
	log.append(L"function : ViewFile");
	log.append(L" ");
	log.append(std::wstring(L"NxlFilePath : ") + _nxlFilePath);
	log.append(L" ");
	log.append(L"Options : " + Options);
	OutputDebugStringW(log.c_str());

	SDWLResult res = RESULT(SDWL_SUCCESS);
	std::wstring json;

	res = GetCurrentLoggedInUser();

	if (res.GetCode() == 0) {
	    res = RESULT(SDWL_SUCCESS);
		std::experimental::filesystem::path _filePath(_nxlFilePath);
		CommonUtils ut;
		if ((_filePath.has_extension() == true) && (ut.StrIcompare(_filePath.extension().wstring(), L".nxl") == true)) {
			HINSTANCE hin = ShellExecuteW(NULL, L"open", _nxlFilePath.c_str(), NULL, NULL, SW_SHOWDEFAULT);
			if (((long)hin < 32)) {
				if ((2L == ((long)hin)) || (3L == ((long)hin))) {
					res = RESULT(ERROR_PATH_NOT_FOUND, "File does not exist.");
				}
				else
				{
					res = RESULT((long)hin, strerror(errno));
				}
			}
		}
		else {
			res = RESULT2(SDWL_INVALID_DATA, "Invalid file name. The file could not be opened.");
		}

		//std::string _Str = _filePath.filename().string();
		//const size_t _Idx = _Str.rfind(_FS_PERIOD);
		//if (!(_Idx == std::string::npos	// no .
		//	|| _Str.size() == 1	// only .
		//	|| (_Str.size() == 2 && _Str[0] == _FS_PERIOD && _Str[1] == _FS_PERIOD))) // only ..	
		//{
		//	_filePath = std::experimental::filesystem::path(_Str.substr(0, _Idx));

		//	if (_filePath.has_extension() == true) {

		//		CommonUtils ut;
		//		FileTypeUtils ft;
		//		FileType fileType = ft.MattchFileType((_filePath.extension().wstring()));

		//		std::wstring winwordExePath;
		//		SDWLResult sdwl = RESULT(ERROR_PATH_NOT_FOUND);
		//		bool isAppRegistered = false;
		//		long regs = 0;
		//		switch (fileType)
		//		{
		//		case WinWord:

		//			regs = ut.NxRegQueryValueEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\Winword.exe", L"", winwordExePath);

		//			if (regs == ERROR_SUCCESS) {
		//				sdwl = m_pRmcInstance->RPMIsAppRegistered(winwordExePath, &isAppRegistered);
		//			}

		//			if ((sdwl.GetCode() == 0) && isAppRegistered) {

		//				if ((0 == Options) && (IsPluginWell(L"Word", L"x64") || IsPluginWell(L"Word", L"x86"))) {

		//					res = ViewFileByNative(_nxlFilePath);
		//				}
		//				else {
		//					res = ViewFileByRMDViewer(_nxlFilePath);
		//				}
		//			}
		//			else {
		//				res = ViewFileByRMDViewer(_nxlFilePath);
		//			}

		//			break;
		//		case PowerPnt:

		//			regs = ut.NxRegQueryValueEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\powerpnt.exe", L"", winwordExePath);

		//			if (regs == ERROR_SUCCESS) {
		//				sdwl = m_pRmcInstance->RPMIsAppRegistered(winwordExePath, &isAppRegistered);
		//			}

		//			if ((sdwl.GetCode() == 0) && isAppRegistered) {

		//				if ((0 == Options) && (IsPluginWell(L"PowerPoint", L"x64") || IsPluginWell(L"PowerPoint", L"x86"))) {

		//					res = ViewFileByNative(_nxlFilePath);
		//				}
		//				else {
		//					res = ViewFileByRMDViewer(_nxlFilePath);
		//				}
		//			}
		//			else {
		//				res = ViewFileByRMDViewer(_nxlFilePath);
		//			}

		//			break;
		//		case Excel:

		//			regs = ut.NxRegQueryValueEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\excel.exe", L"", winwordExePath);

		//			if (regs == ERROR_SUCCESS) {
		//				sdwl = m_pRmcInstance->RPMIsAppRegistered(winwordExePath, &isAppRegistered);
		//			}

		//			if ((sdwl.GetCode() == 0) && isAppRegistered) {

		//				if ((0 == Options) && (IsPluginWell(L"Excel", L"x64") || IsPluginWell(L"Excel", L"x86"))) {

		//					res = ViewFileByNative(_nxlFilePath);
		//				}
		//				else {
		//					res = ViewFileByRMDViewer(_nxlFilePath);
		//				}
		//			}
		//			else {
		//				res = ViewFileByRMDViewer(_nxlFilePath);
		//			}

		//			break;
		//		case Pdf:
		//				res = ViewFileByRMDViewer(_nxlFilePath);
		//			break;
		//		case Hoops:
		//			res = ViewFileByRMDViewer(_nxlFilePath);
		//			break;
		//		case Audio:
		//			res = ViewFileByRMDViewer(_nxlFilePath);
		//			break;
		//		case Video:
		//			res = ViewFileByRMDViewer(_nxlFilePath);
		//			break;
		//		case Image:
		//			res = ViewFileByRMDViewer(_nxlFilePath);
		//			break;
		//		case Text:
		//			res = ViewFileByRMDViewer(_nxlFilePath);
		//			break;
		//		case Vds:
		//			res = ViewFileByRMDViewer(_nxlFilePath);
		//			break;
		//		case UnSupport:
		//			res = res = RESULT2(ERROR_UNSUPPORTED_FILE_TYPE, "The file type is not supported.");
		//			break;
		//		default:
		//			break;
		//		}
		//	}
		//	else {
		//		res = RESULT2(SDWL_INVALID_DATA, "Invalid file name. The file could not be opened.");
		//	}
		//}
		//else {

		//	res = RESULT2(SDWL_INVALID_DATA, "Invalid file name. The file could not be opened.");
		//}
	}

	std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	std::wstring wMsg = converter.from_bytes(res.GetMsg());
	json = BuildOutputJson(res.GetCode(), L"", wMsg);

	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::IsUserLogined(BSTR * OutputJson)
{
	CELOG_LOG(CELOG_INFO, L"function=%s", L"IsUserLogined");
	wstring log;
	log.append(L"function : IsUserLogined");
	OutputDebugStringW(log.c_str());

	SDWLResult res = RESULT(SDWL_SUCCESS);

	res = GetCurrentLoggedInUser();

	std::wstring value;
	std::wstring json;

	value = std::to_wstring(res.GetCode());
	std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	std::wstring wMsg = converter.from_bytes(res.GetMsg());
	json = BuildOutputJson(res.GetCode(), value, wMsg);

	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::LockFileSync(BSTR NxlFilePath, BSTR * OutputJson)
{
	std::wstring _nxlFilePath(NxlFilePath);
	wstring log;
	log.append(L"function : LockFileSync");
	log.append(L" ");
	log.append(std::wstring(L"NxlFilePath : ") + _nxlFilePath);
	OutputDebugStringW(log.c_str());

	SDWLResult res = RESULT(SDWL_SUCCESS);
	std::wstring json;

	res = GetCurrentLoggedInUser();

	if (res.GetCode() == 0) 
	{
		res = m_pRmUser->LockFileSync(_nxlFilePath);
	}

	std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	std::wstring wMsg = converter.from_bytes(res.GetMsg());
	json = BuildOutputJson(res.GetCode(), L"", wMsg);

	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);

}

HRESULT __stdcall CCnxrmcom::ResumeFileSync(BSTR NxlFilePath, BSTR * OutputJson)
{
	std::wstring _nxlFilePath(NxlFilePath);
	wstring log;
	log.append(L"function : ResumeFileSync");
	log.append(L" ");
	log.append(std::wstring(L"NxlFilePath : ") + _nxlFilePath);
	OutputDebugStringW(log.c_str());

	SDWLResult res = RESULT(SDWL_SUCCESS);
	std::wstring json;

	res = GetCurrentLoggedInUser();

	if (res.GetCode() == 0) 
	{
		res = m_pRmUser->ResumeFileSync(_nxlFilePath);
	}

	std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	std::wstring wMsg = converter.from_bytes(res.GetMsg());
	json = BuildOutputJson(res.GetCode(), L"", wMsg);

	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::AddRPMDir(BSTR Path, unsigned int Option, BSTR * OutputJson)
{
	std::wstring _Path(Path);
	wstring log;
	log.append(L"function : AddRPMDir");
	log.append(L" ");
	log.append(std::wstring(L"Path : ") + _Path);
	log.append(L" ");
	log.append(L"Option : " + Option);
	OutputDebugStringW(log.c_str());

	SDWLResult res = RESULT(SDWL_SUCCESS);
	std::wstring json;
	res = GetCurrentLoggedInUser();
	if (res.GetCode() == 0)
	{
		res = m_pRmcInstance->AddRPMDir(_Path, Option);
	}

	nlohmann::json j = nlohmann::json::object();
	j["code"] = res.GetCode();
	j["value"] = utf16toutf8(_Path);
	j["message"] = res.GetMsg();
	std::string str = j.dump();
	json = utf8toutf16(str);

	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::RemoveRPMDir(BSTR Path, BSTR * OutputJson)
{
	std::wstring _Path(Path);
	wstring log;
	log.append(L"function : RemoveRPMDir");
	log.append(L" ");
	log.append(std::wstring(L"Path : ") + _Path);
	OutputDebugStringW(log.c_str());

	SDWLResult res = RESULT(SDWL_SUCCESS);
	std::wstring json;

	res = GetCurrentLoggedInUser();
	if (res.GetCode() == 0)
	{
		res = m_pRmcInstance->RemoveRPMDir(_Path);
	}

	nlohmann::json j = nlohmann::json::object();
	j["code"] = res.GetCode();
	j["value"] = utf16toutf8(_Path);
	j["message"] = res.GetMsg();
	std::string str = j.dump();
	json = utf8toutf16(str);

	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::RPMAddTrustedProcess(unsigned long ProcessId, BSTR Security, BSTR * OutputJson)
{
	CELOG_ENTER;
	CELOG_LOG(CELOG_INFO, L"function=%s, ProcessId=%lu, Security=%s", L"RPMAddTrustedProcess", ProcessId, Security);
	wstring log;
	log.append(L"function : RPMAddTrustedProcess");
	log.append(L" ");
	log.append(L"ProcessId : " + ProcessId);
	OutputDebugStringW(log.c_str());

	CommonUtils utils;
	std::wstring json;
	std::wstring wstrSecurity(Security);
	std::string strSecurity = utils.utf16toutf8(wstrSecurity);
	SDWLResult res = RESULT(SDWL_SUCCESS);
	res = GetCurrentLoggedInUser();
	if (res.GetCode() == 0)
	{
		res = m_pRmcInstance->RPMAddTrustedProcess(ProcessId, strSecurity);
	}

	json = BuildOutputJson(res.GetCode(), L"", utils.utf8toutf16(res.GetMsg()));

	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::RPMRemoveTrustedProcess(unsigned long ProcessId, BSTR Security, BSTR * OutputJson)
{
	CELOG_ENTER;
	CELOG_LOG(CELOG_INFO, L"function=%s, ProcessId=%lu, Security=%s", L"RPMRemoveTrustedProcess", ProcessId, Security);
	wstring log;
	log.append(L"function : RPMRemoveTrustedProcess");
	log.append(L" ");
	log.append(L"ProcessId : " + ProcessId);
	OutputDebugStringW(log.c_str());

	CommonUtils utils;
	std::wstring json;
	std::wstring wstrSecurity(Security);
	std::string strSecurity = utils.utf16toutf8(wstrSecurity);
	SDWLResult res = RESULT(SDWL_SUCCESS);
	res = GetCurrentLoggedInUser();
	if (res.GetCode() == 0)
	{
		res = m_pRmcInstance->RPMRemoveTrustedProcess(ProcessId, strSecurity);
	}

	json = BuildOutputJson(res.GetCode(), L"", utils.utf8toutf16(res.GetMsg()));

	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::RPMRegisterApp(BSTR AppPath, BSTR Security, BSTR * OutputJson)
{
	CELOG_ENTER;
	CELOG_LOG(CELOG_INFO, L"function=%s, AppPath=%s, Security=%s", L"RPMRegisterApp", AppPath, Security);
	wstring _appPath(AppPath);
	wstring log;
	log.append(L"function : RPMRegisterApp");
	log.append(L" ");
	log.append(std::wstring(L"AppPath : ") + _appPath);
	OutputDebugStringW(log.c_str());

	CommonUtils utils;
	std::wstring json;
	std::wstring wstrSecurity(Security);
	std::string strSecurity = utils.utf16toutf8(wstrSecurity);
	SDWLResult res = RESULT(SDWL_SUCCESS);

	res = GetCurrentLoggedInUser();
	if (res.GetCode() == 0)
	{
		res = m_pRmcInstance->RPMRegisterApp(_appPath, strSecurity);
	}

	json = BuildOutputJson(res.GetCode(), L"", utils.utf8toutf16(res.GetMsg()));
	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

HRESULT __stdcall CCnxrmcom::RPMNotifyRMXStatus(boolean Running, BSTR Security, BSTR * OutputJson)
{
	CELOG_ENTER;
	CELOG_LOG(CELOG_INFO, L"function=%s, Running=%d, Security=%s", L"RPMNotifyRMXStatus", Running, Security);

	wstring log;
	log.append(L"function : RPMNotifyRMXStatus");
	log.append(L" ");
	log.append(L"Running : " + Running);
	OutputDebugStringW(log.c_str());

	CommonUtils utils;
	std::wstring json;
	std::wstring wstrSecurity(Security);
	std::string strSecurity = utils.utf16toutf8(wstrSecurity);
	SDWLResult res = RESULT(SDWL_SUCCESS);

	res = GetCurrentLoggedInUser();
	if (res.GetCode() == 0)
	{
		res = m_pRmcInstance->RPMNotifyRMXStatus(Running, strSecurity);
	}

	json = BuildOutputJson(res.GetCode(), L"", utils.utf8toutf16(res.GetMsg()));
	CComBSTR outStr(json.c_str());
	*OutputJson = outStr.Detach();

	return HRESULT_FROM_WIN32(0);
}

//HRESULT __stdcall CCnxrmcom::AddRPMDir(BSTR Path, unsigned int Option, BSTR * OutputJson)
//{
//	std::wstring _Path(Path);
//	SDWLResult res = RESULT(SDWL_SUCCESS);
//	std::wstring json;
//
//	//if (0 == Option) {
//	//	Option = (SDRmRPMFolderOption::RPMFOLDER_NORMAL | SDRmRPMFolderOption::RPMFOLDER_API);
//	//}
//
//	res = GetCurrentLoggedInUser();
//	if (res.GetCode() == 0)
//	{
//		res = m_pRmcInstance->AddRPMDir(_Path, Option);
//	}
//
//	nlohmann::json j = nlohmann::json::object();
//	j["code"] = res.GetCode();
//	j["value"] = utf16toutf8(_Path);
//	j["message"] = res.GetMsg();
//	std::string str = j.dump();
//	json = utf8toutf16(str);
//
//	CComBSTR outStr(json.c_str());
//	*OutputJson = outStr.Detach();
//
//	return HRESULT_FROM_WIN32(0);
//}

//HRESULT __stdcall CCnxrmcom::RemoveRPMDir(BSTR Path, BSTR * OutputJson)
//{
//	std::wstring _Path(Path);
//	SDWLResult res = RESULT(SDWL_SUCCESS);
//	std::wstring json;
//
//	res = GetCurrentLoggedInUser();
//	if (res.GetCode() == 0)
//	{
//		res = m_pRmcInstance->RemoveRPMDir(_Path);
//	}
//
//	nlohmann::json j = nlohmann::json::object();
//	j["code"] = res.GetCode();
//	j["value"] = utf16toutf8(_Path);
//	j["message"] = res.GetMsg();
//	std::string str = j.dump();
//	json = utf8toutf16(str);
//
//	CComBSTR outStr(json.c_str());
//	*OutputJson = outStr.Detach();
//
//	return HRESULT_FROM_WIN32(0);
//}

SDWLResult __stdcall CCnxrmcom::GetCurrentLoggedInUser()
{
	CELOG_ENTER;
	CELOG_LOG(CELOG_INFO, L"function=%s", L"GetCurrentLoggedInUser");
	CommonUtils ut;
	std::string strPasscode = ut.GetRMXPasscode();

	if (NULL != m_pRmcInstance) {
		delete m_pRmcInstance;
		m_pRmcInstance = NULL;
	}

	SDWLResult res = RPMGetCurrentLoggedInUser(strPasscode, m_pRmcInstance, m_pRmTenant, m_pRmUser);
	CELOG_RETURN_VAL_T(res);
}


SDWLResult __stdcall CCnxrmcom::ViewFileByRMDViewer(std::wstring filePath)
{
	CELOG_ENTER;
	CELOG_LOG(CELOG_INFO, L"function=%s, filePath=%s", L"ViewFileByRMDViewer", filePath.c_str());

	SDWLResult res = RESULT(SDWL_SUCCESS);
	HINSTANCE hin = ShellExecuteW(NULL, L"open", filePath.c_str(), NULL, NULL, SW_SHOWDEFAULT);
	if (((long)hin < 32)) {
		if ((2L == ((long)hin)) || (3L == ((long)hin))) {
			res = RESULT(ERROR_PATH_NOT_FOUND, "File does not exist.");
		}
		else
		{
			res = RESULT((long)hin, strerror(errno));
		}
	}
	CELOG_RETURN_VAL_T(res);
}

SDWLResult __stdcall CCnxrmcom::ViewFileByNative(std::wstring filePath)
{
	CELOG_ENTER;
	CELOG_LOG(CELOG_INFO, L"function=%s, filePath=%s", L"ViewFileByNative", filePath.c_str());

	std::wstring outPath = L"";

	SDWLResult res = m_pRmcInstance->RPMEditCopyFile(filePath, outPath);

	if (res.GetCode() == 0) {
		HINSTANCE hin = ShellExecuteW(NULL, L"open", outPath.c_str(), NULL, NULL, SW_SHOWDEFAULT);
		if (((long)hin < 32)) {
			if ( (2L == ((long)hin)) || (3L == ((long)hin))) {
				res = RESULT(ERROR_PATH_NOT_FOUND,"File does not exist.");
			}
			else
			{
				res = RESULT((long)hin, strerror(errno));
			}
		}
	}

	CELOG_RETURN_VAL_T(res);
}



std::wstring __stdcall CCnxrmcom::BuildOutputJson(int code, const std::wstring &value, std::wstring msg)
{
	CELOG_LOG(CELOG_INFO, L"function=%s, code=%d, value=%s, msg=%s", L"BuildOutputJson", code, value.c_str(), msg.c_str());

	switch (code)
	{
		case SDWL_PATH_NOT_FOUND:
			msg = L"File does not exist.";
			break;
		case SDWL_LOGIN_REQUIRED:
			msg = L"Login to open this file.";
			break;
		case SDWL_INVALID_DATA:
			msg = L"Invalid NXL file.";
			break;
		case SDWL_RMS_ERRORCODE_BASE + HTTP_STATUS_FORBIDDEN:
		case SDWL_RMS_ERRORCODE_BASE + HTTP_STATUS_NOT_FOUND:
			msg = L"You are not authorized to perform the operation.";
			break;
		case SDWL_RMS_ERRORCODE_BASE + 4005:
			msg = L"Cannot access the file as permission has changed.";
			break;
		default:
			break;
	}

	//RMCCORE::JsonValue * json_request = RMCCORE::JsonValue::CreateObject();
	//json_request->AsObject()->set("code", RMCCORE::JsonValue::CreateNumber(code));
	//json_request->AsObject()->set("value", RMCCORE::JsonValue::CreateString(utf16toutf8(value)));
	//json_request->AsObject()->set("message", RMCCORE::JsonValue::CreateString(utf16toutf8(msg)));
	//RMCCORE::JsonStringWriter writer;
	//std::string str;
	//str = writer.Write(json_request);
	//wstring json = utf8toutf16(str);
	//delete json_request;
	//auto json = R"(
    //    {
	//     "code": 3.141,
	//	 "value": 3.141,
	//	 "message":3.141
    //    }
	//)"_json;

	nlohmann::json j = nlohmann::json::object();
	j["code"] = code;
	j["value"] = utf16toutf8(value);
	j["message"] = utf16toutf8(msg);
	std::string str = j.dump();
	wstring jsonStr = utf8toutf16(str);
	
	CELOG_LOG(CELOG_INFO, L"function=%s, output json=%s", L"BuildOutputJson", jsonStr);
	return jsonStr;
}




