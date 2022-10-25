#pragma once


#ifdef NXRMSHELL_EXPORTS
#define NXRMSHELL_API __declspec(dllexport)
#else
#define NXRMSHELL_API __declspec(dllimport)
#endif


// {85266F9F-9F6D-4E53-B9B8-8B4452443C99}
//static const GUID << name >> =
//{ 0X85266F9F, 0X9F6D, 0X4E53,{ 0XB9, 0XB8, 0X8B, 0X44, 0X52, 0X44, 0X3C, 0X99 } };

#define NXRMSHELL_NAME								L"SkyDRM"
#define NXRMSHELL_INSTALL_GUID_KEY					L"{85266F9F-9F6D-4E53-B9B8-8B4452443C99}"
#define NXRMSHELL_INSTALL_CLSID_KEY					L"CLSID\\{85266F9F-9F6D-4E53-B9B8-8B4452443C99}"
#define NXRMSHELL_INSTALL_INPROCSERVER32_KEY		L"CLSID\\{85266F9F-9F6D-4E53-B9B8-8B4452443C99}\\InprocServer32"
#define NXRMSHELL_INSTALL_CONTEXTMENUHANDLERS_KEY	L"*\\shellex\\ContextMenuHandlers"


#define NXRMSHELL_INSTALL_CLSID_KEY_CU					L"Software\\Classes\\CLSID\\{85266F9F-9F6D-4E53-B9B8-8B4452443C99}"
#define NXRMSHELL_INSTALL_INPROCSERVER32_KEY_CU			L"Software\\Classes\\CLSID\\{85266F9F-9F6D-4E53-B9B8-8B4452443C99}\\InprocServer32"
#define NXRMSHELL_INSTALL_CLASSES_KEY_CU				L"Software\\Classes"
#define NXRMSHELL_INSTALL_STAR_KEY_CU					L"Software\\Classes\\*"
#define NXRMSHELL_INSTALL_SHELLEX_KEY_CU				L"Software\\Classes\\*\\shellex"
#define NXRMSHELL_INSTALL_CONTEXTMENUHANDLERS_KEY_CU	L"Software\\Classes\\*\\shellex\\ContextMenuHandlers"

// File type section used for .nxl extention register
#define NXL_FILETYPE_KEY						L".nxl"
#define NXL_FILETYPE_APPKEY						L"NextLabs.Handler.1"
#define NXL_FILETYPE_APPKEY_CMD					L"NextLabs.Handler.1\\shell\\open\\command"
#define NXL_FILETYPE_APPKEY_DEFAULT_ICON		L"NextLabs.Handler.1\\DefaultIcon"
#define NXL_FILETYPE_APPKEY_SEX_ICONHANDLER     L"Nextlabs.Handler.1\\Shellex\\IconHandler"
#define NXL_FILETYPE_APPKEY_APPROVED_EXTENTION	L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved"
#define NXL_FILETYPE_APPKEY_APPROVED_CONTENT	L"Nextlabs SkyDRM Icon Handler"

// COM Required Interfaces
STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv);
STDAPI DllCanUnloadNow(void);
STDAPI DllRegisterServer(void);
STDAPI DllUnregisterServer(void);


class Inxrmshell : public IClassFactory
{
public:
	Inxrmshell();
	~Inxrmshell();

	STDMETHODIMP QueryInterface(REFIID riid, void **ppobj);

	STDMETHODIMP_(ULONG) AddRef();

	STDMETHODIMP_(ULONG) Release();

	STDMETHODIMP CreateInstance(IUnknown * pUnkOuter, REFIID riid, void ** ppvObject);

	STDMETHODIMP LockServer(BOOL fLock);

private:
	ULONG				m_uRefCount;
	ULONG				m_uLockCount;
};

