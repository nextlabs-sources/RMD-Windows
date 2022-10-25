// dllmain.cpp : Implementation of DllMain.

#include "stdafx.h"
#include <sstream>
#include "common/celog2/celog.h"
#include "SDLAPI.h"
#include "resource.h"
#include "nxrmcom_i.h"
#include "dllmain.h"

CnxrmcomModule _AtlModule;

// DLL Entry Point
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	BOOL bRet;
	hInstance;

	if (DLL_PROCESS_DETACH == dwReason) {
		SDWLibCleanup();
		CELog_Destroy();
	}

	bRet = _AtlModule.DllMain(dwReason, lpReserved);

	if (DLL_PROCESS_ATTACH == dwReason) {
		CELog_Init();
		SDWLibInit();
	}

	return bRet;
}
