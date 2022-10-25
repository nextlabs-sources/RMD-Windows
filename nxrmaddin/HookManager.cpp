#include "stdafx.h"
#include "HookManager.h"
#include "rightsdef.h"
#include <Shlobj.h>
#include <new>
#include "Log.h"
#include "thirdpart\Detours\include\detours.h"
#ifdef _WIN64 
#pragma comment(lib, "thirdpart/Detours/lib.X64/detours.lib")
#else
#pragma comment(lib, "thirdpart/Detours/lib.X86/detours.lib")
#endif // _WIN64 

HookManager::HookManager()
{
	InitializeCriticalSection(&m_csHookItem);
}

bool HookManager::AddHookItem(void** OldFunction, void* newFunction, const char* szDllName, const char* szFunName)
{
	if (OldFunction == NULL || newFunction == NULL || szDllName == NULL || szFunName == NULL)
	{
		return false;
	}

	if (!FindHookItem(szDllName, szFunName))
	{
		HookItem* item = new HookItem();
		item->OldFunction = OldFunction;
		item->NewFunction = newFunction;
		item->szDllName = szDllName;
		item->szFunName = szFunName;
		item->lHookResult = -1;

		InsertHookItem(item);
	}

	return true;
}

const HookItem* HookManager::FindHookItem(const char* szDllName, const char* szFunName)
{
	try
	{
		CriticalSectionLock lockCS(&m_csHookItem);
		std::list<HookItem*>::const_iterator itHookItem = m_lstHookItem.begin();
		while (itHookItem != m_lstHookItem.end())
		{
			if (_stricmp(szDllName, (*itHookItem)->szDllName) == 0 &&
				_stricmp(szFunName, (*itHookItem)->szFunName) == 0)
			{
				return *itHookItem;
			}

			itHookItem++;
		}
	}
	catch (...)
	{
		theLog.WriteLog(0, NULL, 0, L"Exception on HookManager::FindHookItem\r\n");
	}
	
	return NULL;
}

void HookManager::InsertHookItem(HookItem* item)
{
	CriticalSectionLock lockCS(&m_csHookItem);
	m_lstHookItem.push_back(item);
}

void HookManager::hook()
{
	try
	{
		DetourRestoreAfterWith();
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());

		//begin hook
		CriticalSectionLock lockCS(&m_csHookItem);
		std::list<HookItem*>::iterator itHookItem = m_lstHookItem.begin();
		while (itHookItem != m_lstHookItem.end())
		{
			HookItem* item = *itHookItem;
			void* oldFunction = DetourFindFunction(item->szDllName, item->szFunName);
			if (oldFunction)
			{
				*(item->OldFunction) = oldFunction;
				item->lHookResult = DetourAttach(item->OldFunction, item->NewFunction);
			}
			itHookItem++;
		}

		DetourTransactionCommit();
	}
	catch (...)
	{
		theLog.WriteLog(0, NULL, 0, L"Exception on HookManager::hook.\r\n");
	}
	
}

void HookManager::unhook()
{
	try
	{
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());

		//begin hook
		CriticalSectionLock lockCS(&m_csHookItem);
		std::list<HookItem*>::iterator itHookItem = m_lstHookItem.begin();
		while (itHookItem != m_lstHookItem.end())
		{
			HookItem* item = *itHookItem;
			if (!item->lHookResult)
			{
				DetourDetach(item->OldFunction, item->NewFunction);
			}
			itHookItem++;
		}

		DetourTransactionCommit();
	}
	catch (...)
	{
		theLog.WriteLog(0, NULL, 0, L"Exception on  HookManager::unhook.\r\n");
	}
	
}
