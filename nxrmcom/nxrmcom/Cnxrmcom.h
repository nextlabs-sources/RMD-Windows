// Cnxrmcom.h : Declaration of the CCnxrmcom

#pragma once
#include "resource.h"       // main symbols


#include "nxrmcom_i.h"
#include "SDLAPI.h"



#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

using namespace ATL;


// CCnxrmcom

class ATL_NO_VTABLE CCnxrmcom :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CCnxrmcom, &CLSID_Cnxrmcom>,
	public IDispatchImpl<ICnxrmcom, &IID_ICnxrmcom, &LIBID_nxrmcomLib, /*wMajor =*/ 1, /*wMinor =*/ 0>
{
public:
	CCnxrmcom()
	{
		m_pRmcInstance = NULL;
		m_pRmTenant = NULL;
		m_pRmUser = NULL;
	}
	~CCnxrmcom()
	{
		if (NULL != m_pRmcInstance) {
			delete m_pRmcInstance;
			m_pRmcInstance = NULL;
		}
	}

DECLARE_REGISTRY_RESOURCEID(106)


BEGIN_COM_MAP(CCnxrmcom)
	COM_INTERFACE_ENTRY(ICnxrmcom)
	COM_INTERFACE_ENTRY(IDispatch)
END_COM_MAP()



	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

public:

	// Inherited via IDispatchImpl
	virtual HRESULT __stdcall GetRights(BSTR NxlFilePath, BSTR * Rights) override;

	virtual HRESULT __stdcall ProtectFile(BSTR PlainFilePath, BSTR Tags, BSTR DestinationFolder, BSTR * NxlFilePath) override;

	virtual HRESULT __stdcall ViewFile(BSTR NxlFilePath, int Options, BSTR* OutputJson) override;

	virtual HRESULT __stdcall IsUserLogined(BSTR* OutputJson)override;

	virtual HRESULT __stdcall LockFileSync(BSTR NxlFilePath, BSTR *OutputJson) override;

	virtual HRESULT __stdcall ResumeFileSync( BSTR NxlFilePath, BSTR *OutputJson) override;

	virtual HRESULT __stdcall AddRPMDir(BSTR Path, unsigned int Option, BSTR *OutputJson)override;

	virtual HRESULT __stdcall RemoveRPMDir(BSTR Path, BSTR *OutputJson)override;

	virtual HRESULT __stdcall RPMAddTrustedProcess(unsigned long ProcessId, BSTR Security, BSTR* OutputJson)override;

	virtual HRESULT __stdcall RPMRemoveTrustedProcess(unsigned long ProcessId, BSTR Security, BSTR* OutputJson)override;

	virtual HRESULT __stdcall RPMRegisterApp(BSTR AppPath, BSTR Security, BSTR* OutputJson)override;

	virtual HRESULT __stdcall RPMNotifyRMXStatus(boolean Running, BSTR Security, BSTR* OutputJson)override;

protected:

	SDWLResult __stdcall GetCurrentLoggedInUser();
	SDWLResult __stdcall ViewFileByRMDViewer(std::wstring filePath);
	SDWLResult __stdcall ViewFileByNative(std::wstring filePath);
	std::wstring __stdcall BuildOutputJson(int code, const std::wstring &value, std::wstring msg);
	SDWLResult __stdcall InnerProtectFile(const std::wstring &filepath, std::wstring& newcreatedfilePath, const std::string& tags);
	SDWLResult __stdcall RPMCopyFile(const std::wstring &filepath, std::wstring& destpath, const std::wstring hiddenrpmfolder);
	SDWLResult __stdcall ProjectCreateTempFile(const std::wstring& projectId, const std::wstring& pathId, std::wstring& tmpFilePath);
    std::wstring __stdcall GetTempDirectory();


protected:
	ISDRmcInstance* m_pRmcInstance;
	ISDRmTenant *m_pRmTenant;
	ISDRmUser *m_pRmUser;
	int	m_Ref;

};

OBJECT_ENTRY_AUTO(__uuidof(Cnxrmcom), CCnxrmcom)
