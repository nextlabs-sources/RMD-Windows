#include "stdafx.h"
#include "CoreIDropTarget.h"
#include "nxrmext2.h"
#include "rightsdef.h"
#include "CommonFunction.h"
#include <cctype>

namespace {

	class scope_guard {
	public:
		scope_guard(std::function<void()> fun) {
			m_f = fun;
		}
		~scope_guard() {
			m_f();
		}
	private:
		std::function<void()> m_f;
	};

	inline std::wstring wstr_tolower(std::wstring s) {
		std::transform(s.begin(), s.end(), s.begin(),
			[](unsigned char c) { return std::tolower(c); } // correct
		);
		return s;
	}


	bool is_nxl(const std::wstring& path) {
		if (path.length() < 4) {
			return false;
		}
		if (CommonFunction::IsNXLFile(path.c_str())) {
			return true;
		}
		else {
			if (0 == path.compare(path.length() - 4, 4, L".nxl")) {
				return true;
			}
		}
		return false;
	}

	bool is_contained_nxl_file(IDataObject* pDataObj) {
		if (!pDataObj) {
			return false;
		}
		HRESULT hr = E_FAIL;
		FORMATETC format{ 0 };
		format.cfFormat = CF_HDROP;
		format.tymed = TYMED_HGLOBAL;
		format.dwAspect = DVASPECT_CONTENT;
		format.lindex = -1;
		STGMEDIUM medium;
		hr = pDataObj->GetData(&format, &medium);
		if (FAILED(hr)) {
			return false;
		}
		HDROP hdrop = (HDROP)::GlobalLock(medium.hGlobal);
		if (hdrop == NULL) {
			return false;
		}
		scope_guard sg([&medium]() {
			::GlobalUnlock(medium.hGlobal);
			});

		int total = ::DragQueryFileW(hdrop, -1, NULL, 0);
		for (int i = 0; i < total; i++) {
			wchar_t buf[0x400] = { 0 };
			::DragQueryFileW(hdrop, i, buf, 0x400);
			if (is_nxl(wstr_tolower(buf))) {
				::DragFinish(hdrop);
				return true;
			}
		}

		return false;
	}


	void Force_Abandon_DragDrop() {
		INPUT ip;
		ip.type = INPUT_KEYBOARD;
		ip.ki.wScan = 0; 
		ip.ki.time = 0;
		ip.ki.dwExtraInfo = 0;
		ip.ki.wVk = VK_ESCAPE; 

		for (int i = 0; i < 2; i++)
		{
			::Sleep(400);
			// press esc
			ip.ki.dwFlags = 0; // 0 for key press
			SendInput(1, &ip, sizeof(INPUT));
			// key_up ecs
			ip.ki.dwFlags = KEYEVENTF_KEYUP; // 0 for key press
			SendInput(1, &ip, sizeof(INPUT));
			//
			::PostMessage(GetForegroundWindow(), WM_KEYDOWN, VK_ESCAPE, 0X00010001);
			::PostMessage(GetForegroundWindow(), WM_KEYUP, VK_ESCAPE, 0XC0010001);
		}
	}
}




CoreIDropTarget::CoreIDropTarget()
{
	m_uRefCount = 0;
	m_pIDropTarget = NULL;
}

CoreIDropTarget::CoreIDropTarget(IDropTarget *pTarget)
{
	m_uRefCount = 0;
	pTarget->AddRef();
	m_pIDropTarget = pTarget;
}

CoreIDropTarget::~CoreIDropTarget()
{
	if (m_pIDropTarget)
	{
		m_pIDropTarget->Release();
		m_pIDropTarget = NULL;
	}
}

STDMETHODIMP CoreIDropTarget::QueryInterface(
	/* [in] */ __RPC__in REFIID riid,
	/* [annotation][iid_is][out] */
	_COM_Outptr_  void **ppvObject)
{
	HRESULT hRet = S_OK;
	void *punk = NULL;
	*ppvObject = NULL;
	do
	{
		if (IID_IUnknown == riid)
		{
			punk = (IUnknown *)this;
		}
		else if (IID_IDropTarget == riid)
		{
			punk = (IDropTarget*)this;
		}
		else
		{
			hRet = m_pIDropTarget->QueryInterface(riid, ppvObject);
			break;
		}
		AddRef();
		*ppvObject = punk;
	} while (FALSE);
	return hRet;
}

STDMETHODIMP_(ULONG) CoreIDropTarget::AddRef()
{
	m_uRefCount++;
	return m_uRefCount;
}

STDMETHODIMP_(ULONG) CoreIDropTarget::Release()
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

STDMETHODIMP CoreIDropTarget::DragEnter(
	/* [unique][in] */ __RPC__in_opt IDataObject *pDataObj,
	/* [in] */ DWORD grfKeyState,
	/* [in] */ POINTL pt,
	/* [out][in] */ __RPC__inout DWORD *pdwEffect)
{
	DEVLOG_FUN;
	ULONGLONG activeRights = BUILTIN_RIGHT_ALL;
	if (g_nxrmExt2)
		g_nxrmExt2->GetActiveRights(activeRights);
	if (!(activeRights & BUILTIN_RIGHT_EDIT))
	{
		*pdwEffect = DROPEFFECT_NONE;	// block
		return S_OK;
	}
	if (is_contained_nxl_file(pDataObj)) {
		Force_Abandon_DragDrop();
		*pdwEffect = DROPEFFECT_NONE;	// block
		//m_pIDropTarget->DragEnter(pDataObj, grfKeyState, pt, pdwEffect);
		return S_OK;
	}
	return m_pIDropTarget->DragEnter(pDataObj, grfKeyState, pt, pdwEffect);
}

STDMETHODIMP CoreIDropTarget::DragOver(
	/* [in] */ DWORD grfKeyState,
	/* [in] */ POINTL pt,
	/* [out][in] */ __RPC__inout DWORD *pdwEffect)
{
	DEVLOG_FUN;
	return m_pIDropTarget->DragOver(grfKeyState, pt, pdwEffect);
}

STDMETHODIMP CoreIDropTarget::DragLeave()
{
	DEVLOG_FUN;
	return m_pIDropTarget->DragLeave();
}

STDMETHODIMP CoreIDropTarget::Drop(
	/* [unique][in] */ __RPC__in_opt IDataObject *pDataObj,
	/* [in] */ DWORD grfKeyState,
	/* [in] */ POINTL pt,
	/* [out][in] */ __RPC__inout DWORD *pdwEffect)
{
	DEVLOG_FUN;
	if (is_contained_nxl_file(pDataObj)) {
		Force_Abandon_DragDrop();
		return S_OK;
	}
	return m_pIDropTarget->Drop(pDataObj, grfKeyState, pt, pdwEffect);
}