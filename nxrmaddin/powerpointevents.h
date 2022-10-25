#pragma once

#include "IEventBase.h"
#include "Monitor.h"
#include "CommonFunction.h"


class PowerPointEventListener : public PowerPoint2016::EApplication, public IEventBase
{
public:
	PowerPointEventListener();
	PowerPointEventListener(IDispatch* pRibbonUI, BSTR ActiveDoc, ULONGLONG& ActiveRights);
	~PowerPointEventListener();
public: // impl IUnknown
	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
	ULONG STDMETHODCALLTYPE AddRef(void);
	ULONG STDMETHODCALLTYPE Release(void);
public:	// impl IDispatch
	HRESULT STDMETHODCALLTYPE GetTypeInfoCount(UINT* pctinfo) { return E_NOTIMPL; }
	HRESULT STDMETHODCALLTYPE GetTypeInfo(UINT iTInfo, LCID lcid, ITypeInfo** ppTInfo){ return E_NOTIMPL; }
	HRESULT STDMETHODCALLTYPE GetIDsOfNames( REFIID riid,LPOLESTR *rgszNames,UINT cNames,LCID lcid,DISPID *rgDispId){ return E_NOTIMPL; }
	HRESULT STDMETHODCALLTYPE Invoke( DISPID dispIdMember,REFIID riid,LCID lcid,WORD wFlags,DISPPARAMS *pDispParams,VARIANT *pVarResult,EXCEPINFO *pExcepInfo,UINT *puArgErr);
public: // impl IEventBase
	virtual HRESULT __stdcall GetActiveDoc(std::wstring& ActiveDoc) override { 
		DEVLOG_FUN;
		ActiveDoc = m_ActiveDoc; 
		return S_OK; 
	}

	virtual HRESULT __stdcall GetActiveRights(ULONGLONG& ActiveRights) override {
		DEVLOG_FUN;
		ActiveRights = m_ActiveDocRights; 
		return S_OK;
	}

public: // response office events
	HRESULT STDMETHODCALLTYPE WindowSelectionChange( struct PowerPoint2016::Selection * Sel) { return S_OK; }
	HRESULT STDMETHODCALLTYPE WindowBeforeRightClick( struct PowerPoint2016::Selection * Sel, VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT STDMETHODCALLTYPE WindowBeforeDoubleClick( struct PowerPoint2016::Selection * Sel, VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT STDMETHODCALLTYPE PresentationClose(struct PowerPoint2016::_Presentation * Pres);
	HRESULT STDMETHODCALLTYPE PresentationSave(struct PowerPoint2016::_Presentation* Pres) { return S_OK; }
	HRESULT STDMETHODCALLTYPE PresentationOpen(struct PowerPoint2016::_Presentation * Pres);
	HRESULT STDMETHODCALLTYPE NewPresentation( struct PowerPoint2016::_Presentation * Pres) { return S_OK; }
	HRESULT STDMETHODCALLTYPE PresentationNewSlide( struct PowerPoint2016::_Slide * Sld) { return S_OK; }
	HRESULT STDMETHODCALLTYPE WindowActivate(struct PowerPoint2016::_Presentation * Pres,struct PowerPoint2016::DocumentWindow * Wn);
	HRESULT STDMETHODCALLTYPE WindowDeactivate(struct PowerPoint2016::_Presentation* Pres, struct PowerPoint2016::DocumentWindow* Wn) {
		return S_OK;
	}
	HRESULT STDMETHODCALLTYPE SlideShowBegin(struct PowerPoint2016::SlideShowWindow * Wn);
	HRESULT STDMETHODCALLTYPE SlideShowNextBuild( struct PowerPoint2016::SlideShowWindow * Wn) { return S_OK; }
	HRESULT STDMETHODCALLTYPE SlideShowNextSlide( struct PowerPoint2016::SlideShowWindow * Wn) { return S_OK; }
	HRESULT STDMETHODCALLTYPE SlideShowEnd(struct PowerPoint2016::_Presentation* Pres) { return S_OK; }
	HRESULT STDMETHODCALLTYPE PresentationPrint(struct PowerPoint2016::_Presentation * Pres) { return S_OK; }
	HRESULT STDMETHODCALLTYPE SlideSelectionChanged(struct PowerPoint2016::SlideRange * SldRange) { return S_OK; }
	HRESULT STDMETHODCALLTYPE ColorSchemeChanged(struct PowerPoint2016::SlideRange * SldRange) { return S_OK; }
	HRESULT STDMETHODCALLTYPE PresentationBeforeSave(struct PowerPoint2016::_Presentation * Pres,VARIANT_BOOL * Cancel);
	HRESULT STDMETHODCALLTYPE SlideShowNextClick(struct PowerPoint2016::SlideShowWindow * Wn,struct PowerPoint2016::Effect * nEffect) { return S_OK; }
	HRESULT STDMETHODCALLTYPE AfterNewPresentation(struct PowerPoint2016::_Presentation * Pres) { return S_OK; }
	HRESULT STDMETHODCALLTYPE AfterPresentationOpen(struct PowerPoint2016::_Presentation * Pres) { return S_OK; }
	HRESULT STDMETHODCALLTYPE PresentationSync(struct PowerPoint2016::_Presentation * Pres,enum Office2016::MsoSyncEventType SyncEventType) { return S_OK; }
	HRESULT STDMETHODCALLTYPE SlideShowOnNext(struct PowerPoint2016::SlideShowWindow * Wn) { return S_OK; }
	HRESULT STDMETHODCALLTYPE SlideShowOnPrevious(struct PowerPoint2016::SlideShowWindow * Wn) { return S_OK; }
	HRESULT STDMETHODCALLTYPE PresentationBeforeClose(struct PowerPoint2016::_Presentation * Pres,VARIANT_BOOL * Cancel);
	HRESULT STDMETHODCALLTYPE ProtectedViewWindowOpen(struct PowerPoint2016::ProtectedViewWindow * ProtViewWindow) { return S_OK; }
	HRESULT STDMETHODCALLTYPE ProtectedViewWindowBeforeEdit(struct PowerPoint2016::ProtectedViewWindow * ProtViewWindow,VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT STDMETHODCALLTYPE ProtectedViewWindowBeforeClose(struct PowerPoint2016::ProtectedViewWindow * ProtViewWindow,enum PowerPoint2016::PpProtectedViewCloseReason ProtectedViewCloseReason,VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT STDMETHODCALLTYPE ProtectedViewWindowActivate(struct PowerPoint2016::ProtectedViewWindow * ProtViewWindow);
	HRESULT STDMETHODCALLTYPE ProtectedViewWindowDeactivate(struct PowerPoint2016::ProtectedViewWindow * ProtViewWindow) { return S_OK; }
	HRESULT STDMETHODCALLTYPE PresentationCloseFinal(struct PowerPoint2016::_Presentation * Pres);
	HRESULT STDMETHODCALLTYPE AfterDragDropOnSlide(struct PowerPoint2016::_Slide * Sld,float X,float Y) { return S_OK; }
	HRESULT STDMETHODCALLTYPE AfterShapeSizeChange(struct PowerPoint2016::Shape * shp) { return S_OK; }

	
public:	// others
	void Init(IDispatch* pApplication, IDispatch *pRibbonUI, BSTR ActiveDoc, ULONGLONG ActiveRights)
	{
		m_pApplication = pApplication;
		m_pRibbonUI = pRibbonUI;

		if (ActiveDoc) {
			m_ActiveDoc = ActiveDoc;
		}

		m_ActiveDocRights = ActiveRights;

		InvalidMsoControls();
	}

	static BOOL CALLBACK DlgProcSaveAsTip(HWND hwndDlg, UINT message, WPARAM wParam, LPARAM lParam);

	// got additional request in it, for read_only doc, remove edit rights
	HRESULT CheckPPTRight(PowerPoint2016::_PresentationPtr PPt, ULONGLONG& RightMask);
	
	//inline ULONGLONG GetMinialRights(ULONGLONG CurrentRight) {
	//	return helper_get_minimium_right(CurrentRight, m_NxlRightsContainer);
	//}
	void InvalidMsoControls(void);

private:
	void Impl_Feature_AntiAutoRecovery();
	bool GetAutoRecoveryDir(std::wstring& outPath);
	//void SetupMonitorAutoRecoveryDir();
	//void SetupMonitorAutoSaveDir();
	void DisableAutoRecoverFeature();
	
private:
	ULONG				m_uRefCount;
	IDispatch			*m_pRibbonUI;
	PowerPoint2016::_ApplicationPtr           m_pApplication;
	DirMonitor    m_AutoRecoveryDirMonitor;
	std::wstring		m_ActiveDoc;
	//std::set<ULONGLONG> m_Rights; // as a rights set, ribbon will get a minimal one
	//FileRights m_NxlRightsContainer;

	ULONGLONG			m_ActiveDocRights;
	ULONG				m_InvalidCount;
	FILECHANGE_STATE m_PresentationChanged;//monitor the change of workbook
	typedef enum _PowerPointAppEventId {
		WindowSelectionChange_Id = 2001,
		WindowBeforeRightClick_Id,
		WindowBeforeDoubleClick_Id,
		PresentationClose_Id,
		PresentationSave_Id,
		PresentationOpen_Id,
		NewPresentation_Id,
		PresentationNewSlide_Id,
		WindowActivate_Id,
		WindowDeactivate_Id,
		SlideShowBegin_Id,
		SlideShowNextBuild_Id,
		SlideShowNextSlide_Id,
		SlideShowEnd_Id,
		PresentationPrint_Id,
		SlideSelectionChanged_Id,
		ColorSchemeChanged_Id,
		PresentationBeforeSave_Id,
		SlideShowNextClick_Id,
		AfterNewPresentation_Id,
		AfterPresentationOpen_Id,
		PresentationSync_Id,
		SlideShowOnNext_Id,
		SlideShowOnPrevious_Id,
		PresentationBeforeClose_Id,
		ProtectedViewWindowOpen_Id,
		ProtectedViewWindowBeforeEdit_Id,
		ProtectedViewWindowBeforeClose_Id,
		ProtectedViewWindowActivate_Id,
		ProtectedViewWindowDeactivate_Id,
		PresentationCloseFinal_Id,
		AfterDragDropOnSlide_Id,
		AfterShapeSizeChange_Id

	}PowerPointAppEventId;
};
