#pragma once
#include <Unknwn.h>
#include <string>
#include <set>
#include <map>
#include <algorithm>

typedef std::map<std::wstring, BOOL> FILECHANGE_STATE;

//	Abstract class
class IEventBase
{
public:	// Derived must Impl
	virtual STDMETHODIMP GetActiveDoc(std::wstring& ActiveDoc) = 0;
	virtual STDMETHODIMP GetActiveRights(ULONGLONG& ActiveRights) = 0;
	virtual void Init(IDispatch* pIApplication, IDispatch* pRibbonUI, BSTR ActiveDoc, ULONGLONG ActiveRights) = 0;

public: // 
	void SetFileChanged(FILECHANGE_STATE& changeState, const wchar_t* wszFile){
		if (wszFile){
			changeState[wszFile] = TRUE;
		}
	}
	void RemoveFileChanged(FILECHANGE_STATE& changeState, const wchar_t* wszFile){
		if (wszFile){
			changeState.erase(wszFile);
		}
	}
	bool IsFileChanged(FILECHANGE_STATE& changeState, const wchar_t* wszFile){
		if (wszFile){
			FILECHANGE_STATE::iterator itState = changeState.find(wszFile);
			if (itState != changeState.end()){
				return true;
			}
		}
		return false;
	}
};