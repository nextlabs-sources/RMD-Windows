#include "stdafx.h"
#include "SkyDrmSDKMgr.h"
#include "log.h"
#include "rightsdef.h"
#include "CommonFunction.h"
#include <memory>
#include <experimental/filesystem>

using namespace std;

class OverlayText {
public:
	OverlayText() { _default(); }
	OverlayText(const std::wstring& text) :_text(text) { _default(); }

	inline void InsertReplacableToken(const std::wstring& key, const std::wstring& value) {
		_tokens[key] = value;
	}

	std::wstring GetReplacedText() {
		std::wstring rt = _text;
		const auto flag = std::regex::flag_type::ECMAScript | std::regex::flag_type::icase | std::regex::flag_type::nosubs;
		for (auto& p : _tokens) {
			wregex reg(_get_escaped_key(p.first), flag);
			rt = regex_replace(rt, reg, p.second);
		}

		
		return rt;
	}

private:
	void _default() {
		const static wchar_t* USER = L"$(User)";
		const static wchar_t* EMAIL = L"$(Email)";
		const static wchar_t* HOST = L"$(Host)";
		const static wchar_t* IP = L"$(IP)";
		const static wchar_t* BREAK = L"$(Break)";  //  to \n
		const static wchar_t* DATE = L"$(DATE)";    //  "YYYY-MM-DD"
		const static wchar_t* TIME = L"$(TIME)";	//  "HH:mm:ss"	  

		// TBD
		//_tokens[USER] = L"";
		//_tokens[EMAIL] = L"";
		//_tokens[HOST] = L"";
		//_tokens[IP] = L"";
		// Cur 
		_tokens[BREAK] = L"\n";
		_tokens[DATE] = _get_date()+L" ";
		_tokens[TIME] = _get_time();
	}

	inline std::wstring _get_escaped_key(const std::wstring& key) {
		wstring rt;
		for (auto i : key) {
			if (i == '$' || i == '(' || i == ')') {
				rt.push_back('\\');
			}
			rt.push_back(i);
		}
		return rt;
	}

	inline std::wstring _get_date() {
		std::wstringstream ss;
		auto time = std::time(nullptr);
		ss << std::put_time(std::localtime(&time), L"%Y-%m-%d");
		return ss.str();
	}

	inline std::wstring _get_time() {
		std::wstringstream ss;
		auto time = std::time(nullptr);
		ss << std::put_time(std::localtime(&time), L"%H:%M:%S");
		return ss.str();
	}
	inline std::wstring _get_datetime() {
		std::wstringstream ss;
		auto time = std::time(nullptr);
		ss << std::put_time(std::localtime(&time), L"%Y-%m-%d %H:%M:%S");
		return ss.str();
	}

private:
	std::wstring _text;
	// i.e.  $(User) ->  Osmond.Ye
	std::map<std::wstring, std::wstring> _tokens;
};



SkyDrmSDKMgr::SkyDrmSDKMgr()
{
	m_pRmcInstance = NULL;
	m_pRmTenant = NULL;
	m_pRmUser = NULL;
	m_magic_code = "{6829b159-b9bb-42fc-af19-4a6af3c9fcf6}";
	
}

SkyDrmSDKMgr::~SkyDrmSDKMgr()
{
}


bool SkyDrmSDKMgr::GetCurrentLoggedInUser()
{
	ISDRmcInstance *pInstance = NULL;
	ISDRmTenant *pTenant = NULL;
	ISDRmUser *puser = NULL;
	std::string strPasscode =  GetRMXPasscode();
	SDWLResult res = RPMGetCurrentLoggedInUser(strPasscode, pInstance, pTenant, puser);

	if (res.GetCode()==0){
		m_pRmcInstance = pInstance;
		m_pRmTenant = pTenant;
		m_pRmUser = puser;
	}

	//log
	theLog.WriteLog(0, NULL, 0, L"RPM RPMGetCurrentLoggedInUser res:%d, pInstance:%p, pTenant:%p, puser:%p\r\n",
		res.GetCode(), pInstance, pTenant, puser);

	return res.GetCode() == 0;
}

HRESULT SkyDrmSDKMgr::CheckRights(const WCHAR * wszFullFileName, ULONGLONG & RightMask)
{
	// sanity check
	if (wszFullFileName == NULL)
	{
		RightMask = BUILTIN_RIGHT_ALL;
		return S_FALSE;
	}
	// plain file always get all rights
	if (!CommonFunction::IsNXLFile(wszFullFileName))
	{

#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
		uint32_t dirstatus;
		std::wstring filetags;
		std::wstring wstrPath = wszFullFileName;
		std::wstring directory = L"";
		directory = wstrPath.substr(0, wstrPath.find_last_of(L"\\/"));
		if (CommonFunction::IsSanctuaryFolder(directory, &dirstatus, filetags)) {
			RightMask = BUILTIN_RIGHT_VIEW | BUILTIN_RIGHT_PRINT | BUILTIN_RIGHT_EDIT;
			return S_OK;
		}
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR

		RightMask = BUILTIN_RIGHT_ALL;
		return S_OK;
	}

	//call sdk to get file right
	std::wstring wstrFile = wszFullFileName;
	VECTOR_RIGHTS rightsAndWatermarks;
	SDR_WATERMARK_INFO raw_watermark;
	SDR_Expiration expiration;
	bool bGetRights = this->GetNxlFileRightsByRMInstance(wszFullFileName, true, rightsAndWatermarks, raw_watermark, expiration);

	//convert to right mask
	RightMask = BUILTIN_RIGHT_VIEW;
	if (bGetRights)
	{
		bool expired = false;
		uint64_t nowTime = (uint64_t)(((uint64_t)std::time(nullptr)) * 1000);
		if (expiration.type != NEVEREXPIRE) {
			if (nowTime > expiration.end) {
				expired = true;
			}
		}

		if (!expired) {
			if (HaveRight(rightsAndWatermarks, RIGHT_EDIT)) {
				RightMask |= BUILTIN_RIGHT_EDIT;
			}

			if (HaveRight(rightsAndWatermarks, RIGHT_PRINT)) {
				RightMask |= BUILTIN_RIGHT_PRINT;
			}
		}

		// by osmond 11/28/2019, NXL file will always ban SavaAS_feature
		//if (HaveRight(rightsAndWatermarks, RIGHT_DOWNLOAD)) {
		//	RightMask |= BUILTIN_RIGHT_SAVEAS;
		//}

		// by osmond 11/28/2019, Nxl_Right_Decrypt means you can extract original plain file, so that means 
		// if one file got RIGHT_DECRYPT, you can freely use Clipboard
		if (HaveRight(rightsAndWatermarks, RIGHT_DECRYPT)) {
			RightMask |= BUILTIN_RIGHT_CLIPBOARD;
			RightMask |= BUILTIN_RIGHT_SCREENCAP;
		}
	}

	return S_OK;
}

//bool SkyDrmSDKMgr::GetNxlFileRightsByRMUser(const std::wstring& nxlfilepath, VECTOR_RIGHTS &rightsAndWatermarks)
//{
//	if (m_pRmUser==NULL){
//		return false;
//	}
//
//	SDWLResult res = m_pRmUser->GetRights(nxlfilepath, rightsAndWatermarks);
//
//	//log
//	theLog.WriteLog(0, NULL, 0, L"GetNxlFileRightsByRMUser for:%s, res:%d, righs count:%d, sum rights:%d\r\n", 
//		nxlfilepath.c_str(), res.GetCode(), rightsAndWatermarks.size(), this->SumRights(rightsAndWatermarks));
//
//	return res.GetCode() == 0;
//}

bool SkyDrmSDKMgr::GetNxlFileRightsByRMInstance(const std::wstring& plainFilePath, bool checkOwner,
	std::vector<SDRmFileRight> &rights,
	SDR_WATERMARK_INFO &watermark,
	SDR_Expiration &expiration)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}

	std::string _duid;
	std::vector<std::pair<SDRmFileRight, std::vector<SDR_WATERMARK_INFO>>> _userRightsAndWatermarks;
	//std::vector<SDRmFileRight> _rights;
	//SDR_WATERMARK_INFO _watermark;
	//SDR_Expiration _expiration;
	std::string _tags;
	std::string _tokengroup;
	std::string _creatorid;
	std::string _infoext;
	DWORD _attributes;
	DWORD _isRPMFolder;
	DWORD _isNxlFile;
	auto rt = m_pRmcInstance->RPMGetFileInfo(plainFilePath,
		_duid, _userRightsAndWatermarks, rights, watermark, expiration, _tags, _tokengroup, _creatorid, _infoext,
		_attributes, _isRPMFolder, _isNxlFile);

	//fix Bug 67326 - get rights failed for office central policy file 
		if (rights.empty()) {
			for each (std::pair <SDRmFileRight, std::vector<SDR_WATERMARK_INFO>> var in _userRightsAndWatermarks)
			{
				SDRmFileRight item = var.first;
				rights.push_back(SDRmFileRight(item));
			}
		}
	//end fix Bug 67326 - get rights failed for office central policy file 

		if (watermark.text.empty()) {
			auto iter = std::find_if(_userRightsAndWatermarks.cbegin(), _userRightsAndWatermarks.cend(), [](decltype(_userRightsAndWatermarks)::const_reference pair) {
				const std::vector<SDR_WATERMARK_INFO> & value = pair.second;
				if (value.empty()) {
					return false;
				}
				return !value[0].text.empty();
			});

			if (iter != _userRightsAndWatermarks.cend()) {
				watermark = iter->second[0];
			}
		}

	//log
	theLog.WriteLog(0, NULL, 0, L"GetNxlFileRightsByRMInstance for plainFilePath:%s, res:%d, righs count:%d, sum rights:%d\r\n",
		plainFilePath.c_str(), rt.GetCode(), rights.size(), this->SumRights(rights));

	return rt.GetCode() == 0;
}

bool SkyDrmSDKMgr::HaveRight(const VECTOR_RIGHTS& vecRightAndWaterMark, SDRmFileRight right)
{
	VECTOR_RIGHTS::const_iterator itRights = vecRightAndWaterMark.begin();
	while (itRights != vecRightAndWaterMark.end())
	{
		if (*itRights & right)
		{
			return true;
		}
		itRights++;
	}
	return false;
}

DWORD SkyDrmSDKMgr::SumRights(const VECTOR_RIGHTS& vecRights)
{
	DWORD dwRightsSum = 0;
	VECTOR_RIGHTS::const_iterator itRights = vecRights.begin();
	while (itRights != vecRights.end())
	{
		dwRightsSum += *itRights;
		itRights++;
	}
	return dwRightsSum;
}

bool SkyDrmSDKMgr::EditSaveFile(const std::wstring &filepath, const std::wstring& originalNXLfilePath /* = L"" */, 
	bool deletesource /* = false */, uint32_t exitmode/* =0 */, const std::wstring &labelValue)
{
	if (m_pRmcInstance==NULL)
	{
		return false;
	}

	// fix Bug 59206 - [office RMX]no edit activity log for the local office file 
	// In word will delete file and record edit activityLog failed, so send log before invoke RPMEditSaveFile
	if (exitmode == EXIT_MODE_EXIT_SAVE)
		m_pRmUser->AddActivityLog(filepath, RL_OEdit, RL_RAllowed);

	std::wstring labeltag = L"";
	if (!labelValue.empty())
	{
		labeltag = L"{\"Sensitivity\":[\"" + labelValue + L"\"]}";
	}
	//SDWLResult res = m_pRmcInstance->RPMEditSaveFile(filepath, originalNXLfilePath, deletesource, exitmode);
	SDWLResult res = m_pRmcInstance->RPMEditSaveFile(filepath, originalNXLfilePath, deletesource, exitmode, labeltag);
	
	// Send edit log
	/*if (res) {
		m_pRmUser->AddActivityLog(filepath, RL_OEdit, RL_RAllowed);
	}*/

	theLog.WriteLog(0, NULL, 0, L"RPMEditSaveFile path:%s, originPath:%s, deletesource:%d, exitedit:%d, res=%d\r\n",
		filepath.c_str(), originalNXLfilePath.c_str(), deletesource, exitmode, res.GetCode());


	return res.GetCode() == 0;
}

bool SkyDrmSDKMgr::EditCopyFile(const std::wstring &filepath, std::wstring& destpath)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}

	SDWLResult res = m_pRmcInstance->RPMEditCopyFile(filepath, destpath);

	theLog.WriteLog(0, NULL, 0, L"RPMEditCopyFile path:%s, destpath:%s, res=%d\r\n",
		filepath.c_str(),destpath.c_str(), res.GetCode());


	return res.GetCode() == 0;
}

bool SkyDrmSDKMgr::AddTrustedProcess(unsigned long processId, const std::string &security)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}

	SDWLResult  res = m_pRmcInstance->RPMAddTrustedProcess(processId, security);

	theLog.WriteLog(0, NULL, 0, L"RPMAddTrustedProcess processID:%d, res=%d\r\n",
		processId, res.GetCode() );

	return res.GetCode() == 0;
}

bool SkyDrmSDKMgr::RPMRegisterApp(const std::wstring& appPath, const std::string &security)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}
	SDWLResult res = m_pRmcInstance->RPMRegisterApp(appPath, security);
	theLog.WriteLog(0, NULL, 0, L"RPMRegisterApp app path:%s, res=%d\r\n", appPath.c_str(), res.GetCode());
	return res.GetCode() == 0;
}

bool SkyDrmSDKMgr::NotifyRMXStatus(bool running, const std::string &security)
{
	if (m_pRmcInstance==NULL)
	{
		return false;
	}

	SDWLResult  res = m_pRmcInstance->RPMNotifyRMXStatus(running, security);

	theLog.WriteLog(0, NULL, 0, L"RPMNotifyRMXStatus running:%d, res:%d\r\n",
		running , res.GetCode());

	return res.GetCode() == 0;
}

bool SkyDrmSDKMgr::WaitInstanceInitFinish()
{
	if (m_pRmcInstance==NULL)
	{
		return false;
	}

	const DWORD dwTryTime = 3;
	bool bFinished = false;
	DWORD dwWaitTime = 0;
	do 
	{
		dwWaitTime++;
		bFinished = false;
		SDWLResult res = m_pRmcInstance->IsInitFinished(bFinished);
		theLog.WriteLog(0, NULL, 0, L"IsInitFinished time:%d, res:%d, finished:%d\r\n", dwWaitTime, res.GetCode(), bFinished);

		if (!bFinished)
		{
			Sleep(1000);
		}
	} while ((!bFinished) && (dwWaitTime<dwTryTime) );

	return bFinished;
}

bool SkyDrmSDKMgr::IsRPMFolderFile(const std::wstring& wstrFilePath)
{
	//return true;  // for debug

	if (m_pRmcInstance == NULL)
	{
		return false;
	}
	unsigned int dirstatus = 0;
	bool filestatus = false;
	if (m_pRmcInstance->RPMGetFileStatus(wstrFilePath, &dirstatus, &filestatus).GetCode() != 0) {
		return false;
	}
	return filestatus;
}

bool SkyDrmSDKMgr::IsInRPMFolder(const std::wstring& wstrFilePath)
{
	//return true;  // for debug

	if (m_pRmcInstance == NULL)
	{
		return false;
	}
	unsigned int dirstatus = 0;
	bool filestatus = false;
	if (m_pRmcInstance->RPMGetFileStatus(wstrFilePath, &dirstatus, &filestatus).GetCode() != 0) {
		return false;
	}

	if (dirstatus & (RPM_SAFEDIRRELATION_SAFE_DIR | RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR))
		return true;

	return false;
}


#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
bool SkyDrmSDKMgr::IsSanctuaryFolder(const std::wstring & path, uint32_t * dirstatus, std::wstring & filetags)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}
	if (m_pRmcInstance->IsSanctuaryFolder(path, dirstatus, filetags) != 0) {
		return false;
	}
	if ((*dirstatus) & (RPM_SANCTUARYDIRRELATION_SANCTUARY_DIR | RPM_SANCTUARYDIRRELATION_DESCENDANT_OF_SANCTUARY_DIR)) {
		return true;
	}
	return false;
}
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR


void SkyDrmSDKMgr::DelegateDeleteFolder(const std::wstring& folder)
{
	if (m_pRmcInstance == NULL)
	{
		return ;
	}
	m_pRmcInstance->RPMDeleteFolder(folder);
}

void SkyDrmSDKMgr::DelegateDeleteFile(const std::wstring& path)
{
	if (m_pRmcInstance == NULL)
	{
		return;
	}
	m_pRmcInstance->RPMDeleteFile(path);
}

bool SkyDrmSDKMgr::DelegateCopyFile(const std::wstring& src, const std::wstring& dest, bool deletesrc)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}

	SDWLResult res = m_pRmcInstance->RPMCopyFile(src, dest, deletesrc);

	return (res.GetCode() == 0);
}


void SkyDrmSDKMgr::AddPrintLog(const std::wstring & nxlPath, bool bAllow)
{
	if (m_pRmUser == NULL) {
		return;
	}
	m_pRmUser->AddActivityLog(nxlPath, RL_OPrint, bAllow?RL_RAllowed: RL_RDenied);
}

void SkyDrmSDKMgr::AddViewLog(const std::wstring& nxlPath, bool bAllow)
{
	if (m_pRmUser == NULL) {
		return;
	}
	m_pRmUser->AddActivityLog(nxlPath, RL_OView, bAllow ? RL_RAllowed : RL_RDenied);
}

void SkyDrmSDKMgr::AddCopyLog(const std::wstring& nxlPath, bool bAllow)
{
	if (m_pRmUser == NULL) {
		return;
	}
	m_pRmUser->AddActivityLog(nxlPath, RL_OCopyContent, bAllow ? RL_RAllowed : RL_RDenied);
}

bool SkyDrmSDKMgr::Popup_Message(const std::wstring& message, const std::wstring &nxlpath)
{
	static const std::wstring app = nextlabs::utils::get_app_name();
	std::experimental::filesystem::path _filename(nxlpath);
	return NotifyMessage(app, _filename.filename(), message, 1, 1);
}

bool SkyDrmSDKMgr::Notify_Message(const std::wstring& message)
{
	static const std::wstring app = nextlabs::utils::get_app_name();
	return NotifyMessage(app, app, message, 1);
}

bool SkyDrmSDKMgr::NotifyMessage(const std::wstring &app, const std::wstring &target, const std::wstring &msg,
	uint32_t msgtype, uint32_t result)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}

	// if same message is sent in 1 second, we will not show it again
	static std::wstring lastmsg;
	static std::chrono::milliseconds lastms = std::chrono::duration_cast<std::chrono::milliseconds >(
		std::chrono::system_clock::now().time_since_epoch()
		);
	std::chrono::milliseconds ms = std::chrono::duration_cast<std::chrono::milliseconds >(
		std::chrono::system_clock::now().time_since_epoch()
		);
	long long gap = std::chrono::duration_cast<std::chrono::milliseconds>(ms - lastms).count();
	// using message to compare whether they are different from last one.
	// it is the inefficient way, shall compare message type
	if (lastmsg == msg && gap <= 3000)
		return true;

	lastms = ms;
	lastmsg = msg;

	SDWLResult res = m_pRmcInstance->RPMNotifyMessage(app, target, msg, msgtype, L"", result);

	return res.GetCode() == 0;
}

bool SkyDrmSDKMgr::Notify_PrintDenyMessage(const std::wstring &nxlpath)
{
	static const std::wstring app = nextlabs::utils::get_app_name();
	std::experimental::filesystem::path _filename(nxlpath);
	static const std::wstring msg = L"You are not allowed to perform print operation on the file. ";
	return NotifyMessage(app, _filename.filename(), msg, 1);
}

bool SkyDrmSDKMgr::Notify_PrinterNotSupportedMessage(const std::wstring &nxlpath)
{
	static const std::wstring msg = L"You can't use this printer. Please ask your administrator to add this printer to trusted-printer-list if you want to.";
	std::experimental::filesystem::path _filename(nxlpath);
	static const std::wstring app = nextlabs::utils::get_app_name();
	return NotifyMessage(app, _filename.filename(), msg, 1);
}

bool SkyDrmSDKMgr::Notify_PrinterNotSupportedMultiJobs(const std::wstring &nxlpath)
{
	static const std::wstring msg = L"There is already one printing job running. You cann't print this NextLabs protected file until it is finished.";
	std::experimental::filesystem::path _filename(nxlpath);
	static const std::wstring app = nextlabs::utils::get_app_name();
	return NotifyMessage(app, _filename.filename(), msg, 1);
}

bool SkyDrmSDKMgr::SetupViewOverlay(const std::wstring & nxl_path, HWND target_window, const RECT& display_offset)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}

	if (target_window == NULL) {
		return false;
	}

	if (!CommonFunction::IsNXLFile(nxl_path.c_str()))
	{
		return false;
	}

	VECTOR_RIGHTS r;
	SDR_WATERMARK_INFO raw_watermark;
	SDR_Expiration expiration;
	if (!this->GetNxlFileRightsByRMInstance(nxl_path,true, r, raw_watermark, expiration)) {
		return false;
	}

	//auto iter = std::find_if(r.cbegin(), r.cend(), [](decltype(r)::const_reference pair) {
	//	const std::vector<SDR_WATERMARK_INFO > & value = pair.second;

	//	if (value.empty()) {
	//		return false;
	//	}
	//	return !value[0].text.empty();
	//});

	//if (iter == r.cend()) {
	//	return false;
	//}
	//// by osmond 11/28/2019,  I guess watermark string is coded by UTF8, 
	//// the good thing is UTF8 is compatible with ANSI for English World
	//std::wstring watermark(ATL::CA2W((iter->second)[0].text.c_str(),CP_UTF8));

	std::wstring watermark(ATL::CA2W(raw_watermark.text.c_str(),CP_UTF8));
	if (watermark.empty()) {
		return false;
	}

	// config Repalce tokens
	OverlayText ot(watermark);

	ot.InsertReplacableToken(L"$(User)", this->m_pRmUser->GetEmail());
	ot.InsertReplacableToken(L"$(Email)", this->m_pRmUser->GetEmail());


	SDWLResult res = m_pRmcInstance->RPMSetViewOverlay(target_window, ot.GetReplacedText(),
		{ 70, 0, 128, 21 },L"Arial", 22, 45, 0, 0, 
		{ display_offset.left,display_offset.top,display_offset.right,display_offset.bottom }
	);
	return res.GetCode() == 0;

}

void SkyDrmSDKMgr::ClearViewOverlay(const std::wstring& nxl_path, HWND target_window)
{
	if (m_pRmcInstance == NULL)
	{
		return ;
	}

	if (!CommonFunction::IsNXLFile(nxl_path.c_str()))
	{
		return ;
	}

	m_pRmcInstance->RPMClearViewOverlay(target_window);
}

namespace PrintOverlay {
#include <gdiplus.h>



	struct WatermarkParam {
		std::wstring text;
		int text_rotation;  //i.e.  -10, -90, 10, 30, 45
		Gdiplus::StringAlignment text_alignment;
		Gdiplus::StringAlignment line_alignment;
		int font_size;
		// RGB[0,0,0] is black, RGB[255,255,255] is white
		Gdiplus::Color  font_color;
		Gdiplus::FontStyle font_style;
		std::wstring font_name;

		WatermarkParam() {

			text_rotation = 45;
			font_name = L"Arial";
			font_size = 12;
			font_color.SetValue(Gdiplus::Color::MakeARGB(70, 0, 128, 21));
			font_style = Gdiplus::FontStyle::FontStyleRegular;
			text_alignment = Gdiplus::StringAlignment::StringAlignmentNear;
			line_alignment = Gdiplus::StringAlignment::StringAlignmentNear;

		}
	};


	Gdiplus::SizeF CalcTextSizeF(
		const Gdiplus::Graphics& drawing_surface, 
		const std::wstring& szText, 
		const Gdiplus::StringFormat& strFormat, 
		const Gdiplus::Font& font)
	{
		Gdiplus::RectF rcBound;
		drawing_surface.MeasureString(szText.c_str(), -1, &font, Gdiplus::PointF(0, 0), &strFormat, &rcBound);
		return Gdiplus::SizeF(rcBound.Width, rcBound.Height);
	}

	Gdiplus::PointF CaculateRotated(Gdiplus::PointF& org, int angle)
	{
		static const double PI = std::acos(-1);
		Gdiplus::PointF rt;

		double radians = angle * PI / 180;

#pragma warning(push)
#pragma warning(disable: 4244)
		rt.X = org.X * std::cos(radians) - org.Y * std::sin(radians);
		rt.Y = org.X * std::sin(radians) + org.Y * std::cos(radians);
#pragma warning(pop)
		return rt;
	}

	Gdiplus::RectF CaculateOutbound(Gdiplus::PointF(&org)[4])
	{

		std::vector<Gdiplus::REAL> Xs, Ys;
		for (int i = 0; i < 4; i++) {
			Xs.push_back(org[i].X);
			Ys.push_back(org[i].Y);
		}

		std::sort(Xs.begin(), Xs.end());
		std::sort(Ys.begin(), Ys.end());

		Gdiplus::REAL width = Xs.back() - Xs.front();
		Gdiplus::REAL height = Ys.back() - Ys.front();

		return Gdiplus::RectF(Xs.front(), Ys.front(), width, height);
	}

	Gdiplus::RectF CalculateMinimumEnclosingRectAfterRotate(const Gdiplus::SizeF& size, int rotate)
	{
		Gdiplus::PointF org[4] = {
			{0,0},{0,size.Height},{size.Width, size.Height},  {size.Width, 0}
		};

		Gdiplus::PointF org_r[4];
		for (int i = 0; i < 4; i++) {
			org_r[i] = CaculateRotated(org[i], rotate);
		}

		return CaculateOutbound(org_r);
	}

	Gdiplus::Bitmap* DrawOverlayBitmap(const Gdiplus::Graphics& drawing_surface, const WatermarkParam& param)
	{

		Gdiplus::FontFamily fontfamily(param.font_name.c_str());
		Gdiplus::Font font(&fontfamily, (Gdiplus::REAL)param.font_size, param.font_style, Gdiplus::UnitPixel);
		Gdiplus::SolidBrush brush(param.font_color);
		Gdiplus::StringFormat str_format; {
			str_format.SetAlignment(param.text_alignment);
			str_format.SetLineAlignment(param.line_alignment);
		}
		Gdiplus::SizeF str_size = CalcTextSizeF(drawing_surface, param.text, str_format, font);
		Gdiplus::RectF str_enclosing_rect = CalculateMinimumEnclosingRectAfterRotate(str_size, param.text_rotation);

		Gdiplus::REAL surface_size = 2 * std::ceil(std::hypot(str_size.Width, str_size.Height));

		Gdiplus::Bitmap bitmap_canvas((INT)surface_size, (INT)surface_size, PixelFormat32bppARGB);
		Gdiplus::Graphics g(&bitmap_canvas);

		// set centre point as the base point
		g.TranslateTransform(surface_size / 2, surface_size / 2);
		g.RotateTransform((Gdiplus::REAL)param.text_rotation);
		// set string
		g.SetTextRenderingHint(Gdiplus::TextRenderingHintAntiAlias);
		g.DrawString(param.text.c_str(), -1, &font,
			Gdiplus::RectF(0, 0, str_size.Width, str_size.Height),
			&str_format, &brush);
		g.ResetTransform();
		g.Flush();

		// since drawing org_point is the centre, 
		Gdiplus::RectF absolute_layout = str_enclosing_rect;
		absolute_layout.Offset(surface_size / 2, surface_size / 2);

		// request bitmap is the partly clone with absolute_layout
		return bitmap_canvas.Clone(absolute_layout, PixelFormat32bppARGB);
	}

	void draw_overlay(HDC hdc, const WatermarkParam& param)
	{
		using namespace Gdiplus;
		if (hdc == NULL) return;
		if (param.text.empty()) return;

		Gdiplus::Graphics g(hdc);
		Gdiplus::Bitmap* bm_overlay = DrawOverlayBitmap(g, param);
		if (bm_overlay == NULL) {
			DEVLOG(L"bm_overlay is NULL in draw_overlay");
			return;
		}

		g.SetPageUnit(Gdiplus::UnitPoint);
		Gdiplus::TextureBrush brush(bm_overlay, Gdiplus::WrapModeTile);
		int dwdeviceWidth = ::GetDeviceCaps(hdc, PHYSICALWIDTH);
		int dwdeviceHigh = ::GetDeviceCaps(hdc, PHYSICALHEIGHT);
		Gdiplus::RectF canvas(0, 0, (REAL)dwdeviceWidth, (REAL)dwdeviceHigh);

		g.FillRectangle(&brush, canvas);
		delete bm_overlay;
	}
}


bool SkyDrmSDKMgr::AttachPrintOverlay(const std::wstring& nxl_path, HDC printDC)
{
	if (m_pRmcInstance == NULL)
	{
		return false;
	}

	if (!CommonFunction::IsNXLFile(nxl_path.c_str()))
	{
		return false;
	}

	VECTOR_RIGHTS r;
	SDR_WATERMARK_INFO raw_watermark;
	SDR_Expiration expiration;
	if (!this->GetNxlFileRightsByRMInstance(nxl_path,true, r, raw_watermark, expiration)) {
		return false;
	}

	//auto iter = std::find_if(r.cbegin(), r.cend(), [](decltype(r)::const_reference pair) {
	//	const std::vector<SDR_WATERMARK_INFO >& value = pair.second;

	//	if (value.empty()) {
	//		return false;
	//	}
	//	return !value[0].text.empty();
	//	});

	//if (iter == r.cend()) {
	//	return false;
	//}
	//// by osmond 11/28/2019,  I guess watermark string is coded by UTF8, 
	//// the good thing is UTF8 is compatible with ANSI for English World
	//std::wstring watermark(ATL::CA2W((iter->second)[0].text.c_str(), CP_UTF8));

	std::wstring watermark(ATL::CA2W(raw_watermark.text.c_str(), CP_UTF8));
	if (watermark.empty()) {
		return false;
	}

	// config Repalce tokens
	OverlayText ot(watermark);

	ot.InsertReplacableToken(L"$(User)", this->m_pRmUser->GetEmail());
	ot.InsertReplacableToken(L"$(Email)", this->m_pRmUser->GetEmail());


	PrintOverlay::WatermarkParam param;
	param.text= ot.GetReplacedText();

	PrintOverlay::draw_overlay(printDC, param);

	return true;
}

bool SkyDrmSDKMgr::CovnerFrom_Wrapper(const std::wstring& srcplainfile, 
	const std::wstring& originalnxlfile,
	std::wstring& outGeneratedFile)
{
	if (m_pRmUser == NULL) {
		return false;
	}
	if (!::PathFileExistsW(srcplainfile.c_str())) {
		return false;
	}

	std::unique_ptr<wchar_t[]> folder(new wchar_t[srcplainfile.size()+1]);
	std::copy(srcplainfile.begin(), srcplainfile.end(), folder.get());
	folder[srcplainfile.size()] = '\0';
	::PathRemoveFileSpecW(folder.get());
	std::wstring out = folder.get();

	if (m_pRmUser->ProtectFileFrom(srcplainfile, originalnxlfile, out).GetCode() != 0) {
		return false;
	}
	outGeneratedFile = out;
	return true;
}
