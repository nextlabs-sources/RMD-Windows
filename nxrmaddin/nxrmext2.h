#pragma once
#include "import/msaddndr.tlh"
#include "import/mso2016.tlh"
#include "ribbonrights.h"
#include "IEventBase.h"
#include "HookManager.h"
#include "AntiAutoSaveRecoeryHandler.h"

typedef enum _nxrmOfficeAppType
{
	OfficeAppInvalid = 0,
	OfficeAppPowerpoint = 0x7001,
	OfficeAppWinword,
	OfficeAppExcel,
	OfficeAppOutlook
}nxrmOfficeAppType;

// nxrmExt2 will impl IDispathc for OnLoad and CheckMsoButtonStatus
#define NXRMCOREUI_ONLOAD_PROC_NAME					L"OnLoad"
#define NXRMCOREUI_CHECKMSOBUTTONSTATUS_PROC_NAME	L"CheckMsoButtonStatus"
#define NXRMCOREUI_CHECKMSOBUTTONSTATUS_ID	(0x5001)
#define NXRMCOREUI_ONLOAD_ID				(0x8001)

#define ERROR_MSG_CAPTION           L"SkyDRM Desktop"
#define ERROR_MSG_NO_SAVE_RIGHT     L"Warning: you have no permission to save the file protected by NextLabs SkyDRM. Please contact your system administrator for further help."
#define ERROR_MSG_NO_SAVE_AS_RIGHT  L"Warning: you have no permission to save the file protected by NextLabs SkyDRM. Please contact your system administrator for further help."
#define ERROR_MSG_FILE_PATH_MORE_THAN_259  L"Warning: the file path is too long. We don't support edit protected office file whose full path is more than 260 characters."
//#define ERROR_MSG_NO_SAVE_AS_PDF_RIGHT  L"Warning: you have no permission to Save as PDF the file protected by NextLabs SkyDRM. Please contact your system administrator for further help."

class nxrmExt2 : 
		public AddInDesignerObjects::_IDTExtensibility2, 
		public Office2016::IRibbonExtensibility
{
public:
	nxrmExt2();
	~nxrmExt2();

	//interface for IUnknow
	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,void **ppvObject);
	ULONG STDMETHODCALLTYPE AddRef(void);
	ULONG STDMETHODCALLTYPE Release(void);

	//interface for IDispatch
	HRESULT STDMETHODCALLTYPE GetTypeInfoCount(/* [out] */ __RPC__out UINT *pctinfo) { return E_NOTIMPL; }
	HRESULT STDMETHODCALLTYPE GetTypeInfo(UINT iTInfo, LCID lcid, ITypeInfo **ppTInfo) { return E_NOTIMPL; }
	HRESULT STDMETHODCALLTYPE GetIDsOfNames(__RPC__in REFIID riid,LPOLESTR *rgszNames,UINT cNames,LCID lcid,DISPID *rgDispId);
	HRESULT STDMETHODCALLTYPE Invoke(DISPID dispIdMember,REFIID riid,LCID lcid,WORD wFlags,DISPPARAMS *pDispParams,VARIANT *pVarResult,EXCEPINFO *pExcepInfo,UINT *puArgErr);

	//interface for _IDTExtensibility2
	HRESULT STDMETHODCALLTYPE OnConnection(IDispatch * Application,
		AddInDesignerObjects::ext_ConnectMode ConnectMode,IDispatch * AddInInst,SAFEARRAY * * custom);
	HRESULT STDMETHODCALLTYPE OnDisconnection(AddInDesignerObjects::ext_DisconnectMode RemoveMode, SAFEARRAY * * custom) {
		// should call atl::unadvice here to release office event
		HookManager::Instance()->unhook();
		return S_OK;
	}
	HRESULT STDMETHODCALLTYPE OnAddInsUpdate(SAFEARRAY * * custom) {
		return S_OK;
	}
	HRESULT STDMETHODCALLTYPE OnStartupComplete(SAFEARRAY * * custom) {
		return S_OK;
	}
	HRESULT STDMETHODCALLTYPE OnBeginShutdown(SAFEARRAY** custom);
	//interface for IRibbonExtensibility
	HRESULT STDMETHODCALLTYPE GetCustomUI(BSTR RibbonID,BSTR * RibbonXml);

public:
	//special for hooked api callbacking, when it need the judge nxl rights, others should not use it
	HRESULT GetActiveRights(ULONGLONG &ActiveRights);

protected: // this class will impl the IDispatch for 
	HRESULT STDMETHODCALLTYPE OnCheckMsoButtonStatus(Office2016::IRibbonControl *pControl,VARIANT_BOOL *pvarfEnabled);
	HRESULT STDMETHODCALLTYPE OnLoad(/*[in]*/ IDispatch	*RibbonUI);

protected:
	// plugin loaded and found word 
	HRESULT InitSetupWord(IDispatch* pApp);
	HRESULT InitSetupExcel(IDispatch* pApp);
	HRESULT InitSetupPowerPoint(IDispatch* pApp);


	HRESULT InitWordEventSink(void);
	HRESULT InitExcelEventSink(void);
	HRESULT InitPowerpointEventSink(void);


	void InitRmSDK();

public:
	inline void Register_AASR_Folder(const std::wstring& path) {
		// todo : how to get the root of the jail, and how to organize the frame for jail_path
		m_aasr_handler.register_folder(path, Get_Jail_Root_Path());
	}
protected:
	std::wstring Get_Jail_Root_Path();
	void Delete_Anti_AutoSave_AutoRecoery();
	void Wait__WorkerThreads_End();
	
private: //hook function

	//GetClipboardData 
	static GetClipboardData_Fun m_oldGetClipboardData;
	static HANDLE WINAPI Core_GetClipboardData(UINT uFormat);

	//SetClipboardData
	static SetClipboardData_Fun m_oldSetClipboardData;
	static HANDLE WINAPI Core_SetClipboardData(UINT uFormat, HANDLE hMem);

	//RegisterDragDrop
	static RegisterDragDrop_Fun m_oldRegisterDragDrop;
	static HRESULT WINAPI Core_RegisterDragDrop(IN HWND hwnd, IN LPDROPTARGET pDropTarget);

	//DoDragDrop
	static DoDragDrop_Fun m_oldDoDragDrop;
	static HRESULT WINAPI Core_DoDragDrop(IN LPDATAOBJECT pDataObj, IN LPDROPSOURCE pDropSource,
		IN DWORD dwOKEffects, OUT LPDWORD pdwEffect);

	//OleCreateFromFile
	static OleCreateFromFile_Fun m_oldOleCreateFromFile;
	static HRESULT WINAPI Core_OleCreateFormFile(
		IN REFCLSID        rclsid,
		IN LPCOLESTR       lpszFileName,
		IN REFIID          riid,
		IN DWORD           renderopt,
		IN LPFORMATETC     lpFormatEtc,
		IN LPOLECLIENTSITE pClientSite,
		IN LPSTORAGE       pStg,
		OUT LPVOID* ppvObj
	);
	//OleCreateLinkToFile
	static OleCreateLinkToFile_Fun m_oldOleCreateLinkToFile;
	static HRESULT WINAPI Core_OleCreateLinkToFile(
		IN LPCOLESTR       lpszFileName,
		IN REFIID          riid,
		IN DWORD           renderopt,
		IN LPFORMATETC     lpFormatEtc,
		IN LPOLECLIENTSITE pClientSite,
		IN LPSTORAGE       pStg,
		OUT LPVOID* ppvObj
	);

	static OleCreateLink_Fun m_oldOleCreateLink;
	static HRESULT WINAPI Core_OleCreateLink(
		IN LPMONIKER       pmkLinkSrc,
		IN REFIID          riid,
		IN DWORD           renderopt,
		IN LPFORMATETC     lpFormatEtc,
		IN LPOLECLIENTSITE pClientSite,
		IN LPSTORAGE       pStg,
		OUT LPVOID* ppvObj
		);

	// 3/32/2020, add StardDoc for Powerpoint intercepting print action
	// 05/06/2020, PrintToPDFHandler needs this
	static StartDocW_Fun m_oldStarDocW;	
	static int WINAPI Core_StartDocW(HDC hdc, const DOCINFOW* lpdi);
	
	// 4/16/2020, add EndPage for Print_Overlay supported
	static EndPage_Fun m_oldEndPage;
	static int WINAPI Core_EndPage(HDC hdc);

	static StartPage_Fun m_oldStartPage;
	static int WINAPI Core_StartPage(HDC hdc);


	static EndDoc_Fun m_oldEndDoc;
	static int WINAPI Core_EndDoc(HDC hdc);

	static CreateDCW_Fun m_oldCreateDCW;
	static HDC WINAPI Core_CreateDCW(LPCWSTR pwszDriver, LPCWSTR pwszDevice, LPCWSTR pszPort, const DEVMODEW* pdm);

	//static GetCustomUI_Fun m_oldGetCustomUI;
	//static HRESULT WINAPI Core_GetCustomUI(IN BSTR RibbonID, OUT BSTR * RibbonXml);
	//static std::string WINAPI Core_GetCustomUI(std::string ribbonId);

	static Hooked_CoCreateInstance_Signature Hooked_CoCreateInstance_Next;
	static HRESULT WINAPI Hooked_CoCreateInstance_Instance(
		IN REFCLSID		rclsid,
		IN LPUNKNOWN	pUnkOuter,
		IN DWORD		dwClsContext,
		IN REFIID		riid,
		OUT LPVOID FAR* ppv);


	static Hooked_MessageBoxW Hooked_MessageBoxW_Next;
	static int WINAPI Hooked_MessageBoxW_Instance(
		HWND    hWnd,
		LPCWSTR lpText,
		LPCWSTR lpCaption,
		UINT    uType);


	static Hooked_MessageBoxA Hooked_MessageBoxA_Next;
	static int WINAPI Hooked_MessageBoxA_Instance(
		HWND   hWnd,
		LPCSTR lpText,
		LPCSTR lpCaption,
		UINT   uType);

private:
	static bool is_valid_plugin_config();

private:
	nxrmOfficeAppType	m_OfficeAppType;
	ULONG				m_uRefCount;
	IDispatch			*m_pAppObj;
	IDispatch			*m_pRibbonUI;

	IEventBase			*m_pOfficeEventSink; //Unified sink, Separately used for word, excel, ppt
	DWORD				dwAdviseCookie;

	AntiAutoSaveRecoeryHandler  m_aasr_handler;

	// worker_threads
	// each all _beginthreadex will record its handler here
	// need locker?  -- first introducing, ignored
	std::set<HANDLE> m_worker_Threads;

	struct _case_insensitive_cmp
	{
		bool operator()(const std::wstring &str1, const std::wstring &str2) const
		{
			return _wcsicmp(str1.c_str(), str2.c_str()) < 0;
		}
	};
	std::map<std::wstring, RIBBON_ID_INFO, _case_insensitive_cmp>	m_RibbonRightsMap;

};

extern nxrmExt2* g_nxrmExt2;
