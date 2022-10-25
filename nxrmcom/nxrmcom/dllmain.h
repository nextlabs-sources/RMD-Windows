// dllmain.h : Declaration of module class.

class CnxrmcomModule : public ATL::CAtlDllModuleT< CnxrmcomModule >
{
public :
	DECLARE_LIBID(LIBID_nxrmcomLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_NXRMCOM, "{5e731a5d-713f-477c-bbe6-941cf41ef64d}")
};

extern class CnxrmcomModule _AtlModule;
