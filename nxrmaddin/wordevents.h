#pragma once

#include "IEventBase.h"
#include "Monitor.h"
#include <vector>

class WordEventListener : public Word2016::IApplicationEvents4, public IEventBase
{
private:
	struct CloseRequestInfo
	{
		CloseRequestInfo(Word2016::_DocumentPtr pDoc, const wchar_t* wszName, long count, const wchar_t* wsLabelValue)
		{
			DocumentPtr = pDoc;
			DocumentName = wszName;
			DocumentsCount = count;
			LabelValue = wsLabelValue;

			VARIANT_BOOL bRdOy = VARIANT_FALSE;
			pDoc->get_ReadOnly(&bRdOy);
			IsReadOnly = (bRdOy== VARIANT_TRUE);
		}
		Word2016::_DocumentPtr DocumentPtr;
		std::wstring DocumentName;
		long DocumentsCount;
		bool IsReadOnly; // added for bugfix, readonly file can not call RMP_Edit
		std::wstring LabelValue;
	};

public:
	WordEventListener();
	WordEventListener(IDispatch *pRibbonUI, BSTR ActiveDoc, ULONGLONG &ActiveRights);
	~WordEventListener();
public:	// impl IUnknown
	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,void **ppvObject);
	ULONG STDMETHODCALLTYPE AddRef(void);
	ULONG STDMETHODCALLTYPE Release(void);
public:	// impl IDispatch
	HRESULT STDMETHODCALLTYPE GetTypeInfoCount(UINT *pctinfo){return E_NOTIMPL;}
	HRESULT STDMETHODCALLTYPE GetTypeInfo( UINT iTInfo,LCID lcid,ITypeInfo **ppTInfo) {return E_NOTIMPL;}
	HRESULT STDMETHODCALLTYPE GetIDsOfNames(REFIID riid,LPOLESTR *rgszNames,UINT cNames,LCID lcid,DISPID *rgDispId) {return E_NOTIMPL;}
	HRESULT STDMETHODCALLTYPE Invoke( DISPID dispIdMember,REFIID riid,LCID lcid,WORD wFlags,DISPPARAMS *pDispParams,VARIANT *pVarResult,EXCEPINFO *pExcepInfo,UINT *puArgErr);
public:	// impl IEventBase
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

public:	// Office Events
	HRESULT __stdcall Startup() { return S_OK; }
	HRESULT __stdcall Quit() { return S_OK; }
	HRESULT __stdcall DocumentChange();
	HRESULT __stdcall DocumentOpen(struct Word2016::_Document * Doc);
	HRESULT __stdcall DocumentBeforeClose(struct Word2016::_Document * Doc,VARIANT_BOOL * Cancel);
	HRESULT __stdcall DocumentBeforePrint(struct Word2016::_Document * Doc,VARIANT_BOOL * Cancel);
	HRESULT __stdcall DocumentBeforeSave(struct Word2016::_Document * Doc,VARIANT_BOOL * SaveAsUI,VARIANT_BOOL * Cancel);
	HRESULT __stdcall NewDocument(struct Word2016::_Document * Doc) { return S_OK; }
	HRESULT __stdcall WindowActivate(struct Word2016::_Document * Doc,struct Word2016::Window * Wn);
	HRESULT __stdcall WindowDeactivate(struct Word2016::_Document * Doc,struct Word2016::Window * Wn) { return S_OK; }
	HRESULT __stdcall WindowSelectionChange(struct Word2016::Selection * Sel) { return S_OK; }
	HRESULT __stdcall WindowBeforeRightClick(struct Word2016::Selection * Sel,VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT __stdcall WindowBeforeDoubleClick(struct Word2016::Selection * Sel,VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT __stdcall EPostagePropertyDialog(struct Word2016::_Document * Doc) { return S_OK; }
	HRESULT __stdcall EPostageInsert(struct Word2016::_Document * Doc) { return S_OK; }
	HRESULT __stdcall MailMergeAfterMerge(struct Word2016::_Document * Doc,struct Word2016::_Document * DocResult) { return S_OK; }
	HRESULT __stdcall MailMergeAfterRecordMerge(struct Word2016::_Document * Doc) { return S_OK; }
	HRESULT __stdcall MailMergeBeforeMerge(struct Word2016::_Document * Doc,long StartRecord,long EndRecord,VARIANT_BOOL * Cancel) {return S_OK;}
	HRESULT __stdcall MailMergeBeforeRecordMerge(struct Word2016::_Document * Doc,VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT __stdcall MailMergeDataSourceLoad(struct Word2016::_Document * Doc) { return S_OK; }
	HRESULT __stdcall MailMergeDataSourceValidate(struct Word2016::_Document * Doc,VARIANT_BOOL * Handled) { return S_OK; }
	HRESULT __stdcall MailMergeWizardSendToCustom(struct Word2016::_Document * Doc) { return S_OK; }
	HRESULT __stdcall MailMergeWizardStateChange(struct Word2016::_Document * Doc,int * FromState,int * ToState,VARIANT_BOOL * Handled) { return S_OK; }
	HRESULT __stdcall WindowSize(struct Word2016::_Document * Doc,struct Word2016::Window * Wn) { return S_OK; }
	HRESULT __stdcall XMLSelectionChange(struct Word2016::Selection * Sel,struct Word2016::XMLNode * OldXMLNode,struct Word2016::XMLNode * NewXMLNode,long * Reason) { return S_OK; }
	HRESULT __stdcall XMLValidationError(struct Word2016::XMLNode * XMLNode) { return S_OK; }
	HRESULT __stdcall DocumentSync(struct Word2016::_Document * Doc,enum Office2016::MsoSyncEventType SyncEventType) { return S_OK; }
	HRESULT __stdcall EPostageInsertEx(struct Word2016::_Document * Doc,int cpDeliveryAddrStart,int cpDeliveryAddrEnd,int cpReturnAddrStart,
		int cpReturnAddrEnd,int xaWidth,int yaHeight,BSTR bstrPrinterName,BSTR bstrPaperFeed,VARIANT_BOOL fPrint,
	VARIANT_BOOL * fCancel) {return S_OK;}
	HRESULT __stdcall MailMergeDataSourceValidate2(struct Word2016::_Document * Doc,VARIANT_BOOL * Handled) { return S_OK; }
	HRESULT __stdcall ProtectedViewWindowOpen(struct Word2016::ProtectedViewWindow * PvWindow) { return S_OK; }
	HRESULT __stdcall ProtectedViewWindowBeforeEdit(struct Word2016::ProtectedViewWindow * PvWindow, VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT __stdcall ProtectedViewWindowBeforeClose(struct Word2016::ProtectedViewWindow * PvWindow,int CloseReason,VARIANT_BOOL * Cancel) { return S_OK; }
	HRESULT __stdcall ProtectedViewWindowSize(struct Word2016::ProtectedViewWindow * PvWindow) { return S_OK; }
	HRESULT __stdcall ProtectedViewWindowActivate(struct Word2016::ProtectedViewWindow * PvWindow);
	HRESULT __stdcall ProtectedViewWindowDeactivate(struct Word2016::ProtectedViewWindow * PvWindow) { return S_OK; }

public:	// others
	//HRESULT RefreshActiveRights(void);
	void Init(IDispatch* pAppliation, IDispatch* pRibbonUI, BSTR ActiveDoc, ULONGLONG ActiveRights);
	// got additional request in it, for read_only doc, remove edit rights
	void GetDocumentRights(Word2016::_DocumentPtr doc, ULONGLONG& outRights);


private:
	long GetDocumentsCount();
	void FreeCloseRequestInfo(CloseRequestInfo* pCloseRequestInfo)
	{
		delete pCloseRequestInfo;
		pCloseRequestInfo = NULL;
	}
	void InvalidMsoControls(void);

private: // for anti recovery
	void Impl_Feature_AntiAutoRecovery();

	bool GetAutoRecoveryDir(std::wstring& outPath);
//	void SetupMonitorAutoRecoveryDir();
	void DisableAutoRecoverFeature();

private:
	Word2016::_ApplicationPtr    m_pApplication;
	ULONG				m_uRefCount;
	IDispatch			*m_pRibbonUI;
	DirMonitor    m_AutoRecoveryDirMonitor;
	std::wstring		m_ActiveDoc;
	ULONGLONG			m_ActiveDocRights;
	ULONG				m_InvalidCount;
	//CloseRequestInfo*   m_pCloseRequestInfo;
	std::vector<CloseRequestInfo*> m_pCloseRequestInfos;
	FILECHANGE_STATE   m_documentChangeState;

	typedef enum _WordAppEventId {

		Startup_Id = 1,
		Quit_Id,
		DocumentChange_Id,
		DocumentOpen_Id,
		DocumentBeforeClose_Id = 6,
		DocumentBeforePrint_Id,
		DocumentBeforeSave_Id,
		NewDocument_Id,
		WindowActivate_Id,
		WindowDeactivate_Id,
		WindowSelectionChange_Id,
		WindowBeforeRightClick_Id,
		WindowBeforeDoubleClick_Id,
		EPostagePropertyDialog_Id,
		EPostageInsert_Id,
		MailMergeAfterMerge_Id,
		MailMergeAfterRecordMerge_Id,
		MailMergeBeforeMerge_Id,
		MailMergeBeforeRecordMerge_Id,
		MailMergeDataSourceLoad_Id,
		MailMergeDataSourceValidate_Id,
		MailMergeWizardSendToCustom_Id,
		MailMergeWizardStateChange_Id,
		WindowSize_Id,
		XMLSelectionChange_Id,
		XMLValidationError_Id,
		DocumentSync_Id,
		EPostageInsertEx_Id,
		MailMergeDataSourceValidate2_Id,
		ProtectedViewWindowOpen_Id,
		ProtectedViewWindowBeforeEdit_Id,
		ProtectedViewWindowBeforeClose_Id,
		ProtectedViewWindowSize_Id,
		ProtectedViewWindowActivate_Id,
		ProtectedViewWindowDeactivate_Id

	}WordAppEventId;
};