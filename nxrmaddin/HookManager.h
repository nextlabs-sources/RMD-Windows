#pragma once
#include <list>
#include "CriticalSectionLock.h"

typedef HRESULT(WINAPI *RegisterDragDrop_Fun)(IN HWND hwnd, IN LPDROPTARGET pDropTarget);
typedef HRESULT(WINAPI *DoDragDrop_Fun)(IN LPDATAOBJECT pDataObj, IN LPDROPSOURCE pDropSource, IN DWORD dwOKEffects, OUT LPDWORD pdwEffect);
typedef HANDLE(WINAPI *GetClipboardData_Fun)(_In_ UINT uFormat);
typedef HANDLE(WINAPI *SetClipboardData_Fun)(UINT   uFormat,HANDLE hMem);

// by osmond. 02/17/2017
typedef HRESULT (WINAPI *OleCreateFromFile_Fun)(
	IN REFCLSID        rclsid,
	IN LPCOLESTR       lpszFileName,
	IN REFIID          riid,
	IN DWORD           renderopt,
	IN LPFORMATETC     lpFormatEtc,
	IN LPOLECLIENTSITE pClientSite,
	IN LPSTORAGE       pStg,
	OUT LPVOID* ppvObj
);

typedef HRESULT(WINAPI* OleCreateLinkToFile_Fun)(
	IN LPCOLESTR       lpszFileName,
	IN REFIID          riid,
	IN DWORD           renderopt,
	IN LPFORMATETC     lpFormatEtc,
	IN LPOLECLIENTSITE pClientSite,
	IN LPSTORAGE       pStg,
	OUT LPVOID* ppvObj
);

typedef HRESULT (WINAPI* OleCreateLink_Fun)(
	IN LPMONIKER       pmkLinkSrc,
	IN REFIID          riid,
	IN DWORD           renderopt,
	IN LPFORMATETC     lpFormatEtc,
	IN LPOLECLIENTSITE pClientSite,
	IN LPSTORAGE       pStg,
	OUT LPVOID* ppvObj
);

typedef int (WINAPI* StartDocW_Fun)(HDC hdc, const DOCINFOW* lpdi);

typedef int (WINAPI* EndDoc_Fun)(HDC hdc);

typedef int (WINAPI* EndPage_Fun)(HDC hdc);

typedef int (WINAPI* StartPage_Fun)(HDC hdc);

typedef HDC (WINAPI* CreateDCW_Fun)( LPCWSTR pwszDriver, 
	LPCWSTR pwszDevice, 
	LPCWSTR pszPort, const DEVMODEW* pdm);

// for ole32!CoCreateInstance
// for combase!CoCreateInstance
typedef HRESULT(WINAPI* Hooked_CoCreateInstance_Signature)(
	IN REFCLSID		rclsid,
	IN LPUNKNOWN	pUnkOuter,
	IN DWORD		dwClsContext,
	IN REFIID		riid,
	OUT LPVOID FAR* ppv);
 
typedef int (WINAPI* Hooked_MessageBoxW)(
	HWND    hWnd,
	LPCWSTR lpText,
	LPCWSTR lpCaption,
	UINT    uType
);

typedef int (WINAPI* Hooked_MessageBoxA)(
	HWND   hWnd,
	LPCSTR lpText,
	LPCSTR lpCaption,
	UINT   uType
);

struct HookItem
{
	void** OldFunction;
	void*  NewFunction;
	const char* szDllName;
	const char* szFunName;
	long   lHookResult;
};

class HookManager 
{

private:
	HookManager();
	HookManager(const HookManager&) {}

public:
	static HookManager* Instance()
	{
		static HookManager hookMgr;
		return &hookMgr;
	}
	bool AddHookItem(void** OldFunction, void* newFunction, const char* szDllName, const char* szFunName);

	void hook();
	void unhook();

private:
	const HookItem* FindHookItem(const char* szDllName, const char* szFunName);
	void InsertHookItem(HookItem* item);


private:
	CRITICAL_SECTION m_csHookItem;
	std::list<HookItem*>  m_lstHookItem;

	//dll name and function
};
