// nxrmctxmenu.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "nxrmshell.h"
#include "ContextMenu.h"
#include "nxrmshellglobal.h"
#include "helper.h"

#ifdef __cplusplus
extern "C" {
#endif
	SHELL_GLOBAL_DATA Global;
	BOOL init_rm_section_safe(void);

#ifdef __cplusplus
}
#endif


extern "C" const GUID CLSID_IRmCtxMenu;

BOOL init_rm_section_safe(void)
{
	BOOL bRet = FALSE;

	do
	{
		if (!Global.Section)
		{
			EnterCriticalSection(&Global.SectionLock);

			//TODO initalize to connect with UI
			Global.Section = NULL;// init_transporter_client(); 

			LeaveCriticalSection(&Global.SectionLock);
		}

		if (!Global.Section)
		{
			break;
		}
		//TODO
		bRet = FALSE;//is_transporter_enabled(Global.Section);

	} while (FALSE);

	return bRet;
}


BOOL APIENTRY DllMain( HMODULE hModule,
					  DWORD  ul_reason_for_call,
					  LPVOID lpReserved
					  )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		{
		
			Global.hModule = hModule;

			InitializeCriticalSection(&Global.SectionLock);

			DisableThreadLibraryCalls(hModule);

			break;
		}
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:
		{
			DeleteCriticalSection(&Global.SectionLock);
		}

		break;
	}
	return TRUE;
}


Inxrmshell::Inxrmshell()
{
	m_uRefCount		= 1;
	m_uLockCount	= 0;
}

Inxrmshell::~Inxrmshell()
{
}

STDMETHODIMP Inxrmshell::QueryInterface(REFIID riid, void **ppobj)
{
	HRESULT hRet = S_OK;

	void *punk = NULL;

	*ppobj = NULL;

	do 
	{
		if (IID_IClassFactory == riid)
		{
			punk = (ICallFactory *)this;
		}
		else if (IID_IUnknown == riid)
		{
			punk = (IUnknown *)this;
		}
		else
		{
			hRet = E_NOINTERFACE;
			break;
		}

		AddRef();

		*ppobj = punk;

	} while (FALSE);

	return hRet;
}

STDMETHODIMP Inxrmshell::CreateInstance(IUnknown * pUnkOuter, REFIID riid, void ** ppvObject)
{
	HRESULT hr = S_OK;
	IRmCtxMenu *p = NULL;
	do 
	{
		if(pUnkOuter)
		{
			*ppvObject = NULL;
			hr = CLASS_E_NOAGGREGATION;
			break;
		}

		if (IID_IContextMenu == riid) {
			p = new IRmCtxMenu;

			if (!p)
			{
				*ppvObject = NULL;
				hr = E_OUTOFMEMORY;
				break;
			}

			InterlockedIncrement(&Global.ContextMenuInstanceCount);
			
			hr = p->QueryInterface(riid, ppvObject);
			
			p->Release();
		}

	} while (FALSE);

	return hr;
}

STDMETHODIMP Inxrmshell::LockServer(BOOL fLock)
{
	if(fLock)
	{
		m_uLockCount++;
	}
	else
	{
		if(m_uLockCount > 0)
			m_uLockCount--;
	}

	return m_uLockCount;	
}

STDMETHODIMP_(ULONG) Inxrmshell::AddRef()
{
	m_uRefCount++;

	return m_uRefCount;
}

STDMETHODIMP_(ULONG) Inxrmshell::Release()
{
	ULONG uCount = 0;

	if(m_uRefCount)
		m_uRefCount--;

	uCount = m_uRefCount;

	if(!uCount && (m_uLockCount == 0))
	{
		delete this;
		InterlockedDecrement(&Global.nxrmshellInstanceCount);
	}

	return uCount;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
	HRESULT  hr = E_OUTOFMEMORY;

	Inxrmshell *Inxrmshellnstance = NULL;

	if(IsEqualCLSID(rclsid, CLSID_IRmCtxMenu))
	{
		Inxrmshellnstance = new Inxrmshell;

		if(Inxrmshellnstance)
		{
			InterlockedIncrement(&Global.nxrmshellInstanceCount);

			hr = Inxrmshellnstance->QueryInterface(riid,ppv);

			Inxrmshellnstance->Release();

		}
	}
	else
	{
		hr = CLASS_E_CLASSNOTAVAILABLE;
	}

	return(hr);
}

STDAPI DllCanUnloadNow(void)
{
	if (Global.nxrmshellInstanceCount == 0 && Global.ContextMenuInstanceCount == 0)
	{
		return S_OK;
	}
	else
	{
		return S_FALSE;
	}
}

//
//  modified by osmond, 
//		only support shell context menu plugin, remove other features
//		only support admin mode, remove others
//
STDAPI DllRegisterServer(void)
{
	HRESULT nRet = S_OK;

	WCHAR module_path[MAX_PATH] = { 0 };

	if (!GetModuleFileNameW(Global.hModule,module_path,sizeof(module_path) / sizeof(WCHAR))){
		nRet = E_UNEXPECTED;
		return nRet;
	}

	nRet = create_key_with_default_value(HKEY_CLASSES_ROOT,L"CLSID",NXRMSHELL_INSTALL_GUID_KEY,NXRMSHELL_NAME);
	if(S_OK != nRet) {
		return nRet;
	}
	
	nRet = create_key_with_default_value(HKEY_CLASSES_ROOT,NXRMSHELL_INSTALL_CLSID_KEY,L"InprocServer32",module_path);
	if(S_OK != nRet){
		return nRet;
	}

	nRet = set_value_content(HKEY_CLASSES_ROOT,NXRMSHELL_INSTALL_INPROCSERVER32_KEY,L"ThreadingModel",L"Apartment");
	if(S_OK != nRet){
		return nRet;
	}
	// reg into *\\shellex\\ContextMenuHandlers
	nRet = create_key_with_default_value(HKEY_CLASSES_ROOT,NXRMSHELL_INSTALL_CONTEXTMENUHANDLERS_KEY,
		NXRMSHELL_NAME,NXRMSHELL_INSTALL_GUID_KEY);

	if(S_OK != nRet){
		return nRet;
	}

	// tell shell to refresh
	::SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, NULL, NULL);
	return S_OK;
}

STDAPI DllUnregisterServer(void)
{
	delete_key(HKEY_CLASSES_ROOT, NXRMSHELL_INSTALL_CLSID_KEY, L"InprocServer32");
	delete_key(HKEY_CLASSES_ROOT, L"CLSID", NXRMSHELL_INSTALL_GUID_KEY);
	delete_key(HKEY_LOCAL_MACHINE, NXL_FILETYPE_APPKEY_APPROVED_EXTENTION, NXRMSHELL_INSTALL_GUID_KEY);	
	delete_key(HKEY_CLASSES_ROOT, NXRMSHELL_INSTALL_CONTEXTMENUHANDLERS_KEY, NXRMSHELL_NAME);
	delete_key(HKEY_CURRENT_USER, NXRMSHELL_INSTALL_CLSID_KEY_CU, L"InprocServer32");
	delete_key(HKEY_CURRENT_USER, L"Software\\Classes\\CLSID", NXRMSHELL_INSTALL_GUID_KEY);
	delete_key(HKEY_CURRENT_USER, NXRMSHELL_INSTALL_CONTEXTMENUHANDLERS_KEY_CU, NXRMSHELL_NAME);

	
	// tell shell to refresh
	::SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, NULL, NULL);
	return S_OK;
}



