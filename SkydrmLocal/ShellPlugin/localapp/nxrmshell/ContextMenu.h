#pragma once

class IRmCtxMenu : public IContextMenu, public IShellExtInit
{
public:
	IRmCtxMenu();
	~IRmCtxMenu();

	STDMETHODIMP QueryInterface(REFIID riid, void **ppobj);

	STDMETHODIMP_(ULONG) AddRef();

	STDMETHODIMP_(ULONG) Release();

	STDMETHODIMP QueryContextMenu(  HMENU hmenu, UINT indexMenu,UINT idCmdFirst, UINT idCmdLast,UINT uFlags);

	STDMETHODIMP InvokeCommand( CMINVOKECOMMANDINFO *pici);

	STDMETHODIMP GetCommandString( UINT_PTR idCmd,  UINT uType,  UINT *pReserved,CHAR *pszName,UINT cchMax);

	STDMETHODIMP Initialize(PCIDLIST_ABSOLUTE pidlFolder,  IDataObject *pdtobj,  HKEY hkeyProgID);

private:
	HRESULT InsertNormalFileMenu(_In_  HMENU hmenu,
		_In_  UINT indexMenu,
		_In_  UINT idCmdFirst,
		_In_  UINT idCmdLast,
		_In_  UINT uFlags);
	HRESULT InsertNxlFileMenu(_In_  HMENU hmenu,
		_In_  UINT indexMenu,
		_In_  UINT idCmdFirst,
		_In_  UINT idCmdLast,
		_In_  UINT uFlags);

protected:
	void InitUIResource();
	void DeinitUIResource();

    // Process Command
	void OnShareAProtectedFile( );
    void OnCreateAProtectedFile();
    void OnGotoSkyDRMLocal();
    void OnOpenSkyDRMWeb();

	// Process Nxl Command
	void OnNxlAddFileToProject();
	void OnNxlShare();
	void OnNxlExtractContent();
	void OnNxlModifyRights();
	void OnNxlEditFile();
	void OnNxlViewFile();
	void OnNxlViewFileInfo();
	void OnNxlDelete();
	void OnNxlOpenSkyDRMLocal();
	void OnNxlOpenSkyDRMWeb();

private:

	void SendCmdToSkyDRMApp(std::wstring cmdParam);

	void SendCmdToViewerApp(std::wstring cmdParam);

private:
    ULONG					    m_uRefCount;
	std::wregex				m_regBypassedFilter;
	std::wregex				m_regCtxMenuFilter;

	std::wstring            m_strSelectedFile;
	BOOL					m_bIsNXLFile;
	std::wstring            m_OfficeTagValue;

	std::wstring            m_strNxlFileFingerPrint;

	std::wstring			m_SkyDRMAppPath;

	std::wstring			m_ViewerAppPath;
private: // UI Sections
	// Menu UI
	HBITMAP					    m_hMenuBitmap;
	
	// Normoal File Menu item UI
	HBITMAP						m_hShareBitmap;

	HBITMAP						m_hCreateBitmap;

	HBITMAP						m_hGoLocalBitmap;

	HBITMAP						m_hGoWebBitmap;
	
	// Nxl File Menu item UI
	HBITMAP m_hNxlAddFileToProjectBitmap;
	HBITMAP m_hNxlAddFileToProjectBitmap_Gray;

	HBITMAP m_hNxlShareBitmap;
	HBITMAP m_hNxlShareBitmap_Gray;

	HBITMAP m_hNxlExtractContentBitmap;
	HBITMAP m_hNxlExtractContentBitmap_Gray;

	HBITMAP m_hNxlModifyRightsBitmap;
	HBITMAP m_hNxlModifyRightsBitmap_Gray;

	HBITMAP m_hNxlEditFileBitmap;

	HBITMAP m_hNxlViewFileBitmap;
	HBITMAP m_hNxlViewFileBitmap_Gray;

	HBITMAP m_hNxlViewFileInfoBitmap;
	HBITMAP m_hNxlViewFileInfoBitmap_Gray;

	HBITMAP m_hNxlDeleteBitmap;

	HBITMAP m_hNxlOpenSkyDRMLocalBitmap;

	HBITMAP m_hNxlOpenSkyDRMComBitmap;
};

