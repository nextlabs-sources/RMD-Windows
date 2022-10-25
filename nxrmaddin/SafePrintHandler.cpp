#include "stdafx.h"
#include "CommonFunction.h"
#include "SafePrintHandler.h"
#include "SkyDrmSDKMgr.h"
#include <experimental/filesystem>

//#include <winspool.h>

namespace {
	inline bool is_file_exist(const wchar_t* file)
	{
		DWORD dwAttrib = GetFileAttributesW(file);
		//0 == (dwAttrib & FILE_ATTRIBUTE_DIRECTORY);  -> this is not dir
		return (INVALID_FILE_ATTRIBUTES != dwAttrib) && ( 0 == (dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
	}
	inline bool is_folder_exist(const wchar_t* dir) {
		DWORD dwAttrib = GetFileAttributesW(dir);
		return (INVALID_FILE_ATTRIBUTES != dwAttrib) &&  (0!= (dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
	}


	class ScopeGuard {
	public:
		explicit ScopeGuard(std::function<void()> onLeaveScope) :_onLeaveScope(onLeaveScope), _dismissd(false)  {}
		~ScopeGuard() { if (!_dismissd) _onLeaveScope(); }
		inline void dismiss() { _dismissd = true; }
	private: // nocopyable
		ScopeGuard(ScopeGuard const&);
		ScopeGuard& operator=(ScopeGuard const&);
	private:
		std::function<void()> _onLeaveScope;
		bool _dismissd;
	};

	// for a very large file, 20s is not enought perhaps, using 30s like Network
	// do not need MOVEFILE_WRITE_THROUGH, if you can guarantine
	inline BOOL API_Wrapper_MoveFileW(LPCWSTR lpExistingFileName,LPCWSTR lpNewFileName ) {

		//for sharing violation, give 30s max tolerating
		int max_retry = 300;
		int each_interval_retry_if_sharing_violation = 200;  
		// max toleration time is 30 secounds; 
		ULONG fullfilesize = 0;
		BOOL bfullfile = false;
		BOOL bdelegatetoservice = false;
		int zero_retry = 0;
		while (max_retry--) {
			HANDLE h = INVALID_HANDLE_VALUE;
			do {

				h = ::CreateFileW(lpExistingFileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
				if (INVALID_HANDLE_VALUE == h) {
					break;
				}

				const ULONG fileSize = GetFileSize(h, NULL);
				if (fileSize == INVALID_FILE_SIZE) {
					break;
				}

				if (fileSize == 0) {
					if (zero_retry > 2)
					{
						bdelegatetoservice = true;
						// if the output file is a NXL file to be overwritten, we must ask service to copy
						// as driver will redirect spool.exe to "path.nxl"
					}
					zero_retry++;
					break;
				}

				if (fileSize > fullfilesize)
				{
					fullfilesize = fileSize;
					break;
				}
				else if (fileSize == fullfilesize)
				{
					bfullfile = true;
					break;
				}
			} while (FALSE);
			::CloseHandle(h);

			if (bdelegatetoservice)
				break;

			if (bfullfile)
			{
				if (0 == ::MoveFileExW(lpExistingFileName, lpNewFileName,
					MOVEFILE_COPY_ALLOWED | MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH)) 
				{
					DWORD  err = ::GetLastError();
					if (err == ERROR_SHARING_VIOLATION) {  //0X20 32L
						::Sleep(each_interval_retry_if_sharing_violation);
						continue;
					}
					return false;
				}
				else {
					return true;
				}
			}

			::Sleep(each_interval_retry_if_sharing_violation);
		}

		if (bdelegatetoservice)
		{
			// if the output file is a NXL file to be overwritten, we must ask service to copy
			// as driver will redirect spool.exe to "path.nxl"

			// bad bug, for PowerPoint, after print finished, PowerPoint still hold the file handle
			// need to wait for it to release file handle. in future, when service do file copy, it shall check file locked or not
			// for now, we just sleep 1 second for PowerPoint
			std::wstring processname = CommonFunction::GetProcessName(0);
			std::transform(processname.begin(), processname.end(), processname.begin(), ::toupper);
			if (processname.find(L"POWERPNT.EXE") != std::wstring::npos) {
				::Sleep(1000);
			}

			// another bad bug, in some case, winspool.exe didn't frush the generated file to disk
			// so we always get 0kb for file size, we must make sure the file can be read/write
			// the good way is to let RPM service to check.
			// for now, we just try to open the source file in 60 seconds
			HANDLE rw_h = NULL;
			int rw_max_retry = 300;
			while (rw_max_retry--) {
				rw_h = ::CreateFileW(lpExistingFileName, GENERIC_READ | GENERIC_WRITE, NULL, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
				if (INVALID_HANDLE_VALUE == rw_h) {
					::Sleep(each_interval_retry_if_sharing_violation);
					continue;
				}
				::CloseHandle(rw_h);
				break;
			}
			if (SkyDrmSDKMgr::Instance()->DelegateCopyFile(lpExistingFileName, lpNewFileName, true))
			{
				// call service to copy
				return true;
			}
		}
		return false;
	}

	inline BOOL API_Wrapper_DeleteFileW(LPCWSTR lpFileName) {
		//for sharing violation, give 30s max tolerating
		int max_retry = 100;
		int each_interval_retry = 800;
		// max toleration time is 30 secounds; 
		while (max_retry--) {
			if (0 == ::DeleteFileW(lpFileName)) {
				DWORD  err = ::GetLastError();
				// begin log
#ifdef _DEBUG
				std::wstring str = L"calling DeleteFile Failed, rt=";
				str += std::to_wstring(err);
				str += L"\n";
				DEVLOG(str.c_str());
#endif // _DEBUG
				//end log
				if (err = ERROR_FILE_NOT_FOUND) {
					// this has been deleted by others
					return true;
				}
				if (err == ERROR_SHARING_VIOLATION) {  //0X20 32L
					::Sleep(each_interval_retry);
					continue;
				}
				return false;
			}
			else {
				return true;
			}
		}
		return false;

	}

	// by omsond, I dont know why we have to use this code,
	// but as Allen.Yuen's comments, if you donot use it, RPM may got very strong problems
	// another annoying issue is  if calling in Excel, whole process may blow up;
	inline void Workaround_Code_MakeSure_RPM_Works(const std::wstring path) {
		WIN32_FIND_DATA FindFileData;
		auto h= ::FindFirstFileW(path.c_str(), &FindFileData);
		if (h == INVALID_HANDLE_VALUE) {
			return;
		}
		::FindClose(h);
	}

}



typedef  DWORD (WINAPI *pfn_EnumJobNamedProperties)(HANDLE  hPrinter,
	DWORD              JobId,
	DWORD* pcProperties,
	PrintNamedProperty** ppProperties
);


pfn_EnumJobNamedProperties my_EnumJobNamedProperties = NULL;


SafePrintHandler::SafePrintHandler() {
	if (!my_EnumJobNamedProperties) {
		const wchar_t* dll = L"WINSPOOL.DRV";
		const char* fun_name = "EnumJobNamedProperties";
		auto h = ::LoadLibraryW(dll);
		auto fun = ::GetProcAddress(h, fun_name);
		my_EnumJobNamedProperties = (pfn_EnumJobNamedProperties)fun;
	}

	::InitializeCriticalSection(&_lock);
}


SafePrintHandler::~SafePrintHandler() {

}


bool SafePrintHandler::Is_Printer_Name_In_Trusted_List(const std::wstring printer_name)
{
	// sanity check
	if (printer_name.empty()) {
		return false; // this is allow_only type policy
	}

	/*
	Registry{
		HKEY = Computer\HKEY_LOCAL_MACHINE\SOFTWARE\NextLabs\SkyDRM
		Value: TrustedPrinters  (REG_SZ)
		Value_Data:  printer_name_a;printer_name_b;printer_name_c
	}
	*/
	std::wstring trusted_printers;
	{
		nextlabs::utils::Registry r;
		nextlabs::utils::Registry::param param(LR"_(SOFTWARE\NextLabs\SkyDRM)_");
		if (!r.get(param, L"TrustedPrinters", trusted_printers)) {
			trusted_printers.clear();
		}
	}

	if (trusted_printers.empty()) {
		return false; // allow_only policy, required a must default configuration
	}

	return nextlabs::utils::iconstain(trusted_printers, printer_name);
}

bool SafePrintHandler::Is_Printer_Name_In_Prohibited_List(const std::wstring printer_name)
{
	// Fax connot be supported
	if (0 == _wcsicmp(L"Fax", printer_name.c_str())) {
		return true;
	}
	//// OneNote series will get data leak, must prevent using this printer for nxl file.
	//if (0 != wcsstr(printer_name.c_str(), L"OneNote" )) {
	//	return true;
	//}
	//// deny WebEx serise
	//if (0 != wcsstr(printer_name.c_str(), L"WebEx")) {
	//	return true;
	//}

	// deny Adobe PDF
	if (0 != wcsstr(printer_name.c_str(), L"Adobe PDF")) {
		return true;
	}
	// deny CutePDF Writer
	if (0 != wcsstr(printer_name.c_str(), L"CutePDF Writer")) {
		return true;
	}

	//// for win7 , we can not support safe_print currently, so disable it first
	//if (!my_EnumJobNamedProperties) {
	//	// win7 deny, pdf
	//	if (0 != wcsstr(printer_name.c_str(), L"PDF")) {
	//		return true;
	//	}
	//	// win7 deny, xps
	//	if (0 != wcsstr(printer_name.c_str(), L"XPS")) {
	//		return true;
	//	}
	//}

	// TBD: 
	// Other blocking items can be configrued by PDP at run time to match

	return false;
}

bool SafePrintHandler::Is_Printer_Name_Support_Safe_Print(const std::wstring printer_name)
{
	//// for win7 , we can not support safe_print currently, so disable it first
	//if (!my_EnumJobNamedProperties) {
	//	// win7 deny, pdf
	//	if (0 != wcsstr(printer_name.c_str(), L"PDF")) {
	//		return false;
	//	}
	//	// win7 deny, xps
	//	if (0 != wcsstr(printer_name.c_str(), L"XPS")) {
	//		return false;
	//	}
	//}
	//// we don't need exactly match
	////static wchar_t pdfPrinter[] = L"Microsoft Print to PDF"; 
	////
	//// for nomral, currently we can support and test the PDF and XPS can be supported
	////
	//if (0 != wcsstr(printer_name.c_str(), L"PDF")) {
	//	return true;
	//}
	//// win7 deny, xps
	//if (0 != wcsstr(printer_name.c_str(), L"XPS")) {
	//	return true;
	//}

	return false;
}

bool SafePrintHandler::Modify_JobID(HDC h, int jobID) { 

	if (!Contain(h)) {
		return false;
	}
	m_hs[h].jobID = jobID;
	return true;
}

int SafePrintHandler::Get_JobID(HDC h) {

	if (!Contain(h)) {
		return -1;
	}
	return m_hs[h].jobID;
}

SafePrintHandler::Printer_Result SafePrintHandler::Enforce_Security_Policy(const std::wstring& printer_name)
{
	//
	// $ ALLOW_ONLY policy, only trusted printer_name can be used for nxl file printing
	if (Is_Printer_Name_In_Trusted_List(printer_name)) {
		return Result_True_Support_Safe_Print;
	}

	//
	// $ filter out printers which is reside in build-in prohibited list
	if (Is_Printer_Name_In_Prohibited_List(printer_name)) {
		
		return Result_False_Match_In_Prohibited_List;
	}
	//
	// $ record the handle of the printer that can support Safe-print-output2file feature;
	if (Is_Printer_Name_Support_Safe_Print(printer_name)) {
		return Result_True_Support_Safe_Print;
	}

	return Result_True_General;
}

SafePrintHandler::Printer_Result SafePrintHandler::Enforce_Security_Policy(HDC h)
{
	if (Contain(h) == false)
		return Result_False_Match_In_Prohibited_List;
	
	Value printer = m_hs[h];
	return Enforce_Security_Policy(printer.printerName);
}

void SafePrintHandler::Remove(HDC h)
{
	m_hs.erase(h);
	m_h2nxl.erase(h);
}

bool SafePrintHandler::GetOutputPath_IfHDCIsValid(HDC h, std::vector<std::wstring>& path)
{
	path.clear();

	if (!Contain(h)) {
		return false;
	}

	// for win7, can not be supported
	if (!my_EnumJobNamedProperties) {
		return false;
	}

	std::vector<std::wstring> job_outputs;
	HANDLE hPrint = NULL;
	if (!::OpenPrinterW((LPWSTR)m_hs[h].printerName.c_str(), &hPrint, NULL)) {
		return false;
	}
	if (hPrint == NULL) {
		return false;
	}

	DWORD count = 0;
	PrintNamedProperty* pPNP = NULL;
	my_EnumJobNamedProperties(hPrint, m_hs[h].jobID, &count, &pPNP);
	if (!pPNP) {
		::ClosePrinter(hPrint);
		return false;
	}
	for (int i = 0; i < count; ++i) {
		if (pPNP[i].propertyValue.ePropertyType == EPrintPropertyType::kPropertyTypeString) {
			std::experimental::filesystem::path job_path(pPNP[i].propertyValue.value.propertyString);
			if (job_path.has_extension())
			{
				std::wstring _fpath = pPNP[i].propertyValue.value.propertyString;
				path.push_back(_fpath);
				job_outputs.push_back(job_path);
			}
			else
			{
				auto& handler = SafePrintHandler::Instance();

				if (handler.Enforce_Security_Policy((LPWSTR)m_hs[h].printerName.c_str()) == SafePrintHandler::Result_True_Support_Safe_Print)
				{
					// 
					// printer is in our whitelist
					//
					job_outputs.push_back(pPNP[i].propertyValue.value.propertyString);
				}
			}
		}
	}
	::ClosePrinter(hPrint);


	if (job_outputs.size() > 0)
		return true;

	return false;

	//HANDLE hPrint = NULL;
	//if (!::OpenPrinterW((LPWSTR)m_hs[h].printerName.c_str(), &hPrint, NULL)) {
	//	return false;
	//}
	//if (hPrint == NULL) {
	//	return false;
	//}

	//DWORD count = 0;
	//PrintNamedProperty* pPNP = NULL;
	//my_EnumJobNamedProperties(hPrint, m_hs[h].jobID, &count, &pPNP);
	//if (pPNP) {
	//	for (int i = 0; i < count; ++i) {
	//		static const wchar_t propertyName[] = L"MsPrintJobOutputFile";

	//		if (0 == _wcsicmp(pPNP[i].propertyName, propertyName) &&
	//			pPNP[i].propertyValue.ePropertyType == EPrintPropertyType::kPropertyTypeString
	//			) {
	//			path.assign(pPNP[i].propertyValue.value.propertyString);
	//			::ClosePrinter(hPrint);				
	//			return !path.empty();
	//		}
	//		else {
	//			continue;
	//		}
	//	}
	//}
	//
	//// res resource
	//::ClosePrinter(hPrint);
	//return false;
}

bool SafePrintHandler::GetNxltempaltePath_IfHDCIsValid(HDC h, std::wstring& path)
{
	path.clear();
	if (m_h2nxl.count(h) == 0) {
		return false;
	}
	path = m_h2nxl[h];
	return true;
}



// move to tmp folder first, then finshed the nxl_processing in tmp folder, finally, move safe file back to org
std::wstring SafePrintHandler::Convert_PrintedFile_To_NXL(const std::wstring& org_plain_file_path, const std::wstring& current_nxl_template)
{
	
	std::wstring new_nxl_file;

	ScopeGuard auto_deleter([this, &org_plain_file_path]() {
		this->clear_user_tmp_folder();
		// whether succeed or failed, delete the org_plain_file_path
		//::API_Wrapper_DeleteFileW(org_plain_file_path.c_str()); // prevent data-leak, if can not move, delete org file	
		});

	// make sure thread switch
	::SwitchToThread();

	if (!is_file_exist(org_plain_file_path.c_str())) {
        // bug #63050, the printed pdf/xps file is not ready yet
        int rw_max_retry = 50;
        bool bready = false;
        while (rw_max_retry--) {
            if (is_file_exist(org_plain_file_path.c_str()))
            {
                bready = true;
                break;
            }
            ::Sleep(200);
        }

        if (bready == false)
		    return new_nxl_file;
	}

	// RMP weired request
	//Workaround_Code_MakeSure_RPM_Works(org_plain_file_path);
	

	// only care about valid nxl 
	if (!CommonFunction::IsNXLFile(current_nxl_template.c_str())) {
		return new_nxl_file;
	}

	// move file to tmp folder
	std::wstring org_plain_file_name=::PathFindFileNameW(org_plain_file_path.c_str());
	std::wstring org_plain_file_folder= org_plain_file_path;
	{
		while (org_plain_file_folder.back() != '\\') {
			org_plain_file_folder.pop_back();
		}
	}

	
	std::wstring org_file_in_tmp_folder;
	if (!move_file_to_tmp_folder(org_plain_file_path, org_file_in_tmp_folder)) {
		// file can not be moved
		return new_nxl_file;
	}
	
	// convert to nxl ,for failed, must delete org file.
	if (!SkyDrmSDKMgr::Instance()->CovnerFrom_Wrapper(org_file_in_tmp_folder, current_nxl_template, new_nxl_file)) {
		// rmc api got some problems
		::API_Wrapper_DeleteFileW(org_file_in_tmp_folder.c_str()); // delete org in case of Data-Leak
		new_nxl_file.clear();
		return new_nxl_file;
	}

	// move nxl file back from tmp folder to org_plain_file_folder
	auto rt_value = get_no_clash_nxl_file_path(org_plain_file_folder, org_plain_file_name);
	if (!::API_Wrapper_MoveFileW(new_nxl_file.c_str(), rt_value.c_str())) {
		// can not move it back
		return new_nxl_file;
	}

	return rt_value;
}



bool SafePrintHandler::get_user_tmp_folder(std::wstring& path)
{
	wchar_t tmp[0x100] = { 0 };
	if (FAILED(::SHGetFolderPathW(NULL, CSIDL_LOCAL_APPDATA, NULL, SHGFP_TYPE_CURRENT, tmp))) {
		return false;
	}
	if (!::PathAppendW(tmp, LR"_(NextLabs\SkyDRM\tmp\office\)_")) {
		return false;
	}
	// give a guid now, to make it unique by current	
	GUID guid = { 0 };
	if (FAILED(::CoCreateGuid(&guid))) {
		return false;
	}

	OLECHAR* guidStr = NULL;
	if (FAILED(::StringFromCLSID(guid, &guidStr))) {
		return false;
	}

	if (!::PathAppendW(tmp, guidStr)) {
		return false;
	}

	::CoTaskMemFree(guidStr);

	// make the final composite path-folder exist

	::SHCreateDirectoryEx(NULL, tmp, NULL);  // assume always succeed

	path.assign(tmp);
	return true;
}

void SafePrintHandler::clear_user_tmp_folder()
{
	wchar_t tmp[0x100] = { 0 };
	if (FAILED(::SHGetFolderPathW(NULL, CSIDL_LOCAL_APPDATA, NULL, SHGFP_TYPE_CURRENT, tmp))) {
		return ;
	}
	if (!::PathAppendW(tmp, LR"_(NextLabs\SkyDRM\tmp\office\)_")) {
		return ;
	}

	using namespace ATL;
	CComPtr<IFileOperation> spFO;

	if (FAILED(spFO.CoCreateInstance(__uuidof(FileOperation)))) {
		return;
	}
	spFO->SetOperationFlags(FOF_NO_UI);
	
	// clear folder contents
	CComPtr<IShellItem> spItem;
	CComPtr<IEnumShellItems> spItems;
	if (FAILED(::SHCreateItemFromParsingName(tmp, NULL, __uuidof(IShellItem), (void**)&spItem))) {
		return;
	}
	if (FAILED(spItem->BindToHandler(NULL, BHID_EnumItems, _uuidof(IEnumShellItems), (void**)&spItems))) {
		return;
	}
	if (FAILED(spFO->DeleteItems(spItems))) {
		return;
	}
	spFO->PerformOperations();
}

bool SafePrintHandler::move_file_to_tmp_folder(const std::wstring& org_plain_file_path, std::wstring& out_moved_file_path)
{
	// move org file into tmp folder 
	std::wstring tmp_folder;
	if (!get_user_tmp_folder(tmp_folder)) {
		// can not get the tmp folder
		return false;
	}
	std::wstring org_file_name = ::PathFindFileNameW(org_plain_file_path.c_str());
	std::wstring rt_nxl_file_name = org_file_name + L".nxl";

	// Move File
	if (tmp_folder.back() != '\\' ) {
		tmp_folder.push_back('\\');
	}

	out_moved_file_path = tmp_folder + org_file_name;

	if (!::API_Wrapper_MoveFileW(org_plain_file_path.c_str(), out_moved_file_path.c_str())) {
		// can not move the to tmp folder
		return false;
	}
	return true;
}

std::wstring SafePrintHandler::get_no_clash_nxl_file_path(const std::wstring& folder, const std::wstring& file_name)
{
	// for most situations, this is good enought
	std::wstring probe = folder + file_name+L".nxl";

	if (!is_file_exist(probe.c_str())) {
		return probe;
	}
	// but we have to consider the clash situiation
	std::wstring ext = ::PathFindExtensionW(file_name.c_str());
	std::wstring name_without_ext = file_name; {
		while (name_without_ext.back()!='.')
		{
			name_without_ext.pop_back();
		}
		name_without_ext.pop_back();
	}
	
	int i = 1;
	
	while (i<100) {
		probe = folder;
		probe += name_without_ext + L"(" + std::to_wstring(i) + L")" + ext + L".nxl";
		if (!is_file_exist(probe.c_str())) {
			return probe;
		}

		i++;
	}
	// should never reach herer
	throw new std::runtime_error("should never reach here, some bad guy is doing the bad thing now");
	return std::wstring(); 
}