#include "stdafx.h"
#include "ContextMenu.h"
#include "resource.h"
#include <winternl.h>
#include "nxrmshellglobal.h"
#include "nxrmshell.h"
#include "helper.h"
#include "tag.h"


#pragma warning(disable:4101)

#define IsSupportNxlMenu 1

#ifdef __cplusplus
extern "C" {
#endif

	extern SHELL_GLOBAL_DATA Global;

	extern 	BOOL init_rm_section_safe(void);

#ifdef __cplusplus
}
#endif


//// {85266F9F-9F6D-4E53-B9B8-8B4452443C99}
extern "C" const GUID CLSID_IRmCtxMenu = { 0x85266f9f, 0x9f6d, 0x4e53,{ 0xb9, 0xb8, 0x8b, 0x44, 0x52, 0x44, 0x3c, 0x99 } };


//
// const for named pipe
//
const std::wstring PIPE_NAME_PREFIX = L"\\\\.\\pipe\\nxrmtray_";



//
// design for menu of normal plain file 
//

static const WCHAR MenuGroupName[] = L"SkyDRM";

typedef enum _MenuCommand {
	CmdCreateAProtectedFile = 0,
	CmdShareAProtectedFile,
	CmdSeperator,
	CmdGotoSkyDRMLocal,
	CmdOpenSkyDRMDotCOM,
	CmdMenuCount //for counting purpose. Define all menu ID before it
} MenuCommand;

#define MAX_MENU_ITEM		_MenuCommand::CmdMenuCount

static const wchar_t* 
Default_MenuCommandNameW[MAX_MENU_ITEM] = {
	L"Protect",
	L"Share",
	L"--",
	L"Go to SkyDRM Desktop",
	L"Open SkyDRM Web"
};
static const char * 
Default_MenuCommandNameA[MAX_MENU_ITEM] = {
	"Protect",
	"Share",
	"--",
	"Go to SkyDRM Desktop",
	"Open SkyDRM Web"
};


//
//  design for nxl file menu
//
typedef enum _NxlMenuCommand {
	CmdNxlViewFile,
	CmdNxlViewFileInfo,
	CmdNxlShare,
	CmdNxlAddFileToProject,
	CmdNxlExtractConent,
	CmdNxlSeperator,
	CmdNxlModifyRights,
	//CmdNxlEditFile,
	CmdNxlSeperator2,
	//CmdNxlDelete,
	//CmdNxlSeperator3,
	CmdNxlOpenSkyDRMLocal,
	CmdNxlOpenSkyDRMDotCOM,
	CmdNxlMenuCount //for counting purpose. Define all menu ID before it
} NxlMenuCommand;

static const WCHAR NxlMenuGroupName[] = L"SkyDRM";

static const wchar_t* 
Default_NxlMenuCommandNameW[_NxlMenuCommand::CmdNxlMenuCount] = {
	    L"View file",
		L"View file info",
		L"Share",
	    L"Add file to",
		L"Extract",
		L"--",
		L"Modify permissions",
		L"--",
		L"Go to SkyDRM Desktop",
		L"Open SkyDRM Web",
};
static const char * 
Default_NxlMenuCommandNameA[_NxlMenuCommand::CmdNxlMenuCount] = {
	    "View file",
		"View file info",
		"Share",
	    "Add file to",
		"Extract",
		"--",
		"Modify permissions",
		"--",
		"Go to SkyDRM Desktop",
		"Open SkyDRM Web",
};


IRmCtxMenu::IRmCtxMenu()
	: m_uRefCount(1),
	m_bIsNXLFile(FALSE),
	m_SkyDRMAppPath(L""),
	m_regCtxMenuFilter(std::wregex(L".*", std::regex_constants::icase))
{
	InitUIResource();
	// set up grep string
	std::wstring bypassstr = L"^[c-zC-Z]{1}:\\\\windows\\\\.*|.*\\.exe$|.*\\.dll$|.*\\.ttf$|.*\\.gdoc$|.*\\.gsheet$|.*\\.gslides$|.*\\.gdraw";
	m_regBypassedFilter = std::wregex(bypassstr, std::regex_constants::icase);
}

IRmCtxMenu::~IRmCtxMenu()
{
	DeinitUIResource();
}

STDMETHODIMP IRmCtxMenu::QueryInterface(REFIID riid, void **ppobj)
{
	HRESULT hRet = S_OK;

	void *punk = NULL;

	*ppobj = NULL;

	do
	{
		if (IID_IUnknown == riid || IID_IShellExtInit == riid)
		{
			punk = (IShellExtInit *)this;
		}
		else if (IID_IContextMenu == riid)
		{
			punk = (IContextMenu*)this;
		}
		else
		{
			hRet = E_NOINTERFACE;
			break;
		}

		AddRef();

		*ppobj = punk;

	} while (FALSE);

	return hRet;
}

STDMETHODIMP_(ULONG) IRmCtxMenu::AddRef()
{
	m_uRefCount++;

	return m_uRefCount;
}

STDMETHODIMP_(ULONG) IRmCtxMenu::Release()
{
	ULONG uCount = 0;

	if (m_uRefCount)
		m_uRefCount--;

	uCount = m_uRefCount;

	if (!uCount)
	{
		delete this;
		InterlockedDecrement(&Global.ContextMenuInstanceCount);
	}

	return uCount;
}


/*
	Filter out: Multiple_Selection, Folder, Grep_Matched
*/
STDMETHODIMP IRmCtxMenu::Initialize(_In_opt_  PCIDLIST_ABSOLUTE pidlFolder, _In_opt_  IDataObject *pdtobj, _In_opt_  HKEY hkeyProgID)
{
	OutputDebugString(L"Enter --> IRmCtxMenu::Initialize");
	HRESULT hr = S_OK;
	m_bIsNXLFile = FALSE;

	if (!get_skydrm_exe_path(m_SkyDRMAppPath)) {
		// can not get app path, then all menu itmes will be invalid
		return E_FAIL;
	}

	if (!get_viewer_exe_path(m_ViewerAppPath)) {
		// can not get app path, then all menu itmes will be invalid
		return E_FAIL;
	}

	std::vector<std::wstring> selected_files;
	selected_files = query_selected_file(pdtobj);
	if (selected_files.size() != 1) {
		// - Only supprot 1 file to show Menu-ext currently
		return E_FAIL;
	}
	m_strSelectedFile = fs::convert_dos_full_file_path(selected_files.front());

	//bypass directory
	if (is_dir(m_strSelectedFile.c_str())) {
		m_strSelectedFile.clear();
		return E_FAIL;
	}
	
	// check extension bypass filter and Menu filter
	
	// Now ignore the file type filter.
	/*
	if (std::regex_match(m_strSelectedFile, m_regBypassedFilter)
		|| (!std::regex_match(m_strSelectedFile, m_regCtxMenuFilter))) {
		m_strSelectedFile.clear();
		return E_FAIL;
	} */
	   
	m_bIsNXLFile = is_nxl_suffix(m_strSelectedFile);
	
	if (!IsSupportNxlMenu && m_bIsNXLFile) {
		return E_FAIL;
	}

	// Try to read office365 file tag.
	bool bIsOffice = isOfficeFormatFile(m_strSelectedFile);
	if (bIsOffice && !m_bIsNXLFile) 
	{
		std::wstring libPath = GetTagLibPath();
		std::vector <CTag::TAGPAIR> outVec;
		CTag tag;
		tag.InitTagLib(libPath);
		if (tag.InitSucess() && tag.GetTags(m_strSelectedFile, outVec)) 
		{
			this->m_OfficeTagValue = tag.ParseTag(outVec);
		}
	}

	// Here handle the IPC between explorer plugin with service manager to check file rights
	// and do some disable for the context menu items.
	if (m_bIsNXLFile)
	{
		// Get current user session id.
		DWORD pid = GetCurrentProcessId();
		DWORD sid;
		BOOL ret = ProcessIdToSessionId(pid, &sid);
		if (ret)
		{
			std::wstring pipeName = PIPE_NAME_PREFIX + std::to_wstring(sid);

			// Log
			std::wstring log = L"pipe name is: -->" + pipeName;
			::OutputDebugString(log.c_str());

			NamedPipeClient client(pipeName, m_strSelectedFile);
			if (client.OnConnectPipe())
			{
				// write local path to pipe
				client.OnWritePipe();
				// read rights string from pipe
				client.OnReadPipe();
				m_strNxlFileFingerPrint = client.GetReceivedData();
			}
		}
		else
		{
			::OutputDebugString(L"Call ProcessIdToSessionId failed!");
			::OutputDebugString(std::to_wstring(GetLastError()).c_str());
		}
	}

	return S_OK;
}


HRESULT IRmCtxMenu::InsertNormalFileMenu(HMENU hmenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags)
{
	HRESULT hr = S_OK;

	HMENU   hSubMenu = NULL;
	UINT    uSubMenuFlags = 0;
	UINT	uMenuInd = 0; //The total number of added menus
	UINT	uMenuPos = 0; //The Position of added menu

	// If Flags contains CMF_DEFAULTONLY, ignore it
	if ((uFlags & CMF_DEFAULTONLY) || m_strSelectedFile.empty()) {
		return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
	}



	// Create/Insert Sub Menus
	hSubMenu = CreateMenu();
	if (NULL == hSubMenu) {
		hr = HRESULT_FROM_WIN32(GetLastError());
		return hr;
	}

	//
	// Add Sub menus below. 
	// Increase uMenuInd for menu item which need be handled
	// Increase uMenuPos for every menu item including MF_SEPARATOR etc.

	{
		//Protected
		uSubMenuFlags = MF_STRING | MF_BYPOSITION;
		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd,
			Default_MenuCommandNameW[CmdCreateAProtectedFile]);
		SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hCreateBitmap, m_hCreateBitmap);
		uMenuInd++;
		uMenuPos++;
	}
	{
		//Share
		uSubMenuFlags = MF_STRING | MF_BYPOSITION;

		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, 
			Default_MenuCommandNameW[CmdShareAProtectedFile]);
		SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hShareBitmap, m_hShareBitmap);
		uMenuInd++;
		uMenuPos++;
	}
	{
		// Insert START separator
		InsertMenuW(hSubMenu, uMenuPos, MF_SEPARATOR | MF_BYPOSITION, idCmdFirst + uMenuInd, NULL);
		uMenuInd++;
		uMenuPos++;
	}
	{
		//Go to Local
		uSubMenuFlags = MF_STRING | MF_BYPOSITION;
		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd,
			Default_MenuCommandNameW[CmdGotoSkyDRMLocal]);
		SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hGoLocalBitmap, m_hGoLocalBitmap);
		uMenuInd++;
		uMenuPos++;
	}
	{
		//Open web
		uSubMenuFlags = MF_STRING | MF_BYPOSITION;
		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, 
			Default_MenuCommandNameW[CmdOpenSkyDRMDotCOM]);
		SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hGoWebBitmap, m_hGoWebBitmap);
		uMenuInd++;
		uMenuPos++;
	}


	// Insert START separator
	InsertMenuW(hmenu, indexMenu++, MF_SEPARATOR | MF_BYPOSITION, 0, NULL);
	// Insert Sub Menus -> SkyDRM Menu
	MENUITEMINFOW mii = { sizeof(MENUITEMINFOW) };
	mii.fMask = MIIM_CHECKMARKS | MIIM_SUBMENU | MIIM_STRING | MIIM_ID;
	mii.wID = idCmdFirst + uMenuInd;
	uMenuInd++;
	mii.hSubMenu = hSubMenu;
	mii.hbmpChecked = m_hMenuBitmap;
	mii.hbmpUnchecked = m_hMenuBitmap;
	mii.dwTypeData = (wchar_t *)MenuGroupName;
	InsertMenuItemW(hmenu, indexMenu++, TRUE, &mii);

	//InsertMenuW(hmenu, indexMenu, MF_STRING | MF_POPUP | MF_BYPOSITION, (UINT_PTR)hSubMenu, MenuGroupName);
	//SetMenuItemBitmaps(hmenu, indexMenu, MF_BYPOSITION, m_hMenuBitmap, m_hMenuBitmap);

	// Finally, let Windows Explorer know how many menu items have been added. Count all parent/child menu
	hr = MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, uMenuInd + 2);

	return hr;
}

HRESULT IRmCtxMenu::InsertNxlFileMenu(HMENU hmenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags)
{
	HRESULT hr = S_OK;

	HMENU   hSubMenu = NULL;
	UINT    uSubMenuFlags = 0;
	UINT	uMenuInd = 0; //The total number of added menus
	UINT	uMenuPos = 0; //The Position of added menu

	// If Flags contains CMF_DEFAULTONLY, ignore it
	if ((uFlags & CMF_DEFAULTONLY) || m_strSelectedFile.empty()) {
		return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
	}

	// Insert START separator
	InsertMenuW(hmenu, indexMenu++, MF_SEPARATOR | MF_BYPOSITION, 0, NULL);

	// Create/Insert Sub Menus
	hSubMenu = CreateMenu();
	if (NULL == hSubMenu) {
		hr = HRESULT_FROM_WIN32(GetLastError());
		return hr;
	}

	bool bIsInstallRmd = (m_SkyDRMAppPath.size() != 0);
	bool bIsInstallViewer = (m_ViewerAppPath.size() != 0);

	//
	// Add Sub menus below
	// Increase uMenuInd for menu item which need be handled
	// Increase uMenuPos for every menu item including MF_SEPARATOR etc.

	{
		// CmdNxlViewFile -- load viewer
		bool bCanViewFile = bIsInstallViewer && hasRights(m_strNxlFileFingerPrint, RIGHT_VIEW);
		uSubMenuFlags = bCanViewFile ? (MF_STRING | MF_BYPOSITION) : (MF_STRING | MF_BYPOSITION | MF_GRAYED | MF_DISABLED);

		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlViewFile]);
		if (bCanViewFile)
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlViewFileBitmap, m_hNxlViewFileBitmap);
		else
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlViewFileBitmap_Gray, m_hNxlViewFileBitmap_Gray);

		uMenuInd++;
		uMenuPos++;
	}

	{
		// CmdNxlViewFileInfo
		bool bCanViewFileInfo = bIsInstallRmd;
		uSubMenuFlags = bCanViewFileInfo ? (MF_STRING | MF_BYPOSITION) : (MF_STRING | MF_BYPOSITION | MF_GRAYED | MF_DISABLED);

		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlViewFileInfo]);
		if (bCanViewFileInfo)
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlViewFileInfoBitmap, m_hNxlViewFileInfoBitmap);
		else
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlViewFileInfoBitmap_Gray, m_hNxlViewFileInfoBitmap_Gray);

		uMenuInd++;
		uMenuPos++;
	}

	{
		// Share
		bool bCanShare = bIsInstallRmd && hasRights(m_strNxlFileFingerPrint, RIGHT_SHARE) && isAdhoc(m_strNxlFileFingerPrint) && isFromMyVault(m_strNxlFileFingerPrint);
		uSubMenuFlags = bCanShare ? (MF_STRING | MF_BYPOSITION) : (MF_STRING | MF_BYPOSITION | MF_GRAYED | MF_DISABLED);

		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlShare]);
		if (bCanShare)
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlShareBitmap, m_hNxlShareBitmap);
		else
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlShareBitmap_Gray, m_hNxlShareBitmap_Gray);

		uMenuInd++;
		uMenuPos++;
	}

    // Add nxl
	{
		// add file to other space
		bool bCanAdd = false;
		if (isSaaSRouter(m_strNxlFileFingerPrint)) // SaaS version.
		{
			bCanAdd = bIsInstallRmd 
				      && (isFromMyVault(m_strNxlFileFingerPrint) 
						  || isFromProject(m_strNxlFileFingerPrint));
		}
		else // Enterprise version
		{
			//bCanAdd = bIsInstallRmd 
			//	      && (
			//			  isFromMyVault(m_strNxlFileFingerPrint) // myVault
			//			  || isFromSystemBucket(m_strNxlFileFingerPrint) // system bucket
			//	          || (isFromProject(m_strNxlFileFingerPrint)    // project file
			//		           && ( hasAdminRights(m_strNxlFileFingerPrint) || hasRights(m_strNxlFileFingerPrint, RIGHT_DECRYPT))));

			//  change the add file to  right  from  view to download
			if (!bIsInstallRmd)
			{
				bCanAdd = false;
			}
			else
			{
				if (isFromMyVault(m_strNxlFileFingerPrint)) // my vault
				{
					bCanAdd = hasRights(m_strNxlFileFingerPrint, RIGHT_DOWNLOAD);
				}
				else if (isFromSystemBucket(m_strNxlFileFingerPrint)) //  system bucket
				{
					if (hasAdminRights(m_strNxlFileFingerPrint)) // if from workapace only need view right
					{
						bCanAdd = hasRights(m_strNxlFileFingerPrint, RIGHT_VIEW);
					}
					else
					{
						bCanAdd = (hasRights(m_strNxlFileFingerPrint, RIGHT_DOWNLOAD) || hasRights(m_strNxlFileFingerPrint, RIGHT_DECRYPT));
					}
				}
				else if (isFromProject(m_strNxlFileFingerPrint)) // proiect
				{
					bCanAdd = (hasAdminRights(m_strNxlFileFingerPrint) || hasRights(m_strNxlFileFingerPrint, RIGHT_DECRYPT));
				}
			}
		}
			           			    
		uSubMenuFlags = bCanAdd ? (MF_STRING | MF_BYPOSITION) : (MF_STRING | MF_BYPOSITION | MF_GRAYED | MF_DISABLED);

		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlAddFileToProject]);
		if (bCanAdd)
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlAddFileToProjectBitmap, m_hNxlAddFileToProjectBitmap);
		else
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlAddFileToProjectBitmap_Gray, m_hNxlAddFileToProjectBitmap);

		uMenuInd++;
		uMenuPos++;
	}

	{
		// Extract content -- disable it for myVault files.
		bool bCanExtract = bIsInstallRmd && hasRights(m_strNxlFileFingerPrint, RIGHT_DECRYPT) && !isFromMyVault(m_strNxlFileFingerPrint);
		uSubMenuFlags = bCanExtract ? (MF_STRING | MF_BYPOSITION) : (MF_STRING | MF_BYPOSITION | MF_GRAYED | MF_DISABLED);

		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlExtractConent]);
		if (bCanExtract)
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlExtractContentBitmap, m_hNxlExtractContentBitmap);
		else
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlExtractContentBitmap_Gray, m_hNxlExtractContentBitmap_Gray);

		uMenuInd++;
		uMenuPos++;
	}

	{
		// Insert  separator
		InsertMenuW(hSubMenu, uMenuPos, MF_SEPARATOR | MF_BYPOSITION, idCmdFirst + uMenuInd, NULL);
		uMenuInd++;
		uMenuPos++;
	}

	{
		// CmdNxlModifyRights
		bool bCanModify = bIsInstallRmd && hasAdminRights(m_strNxlFileFingerPrint) && !isAdhoc(m_strNxlFileFingerPrint);
		uSubMenuFlags = bCanModify ? (MF_STRING | MF_BYPOSITION) : (MF_STRING | MF_BYPOSITION | MF_GRAYED | MF_DISABLED);

		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlModifyRights]);
		if(bCanModify)
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlModifyRightsBitmap, m_hNxlModifyRightsBitmap);
		else
			SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlModifyRightsBitmap_Gray, m_hNxlModifyRightsBitmap_Gray);

		uMenuInd++;
		uMenuPos++;
	}

	//{
	//	// CmdNxlEditFile
	//	uSubMenuFlags = hasRights(m_strNxlFileFingerPrint, RIGHT_EDIT) ? (MF_STRING | MF_BYPOSITION) : (MF_STRING | MF_BYPOSITION | MF_DISABLED);

	//	InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlEditFile]);
	//	SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlEditFileBitmap, m_hNxlEditFileBitmap);
	//	uMenuInd++;
	//	uMenuPos++;
	//}

	{
		// Insert  separator
		InsertMenuW(hSubMenu, uMenuPos, MF_SEPARATOR | MF_BYPOSITION, idCmdFirst + uMenuInd, NULL);
		uMenuInd++;
		uMenuPos++;
	}

	//{
	//	// CmdNxlDelete
	//	uSubMenuFlags = MF_STRING | MF_BYPOSITION;
	//	InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlDelete]);
	//	SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlDeleteBitmap, m_hNxlDeleteBitmap);
	//	uMenuInd++;
	//	uMenuPos++;
	//}

	//{
	//	// Insert  separator
	//	InsertMenuW(hSubMenu, uMenuPos, MF_SEPARATOR | MF_BYPOSITION, idCmdFirst + uMenuInd, NULL);
	//	uMenuInd++;
	//	uMenuPos++;
	//}

	{
		// CmdNxlOpenSkyDRMLocal
		uSubMenuFlags = MF_STRING | MF_BYPOSITION;
		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlOpenSkyDRMLocal]);
		SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlOpenSkyDRMLocalBitmap, m_hNxlOpenSkyDRMLocalBitmap);
		uMenuInd++;
		uMenuPos++;
	}

	{
		// CmdNxlOpenSkyDRMDotCOM
		uSubMenuFlags = MF_STRING | MF_BYPOSITION;
		InsertMenuW(hSubMenu, uMenuPos, uSubMenuFlags, idCmdFirst + uMenuInd, Default_NxlMenuCommandNameW[CmdNxlOpenSkyDRMDotCOM]);
		SetMenuItemBitmaps(hSubMenu, uMenuPos, MF_BYPOSITION, m_hNxlOpenSkyDRMComBitmap, m_hNxlOpenSkyDRMComBitmap);
		uMenuInd++;
		uMenuPos++;
	}

	// Insert Sub Menus -> SkyDRM Menu
	InsertMenuW(hmenu, indexMenu, MF_STRING | MF_POPUP | MF_BYPOSITION, (UINT_PTR)hSubMenu, NxlMenuGroupName);
	SetMenuItemBitmaps(hmenu, indexMenu, MF_BYPOSITION, m_hMenuBitmap, m_hMenuBitmap);

	// Finally, let Windows Explorer know how many menu items have been added. Count all parent/child menu
	hr = MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, uMenuInd + 2);

	return hr;

}

void IRmCtxMenu::InitUIResource()
{
	int nWidth = 24;
	int nDPI = GetCurrentDPI();
	if (nDPI >= 192)
	{
		nWidth = 48;
	}
	else if (nDPI >= 168)
	{
		nWidth = 32;
	}
	else
	{
		nWidth = 24;
	}
	
	// for Menu Icon
	HICON hMenuIcon = (HICON)LoadImage(Global.hModule, MAKEINTRESOURCEW(IDI_ICON_SkyDRM), IMAGE_ICON, nWidth, nWidth, 0);
	m_hMenuBitmap = BitmapFromIcon(hMenuIcon);
	if (hMenuIcon) {
		DestroyIcon(hMenuIcon);
		hMenuIcon = NULL;
	}
	

	{
		// for normal file
		HICON hShareIcon = (HICON)LoadImage(Global.hModule, MAKEINTRESOURCEW(IDI_ICON_SHARE), IMAGE_ICON, nWidth, nWidth, 0);
		m_hShareBitmap = BitmapFromIcon(hShareIcon);
		if (hShareIcon) {
			DestroyIcon(hShareIcon);
			hShareIcon = NULL;
		}


		HICON hCreateIcon = (HICON)LoadImage(Global.hModule, MAKEINTRESOURCEW(IDI_ICON_CREATE), IMAGE_ICON, nWidth, nWidth, 0);
		m_hCreateBitmap = BitmapFromIcon(hCreateIcon);
		if (hCreateIcon) {
			DestroyIcon(hCreateIcon);
			hCreateIcon = NULL;
		}


		HICON hGoLocalIcon = (HICON)LoadImage(Global.hModule, MAKEINTRESOURCEW(IDI_ICON_NXLOGO), IMAGE_ICON, nWidth, nWidth, 0);
		m_hGoLocalBitmap = BitmapFromIcon(hGoLocalIcon);
		if (hGoLocalIcon) {
			DestroyIcon(hGoLocalIcon);
			hGoLocalIcon = NULL;
		}

		HICON hGoWebIcon = (HICON)LoadImage(Global.hModule, MAKEINTRESOURCEW(IDI_ICON_NXWEB), IMAGE_ICON, nWidth, nWidth, 0);
		m_hGoWebBitmap = BitmapFromIcon(hGoWebIcon);
		if (hGoWebIcon) {
			DestroyIcon(hGoWebIcon);
			hGoWebIcon = NULL;
		}
	}	

	{
		// for nxl file

		// Add file to project
		HICON hNxlAddFileToProjectIcon = (HICON)LoadImage(Global.hModule, 
			MAKEINTRESOURCEW(IDI_ICON_Nxl_AddFileToProject), 
			IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlAddFileToProjectBitmap = BitmapFromIcon(hNxlAddFileToProjectIcon);
		if (hNxlAddFileToProjectIcon) {
			DestroyIcon(hNxlAddFileToProjectIcon);
			hNxlAddFileToProjectIcon = NULL;
		}

		HICON hNxlAddFileToProjectIcon_Gray = (HICON)LoadImage(Global.hModule,
			MAKEINTRESOURCEW(IDI_ICON_Nxl_AddFileProject_Gray),
			IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlAddFileToProjectBitmap_Gray = BitmapFromIcon(hNxlAddFileToProjectIcon_Gray);
		if (hNxlAddFileToProjectIcon_Gray) {
			DestroyIcon(hNxlAddFileToProjectIcon_Gray);
			hNxlAddFileToProjectIcon_Gray = NULL;
		}

		// Share
		HICON hNxlShareIcon = (HICON)LoadImage(Global.hModule, 
			MAKEINTRESOURCEW(IDI_ICON_Nxl_Share), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlShareBitmap = BitmapFromIcon(hNxlShareIcon);
		if (hNxlShareIcon) {
			DestroyIcon(hNxlShareIcon);
			hNxlShareIcon = NULL;
		}

		HICON hNxlShareIcon_Gray = (HICON)LoadImage(Global.hModule,
			MAKEINTRESOURCEW(IDI_ICON_Nxl_Share_Gray), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlShareBitmap_Gray = BitmapFromIcon(hNxlShareIcon_Gray);
		if (hNxlShareIcon_Gray) {
			DestroyIcon(hNxlShareIcon_Gray);
			hNxlShareIcon_Gray = NULL;
		}

		// Extract content
		HICON hNxlExtractContentIcon = (HICON)LoadImage(Global.hModule,
			MAKEINTRESOURCEW(IDI_ICON_Nxl_ExtractContent), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlExtractContentBitmap = BitmapFromIcon(hNxlExtractContentIcon);
		if (hNxlExtractContentIcon) {
			DestroyIcon(hNxlExtractContentIcon);
			hNxlExtractContentIcon = NULL;
		}

		HICON hNxlExtractContentIcon_Gray = (HICON)LoadImage(Global.hModule,
			MAKEINTRESOURCEW(IDI_ICON_Nxl_Extract_Gray), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlExtractContentBitmap_Gray = BitmapFromIcon(hNxlExtractContentIcon_Gray);
		if (hNxlExtractContentIcon_Gray) {
			DestroyIcon(hNxlExtractContentIcon_Gray);
			hNxlExtractContentIcon_Gray = NULL;
		}

		// Modify rights
		HICON hNxlModifyRightsIcon = (HICON)LoadImage(Global.hModule, 
			MAKEINTRESOURCEW(IDI_ICON_Nxl_ModifyRights), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlModifyRightsBitmap = BitmapFromIcon(hNxlModifyRightsIcon);
		if (hNxlModifyRightsIcon) {
			DestroyIcon(hNxlModifyRightsIcon);
			hNxlModifyRightsIcon = NULL;
		}

		HICON hNxlModifyRightsIcon_Gray = (HICON)LoadImage(Global.hModule,
			MAKEINTRESOURCEW(IDI_ICON_Nxl_ModifyRights_Gray), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlModifyRightsBitmap_Gray = BitmapFromIcon(hNxlModifyRightsIcon_Gray);
		if (hNxlModifyRightsIcon_Gray) {
			DestroyIcon(hNxlModifyRightsIcon_Gray);
			hNxlModifyRightsIcon_Gray = NULL;
		}

		// Edit
		HICON hNxlEditFileIcon = (HICON)LoadImage(Global.hModule, 
			MAKEINTRESOURCEW(IDI_ICON_Nxl_EditFile), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlEditFileBitmap = BitmapFromIcon(hNxlEditFileIcon);
		if (hNxlEditFileIcon) {
			DestroyIcon(hNxlEditFileIcon);
			hNxlEditFileIcon = NULL;
		}

		// View file
		HICON hNxlViewFileIcon = (HICON)LoadImage(Global.hModule, 
			MAKEINTRESOURCEW(IDI_ICON_Nxl_ViewFile), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlViewFileBitmap = BitmapFromIcon(hNxlViewFileIcon);
		if (hNxlViewFileIcon) {
			DestroyIcon(hNxlViewFileIcon);
			hNxlViewFileIcon = NULL;
		}

		HICON hNxlViewFileIcon_Gray = (HICON)LoadImage(Global.hModule,
			MAKEINTRESOURCEW(IDI_ICON_Nxl_ViewFile_Gray), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlViewFileBitmap_Gray = BitmapFromIcon(hNxlViewFileIcon_Gray);
		if (hNxlViewFileIcon_Gray) {
			DestroyIcon(hNxlViewFileIcon_Gray);
			hNxlViewFileIcon_Gray = NULL;
		}

		// View file info
		HICON hNxlViewFileInfoIcon = (HICON)LoadImage(Global.hModule,
			MAKEINTRESOURCEW(IDI_ICON_Nxl_ViewFileInfo), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlViewFileInfoBitmap = BitmapFromIcon(hNxlViewFileInfoIcon);
		if (hNxlViewFileInfoIcon) {
			DestroyIcon(hNxlViewFileInfoIcon);
			hNxlViewFileInfoIcon = NULL;
		}

		HICON hNxlViewFileInfoIcon_Gray = (HICON)LoadImage(Global.hModule,
			MAKEINTRESOURCEW(IDI_ICON_Nxl_ViewFileInfo_Gray), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlViewFileInfoBitmap_Gray = BitmapFromIcon(hNxlViewFileInfoIcon_Gray);
		if (hNxlViewFileInfoIcon_Gray) {
			DestroyIcon(hNxlViewFileInfoIcon_Gray);
			hNxlViewFileInfoIcon_Gray = NULL;
		}

		// Delete
		HICON hNxlDeleteIcon = (HICON)LoadImage(Global.hModule, 
			MAKEINTRESOURCEW(IDI_ICON_Nxl_Delete), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlDeleteBitmap = BitmapFromIcon(hNxlDeleteIcon);
		if (hNxlDeleteIcon) {
			DestroyIcon(hNxlDeleteIcon);
			hNxlDeleteIcon = NULL;
		}

		// Open local app
		HICON hNxlOpenSkyDRMLocalIcon = (HICON)LoadImage(Global.hModule, 
			MAKEINTRESOURCEW(IDI_ICON_Nxl_OpenSkyDRMLocal), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlOpenSkyDRMLocalBitmap = BitmapFromIcon(hNxlOpenSkyDRMLocalIcon);
		if (hNxlOpenSkyDRMLocalIcon) {
			DestroyIcon(hNxlOpenSkyDRMLocalIcon);
			hNxlOpenSkyDRMLocalIcon = NULL;
		}

		// Open skydrm.com
		HICON hNxlOpenSkyDRMComIcon = (HICON)LoadImage(Global.hModule, 
			MAKEINTRESOURCEW(IDI_ICON_Nxl_OpenSkyDRMWeb), IMAGE_ICON, nWidth, nWidth, 0);
		m_hNxlOpenSkyDRMComBitmap = BitmapFromIcon(hNxlOpenSkyDRMComIcon);
		if (hNxlOpenSkyDRMComIcon) {
			DestroyIcon(hNxlOpenSkyDRMComIcon);
			hNxlOpenSkyDRMComIcon = NULL;
		}

	}

}

void IRmCtxMenu::DeinitUIResource()
{
	if (m_hMenuBitmap) {
		DeleteObject(m_hMenuBitmap);
		m_hMenuBitmap = NULL;
	}

	if (m_hShareBitmap) {
		DeleteObject(m_hShareBitmap);
		m_hShareBitmap = NULL;
	}

	if (m_hCreateBitmap) {
		DeleteObject(m_hCreateBitmap);
		m_hCreateBitmap = NULL;
	}

	if (m_hGoLocalBitmap) {
		DeleteObject(m_hGoLocalBitmap);
		m_hGoLocalBitmap = NULL;
	}

	if (m_hGoWebBitmap) {
		DeleteObject(m_hGoWebBitmap);
		m_hGoWebBitmap = NULL;
	}

	if (m_hNxlAddFileToProjectBitmap) {
		DeleteObject(m_hNxlAddFileToProjectBitmap);
		m_hNxlAddFileToProjectBitmap = NULL;
	}

	if (m_hNxlAddFileToProjectBitmap_Gray) {
		DeleteObject(m_hNxlAddFileToProjectBitmap_Gray);
		m_hNxlAddFileToProjectBitmap_Gray = NULL;
	}

	if (m_hNxlShareBitmap) {
		DeleteObject(m_hNxlShareBitmap);
		m_hNxlShareBitmap = NULL;
	}

	if (m_hNxlShareBitmap_Gray) {
		DeleteObject(m_hNxlShareBitmap_Gray);
		m_hNxlShareBitmap_Gray = NULL;
	}

	if (m_hNxlExtractContentBitmap) {
		DeleteObject(m_hNxlExtractContentBitmap);
		m_hNxlExtractContentBitmap = NULL;
	}

	if (m_hNxlExtractContentBitmap_Gray) {
		DeleteObject(m_hNxlExtractContentBitmap_Gray);
		m_hNxlExtractContentBitmap_Gray = NULL;
	}

	if (m_hNxlModifyRightsBitmap) {
		DeleteObject(m_hNxlModifyRightsBitmap);
		m_hNxlModifyRightsBitmap = NULL;
	}

	if (m_hNxlModifyRightsBitmap_Gray) {
		DeleteObject(m_hNxlModifyRightsBitmap_Gray);
		m_hNxlModifyRightsBitmap_Gray = NULL;
	}

	if (m_hNxlEditFileBitmap) {
		DeleteObject(m_hNxlEditFileBitmap);
		m_hNxlEditFileBitmap = NULL;
	}

	if (m_hNxlViewFileBitmap) {
		DeleteObject(m_hNxlViewFileBitmap);
		m_hNxlViewFileBitmap = NULL;
	}

	if (m_hNxlViewFileBitmap_Gray) {
		DeleteObject(m_hNxlViewFileBitmap_Gray);
		m_hNxlViewFileBitmap_Gray = NULL;
	}

	if (m_hNxlViewFileInfoBitmap) {
		DeleteObject(m_hNxlViewFileInfoBitmap);
		m_hNxlViewFileInfoBitmap = NULL;
	}

	if (m_hNxlViewFileInfoBitmap_Gray) {
		DeleteObject(m_hNxlViewFileInfoBitmap_Gray);
		m_hNxlViewFileInfoBitmap_Gray = NULL;
	}

	if (m_hNxlDeleteBitmap) {
		DeleteObject(m_hNxlDeleteBitmap);
		m_hNxlDeleteBitmap = NULL;
	}

	if (m_hNxlOpenSkyDRMLocalBitmap) {
		DeleteObject(m_hNxlOpenSkyDRMLocalBitmap);
		m_hNxlOpenSkyDRMLocalBitmap = NULL;
	}

	if (m_hNxlOpenSkyDRMComBitmap) {
		DeleteObject(m_hNxlOpenSkyDRMComBitmap);
		m_hNxlOpenSkyDRMComBitmap = NULL;
	}
}


STDMETHODIMP IRmCtxMenu::QueryContextMenu(_In_  HMENU hmenu,
	_In_  UINT indexMenu,
	_In_  UINT idCmdFirst,
	_In_  UINT idCmdLast,
	_In_  UINT uFlags)
{
	if (!m_bIsNXLFile) {
		return InsertNormalFileMenu(hmenu, indexMenu, idCmdFirst, idCmdLast, uFlags);
	}
	else {
		return InsertNxlFileMenu(hmenu, indexMenu, idCmdFirst, idCmdLast, uFlags);
	}
}

STDMETHODIMP IRmCtxMenu::GetCommandString(_In_ UINT_PTR idCmd, 
	_In_ UINT uType, _Reserved_ UINT *pReserved,  CHAR *pszName, _In_ UINT cchMax)
{
	HRESULT hr = S_OK;

	do
	{
		if (uType & GCS_HELPTEXT)
		{
			if (idCmd >= MAX_MENU_ITEM)
			{
				hr = E_INVALIDARG;
				break;
			}

			if (uType & GCS_UNICODE)
			{
				if (!m_bIsNXLFile) {
					wcsncpy_s((LPWCH)pszName, cchMax, Default_MenuCommandNameW[idCmd], _TRUNCATE);
				}
				else {
					wcsncpy_s((LPWCH)pszName, cchMax, Default_NxlMenuCommandNameW[idCmd], _TRUNCATE);
				}

			}
			else
			{
				if (!m_bIsNXLFile) {
					strncpy_s(pszName, cchMax, Default_MenuCommandNameA[idCmd], _TRUNCATE);
				}
				else {
					strncpy_s(pszName, cchMax, Default_NxlMenuCommandNameA[idCmd], _TRUNCATE);

				}
			}
		}
		else
		{
			hr = E_INVALIDARG;
		}

	} while (FALSE);

	return hr;
}


STDMETHODIMP IRmCtxMenu::InvokeCommand(_In_  CMINVOKECOMMANDINFO *pici)
{
	HRESULT hr = S_OK;

	// Verb is a string? ignore this
	if (0 != HIWORD(pici->lpVerb)) {
		return E_INVALIDARG;
	}

	assert(!m_strSelectedFile.empty());

	if (m_strSelectedFile.empty()) {
		return E_INVALIDARG;
	}

	do
	{
		if (!m_bIsNXLFile) {
			switch (LOWORD(pici->lpVerb))
			{
			case CmdShareAProtectedFile:
				OnShareAProtectedFile();
				break;
			case CmdCreateAProtectedFile:
				OnCreateAProtectedFile();
				break;
			case CmdGotoSkyDRMLocal:
				OnGotoSkyDRMLocal();
				break;
			case CmdOpenSkyDRMDotCOM:
				OnOpenSkyDRMWeb();
				break;
			default:
				hr = E_INVALIDARG;
				break;
			}
		}
		else {
			switch (LOWORD(pici->lpVerb))
			{
			case CmdNxlAddFileToProject:
				OnNxlAddFileToProject();
				break;
			case CmdNxlShare:
				OnNxlShare();
				break;
			case CmdNxlExtractConent:
				OnNxlExtractContent();
				break;
			case CmdNxlModifyRights:
				OnNxlModifyRights();
				break;
			//case CmdNxlEditFile:
			//	OnNxlEditFile();
			//	break;
			case CmdNxlViewFile:
				OnNxlViewFile();
				break;
			case CmdNxlViewFileInfo:
				OnNxlViewFileInfo();
				break;
			//case CmdNxlDelete:
			//	OnNxlDelete();
			//	break;
			case CmdNxlOpenSkyDRMLocal:
				OnNxlOpenSkyDRMLocal();
				break;
			case CmdNxlOpenSkyDRMDotCOM:
				OnNxlOpenSkyDRMWeb();
				break;
			default:
				hr = E_INVALIDARG;
				break;
			}
		}
	} while (FALSE);

	return hr;
}



void IRmCtxMenu::OnShareAProtectedFile()
{
	//::MessageBoxW(NULL, Default_MenuCommandNameW[CmdShareAProtectedFile], MenuGroupName, 0);
	std::wstring param = std::wstring() + L"-share " + L"\"" + this->m_strSelectedFile + L"\"";
	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnCreateAProtectedFile()
{
	//::MessageBoxW(NULL, Default_MenuCommandNameW[CmdCreateAProtectedFile], MenuGroupName, 0);
	std::wstring param = L"";
	if (m_OfficeTagValue == L"") {
		param = std::wstring() + L"-protect " + L"\"" + this->m_strSelectedFile + L"\"";
	}
	else {
		 param = std::wstring() + L"-protect " + L"\"" + this->m_strSelectedFile  + 
			L"|" + this->m_OfficeTagValue + L"\"";
	}

	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnGotoSkyDRMLocal()
{
	//::MessageBoxW(NULL, Default_MenuCommandNameW[CmdGotoSkyDRMLocal], MenuGroupName, 0);
	std::wstring param = L"-showMain";
	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnOpenSkyDRMWeb()
{
	//::MessageBoxW(NULL, Default_MenuCommandNameW[CmdOpenSkyDRMDotCOM], MenuGroupName, 0);
	//ShellExecuteW(NULL, L"open", L"https://skydrm.com/", NULL, NULL, SW_SHOW);
	std::wstring param = L"-openSkyDRMWeb";
	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnNxlAddFileToProject()
{
	std::wstring param = std::wstring() + L"-addNxlToProject " + L"\"" + this->m_strSelectedFile + L"\"";
	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnNxlShare()
{
	std::wstring param = std::wstring() + L"-share " + L"\"" + this->m_strSelectedFile + L"\"";
	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnNxlExtractContent()
{
	std::wstring param = std::wstring() + L"-extractContent " + L"\"" + this->m_strSelectedFile + L"\"";
	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnNxlModifyRights()
{
	std::wstring param = std::wstring() + L"-modifyRights " + L"\"" + this->m_strSelectedFile + L"\"";
	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnNxlEditFile()
{
	std::wstring param = std::wstring() + L"-edit " + L"\"" + this->m_strSelectedFile + L"\"";
	SendCmdToViewerApp(param);
}

void IRmCtxMenu::OnNxlViewFile()
{
	std::wstring param = std::wstring() + L"-view " + L"\"" + this->m_strSelectedFile + L"\"";
	SendCmdToViewerApp(param);
}

void IRmCtxMenu::OnNxlViewFileInfo()
{
	std::wstring param = std::wstring() + L"-showfileinfo " + L"\"" + this->m_strSelectedFile + L"\"";
	SendCmdToSkyDRMApp(param);
}

void IRmCtxMenu::OnNxlDelete()
{
	if (!::DeleteFileW(m_strSelectedFile.c_str())) {
		::MessageBoxW(NULL, L"Can not delete this file.", L"Error", MB_ICONERROR);
	}
}

void IRmCtxMenu::OnNxlOpenSkyDRMLocal()
{
	this->OnGotoSkyDRMLocal();
}

void IRmCtxMenu::OnNxlOpenSkyDRMWeb()
{
	this->OnOpenSkyDRMWeb();
}

void IRmCtxMenu::SendCmdToSkyDRMApp(std::wstring cmdParam)
{
	std::wstring workingfolder = m_SkyDRMAppPath.substr(0, m_SkyDRMAppPath.find_last_of('\\'));
	::ShellExecuteW(NULL, L"open", m_SkyDRMAppPath.c_str(), cmdParam.c_str(), workingfolder.c_str(), SW_SHOW);
}

void IRmCtxMenu::SendCmdToViewerApp(std::wstring cmdParam)
{
	std::wstring workingfolder = m_ViewerAppPath.substr(0, m_ViewerAppPath.find_last_of('\\'));
	::ShellExecuteW(NULL, L"open", m_ViewerAppPath.c_str(), cmdParam.c_str(), workingfolder.c_str(), SW_SHOW);
}
