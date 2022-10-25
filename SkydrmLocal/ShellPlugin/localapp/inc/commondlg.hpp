

#ifndef __NXRM_COMMON_DLG_HPP__
#define __NXRM_COMMON_DLG_HPP__
#include <vector>
#include <string>
using namespace std;

enum DlgType { DLGTYPE_SHARING,DLGTYPE_PROTECT, DLGTYPE_PROPERTIES, DLGTYPE_PROTECT_PROJ};

typedef struct 
{
	bool bView;
	bool bEdit;
	bool bPrint;
	bool bShare;
	bool bDownload;
	bool bWaterMark;
	bool bAssignedRights;
	DlgType dlgType;
	HICON	hFileIcon;
	HWND	hWnd;
	std::wstring dtString;
	std::wstring sizeString;
	std::vector<std::wstring> peopleArr;

} NXSharingData;
#define NX_PROJ_NAME_LEN	260
#define NX_PROJ_DESC_LEN	1024

enum NxProjFlag
{
    NX_PROJ_CREATE,
    NX_PROJ_INVITE,
    NX_PROJ_ADDFILE
};

typedef struct {
	wchar_t szProjName[NX_PROJ_NAME_LEN];
	wchar_t szProjDesc[NX_PROJ_DESC_LEN];
	vector<wstring>	projMembers;
    NxProjFlag projFlag;
} NXProjData;



LONG WINAPI RmuShowShareDialog(_In_opt_ HWND hParent, _In_ LPCWSTR wzFile, _In_ NXSharingData &sharingIn, _In_ NXSharingData *pSharingOut, _Out_ LPWSTR * pAdresses);
LONG WINAPI RmuShowProjDialog(_In_opt_ HWND hParent,  _Inout_ NXProjData &sharingIn, _Out_ LPWSTR* ppwzEmailAddrs);
LONG WINAPI  RmuShowManageUsersDialog(_In_opt_ HWND hParent, _In_  const LPWSTR pfilename, _In_ const std::vector<std::wstring> &peopleArr, _Out_ LPWSTR* ppwzEmailAddrs);
VOID WINAPI RmuFreeResource(_In_ PVOID mem);

typedef void (*NXStepProgress)(int percentPont);
typedef void (*NXCancel)();

typedef struct {
	NXCancel cancelCallback;
	NXStepProgress pStepProgress;
	NXCancel closeProgress;
	bool bDisableParent;
	std::wstring titleText;
} ProgressFuncs;




bool WINAPI RmuOpenProgressWindow(HWND hParentWnd, ProgressFuncs &funcs, bool bMarquee = true );
LONG WINAPI RmuShowFilePropertiesDialog(_In_opt_ HWND hParent, _In_ LPCWSTR wzFile, _In_ NXSharingData &sharingIn, _Out_ NXSharingData &sharingOut);
#endif