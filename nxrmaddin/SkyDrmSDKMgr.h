#pragma once
#include <windows.h>
#include "SDWL/SDLAPI.h"

//typedef std::vector<std::pair<SDRmFileRight, std::vector<SDR_WATERMARK_INFO>>>  VECTOR_RIGHTS;
typedef std::vector<SDRmFileRight> VECTOR_RIGHTS;


/*
* 0, not exit and not save
* 1, not exit, but save
* 2, exit and not save
* 3 (and others), exit and save
*/
#define EXIT_MODE_NOTEXIT_NOTSAVE   0
#define EXIT_MODE_NOTEXIT_SAVE      1
#define EXIT_MODE_EXIT_NOTSAVE      2
#define EXIT_MODE_EXIT_SAVE         3

class SkyDrmSDKMgr
{
public:
	static SkyDrmSDKMgr* Instance()
	{
		static SkyDrmSDKMgr* pInstance = new SkyDrmSDKMgr();
		return pInstance;
	}

private:
	SkyDrmSDKMgr();
	SkyDrmSDKMgr(const SkyDrmSDKMgr&) {}
	~SkyDrmSDKMgr();

public:	// SDK init related
	inline const std::string& GetRMXPasscode() { return m_magic_code; }
	bool GetCurrentLoggedInUser();
	bool WaitInstanceInitFinish();
	bool AddTrustedProcess(unsigned long processId, const std::string &security);
	bool NotifyRMXStatus(bool running, const std::string &security);
	bool IsRPMFolderFile(const std::wstring& wstrFilePath);
	bool IsInRPMFolder(const std::wstring& wstrFilePath);
	bool RPMRegisterApp(const std::wstring& appPath, const std::string &security);

#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
	bool IsSanctuaryFolder(const std::wstring &path, uint32_t* dirstatus, std::wstring& filetags);
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR

	void DelegateDeleteFolder(const std::wstring& folder);
	void DelegateDeleteFile(const std::wstring& path);
	bool DelegateCopyFile(const std::wstring& src, const std::wstring& dest, bool deletesrc);

public:	// NXL right section
	HRESULT CheckRights(const WCHAR* wszFullFileName, ULONGLONG& RightMask);

public:	// NXL Edit, Sync the accrumentals into Encrypted one through Driver
	bool EditSaveFile(const std::wstring &filepath, const std::wstring& originalNXLfilePath = L"",bool deletesource = false, uint32_t exitmode=0, const std::wstring &labelValue = L"");
	bool EditCopyFile(const std::wstring &filepath, std::wstring& destpath);

public:	// Log section
	void AddPrintLog(const std::wstring& nxlPath, bool bAllow = true);  // for office only got Print Alow log
	void AddViewLog(const std::wstring& nxlPath, bool bAllow = true);
	void AddCopyLog(const std::wstring& nxlPath, bool bAllow = true);
public: // send notification
	bool Popup_Message(const std::wstring& message, const std::wstring &nxlpath);
	bool Notify_Message(const std::wstring& message);
	// unitfied Notifiying Message
	bool Notify_PrintDenyMessage(const std::wstring &nxlpath);
	bool Notify_PrinterNotSupportedMessage(const std::wstring &nxlpath);
	bool Notify_PrinterNotSupportedMultiJobs(const std::wstring &nxlpath);

protected:
	bool NotifyMessage(const std::wstring &app, const std::wstring &target, const std::wstring &message, uint32_t msgtype = 1, uint32_t result = 0);
public:	// Overlay section
	bool SetupViewOverlay(const std::wstring& nxl_path, HWND target_window, const RECT& display_offset = { 10,70,30,40 });
	void ClearViewOverlay(const std::wstring& nxl_path, HWND target_window);

	bool AttachPrintOverlay(const std::wstring& nxl_path, HDC printDC);

public:
	bool CovnerFrom_Wrapper(const std::wstring& srcplainfile, 
							const std::wstring& originalnxlfile,
							std::wstring& outGeneratedFile);

protected:
	//bool GetNxlFileRightsByRMUser(const std::wstring& nxlfilepath, VECTOR_RIGHTS &rightsAndWatermarks);
	bool GetNxlFileRightsByRMInstance(const std::wstring& plainFilePath, bool checkOwner,
										std::vector<SDRmFileRight> &rights,
										SDR_WATERMARK_INFO &watermark,
										SDR_Expiration &expiration);
	bool HaveRight(const VECTOR_RIGHTS& vecRightAndWaterMark, SDRmFileRight right);
	DWORD SumRights(const VECTOR_RIGHTS& vecRights);


private:
	ISDRmcInstance* m_pRmcInstance;
	ISDRmTenant *m_pRmTenant;
	ISDRmUser *m_pRmUser;
	std::string m_magic_code;
};

