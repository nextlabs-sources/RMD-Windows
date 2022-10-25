#pragma once
#include <Ole2.h>
#include "IEventBase.h"
class CoreIDropTarget : public IDropTarget
{
public:
	CoreIDropTarget();
	CoreIDropTarget(IDropTarget *pTarget);
	~CoreIDropTarget();

	STDMETHODIMP QueryInterface(
		/* [in] */ __RPC__in REFIID riid,
		/* [annotation][iid_is][out] */
		_COM_Outptr_  void **ppvObject);

	STDMETHODIMP_(ULONG) AddRef();

	STDMETHODIMP_(ULONG) Release();

	STDMETHODIMP DragEnter(
		/* [unique][in] */ __RPC__in_opt IDataObject *pDataObj,
		/* [in] */ DWORD grfKeyState,
		/* [in] */ POINTL pt,
		/* [out][in] */ __RPC__inout DWORD *pdwEffect);

	STDMETHODIMP DragOver(
		/* [in] */ DWORD grfKeyState,
		/* [in] */ POINTL pt,
		/* [out][in] */ __RPC__inout DWORD *pdwEffect);

	STDMETHODIMP DragLeave();

	STDMETHODIMP Drop(
		/* [unique][in] */ __RPC__in_opt IDataObject *pDataObj,
		/* [in] */ DWORD grfKeyState,
		/* [in] */ POINTL pt,
		/* [out][in] */ __RPC__inout DWORD *pdwEffect);

private:
	ULONG			m_uRefCount;
	IDropTarget		*m_pIDropTarget;
};