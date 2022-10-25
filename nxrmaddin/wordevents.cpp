#include "stdafx.h"
#include "wordevents.h"
#include "nxrmext2.h"
#include "rightsdef.h"
#include "SkyDrmSDKMgr.h"
#include "Log.h"
#include "CommonFunction.h"

extern nxrmExt2* g_nxrmExt2;

extern "C" const std::vector<RIBBON_ID_INFO> g_word_16_ribbon_info;

namespace{

	inline HWND GetDocMainWindow(struct Word2016::_Document * Doc) {
		Word2016::WindowPtr activeWnd = NULL;
		Doc->get_ActiveWindow(&activeWnd);
		long activeHWND = 0;
		activeWnd->get_Hwnd(&activeHWND);			
		return (HWND)(LONG_PTR)activeHWND;		
	}

	inline HRESULT GetDocPath(struct Word2016::_Document * Doc, CComBSTR& FullPath) {
		return Doc->get_FullName(&FullPath);
	}

	inline void ApplyAntiScreenCaptureFeature(struct Word2016::_Document* Doc, ULONGLONG RightsMask) {
		if (!nextlabs::utils::isAppStreamEnv()) {
			auto hwnd = GetDocMainWindow(Doc);
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
		outPath += L"\\Microsoft\\Word\\";
		return outPath;
	}
};

void WordEventListener::Impl_Feature_AntiAutoRecovery()
{
	if (CommonFunction::IsNXLFile(m_ActiveDoc.c_str())) {
		// 09/23/2020 by osmond, I provide fv to handle this gracefully
		// DisableAutoRecoverFeature();  // word self supported

		std::wstring dir_default = GetDefaultAutoSaveFolder();
		if (g_nxrmExt2) {
			g_nxrmExt2->Register_AASR_Folder(dir_default);
		}

		std::wstring dir;
		if (!GetAutoRecoveryDir(dir)) {
			return;
		}
		if (g_nxrmExt2) {
			g_nxrmExt2->Register_AASR_Folder(dir);
		}
	}
}

void WordEventListener::Init(IDispatch* pAppliation, IDispatch* pRibbonUI, BSTR ActiveDoc, ULONGLONG ActiveRights)
{
	m_pApplication = pAppliation;
	m_pRibbonUI = pRibbonUI;

	if (ActiveDoc) {
		m_ActiveDoc = ActiveDoc;
	}

	m_ActiveDocRights = ActiveRights;

	InvalidMsoControls();
}

HRESULT __stdcall WordEventListener::DocumentChange()
{
	HRESULT hr = S_OK;

	//get documents count
	long lDocCount = GetDocumentsCount();

	//check if a document is closed
	//bool bDocClosed = (m_pCloseRequestInfo != NULL ) && (m_pCloseRequestInfo->DocumentsCount > lDocCount);
	//if (bDocClosed)
	//{
		while (m_pCloseRequestInfos.size() > 0)
		{
			CloseRequestInfo* pCloseRequestInfo = m_pCloseRequestInfos[m_pCloseRequestInfos.size() - 1];
			////check if a document is closed
			if (!((pCloseRequestInfo != NULL) && (pCloseRequestInfo->DocumentsCount > lDocCount))) {
				break;
			}
			m_pCloseRequestInfos.pop_back();

			theLog.WriteLog(0, NULL, 0,
				L"DocumentChange detect a document is closed(old docCount:%d, new docCount:%d):%s\r\n",
				pCloseRequestInfo->DocumentsCount, lDocCount, pCloseRequestInfo->DocumentName.c_str());

			if (CommonFunction::IsNXLFile(pCloseRequestInfo->DocumentName.c_str()))
			{
				const wchar_t* wszFileName = pCloseRequestInfo->DocumentName.c_str();

				//need save ?
				bool changed = IEventBase::IsFileChanged(m_documentChangeState, wszFileName);
				IEventBase::RemoveFileChanged(m_documentChangeState, wszFileName);

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
				if (IsSensitiveLabelEnabled && nextlabs::utils::wstr_tolower(pCloseRequestInfo->LabelValue).compare(nextlabs::utils::wstr_tolower(decrptlabel)) == 0 && changed){
					std::wstring file_name(wszFileName);
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
					else{
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
                std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(wszFileName);
                SkyDrmSDKMgr::Instance()->EditSaveFile(wszFileName, L"", true, changed ? EXIT_MODE_EXIT_SAVE : EXIT_MODE_EXIT_NOTSAVE, L"");
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
					//fix bug for if a word file witch path more than 259 do save action will have problem
					if (!CommonFunction::IsPathLenghtMoreThan259(pCloseRequestInfo->DocumentName.c_str())) {
						// for bug-fix readonly nxl doc, should never call EditSaveFile
						if (!pCloseRequestInfo->IsReadOnly) {
							std::wstring nxlFilePath = CommonFunction::RPMEditFindMap(wszFileName);
              if (!IsSensitiveLabelEnabled)
              {
                pCloseRequestInfo->LabelValue = L"";
              }
							SkyDrmSDKMgr::Instance()->EditSaveFile(wszFileName, L"", true, changed ? EXIT_MODE_EXIT_SAVE : EXIT_MODE_EXIT_NOTSAVE, pCloseRequestInfo->LabelValue);
							if (changed) CommonFunction::NotifiyRMDAppToSyncNxlFile(nxlFilePath);
						}
					}
				}

				Impl_Feature_AntiAutoRecovery();
			}

			//free close request info
			FreeCloseRequestInfo(pCloseRequestInfo);
		}
	//}
	return hr;
}

HRESULT __stdcall WordEventListener::DocumentOpen(struct Word2016::_Document * Doc)
{
	HRESULT hr = S_OK;
	CComBSTR path;
	hr = GetDocPath(Doc,path);
	if (FAILED(hr))
	{
		return hr;
	}
	m_ActiveDoc = path;

	theLog.WriteLog(0, NULL, 0, L"DocumentOpen: Word, path:%s\r\n", path);

	//::MessageBoxW(NULL, L"", L"open", 0);

	if (CommonFunction::IsNXLFile(path)) {
		CommonFunction::openedNxlFilePath[std::wstring(path)] = true;
		SkyDrmSDKMgr::Instance()->AddViewLog(this->m_ActiveDoc);
		Impl_Feature_AntiAutoRecovery();
	}



	return hr;
}

HRESULT __stdcall WordEventListener::DocumentBeforeClose(struct Word2016::_Document * Doc,VARIANT_BOOL * /*Cancel*/)
{
	HRESULT hr = S_OK;
	CComBSTR path;
	if (FAILED(hr = GetDocPath(Doc, path))){
		return hr;
	}

	if (CommonFunction::openedNxlFilePath.count(path.m_str)) {
		CommonFunction::openedNxlFilePath.erase(path.m_str);
	}

	theLog.WriteLog(0, NULL, 0, L"DocumentBeforeClose name:%s\r\n", path.m_str);
	
	// cache this file will been closed, 
	// later in EventDocChange will apply the extra logic
	//FreeCloseRequestInfo();

	std::vector<std::pair<std::wstring, std::wstring>> vecTagPair;
	std::wstring labelValue = L"";
	if (CommonFunction::ReadTag(Doc, vecTagPair)) {
		labelValue = CommonFunction::ParseTag(vecTagPair);
	}

	m_pCloseRequestInfos.push_back(new CloseRequestInfo(Doc, path.m_str, GetDocumentsCount(), labelValue.c_str()));
	return hr;
}

HRESULT __stdcall WordEventListener::DocumentBeforePrint(struct Word2016::_Document * Doc, VARIANT_BOOL * Cancel)
{
	// Check the file if has Print rights, if not, deny the action. -- fix bug 60960
	ULONGLONG doc_rights = 0;
	GetDocumentRights(Doc, doc_rights);
	if (!(doc_rights & BUILTIN_RIGHT_PRINT))
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

	return S_OK;
}

// save, save_as, auto_save will callback here
HRESULT __stdcall WordEventListener::DocumentBeforeSave(struct Word2016::_Document * Doc,VARIANT_BOOL * SaveAsUI, VARIANT_BOOL * Cancel)
{
	HRESULT hr = S_OK;
	// new feature added in office 2013, 
	// autosave feature to support different editing-versions by this doc
	// check auto save or user select "don't save" in dialog
	VARIANT_BOOL varAutoSave = VARIANT_FALSE;
	Doc->get_IsInAutosave(&varAutoSave);
	if (varAutoSave!= VARIANT_FALSE)
	{
		return S_OK; // ingore auto-save
	}

	CComBSTR path;
	if (FAILED(GetDocPath(Doc, path)))
	{
		return S_OK;
	}

	if (CommonFunction::IsNXLFile(path)) {
		if (CommonFunction::IsPathLenghtMoreThan259(path.m_str)) {
			*Cancel = VARIANT_TRUE;
			MessageBoxW(GetDocMainWindow(Doc), ERROR_MSG_FILE_PATH_MORE_THAN_259, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
			return S_OK;
		}
	}

	ULONGLONG doc_rights = 0;	   
	GetDocumentRights(Doc, doc_rights);

	//
	// only r_edit can do save
	// only r_saveas can do saveas
	//
	*Cancel = VARIANT_FALSE;
	if (!(doc_rights & BUILTIN_RIGHT_EDIT) && (*SaveAsUI == VARIANT_FALSE)) // No_Edit_Right && want_to_save
	{
		*Cancel = VARIANT_TRUE;
		MessageBoxW(GetDocMainWindow(Doc), ERROR_MSG_NO_SAVE_RIGHT, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
		return hr;
	}
	else if (!(doc_rights & BUILTIN_RIGHT_SAVEAS) && (*SaveAsUI == VARIANT_TRUE)) // No_SaveAs_Right && want_to_saveAS
	{
		*Cancel = VARIANT_TRUE;
		MessageBoxW(GetDocMainWindow(Doc), ERROR_MSG_NO_SAVE_AS_RIGHT, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
		return hr;
	}
	
	// only care about save featrue;
	if (*SaveAsUI == VARIANT_FALSE)
	{
		//save the document
		HRESULT hr = Doc->Save();
		theLog.WriteLog(0, NULL, 0, L"Word DocumentBeforeSave Event, save result:0x%x\r\n", hr);
		//fix in environment of office 365 if save failed will finally save as out as another file
		if (FAILED(hr)) {
			if (CommonFunction::IsNXLFile(path)) {
				*Cancel = VARIANT_TRUE;
				MessageBoxW(GetDocMainWindow(Doc), ERROR_MSG_NO_SAVE_AS_RIGHT, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
				return hr;
			}
		}
		//CComBSTR path;
		//GetDocPath(Doc, path);
		IEventBase::SetFileChanged(m_documentChangeState, path);
	}

	return hr;
}

// each time when doc's wnd get activate, callback here,
// get doc's nxl rights and may need to update Ribbon UI (Disable some items in Ribbon)
HRESULT __stdcall WordEventListener::WindowActivate(struct Word2016::_Document * Doc,struct Word2016::Window * /*Wn*/)
{
	HRESULT hr = S_OK;
	ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
	BOOL bNeedUpdateRibbonUI = FALSE;
	
	CComBSTR path;

	if (SUCCEEDED(hr = GetDocPath(Doc, path))) {
		m_ActiveDoc = path;
	}
	else {
		m_ActiveDoc.clear();
	}

	// get doucment right
	GetDocumentRights(Doc, RightsMask);

	// update rights of active doc, 
	// if same rights, that means we don't need to update RibbonUI 
	if (m_ActiveDocRights != RightsMask){
		bNeedUpdateRibbonUI = TRUE;
		m_ActiveDocRights = RightsMask;
	}
	// update UI
	if (bNeedUpdateRibbonUI || m_InvalidCount == 0){
		InvalidMsoControls();
	}

	// attach a overlay to current doc
	SkyDrmSDKMgr::Instance()->SetupViewOverlay(path.m_str,GetDocMainWindow(Doc));
	ApplyAntiScreenCaptureFeature(Doc, RightsMask);

	if (CommonFunction::IsNXLFile(m_ActiveDoc.c_str())) {
		Impl_Feature_AntiAutoRecovery();
	}

	return hr;
}

void WordEventListener::InvalidMsoControls(void)
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

		for (ULONG i = 0; i < (ULONG)(g_word_16_ribbon_info.size()); i++)
		{
			pRibbonUI->InvalidateControlMso((BSTR)g_word_16_ribbon_info[i].RibbonId);
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

long WordEventListener::GetDocumentsCount()
{
	long lCount = 0;
	if (m_pApplication)
	{
		Word2016::DocumentsPtr docsPtr;
		m_pApplication->get_Documents(&docsPtr);

		if (docsPtr)
		{
			docsPtr->get_Count(&lCount);
		}
	}
	return lCount;
}

void WordEventListener::GetDocumentRights(Word2016::_DocumentPtr Doc, ULONGLONG & outRights)
{
	if (Doc == NULL) {
		return;
	}

	CComBSTR path;
	if (FAILED(GetDocPath(Doc, path)))
	{
		return;
	}

	//if (!CommonFunction::IsNXLFile(path)) {
	//	outRights = BUILTIN_RIGHT_ALL;
	//	return;
	//}

	if (FAILED(SkyDrmSDKMgr::Instance()->CheckRights(path, outRights)))
	{
		return;
	}
	
	// additional request :
	// remove edit right if the file is readonly	
	VARIANT_BOOL varReadOnly = VARIANT_FALSE;
	if (FAILED(Doc->get_ReadOnly(&varReadOnly))) {
		return;
	}
	if ((outRights & BUILTIN_RIGHT_EDIT) && (varReadOnly == VARIANT_TRUE)){
		theLog.WriteLog(0, NULL, 0, L"GetDocumentRights the file is readonly remove edit right:%s\r\n", path.m_str);
		outRights &= ~BUILTIN_RIGHT_EDIT;
	}
}

// wait for later to impl anti-auto_recovery
bool WordEventListener::GetAutoRecoveryDir(std::wstring& outPath) {
	HRESULT hr = S_OK;

	Word2016::OptionsPtr pOpts = NULL;
	hr = m_pApplication->get_Options(&pOpts);
	if (SUCCEEDED(hr) && (pOpts != NULL)) {
		CComBSTR cpath;
		hr = pOpts->get_DefaultFilePath(Word2016::wdAutoRecoverPath, &cpath);
		if (SUCCEEDED(hr)) {
			outPath.assign(cpath);
			return true;
		}
	}
	return false;
}
//
//void WordEventListener::SetupMonitorAutoRecoveryDir()
//{
//	// set autoclearAutorecoveryPath
//	std::wstring arPath;
//	if (GetAutoRecoveryDir(arPath) && m_AutoRecoveryDirMonitor.AddDir(arPath, [](const std::wstring&path) {
//		// clear all file under path
//		theLog.WriteLog(0, NULL, 0, L"Word callback of delete files in folder: %s", path.c_str());
//		CommonFunction::TryingDeleteRegKeyInResiliency();
//		CommonFunction::ClearFolderContents(path);
//	})) {
//		m_AutoRecoveryDirMonitor.StartWork();
//	}
//}
//

void WordEventListener::DisableAutoRecoverFeature()
{
	HRESULT hr = S_OK;

	Word2016::OptionsPtr pOpts = NULL;
	if (m_pApplication == NULL) {
		return;
	}

	hr = m_pApplication->get_Options(&pOpts);
	if (SUCCEEDED(hr) && (pOpts != NULL)) {
		pOpts->put_SaveInterval(0);
	}
}