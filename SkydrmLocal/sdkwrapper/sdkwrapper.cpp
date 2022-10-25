
#include "stdafx.h"
#include "sdkwrapper.h"
#include "SDLAPI.h"
#include "helper.h"
//
// Global
//
ISDRmcInstance* g_RmsIns = NULL;
// for caller user hTenant to reference, we have to cache it here
std::list<ISDRmTenant*> g_listTenant = std::list<ISDRmTenant*>();
// for current login user
ISDRmUser* g_User = NULL;

std::map< ISDRmcInstance*, std::wstring> g_UserRPMFolder;

// RMP security 
static const std::string gRPM_Security = "{6829b159-b9bb-42fc-af19-4a6af3c9fcf6}";


#pragma region SDK_Level

NXSDK_API DWORD GetSDKVersion()
{
	return SDWLibGetVersion();
}

NXSDK_API DWORD CreateSDKSession(const wchar_t * wcTempPath, HANDLE * phSession)
{
	OutputDebugStringA("call CreateSDKSession\n");
	// sanity check
	if (wcTempPath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	// Read NextLabs install folder from registry
	std::wstring strInstallDir;
	HKEY hKey;
	if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\NextLabs", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
		helper::GetStringRegKey(hKey, L"InstallDir", strInstallDir, L"C:\\Program Files\\NextLabs");
	else if (RegOpenKeyExW(HKEY_CURRENT_USER, L"SOFTWARE\\NextLabs", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
		helper::GetStringRegKey(hKey, L"InstallDir", strInstallDir, L"C:\\Program Files\\NextLabs");
	else
		strInstallDir = L"C:\\Program Files\\NextLabs";
	RegCloseKey(hKey);
	wchar_t* wsInstallDir = helper::allocStrInComMem(strInstallDir);

	// since 8/28/2018, we have to provide an cient string, now is :  SkyDRM LocalApp For Windows
	static const char* ProductName = "SkyDRM Desktop for Windows";
	SDWLResult rt = SDWLibCreateSDRmcInstance(ProductName, 10, 0, 0, wsInstallDir, wcTempPath, &g_RmsIns);
	if (!rt) {
		return rt.GetCode();
	}

	// Initialize PDP connection
	bool isPDPFinished = false;
	int i = 0;
	do {
		g_RmsIns->IsInitFinished(isPDPFinished);
		// PDP in theory needs almost 10 - 20 seconds to start, we have to sleep and connect again and again
		if (i == 20 || isPDPFinished)
			break;

		i++;
		Sleep(1000);
	} while (!isPDPFinished);
	// Give Caller an named handle
	*phSession = (HANDLE*)g_RmsIns;
	return SDWL_SUCCESS;
}

NXSDK_API DWORD DeleteSDKSession(HANDLE  hSession)
{
	OutputDebugStringA("call DeleteSDKSession\n");
	if ((void*)hSession != (void*)g_RmsIns) {
		return SDWL_INTERNAL_ERROR;
	}

	SDWLibDeleteRmcInstance(g_RmsIns);
	g_RmsIns = NULL;


	return SDWL_SUCCESS;
}
#pragma endregion

#pragma region SDWL_Session
NXSDK_API DWORD SDWL_Session_Initialize(HANDLE hSession, 
	const wchar_t * router, const wchar_t * tenant)
{
	OutputDebugStringA("call SDWL_Session_Initialize\n");
	// sanity check
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (router == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (tenant == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	auto rt = g_RmsIns->Initialize(router, tenant);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_Session_Initialize2(HANDLE hSession, 
	const wchar_t * workingfolder, 
	const wchar_t * router, 
	const wchar_t * tenant)
{
	OutputDebugStringA("call SDWL_Session_Initialize2\n");
	// sanity check
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (workingfolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (router == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (tenant == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	auto rt = g_RmsIns->Initialize(workingfolder, router, tenant);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_Session_SaveSession(HANDLE hSession, const wchar_t * folder)
{
	OutputDebugStringA("call SDWL_Session_SaveSession\n");
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	auto rt = g_RmsIns->Save();
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_Session_GetCurrentTenant(HANDLE hSession, HANDLE * phTenant)
{
	OutputDebugStringA("call SDWL_Session_GetCurrentTenant\n");
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	ISDRmTenant* pTenant = NULL;
	auto rt = g_RmsIns->GetCurrentTenant(&pTenant);
	if (!rt || pTenant == NULL) {
		return rt.GetCode();
	}
	// set value to caller
	*phTenant = (HANDLE)pTenant;
	// cache this value
	g_listTenant.push_back(pTenant);
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_Session_GetLoginParams(HANDLE hSession, 
	wchar_t** ppURL, NXL_LOGIN_COOKIES** ppCookies, size_t* pSize)
{
	OutputDebugStringA("Call SDWL_Session_GetLoginParams\n");
	//sanity check
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (ppURL == NULL || ppCookies == NULL || pSize ==  NULL) {
		return SDWL_INTERNAL_ERROR;

	}

	ISDRmHttpRequest* pRequest = NULL;
	auto rt = g_RmsIns->GetLoginRequest(&pRequest);
	if (!rt || pRequest == NULL) {
		return rt.GetCode();
	}

	auto url = pRequest->GetPath();
	*ppURL = helper::allocStrInComMem(url);	

	// prepare the values to caller
	auto cookies = pRequest->GetCookies();
	// set size
	int size = (int)cookies.size();
	*pSize = size;
	if (size == 0) {
		return SDWL_SUCCESS;
	}
	// alloc buf
	NXL_LOGIN_COOKIES* p= (NXL_LOGIN_COOKIES*)::CoTaskMemAlloc(size * sizeof(NXL_LOGIN_COOKIES));
	for (int i = 0; i < size; i++) {
		HttpCookie q = cookies[i];
		// key
		p[i].key = helper::allocStrInComMem(q.first);
		p[i].values = helper::allocStrInComMem(q.second);
	}
	*ppCookies = p;
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_Session_SetLoginRequest(HANDLE hSession,
	const wchar_t* JsonReturn, const wchar_t* security, HANDLE* hUser)
{
	OutputDebugStringA("call SDWL_Session_SetLoginRequest\n");
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (JsonReturn == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmUser* puser = NULL;
	std::string utf8JsonReturn = helper::utf162utf8(JsonReturn);
	std::string utf8Security = helper::utf162utf8(security);
	auto rt = g_RmsIns->SetLoginResult(utf8JsonReturn, &puser, utf8Security);
	if (!rt || puser == NULL) {
		return rt.GetCode();
	}

	// set value to caller
	*hUser = (HANDLE)puser;
	// catch value
	g_User = puser;
	// Call once to get heartbeat (user attributes, policies)
	g_User->GetHeartBeatInfo();
	// by osmond, this should to blame SDK-devs, it has to call save() here, I either do not know why
	g_RmsIns->Save();

	// Initialize RPM
	if (g_RmsIns->IsRPMDriverExist())
	{
		g_RmsIns->SetRPMLoginResult(utf8JsonReturn, gRPM_Security);

		// Read NextLabs install folder from registry
		//std::wstring strInstallDir;
		//HKEY hKey;
		//if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\NextLabs", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
		//	helper::GetStringRegKey(hKey, L"InstallDir", strInstallDir, L"C:\\Program Files\\NextLabs");
		//else if (RegOpenKeyExW(HKEY_CURRENT_USER, L"SOFTWARE\\NextLabs", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
		//	helper::GetStringRegKey(hKey, L"InstallDir", strInstallDir, L"C:\\Program Files\\NextLabs");
		//else
		//	strInstallDir = L"C:\\Program Files\\NextLabs";
		//RegCloseKey(hKey);

		// g_RmsIns->SetRPMPDPDir(strInstallDir);
		//g_RmsIns->SetRPMPolicyBundle();
		g_RmsIns->SyncUserAttributes();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_Session_GetLoginUser(HANDLE hSession, 
	const wchar_t * UserEmail, 
	const wchar_t * PassCode,
	HANDLE * hUser)
{
	OutputDebugStringA("call SDWL_Session_GetLoginUser\n");
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (UserEmail == NULL || PassCode == NULL) {
		return SDWL_INTERNAL_ERROR;
	}


	ISDRmUser* puser = NULL;
	auto rt = g_RmsIns->GetLoginUser(
		helper::utf162utf8( UserEmail), 
		helper::utf162utf8(PassCode), 
		&puser);
	if (!rt || puser == NULL ) {
		return rt.GetCode();
	}
	//set value to caller
	*hUser = (HANDLE)puser;
	g_User = puser;

	// Call once to get heartbeat (user attributes, policies)
	g_User->GetHeartBeatInfo();
	// by osmond, this should to blame SDK-devs, it has to call save() here, I either do not know why
	g_RmsIns->Save();

	// Initialize RPM
	if (g_RmsIns->IsRPMDriverExist())
	{
		std::string JsonReturn;
		auto ret = g_RmsIns->GetLoginData(helper::utf162utf8(UserEmail), helper::utf162utf8(PassCode), JsonReturn);
		if (!ret)
		{
			return ret.GetCode();
		}

		ret = g_RmsIns->SetRPMLoginResult(JsonReturn, gRPM_Security);
		if (!ret)
		{
			return ret.GetCode();
		}

		// Read NextLabs install folder from registry
		//std::wstring strInstallDir;
		//HKEY hKey;
		//if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\NextLabs", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
		//	helper::GetStringRegKey(hKey, L"InstallDir", strInstallDir, L"C:\\Program Files\\NextLabs");
		//else if (RegOpenKeyExW(HKEY_CURRENT_USER, L"SOFTWARE\\NextLabs", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
		//	helper::GetStringRegKey(hKey, L"InstallDir", strInstallDir, L"C:\\Program Files\\NextLabs");
		//else
		//	strInstallDir = L"C:\\Program Files\\NextLabs";
		//RegCloseKey(hKey);

		//g_RmsIns->SetRPMPDPDir(strInstallDir);
		//g_RmsIns->SetRPMPolicyBundle();
		g_RmsIns->SyncUserAttributes();
	}

	return SDWL_SUCCESS;
}




#pragma endregion

#pragma region Tenant_Level
NXSDK_API DWORD SDWL_Tenant_GetTenant(HANDLE hTenant, wchar_t ** pptenant)
{
	OutputDebugStringA("call SDWL_Tenant_GetTenant\n");
	// find if hTenant lies in g_listTenant( std::list<ISDRmTenant*> )
	auto result = std::find(g_listTenant.begin(), g_listTenant.end(), hTenant);
	if (result == g_listTenant.end() || *result== NULL) {
		// not found or value is invalid
		return SDWL_INTERNAL_ERROR;
	}

	*pptenant = helper::allocStrInComMem((*result)->GetTenant());

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_Tenant_GetRouterURL(HANDLE hTenant, wchar_t ** pprouterurl)
{
	OutputDebugStringA("call SDWL_Tenant_GetRouterURL\n");
	// find if hTenant lies in g_listTenant
	auto result = std::find(g_listTenant.begin(), g_listTenant.end(), hTenant);
	if (result == g_listTenant.end() || *result == NULL) {
		// not found or value is invalid
		return SDWL_INTERNAL_ERROR;
	}

	*pprouterurl = helper::allocStrInComMem((*result)->GetRouterURL());
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_Tenant_GetRMSURL(HANDLE hTenant, wchar_t ** pprmsurl)
{
	OutputDebugStringA("call SDWL_Tenant_GetRMSURL\n");
	// find if hTenant lies in g_listTenant
	auto result = std::find(g_listTenant.begin(), g_listTenant.end(), hTenant);
	if (result == g_listTenant.end() || *result == NULL) {
		// not found or value is invalid
		return SDWL_INTERNAL_ERROR;
	}
	*pprmsurl = helper::allocStrInComMem((*result)->GetRMSURL());
	return SDWL_SUCCESS;
}



NXSDK_API DWORD SDWL_Tenant_ReleaseTenant(HANDLE hTenant)
{
	OutputDebugStringA("call SDWL_Tenant_ReleaseTenant\n");
	// find if hTenant lies in g_listTenant
	auto result = std::find(g_listTenant.begin(), g_listTenant.end(), hTenant);
	if (result == g_listTenant.end()) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	else {
		g_listTenant.remove(*result);
	}
	return SDWL_SUCCESS;
}
#pragma endregion

#pragma region User_Level
NXSDK_API DWORD SDWL_User_GetUserName(HANDLE hUser, wchar_t ** ppname)
{
	OutputDebugStringA("call SDWL_User_GetUserName\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	*ppname = helper::allocStrInComMem(g_User->GetName());
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetUserEmail(HANDLE hUser, wchar_t ** ppemail)
{
	OutputDebugStringA("call SDWL_User_GetUserEmail\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	*ppemail = helper::allocStrInComMem(g_User->GetEmail());
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetPasscode(HANDLE hUser, wchar_t ** pppasscode)
{
	OutputDebugStringA("call SDWL_User_GetPasscode\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	*pppasscode = helper::allocStrInComMem(helper::utf82utf16(g_User->GetPasscode()));
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ProjectMembershipId(HANDLE hUser, DWORD32 projectId, wchar_t ** projectMembershipId)
{
	OutputDebugStringA("call SDWL_User_ProjectMembershipId\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	// do
	*projectMembershipId = helper::allocStrInComMem(helper::utf82utf16(g_User->GetMembershipID(projectId)));


	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_TenantMembershipId(HANDLE hUser, wchar_t * tenantId, wchar_t ** tenantMembershipId)
{
	OutputDebugStringA("call SDWL_User_TenantMembershipId\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	if (tenantId == NULL || wcslen(tenantId) < 1) {
		return SDWL_INTERNAL_ERROR;
	}

	// do
	*tenantMembershipId = helper::allocStrInComMem(
		helper::utf82utf16(
		g_User->GetMembershipID(
			helper::utf162utf8(tenantId))
	));


	return SDWL_SUCCESS;
}


NXSDK_API DWORD SDWL_User_LogoutUser(HANDLE hUser)
{
	OutputDebugStringA("call SDWL_User_LogoutUser\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	// TBD
	auto rt = g_User->LogoutUser();
	if (!rt) {
		return rt.GetCode();
	}

	if (g_RmsIns != NULL)
	{
		auto ret = g_RmsIns->RPMLogout();
		if (!ret) {
			return ret.GetCode();
		}
	}

	// set glocal to NULL
	g_User = NULL;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetUserType(HANDLE hUser, int * type)
{
	OutputDebugStringA("call SDWL_User_GetUserType\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (type == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	*type = g_User->GetIdpType();
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_UpdateUserInfo(HANDLE hUser)
{
	OutputDebugStringA("call SDWL_User_UpdateUserInfo\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}


	auto rt = g_User->UpdateUserInfo();
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}



NXSDK_API DWORD SDWL_User_UpdateMyDriveInfo(HANDLE hUser)
{
	OutputDebugStringA("call SDWL_User_UpdateMyDriveInfo\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	auto rt = g_User->UpdateMyDriveInfo();
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetMyDriveInfo(HANDLE hUser, DWORD64 * usage, DWORD64 * total)
{
	OutputDebugStringA("call SDWL_User_GetMyDriveInfo\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (usage == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (total == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	// by osmond, for now we only careabout myVault quatas
	DWORD64 u = 0, t = 0, vu = 0,vt=0;
	auto rt = g_User->GetMyDriveInfo(u, t,vu,vt);
	if (!rt) {
		return rt.GetCode();
	}
	//set vaule
	*usage = vu;
	*total = vt;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetLocalFile(HANDLE hUser, HANDLE * hLocalFiles)
{
	OutputDebugStringA("call SDWL_User_GetLocalFile\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRFiles *pF = NULL;
	//auto rt = g_User->GetLocalFileManager(&pF);
	//if (!rt || pF == NULL) {
	//	return SDWL_INTERNAL_ERROR;
	//}	

	// set valueto called
	*hLocalFiles = (HANDLE)pF;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_RemoveLocalFile(HANDLE hUser, const wchar_t * nxlFilePath, bool* pResult)
{
	OutputDebugStringA("call SDWL_User_RemoveLocalFile\n");
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	ISDRFiles *pF = NULL;
	//auto rt = g_User->GetLocalFileManager(&pF);
	//if (!rt || pF == NULL) {
	//	return SDWL_INTERNAL_ERROR;
	//}
	//
	//*pResult=pF->RemoveFile(pF->GetFile(nxlFilePath));

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ProtectFile(HANDLE hUser, 
	const wchar_t* path, 
	int* pRights,int len, 
	WaterMark watermark, Expiration expiration, 
	const wchar_t* tags,
	const wchar_t** outPath)
{
	OutputDebugStringA("call SDWL_User_ProtectFile for default myvault\n");
	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}	
	if (path == NULL || pRights == NULL || len ==0 || outPath== NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
		

	std::wstring p(path);
	std::vector<SDRmFileRight> r;
	for (int i = 0; i < len; i++) {
		SDRmFileRight l = (SDRmFileRight)pRights[i];
		r.push_back(l);
	}
	SDR_WATERMARK_INFO w;
	{
		w.text = helper::utf162utf8(watermark.text);
		w.fontName = helper::utf162utf8(watermark.fontName);
		w.fontColor = helper::utf162utf8(watermark.fontColor);
		w.fontSize = watermark.fontSize;
		w.transparency = watermark.transparency;
		w.rotation = (WATERMARK_ROTATION)watermark.rotation;
		w.repeat = watermark.repeat;
	}
	SDR_Expiration e;
	{
		e.type = (IExpiryType)expiration.type;
		e.start = expiration.start;
		e.end = expiration.end;
	}
	std::string t(helper::utf162utf8(tags));
	std::wstring o;

	auto rt = g_User->ProtectFile(p,o, r,w,e,t);
	if (!rt) {
		return rt.GetCode();
	}

	*outPath = helper::allocStrInComMem(o);

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ProtectFileToProject(int projectId,
	HANDLE hUser,
	const wchar_t* path,
	int* pRights, int len,
	WaterMark watermark, Expiration expiration,
	const wchar_t* tags,
	const wchar_t** outPath) 
{
	OutputDebugStringA("call SDWL_User_ProtectFile for Proejct\n");
	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (path == NULL || outPath== NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	// find membership id by projectId,
	auto mId = g_User->GetMembershipID(projectId);
	if (mId.empty() || mId.length() < 5) {
		// invalid project membership id
		OutputDebugStringA("Error in porject_protect_file can not find proper membershipId by projectID user provided\n");
		return SDWL_INTERNAL_ERROR;
	}


	std::wstring p(path);
	std::vector<SDRmFileRight> r;
	for (int i = 0; i < len; i++) {
		SDRmFileRight l = (SDRmFileRight)pRights[i];
		r.push_back(l);
	}
	SDR_WATERMARK_INFO w;
	{
		w.text = helper::utf162utf8(watermark.text);
		w.fontName = helper::utf162utf8(watermark.fontName);
		w.fontColor = helper::utf162utf8(watermark.fontColor);
		w.fontSize = watermark.fontSize;
		w.transparency = watermark.transparency;
		w.rotation = (WATERMARK_ROTATION)watermark.rotation;
		w.repeat = watermark.repeat;
	}
	SDR_Expiration e;
	{
		e.type = (IExpiryType)expiration.type;
		e.start = expiration.start;
		e.end = expiration.end;
	}
	std::string t(helper::utf162utf8(tags));
	std::wstring o;
	auto rt = g_User->ProtectFile(p,o,r, w, e, t,mId);
	if (!rt) {
		return rt.GetCode();
	}

	*outPath = helper::allocStrInComMem(o);

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_UpdateRecipients(HANDLE hUser,
	HANDLE hNxlFile,
	const wchar_t* addmails[],int lenaddmails,
	const wchar_t* delmails[],int lendelmails)
{
	OutputDebugStringA("call SDWL_User_UpdateRecipients\n");
	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	
	ISDRmNXLFile *pf = (ISDRmNXLFile *)hNxlFile;
	

	// prepare param
	std::vector<std::string> adds;
	std::vector<std::string> dels;

	for (int i = 0; i < lenaddmails; i++) {
		adds.push_back(helper::utf162utf8(addmails[i]));
	}

	for (int i = 0; i < lendelmails; i++) {
		dels.push_back(helper::utf162utf8(delmails[i]));
	}

	// call

	auto rt = g_User->UpdateRecipients(pf, adds);
	if (!rt) {
		return rt.GetCode();
		
	}	

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_UpdateRecipients2(
			HANDLE hUser, 
			const wchar_t * nxlFilePath, 
			const wchar_t * addmails[], int lenaddmails,
			const wchar_t * delmails[], int lendelmails)
{
	OutputDebugStringA("call SDWL_User_UpdateRecipients\n");
	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile * pfile = NULL;
	auto rt=g_User->OpenFile(nxlFilePath, &pfile);
	if (!rt) {
		if (pfile != NULL) {
			g_User->CloseFile(pfile);
		}
		return rt.GetCode();
	}

	DWORD rt2 = SDWL_User_UpdateRecipients(hUser, (HANDLE)pfile, addmails, lenaddmails, delmails, lendelmails);
	if (pfile != NULL) {
		g_User->CloseFile(pfile);
	}
	return rt2;
}

NXSDK_API DWORD SDWL_User_GetRecipients(
			HANDLE hUser, HANDLE hNxlFile, 
			wchar_t ** emails, int * peSize,
			wchar_t ** addEmials, int * paeSize,
			wchar_t ** removEmails, int * preSize)
{
	OutputDebugStringA("call SDWL_User_UpdateRecipients\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile *pf = (ISDRmNXLFile *)hNxlFile;
	
	if (!emails || !peSize ||
		!addEmials || !paeSize ||
		!removEmails || !preSize) {
		return SDWL_INTERNAL_ERROR;
	}

	// prepare params
	std::vector<std::string> totalE;
	std::vector<std::string> addE;
	std::vector<std::string> reE;

	*emails = NULL, *peSize = 0;
	*addEmials = NULL, *paeSize = 0;
	*removEmails = NULL, *preSize = 0;
	
	auto rt = g_User->GetRecipients(pf, totalE, addE, reE);
	if (!rt) {
		return rt.GetCode();
	}
	
	// prepare out params
	if (totalE.size() > 0) {
		*peSize = (int)totalE.size();
		// prepare Mem
		wchar_t** pbuf = (wchar_t**)::CoTaskMemAlloc(*peSize * sizeof(wchar_t*));
		for (size_t i = 0; i < totalE.size(); i++) {
			std::wstring s = helper::utf82utf16(totalE.at(i));
			pbuf[i] = (wchar_t*)::CoTaskMemAlloc((s.size()+1) * sizeof(wchar_t));
			wcscpy_s(pbuf[i], s.size() + 1, s.c_str());
		}
		*emails = (wchar_t*)pbuf;
	}
	if (addE.size() > 0) {
		*paeSize = (int)addE.size();
		// prepare Mem
		wchar_t** pbuf = (wchar_t**)::CoTaskMemAlloc(*peSize * sizeof(wchar_t*));
		for (size_t i = 0; i < addE.size(); i++) {
			std::wstring s = helper::utf82utf16(addE.at(i));
			pbuf[i] = (wchar_t*)::CoTaskMemAlloc((s.size() + 1) * sizeof(wchar_t));
			wcscpy_s(pbuf[i], s.size() + 1, s.c_str());
		}
		*addEmials = (wchar_t*)pbuf;
	}
	if (reE.size() > 0 ) {
		*preSize = (int)reE.size();
		// prepare Mem
		wchar_t** pbuf = (wchar_t**)::CoTaskMemAlloc(*peSize * sizeof(wchar_t*));
		for (size_t i = 0; i < reE.size(); i++) {
			std::wstring s = helper::utf82utf16(reE.at(i));
			pbuf[i] = (wchar_t*)::CoTaskMemAlloc((s.size() + 1) * sizeof(wchar_t));
			wcscpy_s(pbuf[i], s.size() + 1, s.c_str());
		}
		*removEmails = (wchar_t*)pbuf;
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetRecipients2(
		HANDLE hUser, const wchar_t * nxlFilePath, 
		wchar_t ** emails, int * peSize,
		wchar_t ** addEmials, int * paeSize,
		wchar_t ** removEmails, int * preSize)
{
	OutputDebugStringA("call SDWL_User_UpdateRecipients\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile * pfile = NULL;
	auto rt = g_User->OpenFile(nxlFilePath, &pfile);
	if (!rt) {
		if (pfile != NULL) {
			g_User->CloseFile(pfile);
		}
		return rt.GetCode();
	}

	DWORD rt2 = SDWL_User_GetRecipients(hUser, (HANDLE)pfile, emails, peSize, addEmials, paeSize, removEmails, preSize);
	if (pfile != NULL) {
		g_User->CloseFile(pfile);
	}

	return rt2;
}

NXSDK_API DWORD SDWL_User_GetRecipents3(
	HANDLE hUser, const wchar_t* nxlFilePath,
	wchar_t** emails, wchar_t** addEmails, wchar_t** removeEmails) {
	OutputDebugStringA("call SDWL_User_GetRecipients3\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	ISDRmNXLFile * pfile = NULL;
	auto rt = g_User->OpenFile(nxlFilePath, &pfile);
	if (!rt) {
		if (pfile) {
			g_User->CloseFile(pfile);
			pfile = NULL;
		}
		return rt.GetCode();
	}

	std::vector<std::string> totalE;
	std::vector<std::string> addE;
	std::vector<std::string> removeE;
	auto ret = g_User->GetRecipients(pfile, totalE, addE, removeE);
	if (!ret) {
		if (pfile) {
			g_User->CloseFile(pfile);
			pfile = NULL;
		}
		return rt.GetCode();
	}

	//Release nxl file handle.
	if (pfile) {
		g_User->CloseFile(pfile);
		pfile = NULL;
	}

	std::string recipents;
	std::string recipentsAdd;
	std::string recipentsRemove;

	//Parse recipents email.
	std::for_each(totalE.begin(), totalE.end(), [&recipents](std::string e) {
		recipents.append(e).append(";");
	});
	//Parse recipents added email.
	std::for_each(addE.begin(), addE.end(), [&recipentsAdd](std::string e) {
		recipentsAdd.append(e).append(";");
	});
	//Parse recipents removed email.
	std::for_each(removeE.begin(), removeE.end(), [&recipentsRemove](std::string e) {
		recipentsRemove.append(e).append(";");
	});

	//Prepare data.
	{
		*emails = helper::allocStrInComMem(helper::utf82utf16(recipents));
		*addEmails = helper::allocStrInComMem(helper::utf82utf16(recipentsAdd));
		*removeEmails = helper::allocStrInComMem(helper::utf82utf16(recipentsRemove));
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_UploadFile(HANDLE hUser, const wchar_t* nxlFilePath, const wchar_t* sourcePath, const wchar_t* recipients, const wchar_t* comments)
{
	OutputDebugStringA("call SDWL_User_UploadFile\n");
	// sanity check
	if (g_User == NULL || nxlFilePath == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	// call
	auto rt = g_User->UploadFile(nxlFilePath, sourcePath, recipients, comments);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_UploadMyVaultFile(HANDLE hUser, const wchar_t * nxlFilePath, const wchar_t* sourcePath, const wchar_t* recipients, const wchar_t* comments)
{
	OutputDebugStringA("call SDWL_User_UploadMyVaultFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	DWORD rt2 = SDWL_User_UploadFile(hUser, nxlFilePath, sourcePath, recipients, comments);

	return rt2;
}

NXSDK_API DWORD SDWL_User_ListMyVaultAllFiles(HANDLE hUser,
	const wchar_t * orderBy, const wchar_t * searchString,
	MyVaultFileInfo ** ppFiles, uint32_t * psize)
{
	OutputDebugStringA("call SDWL_User_ListMyVaultAllFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	std::vector<SDR_MYVAULT_FILE_INFO> vec;
	std::vector<SDR_MYVAULT_FILE_INFO> pageVec;
	uint32_t pageId = 1;
	uint32_t pageSize = 1000;
	do
	{
		pageVec.clear();

		auto rt = g_User->GetMyVaultFiles(pageId, pageSize,
			helper::utf162utf8(orderBy),
			helper::utf162utf8(searchString),
			pageVec);
		if (!rt) {
			return rt.GetCode();
		}

		for (auto i : pageVec)
		{
			vec.push_back(i);
		}

	} while (pageVec.size() == pageSize && pageId++);

	auto size = vec.size();
	*psize = (uint32_t)size;
	if (*psize == 0) {
		*ppFiles = NULL;
		return SDWL_SUCCESS;
	}

	//
	MyVaultFileInfo* p = (MyVaultFileInfo*)::CoTaskMemAlloc(sizeof(MyVaultFileInfo)*size);

	for (size_t i = 0; i < size; i++)
	{
		SDR_MYVAULT_FILE_INFO pif = vec[i];

		p[i].pathId = helper::allocStrInComMem(helper::utf82utf16(pif.m_pathid));
		p[i].displayPath = helper::allocStrInComMem(helper::utf82utf16(pif.m_pathdisplay));
		p[i].repoId = helper::allocStrInComMem(helper::utf82utf16(pif.m_repoid));
		p[i].duid = helper::allocStrInComMem(helper::utf82utf16(pif.m_duid));
		p[i].nxlName = helper::allocStrInComMem(helper::utf82utf16(pif.m_nxlname));
		//
		p[i].lastModifiedTime = pif.m_lastmodified;
		p[i].creationTime = pif.m_creationtime;
		p[i].sharedTime = pif.m_sharedon;
		p[i].sharedWithList = helper::allocStrInComMem(helper::utf82utf16(pif.m_sharedwith));
		p[i].size = pif.m_size;
		//
		p[i].is_deleted = pif.m_bdeleted;
		p[i].is_revoked = pif.m_brevoked;
		p[i].is_shared = pif.m_bshared;
		//
		p[i].source_repo_type = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcerepotype));
		p[i].source_file_displayPath = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcefilepathdisplay));
		p[i].source_file_pathId = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcefilepathid));
		p[i].source_repo_name = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcereponame));
		p[i].source_repo_id = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcerepoid));

	}
	*ppFiles = p;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ListMyVaultFiles(HANDLE hUser, 
	uint32_t pageId, uint32_t pageSize, 
	const wchar_t * orderBy, const wchar_t * searchString,
	MyVaultFileInfo ** ppFiles, uint32_t * psize)
{
	OutputDebugStringA("call SDWL_User_ListMyVaultFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	std::vector<SDR_MYVAULT_FILE_INFO> vec;
	auto rt = g_User->GetMyVaultFiles(pageId, pageSize, 
		helper::utf162utf8(orderBy), 
		helper::utf162utf8(searchString), 
		vec);
	if (!rt) {
		return rt.GetCode();
	}
	auto size = vec.size();
	*psize = (uint32_t)size;
	if (size == 0) {
		*ppFiles = NULL;
		return SDWL_SUCCESS;
	}

	//
	MyVaultFileInfo* p = (MyVaultFileInfo*)::CoTaskMemAlloc(sizeof(MyVaultFileInfo)*size);
	
	for (size_t i = 0; i < size; i++)
	{
		SDR_MYVAULT_FILE_INFO pif = vec[i];

		p[i].pathId = helper::allocStrInComMem(helper::utf82utf16(pif.m_pathid));
		p[i].displayPath = helper::allocStrInComMem(helper::utf82utf16(pif.m_pathdisplay));
		p[i].repoId = helper::allocStrInComMem(helper::utf82utf16(pif.m_repoid));
		p[i].duid = helper::allocStrInComMem(helper::utf82utf16(pif.m_duid));
		p[i].nxlName = helper::allocStrInComMem(helper::utf82utf16(pif.m_nxlname));
		//
		p[i].lastModifiedTime = pif.m_lastmodified;
		p[i].creationTime = pif.m_creationtime;
		p[i].sharedTime = pif.m_sharedon;
		p[i].sharedWithList = helper::allocStrInComMem(helper::utf82utf16(pif.m_sharedwith));
		p[i].size = pif.m_size;
		//
		p[i].is_deleted = pif.m_bdeleted;
		p[i].is_revoked = pif.m_brevoked;
		p[i].is_shared = pif.m_bshared;
		//
		p[i].source_repo_type = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcerepotype));
		p[i].source_file_displayPath = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcefilepathdisplay));
		p[i].source_file_pathId = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcefilepathid));
		p[i].source_repo_name = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcereponame));
		p[i].source_repo_id = helper::allocStrInComMem(helper::utf82utf16(pif.m_sourcerepoid));

	}
	*ppFiles = p;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_DownloadMyVaultFiles(HANDLE hUser, 
	const wchar_t * rmsFilePathId,
	const wchar_t * downloadLocalFolder, 
	Type_DownlaodFile type, 
	const wchar_t** outPath)
{
	OutputDebugStringA("call SDWL_User_DownloadMyVaultFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (rmsFilePathId == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (downloadLocalFolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	auto utf8Pathid = helper::utf162utf8(rmsFilePathId);
	std::wstring destfolder = downloadLocalFolder;
	auto rt = g_User->MyVaultDownloadFile(utf8Pathid, destfolder, type);
	if (!rt) {
		return rt.GetCode();
	}

	*outPath = helper::allocStrInComMem(destfolder);

	return SDWL_SUCCESS;

}

NXSDK_API DWORD SDWL_User_DownloadMyVaultPartialFiles(HANDLE hUser,
	const wchar_t * rmsFilePathId,
	const wchar_t * downloadLocalFolder,
	Type_DownlaodFile type,
	const wchar_t** outPath)
{
	OutputDebugStringA("call SDWL_User_DownloadMyVaultPartialFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (rmsFilePathId == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (downloadLocalFolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	auto utf8Pathid = helper::utf162utf8(rmsFilePathId);
	std::wstring destfolder = downloadLocalFolder;
	auto rt = g_User->MyVaultDownloadPartialFile(utf8Pathid, destfolder, type);
	if (!rt) {
		return rt.GetCode();
	}

	*outPath = helper::allocStrInComMem(destfolder);

	return SDWL_SUCCESS;

}

NXSDK_API DWORD SDWL_User_OpenFile(HANDLE hUser,
	const wchar_t* nxl_path,
	HANDLE* hNxlFile )
{
	OutputDebugStringA("call SDWL_User_OpenFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxl_path == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	ISDRmNXLFile* p = NULL;
	auto rt = g_User->OpenFile(std::wstring(nxl_path), &p);
	if (!rt) {
		return rt.GetCode();
	}

	//set value
	*hNxlFile = (HANDLE)p;	

	//// Open file successfully, set the token to RPM also
	//if (g_RmsIns->IsRPMDriverExist())
	//{
	//	g_RmsIns->CacheRPMFileToken(nxl_path);
	//}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_CacheRPMFileToken(HANDLE hUser,
	const wchar_t* nxl_path)
{
	OutputDebugStringA("call SDWL_User_CacheRPMFileToken\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (nxl_path == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	//// Open file successfully, set the token to RPM also
	//if (g_RmsIns->IsRPMDriverExist())
	//{
	//	g_RmsIns->CacheRPMFileToken(nxl_path);
	//}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_CloseFile(HANDLE hUser, HANDLE hNxlFile)
{
	OutputDebugStringA("call SDWL_User_CloseFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;
	auto rt = g_User->CloseFile(pF);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ForceCloseFile(HANDLE hUser, const wchar_t * nxl_path)
{
	OutputDebugStringA("call SDWL_User_ForceCloseFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxl_path == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	ISDRmNXLFile *pFile = NULL;
	auto rt = g_User->OpenFile(nxl_path, &pFile);
	if (pFile != NULL) {
		g_User->CloseFile(pFile);
	}
	return rt;
}

NXSDK_API DWORD SDWL_User_DecryptNXLFile(HANDLE hUser, 
	HANDLE hNxlFile, 
	const wchar_t* path)
{
	OutputDebugStringA("call SDWL_User_DecryptFile\n");
	//// sanity check
	//if (g_User == NULL) {
	//	// not found
	//	return SDWL_INTERNAL_ERROR;
	//}
	//if ((ISDRmUser*)hUser != g_User) {
	//	return SDWL_INTERNAL_ERROR;
	//}
	//if (hNxlFile == NULL) {
	//	return SDWL_INTERNAL_ERROR;
	//}
	//if (path == NULL) {
	//	return SDWL_INTERNAL_ERROR;
	//}

	//ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;

	//auto rt = g_User->DecryptNXLFile(pF, std::wstring(path), RLOView);
	//if (!rt) {
	//	return rt.GetCode();
	//}
		
	return SDWL_ACCESS_DENIED;
}

NXSDK_API DWORD SDWL_User_UploadActivityLogs(HANDLE hUser)
{
	OutputDebugStringA("call SDWL_User_UploadActivityLogs\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	// call
	//auto rt = g_User->UploadActivityLogs();
	//if (!rt) {
	//	return rt.GetCode();
	//}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetHeartBeatInfo(HANDLE hUser)
{
	OutputDebugStringA("call SDWL_User_GetHeartBeatInfo\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	auto rt = g_User->GetHeartBeatInfo();
	if (!rt) {
		return rt.GetCode();
	}
	


	// Do heartbeat to RPM
	if (g_RmsIns->IsRPMDriverExist())
	{
		//g_RmsIns->SetRPMPolicyBundle();
		g_RmsIns->SyncUserAttributes();
	}
	return SDWL_SUCCESS;
}

//NXSDK_API DWORD SDWL_User_GetPolicyBundle(HANDLE hUser, char * tenantName, char ** ppPolicyBundle)
//{
//	OutputDebugStringA("call SDWL_User_GetPolicyBundle\n");
//	// sanity check
//	if (g_User == NULL) {
//		// not found
//		return SDWL_INTERNAL_ERROR;
//	}
//	if ((ISDRmUser*)hUser != g_User) {
//		return SDWL_INTERNAL_ERROR;
//	}
//
//	if (tenantName == NULL || strlen(tenantName) < 5) {
//		return SDWL_INTERNAL_ERROR;
//	}
//
//	// do
//	std::string bundle;
//	auto rt = g_User->GetPolicyBundle(helper::ansi2utf16(tenantName), bundle);
//	if (!rt) {
//		return SDWL_INTERNAL_ERROR;
//	}
//	*ppPolicyBundle = helper::allocStrInComMem(bundle);
//	return SDWL_SUCCESS;
//}

NXSDK_API DWORD SDWL_User_GetWaterMarkInfo(HANDLE hUser, WaterMark * pWaterMark)
{
	OutputDebugStringA("call SDWL_User_GetWaterMarkInfo\n");
	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	SDR_WATERMARK_INFO wm = g_User->GetWaterMarkInfo();
	WaterMark* buf = pWaterMark;
	{
		buf->text = helper::allocStrInComMem(helper::utf82utf16( wm.text));
		buf->fontName = helper::allocStrInComMem(helper::utf82utf16(wm.fontName));
		buf->fontColor = helper::allocStrInComMem(helper::utf82utf16(wm.fontColor));
		buf->repeat = wm.repeat;
		buf->fontSize = wm.fontSize;
		buf->rotation = wm.rotation;
		buf->transparency = wm.transparency;
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetHeartBeatFrequency(HANDLE hUser, uint32_t * nSeconds)
{
	OutputDebugStringA("call SDWL_User_GetHeartBeatFrequency\n");
	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	int minutes=g_User->GetHeartBeatFrequency();
	// server will return minutes as the unity, change it to seconds.
	*nSeconds = minutes * 60;
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetProjectsInfo(HANDLE hUser, 
	ProjtectInfo ** pProjects, int * pSize)
{
	OutputDebugStringA("call SDWL_User_GetProjectsInfo\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (pProjects == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (pSize == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	auto vec = g_User->GetProjectsInfo();
	int size = (int)vec.size();
	*pSize = size;

	if (!size) {
		*pProjects = NULL;
		return SDWL_SUCCESS;
	}

	ProjtectInfo* p = (ProjtectInfo*)::CoTaskMemAlloc(size * sizeof(ProjtectInfo));

	for (int i = 0; i < size; i++) {
		SDR_PROJECT_INFO pif = vec[i];
		p[i].id = pif.projid;
		p[i].name = helper::allocStrInComMem(helper::utf82utf16(pif.name));
		p[i].displayname = helper::allocStrInComMem(helper::utf82utf16(pif.displayname));
		p[i].description = helper::allocStrInComMem(helper::utf82utf16(pif.description));
		p[i].owner = pif.bowner;
		p[i].totalfiles = pif.totalfiles;
		p[i].tenantid = helper::allocStrInComMem(helper::utf82utf16(pif.tokengroupname));
		// get isEnabledAdhoc
		bool isEnabledAhodc = true;
		SDWL_User_CheckProjectEnableAdhoc(hUser, NULL, &isEnabledAhodc);
		p[i].isEnableAdhoc = isEnabledAhodc == true ? 1 : 0;
	}
	*pProjects = p;
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_CheckProjectEnableAdhoc(HANDLE hUser, const wchar_t * projectTenandId, bool * isEnable)
{
	OutputDebugStringA("call SDWL_User_CheckProjectEnableAdhoc\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (hUser == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (isEnable == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	int heartbeatNoUse=0;

	std::string tenantId = projectTenandId==NULL?std::string():helper::utf162utf8(projectTenandId);
	std::string _systemProjectTenantId = "";
	int _systemProjectId = 0;

	auto rt=g_User->GetTenantPreference(isEnable, &heartbeatNoUse, &_systemProjectId, _systemProjectTenantId, tenantId);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

// by osmond, this is almost same as SDWL_User_CheckProjectEnableAdhoc, special designed for 
NXSDK_API DWORD SDWL_User_CheckSystemBucketEnableAdhoc(HANDLE hUser, bool * isEnable)
{
	OutputDebugStringA("call SDWL_User_CheckSystemBucketEnableAdhoc\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (hUser == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (isEnable == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	int heartbeatNoUse = 0;
	std::string tenantId = std::string();
	std::string _systemProjectTenantId = "";
	int _systemProjectId = 0;


	auto rt = g_User->GetTenantPreference(isEnable, &heartbeatNoUse, &_systemProjectId, _systemProjectTenantId, tenantId);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;

}

NXSDK_API DWORD SDWL_User_CheckSystemProject(HANDLE hUser, const wchar_t * projectTenandId, int * systemProjectId, const wchar_t** systemProjectTenandId)
{
	OutputDebugStringA("call SDWL_User_CheckSystemProject\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (hUser == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (systemProjectId == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	int heartbeatNoUse = 0;

	std::string tenantId = projectTenandId == NULL ? std::string() : helper::utf162utf8(projectTenandId);
	std::string _systemProjectTenantId = "";
	int _systemProjectId = 0;

	bool isEnable;
	auto rt = g_User->GetTenantPreference(&isEnable, &heartbeatNoUse, &_systemProjectId, _systemProjectTenantId, tenantId);
	if (!rt) {
		return rt.GetCode();
	}

	// if SDK return 0 for system project id, let's try to read from registry
	if (_systemProjectId <= 0 && _systemProjectTenantId.size() == 0)
	{
		DWORD dwSysProjectID = 0;
		std::wstring strSysProjectTenantID;
		HKEY hKey;
		if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\NextLabs\\SkyDRM", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
		{
			helper::GetDWORDRegKey(hKey, L"SysProjectID", dwSysProjectID, 0);
			helper::GetStringRegKey(hKey, L"SysProjectTenantID", strSysProjectTenantID, L"");

			_systemProjectId = dwSysProjectID;
			_systemProjectTenantId = helper::utf162utf8(strSysProjectTenantID);
			RegCloseKey(hKey);
		}
	}

	*systemProjectId = _systemProjectId;
	*systemProjectTenandId = helper::allocStrInComMem(helper::utf82utf16(_systemProjectTenantId));

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_CheckInPlaceProtection(HANDLE hUser, const wchar_t * projectTenandId, bool * DeleteSource)
{
	OutputDebugStringA("call SDWL_User_CheckSystemProject\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (hUser == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (DeleteSource == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	bool isDeleteSource = false;

	HKEY hKey;
	if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\NextLabs\\SkyDRM", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
	{
		helper::GetBoolRegKey(hKey, L"DeleteSource", isDeleteSource, false);
		RegCloseKey(hKey);
	}

	*DeleteSource = isDeleteSource;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetListProjtects(HANDLE hUser, 
	uint32_t pagedId, uint32_t pageSize, 
	const wchar_t * orderBy, ProjtectFilter filter)
{
	OutputDebugStringA("call SDWL_User_GetListProjtects\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	auto rt = g_User->GetListProjects(pagedId, pageSize, 
		 helper::utf162utf8(orderBy), 
		(RM_ProjectFilter)filter);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ProjectDownloadFile(HANDLE hUser, 
	uint32_t projectId, 
	const wchar_t * pathId, 
	const wchar_t * downloadPath, 
	Type_DownlaodFile type, const wchar_t** outPath)
{
	OutputDebugStringA("call SDWL_User_ProjectDownloadFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (pathId == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (downloadPath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (outPath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	//
	auto rmsPath = helper::utf162utf8(pathId);

	std::wstring destfolder = downloadPath;
	auto rt = g_User->ProjectDownloadFile(projectId, rmsPath, destfolder, (RM_ProjectFileDownloadType)type);
	if (!rt) {
		return rt.GetCode();
	}
	*outPath = helper::allocStrInComMem(destfolder);

	return SDWL_SUCCESS;

}

NXSDK_API DWORD SDWL_User_ProjectDownloadPartialFile(HANDLE hUser,
	uint32_t projectId,
	const wchar_t * pathId,
	const wchar_t * downloadPath,
	Type_DownlaodFile type, const wchar_t** outPath)
{
	OutputDebugStringA("call SDWL_User_ProjectDownloadPartialFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (pathId == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (downloadPath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	//
	auto rmsPath = helper::utf162utf8(pathId);

	std::wstring destfolder = downloadPath;
	auto rt = g_User->ProjectDownloadPartialFile(projectId, rmsPath, destfolder, (RM_ProjectFileDownloadType)type);
	if (!rt) {
		return rt.GetCode();
	}
	*outPath = helper::allocStrInComMem(destfolder);

	return SDWL_SUCCESS;

}

NXSDK_API DWORD SDWL_User_ProjectListAllFiles(HANDLE hUser,
	uint32_t projectId,
	const wchar_t * orderby,
	const wchar_t * pathId,
	const wchar_t * searchStr,
	ProjectFileInfo ** pplistFiles,
	uint32_t* plistSize)
{
	OutputDebugStringA("call SDWL_User_ProjectListAllFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	// For fix bug 52592 that the full path contains Chinese character folder.
	std::string encodedPathId = helper::UrlEncode(helper::utf162utf8(pathId));

	std::vector<SDR_PROJECT_FILE_INFO> vec;
	std::vector<SDR_PROJECT_FILE_INFO> pageVec;
	uint32_t pagedId = 1;
	uint32_t pageSize = 1000;
	do
	{
		pageVec.clear();

		auto rt = g_User->GetProjectListFiles(projectId, pagedId, pageSize,
			helper::utf162utf8(orderby),
			encodedPathId,
			helper::utf162utf8(searchStr), pageVec);
		if (!rt) {
			return rt.GetCode();
		}

		for (auto i : pageVec)
		{
			vec.push_back(i);
		}
	} while (pageVec.size() == pageSize && pagedId++);

	auto size = vec.size();
	*plistSize = (uint32_t)size;
	if (size == 0) {
		*pplistFiles = NULL;
		return SDWL_SUCCESS;
	}

	ProjectFileInfo* p = (ProjectFileInfo*)::CoTaskMemAlloc(size * sizeof(ProjectFileInfo));
	for (int i = 0; i < size; i++) {
		auto pif = vec[i];
		p[i].id = helper::allocStrInComMem(helper::utf82utf16(pif.m_fileid));
		p[i].duid = helper::allocStrInComMem(helper::utf82utf16(pif.m_duid));
		p[i].displayPath = helper::allocStrInComMem(helper::utf82utf16(pif.m_pathdisplay));
		p[i].pathId = helper::allocStrInComMem(helper::utf82utf16(pif.m_pathid));
		p[i].nxlName = helper::allocStrInComMem(helper::utf82utf16(pif.m_nxlname));
		// new added , as RMS defined,each timestamp will use millseconds,but we will use second
		p[i].lastModifiedTime = pif.m_lastmodified;
		p[i].creationTime = pif.m_created;
		p[i].filesize = pif.m_size;
		p[i].isFolder = pif.m_bfolder;
		p[i].ownerId = pif.m_ownerid;
		p[i].ownerDisplayName = helper::allocStrInComMem(helper::utf82utf16(pif.m_ownerdisplayname));
		p[i].ownerEmail = helper::allocStrInComMem(helper::utf82utf16(pif.m_owneremail));
	}
	*pplistFiles = p;
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ProjectListFiles(HANDLE hUser,
	uint32_t projectId, 
	uint32_t pagedId, 
	uint32_t pageSize, 
	const wchar_t * orderby, 
	const wchar_t * pathId,
	const wchar_t * searchStr,
	ProjectFileInfo ** pplistFiles, 
	uint32_t* plistSize)
{
	OutputDebugStringA("call SDWL_User_ProjectListFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	// For fix bug 52592 that the full path contains Chinese character folder.
	std::string encodedPathId = helper::UrlEncode(helper::utf162utf8(pathId));

	std::vector<SDR_PROJECT_FILE_INFO> vec;

	auto rt = g_User->GetProjectListFiles(projectId, pagedId, pageSize, 
		helper::utf162utf8(orderby),
		encodedPathId, 
		helper::utf162utf8(searchStr), vec);
	if (!rt) {
		return rt.GetCode();
	}

	int size = (int)vec.size();
	*plistSize = (uint32_t)size;
	if (size == 0) {
		*pplistFiles = NULL;
		return SDWL_SUCCESS;
	}

	ProjectFileInfo* p = (ProjectFileInfo*)::CoTaskMemAlloc(size * sizeof(ProjectFileInfo));
	for (int i = 0; i < size; i++) {
		auto pif = vec[i];
		p[i].id = helper::allocStrInComMem(helper::utf82utf16(pif.m_fileid));
		p[i].duid = helper::allocStrInComMem(helper::utf82utf16(pif.m_duid));
		p[i].displayPath = helper::allocStrInComMem(helper::utf82utf16(pif.m_pathdisplay));
		p[i].pathId = helper::allocStrInComMem(helper::utf82utf16(pif.m_pathid));
		p[i].nxlName = helper::allocStrInComMem(helper::utf82utf16(pif.m_nxlname));
		// new added , as RMS defined,each timestamp will use millseconds,but we will use second
		p[i].lastModifiedTime = pif.m_lastmodified;
		p[i].creationTime = pif.m_created;
		p[i].filesize = pif.m_size;
		p[i].isFolder = pif.m_bfolder;
		p[i].ownerId = pif.m_ownerid;
		p[i].ownerDisplayName= helper::allocStrInComMem(helper::utf82utf16(pif.m_ownerdisplayname));
		p[i].ownerEmail= helper::allocStrInComMem(helper::utf82utf16(pif.m_owneremail));
	}
	*pplistFiles = p;
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ProjectUploadFile(
	HANDLE hUser, 
	uint32_t projectid, 
	const wchar_t * rmsDestFolder, 
	HANDLE hNxlFile)
{
	OutputDebugStringA("call SDWL_User_ProjectUploadFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (rmsDestFolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile *pf = (ISDRmNXLFile *)hNxlFile;
	auto rt = g_User->UploadProjectFile(projectid, rmsDestFolder, pf, false);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ProjectUploadFile2(
	HANDLE hUser, 
	uint32_t projectid, 
	const wchar_t * rmsDestFolder, 
	const wchar_t * nxlFilePath)
{
	OutputDebugStringA("call SDWL_User_ProjectUploadFile2\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (rmsDestFolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	// open the file first
	ISDRmNXLFile *pFile = NULL;
	auto rt = g_User->OpenFile(nxlFilePath, &pFile);
	if (!rt) {
		if (pFile != NULL) {
			g_User->CloseFile(pFile);
		}
		return rt.GetCode();
	}

	DWORD rt2 = SDWL_User_ProjectUploadFile(hUser, projectid, rmsDestFolder, (HANDLE)pFile);
	if (pFile != NULL) {
		g_User->CloseFile(pFile);
	}

	return rt2;
}

NXSDK_API DWORD SDWL_User_Inner_UploadEditedProjectFile(
	HANDLE hUser,
	uint32_t projectid,
	const wchar_t * rmsDestFolder,
	HANDLE hNxlFile)
{
	OutputDebugStringA("call SDWL_User_ProjectUploadFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (rmsDestFolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile *pf = (ISDRmNXLFile *)hNxlFile;
	auto rt = g_User->UploadProjectFile(projectid, rmsDestFolder, pf, true);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}


NXSDK_API DWORD UploadEditedProjectFile(HANDLE hUser, uint32_t projectid, const wchar_t * rmsDestFolder, const wchar_t * nxlFilePath)
{
	OutputDebugStringA("call UploadEditedProjectFile\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if ((ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (rmsDestFolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	// open the file first
	ISDRmNXLFile *pFile = NULL;
	auto rt = g_User->OpenFile(nxlFilePath, &pFile);
	if (!rt) {
		if (pFile != NULL) {
			g_User->CloseFile(pFile);
		}
		return rt.GetCode();
	}

	DWORD rt2 = SDWL_User_Inner_UploadEditedProjectFile(hUser, projectid, rmsDestFolder, (HANDLE)pFile);
	if (pFile != NULL) {
		g_User->CloseFile(pFile);
	}

	return rt2;
}




#pragma endregion

#pragma region LocalFiles

NXSDK_API DWORD SDWL_User_ProjectClassifacation(
	HANDLE hUser, 
	const wchar_t * tenantid, 
	ProjectClassifacationLables ** ppProjectClassifacationLables,
	uint32_t* pSize)
{
	OutputDebugStringA("call SDWL_User_ProjectClassifacation\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (tenantid == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	std::wstring wstrTenantID = tenantid;
	if (helper::utf162utf8(wstrTenantID) == g_User->GetSystemProjectTenantId())
	{
		wstrTenantID = helper::utf82utf16(g_User->GetDefaultTokenGroupName());
	}
	// do
	std::vector<SDR_CLASSIFICATION_CAT> cats;
	auto rt = g_User->GetClassification(
		helper::utf162utf8(wstrTenantID),
		cats);
	if (!rt) {
		return rt.GetCode();
	}

	int size = (int)cats.size();
	*pSize = (uint32_t)size;
	if (size == 0) {
		*pSize = NULL;
		return SDWL_SUCCESS;
	}


	// convert cats into ProjectClassifacationLables;
	ProjectClassifacationLables *p = (ProjectClassifacationLables*)::CoTaskMemAlloc(size* sizeof(ProjectClassifacationLables));
	for (auto i = 0; i < size; i++) {
		auto pif = cats[i];
		p[i].name = helper::allocStrInComMem(helper::utf82utf16(pif.name));
		p[i].multiseclect = pif.multiSelect;
		p[i].mandatory = pif.mandatory;
		//
		std::string tmp_lables,tmp_defautls;
		for (auto j = 0; j < pif.labels.size(); j++) {
			tmp_lables += pif.labels[j].name + ";";
			if (pif.labels[j].allow) {
				tmp_defautls += "1;";
			}
			else {
				tmp_defautls += "0;";
			}
		}
		p[i].labels = helper::allocStrInComMem(helper::utf82utf16(tmp_lables));
		p[i].isdefaults = helper::allocStrInComMem(helper::utf82utf16(tmp_defautls));
	}
	*ppProjectClassifacationLables = p;
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ListSharedWithMeAllFiles(HANDLE hUser,
	const wchar_t * orderBy,
	const wchar_t * searchString,
	SharedWithMeFileInfo ** ppFiles,
	uint32_t* pSize)
{
	OutputDebugStringA("call SDWL_User_ListSharedWithMeAllFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	std::vector<SDR_SHAREDWITHME_FILE_INFO> vec;
	std::vector<SDR_SHAREDWITHME_FILE_INFO> pageVec;
	uint32_t pageId = 1;
	uint32_t pageSize = 1000;
	do
	{
		pageVec.clear();

		auto rt = g_User->GetSharedWithMeFiles(pageId, pageSize,
			helper::utf162utf8(orderBy),
			helper::utf162utf8(searchString),
			pageVec);

		if (!rt) {
			return rt.GetCode();
		}

		for (auto i : pageVec)
		{
			vec.push_back(i);
		}

	} while (pageVec.size() == pageSize && pageId++);

	auto size = vec.size();
	*pSize = (uint32_t)size;
	if (size == 0) {
		*ppFiles = NULL;
		return SDWL_SUCCESS;
	}

	// convert it to c-like struct and encapsule into COM-mem for c# used
	SharedWithMeFileInfo* p = (SharedWithMeFileInfo*)::CoTaskMemAlloc(size * sizeof(SharedWithMeFileInfo));

	for (size_t i = 0; i < size; i++)
	{
		auto j = vec[i];
		p[i].duid = helper::allocStrInComMem(helper::utf82utf16(j.m_duid));
		p[i].nxlName = helper::allocStrInComMem(helper::utf82utf16(j.m_nxlname));
		p[i].fileType = helper::allocStrInComMem(helper::utf82utf16(j.m_filetype));
		p[i].sharedbyWhoEmail = helper::allocStrInComMem(helper::utf82utf16(j.m_sharedby));
		p[i].transactionId = helper::allocStrInComMem(helper::utf82utf16(j.m_transactionid));
		p[i].transactionCode = helper::allocStrInComMem(helper::utf82utf16(j.m_transactioncode));
		p[i].sharedlinkUrl = helper::allocStrInComMem(helper::utf82utf16(j.m_sharedlink));
		p[i].rights = helper::allocStrInComMem(helper::utf82utf16(j.m_rights));
		p[i].comments = helper::allocStrInComMem(helper::utf82utf16((j.m_comments)));

		p[i].size = j.m_size;
		p[i].sharedDateMillis = j.m_shareddate;
		p[i].isOwner = j.m_isowner;
	}
	*ppFiles = p;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ListSharedWithMeFiles(HANDLE hUser, 
	uint32_t pageId, uint32_t pageSize, 
	const wchar_t * orderBy, 
	const wchar_t * searchString,
	SharedWithMeFileInfo ** ppFiles, 
	uint32_t* pSize)
{
	OutputDebugStringA("call SDWL_User_ListSharedWithMeFiels\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	std::vector<SDR_SHAREDWITHME_FILE_INFO> vec;
	auto rt = g_User->GetSharedWithMeFiles(pageId, pageSize,
		helper::utf162utf8( orderBy), 
		helper::utf162utf8(searchString),
		vec);

	auto size = vec.size();
	*pSize = (uint32_t)size;
	if (!rt ) {
		return rt.GetCode();
	}

	if (size == 0) {
		*ppFiles = NULL;
		return SDWL_SUCCESS;
	}

	// convert it to c-like struct and encapsule into COM-mem for c# used
	SharedWithMeFileInfo* p = (SharedWithMeFileInfo*)::CoTaskMemAlloc(size * sizeof(SharedWithMeFileInfo));

	for (size_t i = 0; i < size; i++)
	{
		auto j = vec[i];
		p[i].duid = helper::allocStrInComMem(helper::utf82utf16( j.m_duid));
		p[i].nxlName = helper::allocStrInComMem(helper::utf82utf16(j.m_nxlname ));
		p[i].fileType = helper::allocStrInComMem(helper::utf82utf16(j.m_filetype));
		p[i].sharedbyWhoEmail = helper::allocStrInComMem(helper::utf82utf16(j.m_sharedby));
		p[i].transactionId = helper::allocStrInComMem(helper::utf82utf16(j.m_transactionid));
		p[i].transactionCode = helper::allocStrInComMem(helper::utf82utf16(j.m_transactioncode));
		p[i].sharedlinkUrl = helper::allocStrInComMem(helper::utf82utf16(j.m_sharedlink));
		p[i].rights = helper::allocStrInComMem(helper::utf82utf16(j.m_rights));
		p[i].comments = helper::allocStrInComMem(helper::utf82utf16((j.m_comments)));

		p[i].size = j.m_size;
		p[i].sharedDateMillis = j.m_shareddate;
		p[i].isOwner = j.m_isowner;
	}
	*ppFiles = p;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_DownloadSharedWithMeFiles(HANDLE hUser, 
	const wchar_t * transactionId, 
	const wchar_t * transactionCode, 
	const wchar_t * downlaodDestLocalFolder, 
	bool forViewer, 
	const wchar_t** outPath)
{
	OutputDebugStringA("call SDWL_User_DownloadSharedWithMeFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (transactionId == NULL || transactionCode == NULL || downlaodDestLocalFolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	std::wstring destfolder = downlaodDestLocalFolder;
	auto rt = g_User->SharedWithMeDownloadFile(transactionCode, transactionId, destfolder, forViewer);
	if (!rt) {
		return rt.GetCode();
	}
	*outPath = helper::allocStrInComMem(destfolder);

	return SDWL_SUCCESS;

}

NXSDK_API DWORD SDWL_User_DownloadSharedWithMePartialFiles(HANDLE hUser,
	const wchar_t * transactionId,
	const wchar_t * transactionCode,
	const wchar_t * downlaodDestLocalFolder,
	bool forViewer,
	const wchar_t** outPath)
{
	OutputDebugStringA("call SDWL_User_DownloadSharedWithMePartialFiles\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (transactionId == NULL || transactionCode == NULL || downlaodDestLocalFolder == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	std::wstring destfolder = downlaodDestLocalFolder;
	auto rt = g_User->SharedWithMeDownloadPartialFile(transactionCode, transactionId, destfolder, forViewer);
	if (!rt) {
		return rt.GetCode();
	}
	*outPath = helper::allocStrInComMem(destfolder);

	return SDWL_SUCCESS;

}

NXSDK_API DWORD SDWL_User_EvaulateNxlFileRights(HANDLE hUser, 
	const wchar_t * filePath, 
	int ** pprights, int * pLen, 
	WaterMark * pWaterMark)
{
	OutputDebugStringA("call SDWL_User_EvaulateNxlFileRights\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (filePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	std::vector<std::pair<SDRmFileRight, std::vector<SDR_WATERMARK_INFO>>> rightsAndWatermarks;
	auto rt = g_User->GetRights(filePath, rightsAndWatermarks);
	if (!rt) {
		return rt.GetCode();
	}
	if (rightsAndWatermarks.size() == 0) {
		*pprights = NULL;
		*pLen = 0;
		return SDWL_SUCCESS;
	}
	int size = (int)rightsAndWatermarks.size();
	*pLen = size;
	int* buf = (int*)::CoTaskMemAlloc(sizeof(int*)*size);
	bool bFilledWaterMark = false;
	// fill data, with ugly code, we only care abour the first watermark valuse and ignore others
	for (int i = 0; i < size; i++) {
		auto& cur = rightsAndWatermarks[i];
		buf[i] = cur.first;
		if (!bFilledWaterMark && cur.second.size() > 0) {
			WaterMark* buf = pWaterMark;
			{
				auto& wm = cur.second[0];
				buf->text = helper::allocStrInComMem(helper::utf82utf16(wm.text));
				buf->fontName = helper::allocStrInComMem(helper::utf82utf16(wm.fontName));
				buf->fontColor = helper::allocStrInComMem(helper::utf82utf16(wm.fontColor));
				buf->repeat = wm.repeat;
				buf->fontSize = wm.fontSize;
				buf->rotation = wm.rotation;
				buf->transparency = wm.transparency;
			}
		}
	}

	*pprights = buf;
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_AddNxlFileLog(HANDLE hUser, const wchar_t * filePath, int Oper, bool isAllow)
{
	OutputDebugStringA("call SDWL_User_AddNxlFileLog\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (filePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	// by osmond, in 12/27/2018 sdk exported new fun for add log with only path,
	// so we dont need to open the nxl first before adding tag
	/*ISDRmNXLFile * file = NULL;
	auto rt = g_User->OpenFile(filePath, &file);
	if (!rt) {
		return rt.GetCode();
	}
	if (file == NULL) {
		return SDWL_INTERNAL_ERROR;
	}*/

	auto rt = g_User->AddActivityLog(filePath,
		(RM_ActivityLogOperation)Oper, isAllow ? RL_RAllowed: RL_RDenied);
	// close file immediately
	//g_User->CloseFile(file);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetPreference(HANDLE hUser, 
	Expiration* expiration,
	wchar_t**  watermarkStr)
{
	OutputDebugStringA("call SDWL_User_GetPreference\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	std::wstring watermark;	
	uint32_t option; uint64_t start; uint64_t end;
	auto rt = g_User->GetUserPreference(option,
		start, end, watermark);
	if (!rt) {
		return rt.GetCode();
	}
	// prepare out params
	expiration->type = (ExpiryType)option;
	expiration->start = start;
	expiration->end = end;
	*watermarkStr = helper::allocStrInComMem(watermark);
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_UpdatePreference(HANDLE hUser, Expiration expiration,
	const wchar_t*  watermarkStr)
{
	OutputDebugStringA("call SDWL_User_UpdatePreference\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	auto rt = g_User->UpdateUserPreference(expiration.type,
		expiration.start, expiration.end, watermarkStr);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetNxlFileFingerPrint(HANDLE hUser, 
	const wchar_t * nxlFilePath, 
	NXL_FILE_FINGER_PRINT * pFingerPrint)
{
	OutputDebugStringA("call SDWL_User_GetNxlFileFingerPrint\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	SDR_NXL_FILE_FINGER_PRINT fp;
	auto rt = g_User->GetFingerPrint(nxlFilePath, fp);
	if (!rt) {
		return rt.GetCode();
	}

	ISDRmNXLFile * file = NULL;
	rt = g_User->OpenFile(nxlFilePath, &file);
	if (!rt) {
		return rt.GetCode();
	}
	if (file == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	DWORD64 datecreated = file->Get_DateCreated();
	DWORD64 datemodified = file->Get_DateModified();
	if (datemodified == 0) datemodified = datecreated;

	g_User->CloseFile(file);
	// fill 
	{
		pFingerPrint->name = helper::allocStrInComMem(fp.name);
		pFingerPrint->localPath = helper::allocStrInComMem(fp.localPath);
		pFingerPrint->fileSize = fp.fileSize;
		pFingerPrint->isOwner = fp.isOwner;
		pFingerPrint->isFromMyVault = fp.isFromMyVault;
		pFingerPrint->isFromProject = fp.isFromProject;
		pFingerPrint->isFromSystemBucket = fp.isFromSystemBucket;
		pFingerPrint->projectId = fp.projectId;
		pFingerPrint->isByAdHocPolicy = fp.isByAdHocPolicy;
		pFingerPrint->IsByCentrolPolicy = fp.IsByCentrolPolicy;
		pFingerPrint->tags = helper::allocStrInComMem(fp.tags);
		pFingerPrint->expiration.type = (ExpiryType)fp.expiration.type;
		pFingerPrint->expiration.start = fp.expiration.start;
		pFingerPrint->expiration.end = fp.expiration.end;
		pFingerPrint->adHocWatermar = helper::allocStrInComMem(fp.adHocWatermar);
		pFingerPrint->fileCreated = datecreated;
		pFingerPrint->fileModified = datemodified;
		//New added for Admin Rights feature.
		pFingerPrint->hasAdminRights = fp.hasAdminRights;
		pFingerPrint->duid = helper::allocStrInComMem(fp.duid);
		// regard rights as bit-enabled one
		DWORD64 rs = 0;
		std::for_each(fp.rights.begin(), fp.rights.end(), [&rs](SDRmFileRight i) {
			rs |= (DWORD64)i;
		});

		pFingerPrint->rights = rs;
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetNxlFileTagsWithoutToken(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	wchar_t** pTags) {
	OutputDebugStringA("call SDWL_User_GetNxlFileTagsWithoutToken\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	SDR_NXL_FILE_FINGER_PRINT fp;
	auto rt = g_User->GetFingerPrintWithoutToken(nxlFilePath, fp);
	if (!rt) {
		return rt.GetCode();
	}
	*pTags= helper::allocStrInComMem(fp.tags);
	
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_UpdateNxlFileRights(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	int* pRights, int rightsArrLength,
	WaterMark watermark, Expiration expiration,
	const wchar_t* tags)
{	
	OutputDebugStringA("call SDWL_User_UpdateNxlFileRights\n");
	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	
	//Get Nxl file path first.
	std::wstring p(nxlFilePath);

	//Padding rights.
	int len = rightsArrLength;
	std::vector<SDRmFileRight> r;
	for (int i = 0; i < len; i++) {
		SDRmFileRight l = (SDRmFileRight)pRights[i];
		r.push_back(l);
	}
	//Padding watermark.
	SDR_WATERMARK_INFO w;
	{
		w.text = helper::utf162utf8(watermark.text);
		w.fontName = helper::utf162utf8(watermark.fontName);
		w.fontColor = helper::utf162utf8(watermark.fontColor);
		w.fontSize = watermark.fontSize;
		w.transparency = watermark.transparency;
		w.rotation = (WATERMARK_ROTATION)watermark.rotation;
		w.repeat = watermark.repeat;
	}
	//Padding expiration.
	SDR_Expiration e;
	{
		e.type = (IExpiryType)expiration.type;
		e.start = expiration.start;
		e.end = expiration.end;
	}
	//Padding tags.
	std::string t(helper::utf162utf8(tags));
	auto rt = g_User->ChangeRightsOfFile(p, r, w, e, t);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_UpdateProjectNxlFileRights(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	const uint32_t projectId,
	const wchar_t* fileName,
	const wchar_t* parentPathId,
	int* rights, int rightsArrLength,
	WaterMark watermark, Expiration expiration,
	const wchar_t* tags) {
	OutputDebugStringA("call SDWL_User_UpdateProjectNxlFileRights");

	// sanity check.
	if (hUser == NULL || (ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL || fileName == NULL) {
		// missing required params.
		return SDWL_INTERNAL_ERROR;
	}
	std::wstring nxlfp(nxlFilePath);
	UINT32 pId = projectId;
	std::string fName(helper::utf162utf8(fileName));
	std::string parentPId(helper::utf162utf8(parentPathId));

	// padding rights section.
	std::vector<SDRmFileRight> r;
	{
		int len = rightsArrLength;
		for (int i = 0; i < len; i++) {
			r.push_back((SDRmFileRight)rights[i]);
		}
	}
	
	// padding watermark.
	SDR_WATERMARK_INFO w;
	{
		w.text = helper::utf162utf8(watermark.text);
		w.fontName = helper::utf162utf8(watermark.fontName);
		w.fontColor = helper::utf162utf8(watermark.fontColor);
		w.fontSize = watermark.fontSize;
		w.transparency = watermark.transparency;
		w.rotation = (WATERMARK_ROTATION)watermark.rotation;
		w.repeat = watermark.repeat;
	}

	// padding expiration.
	SDR_Expiration e;
	{
		e.type = (IExpiryType)expiration.type;
		e.start = expiration.start;
		e.end = expiration.end;
	}

	// padding tags.
	std::string t(helper::utf162utf8(tags));

	auto rt = g_User->ChangeRightsOfProjectFile(nxlfp, pId, fName, parentPId, r, w, e, t);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetMyVaultFileMetaData(
	HANDLE hUser,
	const wchar_t* pNxlFilePath,
	const wchar_t* pPathId,
	MYVAULT_FILE_META_DATA* pMetadata) {
	OutputDebugStringA("call SDWL_NXL_FILE_GetMyVaultFileMetaData");
	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (pNxlFilePath == NULL || pPathId == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (pMetadata == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	std::wstring p(pNxlFilePath);
	std::string pid(helper::utf162utf8(pPathId));

	SDR_FILE_META_DATA md;
	auto rt = g_User->GetNXLFileMetadata(p, pid, md);
	if (!rt) {
		return rt.GetCode();
	}

	{
		//Padding metadata.
		pMetadata->name = helper::allocStrInComMem(helper::utf82utf16(md.name));
		pMetadata->fileLink = helper::allocStrInComMem(helper::utf82utf16(md.fileLink));

		pMetadata->lastModify = md.lastmodify;
		pMetadata->protectOn = md.protectedon;
		pMetadata->sharedOn = md.sharedon;
		pMetadata->isShared = md.shared;
		pMetadata->isDeleted = md.deleted;
		pMetadata->isRevoked = md.revoked;
		pMetadata->protectionType = md.protectionType;
		pMetadata->isOwner = md.owner;
		pMetadata->isNxl = md.nxl;

		std::string recipents;
		std::for_each(md.recipients.begin(), md.recipients.end(), [&recipents](std::string i){
			recipents.append(i).append(";");
		});
		pMetadata->recipents = helper::allocStrInComMem(helper::utf82utf16(recipents));
		pMetadata->pathDisplay = helper::allocStrInComMem(helper::utf82utf16(md.pathDisplay));
		pMetadata->pathId = helper::allocStrInComMem(helper::utf82utf16(md.pathid));
		pMetadata->tags = helper::allocStrInComMem(helper::utf82utf16(md.tags));

		pMetadata->expiration.type = (ExpiryType)md.expiration.type;
		pMetadata->expiration.start = md.expiration.start;
		pMetadata->expiration.end = md.expiration.end;
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_MyVaultShareFile(
	HANDLE hUser,
	const wchar_t* nxlLocalPath,
	const wchar_t* recipents,
	const wchar_t* repositoryId,
	const wchar_t* fileName,
	const wchar_t* filePathId,
	const wchar_t* filePath,
	const wchar_t* comments) {
	OutputDebugStringA("call SDWL_User_MyVaultShareFile");

	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}

	if (nxlLocalPath == NULL) {
		// nxl local file not found.
		return SDWL_INTERNAL_ERROR;
	}
	if (recipents == NULL) {
		// recipents must need first.
		return SDWL_INTERNAL_ERROR;
	}
	if (fileName == NULL || filePathId == NULL || filePath == NULL) {
		// params reqired missing.
		return SDWL_INTERNAL_ERROR;
	}

	// prepare params send them to sdk.
	std::wstring nlp(nxlLocalPath);

	std::vector<std::string> rv;
	{
		std::string recipentsStr(helper::utf162utf8(recipents));
		helper::SplitStr(recipentsStr, rv, ",");
	}

	std::string rId(helper::utf162utf8(repositoryId));
	std::string fn(helper::utf162utf8(fileName));
	std::string fpid(helper::utf162utf8(filePathId));
	std::string fp(helper::utf162utf8(filePath));
	std::string cmts(helper::utf162utf8(comments));

	auto rt = g_User->ShareFileFromMyVault(nlp, rv, rId, fn, fpid, fp, cmts);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_SharedWithMeReshareFile(
	HANDLE hUser,
	const wchar_t* transactionId,
	const wchar_t* transactionCode,
	const wchar_t* emaillist) {
	OutputDebugStringA("call SDWL_User_SharedWithMeReshareFile");

	// sanity check
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (transactionId == NULL || transactionCode == NULL || emaillist == NULL) {
		// params are required.
		return SDWL_INTERNAL_ERROR;
	}

	std::string tranId(helper::utf162utf8(transactionId));
	std::string tranCode(helper::utf162utf8(transactionCode));
	std::string emails(helper::utf162utf8(emaillist));

	auto rt = g_User->SharedWithMeReShareFile(tranId, tranCode, emails);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_ResetSourcePath(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	const wchar_t* sourcePath) {
	OutputDebugStringA("Call SDWL_NXL_File_Reshare_ResetSourcePath.\n");

	//Sanity check.
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	if (nxlFilePath == NULL || sourcePath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	std::wstring nxlpth(nxlFilePath);
	std::wstring srcpth(sourcePath);

	auto rt = g_User->ResetSourcePath(nxlpth, srcpth);
	if (!rt) {
		return rt.GetCode();
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetFileRightsFromCentralPoliciesByTenant(
	HANDLE hUser,
	const wchar_t* tenantName,
	const wchar_t* tags,
	CENTRAL_RIGHTS** pArray,
	uint32_t* pArrSize) {
	OutputDebugStringA("Call SDWL_User_GetFileRightsFromCentralPoliciesByTenant.\n");

	//Sanity check.
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}

	if (tenantName == NULL || tags == NULL) {
		//Params needed.
		return SDWL_INTERNAL_ERROR;
	}

	std::string tn(helper::utf162utf8(tenantName));
	std::string t(helper::utf162utf8(tags));

	std::vector<std::pair<SDRmFileRight, std::vector<SDR_WATERMARK_INFO>>> tmp;
	auto rt = g_User->GetFileRightsFromCentralPolicies(tn, t, tmp);
	if (!rt) {
		return rt.GetCode();
	}

	//Prepare output data.
	{
		const auto pSize = tmp.size();
		if (pSize > 0) {
			CENTRAL_RIGHTS* p = (CENTRAL_RIGHTS*)::CoTaskMemAlloc(sizeof(CENTRAL_RIGHTS)*pSize);
			for (size_t i = 0; i < pSize; i++) {
				//Padding single CENTRAL_RIGHTS.
				p[i].rights = tmp[i].first;

				std::vector<SDR_WATERMARK_INFO> wms = tmp[i].second;
				const auto wmsSize = wms.size();
				if (wmsSize > 0) {
					WaterMark* wm = (WaterMark*)::CoTaskMemAlloc(sizeof(WaterMark)*wmsSize);
					for (size_t j = 0; j < wmsSize; j++) {
						wm[j].text = helper::allocStrInComMem(helper::utf82utf16(wms[j].text));
						wm[j].fontName = helper::allocStrInComMem(helper::utf82utf16(wms[j].fontName));
						wm[j].fontColor = helper::allocStrInComMem(helper::utf82utf16(wms[j].fontColor));
						wm[j].repeat = wms[j].repeat;
						wm[j].fontSize = wms[j].fontSize;
						wm[j].rotation = wms[j].rotation;
						wm[j].transparency = wms[j].transparency;
					}
					*(p[i].watermarks) = wm;
					p[i].watermarkLenth = wmsSize;
				}
				else {
					p[i].watermarks = NULL;
					p[i].watermarkLenth = 0;
				}
			}
			*pArray = p;
			*pArrSize = (uint32_t)pSize;
		}
		else {
			*pArray = NULL;
			*pArrSize = 0;
		}
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_User_GetFileRightsFromCentralPolicyByProjectId(
	HANDLE hUser,
	const uint32_t projectId,
	const wchar_t* tags,
	CENTRAL_RIGHTS** pArray,
	uint32_t* pArrSize) {

	OutputDebugStringA("Call SDWL_User_GetFileRightsFromCentralPolicyByProjectId.\n");

	//Sanity check.
	if (g_User == NULL || (ISDRmUser*)hUser != g_User) {
		return SDWL_INTERNAL_ERROR;
	}
	
	if (tags == NULL) {
		//Params needed.
		return SDWL_INTERNAL_ERROR;
	}

	uint32_t pid = projectId;
	std::string t(helper::utf162utf8(tags));

	std::vector<std::pair<SDRmFileRight, std::vector<SDR_WATERMARK_INFO>>> tmp;
	auto rt = g_User->GetFileRightsFromCentralPolicies(pid, t, tmp);
	if (!rt) {
		return rt.GetCode();
	}

	//Prepare output data.
	{
		const auto pSize = tmp.size();
		if (pSize > 0) {
			CENTRAL_RIGHTS* p = (CENTRAL_RIGHTS*)::CoTaskMemAlloc(sizeof(CENTRAL_RIGHTS)*pSize);
			for (size_t i = 0; i < pSize; i++) {
				//Padding single CENTRAL_RIGHTS.
				p[i].rights = tmp[i].first;

				std::vector<SDR_WATERMARK_INFO> wms = tmp[i].second;
				const auto wmsSize = wms.size();
				if (wmsSize > 0) {
					WaterMark* wm = (WaterMark*)::CoTaskMemAlloc(sizeof(WaterMark)*wmsSize);
					for (size_t j = 0; j < wmsSize; j++) {
						wm[j].text = helper::allocStrInComMem(helper::utf82utf16(wms[j].text));
						wm[j].fontName = helper::allocStrInComMem(helper::utf82utf16(wms[j].fontName));
						wm[j].fontColor = helper::allocStrInComMem(helper::utf82utf16(wms[j].fontColor));
						wm[j].repeat = wms[j].repeat;
						wm[j].fontSize = wms[j].fontSize;
						wm[j].rotation = wms[j].rotation;
						wm[j].transparency = wms[j].transparency;
					}
					*(p[i].watermarks) = wm;
					p[i].watermarkLenth = wmsSize;
				}
				else {
					p[i].watermarks = NULL;
					p[i].watermarkLenth = 0;
				}
			}
			*pArray = p;
			*pArrSize = (uint32_t)pSize;
		}
		else {
			*pArray = NULL;
			*pArrSize = 0;
		}
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_File_GetListNumber(HANDLE hFile, int* pSize)
{
	OutputDebugStringA("call SDWL_File_GetListNumber\n");
	// sanity check
	ISDRFiles* pF = (ISDRFiles*)hFile;

	*pSize  = (int)pF->GetListNumber();

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_File_GetList(HANDLE hFile, 
	wchar_t** strArray, 
	int* pSize)
{
	OutputDebugStringA("call SDWL_File_GetList\n");
	// sanity check
	if (strArray == NULL || pSize == NULL) {
		return SDWL_INTERNAL_ERROR;

	}
	
	ISDRFiles* pF = (ISDRFiles*)hFile;
	auto rt = pF->GetList();
	const auto size = rt.size();

	if (size < 0) {
		*strArray = NULL;
		*pSize = (int)size;
		return SDWL_INTERNAL_ERROR;
	}

	if (size == 0) {
		*strArray = NULL;
		*pSize = 0;
		return SDWL_SUCCESS;
	}

	// alloc mem
	wchar_t** pBuf = (wchar_t**)::CoTaskMemAlloc(size*sizeof(wchar_t*));
	// alloc each item
	for (size_t i = 0; i < size; i++) {
		pBuf[i] = helper::allocStrInComMem(rt[i]);
	}
	// set pSize
	*pSize = (int)size;
	*strArray = (wchar_t*)pBuf;


	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_File_GetFile(HANDLE hFile, int index, HANDLE* hNxlFile)
{
	OutputDebugStringA("call SDWL_File_GetFile\n");
	// sanity check
	ISDRFiles* pF = (ISDRFiles*)hFile;
	
	// begin
	auto rt = pF->GetFile(index);
	if (rt == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	
	*hNxlFile = rt;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_File_GetFile2(HANDLE hFile, 
	const wchar_t * FileName,HANDLE* hNxlFile)
{
	OutputDebugStringA("call SDWL_File_GetFile2\n");
	// sanity check
	ISDRFiles* pF = (ISDRFiles*)hFile;
	
	// begin
	std::wstring w = std::wstring(FileName);
	auto rt = pF->GetFile(w);
	if (rt != NULL) {
		*hNxlFile = rt;
		return SDWL_SUCCESS;		
	}

	// SDK new added function, by design SDK's nxl file list only save that local created.

	// for rms downloaded file, we must use g_User's OpenFile to get it object
	ISDRmNXLFile* pNxl = NULL;
	g_User->OpenFile(w, &pNxl);

	if (pNxl != NULL) {
		*hNxlFile = pNxl;
		return SDWL_SUCCESS;
	}

	return SDWL_INTERNAL_ERROR;

	
}

NXSDK_API DWORD SDWL_File_RemoveFile(HANDLE hFile, HANDLE hNxlFile,bool *pResult)
{
	OutputDebugStringA("call SDWL_File_RemoveFile\n");
	// sanity check
	ISDRFiles* pF = (ISDRFiles*)hFile;
	
	ISDRmNXLFile* p = (ISDRmNXLFile*)hNxlFile;
	
	auto rt = pF->RemoveFile(p);

	*pResult = rt;

	return SDWL_SUCCESS;
}
#pragma endregion

#pragma region NxlFile

NXSDK_API DWORD SDWL_NXL_File_GetFileName(HANDLE hNxlFile, wchar_t** ppname)
{
	OutputDebugStringA("call SDWL_NXL_File_GetFileName\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;

	*ppname = helper::allocStrInComMem(pF->GetFileName());
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_GetFileSize(HANDLE hNxlFile, DWORD64* pSize)
{
	OutputDebugStringA("call SDWL_NXL_File_GetFileSize\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;

	*pSize = pF->GetFileSize();

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_IsValidNxl(HANDLE hNxlFile, bool* pResult)
{
	OutputDebugStringA("call SDWL_NXL_File_IsValidNxl\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;

	*pResult = pF->IsValidNXL();

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_GetRights(HANDLE hNxlFile, int** pprights, int* pLen)
{
	OutputDebugStringA("call SDWL_NXL_File_GetRights\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;
	// begin
	auto rt = pF->GetRights();
	size_t size = rt.size();
	if (size == 0) {
		*pprights = NULL;
		*pLen = 0;
		return SDWL_SUCCESS;
	}

	int* buf = (int*)::CoTaskMemAlloc(sizeof(int*)*size);

	for (size_t i=0; i < size; i++) {
		buf[i] = rt[i];
	}
	*pLen = (int)size;
	*pprights = buf;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_GetWaterMark(HANDLE hNxlFile, WaterMark * pWaterMark)
{
	OutputDebugStringA("call SDWL_NXL_File_GetWaterMark\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;
	// begin
	SDR_WATERMARK_INFO wm = pF->GetWaterMark();

	WaterMark* buf = pWaterMark;
	
	{
		buf->text = helper::allocStrInComMem(helper::utf82utf16(wm.text));
		buf->fontName = helper::allocStrInComMem(helper::utf82utf16(wm.fontName));
		buf->fontColor = helper::allocStrInComMem(helper::utf82utf16(wm.fontColor));
		buf->repeat = wm.repeat;
		buf->fontSize = wm.fontSize;
		buf->rotation = wm.rotation;
		buf->transparency = wm.transparency;		
	}

	return SDWL_SUCCESS;

}

NXSDK_API DWORD SDWL_NXL_File_GetExpiration(HANDLE hNxlFile, Expiration * pExpiration)
{
	OutputDebugStringA("call SDWL_NXL_File_GetExpiration\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;
	// begin

	auto ex = pF->GetExpiration();

	{
		pExpiration->start = ex.start;
		pExpiration->end = ex.end;
		pExpiration->type = (ExpiryType)ex.type;
	}

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_GetTags(HANDLE hNxlFile, wchar_t ** ppTags)
{
	OutputDebugStringA("call SDWL_NXL_File_GetExpiration\n");
	
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;
	
	// begin
	*ppTags = helper::allocStrInComMem(helper::utf82utf16(pF->GetTags()));

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_CheckRights(HANDLE hNxlFile, int right, bool* pResult)
{
	OutputDebugStringA("call SDWL_NXL_File_CheckRights\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;

	auto rt = pF->CheckRights((SDRmFileRight)right);

	*pResult = rt;

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_IsUploadToRMS(HANDLE hNxlFile, bool* pResult)
{
	OutputDebugStringA("call SDWL_NXL_File_IsUploadToRMS\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	//ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;

	//*pResult = pF->IsUploadToRMS();

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_GetAdhocWatermarkString(HANDLE hNxlFile, wchar_t** ppWmStr)
{
	OutputDebugStringA("call SDWL_NXL_File_GetAdhocWatermarkString\n");
	// sanity check
	if (hNxlFile == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	ISDRmNXLFile* pF = (ISDRmNXLFile*)hNxlFile;

	std::string utf8Str = pF->GetAdhocWaterMarkString();
	*ppWmStr = helper::allocStrInComMem(helper::utf82utf16(utf8Str));
	
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_NXL_File_GetActivityInfo(const wchar_t* fileName,
	NXL_FILE_ACTIVITY_INFO** pInfo,
	DWORD* pSize)
{
	OutputDebugStringA("call SDWL_User_GetActivityInfo\n");
	// sanity check
	if (g_User == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (fileName == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	std::vector<SDR_FILE_ACTIVITY_INFO>v;
	auto rt = g_User->GetActivityInfo(fileName, v);
	if (!rt) {
		return rt.GetCode();
	}

	// fill info
	auto size = v.size();
	*pSize = (DWORD)size;
	if (size == 0) {
		*pInfo = NULL;
		return SDWL_SUCCESS;

	}

	NXL_FILE_ACTIVITY_INFO* p = (NXL_FILE_ACTIVITY_INFO*)::CoTaskMemAlloc(size * sizeof(NXL_FILE_ACTIVITY_INFO));

	for (size_t i = 0; i < size; i++) {
		SDR_FILE_ACTIVITY_INFO item = v[i];

		p[i].duid = helper::allocStrInComMem(helper::utf82utf16(item.duid));
		p[i].email = helper::allocStrInComMem(helper::utf82utf16(item.email));
		p[i].operation = helper::allocStrInComMem(helper::utf82utf16(item.operation));
		p[i].deviceType = helper::allocStrInComMem(helper::utf82utf16(item.deviceType));
		p[i].deviceId = helper::allocStrInComMem(helper::utf82utf16(item.deviceId));
		p[i].accessResult = helper::allocStrInComMem(helper::utf82utf16(item.accessResult));
		p[i].accessTime = item.accessTime;
	}

	*pInfo = p;

	return SDWL_SUCCESS;
}


NXSDK_API DWORD SDWL_NXL_File_GetNxlFileActivityLog(
	HANDLE hNxlFile,
	DWORD64 startPos, DWORD64 count,
	BYTE searchField, 
	const wchar_t* searchText,
	BYTE orderByField, 
	bool orderByReverse)
{
	OutputDebugStringA("call SDWL_User_GetNxlFileActivityLog\n");
	// sanity check
	//if (g_User == NULL) {
	//	// not found
	//	return SDWL_INTERNAL_ERROR;
	//}	

	//ISDRmNXLFile *pf = (ISDRmNXLFile *)hNxlFile;
	//
	//auto rt = g_User->GetNXLFileActivitylog(pf,
	//	startPos,
	//	count,
	//	searchField,
	//	helper::utf162utf8(searchText),
	//	orderByField, 
	//	orderByReverse);

	//if (!rt) {
	//	return rt.GetCode();
	//}
	return SDWL_SUCCESS;
}
#pragma endregion

#pragma region RPM_DRIVER

NXSDK_API DWORD SDWL_RPM_IsRPMDriverExist(HANDLE hSession, bool* pResult)
{
	OutputDebugStringA("call SDWL_Is_RPMDriverExist to check RPM driver\n");
	

	// sanity check
	if (g_RmsIns == NULL ) {
		return SDWL_INTERNAL_ERROR;
	}
	if (g_RmsIns != hSession) {
		return SDWL_INTERNAL_ERROR;
	}

	*pResult=g_RmsIns->IsRPMDriverExist();

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_AddRPMDir(HANDLE hSession, const wchar_t* path)
{
	OutputDebugStringA("call SDWL_User_AddRPMDir to set RPM folder\n");
	// sanity check
	if (g_RmsIns == NULL ) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (g_RmsIns != hSession) {
		return SDWL_INTERNAL_ERROR;
	}

	if (!g_RmsIns->IsRPMDriverExist()) {
		return SDWL_INTERNAL_ERROR;
	}
	
	 auto rt=g_RmsIns->AddRPMDir(path);
	 //if (!rt) {
		// return rt.GetCode();
	 //}

	 //
	 // extera feature, set path ok, we need to cache this path
	 //
	 g_UserRPMFolder[g_RmsIns]=std::wstring(path);

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_RemoveRPMDir(HANDLE hSession, const wchar_t* path)
{
	OutputDebugStringA("call SDWL_User_RemoveRPMDir to remove RPM folder\n");
	// sanity check
	if (g_RmsIns == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (g_RmsIns != hSession) {
		return SDWL_INTERNAL_ERROR;
	}
	if (!g_RmsIns->IsRPMDriverExist()) {
		return SDWL_INTERNAL_ERROR;
	}
	
	// do
	auto rt= g_RmsIns->RemoveRPMDir(path);
	if (!rt) {
		return rt.GetCode();
	}

	g_UserRPMFolder.erase(g_RmsIns);

	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_RegisterApp(HANDLE hSession, const wchar_t * appPath)
{
	OutputDebugStringA("call SDWL_RPM_RegisterApp\n");
	// sanity check
	if (g_RmsIns == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (g_RmsIns != hSession) {
		return SDWL_INTERNAL_ERROR;
	}
	if (appPath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	auto rt=g_RmsIns->RPMRegisterApp(appPath, gRPM_Security);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_NotifyRMXStatus(HANDLE hSession, bool running)
{
	OutputDebugStringA("call SDWL_RPM_RegisterRMXStatus\n");
	// sanity check
	if (g_RmsIns == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (g_RmsIns != hSession) {
		return SDWL_INTERNAL_ERROR;
	}
	auto rt = g_RmsIns->RPMNotifyRMXStatus(running, gRPM_Security);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_UnregisterApp(HANDLE hSession, const wchar_t * appPath)
{
	OutputDebugStringA("call SDWL_RPM_RegisterApp");
	// sanity check
	if (g_RmsIns == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (g_RmsIns != hSession) {
		return SDWL_INTERNAL_ERROR;
	}
	if (appPath == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	auto rt = g_RmsIns->RPMUnregisterApp(appPath, gRPM_Security);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_AddTrustedProcess(HANDLE hSession, int pid)
{
	OutputDebugStringA("call SDWL_RPM_RegisterApp");
	// sanity check
	if (g_RmsIns == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (g_RmsIns != hSession) {
		return SDWL_INTERNAL_ERROR;
	}
	auto rt = g_RmsIns->RPMAddTrustedProcess(pid, gRPM_Security);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_RemoveTrustedProcess(HANDLE hSession, int pid)
{
	OutputDebugStringA("call SDWL_RPM_RegisterApp");
	// sanity check
	if (g_RmsIns == NULL) {
		// not found
		return SDWL_INTERNAL_ERROR;
	}
	if (g_RmsIns != hSession) {
		return SDWL_INTERNAL_ERROR;
	}
	auto rt = g_RmsIns->RPMRemoveTrustedProcess(pid, gRPM_Security);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_EditFile(HANDLE hSession, const wchar_t * srcNxlPath, wchar_t ** outSrcPath)
{
	OutputDebugStringA("call SDWL_RPM_EditFile\n");
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	// extra feature, workaround Drive bug;
//	std::wstring outPath= g_UserRPMFolder[g_RmsIns];
	std::wstring outPath = L"";

	auto rt = g_RmsIns->RPMEditCopyFile(srcNxlPath, outPath);
	if (!rt) {
		return rt.GetCode();
	}

	*outSrcPath = helper::allocStrInComMem(outPath);
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_DeleteFile(HANDLE hSession, const wchar_t * srcNxlPath)
{
	OutputDebugStringA("call SDWL_RPM_DeleteFile\n");
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}

	auto rt = g_RmsIns->RPMDeleteFile(srcNxlPath);
	if (!rt) {
		return rt.GetCode();
	}
	return SDWL_SUCCESS;
}

NXSDK_API DWORD SDWL_RPM_IsSafeFolder(HANDLE hSession, const wchar_t * srcNxlPath, bool * outIsSafeFolder)
{
	OutputDebugStringA("call SDWL_RPM_IsSafeFolder\n");
	if (g_RmsIns == NULL) {
		return SDWL_INTERNAL_ERROR;
	}
	unsigned int dirStat = -1; // as docuemt, if 0/2 is rmp
	auto rt = g_RmsIns->IsRPMFolder(srcNxlPath,&dirStat);
	if (!rt) {
		return rt.GetCode();
	}

	*outIsSafeFolder = (dirStat == 0 || dirStat == 2) ? true:false;

	return rt;
}

#pragma endregion // RPM_DRIVER


#pragma region SysHelper

// call win32
			    
NXSDK_API DWORD SDWL_SYSHELPER_MonitorRegValueDeleted(const wchar_t* regValuename,RegChangeCallback callback)
{
	DWORD rt = SDWL_SUCCESS;
	//Notify the caller of changes to a value of the key. 
	//This can include adding or deleting a value, or changing an existing value.
	DWORD  dwFilter = REG_NOTIFY_CHANGE_LAST_SET;
	const wchar_t* SubKey = L"Software\\NextLabs\\SkyDRM\\Session";
	HKEY hkSession = 0;
	LONG   lErrorCode;

	// sanity check
	if (NULL == callback) {
		return SDWL_INTERNAL_ERROR;
	}
	//
	//  moniter
	//
	bool isContinue = true;
	while (isContinue)
	{
		
		// open key as Change Notify used
		lErrorCode = ::RegOpenKeyExW(HKEY_CURRENT_USER,
			SubKey, 0, KEY_NOTIFY | KEY_QUERY_VALUE , &hkSession);

		if (lErrorCode != ERROR_SUCCESS) {
			rt= SDWL_INTERNAL_ERROR;
			break;
		}

		// monitor changed
		lErrorCode = ::RegNotifyChangeKeyValue(hkSession, false, dwFilter, NULL, FALSE);
		if (lErrorCode != ERROR_SUCCESS) {
			// release res
			rt = SDWL_INTERNAL_ERROR;
			break;
		}

		// whether regKeyname has been deleted
		if (helper::IsRegValueExist(hkSession, regValuename)) {
			continue;
		}
		
		// notify
		callback(helper::allocStrInComMem(regValuename));
		isContinue = false;
		rt = SDWL_SUCCESS;

		// release res
		RegCloseKey(hkSession);
	}
	   	 
	return rt;
}
#pragma endregion
