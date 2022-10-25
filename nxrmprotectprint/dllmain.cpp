// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "SafePrintHandler.h"
#include "HookManager.h"

extern "C" __declspec(dllexport) bool StartSafePrint();

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	{
		::DisableThreadLibraryCalls(hModule);
		//DetourTransactionBegin();
		//DetourUpdateThread(GetCurrentThread());
		//DetourAttach(&(PVOID&)True_CreateDCW, HookedCreateDCW);
		//DetourAttach(&(PVOID&)True_StartDocW, HookedStartDocW);
		//DetourAttach(&(PVOID&)True_EndDoc, HookedEndDoc);
		//LONG lError = DetourTransactionCommit();
		//if (lError != NO_ERROR) {
		//	MessageBox(HWND_DESKTOP, L"Failed to detour", L"timb3r", MB_OK);
		//	return FALSE;
		//}
	}
	break;
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:
	{
		HookManager::Instance()->unhook();
		//DetourTransactionBegin();
		//DetourUpdateThread(GetCurrentThread());
		//DetourDetach(&(PVOID&)True_CreateDCW, HookedCreateDCW);
		//DetourDetach(&(PVOID&)True_StartDocW, HookedStartDocW);
		//DetourDetach(&(PVOID&)True_EndDoc, HookedEndDoc);
		//LONG lError = DetourTransactionCommit();
		//if (lError != NO_ERROR) {
		//	MessageBox(HWND_DESKTOP, L"Failed to detour", L"timb3r", MB_OK);
		//	return FALSE;
		//}
	}
	break;
	}
	return TRUE;
}

HDC(WINAPI* True_CreateDCW)(LPCWSTR pwszDriver, LPCWSTR pwszDevice, LPCWSTR pszPort, const DEVMODEW* pdm) = CreateDCW;
HDC WINAPI HookedCreateDCW(_In_opt_ LPCWSTR pwszDriver, _In_opt_ LPCWSTR pwszDevice, _In_opt_ LPCWSTR pszPort, _In_opt_ CONST DEVMODEW * pdm) {
	//::MessageBoxW(NULL, L"", L"Good hook point!!! of CreateDCW", 0);
	// filter out
	// this is also for monitor screen to set display_dc, filter out this 
	if (pwszDriver && 0 == _wcsicmp(pwszDriver, L"DISPLAY")) {
		return True_CreateDCW(pwszDriver, pwszDevice, pszPort, pdm);
	}
	// filter out
	// NULL ==pwszDevice
	if (NULL == pwszDevice) {
		return True_CreateDCW(pwszDriver, pwszDevice, pszPort, pdm);
	}

	HDC rt = NULL;
	std::wstring printer_name = pwszDevice;
	auto& handler = SafePrintHandler::Instance();
	rt = True_CreateDCW(pwszDriver, pwszDevice, pszPort, pdm);
	handler.Insert(rt, -1, printer_name);
	return rt;
}


template<typename T>
int icompare(T c1, T c2)
{
	c1 = ::tolower(c1);
	c2 = ::tolower(c2);
	return (c1 == c2) ? 0 : ((c1 > c2) ? 1 : -1);
}

template<typename T>
int incompare(const T* s1, const T* s2, size_t n)
{
	while ((*s1 || *s2) && n)
	{
		int ret = icompare(*s1, *s2);
		if (0 != ret)
			return ret;

		++s1;
		++s2;
		--n;
	}
	return 0;
}

template<typename T>
bool ibegin_with(const std::basic_string<T>& s, const std::basic_string<T>& s2)
{
	if (s.length() < s2.length())
		return false;
	return (0 == incompare<T>(s.c_str(), s2.c_str(), s2.length()));
}

int (WINAPI* True_StartDocW)(HDC hdc, const DOCINFOW* lpdi) = StartDocW;
int WINAPI HookedStartDocW(_In_ HDC hdc, _In_ CONST DOCINFOW *lpdi) {
	//::MessageBoxW(NULL, L"", L"Good hook point!!! of StartDocW", 0);
	auto& handler = SafePrintHandler::Instance();
	std::wstring wstrFilePath(lpdi->lpszDocName);
	std::wstring prefix = L"document:";
	if (ibegin_with(wstrFilePath, prefix)) {
		wstrFilePath = wstrFilePath.substr(9);
		////lpdi->lpszDocName = L"document";
		//DOCINFOW cp_lpdi = *lpdi;
		//cp_lpdi.lpszDocName = L"document";
		//lpdi = &cp_lpdi;
	}
	unsigned int dirstatus = 0;
	bool filestatus = false;
	SDWLResult ret = handler.m_pRmcInstance->RPMGetFileStatus(wstrFilePath, &dirstatus, &filestatus);
	if (ret.GetCode() != 0) {
		std::wstring outputLog = L"Error in call method RPMGetFileStatus ";
		outputLog += L"filePath:" + wstrFilePath;
		OutputDebugStringW(outputLog.c_str());
		return True_StartDocW(hdc, lpdi);
	}

	if (!filestatus) {
		std::wstring outputLog = L"This file is normal file , allow it pass ";
		outputLog += L"filePath:" + wstrFilePath;
		OutputDebugStringW(outputLog.c_str());
		return True_StartDocW(hdc, lpdi);
	}

	if (handler.Enforce_Security_Policy(hdc) != SafePrintHandler::Result_True_Support_Safe_Print)
	{
		if (handler.Enforce_Security_Policy(hdc) == SafePrintHandler::Result_False_Match_In_Prohibited_List)
		{
			SDWLResult ret = handler.m_pUser->AddActivityLog(wstrFilePath, RL_OPrint, RL_RDenied);
			std::wstring msg = L"You can't use this printer. Please ask your administrator to add this printer to trusted-printer-list if you want to.";
			handler.Popup_Message(msg, wstrFilePath, 0);
			return 0;
		}
	}
	auto jobID = True_StartDocW(hdc, lpdi);
	if (jobID > 0) {
		handler.Modify_JobID(hdc, jobID);
		handler.Insert(hdc, wstrFilePath);
		std::wstring outputLog = L"This file is nxl file , modify job id and record nxl file path ";
		outputLog += L"filePath:" + wstrFilePath;
		OutputDebugStringW(outputLog.c_str());
	}
	return jobID;
}

int (WINAPI* True_EndDoc)(_In_ HDC hdc) = EndDoc;
int HookedEndDoc(_In_ HDC hdc) {
	//::MessageBoxW(NULL, L"", L"Good hook point!!! of EndDoc", 0);
	auto& handler = SafePrintHandler::Instance();
	::EnterCriticalSection(&(handler._lock));
	if (!handler.Contain(hdc)) {
		::LeaveCriticalSection(&(handler._lock));
		// this hdc was not record in startDoc
		return True_EndDoc(hdc);
	}

	if (handler.Get_JobID(hdc) == -1) {
		handler.Remove(hdc);
		::LeaveCriticalSection(&(handler._lock));
		//this job id is -1 , that mean it's a normal file maybe.
		return True_EndDoc(hdc);
	}

	//if (!handler.Contain(hdc) || handler.Get_JobID(hdc) == -1) {
	//	::LeaveCriticalSection(&(handler._lock));
	//	// this hdc was not record in startDoc, that means it's a normal file maybe.
	//	return True_EndDoc(hdc);
	//}

	std::vector<std::wstring> path;
	// this is for nxl file finished print, retirve the nxl file path first
	std::wstring nxl = handler.GetNxlPath(hdc);
	if (handler.GetOutputPath_IfHDCIsValid(hdc, path))
	{
		handler.Remove(hdc);
		::LeaveCriticalSection(&(handler._lock));

		if (path.size() == 1)
		{
			// Notice:  path is valid, but the caller may not release it.
			auto rt = True_EndDoc(hdc);
			std::wstring SafePrinter_Path = path[0];

			//auto param = new_param(SafePrinter_Path, nxl);
			//// fire an thread to handle the converting task	
			//HANDLE h = (HANDLE)::_beginthreadex(NULL, NULL,
			//	convert_file_to_nxl_worker, (void*)param, NULL, NULL);

			//if (h != INVALID_HANDLE_VALUE) {
			//	//g_nxrmExt2->m_worker_Threads.insert(h);
			//}
			std::wstring new_nxl_file = handler.Convert_PrintedFile_To_NXL(SafePrinter_Path, nxl);
			if (!new_nxl_file.empty())
			{
				std::wstring msg = L"The printed file is protected as '" + new_nxl_file + L"'.";
				handler.Popup_Message(msg, nxl, 1);
			}
			else {
				// failed when converting
				std::wstring msg = L"System error occurred when printing this file.";
				handler.Popup_Message(msg, nxl, 1);
			}

			return rt;
		}
		else if (path.size() >= 2)
		{
			// more than 2 jobs are running; before we align the jobid + GetActiveDoc(nxl) earlier (in startdoc), we will block 2 jobs
			std::wstring msg = L"There is already one printing job running. You cann't print this NextLabs protected file until it is finished.";
			handler.Popup_Message(msg, nxl, 0);
			return 0;
		}
		else
		{
			// allowed job in printer which has no path; it might be physical printer in our printer whitelist
			return True_EndDoc(hdc);
		}
	}
	else {
		//
		// STOP printing as we can't find the output file path
		//
		//		it would be to print file to OneNote, Adobe PDF, WebEx Meeting
		//
		// send notification

		::LeaveCriticalSection(&(handler._lock));
		if (handler.Enforce_Security_Policy(hdc) != SafePrintHandler::Result_True_Support_Safe_Print)
		{
			std::wstring msg = L"You can't use this printer. Please ask your administrator to add this printer to trusted-printer-list if you want to.";
			handler.Popup_Message(msg, nxl, 0);
		}
		else
		{
			return True_EndDoc(hdc);
		}

		return 0;
	}
	::LeaveCriticalSection(&(handler._lock));
	return True_EndDoc(hdc);
}


extern "C" __declspec(dllexport) bool StartSafePrint()
{
	ISDRmcInstance *pInstance = NULL;
	ISDRmTenant *pTenant = NULL;
	ISDRmUser *puser = NULL;
	std::string strPasscode = "{6829b159-b9bb-42fc-af19-4a6af3c9fcf6}";
	SDWLResult res = RPMGetCurrentLoggedInUser(strPasscode, pInstance, pTenant, puser);
	if (res != 0) {
		return false;
	}
	auto& handler = SafePrintHandler::Instance();
	handler.m_pRmcInstance = pInstance;
	handler.m_pUser = puser;
	handler.m_pTenant = pTenant;
	HookManager::Instance()->AddHookItem((void**)&True_CreateDCW, HookedCreateDCW, "gdi32.dll", "CreateDCW");
	HookManager::Instance()->AddHookItem((void**)&True_StartDocW, HookedStartDocW, "gdi32.dll", "StartDocW");
	HookManager::Instance()->AddHookItem((void**)&True_EndDoc, HookedEndDoc, "gdi32.dll", "EndDoc");
	HookManager::Instance()->hook();
	return true;
}



