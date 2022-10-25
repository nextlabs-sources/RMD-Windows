/*
Special designed for c# rmsdk api used, Memory De/Allocation will use 
COM memalloc api CoTaskMemAlloc(), C# p/invoke will automaticly call 
FreeCoTaskMem() as standard Marshalling.
So native language c/c++ must call FreeCoTaskMem() itself

*/
#include "stdafx.h"
#include "SDLError.h"
#include "sdktypes.h"

#define NXSDK_API extern "C" __declspec(dllexport)  

// Whole Lib Level
#pragma region SDWL_Library_Interface

NXSDK_API DWORD GetSDKVersion();

NXSDK_API DWORD CreateSDKSession(const wchar_t* wcTempPath, HANDLE* phSession);

NXSDK_API DWORD DeleteSDKSession(HANDLE hSession);

#pragma endregion

// Session Related Level
#pragma region SDWL_Session
NXSDK_API DWORD SDWL_Session_Initialize(HANDLE hSession, 
	const wchar_t* router, const wchar_t* tenant);

NXSDK_API DWORD SDWL_Session_Initialize2(HANDLE hSession, 
	const wchar_t* workingfolder, const wchar_t* router, 
	const wchar_t* tenant);

NXSDK_API DWORD SDWL_Session_SaveSession(HANDLE hSession, const  wchar_t* folder);

NXSDK_API DWORD SDWL_Session_GetCurrentTenant(HANDLE hSession, HANDLE* phTenant);

NXSDK_API DWORD SDWL_Session_GetLoginParams(HANDLE hSession, 
	wchar_t** ppURL, NXL_LOGIN_COOKIES** ppCookies,size_t* pSize);

NXSDK_API DWORD SDWL_Session_SetLoginRequest(HANDLE hSession, 
	const wchar_t* JsonReturn, const wchar_t* security, HANDLE* hUser);

NXSDK_API DWORD SDWL_Session_GetLoginUser(HANDLE hSession, 
	const wchar_t* UserEmail, const wchar_t* PassCode, HANDLE *hUser);
#pragma endregion //SDWL_Session

// RMP is part of Seesion Level
// RMP depends on hSession, that means user must login first in order to create session
#pragma region RPM_DRIVER

NXSDK_API DWORD SDWL_RPM_IsRPMDriverExist(HANDLE hSession, bool* pResult);

NXSDK_API DWORD SDWL_RPM_AddRPMDir(HANDLE hSession, const wchar_t* path);

NXSDK_API DWORD SDWL_RPM_RemoveRPMDir(HANDLE hSession, const wchar_t* path);

NXSDK_API DWORD SDWL_RPM_RegisterApp(HANDLE hSession, const wchar_t* appPath);

NXSDK_API DWORD SDWL_RPM_NotifyRMXStatus(HANDLE hSession, bool running);

NXSDK_API DWORD SDWL_RPM_UnregisterApp(HANDLE hSession, const wchar_t* appPath);

NXSDK_API DWORD SDWL_RPM_AddTrustedProcess(HANDLE hSession, int pid);

NXSDK_API DWORD SDWL_RPM_RemoveTrustedProcess(HANDLE hSession, int pid);

NXSDK_API DWORD SDWL_RPM_EditFile(HANDLE hSession,	const wchar_t* srcNxlPath, wchar_t** outSrcPath);

NXSDK_API DWORD SDWL_RPM_DeleteFile(HANDLE hSession, const wchar_t* srcNxlPath);

NXSDK_API DWORD SDWL_RPM_IsSafeFolder(HANDLE hSession, const wchar_t* srcNxlPath, bool* outIsSafeFolder);

#pragma endregion //RPM_DRIVER

// Tenant Level
#pragma region SDWL_Tenant
NXSDK_API DWORD SDWL_Tenant_GetTenant(HANDLE hTenant, wchar_t** pptenant);

NXSDK_API DWORD SDWL_Tenant_GetRouterURL(HANDLE hTenant, wchar_t** pprouterurl);

NXSDK_API DWORD SDWL_Tenant_GetRMSURL(HANDLE hTenant, wchar_t** pprmsurl);

NXSDK_API DWORD SDWL_Tenant_ReleaseTenant(HANDLE hTenant);

#pragma endregion //SDWL_Tenant

// User Level
#pragma region SDWL_User
NXSDK_API DWORD SDWL_User_GetUserName(HANDLE hUser, wchar_t** ppname);

NXSDK_API DWORD SDWL_User_GetUserEmail(HANDLE hUser, wchar_t** ppemail);

NXSDK_API DWORD SDWL_User_GetPasscode(HANDLE hUser, wchar_t** ppasscode);

NXSDK_API DWORD SDWL_User_ProjectMembershipId(HANDLE hUser, DWORD32 projectId, wchar_t** projectMembershipId);

NXSDK_API DWORD SDWL_User_TenantMembershipId(HANDLE hUser, wchar_t* tenantId, wchar_t** tenantMembershipId);

NXSDK_API DWORD SDWL_User_LogoutUser(HANDLE hUser);

/*skydrm 0; smal 1; google 2; facebook 3; yahoo 4*/
NXSDK_API DWORD SDWL_User_GetUserType(HANDLE hUser, int* type);

NXSDK_API DWORD SDWL_User_UpdateUserInfo(HANDLE hUser);

NXSDK_API DWORD SDWL_User_UpdateMyDriveInfo(HANDLE hUser);

NXSDK_API DWORD SDWL_User_GetMyDriveInfo(HANDLE hUser, DWORD64* usage, DWORD64* total);

NXSDK_API DWORD SDWL_User_GetLocalFile(HANDLE hUser, HANDLE* hLocalFiles);

NXSDK_API DWORD SDWL_User_RemoveLocalFile(HANDLE hUser, const wchar_t* nxlFilePath, bool* pResult);

NXSDK_API DWORD SDWL_User_ProtectFile(HANDLE hUser, 
	const wchar_t* path, 
	int* pRights, int len,
	WaterMark watermark, Expiration expiration,
	const wchar_t* tags,
	const wchar_t** outPath);

NXSDK_API DWORD SDWL_User_ProtectFileToProject(int projectId,
	HANDLE hUser,
	const wchar_t* path,
	int* pRights, int len,
	WaterMark watermark, Expiration expiration,
	const wchar_t* tags,
	const wchar_t** outPath);

NXSDK_API DWORD SDWL_User_UpdateRecipients(HANDLE hUser, 
	HANDLE hNxlFile, 
	const wchar_t* addmails[], int lenaddmails,
	const wchar_t* delmails[], int lendelmails);

NXSDK_API DWORD SDWL_User_UpdateRecipients2(HANDLE hUser,
	const wchar_t* nxlFilePath,
	const wchar_t* addmails[], int lenaddmails,
	const wchar_t* delmails[], int lendelmails);

NXSDK_API DWORD SDWL_User_GetRecipients(HANDLE hUser, 
	HANDLE hNxlFile, 
	wchar_t** emails,int* peSize,
	wchar_t** addEmials,int* paeSize,
	wchar_t** removEmails,int*preSize);

NXSDK_API DWORD SDWL_User_GetRecipients2(HANDLE hUser,
	const wchar_t* nxlFilePath,
	wchar_t** emails, int* peSize,
	wchar_t** addEmials, int* paeSize,
	wchar_t** removEmails, int*preSize);

NXSDK_API DWORD SDWL_User_GetRecipents3(HANDLE hUser, const wchar_t* nxlFilePath,
	wchar_t** emails, wchar_t** addEmails, wchar_t** removeEmails);

NXSDK_API DWORD SDWL_User_UploadFile(HANDLE hUser, const wchar_t* nxlFilePath, const wchar_t* sourcePath, const wchar_t* recipients = nullptr, const wchar_t* comments = nullptr);

NXSDK_API DWORD SDWL_User_UploadMyVaultFile(HANDLE hUser, const wchar_t* nxlFilePath, const wchar_t* sourcePath, const wchar_t* recipients = nullptr, const wchar_t* comments = nullptr);

NXSDK_API DWORD SDWL_User_ListMyVaultAllFiles(
	HANDLE hUser,
	const wchar_t* orderBy,
	const wchar_t* searchString,
	MyVaultFileInfo** ppFiles,
	uint32_t* psize);

NXSDK_API DWORD SDWL_User_ListMyVaultFiles(
	    HANDLE hUser,
		uint32_t pageId, 
		uint32_t pageSize, 
		const wchar_t* orderBy, 
		const wchar_t* searchString,
		MyVaultFileInfo** ppFiles,
		uint32_t* psize);

NXSDK_API DWORD SDWL_User_DownloadMyVaultFiles(
	HANDLE hUser,
	const wchar_t* rmsFilePathId,
	const wchar_t* downloadLocalFolder,
	Type_DownlaodFile type, 
	const wchar_t** outPath
	);

NXSDK_API DWORD SDWL_User_DownloadMyVaultPartialFiles(
	HANDLE hUser,
	const wchar_t* rmsFilePathId,
	const wchar_t* downloadLocalFolder,
	Type_DownlaodFile type,
	const wchar_t** outPath
);

NXSDK_API DWORD SDWL_User_OpenFile(HANDLE hUser, 
	const wchar_t* nxl_path,
	HANDLE* hNxlFile);

NXSDK_API DWORD SDWL_User_CacheRPMFileToken(HANDLE hUser,
	const wchar_t* nxl_path);

NXSDK_API DWORD SDWL_User_CloseFile(HANDLE hUser,
	HANDLE hNxlFile);

NXSDK_API DWORD SDWL_User_ForceCloseFile(HANDLE hUser,
	const wchar_t* nxl_path);

NXSDK_API DWORD SDWL_User_DecryptNXLFile(HANDLE hUser, 
	HANDLE hNxlFile, 
	const wchar_t* path);

NXSDK_API DWORD SDWL_User_UploadActivityLogs(HANDLE hUser);

// call RMS to fetch
NXSDK_API DWORD SDWL_User_GetHeartBeatInfo(HANDLE hUser);

// must call SDWL_User_GetHeartBeatInfo first
//NXSDK_API DWORD SDWL_User_GetPolicyBundle(HANDLE hUser, char* tenantName, char** ppPolicyBundle);

// must call SDWL_User_GetHeartBeatInfo first
NXSDK_API DWORD SDWL_User_GetWaterMarkInfo(HANDLE hUser, WaterMark* pWaterMark);

NXSDK_API DWORD SDWL_User_GetHeartBeatFrequency(HANDLE hUser, uint32_t* nSeconds);


NXSDK_API DWORD SDWL_User_GetProjectsInfo(HANDLE hUser, 
	ProjtectInfo** pProjects, int* pSize);

NXSDK_API DWORD SDWL_User_CheckProjectEnableAdhoc(HANDLE hUser, const wchar_t * projectTenandId, bool * isEnable);

NXSDK_API DWORD SDWL_User_CheckSystemBucketEnableAdhoc(HANDLE hUser, bool * isEnable);

NXSDK_API DWORD SDWL_User_CheckSystemProject(HANDLE hUser, const wchar_t * projectTenandId, int * systemProjectId, const wchar_t** systemProjectTenandId);

NXSDK_API DWORD SDWL_User_CheckInPlaceProtection(HANDLE hUser, const wchar_t * projectTenandId, bool * DeleteSource);

NXSDK_API DWORD SDWL_User_GetListProjtects(HANDLE hUser, 
	uint32_t pagedId, uint32_t pageSize,
	const wchar_t* orderBy, ProjtectFilter filter);

NXSDK_API DWORD SDWL_User_ProjectDownloadFile(HANDLE hUser, 
	uint32_t projectId, 
	const wchar_t* pathId,
	const wchar_t* downloadPath, 
	Type_DownlaodFile type, 
	const wchar_t** outPath);

NXSDK_API DWORD SDWL_User_ProjectDownloadPartialFile(HANDLE hUser,
	uint32_t projectId,
	const wchar_t* pathId,
	const wchar_t* downloadPath,
	Type_DownlaodFile type,
	const wchar_t** outPath);

NXSDK_API DWORD SDWL_User_ProjectListAllFiles(HANDLE hUser,
	uint32_t projectId,
	const wchar_t* orderby,
	const wchar_t* pathId,
	const wchar_t* searchStr,
	ProjectFileInfo** pplistFiles,
	uint32_t* plistSize);

NXSDK_API DWORD SDWL_User_ProjectListFiles(HANDLE hUser, 
	uint32_t projectId, 
	uint32_t pagedId, 
	uint32_t pageSize,
	const wchar_t* orderby,
	const wchar_t* pathId,
	const wchar_t* searchStr,
	ProjectFileInfo** pplistFiles, 
	uint32_t* plistSize);

NXSDK_API DWORD SDWL_User_ProjectUploadFile(HANDLE hUser,
	uint32_t projectid, 
	const wchar_t* rmsDestFolder, 
	HANDLE hNxlFile);

NXSDK_API DWORD SDWL_User_ProjectUploadFile2(HANDLE hUser,
	uint32_t projectid,
	const wchar_t* rmsDestFolder,
	const wchar_t* nxlFilePath);

NXSDK_API DWORD SDWL_User_Inner_UploadEditedProjectFile(
	HANDLE hUser,
	uint32_t projectid,
	const wchar_t * rmsDestFolder,
	HANDLE hNxlFile);

NXSDK_API DWORD UploadEditedProjectFile(HANDLE hUser,
	uint32_t projectid,
	const wchar_t* rmsDestFolder,
	const wchar_t* nxlFilePath);


NXSDK_API DWORD SDWL_User_ProjectClassifacation(HANDLE hUser, 
	const wchar_t * tenantid,
	ProjectClassifacationLables** ppProjectClassifacationLables,
	uint32_t* pSize);

NXSDK_API DWORD SDWL_User_ListSharedWithMeAllFiles(
	HANDLE hUser,
	const wchar_t* orderBy,
	const wchar_t* searchString,
	SharedWithMeFileInfo** ppFiles,
	uint32_t* pSize
);

NXSDK_API DWORD SDWL_User_ListSharedWithMeFiles(
	HANDLE hUser,
	uint32_t pageId, uint32_t pageSize, 
	const wchar_t* orderBy,
	const wchar_t* searchString,
	SharedWithMeFileInfo** ppFiles,
	uint32_t* pSize
	);

NXSDK_API DWORD SDWL_User_DownloadSharedWithMeFiles(
	HANDLE hUser,
	const wchar_t* transactionId,
	const wchar_t* transactionCode,
	const wchar_t* downlaodDestLocalFolder,
	bool forViewer, 
	const wchar_t** outPath);

NXSDK_API DWORD SDWL_User_DownloadSharedWithMePartialFiles(
	HANDLE hUser,
	const wchar_t* transactionId,
	const wchar_t* transactionCode,
	const wchar_t* downlaodDestLocalFolder,
	bool forViewer,
	const wchar_t** outPath);

NXSDK_API DWORD SDWL_User_EvaulateNxlFileRights(
	HANDLE hUser,
	const wchar_t* filePath,
	int** pprights, int* pLen,
	WaterMark* pWaterMark
);

NXSDK_API DWORD SDWL_User_AddNxlFileLog(
	HANDLE hUser,
	const wchar_t* filePath,
	int Oper,
	bool isAllow
);

NXSDK_API DWORD SDWL_User_GetPreference(
	HANDLE hUser,
	Expiration* expiration,
	wchar_t**  watermarkStr
);

NXSDK_API DWORD SDWL_User_UpdatePreference(
	HANDLE hUser,
	Expiration expiration,
	const wchar_t*  watermarkStr
);

NXSDK_API DWORD SDWL_User_GetNxlFileFingerPrint(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	NXL_FILE_FINGER_PRINT* pFingerPrint);

NXSDK_API DWORD SDWL_User_GetNxlFileTagsWithoutToken(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	wchar_t** pTags);

NXSDK_API DWORD SDWL_User_UpdateNxlFileRights(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	int* rights, int rightsArrLength,
	WaterMark watermark, Expiration expiration,
	const wchar_t* tags);

NXSDK_API DWORD SDWL_User_UpdateProjectNxlFileRights(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	const uint32_t projectId,
	const wchar_t* fileName,
	const wchar_t* parentPathId,
	int* rights, int rightsArrLength,
	WaterMark watermark, Expiration expiration,
	const wchar_t* tags);

NXSDK_API DWORD SDWL_User_GetMyVaultFileMetaData(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	const wchar_t* pathId,
	MYVAULT_FILE_META_DATA* metadata);

NXSDK_API DWORD SDWL_User_MyVaultShareFile(
	HANDLE hUser,
	const wchar_t* nxlLocalPath,
	const wchar_t* recipents,
	const wchar_t* repositoryId,
	const wchar_t* fileName,
	const wchar_t* filePathId,
	const wchar_t* filePath,
	const wchar_t* comments);

NXSDK_API DWORD SDWL_User_SharedWithMeReshareFile(
	HANDLE hUser,
	const wchar_t* transactionId,
	const wchar_t* transactionCode,
	const wchar_t* emaillist);

NXSDK_API DWORD SDWL_User_ResetSourcePath(
	HANDLE hUser,
	const wchar_t* nxlFilePath,
	const wchar_t* sourcePath);

NXSDK_API DWORD SDWL_User_GetFileRightsFromCentralPoliciesByTenant(
	HANDLE hUser,
	const wchar_t* tenantName,
	const wchar_t* tags,
	CENTRAL_RIGHTS** pArray,
	uint32_t* pArrSize);

NXSDK_API DWORD SDWL_User_GetFileRightsFromCentralPolicyByProjectId(
	HANDLE hUser,
	const uint32_t projectId,
	const wchar_t* tags,
	CENTRAL_RIGHTS** pArray,
	uint32_t* pArrSize);
#pragma endregion //SDWL_User

#pragma region LocalFiles
//
// Wrapper ISDRFiles
//
NXSDK_API DWORD SDWL_File_GetListNumber(HANDLE hFile,  int* pSize);

NXSDK_API DWORD SDWL_File_GetList(HANDLE hFile, wchar_t** strArray, int* pSize);

NXSDK_API DWORD SDWL_File_GetFile(HANDLE hFile, int index, HANDLE* hNxlFile);

NXSDK_API DWORD SDWL_File_GetFile2(HANDLE hFile, 
	const wchar_t* FileName, HANDLE* hNxlFile);

NXSDK_API DWORD SDWL_File_RemoveFile(HANDLE hFile, HANDLE hNxlFile, bool *pResult);

#pragma endregion //LocalFiles

#pragma region NxlFile
//
// Wrapper ISDRNXLFile
//
NXSDK_API DWORD SDWL_NXL_File_GetFileName(HANDLE hNxlFile, wchar_t** ppname);

NXSDK_API DWORD SDWL_NXL_File_GetFileSize(HANDLE hNxlFile, DWORD64* pSize);

NXSDK_API DWORD SDWL_NXL_File_IsValidNxl(HANDLE hNxlFile, bool* pResult );

NXSDK_API DWORD SDWL_NXL_File_GetRights(HANDLE hNxlFile, int** pprights, int* pLen );

NXSDK_API DWORD SDWL_NXL_File_GetWaterMark(HANDLE hNxlFile, WaterMark* pWaterMark);

NXSDK_API DWORD SDWL_NXL_File_GetExpiration(HANDLE hNxlFile, Expiration* pExpiration);

NXSDK_API DWORD SDWL_NXL_File_GetTags(HANDLE hNxlFile, wchar_t** ppTags);

NXSDK_API DWORD SDWL_NXL_File_CheckRights(HANDLE hNxlFile, int right, bool* pResult);

//NXSDK_API DWORD SDWL_NXL_File_GetClassificationSetting(HANDLE hNxlFile, char** ppResult);

NXSDK_API DWORD SDWL_NXL_File_IsUploadToRMS(HANDLE hNxlFile, bool* pResult);

NXSDK_API DWORD SDWL_NXL_File_GetAdhocWatermarkString(HANDLE hNxlFile, wchar_t** ppWmStr);

NXSDK_API DWORD SDWL_NXL_File_GetActivityInfo(const wchar_t* fileName,
	NXL_FILE_ACTIVITY_INFO** ppInfo,
	DWORD* pSize);

//NXSDK_API DWORD SDWL_NXL_File_GetNxlFileActivityLog(HANDLE hNxlFile,
//	DWORD64 startPos, DWORD64 count,
//	BYTE searchField, 
//	const wchar_t* searchText,
//	BYTE orderByField, 
//	bool orderByReverse);
#pragma endregion //NxlFile


#pragma region SysHelper

// return_value   
//		true: continue monitor  
//		false: return;
typedef  void (__stdcall *RegChangeCallback)(const wchar_t* path);

NXSDK_API DWORD SDWL_SYSHELPER_MonitorRegValueDeleted(const wchar_t* regValuename, RegChangeCallback callback);



#pragma endregion









