#include "stdafx.h"
#include "nxrmext2.h"
#include "rightsdef.h"
#include "SkyDrmSDKMgr.h"
#include "Log.h"
#include "CommonFunction.h"
#include "excelevents.h"

extern "C" const std::vector<RIBBON_ID_INFO> g_excel_16_ribbon_info;

extern HMODULE g_hModule;

namespace
{
	bool inline IsReadOnly(Excel2016::_Workbook * Wb) {
		bool rt = false;
		VARIANT_BOOL bReadOnly = VARIANT_FALSE;
		if (FAILED(Wb->get_ReadOnly(EXCEL_LCID_ENG, &bReadOnly))) {
			return rt;
		}
		return bReadOnly == VARIANT_TRUE;
	}

	inline HWND GetDocMainWindow(Excel2016::Window* Wn) {
		DISPID id = -1;
		CComBSTR name = L"hWnd";
		Wn->GetIDsOfNames(IID_NULL, &name, 1, LOCALE_SYSTEM_DEFAULT, &id);
		DISPPARAMS param{ NULL,NULL,NULL,NULL };
		CComVariant CVHwnd;
		CVHwnd.lVal;
		Wn->Invoke(id, IID_NULL, LOCALE_SYSTEM_DEFAULT, DISPATCH_PROPERTYGET, &param, &CVHwnd, NULL, NULL);

		return HWND(CVHwnd.lVal);
	}

	inline HWND GetTopLevelParent(HWND w) {
		HWND hWndParent = w;
		HWND hWndTmp;
		while ((hWndTmp = ::GetParent(hWndParent)) != NULL)
			hWndParent = hWndTmp;

		return hWndParent;
	}

	inline bool GetPathFrom(Excel2016::_Workbook* Wb, CComBSTR& path) {
		if (Wb == NULL) {
			return false;
		}
		path.Empty();
		if (SUCCEEDED(Wb->get_FullName(EXCEL_LCID_ENG, &path)) &&  path.Length() ) {
			return true;
		}
		else {
			return false;
		}

	}

	inline void ApplyAntiScreenCaptureFeature(Excel2016::Window* Wn, ULONGLONG RightsMask) {
		if (!nextlabs::utils::isAppStreamEnv()) {
			auto hwnd = GetDocMainWindow(Wn);
			bool bgranted = RightsMask & BUILTIN_RIGHT_CLIPBOARD;

			::SetWindowDisplayAffinity(hwnd, bgranted ? WDA_NONE : WDA_MONITOR);
		}
		else {
			// When target customers use Amazon AppStream, we don't call SetWindowDisplayAffinity(WDA_MONITOR).
			// This is because calling it causes the whole application window to go black.
			//
			// Of course, not calling the function means that screen-capture
			// can no longer be denied even when the user doesn't have the right.
			DEVLOG(L"***ApplyAntiScreenCaptureFeature!Skip calling SetWindowDisplayAffinity(WDA_MONITOR) in AppStream environment. ***\n");
		}
	}

	inline std::wstring GetDefaultAutoSaveFolder() {
		std::wstring outPath;
		wchar_t var_env[255] = { 0 };
		::GetEnvironmentVariableW(L"APPDATA", var_env, 255);
		outPath.assign(var_env);
		outPath += L"\\Microsoft\\Excel\\";
		return outPath;
	}
}

void look_into_CustomProperties(Excel2016::_Worksheet* sh) {
	HRESULT hr = S_OK;
	Excel2016::CustomProperties* CustomProperties = NULL;
	long count = 0;
	Excel2016::CustomProperty* CustomProperty = NULL;

	hr = sh->get_CustomProperties(&CustomProperties);
	if (FAILED(hr))
	{
		return;
	}
	hr = CustomProperties->Get_Count(&count);
	if (FAILED(hr))
	{
		return;
	}
	for (long i = 1; i <= count; i++)
	{
		CComBSTR name;
		CComVariant value;
		hr = CustomProperties->Item(CComVariant(i), &CustomProperty);
		if (FAILED(hr))
		{
			continue;
		}
		hr = CustomProperty->Get_Name(&name);
		if (FAILED(hr))
		{
			continue;
		}
		hr = CustomProperty->Get_Value(&value);
		if (FAILED(hr))
		{
			continue;
		}
	}
}

STDMETHODIMP ExcelEventListener::SheetActivate(Excel2016::_Worksheet* sh) {
	DEVLOG_FUN;
	::OutputDebugStringW(L"SheetActivate");
	HRESULT hr = S_OK;
	Excel2016::CustomProperties* customProperties = NULL;
	Excel2016::CustomProperty* customPropertie = NULL;
	long count_customProperties = 0;
	std::wstring nxlFilePath = L"";
	bool isNxlFile = false;
	bool isSancFile = false;

	// in some excel file, it will throw access violation writing location exception  for  Bug 70902
	_try
	{
		hr = sh->get_CustomProperties(&customProperties);
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return S_OK;
	}

	if (FAILED(hr))
	{
		return S_OK;
	}
	hr = customProperties->Get_Count(&count_customProperties);
	if (FAILED(hr))
	{
		return S_OK;
	}
	
	for (long i = 1; i <= count_customProperties; i++)
	{
		CComBSTR name(L"");
		CComVariant value(L"");
		hr = customProperties->Item(CComVariant(i), &customPropertie);
		if (FAILED(hr))
		{
			continue;
		}
		hr = customPropertie->Get_Name(&name);
		if (FAILED(hr))
		{
			continue;
		}

		std::wstring w_name(name.Detach());
		if (0 == _wcsicmp(w_name.c_str(), L"nxl_file_path")) {
			hr = customPropertie->Get_Value(&value);
			if (FAILED(hr))
			{
				continue;
			}
			nxlFilePath = std::wstring(value.bstrVal);
			break;
		}
	}

	Excel2016::Workbooks* workbooks;
	long workbooks_count = 0;
	if (!nxlFilePath.empty()) {
		// is active workboos's own worksheet
		Excel2016::_Workbook * active_workbook = NULL;
		CComBSTR active_workbook_path;
		std::wstring wstr_active_workbook_path = L"";
		hr = m_pApplication->get_ActiveWorkbook(&active_workbook);
		if (FAILED(hr))
		{
			return S_OK;
		}
		hr = active_workbook->get_FullName(EXCEL_LCID_ENG, &active_workbook_path);
		if (FAILED(hr))
		{
			return S_OK;
		}
		wstr_active_workbook_path = active_workbook_path.Detach();
		if (0 == _wcsicmp(wstr_active_workbook_path.c_str(), nxlFilePath.c_str())) {
			return S_OK;
		}

		// isn't active workboos's own worksheet , to check up this worksheet is relative nxl file 
		ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
		SkyDrmSDKMgr::Instance()->CheckRights(nxlFilePath.c_str(), RightsMask);
		if (!(RightsMask & BUILTIN_RIGHT_CLIPBOARD)) {
			// this active worksheet relative a nxl file and isn't current active workbook own 
			// and this active worksheet related nxl has not right to move out this active worksheet
			::OutputDebugStringW(nxlFilePath.c_str());
			hr = m_pApplication->put_DisplayAlerts(EXCEL_LCID_ENG, VARIANT_FALSE);
			if (FAILED(hr))
			{
				sh->Delete();
			}
			hr = m_pApplication->get_Workbooks(&workbooks);
			if (FAILED(hr))
			{
				sh->Delete();
			}
			hr = workbooks->get_Count(&workbooks_count);
			if (FAILED(hr))
			{
				sh->Delete();
			}

			Excel2016::_Workbook* workbook = NULL;
			for (long i = 1; i <= workbooks_count; i++)
			{
				Excel2016::_Workbook* t_workbook = NULL;
				CComBSTR path;
				hr = workbooks->get_Item(CComVariant(i), &t_workbook);
				if (FAILED(hr))
				{
					continue;
				}
			    hr = t_workbook->get_FullName(EXCEL_LCID_ENG, &path);
				if (FAILED(hr))
				{
					continue;
				}
				std::wstring w_path(path.Detach());
				if (0 == _wcsicmp(w_path.c_str(), nxlFilePath.c_str())) 
				{
					workbook = t_workbook;
					break;
				}
			}

			if (workbook != NULL) {
				Excel2016::Sheets* sheets = NULL;
				long sheets_count = 0;
				hr = workbook->get_Worksheets(&sheets);
				if (FAILED(hr))
				{
					sh->Delete();
				}
				hr = sheets->get_Count(&sheets_count);
				if (FAILED(hr))
				{
					sh->Delete();
				}
				if (sheets_count>0) {
					IDispatch * workSheet = NULL;
					hr = sheets->get_Item(CComVariant(1), &workSheet);
					if (FAILED(hr))
					{
						sh->Delete();
					}
					//hr = sh->Copy(vtMissing, CComVariant(workSheet), EXCEL_LCID_ENG);
					hr = sh->Move(vtMissing, CComVariant(workSheet), EXCEL_LCID_ENG);
				}
				else {
					sh->Delete();
				}
			}
			else {
				// may be the workbook has closed or hided
				sh->Delete();
			}

			hr = m_pApplication->put_DisplayAlerts(EXCEL_LCID_ENG, VARIANT_TRUE);
			const wchar_t* wszNotify = L"We have blocked Drag-Drop functionality for security reasons.";
			//get parent window
			HWND hWnd = GetForegroundWindow();
			//notify 
			MessageBoxW(hWnd, wszNotify, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
		}
		else {
			// this active worksheet relative nxl file have right allow move out this avtive worksheet
			// update this active worksheet custome value to reflect to current active workbook
			if (CommonFunction::IsNXLFile(wstr_active_workbook_path.c_str())) {
				//update
					//hr = customProperties->Add(CComBSTR("nxl_file_path"), CComVariant(wstr_active_workbook_path.c_str()), &need_update_customPropertie);
				Excel2016::CustomProperty* need_update_customPropertie = NULL;
				bool found_out = false;
				for (long i = 1; i <= count_customProperties; i++) {
					CComBSTR need_update_name(L"");
					hr = customProperties->Item(CComVariant(i), &need_update_customPropertie);
					if (FAILED(hr))
					{
						continue;
					}

					hr = customPropertie->Get_Name(&need_update_name);
					if (FAILED(hr))
					{
						continue;
					}

					std::wstring w_need_update_name(need_update_name.Detach());
					if (0 == _wcsicmp(w_need_update_name.c_str(), L"nxl_file_path")) {
						found_out = true;
						break;
					}
				}

				if (found_out) {
					hr = need_update_customPropertie->Put_Value(CComVariant(wstr_active_workbook_path.c_str()));
				}

			}
			else {
				//remove
				bool found_out = false;
				Excel2016::CustomProperty* need_remove_customPropertie = NULL;
				for (long i = 1; i <= count_customProperties; i++) {
					CComBSTR need_remove_name(L"");
					hr = customProperties->Item(CComVariant(i), &need_remove_customPropertie);
					if (FAILED(hr))
					{
						continue;
					}

					hr = customPropertie->Get_Name(&need_remove_name);
					if (FAILED(hr))
					{
						continue;
					}

					std::wstring w_need_remove_name(need_remove_name.Detach());
					if (0 == _wcsicmp(w_need_remove_name.c_str(), L"nxl_file_path")) {
						found_out = true;
						break;
					}
				}

				if(found_out){
					hr = need_remove_customPropertie->Delete();
				}
			}
			//look_into_CustomProperties(sh);
		}
	}
	else {
	    // if customProperties cannot find value of nxl_file_path. means this sheet is moved from other workbook or new created by current workbook
		Excel2016::_Workbook * active_workbook = NULL;
		CComBSTR active_workbook_path;
		std::wstring wstr_active_workbook_path = L"";

		if (m_pApplication == NULL) {
			return S_OK;
		}

		__try
		{
			hr = m_pApplication->get_ActiveWorkbook(&active_workbook);
			if (FAILED(hr))
			{
				return S_OK;
			}

			hr = active_workbook->get_FullName(EXCEL_LCID_ENG, &active_workbook_path);
			if (FAILED(hr))
			{
				return S_OK;
			}
		}
		__except (EXCEPTION_EXECUTE_HANDLER) {
			return S_OK;
		}


		wstr_active_workbook_path = active_workbook_path.Detach();
		if (CommonFunction::IsNXLFile(wstr_active_workbook_path.c_str())) {
			hr = customProperties->Add(CComBSTR("nxl_file_path"), CComVariant(wstr_active_workbook_path.c_str()), &customPropertie);
		}
	}


	return S_OK;
}


STDMETHODIMP ExcelEventListener::SheetBeforeDelete(Excel2016::_Worksheet* Sh) {
	DEVLOG_FUN;
	::OutputDebugStringW(L"SheetBeforeDelete");
	//BSTR sheetName;
	//long index=0;
	//Sh->get_Name(&sheetName);
	//Sh->get_Index(EXCEL_LCID_ENG, &index);
	return S_OK;
}

STDMETHODIMP ExcelEventListener::SheetDeactivate(Excel2016::_Worksheet* Sh) {
	DEVLOG_FUN;
	::OutputDebugStringW(L"SheetDeactivate");
	//BSTR sheetName;
	//long index = 0;
	//Sh->get_Name(&sheetName);
	//Sh->get_Index(EXCEL_LCID_ENG, &index);
	return S_OK;
}

STDMETHODIMP ExcelEventListener::WorkbookNewSheet(Excel2016::_Workbook* Wb, Excel2016::_Worksheet* Sh) {
	DEVLOG_FUN;
	::OutputDebugStringW(L"WorkbookNewSheet");
	return S_OK;
}

STDMETHODIMP ExcelEventListener::WorkbookOpen(Excel2016::_Workbook* Wb)
{
	DEVLOG_FUN;
	::OutputDebugStringW(L"WorkbookOpen");
	CComBSTR path;
	HRESULT hr = Wb->get_FullName(EXCEL_LCID_ENG, &path);
	if (FAILED(hr))
	{
		return hr;
	}

	theLog.WriteLog(0, NULL, 0, L"WorkbookOpen, Excel, path:%s\r\n", path);

	if (CommonFunction::IsNXLFile(path)) {
		long worksheets_count = 0;
		Excel2016::Sheets* sheets = NULL;
		IDispatch* sheet = NULL;
		Excel2016::CustomProperties* customProperties = NULL;
	

		hr = Wb->get_Worksheets(&sheets);
		sheets->get_Count(&worksheets_count);

		for (long i = 1; i <= worksheets_count; i++) {
			customProperties = NULL;
			hr = sheets->get_Item(CComVariant(i), &sheet);
			if (FAILED(hr))
			{
				continue;
			}
			hr = ((Excel2016::_Worksheet*)sheet)->get_CustomProperties(&customProperties);
			if (FAILED(hr))
			{
				continue;
			}
			//hr = customProperties->Add(CComBSTR("nxl_file_path"), CComVariant(std::wstring(path).c_str()), &customPropertie);

			long count_customProperties = 0;
			hr = customProperties->Get_Count(&count_customProperties);
			if (FAILED(hr))
			{
				continue;
			}

			Excel2016::CustomProperty* need_update_customPropertie = NULL;
			bool found_out = false;
			for (long i = 1; i <= count_customProperties; i++)
			{
				CComBSTR need_update_name(L"");
				hr = customProperties->Item(CComVariant(i), &need_update_customPropertie);
				if (FAILED(hr))
				{
					continue;
				}

				hr = need_update_customPropertie->Get_Name(&need_update_name);
				if (FAILED(hr))
				{
					continue;
				}

				std::wstring w_need_update_name(need_update_name.Detach());
				if (0 == _wcsicmp(w_need_update_name.c_str(), L"nxl_file_path")) {
					found_out = true;
					break;
				}
			}
			if (found_out) {
				hr = need_update_customPropertie->Put_Value(CComVariant(std::wstring(path).c_str()));
			}
			else {
				Excel2016::CustomProperty* add_customPropertie = NULL;
				hr = customProperties->Add(CComBSTR("nxl_file_path"), CComVariant(std::wstring(path).c_str()), &add_customPropertie);
			}
		}

		SkyDrmSDKMgr::Instance()->AddViewLog(std::wstring(path));
		CommonFunction::openedNxlFilePath[std::wstring(path)] = true;
		Impl_Feature_AntiAutoRecovery();

		ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
		CheckWorkbookRight(Wb, RightsMask);
		m_ActiveDoc = path.m_str;
		//update right
		BOOL UpdateRibbonUI = FALSE;
		if (m_ActiveDocRights != RightsMask)
		{
			UpdateRibbonUI = TRUE;
			m_ActiveDocRights = RightsMask;
		}
		if (UpdateRibbonUI || m_InvalidCount == 0)
		{
			InvalidMsoControls();
		}
	}
	return S_OK;
}

STDMETHODIMP ExcelEventListener::WindowActivate(Excel2016::_Workbook * Wb,Excel2016::Window * Wn)
{
	::OutputDebugStringW(L"WindowActivate");
	DEVLOG_FUN;
	HRESULT hr = S_OK;
	//get rights
	ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
	CheckWorkbookRight(Wb, RightsMask);

	//update active workbook name
	CComBSTR wbFullName;
	Wb->get_FullName(EXCEL_LCID_ENG, &wbFullName);
	if (wbFullName.m_str)
	{
		m_ActiveDoc = wbFullName.m_str;
	}
	else
	{
		m_ActiveDoc.clear();
	}

	//update right
	BOOL UpdateRibbonUI = FALSE;
	if (m_ActiveDocRights != RightsMask)
	{
		UpdateRibbonUI = TRUE;
		m_ActiveDocRights = RightsMask;
	}

	if (UpdateRibbonUI || m_InvalidCount == 0)
	{
		InvalidMsoControls();
	}
	   
	//update copy/past shortcut
	if (m_pApplication)
	{
		VARIANT_BOOL varHaveClipRight = (m_ActiveDocRights&BUILTIN_RIGHT_CLIPBOARD) ? VARIANT_TRUE : VARIANT_FALSE;

		CComBSTR bstrCtrlVKey1 = SysAllocString(L"^{v}");
		CComBSTR bstrCtrlVKey2 = SysAllocString(L"^{V}");
		CComBSTR bstrCtrlCKey1 = SysAllocString(L"^{c}");
		CComBSTR bstrCtrlCKey2 = SysAllocString(L"^{C}");
		CComBSTR bstrCtrlXKey1 = SysAllocString(L"^{x}");
		CComBSTR bstrCtrlXKey2 = SysAllocString(L"^{X}");
		if (varHaveClipRight == VARIANT_FALSE)
		{
			CComVariant varPre = L"";
			if (!(m_ActiveDocRights & BUILTIN_RIGHT_EDIT)) {
				//disable control+V
				m_pApplication->OnKey(bstrCtrlVKey1, varPre, EXCEL_LCID_ENG);
				m_pApplication->OnKey(bstrCtrlVKey2, varPre, EXCEL_LCID_ENG);
			}
			else {
				//return control+v to its its normal meaning
				m_pApplication->OnKey(bstrCtrlVKey1, vtMissing, EXCEL_LCID_ENG);
				m_pApplication->OnKey(bstrCtrlVKey2, vtMissing, EXCEL_LCID_ENG);
			}

			//disable control+c
			m_pApplication->OnKey(bstrCtrlCKey1, varPre, EXCEL_LCID_ENG);
			m_pApplication->OnKey(bstrCtrlCKey2, varPre, EXCEL_LCID_ENG);

			//disable control+x
			m_pApplication->OnKey(bstrCtrlXKey1, varPre, EXCEL_LCID_ENG);
			m_pApplication->OnKey(bstrCtrlXKey2, varPre, EXCEL_LCID_ENG);
		}
		else
		{
			//return control+v to its its normal meaning
			m_pApplication->OnKey(bstrCtrlVKey1, vtMissing, EXCEL_LCID_ENG);
			m_pApplication->OnKey(bstrCtrlVKey2, vtMissing, EXCEL_LCID_ENG);

			//return control+c
			m_pApplication->OnKey(bstrCtrlCKey1, vtMissing, EXCEL_LCID_ENG);
			m_pApplication->OnKey(bstrCtrlCKey2, vtMissing, EXCEL_LCID_ENG);

			//control+x
			m_pApplication->OnKey(bstrCtrlXKey1, vtMissing, EXCEL_LCID_ENG);
			m_pApplication->OnKey(bstrCtrlXKey2, vtMissing, EXCEL_LCID_ENG);
		}

	}
	// setup overlay
	SkyDrmSDKMgr::Instance()->SetupViewOverlay(wbFullName.m_str, GetDocMainWindow(Wn));

	ApplyAntiScreenCaptureFeature(Wn, RightsMask);

	if (CommonFunction::IsNXLFile(m_ActiveDoc.c_str())) {
		Impl_Feature_AntiAutoRecovery();
	}
	return hr;
}

STDMETHODIMP ExcelEventListener::ProtectedViewWindowActivate(Excel2016::ProtectedViewWindow * Pvw)
{
	DEVLOG_FUN;
	HRESULT hr = S_OK;

	do
	{
		Excel2016::IProtectedViewWindow *pProtectVw = (Excel2016::IProtectedViewWindow *)Pvw;

		Excel2016::_WorkbookPtr Wb = NULL;
		hr = pProtectVw->get_Workbook(&Wb);

		if (FAILED(hr) || Wb == NULL)
		{
			break;
		}

		ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
		CheckWorkbookRight(Wb, RightsMask);

		//update active doc
		CComBSTR WbFullName = NULL;
		Wb->get_FullName(EXCEL_LCID_ENG, &WbFullName);
		if (WbFullName.m_str)
		{
			m_ActiveDoc = WbFullName;
		}
		else
		{
			m_ActiveDoc.clear();
		}

		//
		BOOL UpdateRibbonUI = FALSE;
		if (m_ActiveDocRights != RightsMask)
		{
			UpdateRibbonUI = TRUE;
			m_ActiveDocRights = RightsMask;
		}

		if (UpdateRibbonUI || m_InvalidCount == 0)
		{
			InvalidMsoControls();
		}

	} while (FALSE);

	return hr;
}

STDMETHODIMP ExcelEventListener::WorkbookBeforePrint(Excel2016::_Workbook * Wb,VARIANT_BOOL * Cancel)
{
	HRESULT hr = S_OK;

	// Check the file if has Print rights, if not, deny the action. -- fix bug 60960
	ULONGLONG RightsMask = 0;
	hr = CheckWorkbookRight(Wb, RightsMask);
	if (!(RightsMask & BUILTIN_RIGHT_PRINT)) 
	{
		SkyDrmSDKMgr::Instance()->AddPrintLog(this->m_ActiveDoc, false);
		*Cancel = VARIANT_TRUE;

		// send notification
		SkyDrmSDKMgr::Instance()->Notify_PrintDenyMessage(this->m_ActiveDoc);
	}
	else
	{
		SkyDrmSDKMgr::Instance()->AddPrintLog(this->m_ActiveDoc);
	}

	return hr;
}

STDMETHODIMP ExcelEventListener::WorkbookBeforeClose(Excel2016::_Workbook* Wb, VARIANT_BOOL* Cancel)
{
	::OutputDebugStringW(L"WorkbookBeforeClose");
	DEVLOG_FUN;
	HRESULT hr = S_OK;
	CComBSTR wbName;
	if (!GetPathFrom(Wb, wbName)) {
		return hr;
	}

	if (!CommonFunction::IsNXLFile(wbName.m_str)) {
		return hr;
	}

	if (CommonFunction::IsPathLenghtMoreThan259(wbName.m_str)) {
		HWND hWnd = GetForegroundWindow();
		MessageBoxW(hWnd, ERROR_MSG_FILE_PATH_MORE_THAN_259, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
		return S_OK;
	}

	//check if the file is saved
	VARIANT_BOOL varSaved = VARIANT_TRUE;
	hr = Wb->get_Saved(EXCEL_LCID_ENG, &varSaved);
	if (SUCCEEDED(hr) && (varSaved != VARIANT_TRUE))
	{
		DWORD dwRes = DialogBoxParamW(g_hModule,
			MAKEINTRESOURCEW(IDD_EXCEL_SAVETIP),
			GetForegroundWindow(),
			(DLGPROC)DlgProcSaveTip, (LPARAM)this);

		if (dwRes == ID_SAVE)
		{
			HRESULT hr = Wb->Save(EXCEL_LCID_ENG);

			//if save failed, cancel the action
			hr = Wb->get_Saved(EXCEL_LCID_ENG, &varSaved);
			if (SUCCEEDED(hr) && (varSaved != VARIANT_TRUE))
			{
				*Cancel = VARIANT_TRUE;
			}
		}
		else if (dwRes == IDC_DONT_SAVE)
		{
			HRESULT hr = Wb->put_Saved(EXCEL_LCID_ENG, VARIANT_TRUE);
			if (FAILED(hr)) {
				*Cancel = VARIANT_TRUE;
			}
		}
		else
		{//cancel
			*Cancel = VARIANT_TRUE;
			return S_OK;
		}
	}

	if (*Cancel == VARIANT_FALSE)
	{
		//finally decided to close
		if (CommonFunction::openedNxlFilePath.count(wbName.m_str)) {
			CommonFunction::openedNxlFilePath.erase(wbName.m_str);
		}

		//need save ?
		bool changed = IEventBase::IsFileChanged(m_workbookChangeState, wbName);
		IEventBase::RemoveFileChanged(m_workbookChangeState, wbName);
		std::wstring wstrFileName = wbName.m_str;
		if (!IsReadOnly(Wb)) {
			std::vector<std::pair<std::wstring, std::wstring>> vecTagPair;
			std::wstring labelValue = L"";
			if (CommonFunction::ReadTag(Wb, vecTagPair)) {
				labelValue = CommonFunction::ParseTag(vecTagPair);
			}

			/*Registry{
				 HKEY = Computer\HKEY_CURRENT_USER\SOFTWARE\NextLabs\SkyDRM\MIP
				 Value: Decrypt_Label  (REG_SZ)
				 Value_Data:  decrypt label
				 }*/
			bool IsSensitiveLabelEnabled = false;
			std::uint32_t enablelabel;
			{
				nextlabs::utils::Registry r;
				nextlabs::utils::Registry::param param(LR"_(SOFTWARE\NextLabs\SkyDRM\MIP)_", HKEY_CURRENT_USER);
				if (r.get(param, L"Enable_Label", enablelabel) && enablelabel == 1) {
					IsSensitiveLabelEnabled = true;
				}
			}

			std::wstring decrptlabel;
			{
				nextlabs::utils::Registry r;
				nextlabs::utils::Registry::param param(LR"_(SOFTWARE\NextLabs\SkyDRM\MIP)_", HKEY_CURRENT_USER);
				if (!r.get(param, L"Decrypt_Label", decrptlabel)) {
					decrptlabel.clear();
				}
			}
			if (IsSensitiveLabelEnabled && nextlabs::utils::wstr_tolower(labelValue).compare(nextlabs::utils::wstr_tolower(decrptlabel)) == 0 && changed) {
				std::wstring file_name(wstrFileName);
				std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(file_name);
				if (nxlFilePath.empty())// in user rpm folder
				{
          TCHAR temp_path[265];
          GetTempPath(255, temp_path);
          std::wstring temp(temp_path);
          std::wstring temp_file = temp + L"nextlabs.temp";
          bool res1 = CopyFile(file_name.c_str(), temp_file.c_str(), false);
         // SkyDrmSDKMgr::Instance()->DelegateDeleteFile(file_name + L".nxl");
          bool res2 = SkyDrmSDKMgr::Instance()->DelegateCopyFile(temp_file.c_str(), file_name.c_str(), true);
				}
				else {
          std::wstring nxlTempPath = nxlFilePath.c_str();
          auto pos = nxlTempPath.rfind(L".nxl");
          if (pos != std::wstring::npos)
          {
            nxlTempPath.erase(pos);
          }

          DWORD dwAttrib = GetFileAttributesW(nxlTempPath.c_str());
          bool isExist = (INVALID_FILE_ATTRIBUTES != dwAttrib) && (0 == (dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
          if (isExist)
          {
            std::wstring msg = L"The destination folder already has a file named \"";
            msg.append(nxlTempPath);
            msg.append(L"\". ");
            msg.append(L"Are you sure you want to replace this file in the destination folder?");

            std::wstring caption = L"NextLabs Rights Management";

            //get parent window
           // HWND hWnd = GetForegroundWindow();
            //notify 
            int msgboxID = ::MessageBoxW(NULL, msg.c_str(), caption.c_str(), MB_OKCANCEL);

            if (msgboxID == 1) // user click  OK 
            {
              SkyDrmSDKMgr::Instance()->DelegateDeleteFile(nxlFilePath);
              auto pos = nxlFilePath.rfind(L".nxl");
              if (pos != std::wstring::npos)
              {
                nxlFilePath.erase(pos);
              }

              CopyFile(file_name.c_str(), nxlFilePath.c_str(), false);
              SkyDrmSDKMgr::Instance()->DelegateDeleteFile(file_name + L".nxl");
            }
            else
            {
              std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(wstrFileName);
              SkyDrmSDKMgr::Instance()->EditSaveFile(wstrFileName, L"", true, changed ? EXIT_MODE_EXIT_SAVE : EXIT_MODE_EXIT_NOTSAVE, L"");
              if (changed) CommonFunction::NotifiyRMDAppToSyncNxlFile(nxlFilePath);
            }
          }
          else
          {
            SkyDrmSDKMgr::Instance()->DelegateDeleteFile(nxlFilePath);
            auto pos = nxlFilePath.rfind(L".nxl");
            if (pos != std::wstring::npos)
            {
              nxlFilePath.erase(pos);
            }

            CopyFile(file_name.c_str(), nxlFilePath.c_str(), false);
            SkyDrmSDKMgr::Instance()->DelegateDeleteFile(file_name + L".nxl");
          }
				}
			}
			else {
				std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(wstrFileName);
        if (!IsSensitiveLabelEnabled)
        {
            labelValue = L"";
        }
				SkyDrmSDKMgr::Instance()->EditSaveFile(wstrFileName, L"", true, changed ? EXIT_MODE_EXIT_SAVE : EXIT_MODE_EXIT_NOTSAVE, labelValue);
				if (changed) CommonFunction::NotifiyRMDAppToSyncNxlFile(nxlFilePath);
			}
			
		}
	}
	return S_OK;
}

STDMETHODIMP ExcelEventListener::WorkbookBeforeSave(Excel2016::_Workbook * Wb,VARIANT_BOOL SaveAsUI,VARIANT_BOOL * Cancel)
{
	::OutputDebugStringW(L"WorkbookBeforeSave");
	HRESULT hr = S_OK;
	CComBSTR wbName;
	if (!GetPathFrom(Wb, wbName)) {
		return hr;
	}

	if (!CommonFunction::IsNXLFile(wbName.m_str)) {
		return hr;
	}

	if (CommonFunction::IsPathLenghtMoreThan259(wbName.m_str)) {
		*Cancel = VARIANT_TRUE;
		HWND hWnd = GetForegroundWindow();
		MessageBoxW(hWnd, ERROR_MSG_FILE_PATH_MORE_THAN_259, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
		return S_OK;
	}

	ULONGLONG RightsMask = 0;
	hr = CheckWorkbookRight(Wb, RightsMask);

	//
	// don't have edit right and it's SaveAs
	//
	if (!(RightsMask & BUILTIN_RIGHT_EDIT) && (SaveAsUI == VARIANT_FALSE))
	{
		*Cancel = VARIANT_TRUE;
	}
	else if (!(RightsMask & BUILTIN_RIGHT_SAVEAS) && (SaveAsUI == VARIANT_TRUE))
	{
		*Cancel = VARIANT_TRUE;
	}

	//notify user
	if (*Cancel == VARIANT_TRUE)
	{
		const wchar_t* wszNotify = ERROR_MSG_NO_SAVE_RIGHT;
		if (SaveAsUI == VARIANT_TRUE) {
			wszNotify = ERROR_MSG_NO_SAVE_AS_RIGHT;
		}

		//get parent window
		HWND hWnd = GetForegroundWindow();

		//notify 
		MessageBoxW(hWnd, wszNotify, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
	}
	else
	{
		CComBSTR DocFullName = NULL;
		Wb->get_FullName(EXCEL_LCID_ENG, &DocFullName);
		if (DocFullName.m_str)
		{
			IEventBase::SetFileChanged(m_workbookChangeState, DocFullName);
		}
	}

	return hr;
}

STDMETHODIMP ExcelEventListener::WorkbookAfterSave(Excel2016::_Workbook* Wb, VARIANT_BOOL Success) {
	::OutputDebugStringW(L"WorkbookAfterSave");
	HRESULT hr = S_OK;

	//CComBSTR wbName;
	//if (!GetPathFrom(Wb, wbName)) {
	//	return S_OK;
	//}
	//if (!CommonFunction::IsNXLFile(wbName.m_str)) {
	//	return S_OK;
	//}
	//
	//bool changed = IEventBase::IsFileChanged(m_workbookChangeState, wbName);
	//std::wstring wstrFileName = wbName.m_str;
	//if (!IsReadOnly(Wb)) {
	//	std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(wstrFileName);
	//	//SkyDrmSDKMgr::Instance()->EditSaveFile(wstrFileName, L"", true, changed ? EXIT_MODE_EXIT_SAVE : EXIT_MODE_EXIT_NOTSAVE);
	//	SkyDrmSDKMgr::Instance()->EditSaveFile(wstrFileName, L"", false, changed ? EXIT_MODE_NOTEXIT_SAVE : EXIT_MODE_NOTEXIT_NOTSAVE);
	//	if (changed) CommonFunction::NotifiyRMDAppToSyncNxlFile(nxlFilePath);
	//}

	return hr;
}

void ExcelEventListener::InvalidMsoControls(void)
{
	::OutputDebugStringW(L"InvalidMsoControls");
	HRESULT hr = S_OK;

	do
	{
		if (!m_pRibbonUI)
		{
			break;
		}

		Office2016::IRibbonUI *pRibbonUI = NULL;

		hr = m_pRibbonUI->QueryInterface(__uuidof(Office2016::IRibbonUI), (void**)&pRibbonUI);

		if (!SUCCEEDED(hr))
		{
			break;
		}

		for (ULONG i = 0; i < (ULONG)(g_excel_16_ribbon_info.size()); i++)
		{
			pRibbonUI->InvalidateControlMso((BSTR)g_excel_16_ribbon_info[i].RibbonId);
		}

		if (pRibbonUI)
		{
			pRibbonUI->Release();
			pRibbonUI = NULL;
		}

		m_InvalidCount++;

	} while (FALSE);

	return;
}

BOOL CALLBACK ExcelEventListener::DlgProcSaveTip(HWND hwndDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
	static HICON hICONWarn = NULL;

	switch (message)
	{
	case WM_INITDIALOG:
	{
		ExcelEventListener* pExcelListener = (ExcelEventListener*)lParam;

		//load and show icon
		hICONWarn = ::LoadIconW(NULL, MAKEINTRESOURCEW(IDI_WARNING));
		DWORD dwLastError = GetLastError();
		if (hICONWarn)
		{
			SendDlgItemMessage(hwndDlg, IDC_PICTURE, STM_SETIMAGE, (WPARAM)IMAGE_ICON, (LPARAM)hICONWarn);
		}

		//set path info
		std::wstring wstrFileName = CommonFunction::GetFileName(pExcelListener->m_ActiveDoc);
		wstrFileName.insert(0, 1, L'\'');
		wstrFileName.append(L"'?");
		SetDlgItemTextW(hwndDlg, IDC_FILE_NAME, wstrFileName.c_str());
	}
	return TRUE;
	break;

	case WM_COMMAND:
		switch (LOWORD(wParam))
		{
		case ID_SAVE:
		case IDC_DONT_SAVE:
		case IDCANCEL:
		case IDCLOSE:
			EndDialog(hwndDlg, wParam);
			break;
		}
		return TRUE;

	case WM_DESTROY:
		if (hICONWarn)
		{
			DestroyIcon(hICONWarn);
			hICONWarn = NULL;
		}
		break;
	}
	return FALSE;
}

HRESULT ExcelEventListener::CheckWorkbookRight(Excel2016::_WorkbookPtr Wb, ULONGLONG& RightMask)
{
	HRESULT hr = S_OK;
	RightMask = BUILTIN_RIGHT_ALL;

	CComBSTR wbFullName;
	hr = Wb->get_FullName(EXCEL_LCID_ENG, &wbFullName);
	if (SUCCEEDED(hr) && (wbFullName.m_str != NULL))
	{
		SkyDrmSDKMgr::Instance()->CheckRights(wbFullName.m_str, RightMask);

		//remove edit right if the file is readonly
		if (CommonFunction::IsNXLFile(wbFullName.m_str))
		{
			VARIANT_BOOL varReadOnly = VARIANT_FALSE;
			hr = Wb->get_ReadOnly(EXCEL_LCID_ENG, &varReadOnly);

			if ((RightMask&BUILTIN_RIGHT_EDIT) &&
				SUCCEEDED(hr) &&
				(varReadOnly == VARIANT_TRUE))
			{
				theLog.WriteLog(0, NULL, 0, L"CheckWorkbookRight the file is readonly remove edit right:%s\r\n", wbFullName.m_str);
				RightMask &= ~BUILTIN_RIGHT_EDIT;
			}
		}
	}
	return hr;
}


std::vector<HWND> fondHwnd;
BOOL CALLBACK  Win_Enum(HWND found, LPARAM ) {
	fondHwnd.push_back(found);
	return true;
}

void ExcelEventListener::ShowViewOverlayAuto_SpecialCase_ForBugFix(BSTR path) {

	//Excel2016::Window* Wnd=NULL;
	//if (FAILED(m_pApplication->get_ActiveWindow(&Wnd))){
	//	return;
	//}
	//if (Wnd == NULL) {
	//	return;
	//}
	if (path == NULL) {
		return;
	}
	
	theLog.WriteLog(0, NULL, 0, L"wordaround for the special-case, file opened before plguin-inited,trying to show watermark, %s", path);
	fondHwnd.clear();
	::EnumThreadWindows(::GetCurrentThreadId(),
		Win_Enum,
		NULL);

	for (auto h : fondHwnd) {
		// setup overlay
		wchar_t clsName[0x20] = { 0 };
		int acturalLen=::GetClassNameW(h, clsName, 0x20);
		if (acturalLen == 0) {
			continue;
		}
		if (0 == std::wcsncmp(clsName, L"XLMAIN", 6)) {
			SkyDrmSDKMgr::Instance()->SetupViewOverlay(path, h);
      /// Fixed bug 70622
      ULONGLONG RightMask;
      SkyDrmSDKMgr::Instance()->CheckRights(path, RightMask);
      bool bgranted = RightMask & BUILTIN_RIGHT_CLIPBOARD;
      ::OutputDebugStringW(L"ShowViewOverlayAuto_SpecialCase_ForBugFix");
      ::SetWindowDisplayAffinity(h, bgranted ? WDA_NONE : WDA_MONITOR);
      // only set one is enough 
      break;
		}
	}


	

}


void ExcelEventListener::Impl_Feature_AntiAutoRecovery()
{
	if (CommonFunction::IsNXLFile(m_ActiveDoc.c_str())) {
		//DisableAutoRecoverFeature();

		std::wstring dir_default = GetDefaultAutoSaveFolder();
		if (g_nxrmExt2) {
			g_nxrmExt2->Register_AASR_Folder(dir_default);
		}

		// 09/23/2020 by osmond, I provide fv to handle this gracefully
		std::wstring dir;
		if (!GetAutoRecoveryDir(dir)) {
			return;
		}
		if (g_nxrmExt2) {
			g_nxrmExt2->Register_AASR_Folder(dir);
		}
	}
}

bool ExcelEventListener::GetAutoRecoveryDir(std::wstring& outPath) {

	HRESULT hr = S_OK;
	using namespace Excel2016;

	Excel2016::IAutoRecoverPtr spIAR;
	hr = this->m_pApplication->get_AutoRecover((AutoRecover**)&spIAR);
	if (FAILED(hr)) {
		return false;
	}
	CComBSTR arPath;
	hr = spIAR->get_Path(&arPath);
	if (FAILED(hr)) {
		return false;
	}
	outPath.assign(arPath);
	return true;
}

//void ExcelEventListener::SetupMonitorAutoRecoveryDir()
//{
//	// set autoclearAutorecoveryPath
//	std::wstring arPath;
//	if (GetAutoRecoveryDir(arPath) && m_AutoRecoveryDirMonitor.AddDir(arPath, [](const std::wstring& path) {
//		// clear all file under path
//		theLog.WriteLog(0, NULL, 0, L"excel callback of delete files in folder: %s", path.c_str());
//		CommonFunction::TryingDeleteRegKeyInResiliency();
//		CommonFunction::ClearFolderContents(path);
//	})) {
//		m_AutoRecoveryDirMonitor.StartWork();
//	}
//}
//
void ExcelEventListener::DisableAutoRecoverFeature() {

	Excel2016::IAutoRecoverPtr par = NULL;
	HRESULT hr = m_pApplication->get_AutoRecover((Excel2016::AutoRecover**) & par);
	if (SUCCEEDED(hr) && (par != NULL)) {
		hr = par->put_Enabled(VARIANT_FALSE);
	}
}
