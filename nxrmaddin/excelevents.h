#pragma once

#include "IEventBase.h"
#include "Monitor.h"

#define EXCEL_LCID_ENG 0x0409

class ExcelEventListener : public IDispatch, public IEventBase
{
protected:
	//class used to monitor workbook closed event.
	//source code:https://gist.github.com/jmangelo/301884
	struct CloseRequestInfo
	{
		CloseRequestInfo(const wchar_t* name, VARIANT_BOOL varSaved, long count)
		{
			WorkbookName = name;
			WorkbookSaved = varSaved;
			WorkbookCount = count;
		}
		std::wstring WorkbookName;
		VARIANT_BOOL WorkbookSaved;
		long WorkbookCount;
	};

public:
	virtual HRESULT __stdcall GetActiveDoc(std::wstring& ActiveDoc) override {
		DEVLOG_FUN;
		ActiveDoc = m_ActiveDoc;
		return S_OK;
	}

	virtual HRESULT __stdcall GetActiveRights(ULONGLONG& ActiveRights)override {
		DEVLOG_FUN;
		ActiveRights = m_ActiveDocRights;
		return S_OK;
	}

public:
	ExcelEventListener();

	ExcelEventListener(IDispatch *pRibbonUI, BSTR ActiveDoc, ULONGLONG &ActiveRights);
	
	~ExcelEventListener();

	HRESULT STDMETHODCALLTYPE QueryInterface( REFIID riid, void **ppvObject);

	ULONG STDMETHODCALLTYPE AddRef(void);

	ULONG STDMETHODCALLTYPE Release(void);

	virtual HRESULT STDMETHODCALLTYPE GetTypeInfoCount(UINT* pctinfo) { return E_NOTIMPL; }

	virtual HRESULT STDMETHODCALLTYPE GetTypeInfo(UINT iTInfo, LCID lcid, ITypeInfo** ppTInfo) { return E_NOTIMPL; }

	virtual HRESULT STDMETHODCALLTYPE GetIDsOfNames(REFIID riid,LPOLESTR* rgszNames,UINT cNames,LCID lcid,DISPID* rgDispId) { return E_NOTIMPL; }

	HRESULT STDMETHODCALLTYPE Invoke( DISPID dispIdMember,REFIID riid,LCID lcid,WORD wFlags,DISPPARAMS *pDispParams,
	VARIANT *pVarResult,EXCEPINFO *pExcepInfo,UINT *puArgErr);

	STDMETHODIMP WorkbookOpen(Excel2016::_Workbook* Wb);

	STDMETHODIMP WorkbookBeforeClose (Excel2016::_Workbook * Wb,VARIANT_BOOL * Cancel );

	STDMETHODIMP WorkbookBeforePrint(
		/*[in]*/ Excel2016::_Workbook * Wb,
		/*[in,out]*/ VARIANT_BOOL * Cancel);


	STDMETHODIMP WindowActivate (/*[in]*/ Excel2016::_Workbook * Wb,/*[in]*/ Excel2016::Window * Wn );

	STDMETHODIMP WindowDeactivate(Excel2016::_Workbook* Wb, Excel2016::Window* Wn) { 
		::OutputDebugStringW(L"WindowDeactivate");
		return S_OK; 
	}


	STDMETHODIMP ProtectedViewWindowActivate (/*[in]*/ Excel2016::ProtectedViewWindow * Pvw );

	STDMETHODIMP ProtectedViewWindowDeactivate (/*[in]*/ Excel2016::ProtectedViewWindow * Pvw ) { 
		::OutputDebugStringW(L"ProtectedViewWindowDeactivate");
		return S_OK; 
	}


	STDMETHODIMP WorkbookBeforeSave(
		/*[in]*/ Excel2016::_Workbook * Wb,
		/*[in]*/ VARIANT_BOOL SaveAsUI,
		/*[in,out]*/ VARIANT_BOOL * Cancel);


	STDMETHODIMP WorkbookDeactive(Excel2016::_Workbook* Wb) {
		::OutputDebugStringW(L"WorkbookDeactive");
		return S_OK;
	}

	STDMETHODIMP WorkbookActive(Excel2016::_Workbook* Wb) 
	{ 
		::OutputDebugStringW(L"WorkbookActive");
		return S_OK; 
	}

	STDMETHODIMP WorkbookAfterSave(Excel2016::_Workbook* Wb, VARIANT_BOOL Success);

	STDMETHODIMP SheetActivate(Excel2016::_Worksheet* sh);

	STDMETHODIMP WorkbookNewSheet(Excel2016::_Workbook* Wb, Excel2016::_Worksheet* Sh);

	STDMETHODIMP SheetBeforeDelete(Excel2016::_Worksheet* Sh);

	STDMETHODIMP SheetDeactivate(Excel2016::_Worksheet* Sh);

	void Init(IDispatch* pApplication, IDispatch *pRibbonUI, BSTR ActiveDoc, ULONGLONG ActiveRights)
	{
		m_pApplication = pApplication;
		m_pRibbonUI = pRibbonUI;

		if (ActiveDoc){
			m_ActiveDoc = ActiveDoc;
		}
		
		m_ActiveDocRights = ActiveRights;

		InvalidMsoControls();
	}

	// got additional request in it, for read_only doc, remove edit rights
	HRESULT CheckWorkbookRight(Excel2016::_WorkbookPtr Wb, ULONGLONG& RightMask);

	void ShowViewOverlayAuto_SpecialCase_ForBugFix(BSTR path);

private:
	void InvalidMsoControls(void);
	static BOOL CALLBACK DlgProcSaveTip(HWND hwndDlg, UINT message, WPARAM wParam, LPARAM lParam);

private:// for anti-auto_recovery
	void Impl_Feature_AntiAutoRecovery();
	bool GetAutoRecoveryDir(std::wstring& outPath);
	//void SetupMonitorAutoRecoveryDir();
	void DisableAutoRecoverFeature();
	
private:
	ULONG				m_uRefCount;

	IDispatch			*m_pRibbonUI;
	Excel2016::_ApplicationPtr m_pApplication;
	//NX::utility::CRwLock	m_ActiveDocLock;

	std::wstring		m_ActiveDoc;

	ULONGLONG			m_ActiveDocRights;

	ULONG				m_InvalidCount;

	FILECHANGE_STATE  m_workbookChangeState;

	DirMonitor    m_AutoRecoveryDirMonitor;

	typedef enum _ExcelAppEventId {
		WorkbookAfterSave_Id = 0x00000b5f,
		WorkbookOpen_Id = 0x0000061F,
		WorkbookActive_Id = 0x00000620,
		WorkbookDeactive_Id = 0x00000621,
		WorkbookBeforeClose_Id = 0x00000622,

		WorkbookBeforeSave_Id = 0x00000623,

		WorkbookBeforePrint_Id = 0x00000624,


		WindowActivate_Id = 0x00000614,

		WindowDeactivate_Id = 0x00000615,

		ProtectedViewWindowActivate_Id = 0x00000b5d,

		ProtectedViewWindowDeactivate_Id = 0x00000b5e,

		SheetActivate_id = 0x00000619,

		WorkbookNewSheet_id = 0x00000625,

		SheetBeforeDelete_id = 0x00000c07,

		SheetDeactivate_id = 0x0000061a,

		//SheetChange_id = 0x0000061c,
	}ExcelAppEventId;
};