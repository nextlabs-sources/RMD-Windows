#pragma once
#include <list>
#include "CriticalSectionLock.h"

struct HookItem
{
	void** OldFunction;
	void*  NewFunction;
	const char* szDllName;
	const char* szFunName;
	long   lHookResult;
};

class HookManager
{
private:
	HookManager();
	HookManager(const HookManager&) {}

public:
	static HookManager* Instance()
	{
		static HookManager hookMgr;
		return &hookMgr;
	}
	bool AddHookItem(void** OldFunction, void* newFunction, const char* szDllName, const char* szFunName);

	void hook();
	void unhook();

private:
	const HookItem* FindHookItem(const char* szDllName, const char* szFunName);
	void InsertHookItem(HookItem* item);


private:
	CRITICAL_SECTION m_csHookItem;
	std::list<HookItem*>  m_lstHookItem;
	//dll name and function
};

