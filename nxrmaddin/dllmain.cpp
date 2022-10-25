#include "stdafx.h"
#include "Log.h"

HMODULE g_hModule = NULL;

BOOL APIENTRY DllMain(HMODULE hModule,WORD  ul_reason_for_call,LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		DEVLOG(L"nxrmaddin dllmain \n");
		DisableThreadLibraryCalls(hModule);
		g_hModule = hModule;
		theLog.Init(0, 0);
		break;

	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}

	return TRUE;
}

