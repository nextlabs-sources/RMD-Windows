#include "stdafx.h"
#include "nxrmext2.h"
#include "ribbonrights.h"
#include "rightsdef.h"

#include "wordevents.h"
#include "powerpointevents.h"
#include "excelevents.h"
#include "SkyDrmSDKMgr.h"
#include "import/excel2016.tlh"
#include "import/msword2016.tlh"
#include "import/msppt2016.tlh"
#include "HookManager.h"
#include <atlbase.h>
#include "CoreIDropTarget.h"
#include "log.h"
#include "CommonFunction.h"
#include "SafePrintHandler.h"     // see .h for details
//#include <experimental/filesystem>
#include <fstream>
#include <iostream>
#include <experimental/filesystem>

#define IsEqualIID(riid1, riid2) IsEqualGUID(riid1, riid2)
#define IsEqualCLSID(rclsid1, rclsid2) IsEqualGUID(rclsid1, rclsid2)

// Global Defined Here
nxrmExt2* g_nxrmExt2 = NULL;
extern LONG g_unxrmext2InstanceCount;


#pragma region OfficeRibbonItems
// RIBBON_ID_INFO: id,ristmask,customrisht
extern "C" const std::vector<RIBBON_ID_INFO> g_powerpoint_16_ribbon_info = { \
{L"TabInfo", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabOfficeStart", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabRecent", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileClose", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileSave", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FilePrintQuick", BUILTIN_RIGHT_PRINT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabSave", BUILTIN_RIGHT_SAVEAS | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabPrint", BUILTIN_RIGHT_PRINT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabShare", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"TabPublish", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ApplicationOptionsDialog", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"AdvancedFileProperties", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"UpgradeDocument", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileSendAsAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileEmailAsPdfEmailAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileEmailAsXpsEmailAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileInternetFax", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"TabHome", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabInsert", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabDesign", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabTransitions", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabAnimations", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabSlideShow", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabReview", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabDeveloper", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabView", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ScreenshotInsertGallery", RIGHTS_NOT_CACHE, 0ULL}, \
{L"ScreenClipping", RIGHTS_NOT_CACHE, 0ULL}, \
{L"OleObjectctInsert", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"OleObjectInsertMenu", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Paste", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT | BUILTIN_RIGHT_EDIT, 0ULL}, \
{L"PasteGallery", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"PasteGalleryMini", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL }, \
{L"PasteSpecialDialog", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ShowClipboard", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Cut", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Copy", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ObjectSaveAsPicture", BUILTIN_RIGHT_SAVEAS | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileSaveAs", BUILTIN_RIGHT_SAVEAS | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ShareDocument", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"Collaborate", BUILTIN_RIGHT_SEND, 0ULL} \
};


extern "C" const std::vector<RIBBON_ID_INFO> g_excel_16_ribbon_info = { \
{L"TabInfo", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabOfficeStart", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabRecent", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileClose", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileSave", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FilePrintQuick", BUILTIN_RIGHT_PRINT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabSave", BUILTIN_RIGHT_SAVEAS | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabPrint", BUILTIN_RIGHT_PRINT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabShare", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"TabPublish", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Publish2Tab", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ApplicationOptionsDialog", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"AdvancedFileProperties", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"UpgradeDocument", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileSendAsAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileEmailAsPdfEmailAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileEmailAsXpsEmailAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileInternetFax", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"TabHome", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabInsert", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabFormulas", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabReview", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabData", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"SheetMoveOrCopy", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ScreenshotInsertGallery", RIGHTS_NOT_CACHE, 0ULL}, \
{L"ScreenClipping", RIGHTS_NOT_CACHE, 0ULL}, \
{L"OleObjectctInsert", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Paste", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT | BUILTIN_RIGHT_EDIT, 0ULL}, \
{L"PasteGallery", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT | BUILTIN_RIGHT_EDIT, 0ULL}, \
{L"PasteSpecialDialog", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT | BUILTIN_RIGHT_EDIT, 0ULL}, \
{L"PasteGalleryMini", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT | BUILTIN_RIGHT_EDIT, 0ULL}, \
{L"ShowClipboard", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Cut", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Copy", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"CopyAsPicture", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ShareDocument", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"Collaborate", BUILTIN_RIGHT_SEND, 0ULL} \
};


extern "C" const std::vector<RIBBON_ID_INFO> g_word_16_ribbon_info = { \
{L"TabInfo", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabOfficeStart", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabRecent", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileClose", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileSave", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FilePrintQuick", BUILTIN_RIGHT_PRINT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabSave", BUILTIN_RIGHT_SAVEAS | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabPrint", BUILTIN_RIGHT_PRINT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabShare", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"TabPublish", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ApplicationOptionsDialog", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"UpgradeDocument", BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"FileSendAsAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileEmailAsPdfEmailAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileEmailAsXpsEmailAttachment", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"FileInternetFax", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"TabHome", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabInsert", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabWordDesign", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabPageLayoutWord", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabReferences", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"TabMailings", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"TabReviewWord", BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ScreenshotInsertGallery", RIGHTS_NOT_CACHE, 0ULL}, \
{L"ScreenClipping", RIGHTS_NOT_CACHE, 0ULL}, \
{L"OleObjectInsertMenu", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"OleObjectctInsert", BUILTIN_RIGHT_EDIT | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Paste", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT | BUILTIN_RIGHT_EDIT, 0ULL}, \
{L"PasteGallery", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"PasteSpecialDialog", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"PasteGalleryMini", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ShowClipboard", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"PasteSetDefault", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Cut", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"Copy", BUILTIN_RIGHT_CLIPBOARD | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ObjectSaveAsPicture", BUILTIN_RIGHT_SAVEAS | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"DigitalPrint", BUILTIN_RIGHT_SAVEAS | BUILTIN_RIGHT_DECRYPT, 0ULL}, \
{L"ShareDocument", BUILTIN_RIGHT_SEND, 0ULL}, \
{L"Collaborate", BUILTIN_RIGHT_SEND, 0ULL}, \
};

#pragma endregion


GetClipboardData_Fun nxrmExt2::m_oldGetClipboardData = NULL;
SetClipboardData_Fun nxrmExt2::m_oldSetClipboardData = NULL;
RegisterDragDrop_Fun nxrmExt2::m_oldRegisterDragDrop = NULL;
DoDragDrop_Fun   nxrmExt2::m_oldDoDragDrop = NULL;
OleCreateFromFile_Fun nxrmExt2::m_oldOleCreateFromFile = NULL;
OleCreateLinkToFile_Fun nxrmExt2::m_oldOleCreateLinkToFile = NULL;
OleCreateLink_Fun nxrmExt2::m_oldOleCreateLink = NULL;
StartDocW_Fun nxrmExt2::m_oldStarDocW = NULL;
EndPage_Fun nxrmExt2::m_oldEndPage = NULL;
StartPage_Fun nxrmExt2::m_oldStartPage = NULL;
CreateDCW_Fun nxrmExt2::m_oldCreateDCW = NULL;
EndDoc_Fun nxrmExt2::m_oldEndDoc = NULL;
Hooked_CoCreateInstance_Signature nxrmExt2::Hooked_CoCreateInstance_Next = NULL;
Hooked_MessageBoxW nxrmExt2::Hooked_MessageBoxW_Next = NULL;
Hooked_MessageBoxA nxrmExt2::Hooked_MessageBoxA_Next = NULL;

HRESULT STDMETHODCALLTYPE nxrmExt2::OnConnection(
	/*[in]*/ IDispatch * Application,
	/*[in]*/ AddInDesignerObjects::ext_ConnectMode ConnectMode,
	/*[in]*/ IDispatch * AddInInst,
	/*[in]*/ SAFEARRAY * * custom)
{
	DEVLOG_FUN;
	HRESULT	hr = E_FAIL;
	// sanity check
	if (!Application) { return hr; }
	if (m_pAppObj != NULL) { return hr; }

	//get application interface
	g_nxrmExt2 = this;
	IDispatch* pAppObj = NULL;

	if (SUCCEEDED(Application->QueryInterface(__uuidof(Word2016::_Application), (void**)&pAppObj)))
	{
		InitSetupWord(pAppObj);// ingore error
		// make word support print_overlay
		HookManager::Instance()->AddHookItem((void**)&m_oldEndPage, Core_EndPage, "gdi32.dll", "EndPage");
	}
	else if (SUCCEEDED(Application->QueryInterface(__uuidof(Excel2016::_Application), (void**)&pAppObj))) {
		// internal will hook SetClipBoardData
		InitSetupExcel(pAppObj);// ingore error
		// excel needs hook extra api , ::SetClipBoardData
		HookManager::Instance()->AddHookItem((void**)&m_oldSetClipboardData, Core_SetClipboardData,
			"user32.dll", "SetClipboardData");
		// excel print_overlay, need to hook StartPage, not EndPage
		HookManager::Instance()->AddHookItem((void**)&m_oldStartPage, Core_StartPage, "gdi32.dll", "StartPage");

	}
	else if (SUCCEEDED(Application->QueryInterface(__uuidof(PowerPoint2016::_Application), (void**)&pAppObj))) {
		InitSetupPowerPoint(pAppObj);// ingore error

		// make ppt support print_overlay
		HookManager::Instance()->AddHookItem((void**)&m_oldEndPage, Core_EndPage, "gdi32.dll", "EndPage");
	}
	//hook
	//hook GetClipboardData for "Paste Special..." context menu item in PPT and "Clipboard panel paste all".
	//when just select a cell(not double click a cell), the "Paste Special..." item can't be disabled by Ribbon.
	HookManager::Instance()->AddHookItem((void**)&m_oldGetClipboardData, Core_GetClipboardData,
		"user32.dll", "GetClipboardData");

	HookManager::Instance()->AddHookItem((void**)&m_oldRegisterDragDrop, Core_RegisterDragDrop,
		"ole32.dll", "RegisterDragDrop");
	HookManager::Instance()->AddHookItem((void**)&m_oldDoDragDrop, Core_DoDragDrop,
		"ole32.dll", "DoDragDrop");
	// by osmond. 02/17/2017
	HookManager::Instance()->AddHookItem((void**)&m_oldOleCreateFromFile, Core_OleCreateFormFile,
		"ole32.dll", "OleCreateFromFile");
	HookManager::Instance()->AddHookItem((void**)&m_oldOleCreateLinkToFile, Core_OleCreateLinkToFile,
		"ole32.dll", "OleCreateLinkToFile");
	HookManager::Instance()->AddHookItem((void**)&m_oldOleCreateLink, Core_OleCreateLink,
		"ole32.dll", "OleCreateLink");


	// by osmond. 05/06/2020, add createdcw and enddoc to determing pdf output to file finished
	HookManager::Instance()->AddHookItem((void**)&m_oldEndDoc, Core_EndDoc, "gdi32.dll", "EndDoc");
	HookManager::Instance()->AddHookItem((void**)&m_oldCreateDCW, Core_CreateDCW, "gdi32.dll", "CreateDCW");
	// powerpoint need to hook StartDoc to intercept print action
	// 05/06/2020, move from PowerPoint itself, since PrintToPDF_handler needs this
	HookManager::Instance()->AddHookItem((void**)&m_oldStarDocW, Core_StartDocW, "gdi32.dll", "StartDocW");
	// by osmond, give anti_autosave_handler an opportunity to handle Hook API

	if (CommonFunction::is_win8andabove()) {
		//by jack . 03/10/2021 , add CoCreateInstance to prevent PDF Maker operate relevence to data leak
		HookManager::Instance()->AddHookItem((void**)&Hooked_CoCreateInstance_Next, Hooked_CoCreateInstance_Instance,
			"combase.dll", "CoCreateInstance");
	}
	else {
		HookManager::Instance()->AddHookItem((void**)&Hooked_CoCreateInstance_Next, Hooked_CoCreateInstance_Instance,
			"ole32.dll", "CoCreateInstance");
	}

	//by jack. 03/10/2021 , add MessageBoxW to prevent a dialog about PDF Maker resource not found
	HookManager::Instance()->AddHookItem((void**)&Hooked_MessageBoxW_Next, Hooked_MessageBoxW_Instance,
		"user32.dll", "MessageBoxW");
	//by jack. 03/10/2021 , add MessageBoxA to prevent a dialog about PDF Maker resource not found
	HookManager::Instance()->AddHookItem((void**)&Hooked_MessageBoxA_Next, Hooked_MessageBoxA_Instance,
		"user32.dll", "MessageBoxA");
	
	m_aasr_handler.instance_setup();
	HookManager::Instance()->hook();
	// by osmond, MUST call here,
	// mark current process as Trushed via API
	InitRmSDK();
	
	return S_OK;
}

void nxrmExt2::InitRmSDK() {
	//get RPM user and added trust process
	SkyDrmSDKMgr* pSkyDrmSDKMgr = SkyDrmSDKMgr::Instance();
	if (pSkyDrmSDKMgr->GetCurrentLoggedInUser())
	{
		std::wstring wordapp = CommonFunction::GetApplicationPath(L"Word.Application");
		std::wstring excelapp = CommonFunction::GetApplicationPath(L"Excel.Application");
		std::wstring powerpointapp = CommonFunction::GetApplicationPath(L"PowerPoint.Application");

		::OutputDebugStringW(L"###########Enter Register Office RMX File Association#############");
		::OutputDebugStringW(wordapp.c_str());
		::OutputDebugStringW(excelapp.c_str());
		::OutputDebugStringW(powerpointapp.c_str());

		if (wordapp.size() > 0) {
			pSkyDrmSDKMgr->Instance()->RPMRegisterApp(wordapp, pSkyDrmSDKMgr->GetRMXPasscode());
		}

		if (excelapp.size() > 0) {
			pSkyDrmSDKMgr->Instance()->RPMRegisterApp(excelapp, pSkyDrmSDKMgr->GetRMXPasscode());
		}

		if (powerpointapp.size() > 0) {
			pSkyDrmSDKMgr->Instance()->RPMRegisterApp(powerpointapp, pSkyDrmSDKMgr->GetRMXPasscode());
		}

		pSkyDrmSDKMgr->NotifyRMXStatus(true, pSkyDrmSDKMgr->GetRMXPasscode());
		pSkyDrmSDKMgr->AddTrustedProcess(GetCurrentProcessId(), pSkyDrmSDKMgr->GetRMXPasscode());

		//connect to pc
		pSkyDrmSDKMgr->WaitInstanceInitFinish();
	}
}


HRESULT nxrmExt2::InitSetupWord(IDispatch* pApp)
{
	DEVLOG_FUN;
	m_OfficeAppType = OfficeAppWinword;
	nextlabs::utils::set_app_name(L"WINWORD.EXE");
	HRESULT hr = S_OK;
	m_pAppObj = pApp;
	for (const auto &ite : g_word_16_ribbon_info)
	{
		m_RibbonRightsMap.insert(std::pair<std::wstring, RIBBON_ID_INFO>(ite.RibbonId, ite));
	}
	
	// set event response
	WordEventListener *pWordEventSink = new WordEventListener();


	hr = ATL::AtlAdvise(m_pAppObj, pWordEventSink, __uuidof(Word2016::ApplicationEvents4), &dwAdviseCookie);
	if (!SUCCEEDED(hr))
	{
		return hr;
	}
	m_pOfficeEventSink = pWordEventSink;
	return hr;

}

HRESULT nxrmExt2::InitSetupExcel(IDispatch * pApp)
{
	DEVLOG_FUN;
	HRESULT hr = S_OK;
	m_OfficeAppType = OfficeAppExcel;
	nextlabs::utils::set_app_name(L"EXCEL.EXE");
	m_pAppObj = pApp;
	ExcelEventListener *pExcelEventSink = new ExcelEventListener();

	for (const auto &ite : g_excel_16_ribbon_info)
	{
		m_RibbonRightsMap.insert(std::pair<std::wstring, RIBBON_ID_INFO>(ite.RibbonId, ite));
	}

	hr = ATL::AtlAdvise(pApp, pExcelEventSink, __uuidof(Excel2016::AppEvents), &dwAdviseCookie);
	if (!SUCCEEDED(hr))
	{
		return hr;
	}
	m_pOfficeEventSink = pExcelEventSink;

	
	return hr;
}

HRESULT nxrmExt2::InitSetupPowerPoint(IDispatch * pApp)
{
	DEVLOG_FUN;
	m_OfficeAppType = OfficeAppPowerpoint;
	nextlabs::utils::set_app_name(L"POWERPNT.EXE");
	m_pAppObj = pApp;

	for (const auto &ite : g_powerpoint_16_ribbon_info)
	{
		m_RibbonRightsMap.insert(std::pair<std::wstring, RIBBON_ID_INFO>(ite.RibbonId, ite));
	}
	
	HRESULT hr = S_OK;
	PowerPointEventListener *pPowerPointEventSink = new PowerPointEventListener();

	hr = ATL::AtlAdvise(pApp, pPowerPointEventSink, __uuidof(PowerPoint2016::EApplication), &dwAdviseCookie);
	if (!SUCCEEDED(hr))
	{
		return hr;
	}
	m_pOfficeEventSink = pPowerPointEventSink;
	//DisablePowerPointAutoRecoveryFeature((PowerPoint2016::_Application*)pAppObj);
	return hr;
}


HRESULT STDMETHODCALLTYPE nxrmExt2::OnLoad(IDispatch *RibbonUI)
{
	DEVLOG_FUN;
	HRESULT hr = S_OK;

	m_pRibbonUI = RibbonUI;

	if (m_OfficeAppType == OfficeAppExcel)
	{
		InitExcelEventSink();
	}
	else if (m_OfficeAppType == OfficeAppPowerpoint)
	{
		InitPowerpointEventSink();
	}
	else if (m_OfficeAppType == OfficeAppWinword)
	{
		InitWordEventSink();
	}
	else
	{
		DEVLOG(L"should never reach here");
	}

	return hr;
}

HRESULT STDMETHODCALLTYPE nxrmExt2::OnCheckMsoButtonStatus(Office2016::IRibbonControl *pControl,VARIANT_BOOL *pvarfEnabled)
{
	CComBSTR	ribbon_id;
	ULONGLONG uAcitveRight = BUILTIN_RIGHT_ALL;
	VARIANT_BOOL varEnable = VARIANT_TRUE;
	std::wstring activeDoc;
	//bool isInSanctuaryFolder = false;

	//get right for acitve document
	if (!m_pOfficeEventSink)
	{
		return S_OK;
	}
	
	m_pOfficeEventSink->GetActiveRights(uAcitveRight);
	m_pOfficeEventSink->GetActiveDoc(activeDoc);
	//m_pOfficeEventSink->IsInSanctuaryFolder(isInSanctuaryFolder);
	
	//check button status
	pControl->get_Id(&ribbon_id);
	const auto &ite = m_RibbonRightsMap.find(ribbon_id.operator LPWSTR());

	if (ite != m_RibbonRightsMap.end())
	{
		varEnable = ((*ite).second.RightsMask & uAcitveRight) ? VARIANT_TRUE : VARIANT_FALSE;
				//if (varEnable && isInSanctuaryFolder) {
		//	// disable Share pane altogether
		//	if (((*ite).second.RightsMask & BUILTIN_RIGHT_SEND)) {
		//		varEnable = VARIANT_FALSE;
		//	}

		//	// disable Save as pane altogether
		//	if (((*ite).second.RightsMask & BUILTIN_RIGHT_SAVEAS)) {
		//		varEnable = VARIANT_FALSE;
		//	}

		//	if (_wcsicmp((*ite).second.RibbonId, L"ApplicationOptionsDialog") == 0) {
		//		varEnable = VARIANT_FALSE;
		//	}

		//	if (_wcsicmp((*ite).second.RibbonId, L"TabPublish") == 0) {
		//		varEnable = VARIANT_FALSE;
		//	}
		//}
		
		if ((wcsicmp(ribbon_id, L"ScreenshotInsertGallery") == 0) || (wcsicmp(ribbon_id, L"ScreenClipping") == 0)) {
			if (CommonFunction::openedNxlFilePath.empty()) {
				varEnable = VARIANT_TRUE;
			}
			else {
				varEnable = VARIANT_FALSE;
			}
		}
	}

	*pvarfEnabled = varEnable;

	return S_OK;
}

HRESULT nxrmExt2::InitWordEventSink(void)
{
	DEVLOG_FUN;
	HRESULT hr = S_OK;
	CComBSTR DocFullName;
	ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
	 

	if (m_OfficeAppType != OfficeAppWinword || m_pRibbonUI == NULL)
	{
		hr = E_UNEXPECTED;
		return hr;
	}

	//get active document
	Word2016::_Application* pWinwordAppObj = (Word2016::_Application*)m_pAppObj;


	// cache version
	CComBSTR version;
	pWinwordAppObj->get_Version(&version);
	CommonFunction::SetOfficeVersion(version.operator LPWSTR());
	CommonFunction::SetOfficeName(L"Word");

	Word2016::_DocumentPtr pDoc = NULL;
	hr = pWinwordAppObj->get_ActiveDocument(&pDoc);
	if (!SUCCEEDED(hr) || (pDoc == NULL))
	{
		Word2016::ProtectedViewWindowPtr protectViewWind = NULL;
		hr = pWinwordAppObj->get_ActiveProtectedViewWindow(&protectViewWind);
		if (SUCCEEDED(hr) && (protectViewWind != NULL))
		{
			protectViewWind->get_Document(&pDoc);
		}
	}

	//get document name and right
	if (pDoc)
	{
		WordEventListener* wordEvent = dynamic_cast<WordEventListener*>(m_pOfficeEventSink);
		if (wordEvent)
		{
			wordEvent->GetDocumentRights(pDoc, RightsMask);
		}

		pDoc->get_FullName(&DocFullName);
	}

	m_pOfficeEventSink->Init(m_pAppObj, m_pRibbonUI, DocFullName, RightsMask);
	
	return hr;
}

HRESULT nxrmExt2::InitExcelEventSink(void)
{
	DEVLOG_FUN;
	HRESULT hr = S_OK;

	CComBSTR DocFullName = NULL;
	ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
	Excel2016::_Application *pExcelAppObj = NULL;
	Excel2016::_WorkbookPtr	pWb = NULL;

	if (m_OfficeAppType != OfficeAppExcel || m_pRibbonUI == NULL){
		return E_UNEXPECTED;
	}

	pExcelAppObj = (Excel2016::_Application*)m_pAppObj;

	// cache version
	CComBSTR version;
	pExcelAppObj->get_Version(0x0409, &version);
	CommonFunction::SetOfficeVersion(version.operator LPWSTR());
	CommonFunction::SetOfficeName(L"Excel");

	//get active workbook
	hr = pExcelAppObj->get_ActiveWorkbook(&pWb);
	if (!SUCCEEDED(hr) || (pWb == NULL))
	{
		theLog.WriteLog(0, NULL, 0, L"in InitExcelEventSink, can not get activeworkbook, tyring to treat it as protectedwindow");
		Excel2016::ProtectedViewWindowPtr pProtectedWn = NULL;
		hr = pExcelAppObj->get_ActiveProtectedViewWindow(&pProtectedWn);
		if (SUCCEEDED(hr) && (pProtectedWn != NULL))
		{
			Excel2016::IProtectedViewWindowPtr pIProtectViewWnd = NULL;
			hr = pProtectedWn->QueryInterface(&pIProtectViewWnd);
			if (SUCCEEDED(hr) && (pIProtectViewWnd != NULL))
			{
				theLog.WriteLog(0, NULL, 0, L"in InitExcelEventSink, get protectedWnd's doc ");
				pIProtectViewWnd->get_Workbook(&pWb);
			}
		}
	}

	if (pWb)
	{
		hr = pWb->get_FullName(EXCEL_LCID_ENG, &DocFullName);
		theLog.WriteLog(0, NULL, 0, L"in InitExcelEventSink, a stange case, plugin not complete init,but doc has been opened.file:%s",DocFullName);
		ExcelEventListener* excelEvent = dynamic_cast<ExcelEventListener*>(m_pOfficeEventSink);
		if (excelEvent)
		{
			excelEvent->CheckWorkbookRight(pWb, RightsMask);
			excelEvent->ShowViewOverlayAuto_SpecialCase_ForBugFix(DocFullName);
		}
	}
	m_pOfficeEventSink->Init(m_pAppObj, m_pRibbonUI, DocFullName, RightsMask);



	return hr;
}

HRESULT nxrmExt2::InitPowerpointEventSink(void)
{
	DEVLOG_FUN;
	HRESULT hr = S_OK;
	ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
	PowerPoint2016::_Application *pPowerPointAppObj = NULL;
	PowerPoint2016::_PresentationPtr pPres = NULL;
	CComBSTR DocFullName = NULL;
	
	if (m_OfficeAppType != OfficeAppPowerpoint || m_pRibbonUI == NULL) {
		return  E_UNEXPECTED;
	}
	pPowerPointAppObj = (PowerPoint2016::_Application*)m_pAppObj;

	// cache version
	CComBSTR version;
	pPowerPointAppObj->get_Version(&version);
	CommonFunction::SetOfficeVersion(version.operator LPWSTR());
	CommonFunction::SetOfficeName(L"PowerPoint");

	//get active presentation
	hr = pPowerPointAppObj->get_ActivePresentation(&pPres);
	if (!SUCCEEDED(hr) || (pPres == NULL))
	{
		PowerPoint2016::ProtectedViewWindowPtr pProtectedWn = NULL;
		hr = pPowerPointAppObj->get_ActiveProtectedViewWindow(&pProtectedWn);
		if (SUCCEEDED(hr) && (pProtectedWn != NULL))
		{
			pProtectedWn->get_Presentation(&pPres);
		}
	}

	if (pPres)
	{
		pPres->get_FullName(&DocFullName);

		PowerPointEventListener* pptEvent = dynamic_cast<PowerPointEventListener*>(m_pOfficeEventSink);
		if (pptEvent)
		{
			pptEvent->CheckPPTRight(pPres, RightsMask);
		}
	}

	m_pOfficeEventSink->Init(m_pAppObj, m_pRibbonUI, DocFullName, RightsMask);

	return hr;
}

std::wstring nxrmExt2::Get_Jail_Root_Path()
{
	std::wstring gRedirected;
	if (nextlabs::utils::get_SkyDRM_folder(gRedirected)) {
		gRedirected += L"fv\\";
	}
	// make sure the whole path exist
	::SHCreateDirectoryEx(NULL, gRedirected.c_str(), NULL);
	return gRedirected;
}

void nxrmExt2::Delete_Anti_AutoSave_AutoRecoery()
{
	m_aasr_handler.impl_handler();
}

void nxrmExt2::Wait__WorkerThreads_End()
{
	if (m_worker_Threads.empty()) {
		return;
	}
	auto hs = m_worker_Threads;
	for (auto h : hs) {
		::WaitForSingleObject(h, INFINITE);
		::CloseHandle(h);
	}
}

HANDLE WINAPI nxrmExt2::Core_SetClipboardData(UINT uFormat, HANDLE hMem)
{
	DEVLOG_FUN;
	if (g_nxrmExt2 && g_nxrmExt2->m_pOfficeEventSink)
	{
		ULONGLONG uRights = 0;
		g_nxrmExt2->m_pOfficeEventSink->GetActiveRights(uRights);

		if (!(uRights&BUILTIN_RIGHT_CLIPBOARD))
		{
			return NULL;
		}
	}

	if (m_oldSetClipboardData)
	{
		return m_oldSetClipboardData(uFormat, hMem);
	}

	return NULL;
}

HANDLE WINAPI nxrmExt2::Core_GetClipboardData(UINT uFormat)
{
	DEVLOG_FUN;
	if (g_nxrmExt2 && g_nxrmExt2->m_pOfficeEventSink)
	{
		ULONGLONG uRights = 0;
		g_nxrmExt2->m_pOfficeEventSink->GetActiveRights(uRights);

		if (!(uRights&(BUILTIN_RIGHT_CLIPBOARD|BUILTIN_RIGHT_EDIT)))
		{
			return NULL;
		}
	}

	if (m_oldGetClipboardData)
	{
		return m_oldGetClipboardData(uFormat);
	}

	return NULL;
}

HRESULT WINAPI nxrmExt2::Core_RegisterDragDrop(IN HWND hwnd, IN LPDROPTARGET pDropTarget)
{
	HRESULT hr = S_OK;
	CoreIDropTarget *pCoreIDropTarget = NULL;
	IDropTarget *pMyIDropTarget = pDropTarget;
	do
	{
		if (!pDropTarget)
		{
			if (m_oldRegisterDragDrop)
			{
				hr = m_oldRegisterDragDrop(hwnd, pDropTarget);
			}	
			break;
		}

		if (m_oldRegisterDragDrop)
		{
			try
			{
				pCoreIDropTarget = new CoreIDropTarget(pDropTarget);
				pMyIDropTarget = (IDropTarget *)pCoreIDropTarget;
			}
			catch (std::bad_alloc e)
			{
				pMyIDropTarget = pDropTarget;
			}
			hr = m_oldRegisterDragDrop(hwnd, pMyIDropTarget);
		}
		
	} while (FALSE);

	return hr;
}

HRESULT WINAPI nxrmExt2::Core_DoDragDrop(IN LPDATAOBJECT pDataObj, IN LPDROPSOURCE pDropSource, IN DWORD dwOKEffects, OUT LPDWORD pdwEffect)
{
	DEVLOG_FUN;
	ULONGLONG activeRights = BUILTIN_RIGHT_ALL;
	if (g_nxrmExt2)
		g_nxrmExt2->GetActiveRights(activeRights);
	if (!(activeRights & BUILTIN_RIGHT_EDIT))
	{
		//pdwEffect = DROPEFFECT_NONE;
		return DRAGDROP_S_CANCEL;	// block
	}
	else
	{
		return m_oldDoDragDrop(pDataObj, pDropSource, dwOKEffects, pdwEffect);
	}
}

//static // by osmond. 02/17/2017
HRESULT WINAPI nxrmExt2::Core_OleCreateFormFile(
	IN REFCLSID        rclsid,
	IN LPCOLESTR       lpszFileName,
	IN REFIID          riid,
	IN DWORD           renderopt,
	IN LPFORMATETC     lpFormatEtc,
	IN LPOLECLIENTSITE pClientSite,
	IN LPSTORAGE       pStg,
	OUT LPVOID* ppvObj
) {
	DEVLOG_FUN;
	// any time, deny insert nxl file
	if (lpszFileName) {
		if (CommonFunction::IsNXLSuffix(lpszFileName)) {
			return STG_E_FILENOTFOUND;
		}
		if (CommonFunction::IsNXLFile(lpszFileName)) {
			return STG_E_FILENOTFOUND;
		}

#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
		uint32_t dirstatus;
		std::wstring filetags;
		std::wstring wstrPath = lpszFileName;
		std::wstring directory = L"";
		directory = wstrPath.substr(0, wstrPath.find_last_of(L"\\/"));
		if (CommonFunction::IsSanctuaryFolder(directory, &dirstatus, filetags)) {
			return STG_E_FILENOTFOUND;
		}
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR

	}

	if (m_oldOleCreateFromFile) {
		return m_oldOleCreateFromFile(rclsid, lpszFileName, riid, renderopt, lpFormatEtc, pClientSite, pStg, ppvObj);
	}
	else {
		return STG_E_FILENOTFOUND;
	}

}

HRESULT WINAPI nxrmExt2::Core_OleCreateLinkToFile(
	IN LPCOLESTR       lpszFileName,
	IN REFIID          riid,
	IN DWORD           renderopt,
	IN LPFORMATETC     lpFormatEtc,
	IN LPOLECLIENTSITE pClientSite,
	IN LPSTORAGE       pStg,
	OUT LPVOID* ppvObj)
{
	DEVLOG_FUN;
	// any time, deny insert nxl file
	if (lpszFileName) {
		if (CommonFunction::IsNXLSuffix(lpszFileName)) {
			return STG_E_FILENOTFOUND;
			//return S_OK;
		}
		if (CommonFunction::IsNXLFile(lpszFileName)) {
			return STG_E_FILENOTFOUND;
			//return S_OK;
		}

#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
		uint32_t dirstatus;
		std::wstring filetags;
		std::wstring wstrPath = lpszFileName;
		std::wstring directory = L"";
		directory = wstrPath.substr(0, wstrPath.find_last_of(L"\\/"));
		if (CommonFunction::IsSanctuaryFolder(directory, &dirstatus, filetags)) {
			return STG_E_FILENOTFOUND;
		}
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR

	}

	if (m_oldOleCreateLinkToFile) {
		return m_oldOleCreateLinkToFile(lpszFileName, riid, renderopt, lpFormatEtc, pClientSite, pStg, ppvObj);
	}
	else {
		return STG_E_FILENOTFOUND;
	}

}

HRESULT WINAPI nxrmExt2::Core_OleCreateLink(
	IN LPMONIKER       pmkLinkSrc,
	IN REFIID          riid,
	IN DWORD           renderopt,
	IN LPFORMATETC     lpFormatEtc,
	IN LPOLECLIENTSITE pClientSite,
	IN LPSTORAGE       pStg,
	OUT LPVOID* ppvObj
) {
	DEVLOG_FUN;
	// try get parse from pmkLinkSrc, and then check if is nxl
	auto routine_is_nxl_file = [pmkLinkSrc]() {
		if (pmkLinkSrc == NULL) {
			return false;
		}
		ATL::CComPtr<IBindCtx> spBind;
		if (FAILED(::CreateBindCtx(NULL, &spBind))) {
			return false;
		}
		ATL::CComBSTR spPath;
		if (FAILED(pmkLinkSrc->GetDisplayName(spBind,NULL,&spPath))) {
			return false;
		}
		if (spPath.Length() < 4) {
			return false;
		}

		std::wstring path = spPath;
		spPath.Detach();  // may cause dataleak, but hwo to find IMalloc::Free?

		if (CommonFunction::IsNXLSuffix(path.c_str())) {
			
			return true;
			//return S_OK;
		}

		if (CommonFunction::IsNXLFile(path.c_str())) {
			return true;
			//return S_OK;
		}

#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
		uint32_t dirstatus;
		std::wstring filetags;
		std::wstring wstrPath = path;
		std::wstring directory = L"";
		directory = wstrPath.substr(0, wstrPath.find_last_of(L"\\/"));
		if (CommonFunction::IsSanctuaryFolder(directory, &dirstatus, filetags)) {
			return true;
		}
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR

		return false;
	};
	
	if (routine_is_nxl_file()) {
		return OLE_E_CANT_BINDTOSOURCE;
	}
	else {
		return m_oldOleCreateLink(pmkLinkSrc, riid, renderopt, lpFormatEtc, pClientSite, pStg, ppvObj);
	}

}


// for powerpoint.exe exclusively, other will rely on Plguin-provided function.
// // 05/06/2020, PrintToPDFHandler needs this
int __stdcall nxrmExt2::Core_StartDocW(HDC hdc, const DOCINFOW* lpdi)
{
	DEVLOG_FUN;	
	// sanity check
	if (!m_oldStarDocW) {
		return 0;
	}
	if (!is_valid_plugin_config()) {
		return 0;
	}
	// normal file, directly go through
	std::wstring nxl;
	g_nxrmExt2->m_pOfficeEventSink->GetActiveDoc(nxl);
	if (!CommonFunction::IsNXLFile(nxl.c_str())) {
		// non NXL file, continue to print
		theLog.WriteLog(L"call Core_StartDocW, pass through for normal file: %s\n",nxl.c_str());
		return m_oldStarDocW(hdc, lpdi);
	}

	//
	// Algorithm, for nxl file 
	//
	auto& handler = SafePrintHandler::Instance();
	if (handler.Enforce_Security_Policy(hdc) != SafePrintHandler::Result_True_Support_Safe_Print)
	{
		if (handler.Enforce_Security_Policy(hdc) == SafePrintHandler::Result_False_Match_In_Prohibited_List)
		{
			SkyDrmSDKMgr::Instance()->AddPrintLog(nxl, false);
			SkyDrmSDKMgr::Instance()->Notify_PrinterNotSupportedMessage(nxl);
			return 0;
		}
	}

	// Powerpoint extras: 
	// powerpoint's print action will works here
	// powerpoint using this API to intercept Print
	if (g_nxrmExt2->m_OfficeAppType == nxrmOfficeAppType::OfficeAppPowerpoint) {
		ULONGLONG rights = 0;
		g_nxrmExt2->m_pOfficeEventSink->GetActiveRights(rights);
		if (!(rights & BUILTIN_RIGHT_PRINT)) {
			//
			// handle deny print, log + notify
			//
			SkyDrmSDKMgr::Instance()->AddPrintLog(nxl, false);
			// send notification
			SkyDrmSDKMgr::Instance()->Notify_PrintDenyMessage(nxl);
			return 0;
		}
		else {
			//
			// handle allow print
			//
			SkyDrmSDKMgr::Instance()->AddPrintLog(nxl, true);
		}		
	}


	// both Office App work: 
	auto jobID = m_oldStarDocW(hdc, lpdi);

	// extra allow routine, record the jobID
	// record handler
	if (jobID > 0) {
		handler.Modify_JobID(hdc, jobID);
		handler.Insert(hdc, nxl);
		theLog.WriteLog(L"call Core_StartDocW, record the nxl file: %s\n", nxl.c_str());
	}
	return jobID;

}

int __stdcall nxrmExt2::Core_StartPage(HDC hdc)
{
	static int x = 0;
	DEVLOG_FUN;
	// sanity check
	if (!m_oldStartPage) {
		return 0;
	}
	if (!is_valid_plugin_config()) {
		return 0;
	}

	//
	// Only Excel will call here, 
	//
	auto rt = m_oldStartPage(hdc);
	if (rt > 0) {
		auto x = ::SaveDC(hdc);
		// here to attach overlay to the page
		std::wstring path;
		g_nxrmExt2->m_pOfficeEventSink->GetActiveDoc(path);
		SkyDrmSDKMgr::Instance()->AttachPrintOverlay(path, hdc);

		::RestoreDC(hdc, x);

	}
	::SetLastError(ERROR_SUCCESS);
	return rt;
}

int __stdcall nxrmExt2::Core_EndPage(HDC hdc)
{
	DEVLOG_FUN;
	// sanity check
	if (!m_oldEndPage) {
		return 0;
	}
	if (!is_valid_plugin_config()) {
		return 0;
	}

	// here to attach overlay to the page
	std::wstring path;
	g_nxrmExt2->m_pOfficeEventSink->GetActiveDoc(path);
	SkyDrmSDKMgr::Instance()->AttachPrintOverlay(path, hdc);
	return m_oldEndPage(hdc);

}


namespace {
	//
	//  back ground nxl convert handler, 
	//
	typedef struct _convert_nxl_thread_param {
		wchar_t* output_file_org_path;
		wchar_t* template_nxl_file_path;
	}convert_nxl_thread_param;

	inline convert_nxl_thread_param* new_param(const std::wstring& p1, const std::wstring& p2) {

		auto* rt = new convert_nxl_thread_param;

		wchar_t* v1 = new wchar_t[p1.length() + 1]; {
			wcsncpy(v1, p1.c_str(), p1.length() + 1);
		}

		wchar_t* v2 = new wchar_t[p2.length() + 1]; {
			wcsncpy(v2, p2.c_str(), p2.length() + 1);
		}
		
		rt->output_file_org_path = v1;
		rt->template_nxl_file_path = v2;

		return rt;
	}

	inline void delete_param(convert_nxl_thread_param* param) {
		if (param == NULL) {
			return;
		}
		if (param->output_file_org_path != NULL) {
			delete [] param->output_file_org_path;
		}
		if (param->template_nxl_file_path != NULL) {
			delete[] param->template_nxl_file_path;
		}
		delete param;
	}

	unsigned __stdcall convert_file_to_nxl_worker(void* params) {

		if (params == NULL) {
			DEVLOG(L"NULL param in nxl-convert thread, coding problem");
			return NULL;
		}
		convert_nxl_thread_param* p = (convert_nxl_thread_param*)params;

		if (p->output_file_org_path == NULL || p->template_nxl_file_path == NULL) {
			DEVLOG(L"invlaid param in nxl-convert thread, coding problem");
			return NULL;
		}

		std::wstring path = p->output_file_org_path;
		std::wstring nxl = p->template_nxl_file_path;


		std::wstring new_nxl_file = SafePrintHandler::Instance().Convert_PrintedFile_To_NXL(path, nxl);
		if (!new_nxl_file.empty())
		{
			std::wstring msg = L"The printed file is protected as '" + new_nxl_file + L"'.";
			SkyDrmSDKMgr::Instance()->Popup_Message(msg, nxl);
		}
		else {
			// failed when converting
			std::wstring msg = L"System error occurred when printing this file.";
			SkyDrmSDKMgr::Instance()->Popup_Message(msg, nxl);
		}

		delete_param(p);

		return 0;
	}

}


/*
	GDI32 internal deficience:
		EndDoc will be called twice at a time,   EndDoc -> EndDocImpl -> Endoc
*/
int __stdcall nxrmExt2::Core_EndDoc(HDC hdc)
{
	DEVLOG_FUN;
	// sanity check
	if (!m_oldEndDoc) {
		return 0;
	}
	if (!is_valid_plugin_config()) {
		return 0;
	}

	//
	// EndDoc means printing job finished, Only nxl file will be add extra code logic. Normal File no detoured
	//

	//
	// Algorithm, only care about nxl
	//
	auto& handler = SafePrintHandler::Instance();

	::EnterCriticalSection(&(handler._lock));
	if (!handler.Contain(hdc) || handler.Get_JobID(hdc) == -1) {
		::LeaveCriticalSection(&(handler._lock));
		// this hdc was not record in startDoc, that means it's a normal file maybe.
		theLog.WriteLog(L"call Core_EndDoc, can not find hdc in print_handler, regart as normal file, pass through\n");
		return m_oldEndDoc(hdc);
	}

	std::vector<std::wstring> path;
	// this is for nxl file finished print, retirve the nxl file path first
	std::wstring nxl = handler.GetNxlPath(hdc);
	// path exist and valid
	if (SafePrintHandler::Instance().GetOutputPath_IfHDCIsValid(hdc, path)) {	

		if (g_nxrmExt2->m_OfficeAppType == nxrmOfficeAppType::OfficeAppPowerpoint)
		{
			// in PowerPoint, the 2nd print will reuse the old hdc
			handler.Modify_JobID(hdc, -1);
		}
		else
		{
			handler.Remove(hdc);
		}

		::LeaveCriticalSection(&(handler._lock));

		if (path.size() == 1)
		{
			// Notice:  path is valid, but the caller may not release it.
			auto rt = m_oldEndDoc(hdc);

			std::wstring SafePrinter_Path = path[0];
			theLog.WriteLog(L"call Core_EndDoc, Finished NXL[%s] File Print,to [%s]\ncall _beginthreadex to start convert_file_to_nxl_worker\n", nxl.c_str(), SafePrinter_Path.c_str());

			auto param = new_param(SafePrinter_Path, nxl);
			// fire an thread to handle the converting task	
			HANDLE h = (HANDLE)::_beginthreadex(NULL, NULL,
				convert_file_to_nxl_worker, (void*)param, NULL, NULL);

			if (h != INVALID_HANDLE_VALUE) {
				g_nxrmExt2->m_worker_Threads.insert(h);
			}

			return rt;
		}
		else if (path.size() >= 2)
		{
			// more than 2 jobs are running; before we align the jobid + GetActiveDoc(nxl) earlier (in startdoc), we will block 2 jobs
			SkyDrmSDKMgr::Instance()->Notify_PrinterNotSupportedMultiJobs(nxl);
			return 0;
		}
		else
		{
			// allowed job in printer which has no path; it might be physical printer in our printer whitelist
			return m_oldEndDoc(hdc);
		}
	}
	else
	{
		//
		// STOP printing as we can't find the output file path
		//
		//		it would be to print file to OneNote, Adobe PDF, WebEx Meeting
		//
		// send notification

		::LeaveCriticalSection(&(handler._lock));
        if (handler.Enforce_Security_Policy(hdc) != SafePrintHandler::Result_True_Support_Safe_Print)
        {
            SkyDrmSDKMgr::Instance()->Notify_PrinterNotSupportedMessage(nxl);
        }
        else
        {
            return m_oldEndDoc(hdc);
        }

		return 0;
	}
	::LeaveCriticalSection(&(handler._lock));

	// for miss match, go as normal
	return m_oldEndDoc(hdc);
}

//
// Printer will be created here, and it must be distinguished which printer has been invoked.
//
HDC __stdcall nxrmExt2::Core_CreateDCW(LPCWSTR pwszDriver, LPCWSTR pwszDevice, LPCWSTR pszPort, const DEVMODEW* pdm)
{
	DEVLOG_FUN;
	
	// sanity check
	if (!m_oldCreateDCW) {
		return NULL;
	}
	// filter out
	// this is also for monitor screen to set display_dc, filter out this 
	if (pwszDriver && 0 == _wcsicmp(pwszDriver, L"DISPLAY")) {
		return m_oldCreateDCW(pwszDriver, pwszDevice, pszPort, pdm);
	}
	// filter out
	// NULL ==pwszDevice
	if (NULL == pwszDevice) {
		return m_oldCreateDCW(pwszDriver, pwszDevice, pszPort, pdm);
	}
	// filter out
	// normal, none- nxl file, will get through directly
	{
		if (!is_valid_plugin_config()) {
			return m_oldCreateDCW(pwszDriver, pwszDevice, pszPort, pdm);
		}
		std::wstring nxl;
		g_nxrmExt2->m_pOfficeEventSink->GetActiveDoc(nxl);
		// plain file(not nxl) should pass through, with out any extra processing
		if (!CommonFunction::IsNXLFile(nxl.c_str())) {
			// non NXL file, continue to print
			return m_oldCreateDCW(pwszDriver, pwszDevice, pszPort, pdm);
		}
	}
	
	//
	// Algorithm, only for nxl file
	//
	HDC rt = NULL;
	std::wstring printer_name = pwszDevice;
	auto& handler = SafePrintHandler::Instance();
	rt = m_oldCreateDCW(pwszDriver, pwszDevice, pszPort, pdm);
	handler.Insert(rt, -1, printer_name);
	return rt;
}

HRESULT __stdcall nxrmExt2::Hooked_CoCreateInstance_Instance(
	IN REFCLSID		rclsid,
	IN LPUNKNOWN	pUnkOuter,
	IN DWORD		dwClsContext,
	IN REFIID		riid,
	OUT LPVOID FAR* ppv)
{
	HRESULT hr = S_FALSE;
	CLSID clsidFilter1;
	wchar_t* clsid_str1 =L"{20AA230C-5224-451E-B8C0-37F261DD5F4A}";
	CLSIDFromString(clsid_str1, &clsidFilter1);

	if (IsEqualCLSID(rclsid, clsidFilter1))
	{
		std::wstring nxl;
		g_nxrmExt2->m_pOfficeEventSink->GetActiveDoc(nxl);
		if (CommonFunction::IsNXLFile(nxl.c_str())) {
			SkyDrmSDKMgr::Instance()->Popup_Message(ERROR_MSG_NO_SAVE_AS_RIGHT, nxl);
			//MessageBoxW(GetForegroundWindow(), ERROR_MSG_NO_SAVE_AS_PDF_RIGHT, ERROR_MSG_CAPTION, MB_OK | MB_ICONWARNING);
			hr = REGDB_E_CLASSNOTREG;
			return hr;
		}

#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
		uint32_t dirstatus;
		std::wstring filetags;
		std::wstring wstrPath = nxl;
		std::wstring directory = L"";
		directory = wstrPath.substr(0, wstrPath.find_last_of(L"\\/"));
		if (CommonFunction::IsSanctuaryFolder(directory, &dirstatus, filetags)) {
			SkyDrmSDKMgr::Instance()->Popup_Message(ERROR_MSG_NO_SAVE_AS_RIGHT, nxl);
			hr = REGDB_E_CLASSNOTREG;
			return hr;
		}
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR

	}
	hr= Hooked_CoCreateInstance_Next(rclsid, pUnkOuter, dwClsContext, riid, ppv);
	return hr;
}

int __stdcall nxrmExt2::Hooked_MessageBoxW_Instance(
	HWND    hWnd,
	LPCWSTR lpText,
	LPCWSTR lpCaption,
	UINT    uType) 
{
	int res;
	if ((0 == _wcsicmp(lpCaption, L"Acrobat PDFMaker")) && (0== _wcsicmp(lpText, L"Unable to find \"Adobe PDF\" resource files.\n\n\"Acrobat PDFMaker\"\n\nYou must have Administrator priveleges to install these files. Please contact your local system administrator."))) {
		//SetLastError();
		return 0;
	}

	if (0 == _wcsicmp(lpCaption, L"Acrobat PDFMaker")) {
		//SetLastError();
		return 0;
	}
	res=Hooked_MessageBoxW_Next(hWnd, lpText, lpCaption, uType);
	return res;
}

int __stdcall nxrmExt2::Hooked_MessageBoxA_Instance(
	HWND   hWnd,
	LPCSTR lpText,
	LPCSTR lpCaption,
	UINT   uType)
{
	int res;
	if ((0==_stricmp(lpCaption, "Acrobat PDFMaker")) && (0== _stricmp(lpText, "Unable to find \"Adobe PDF\" resource files.\n\n\"Acrobat PDFMaker\"\n\nYou must have Administrator priveleges to install these files. Please contact your local system administrator."))) {
		return 0;
	}

	if (0 == _stricmp(lpCaption, "Acrobat PDFMaker")) {
		//SetLastError();
		return 0;
	}
	res = Hooked_MessageBoxA_Next(hWnd, lpText, lpCaption, uType);
	return res;
}

bool nxrmExt2::is_valid_plugin_config() {
	if (!g_nxrmExt2) {
		return false;
	}
	if (!g_nxrmExt2->m_pOfficeEventSink) {
		return false;
	}
	return true;
}

HRESULT  nxrmExt2::GetActiveRights(ULONGLONG &ActiveRights)
{
	ActiveRights = BUILTIN_RIGHT_ALL;
	if (m_pOfficeEventSink)
	{
		return m_pOfficeEventSink->GetActiveRights(ActiveRights);
	}
	return E_FAIL;
}