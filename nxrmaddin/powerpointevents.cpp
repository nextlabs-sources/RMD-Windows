#include "stdafx.h"
#include "nxrmext2.h"
#include "rightsdef.h"
#include "SkyDrmSDKMgr.h"
#include "CommonFunction.h"
#include "Log.h"

#include "powerpointevents.h"

namespace {
	inline HWND GetTopLevelParent(HWND w) {
		HWND hWndParent = w;
		HWND hWndTmp;
		while ((hWndTmp = ::GetParent(hWndParent)) != NULL)
			hWndParent = hWndTmp;

		return hWndParent;
	}

	inline HWND GetDocMainWindow(struct PowerPoint2016::DocumentWindow* Wn) {
		long hwnd;
		Wn->get_HWND(&hwnd);
		return (HWND)hwnd;
	}

	inline void ApplyAntiScreenCaptureFeature(struct PowerPoint2016::DocumentWindow* Wn, ULONGLONG RightsMask) {
		if (!nextlabs::utils::isAppStreamEnv()) {
			auto hwnd = GetTopLevelParent(GetDocMainWindow(Wn));
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
	inline void ApplyAntiScreenCaptureFeature(HWND h, ULONGLONG RightsMask) {
		if (!nextlabs::utils::isAppStreamEnv()) {
			bool bgranted = RightsMask & BUILTIN_RIGHT_CLIPBOARD;
			::SetWindowDisplayAffinity(h, bgranted ? WDA_NONE : WDA_MONITOR);
		}
		else {
			DEVLOG(L"***ApplyAntiScreenCaptureFeature!Skip calling SetWindowDisplayAffinity(WDA_MONITOR) in AppStream environment. ***\n");
		}
	}

	inline std::wstring GetDefaultAutoSaveFolder() {
		std::wstring outPath;
		wchar_t var_env[255] = { 0 };
		::GetEnvironmentVariableW(L"APPDATA", var_env, 255);
		outPath.assign(var_env);
		outPath += L"\\Microsoft\\PowerPoint\\";
		return outPath;
	}

}

extern "C" const std::vector<RIBBON_ID_INFO> g_powerpoint_16_ribbon_info;
extern HMODULE g_hModule;

HRESULT STDMETHODCALLTYPE PowerPointEventListener::PresentationClose(
/*[in]*/ struct PowerPoint2016::_Presentation * Pres)
{
	HRESULT hr = S_OK;
	CComBSTR path = NULL;
	hr = Pres->get_FullName(&path);
	if (FAILED(hr))
	{
		return hr;
	}
	theLog.WriteLog(0, NULL, 0, L"PresentationClose PPT,path:%s\r\n", path);

	//if (CommonFunction::IsNXLFile(path)) {
	//	m_NxlRightsContainer.remove(std::wstring(path));
	//}

	//// work around fix bug: 58112 
	//{
	//	ATL::CComPtr<struct PowerPoint2016::DocumentWindows> spWnds = NULL;
	//	if (SUCCEEDED(Pres->get_Windows(&spWnds)) && spWnds!=NULL ) {
	//		long count = 0;
	//		if (SUCCEEDED(spWnds->get_Count(&count))) {
	//			for (int i = 1; i <= count; ++i) {
	//				ATL::CComPtr<struct PowerPoint2016::DocumentWindow> spWnd = NULL;
	//				if (FAILED(spWnds->Item(i, &spWnd)))  continue;
	//				HWND hWnd = NULL;
	//				if (FAILED(spWnd->get_HWND((long*)&hWnd))) continue;

	//				SkyDrmSDKMgr::Instance()->ClearViewOverlay(path.m_str, GetTopLevelParent(hWnd));

	//			}
	//		}
	//	}
	//}

	return hr;
}

HRESULT 
STDMETHODCALLTYPE PowerPointEventListener::PresentationOpen( struct PowerPoint2016::_Presentation * Pres)
{
	::OutputDebugStringW(L"PresentationOpen");
	HRESULT hr = S_OK;
	CComBSTR path = NULL;
	hr = Pres->get_FullName(&path);
	if (FAILED(hr))
	{
		return hr;
	}
	else {
		m_ActiveDoc = path;
	}

	ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
	CheckPPTRight(Pres, RightsMask);
	m_ActiveDocRights = RightsMask;

	theLog.WriteLog(0, NULL, 0, L"PresentationOpen PPT,path:%s\r\n", path);
	
	if (CommonFunction::IsNXLFile(path)) {

		SkyDrmSDKMgr::Instance()->AddViewLog(std::wstring(path));
		
		// add this path
		//CommonFunction::nxlFilePath.push_back(std::wstring(path));
		CommonFunction::openedNxlFilePath[std::wstring(path)] = true;

		// nxl opened SetUp clear AutoRecovery files	
		//SetupMonitorAutoRecoveryDir();
		//DisableAutoRecoverFeature();
		Impl_Feature_AntiAutoRecovery();
	}

	return S_OK;
}

HRESULT STDMETHODCALLTYPE PowerPointEventListener::WindowActivate (
/*[in]*/ struct PowerPoint2016::_Presentation * Pres,
/*[in]*/ struct PowerPoint2016::DocumentWindow * Wn )
{
	HRESULT hr = S_OK;

	//get right
	ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
	CheckPPTRight(Pres, RightsMask);
	//RightsMask = GetMinialRights(RightsMask);
	//update active document
	CComBSTR PptFullName = NULL;
	Pres->get_FullName(&PptFullName);
	if (PptFullName.m_str)
	{
		m_ActiveDoc = PptFullName;
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
	
	SkyDrmSDKMgr::Instance()->SetupViewOverlay(PptFullName.m_str, GetTopLevelParent(GetDocMainWindow(Wn)));

	ApplyAntiScreenCaptureFeature(Wn, RightsMask);
	
	return hr;
}


//std::set<HWND> g_wnd_minimized_for_splash;
//
//inline void MiniMiumAllDocWnds(struct PowerPoint2016::SlideShowWindow* Wn) {
//	ATL::CComPtr<struct PowerPoint2016::_Application> spApp = NULL;
//	ATL::CComPtr<struct PowerPoint2016::DocumentWindows> spWnds = NULL;
//	long count = 0;
//	if(FAILED( Wn->get_Application(&spApp))) 
//		return;
//	if (FAILED( spApp->get_Windows(&spWnds)))
//		return;
//	if (FAILED(spWnds->get_Count(&count)))
//		return;
//
//	for (int i = 1; i <= count; ++i) {
//		ATL::CComPtr<struct PowerPoint2016::DocumentWindow> spWnd = NULL;
//		if (FAILED(spWnds->Item(i, &spWnd)))
//			return;
//		HWND hWnd = NULL;
//		if (FAILED(spWnd->get_HWND((long*)&hWnd)))
//			return;
//		hWnd = GetTopLevelParent(hWnd);
//		std::wstring clsName;
//		clsName.resize(0x20);
//		clsName.resize(::GetClassNameW(hWnd, (wchar_t*)clsName.c_str(), 0x20));
//		std::transform(clsName.begin(), clsName.end(), clsName.begin(), ::tolower);
//		if (clsName.find(L"screenclass") == clsName.npos) {
//			::ShowWindow(hWnd, SW_MINIMIZE);
//			g_wnd_minimized_for_splash.insert(hWnd);
//		}
//
//	}
//}
//inline void RestoreAllDocWnd() {
//	for (auto h : g_wnd_minimized_for_splash) {
//		::ShowWindow(h, SW_RESTORE);
//	}
//	g_wnd_minimized_for_splash.clear();
//}

HRESULT STDMETHODCALLTYPE PowerPointEventListener::SlideShowBegin(
/*[in]*/ struct PowerPoint2016::SlideShowWindow * Wn)
{
	HRESULT hr = S_OK;
	//OutputDebugStringA(__FUNCTION__);
	std::wstring path;
	this->GetActiveDoc(path);
	HWND hwnd = NULL;
	if (FAILED(Wn->get_HWND((long*)&hwnd))) {
		return S_OK;  // not thing to do 
	}
	if (hwnd == NULL) {
		hwnd = ::GetForegroundWindow();
	}
	else {
		/* 
		   Work-Around:
				SkyDRM Viewer will have a strange behavior. View open a nxl ppt file, will come here, 
				but the hwnd is not belong to this process
		*/
		DWORD pID=-1;
		
		::GetWindowThreadProcessId(hwnd, &pID);
		DWORD pCurID = ::GetCurrentProcessId();
		if (pID != pCurID) {
			// not equal, this hwnd is created by other process ignore
			return S_OK;
		}

	}
	// screen splash do not need any offset
	SkyDrmSDKMgr::Instance()->SetupViewOverlay(path, GetTopLevelParent(hwnd), {0,0,0,0});
	ApplyAntiScreenCaptureFeature(GetTopLevelParent(hwnd), m_ActiveDocRights);

	//
	// additional,
	//
	// since we have support doc-based watermark, do not need it 
	//
	//MiniMiumAllDocWnds(Wn);

	return hr;
}

HRESULT STDMETHODCALLTYPE PowerPointEventListener::PresentationBeforeSave(
/*[in]*/ struct PowerPoint2016::_Presentation * Pres,
	/*[in,out]*/ VARIANT_BOOL * Cancel)
{
	HRESULT hr = S_OK;

	do
	{
		CComBSTR pptFullName;
		hr = Pres->get_FullName(&pptFullName);
		if (FAILED(hr)) {
			return S_OK;
		}
		if (CommonFunction::IsNXLFile(pptFullName)) {
			if (CommonFunction::IsPathLenghtMoreThan259(pptFullName.m_str)) {
				*Cancel = VARIANT_TRUE;
				HWND hWnd = GetForegroundWindow();
				MessageBoxW(hWnd, ERROR_MSG_FILE_PATH_MORE_THAN_259, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
				return S_OK;
			}
		}

		ULONGLONG RightsMask = 0;
		CheckPPTRight(Pres, RightsMask);

		// don't have extract right and it's SaveAs
		if (!(RightsMask & BUILTIN_RIGHT_SAVEAS) && !(RightsMask & BUILTIN_RIGHT_EDIT))
		{
			*Cancel = VARIANT_TRUE;
		}

		
		//notify user
		if (*Cancel == VARIANT_TRUE)
		{
			const wchar_t* wszNotify = ERROR_MSG_NO_SAVE_RIGHT;

			//get parent window
			HWND hWnd = GetForegroundWindow();

			//notify 
			MessageBoxW(hWnd, wszNotify, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
		}
		else
		{
			CComBSTR DocFullName;
			Pres->get_FullName(&DocFullName);
			if (DocFullName.m_str)
			{
				IEventBase::SetFileChanged(m_PresentationChanged, DocFullName);
			}
		}

	} while (FALSE);

	return hr;
}

HRESULT STDMETHODCALLTYPE PowerPointEventListener::PresentationBeforeClose(
/*[in]*/ struct PowerPoint2016::_Presentation * Pres,
	/*[in,out]*/ VARIANT_BOOL * Cancel)
{
	::OutputDebugStringW(L"PresentationBeforeClose");
	HRESULT hr = S_OK;

	BSTR DocFullName = NULL;
	hr = Pres->get_FullName(&DocFullName);
	if (FAILED(hr) || !DocFullName)
	{
		return    S_OK;
	}

	if (!CommonFunction::IsNXLFile(DocFullName)) {
		return    S_OK;
	}

	//get file is readonly
	Office2016::MsoTriState readonlyState = Office2016::msoFalse;
	hr = Pres->get_ReadOnly(&readonlyState);
	if (SUCCEEDED(hr) && (readonlyState==Office2016::msoTrue) )
	{
		//is file unsaved.
		Office2016::MsoTriState savedState = Office2016::msoTrue;
	  	hr = Pres->get_Saved(&savedState); 
	    if (SUCCEEDED(hr) && (savedState==Office2016::msoFalse))
	    {
			DWORD dwRes = DialogBoxParamW(g_hModule, MAKEINTRESOURCEW(IDD_PPT_SAVEASTIP), GetForegroundWindow(),
				(DLGPROC)DlgProcSaveAsTip, (LPARAM)this);
			if (dwRes == ID_SAVEAS)
			{
				//notify 
				MessageBoxW(GetForegroundWindow(), ERROR_MSG_NO_SAVE_RIGHT, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
				*Cancel = VARIANT_TRUE;
			}
			else if (dwRes==IDC_DONT_SAVE)
			{
				hr = Pres->put_Saved(Office2016::msoTrue);
				if (FAILED(hr)){
					 *Cancel = VARIANT_TRUE;
				}
			}
			else {
				*Cancel = VARIANT_TRUE;
			}

	    }
	}
	return hr;
}



HRESULT STDMETHODCALLTYPE PowerPointEventListener::ProtectedViewWindowActivate (
/*[in]*/ struct PowerPoint2016::ProtectedViewWindow * ProtViewWindow )
{
	HRESULT hr = S_OK;

	do 
	{
		PowerPoint2016::_PresentationPtr Pres = NULL;
		hr = ProtViewWindow->get_Presentation(&Pres);
		if (!SUCCEEDED(hr) || Pres == NULL)
		{
			break;
		}

		//get rights
		ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
		CheckPPTRight(Pres, RightsMask);
		//RightsMask = GetMinialRights(RightsMask);
		//update active document
		CComBSTR PptFullName = NULL;
		Pres->get_FullName(&PptFullName);
		if (PptFullName.m_str)
		{
			m_ActiveDoc = PptFullName;
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

	} while (FALSE);

	return hr;
}

HRESULT STDMETHODCALLTYPE PowerPointEventListener::PresentationCloseFinal(
/*[in]*/ struct PowerPoint2016::_Presentation * Pres)
{
	::OutputDebugStringW(L"PresentationCloseFinal");
	HRESULT hr = S_OK;
	BSTR DocFullName = NULL;

	do
	{
		hr = Pres->get_FullName(&DocFullName);

		if (FAILED(hr) || !DocFullName)
		{
			break;
		}

		if (CommonFunction::IsNXLFile(DocFullName))
		{
			//need save
			bool changed = IEventBase::IsFileChanged(m_PresentationChanged, DocFullName);
			IEventBase::RemoveFileChanged(m_PresentationChanged, DocFullName);

			if (CommonFunction::openedNxlFilePath.count(DocFullName)) {
				CommonFunction::openedNxlFilePath.erase(DocFullName);
			}

			if (CommonFunction::IsPathLenghtMoreThan259(DocFullName)) {
				HWND hWnd = GetForegroundWindow();
				MessageBoxW(hWnd, ERROR_MSG_FILE_PATH_MORE_THAN_259, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
				return S_OK;
			}

			std::vector<std::pair<std::wstring, std::wstring>> vecTagPair;
			std::wstring labelValue = L"";
			if (CommonFunction::ReadTag(Pres, vecTagPair)) {
				labelValue = CommonFunction::ParseTag(vecTagPair);
			}

			/*Registry{
			 HKEY = Computer\HKEY_CURRENT_USER\SOFTWARE\NextLabs\SkyDRM\MIP
			 Value: Decrypt_Label  (REG_SZ)
			 Value_Data:  decrypt label
			 }*/

			bool IsSensitiveLabelEnabled = false;
			uint32_t enablelabel;
			{
				nextlabs::utils::Registry r;
				nextlabs::utils::Registry::param param(LR"_(SOFTWARE\NextLabs\SkyDRM\MIP)_", HKEY_CURRENT_USER);
				if (r.get(param, L"Enable_Label", enablelabel) && 1== enablelabel) {
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
				std::wstring file_name(DocFullName);
				std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(file_name);
				if (nxlFilePath.empty())// in user rpm folder
				{
          TCHAR temp_path[265];
          GetTempPath(255, temp_path);
          std::wstring temp(temp_path);
          std::wstring temp_file = temp + L"nextlabs.temp";
          bool res1 = CopyFile(file_name.c_str(), temp_file.c_str(), false);
          SkyDrmSDKMgr::Instance()->DelegateDeleteFile(file_name + L".nxl");
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
              std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(DocFullName);
              SkyDrmSDKMgr::Instance()->EditSaveFile(DocFullName, L"", true, changed ? EXIT_MODE_EXIT_SAVE : EXIT_MODE_EXIT_NOTSAVE, L"");
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
				std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(DocFullName);
        if (!IsSensitiveLabelEnabled)
        {
          labelValue = L"";
        }
				SkyDrmSDKMgr::Instance()->EditSaveFile(DocFullName, L"", true, changed ? EXIT_MODE_EXIT_SAVE : EXIT_MODE_EXIT_NOTSAVE, labelValue);
				if (changed) CommonFunction::NotifiyRMDAppToSyncNxlFile(nxlFilePath);
			}
		}

		// work around fix bug: 58112 
		{
			ATL::CComPtr<struct PowerPoint2016::DocumentWindows> spWnds = NULL;
			if (SUCCEEDED(Pres->get_Windows(&spWnds)) && spWnds != NULL) {
				long count = 0;
				if (SUCCEEDED(spWnds->get_Count(&count))) {
					for (int i = 1; i <= count; ++i) {
						ATL::CComPtr<struct PowerPoint2016::DocumentWindow> spWnd = NULL;
						if (FAILED(spWnds->Item(i, &spWnd)))  continue;
						HWND hWnd = NULL;
						if (FAILED(spWnd->get_HWND((long*)&hWnd))) continue;

						SkyDrmSDKMgr::Instance()->ClearViewOverlay(((CComBSTR)DocFullName).m_str, GetTopLevelParent(hWnd));

					}
				}
			}
		}

	} while (FALSE);

	if (DocFullName)
	{
		SysFreeString(DocFullName);
		DocFullName = NULL;
	}

	return hr;
}

void PowerPointEventListener::InvalidMsoControls(void)
{
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

		for (ULONG i = 0; i < (ULONG)(g_powerpoint_16_ribbon_info.size()); i++)
		{
			pRibbonUI->InvalidateControlMso((BSTR)g_powerpoint_16_ribbon_info[i].RibbonId);
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

BOOL CALLBACK PowerPointEventListener::DlgProcSaveAsTip(HWND hwndDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
	static HICON hICONWarn = NULL;

	switch (message)
	{
	case WM_INITDIALOG:
	{
		PowerPointEventListener* pPPTListener = (PowerPointEventListener*)lParam;

		//load and show icon
		hICONWarn = ::LoadIconW(NULL, MAKEINTRESOURCEW(IDI_WARNING));
		DWORD dwLastError = GetLastError();
		if (hICONWarn)
		{
			SendDlgItemMessage(hwndDlg, IDC_PICTURE, STM_SETIMAGE, (WPARAM)IMAGE_ICON, (LPARAM)hICONWarn);
		}

		//set path info
		std::wstring wstrFileName = CommonFunction::GetFileName(pPPTListener->m_ActiveDoc);
		std::wstring wstrTip = L"We can't save ";
		wstrTip += wstrFileName;
		wstrTip += L" because the file is read-only.";
		SetDlgItemTextW(hwndDlg, IDC_FILE_NAME, wstrTip.c_str());
	}
	return TRUE;
	break;

	case WM_COMMAND:
		switch (LOWORD(wParam))
		{
		case ID_SAVEAS:
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

HRESULT PowerPointEventListener::CheckPPTRight(PowerPoint2016::_PresentationPtr PPt, ULONGLONG& RightMask)
{
	HRESULT hr = S_OK;
	RightMask = BUILTIN_RIGHT_ALL;

	do
	{
		CComBSTR pptFullName;
		hr = PPt->get_FullName(&pptFullName);
		if (SUCCEEDED(hr) && (pptFullName.m_str != NULL))
		{
			SkyDrmSDKMgr::Instance()->CheckRights(pptFullName.m_str, RightMask);
			// comment by osmond, ignore record each file's rigths
			//m_NxlRightsContainer.insert(std::wstring(pptFullName), RightMask);
			//remove edit right if the file is readonly
			if (CommonFunction::IsNXLFile(pptFullName.m_str))
			{
				Office2016::MsoTriState stateReadonly = Office2016::msoFalse;
				hr = PPt->get_ReadOnly(&stateReadonly);

				if ((RightMask&BUILTIN_RIGHT_EDIT) &&
					SUCCEEDED(hr) &&
					(stateReadonly == Office2016::msoTrue))
				{
					theLog.WriteLog(0, NULL, 0, L"CheckPPTRight the file is readonly remove edit right:%s\r\n", pptFullName.m_str);
					RightMask &= ~BUILTIN_RIGHT_EDIT;
				}
			}
		}

	} while (FALSE);


	return hr;
}


void PowerPointEventListener::Impl_Feature_AntiAutoRecovery()
{
	std::wstring default_dir;
	std::wstring dir;

	default_dir = GetDefaultAutoSaveFolder();
	if (g_nxrmExt2) {
		g_nxrmExt2->Register_AASR_Folder(default_dir);
	}
	if (GetAutoRecoveryDir(dir)) {

		if (g_nxrmExt2) {
			g_nxrmExt2->Register_AASR_Folder(dir);

		}
	}
}

bool PowerPointEventListener::GetAutoRecoveryDir(std::wstring& outPath) {

	HRESULT hr = S_OK;
	if (m_pApplication==NULL) {
		return false;
	}

	std::wstring path = L"Software\\Microsoft\\Office\\{Version}\\PowerPoint\\Options";

	CComBSTR ver;
	m_pApplication->get_Version(&ver);
	path.replace(path.find(L"{Version}"), sizeof("{Version}") - 1, ver);

	CRegKey key;
	if (ERROR_SUCCESS == key.Open(HKEY_CURRENT_USER, path.c_str(), KEY_WRITE | KEY_READ))
	{
		wchar_t buf[255] = { 0 };
		ULONG len = 255;
		if (ERROR_SUCCESS != key.QueryStringValue(L"PathToAutoRecoveryInfo", buf, &len)) {
			// value not exist, ppt use default one, set it default one
			wchar_t var_env[255] = { 0 };
			::GetEnvironmentVariableW(L"APPDATA", var_env, 255);
			outPath.assign(var_env);
			outPath += L"\\Microsoft\\PowerPoint\\";
			return true;
		}
		else {
			outPath.assign(buf);

			return true;
		}
	}

	return false;
}

//void PowerPointEventListener::SetupMonitorAutoRecoveryDir()
//{
//	// set autoclearAutorecoveryPath
//	std::wstring arPath;
//	if (GetAutoRecoveryDir(arPath) && m_AutoRecoveryDirMonitor.AddDir(arPath, [](const std::wstring& path) {
//		// clear all file under path
//		theLog.WriteLog(0, NULL, 0, L"ppt callback of delete files in folder: %s", path.c_str());
//		CommonFunction::TryingDeleteRegKeyInResiliency();
//		CommonFunction::ClearFolderContents(path);
//		})) {
//		m_AutoRecoveryDirMonitor.StartWork();
//	}
//}
//
//void PowerPointEventListener::SetupMonitorAutoSaveDir()
//{
//	// set autoclearAutorecoveryPath
//	std::wstring arPath;
//	if (GetAutoRecoveryDir(arPath) && m_AutoRecoveryDirMonitor.AddDir(arPath, [](const std::wstring& path) {
//		// clear all file under path
//		theLog.WriteLog(0, NULL, 0, L"ppt callback of delete files in folder: %s", path.c_str());
//		CommonFunction::ClearFolderContents(path);
//		})) {
//		m_AutoRecoveryDirMonitor.StartWork();
//	}
//}
//
void PowerPointEventListener::DisableAutoRecoverFeature() {
	if (m_pApplication == NULL) {
		return;
	}
	// disable PowerPoint AutoRecovery feature
	std::wstring path = L"Software\\Microsoft\\Office\\{Version}\\PowerPoint\\Options";

	CComBSTR ver;
	m_pApplication->get_Version(&ver);

	path.replace(path.find(L"{Version}"), sizeof("{Version}") - 1, ver);

	CRegKey key;
	if (ERROR_SUCCESS == key.Open(HKEY_CURRENT_USER, path.c_str(), KEY_WRITE))
	{
		key.SetDWORDValue(L"KeepUnsavedChanges", 0);
		key.SetDWORDValue(L"SaveAutoRecoveryInfo", 0);
	}
}
