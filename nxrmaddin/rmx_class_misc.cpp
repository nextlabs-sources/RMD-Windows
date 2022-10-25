/*
	Special designed to put in some non-important, trivials class functions definitions

*/
#include "stdafx.h"
#include "rightsdef.h"
#include "nxrmext2.h"
#include "officelayout.h"
#include "wordevents.h"
#include "powerpointevents.h"
#include "excelevents.h"

#pragma region For_nxrmExt2

extern LONG g_unxrmext2InstanceCount;

nxrmExt2::nxrmExt2()
{
	m_uRefCount = 1;

	m_OfficeAppType = OfficeAppInvalid;
	m_pAppObj = NULL;
	m_pRibbonUI = NULL;
	m_pOfficeEventSink = NULL;
	dwAdviseCookie = NULL;
}


nxrmExt2::~nxrmExt2()
{
}

HRESULT STDMETHODCALLTYPE nxrmExt2::QueryInterface(REFIID riid, void** ppvObject)
{
	HRESULT hr = S_OK;
	void* punk = NULL;
	*ppvObject = NULL;


	if (IID_IUnknown == riid || IID_IDispatch == riid)
	{
		punk = this;
	}
	else if (__uuidof(AddInDesignerObjects::_IDTExtensibility2) == riid)
	{
		punk = dynamic_cast<AddInDesignerObjects::_IDTExtensibility2*>(this);
	}
	else if (__uuidof(Office2016::IRibbonExtensibility) == riid)
	{
		punk = dynamic_cast<Office2016::IRibbonExtensibility*>(this);
	}
	else
	{
		hr = E_NOINTERFACE;
		return hr;
	}

	// config outer prarm
	AddRef();
	*ppvObject = punk;
	return hr;
}

ULONG STDMETHODCALLTYPE nxrmExt2::AddRef(void)
{
	m_uRefCount++;
	return m_uRefCount;
}

ULONG STDMETHODCALLTYPE nxrmExt2::Release(void)
{
	ULONG uCount = 0;

	if (m_uRefCount)
		m_uRefCount--;

	uCount = m_uRefCount;

	if (!uCount)
	{
		delete this;
		InterlockedDecrement(&g_unxrmext2InstanceCount);
	}

	return uCount;
}

HRESULT STDMETHODCALLTYPE nxrmExt2::GetIDsOfNames(
	/* [in] */ __RPC__in REFIID riid,
	/* [size_is][in] */ __RPC__in_ecount_full(cNames) LPOLESTR* rgszNames,
	/* [range][in] */ __RPC__in_range(0, 16384) UINT cNames,
	/* [in] */ LCID lcid,
	/* [size_is][out] */ __RPC__out_ecount_full(cNames) DISPID* rgDispId)
{
	HRESULT hr = DISP_E_UNKNOWNNAME;

	UINT i = 0;

	//OutputDebugStringW(L"GetIDsOfNames");

	for (i = 0; i < cNames; i++)
	{
		//OutputDebugStringW(rgszNames[i]);

		if (wcscmp(rgszNames[i], NXRMCOREUI_CHECKMSOBUTTONSTATUS_PROC_NAME) == 0)
		{
			rgDispId[i] = NXRMCOREUI_CHECKMSOBUTTONSTATUS_ID;
			hr = S_OK;
		}
		else if (wcscmp(rgszNames[i], NXRMCOREUI_ONLOAD_PROC_NAME) == 0)
		{
			rgDispId[i] = NXRMCOREUI_ONLOAD_ID;
			hr = S_OK;
		}
		//else if (wcscmp(rgszNames[i], NXRMCOREUI_ISBUTTONVISIBLE_PROC_NAME) == 0) {
		//	rgDispId[i] = NXRMCOREUI_ISBUTTONVISIBLE_ID;
		//	hr = S_OK;
		//}
		//else if (wcscmp(rgszNames[i], NXRMCOREUI_BTNCLICK_PROC_NAME) == 0)
		//{
		//	rgDispId[i] = NXRMCOREUI_BTNCLICK_ID;
		//	hr = S_OK;
		//}
		else
		{
			rgDispId[i] = DISPID_UNKNOWN;
		}

	}
	return hr;
}

HRESULT STDMETHODCALLTYPE nxrmExt2::Invoke(
	_In_  DISPID dispIdMember,
	_In_  REFIID riid,
	_In_  LCID lcid,
	_In_  WORD wFlags,
	_In_  DISPPARAMS* pDispParams,
	_Out_opt_  VARIANT* pVarResult,
	_Out_opt_  EXCEPINFO* pExcepInfo,
	_Out_opt_  UINT* puArgErr)
{
	HRESULT hr = DISP_E_MEMBERNOTFOUND;
	VARIANT_BOOL bEnable = VARIANT_FALSE;

	switch (dispIdMember)
	{
	case NXRMCOREUI_CHECKMSOBUTTONSTATUS_ID:
		hr = OnCheckMsoButtonStatus((Office2016::IRibbonControl*)(pDispParams->rgvarg[0].pdispVal), &bEnable);
		if (SUCCEEDED(hr))
		{
			pVarResult->vt = VT_BOOL;
			pVarResult->boolVal = bEnable;
		}
		break;

	case NXRMCOREUI_ONLOAD_ID:
		hr = OnLoad((Office2016::IRibbonUI*)pDispParams->rgvarg[0].pdispVal);
		break;

	//case NXRMCOREUI_ISBUTTONVISIBLE_ID:
	//	hr = OnIsButtonVisible((Office2016::IRibbonControl*)(pDispParams->rgvarg[0].pdispVal), &bEnable);
	//	if (SUCCEEDED(hr))
	//	{
	//		pVarResult->vt = VT_BOOL;
	//		pVarResult->boolVal = bEnable;
	//	}
	//	break;
	//case NXRMCOREUI_BTNCLICK_ID:
	//	hr = OnAction((Office2016::IRibbonControl*)(pDispParams->rgvarg[0].pdispVal));
	//	break;
	default:
		break;
	}
	return hr;
}

HRESULT __stdcall nxrmExt2::OnBeginShutdown(SAFEARRAY** custom) {
	Delete_Anti_AutoSave_AutoRecoery();
	Wait__WorkerThreads_End();
	return S_OK;
}

HRESULT STDMETHODCALLTYPE nxrmExt2::GetCustomUI(BSTR RibbonID, BSTR* RibbonXml)
{
	WCHAR* wszRibbon = NULL;
	switch (m_OfficeAppType)
	{
	case OfficeAppPowerpoint:
		wszRibbon = POWERPNT_LAYOUT_XML_16;
		break;

	case OfficeAppWinword:
		wszRibbon = WORD_LAYOUT_XML_16;
		break;

	case OfficeAppExcel:
		wszRibbon = EXCEL_LAYOUT_XML_16;
		break;
	}
	//
	if (wszRibbon)
	{
		BSTR CustomUIXML = SysAllocString(wszRibbon);
		*RibbonXml = CustomUIXML;
	}
	return S_OK;
}



#pragma endregion

#pragma region WordEvents

WordEventListener::WordEventListener()
{
	m_uRefCount = 0;
	m_ActiveDocRights = BUILTIN_RIGHT_ALL;
	m_pRibbonUI = NULL;
	m_InvalidCount = 0;
	m_pCloseRequestInfos = {};
};

WordEventListener::WordEventListener(IDispatch* pRibbonUI, BSTR ActiveDoc, ULONGLONG& ActiveRights) :WordEventListener()
{
	m_pRibbonUI = pRibbonUI;
	m_ActiveDocRights = ActiveRights;
	m_ActiveDoc = ActiveDoc ? ActiveDoc : L"";
};

WordEventListener::~WordEventListener()
{

}

ULONG STDMETHODCALLTYPE WordEventListener::AddRef(void)
{
	m_uRefCount++;
	return m_uRefCount;
}

ULONG STDMETHODCALLTYPE WordEventListener::Release(void)
{
	ULONG uCount = 0;
	if (m_uRefCount)
		m_uRefCount--;
	uCount = m_uRefCount;
	if (!uCount)
	{
		delete this;
	}
	return uCount;
}

HRESULT STDMETHODCALLTYPE WordEventListener::QueryInterface(REFIID riid, void** ppvObject)
{
	HRESULT hRet = S_OK;
	void* punk = NULL;
	*ppvObject = NULL;


	if (__uuidof(Word2016::ApplicationEvents4) == riid)
	{
		punk = (Word2016::ApplicationEvents4*)this;
	}
	else if (IID_IUnknown == riid)
	{
		punk = (IUnknown*)this;
	}
	else if (IID_IDispatch == riid)
	{
		punk = (IDispatch*)this;
	}
	else
	{
		hRet = E_NOINTERFACE;
		return hRet;
	}

	AddRef();
	*ppvObject = punk;

	return hRet;
}

HRESULT STDMETHODCALLTYPE WordEventListener::Invoke(DISPID dispIdMember, REFIID riid,
	LCID lcid, WORD wFlags, DISPPARAMS* pDispParams, VARIANT* pVarResult, EXCEPINFO* pExcepInfo, UINT* puArgErr)
{
	HRESULT hr = DISP_E_MEMBERNOTFOUND;
	void* Doc = NULL;
	void* Wn = NULL;
	void* PvWindow = NULL;
	VARIANT_BOOL* Cancel = NULL;
	VARIANT_BOOL* SaveAsUI = NULL;

	switch (dispIdMember)
	{
	case DocumentOpen_Id:
	{
		PvWindow = (void*)pDispParams->rgvarg[0].pdispVal;
		hr = DocumentOpen((struct Word2016::_Document*)PvWindow);
		break;
	}
	case WindowActivate_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Doc = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Wn = (void*)pDispParams->rgvarg[i].pdispVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Doc = (void*)pDispParams->rgvarg[1].pdispVal;
			Wn = (void*)pDispParams->rgvarg[0].pdispVal;
		}

		hr = WindowActivate((Word2016::_Document*)Doc, (Word2016::Window*)Wn);
		break;
	case WindowDeactivate_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Doc = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Wn = (void*)pDispParams->rgvarg[i].pdispVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Doc = (void*)pDispParams->rgvarg[1].pdispVal;
			Wn = (void*)pDispParams->rgvarg[0].pdispVal;
		}

		hr = WindowDeactivate((Word2016::_Document*)Doc, (Word2016::Window*)Wn);

		break;

	case ProtectedViewWindowActivate_Id:

		PvWindow = (void*)pDispParams->rgvarg[0].pdispVal;

		hr = ProtectedViewWindowActivate((Word2016::ProtectedViewWindow*)PvWindow);

		break;

	case ProtectedViewWindowDeactivate_Id:

		PvWindow = (void*)pDispParams->rgvarg[0].pdispVal;

		hr = ProtectedViewWindowDeactivate((Word2016::ProtectedViewWindow*)PvWindow);

		break;

	case DocumentBeforeClose_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Doc = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Cancel = pDispParams->rgvarg[i].pboolVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Doc = (void*)pDispParams->rgvarg[1].pdispVal;
			Cancel = pDispParams->rgvarg[0].pboolVal;
		}
		hr = DocumentBeforeClose((Word2016::_Document*)Doc, Cancel);

		break;

	case DocumentBeforePrint_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Doc = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Cancel = pDispParams->rgvarg[i].pboolVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Doc = (void*)pDispParams->rgvarg[1].pdispVal;
			Cancel = pDispParams->rgvarg[0].pboolVal;
		}
		hr = DocumentBeforePrint((Word2016::_Document*)Doc, Cancel);

		break;

	case DocumentBeforeSave_Id:
		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 3); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Doc = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					SaveAsUI = pDispParams->rgvarg[i].pboolVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 2)
				{
					Cancel = pDispParams->rgvarg[i].pboolVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Doc = (void*)pDispParams->rgvarg[2].pdispVal;
			SaveAsUI = pDispParams->rgvarg[1].pboolVal;
			Cancel = pDispParams->rgvarg[0].pboolVal;
		}

		hr = DocumentBeforeSave((Word2016::_Document*)Doc, SaveAsUI, Cancel);

		break;

	case DocumentChange_Id:
		hr = DocumentChange();
		break;

	default:
		break;
	}

	return hr;
}

HRESULT __stdcall WordEventListener::ProtectedViewWindowActivate(
	/*[in]*/ struct Word2016::ProtectedViewWindow* PvWindow)
{
	HRESULT hr = S_OK;

	long hWnd = 0;

	BSTR DocFullName = NULL;

	ULONGLONG RightsMask = BUILTIN_RIGHT_ALL;
	ULONGLONG EvaluationId = 0;

	Word2016::_Document* Doc = NULL;

	BOOL UpdateRibbonUI = FALSE;

	do
	{
		hr = PvWindow->get_Document(&Doc);

		if (!SUCCEEDED(hr) || Doc == NULL)
		{
			break;
		}

		hr = Doc->get_FullName(&DocFullName);

		if (FAILED(hr))
		{
			break;
		}


		if (!DocFullName)
		{
			break;
		}


	} while (FALSE);

	if (Doc)
	{
		Doc->Release();
		Doc = NULL;
	}

	if (DocFullName)
	{
		m_ActiveDoc = DocFullName;

		SysFreeString(DocFullName);
		DocFullName = NULL;
	}
	else
	{
		m_ActiveDoc.clear();
	}

	if (m_ActiveDocRights != RightsMask)
	{
		UpdateRibbonUI = TRUE;
		m_ActiveDocRights = RightsMask;
	}


	if (UpdateRibbonUI || m_InvalidCount == 0)
	{
		InvalidMsoControls();
	}

	return hr;
}



#pragma endregion

#pragma region PowerPoint
PowerPointEventListener::PowerPointEventListener()
{
	m_uRefCount = 0;
	m_ActiveDocRights = BUILTIN_RIGHT_ALL;
	m_pRibbonUI = NULL;
	m_InvalidCount = 0;
}

PowerPointEventListener::PowerPointEventListener(IDispatch* pRibbonUI, BSTR ActiveDoc, ULONGLONG& ActiveRights)
{
	m_uRefCount = 0;
	m_pRibbonUI = pRibbonUI;
	m_ActiveDocRights = ActiveRights;
	m_ActiveDoc = ActiveDoc ? ActiveDoc : L"";
	m_InvalidCount = 0;
}

PowerPointEventListener::~PowerPointEventListener()
{

}
HRESULT STDMETHODCALLTYPE PowerPointEventListener::QueryInterface(
	/* [in] */ REFIID riid,
	/* [annotation][iid_is][out] */
	_COM_Outptr_  void** ppvObject)
{
	HRESULT hRet = S_OK;

	void* punk = NULL;

	*ppvObject = NULL;

	do
	{
		if (__uuidof(PowerPoint2016::EApplication) == riid)
		{
			punk = (PowerPoint2016::EApplication*)this;
		}
		else if (IID_IUnknown == riid)
		{
			punk = (IUnknown*)this;
		}
		else if (IID_IDispatch == riid)
		{
			punk = (IDispatch*)this;
		}
		else
		{
			hRet = E_NOINTERFACE;
			break;
		}

		AddRef();

		*ppvObject = punk;

	} while (FALSE);

	return hRet;
}

ULONG STDMETHODCALLTYPE PowerPointEventListener::AddRef(void)
{
	m_uRefCount++;

	return m_uRefCount;
}

ULONG STDMETHODCALLTYPE PowerPointEventListener::Release(void)
{
	ULONG uCount = 0;

	if (m_uRefCount)
		m_uRefCount--;

	uCount = m_uRefCount;

	if (!uCount)
	{
		delete this;
	}

	return uCount;
}


HRESULT STDMETHODCALLTYPE PowerPointEventListener::Invoke(_In_  DISPID dispIdMember,
	_In_  REFIID riid, _In_  LCID lcid, _In_  WORD wFlags, _In_  DISPPARAMS* pDispParams, _Out_opt_  VARIANT* pVarResult,
	_Out_opt_  EXCEPINFO* pExcepInfo, _Out_opt_  UINT* puArgErr)
{
	HRESULT hr = DISP_E_MEMBERNOTFOUND;

	void* Pres = NULL;
	void* Wn = NULL;
	void* PvWindow = NULL;

	VARIANT_BOOL* Cancel = NULL;

	switch (dispIdMember)
	{
	case PresentationOpen_Id:
	{
		hr = PresentationOpen((struct PowerPoint2016::_Presentation*)pDispParams->rgvarg[0].pdispVal);
		break;
	}

	case PresentationPrint_Id:
	{
		hr = PresentationPrint((struct PowerPoint2016::_Presentation*)pDispParams->rgvarg[0].pdispVal);
		break;
	}

	case WindowActivate_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Pres = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Wn = (void*)pDispParams->rgvarg[i].pdispVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Pres = (void*)pDispParams->rgvarg[1].pdispVal;
			Wn = (void*)pDispParams->rgvarg[0].pdispVal;
		}


		hr = WindowActivate((PowerPoint2016::_Presentation*)Pres, (PowerPoint2016::DocumentWindow*)Wn);

		break;
	case WindowDeactivate_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Pres = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Wn = (void*)pDispParams->rgvarg[i].pdispVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Pres = (void*)pDispParams->rgvarg[1].pdispVal;
			Wn = (void*)pDispParams->rgvarg[0].pdispVal;
		}

		hr = WindowDeactivate((PowerPoint2016::_Presentation*)Pres, (PowerPoint2016::DocumentWindow*)Wn);

		break;

	case ProtectedViewWindowActivate_Id:

		PvWindow = (void*)pDispParams->rgvarg[0].pdispVal;

		hr = ProtectedViewWindowActivate((PowerPoint2016::ProtectedViewWindow*)PvWindow);

		break;

	case ProtectedViewWindowDeactivate_Id:

		PvWindow = (void*)pDispParams->rgvarg[0].pdispVal;

		hr = ProtectedViewWindowDeactivate((PowerPoint2016::ProtectedViewWindow*)PvWindow);

		break;

	case PresentationBeforeClose_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Pres = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Cancel = pDispParams->rgvarg[i].pboolVal;
				}
			}
		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Pres = (void*)pDispParams->rgvarg[1].pdispVal;
			Cancel = pDispParams->rgvarg[0].pboolVal;
		}

		hr = PresentationBeforeClose((PowerPoint2016::_Presentation*)Pres, Cancel);

		break;

	case PresentationClose_Id:

		Pres = (void*)pDispParams->rgvarg[0].pdispVal;

		hr = PresentationClose((PowerPoint2016::_Presentation*)Pres);

		break;

	case PresentationCloseFinal_Id:
		Pres = (void*)pDispParams->rgvarg[0].pdispVal;

		hr = PresentationCloseFinal((PowerPoint2016::_Presentation*)Pres);
		break;

	case PresentationBeforeSave_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Pres = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Cancel = pDispParams->rgvarg[i].pboolVal;
				}
			}
		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Pres = (void*)pDispParams->rgvarg[1].pdispVal;
			Cancel = pDispParams->rgvarg[0].pboolVal;
		}

		hr = PresentationBeforeSave((PowerPoint2016::_Presentation*)Pres, Cancel);

		break;
	case SlideShowBegin_Id:
		hr = SlideShowBegin((PowerPoint2016::SlideShowWindow*)pDispParams->rgvarg[0].pdispVal);
		break;
	case SlideShowEnd_Id:
		hr = SlideShowEnd((PowerPoint2016::_Presentation*)pDispParams->rgvarg[0].pdispVal);
		break;

	default:
		break;
	}

	return hr;
}


#pragma endregion

#pragma region Excel
ExcelEventListener::ExcelEventListener()
{
	m_uRefCount = 0;
	m_ActiveDocRights = BUILTIN_RIGHT_ALL;
	m_pRibbonUI = NULL;
	m_InvalidCount = 0;

};

ExcelEventListener::ExcelEventListener(IDispatch* pRibbonUI, BSTR ActiveDoc, ULONGLONG& ActiveRights)
{
	m_uRefCount = 0;
	m_pRibbonUI = pRibbonUI;
	m_ActiveDocRights = ActiveRights;
	m_ActiveDoc = ActiveDoc ? ActiveDoc : L"";
	m_InvalidCount = 0;
};

ExcelEventListener::~ExcelEventListener()
{

}
HRESULT STDMETHODCALLTYPE ExcelEventListener::QueryInterface(REFIID riid, void** ppvObject)
{
	HRESULT hRet = S_OK;

	void* punk = NULL;

	*ppvObject = NULL;

	do
	{
		if (__uuidof(Excel2016::AppEvents) == riid)
		{
			punk = (Excel2016::AppEvents*)this;
		}
		else if (IID_IUnknown == riid)
		{
			punk = (IUnknown*)this;
		}
		else if (IID_IDispatch == riid)
		{
			punk = (IDispatch*)this;
		}
		else
		{
			hRet = E_NOINTERFACE;
			break;
		}

		AddRef();

		*ppvObject = punk;

	} while (FALSE);

	return hRet;
}

ULONG STDMETHODCALLTYPE ExcelEventListener::AddRef(void)
{
	m_uRefCount++;

	return m_uRefCount;
}

ULONG STDMETHODCALLTYPE ExcelEventListener::Release(void)
{
	ULONG uCount = 0;

	if (m_uRefCount)
		m_uRefCount--;

	uCount = m_uRefCount;

	if (!uCount)
	{
		delete this;
	}

	return uCount;
}
HRESULT STDMETHODCALLTYPE ExcelEventListener::Invoke(DISPID dispIdMember, REFIID riid, LCID lcid, WORD wFlags, DISPPARAMS* pDispParams,
	VARIANT* pVarResult, EXCEPINFO* pExcepInfo, UINT* puArgErr)
{
	HRESULT hr = DISP_E_MEMBERNOTFOUND;

	void* Wb = NULL;
	void* Wn = NULL;
	void* PvWindow = NULL;
	void* Sh = NULL;

	VARIANT_BOOL* Cancel = NULL;
	VARIANT_BOOL SaveAsUI = VARIANT_TRUE;

	switch (dispIdMember)
	{
	case WorkbookOpen_Id:
	{
		Excel2016::_Workbook* Wb = (Excel2016::_Workbook*)pDispParams->rgvarg[0].pdispVal;
		hr = WorkbookOpen(Wb);
		break;
	}
	case WindowActivate_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Wb = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Wn = (void*)pDispParams->rgvarg[i].pdispVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Wb = (void*)pDispParams->rgvarg[1].pdispVal;
			Wn = (void*)pDispParams->rgvarg[0].pdispVal;
		}


		hr = WindowActivate((Excel2016::_Workbook*)Wb, (Excel2016::Window*)Wn);

		break;
	case WindowDeactivate_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Wb = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Wn = (void*)pDispParams->rgvarg[i].pdispVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Wb = (void*)pDispParams->rgvarg[1].pdispVal;
			Wn = (void*)pDispParams->rgvarg[0].pdispVal;
		}

		hr = WindowDeactivate((Excel2016::_Workbook*)Wb, (Excel2016::Window*)Wn);

		break;

	case ProtectedViewWindowActivate_Id:

		PvWindow = (void*)pDispParams->rgvarg[0].pdispVal;

		hr = ProtectedViewWindowActivate((Excel2016::ProtectedViewWindow*)PvWindow);

		break;

	case ProtectedViewWindowDeactivate_Id:

		PvWindow = (void*)pDispParams->rgvarg[0].pdispVal;

		hr = ProtectedViewWindowDeactivate((Excel2016::ProtectedViewWindow*)PvWindow);
		break;

	case WorkbookBeforeClose_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Wb = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Cancel = pDispParams->rgvarg[i].pboolVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Wb = (void*)pDispParams->rgvarg[1].pdispVal;
			Cancel = pDispParams->rgvarg[0].pboolVal;
		}

		hr = WorkbookBeforeClose((Excel2016::_Workbook*)Wb, Cancel);

		break;

	case WorkbookBeforePrint_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Wb = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Cancel = pDispParams->rgvarg[i].pboolVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Wb = (void*)pDispParams->rgvarg[1].pdispVal;
			Cancel = pDispParams->rgvarg[0].pboolVal;
		}

		hr = WorkbookBeforePrint((Excel2016::_Workbook*)Wb, Cancel);

		break;

	case WorkbookBeforeSave_Id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 3); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Wb = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					SaveAsUI = pDispParams->rgvarg[i].boolVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 2)
				{
					Cancel = pDispParams->rgvarg[i].pboolVal;
				}
			}

		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Wb = (void*)pDispParams->rgvarg[2].pdispVal;
			SaveAsUI = pDispParams->rgvarg[1].boolVal;
			Cancel = pDispParams->rgvarg[0].pboolVal;
		}

		hr = WorkbookBeforeSave((Excel2016::_Workbook*)Wb, SaveAsUI, Cancel);


		break;

	case WorkbookDeactive_Id:
		Wb = (void*)pDispParams->rgvarg[0].pdispVal;
		hr = WorkbookDeactive((Excel2016::_Workbook*)Wb);
		break;

	case WorkbookActive_Id:
		Wb = (void*)pDispParams->rgvarg[0].pdispVal;
		hr = WorkbookActive((Excel2016::_Workbook*)Wb);
		break;

	case WorkbookAfterSave_Id:
	{
		Wb = (void*)pDispParams->rgvarg[0].pdispVal;
		VARIANT_BOOL varSuccess = pDispParams->rgvarg[1].boolVal;
		hr = WorkbookAfterSave((Excel2016::_Workbook*)Wb, varSuccess);
		break;
	}

	case SheetActivate_id:
		Sh = (void*)pDispParams->rgvarg[0].pdispVal;
		hr = SheetActivate((Excel2016::_Worksheet*)Sh);
		break;

	case WorkbookNewSheet_id:

		if (pDispParams->rgdispidNamedArgs)
		{
			for (UINT i = 0; i < min(pDispParams->cArgs, 2); i++)
			{
				if (pDispParams->rgdispidNamedArgs[i] == 0)
				{
					Wb = (void*)pDispParams->rgvarg[i].pdispVal;
				}

				if (pDispParams->rgdispidNamedArgs[i] == 1)
				{
					Sh = (void*)pDispParams->rgvarg[i].pdispVal;
				}
			}
		}
		else
		{
			// (parameters are on stack, thus in reverse order)
			Wb = (void*)pDispParams->rgvarg[1].pdispVal;
			Sh = (void*)pDispParams->rgvarg[0].pdispVal;
		}
		hr = WorkbookNewSheet((Excel2016::_Workbook*)Wb , (Excel2016::_Worksheet*)Sh);
		break;

	case SheetBeforeDelete_id:
		//void SheetBeforeDelete([in] IDispatch* Sh);
		Sh = (void*)pDispParams->rgvarg[0].pdispVal;
		hr = SheetBeforeDelete((Excel2016::_Worksheet*)Sh);
		break;

	case SheetDeactivate_id:
		//void SheetDeactivate([in] IDispatch* Sh);
		Sh = (void*)pDispParams->rgvarg[0].pdispVal;
		hr = SheetDeactivate((Excel2016::_Worksheet*)Sh);
		break;

	//case SheetChange_id:
	//	//void SheetChange([in] IDispatch* Sh,[in] Range* Target);
	//	hr = SheetChange();
	//	break;

	default:
		break;
	}

	return hr;
}


#pragma endregion

#pragma region Dummy4

#pragma endregion

#pragma region Dummy5

#pragma endregion

#pragma region Dummy6

#pragma endregion

