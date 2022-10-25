// view_wateramrk.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "view_watermark.h"
#include "misc.h"



// quick log
#define LOG(x) ::OutputDebugStringA(x);
#define LOG_FUNC ::OutputDebugStringW(__FUNCTIONW__ L"\n");


void log_printf(const char* format, ...) {
	char buf[0x200] = { 0 };

	va_list ap;
	va_start(ap, format);
	vsprintf_s(buf, format, ap);
	va_end(ap);

	LOG(buf);
}


using namespace std;
using namespace ATL;


typedef CWinTraits<	WS_POPUP | WS_VISIBLE | WS_DISABLED,
					//WS_EX_TOPMOST | WS_EX_LAYERED |	WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT     // topmost will result print output to screen
					WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE
		>OverlayWindowTraits;

/*
with WS_EX_LAYERED
with WS_EX_TOOLWINDOW, floating and convering the target windows,

Notice:
	- using controller to change this wnd's size and make it friendly convered with target wnd
	- changing target_wnd , changing this object simultaneously

*/
class OverlayWindow : public CWindowImpl<OverlayWindow, CWindow, OverlayWindowTraits>
{
	friend class ViewOverlyController;
public:
	OverlayWindow() :OldTarget(-1, -1, -1, -1) {}
	~OverlayWindow() {
		if (IsWindow()) {
			this->ShowWindow(SW_HIDE);
			this->DestroyWindow();
		}
	}
private:
	inline void SetOverlay(const OverlayConfig& config) { _config = config; }
	void Init(const OverlayConfig& ol, HWND target);
	void UpdateOverlaySizePosStatus(HWND target);
	inline void HideWnd() {
		if (IsWindow()) {
			this->ShowWindow(SW_HIDE);
		}
	}
	void _DrawOverlay(HDC dc, LPRECT lpRect);
	void _PrepareOverly();

private:
	Gdiplus::Bitmap* _GetOverlayBitmap(const Gdiplus::Graphics& drawing_surface);

public: // section for wnd_registration and msg_handler
	DECLARE_WND_CLASS_EX(L"NextlabsOverlay", 0, (HBRUSH)(COLOR_WINDOW + 1));
	// don't need handle msg, using win_default_msg_handler is enough
	BEGIN_MSG_MAP_EX(OverlayWindow)
		MSG_WM_WINDOWPOSCHANGED(OnPosChanged)
		END_MSG_MAP()

	LRESULT OnPosChanged(WINDOWPOS* lpwndpos);
private:
	OverlayConfig _config;
	CMemoryDC* pmdc;
	CRect OldTarget;
};


class ViewOverlyController {
	HHOOK _swhHook; // standard by ::SetWindowsHookEx
	std::map<HWND, std::shared_ptr<OverlayWindow> > _wnds; // targetDocWnd -> OverlayWnd
	typedef struct _queue_task {
		HWND target;
		OverlayConfig config_param;
	}queue_task;
	std::queue<queue_task> delayed_task;
public:
	static ViewOverlyController& getInstance();
private:
	ViewOverlyController();
	~ViewOverlyController();
public:
	void Attach(HWND target, const OverlayConfig& config);
	void AttachFromOtherThread(HWND target, const OverlayConfig& config);
	void Detach(HWND target);
	void Clear();	// clear all watermarks
	void SetOverlyTarget(HWND target);
	void UpdateWatermark(HWND target);
private:
	LRESULT OnMessageHook(int code, WPARAM wParam, LPARAM lParam);
	void OnHandleDelayedQueue();
	void install_windows_hook_to_main_ui_thread(DWORD main_ui_tid);
private:
	static ViewOverlyController* sgIns;
	static std::recursive_mutex sgRMutex; // this class mutex
	static LRESULT CALLBACK HookProxy(int code, WPARAM wParam, LPARAM lParam);
};


LRESULT OverlayWindow::OnPosChanged(WINDOWPOS* lpwndpos)
{
	// Get WaterMark Window Parent
	HWND hparent = ::GetParent(m_hWnd);
	if (hparent)
	{
		// am I ahead of my parent window
		HWND hwndnext = m_hWnd;
		BOOL bfoundparent = false;

		for (int i = 0; i < 8; i++) // try to find parent windows in next 8 same level window
		{
			hwndnext = ::GetWindow(hwndnext, GW_HWNDNEXT);

			std::wstring strMsg = L"*** OnPosChanged::GetWindow ===\n hwnd :" + std::to_wstring((uint64_t)m_hWnd)
				+ L" hwndnext : " + std::to_wstring((uint64_t)hwndnext)
				+ L" hparent : " + std::to_wstring((uint64_t)hparent)
				+ L" ***\n";

			::OutputDebugStringW(strMsg.c_str());

			if (hwndnext == hparent)
			{
				bfoundparent = true;
				break;
			}

			if (::IsWindow(hwndnext) == false)
				break;
		}

		if (bfoundparent == false)
		{
			// watermark window is not before target/parent window, reset it
			BOOL bRet = ::SetWindowPos(hparent, m_hWnd, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE);

			std::wstring strMsg = L"*** OnPosChanged::SetWindowPos ";
			if (!bRet)
			{
				DWORD dwErr = ::GetLastError();
				strMsg += L" , error : " + std::to_wstring(dwErr);
			}
			strMsg += L" bRet : " + std::to_wstring(bRet)
				+ L" hwnd : " + std::to_wstring((uint64_t)m_hWnd)
				+ L" hwndnext : " + std::to_wstring((uint64_t)hwndnext)
				+ L" hparent : " + std::to_wstring((uint64_t)hparent)
				+ L" ***\n";
			::OutputDebugStringW(strMsg.c_str());
		}
	}

	return 0;
}

void OverlayWindow::Init(const OverlayConfig& ol, HWND target) {
	SetOverlay(ol);
	Create(target);
	_PrepareOverly();
}

void OverlayWindow::UpdateOverlaySizePosStatus(HWND target)
{
	CRect targetRC;
	if (target == NULL) {
		// user physical device may changed ,you get it each tiem
		targetRC = { 0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN) };
	}
	else {
		::GetWindowRect(target, &targetRC);
	}

	if (OldTarget.EqualRect(targetRC)) {
		//OutputDebugStringA("same CRect, return directly");
		return;
	}
	else {
		OldTarget = targetRC;
	}

	targetRC.DeflateRect(_config.GetDisplayOffset());
	// make layered wnd always covered the targert Wnd
	MoveWindow(targetRC, false);

	BLENDFUNCTION blend = { AC_SRC_OVER ,0,0xFF,AC_SRC_ALPHA };
	//CPoint p(targetRC.left, targetRC.right);
	CPoint p(0, 0);
	CPoint dstpt(targetRC.left, targetRC.top);
	CSize s(targetRC.Width(), targetRC.Height());

	// draw in Screen, but always get target wnd's region info
	if (!::UpdateLayeredWindow(this->m_hWnd,
		NULL,
		&dstpt, &s,
		*pmdc, &p,   // src dc and {left,top}
		NULL, &blend, ULW_ALPHA)  // using alpha blend,
		) {
		// error occured
		auto err = ::GetLastError();
		std::string strErr = "Faied call UpdateLayeredWindow,error is ";
		strErr += std::to_string(err);
		::OutputDebugStringA(strErr.c_str());
	}
}



Gdiplus::Bitmap* OverlayWindow::_GetOverlayBitmap(const Gdiplus::Graphics& drawing_surface)
{
	CString overlay_str(_config.GetString().c_str());
	Gdiplus::FontFamily fontfamily(_config.GetFontName().c_str());
	Gdiplus::Font font(&fontfamily, (Gdiplus::REAL)_config.GetFontSize(), _config.GetGdiFontStyle(), Gdiplus::UnitPixel);
	Gdiplus::SolidBrush brush(_config.GetFontColor());
	Gdiplus::StringFormat str_format;
	str_format.SetAlignment(_config.GetGdiTextAlignment());
	str_format.SetLineAlignment(_config.GetGdiLineAlignment());
	Gdiplus::SizeF str_size = CalcTextSizeF(drawing_surface, overlay_str, str_format, font);
	Gdiplus::RectF str_enclosing_rect = gdi::CalculateMinimumEnclosingRectAfterRotate(str_size, _config.GetFontRotation());

	Gdiplus::REAL surface_size = 2 * std::ceil(std::hypot(str_size.Width, str_size.Height));

	Gdiplus::Bitmap surface((INT)surface_size, (INT)surface_size, PixelFormat32bppARGB);
	Gdiplus::Graphics g(&surface);
	// make a good quality
	g.SetSmoothingMode(Gdiplus::SmoothingModeHighQuality);
	g.SetTextRenderingHint(Gdiplus::TextRenderingHintAntiAlias);
	// set centre point as the base point
	g.TranslateTransform(surface_size / 2, surface_size / 2);
	g.RotateTransform((Gdiplus::REAL)_config.GetFontRotation());
	// set string
	g.DrawString(overlay_str.GetString(), -1, &font,
		Gdiplus::RectF(0, 0, str_size.Width, str_size.Height),
		&str_format, &brush);
	g.ResetTransform();
	g.Flush();

	// since drawing org_point is the centre, 
	Gdiplus::RectF absolute_layout = str_enclosing_rect;
	absolute_layout.Offset(surface_size / 2, surface_size / 2);


	// request bitmap is the partly clone with absolute_layout
	return surface.Clone(absolute_layout, PixelFormat32bppARGB);
}


void OverlayWindow::_DrawOverlay(HDC dcScreen, LPRECT lpRestrictDrawingRect)
{
	if (dcScreen == NULL) {
		return;
	}
	CRect rc(lpRestrictDrawingRect);
	Gdiplus::Graphics g(dcScreen);
	// make a good quality
	g.SetSmoothingMode(Gdiplus::SmoothingModeHighQuality);
	g.SetTextRenderingHint(Gdiplus::TextRenderingHintAntiAlias);
	// beging drawing
	Gdiplus::Bitmap* bk = _GetOverlayBitmap(g);
	Gdiplus::TextureBrush brush(bk, Gdiplus::WrapModeTile);
	Gdiplus::RectF surface(0, 0, (Gdiplus::REAL)rc.Width(), (Gdiplus::REAL)rc.Height());
	g.FillRectangle(&brush, surface);
	delete bk;
}

void OverlayWindow::_PrepareOverly()
{
	// Get Whole Screen pixels
	CRect ScreenRC = {
		GetSystemMetrics(SM_XVIRTUALSCREEN),
		GetSystemMetrics(SM_YVIRTUALSCREEN),
		GetSystemMetrics(SM_CXVIRTUALSCREEN) + 100,
		GetSystemMetrics(SM_CYVIRTUALSCREEN) + 100 };

	// Get a large surface to draw overlay
	CDC dc = ::GetDC(NULL);
	pmdc = new CMemoryDC(dc, ScreenRC);
	_DrawOverlay(*pmdc, ScreenRC);
}




//
//  classController
//
//

ViewOverlyController* ViewOverlyController::sgIns = NULL;
std::recursive_mutex ViewOverlyController::sgRMutex;
#ifdef ViewOverlyControllerScopeGurad
#error this is Impossible
#endif // ViewOverlyControllerScopeGurad
#define ViewOverlyControllerScopeGurad std::lock_guard<std::recursive_mutex> g(sgRMutex)

ULONG_PTR gGidplusToken;
Gdiplus::GdiplusStartupInput gGdipulsInput;


DWORD gFirstThreadId = -1;


MsgAntiReenter g_mar;


ViewOverlyController& ViewOverlyController::getInstance()
{
	ViewOverlyControllerScopeGurad;
	if (sgIns == NULL) {
		{
			LOG_FUNC;
			log_printf("calling_thread_id=0x%x\n", ::GetCurrentThreadId());
		}
		// init gdi++
		Gdiplus::GdiplusStartup(&gGidplusToken, &gGdipulsInput, NULL);
		// we think the first created tid is the main_ui_id;
		//gFirstThreadId = ::GetMainThreadID();  // team centre is not in this case
		gFirstThreadId = ::GetCurrentThreadId();
		{
			log_printf("In ViewOverlyController::getInstance, calling_thread=0x%x, first_thread_id=0x%x\n",
				::GetCurrentThreadId(), gFirstThreadId);
		}
		sgIns = new ViewOverlyController();
		sgIns->install_windows_hook_to_main_ui_thread(gFirstThreadId);
	}
	return *sgIns;
}

ViewOverlyController::ViewOverlyController() : _swhHook(NULL) {

}

ViewOverlyController::~ViewOverlyController()
{
	if (_swhHook != NULL) {
		::UnhookWindowsHookEx(_swhHook);
	}
	// deinit gdi++
	if (gGidplusToken != NULL) {
		Gdiplus::GdiplusShutdown(gGidplusToken);
		gGidplusToken = NULL;
	}

}

void ViewOverlyController::Attach(HWND target, const OverlayConfig& config)
{
	ViewOverlyControllerScopeGurad;
	
	if (_wnds.find(target) != _wnds.end()) {// exist
		if (_wnds[target]->_config != config) {
			_wnds[target]->SetOverlay(config);
		}
	}
	else {
		std::shared_ptr<OverlayWindow> spWnd(new OverlayWindow());
		spWnd->Init(config, target);
		_wnds[target] = spWnd;
		SetOverlyTarget(target);
	}
}

void ViewOverlyController::AttachFromOtherThread(HWND target, const OverlayConfig& config)
{
	ViewOverlyControllerScopeGurad;
	delayed_task.push({ target,config });
}

void ViewOverlyController::Detach(HWND target)
{
	ViewOverlyControllerScopeGurad;

	if (_wnds.find(target) != _wnds.end()) {
		_wnds[target]->HideWnd();
		_wnds.erase(target);
	}
}

void ViewOverlyController::Clear()
{
	ViewOverlyControllerScopeGurad;
	_wnds.clear();
}

void ViewOverlyController::SetOverlyTarget(HWND target)
{
	// must determine if is the same thread for 
	//		current calling  V.S.  target's owner thread
	ViewOverlyControllerScopeGurad;
	
	if (!::IsWindow(target)) {
		// target wnd is not the valid window
		return;
	}

	DWORD tid = ::GetWindowThreadProcessId(target, NULL);

	if (tid != gFirstThreadId) {
		return;   // only supprot attach watermark into main_ui_thread
	}

	

	if (_wnds.find(target) != _wnds.end()) {
		_wnds[target]->UpdateOverlaySizePosStatus(target);
	}
}

void ViewOverlyController::UpdateWatermark(HWND target) {
	ViewOverlyControllerScopeGurad;
	if (_wnds.find(target) != _wnds.end()) {
		_wnds[target]->UpdateOverlaySizePosStatus(target);
	}
}

void ViewOverlyController::OnHandleDelayedQueue()
{
	ViewOverlyControllerScopeGurad;
	while (!delayed_task.empty()) {
		auto t = delayed_task.front();
		Attach(t.target, t.config_param);
		delayed_task.pop();
	}
}

void ViewOverlyController::install_windows_hook_to_main_ui_thread(DWORD main_ui_tid)
{
	// current,we only support on window_hook
	if (_swhHook == NULL) {
		// call this function on UI thread
		_swhHook = ::SetWindowsHookEx(WH_CALLWNDPROCRET,	// after wnd had processed the message
			ViewOverlyController::HookProxy,
			NULL,
			main_ui_tid
		);

		if (_swhHook == NULL) {
			throw new std::exception("failed, call SetWindowsHookEx");
		}

		{
			log_printf("success to call SetWindowsHookEx\n");
		}
	}
}


LRESULT ViewOverlyController::OnMessageHook(int code, WPARAM wParam, LPARAM lParam)
{
	if (code < 0 || lParam == 0) {
		return ::CallNextHookEx(_swhHook, code, wParam, lParam);
	}

	// extra code, to avoid meaningless reentrance
	if (g_mar.is_thread_disabled()) {
		return ::CallNextHookEx(_swhHook, code, wParam, lParam);
	}
	MsgAntiRenter_Control auto_control(g_mar);

	// handle delayed queue
	OnHandleDelayedQueue();

	CWPRETSTRUCT* p = (CWPRETSTRUCT*)lParam;
	// may be main window moving
	UINT msg = p->message;
	HWND t = p->hwnd;
	ViewOverlyControllerScopeGurad;
	if (_wnds.empty() || _wnds.find(t) == _wnds.end()) {
		return ::CallNextHookEx(_swhHook, code, wParam, lParam);
	}

	switch (msg)
	{
	case WM_MOVE:
	case WM_MOVING:
	case WM_WINDOWPOSCHANGING:
	case WM_WINDOWPOSCHANGED:
	case WM_SHOWWINDOW:
		//case WM_SIZE:
		//case WM_SIZING:
	case WM_SYSCOMMAND:
	{
		_wnds[t]->UpdateOverlaySizePosStatus(t);
		break;
	}
	case WM_DESTROY:
	{
		// target wnd wants destory tell to destory overlay wnd
		_wnds[t]->HideWnd();
		_wnds.erase(t);
		break;
	}
	default:
		break;
	}

	return ::CallNextHookEx(_swhHook, code, wParam, lParam);
}



LRESULT ViewOverlyController::HookProxy(int code, WPARAM wParam, LPARAM lParam)
{
	return getInstance().OnMessageHook(code, wParam, lParam);
}



bool RPMInitViewOverlayInstance()
{
	LOG_FUNC;
	log_printf("calling_thread_id=0x%x\n", ::GetCurrentThreadId());
	ViewOverlyController::getInstance();
	return true;
}

bool RPMSetViewOverlay(void* target_window, const std::wstring& overlay_text, const std::tuple<unsigned char, unsigned char, unsigned char, unsigned char>& font_color, const std::wstring& font_name, int font_size, int font_rotation, int font_sytle, int text_alignment, const std::tuple<int, int, int, int>& display_offset)
{
	if (overlay_text.empty()) {
		return false;
	}
	// force get GDI+ init here for first use, in case of user env not do this job
	auto& ins = ViewOverlyController::getInstance();

	// prepare params
	OverlayConfig::FontStyle fs;
	switch (font_sytle)
	{
	case 0:
		fs = OverlayConfig::FontStyle::FS_Regular;
		break;
	case 1:
		fs = OverlayConfig::FontStyle::FS_Bold;
		break;
	case 2:
		fs = OverlayConfig::FontStyle::FS_Italic;
		break;
	case 3:
		fs = OverlayConfig::FontStyle::FS_BoldItalic;
		break;
	default:
		fs = OverlayConfig::FontStyle::FS_Regular;
		break;
	}
	OverlayConfig::TextAlignment	alignment;
	switch (text_alignment)
	{
	case 0:
		alignment = OverlayConfig::TextAlignment::TA_Left;
		break;
	case 1:
		alignment = OverlayConfig::TextAlignment::TA_Centre;
		break;
	case 2:
		alignment = OverlayConfig::TextAlignment::TA_Right;
		break;
	default:
		alignment = OverlayConfig::TextAlignment::TA_Centre;
		break;
	}

	OverlayConfigBuilder builder;
	builder
		.SetString(overlay_text)
		.SetFontColor(std::get<0>(font_color), std::get<1>(font_color), std::get<2>(font_color), std::get<3>(font_color))
		.SetFontName(font_name)
		.SetFontSize(font_size)
		.SetFontRotation(font_rotation)
		.SetFontStyle(fs)
		.SetLineAlignment(alignment)
		.SetTextAlignment(alignment)
		.SetDisplayOffset({ std::get<0>(display_offset), std::get<1>(display_offset), std::get<2>(display_offset), std::get<3>(display_offset) });
	;

	ins.Attach((HWND)target_window, builder.Build());

	return true;
}

bool RPMSetViewOverlay_Background(void* target_window, const std::wstring& overlay_text, const std::tuple<unsigned char, unsigned char, unsigned char, unsigned char>& font_color, const std::wstring& font_name, int font_size, int font_rotation, int font_sytle, int text_alignment, const std::tuple<int, int, int, int>& display_offset)
{
	LOG_FUNC;
	// force get GDI+ init here for first use, in case of user env not do this job
	auto& ins = ViewOverlyController::getInstance();

	int main_ui_tid = gFirstThreadId;

	{
		log_printf("in RPMSetViewOverlay_Background, main_ui_tid=0x%x", main_ui_tid);
	}

	if (!::IsWindow((HWND)target_window)) {
		return false;
	}
	if (overlay_text.empty()) {
		return false;
	}

	int wnd_tid = ::GetWindowThreadProcessId((HWND)target_window, NULL);

	{
		log_printf("in RPMSetViewOverlay_Background, target_window_tid=0x%x, target_window_handle=0x%x", wnd_tid,target_window);
	}

	if (main_ui_tid != wnd_tid) {
		return false; // only main_thread will be supported
	}
	int cur_tid = ::GetCurrentThreadId();

	{
		log_printf("in RPMSetViewOverlay_Background, cur_tid=0x%x\n", cur_tid);
	}




	if (cur_tid == main_ui_tid) {
		return RPMSetViewOverlay(target_window, overlay_text, font_color, font_name, font_size, font_rotation, font_sytle, text_alignment, display_offset);
	}
	else {
		// prepare params
		OverlayConfig::FontStyle fs;
		switch (font_sytle)
		{
		case 0:
			fs = OverlayConfig::FontStyle::FS_Regular;
			break;
		case 1:
			fs = OverlayConfig::FontStyle::FS_Bold;
			break;
		case 2:
			fs = OverlayConfig::FontStyle::FS_Italic;
			break;
		case 3:
			fs = OverlayConfig::FontStyle::FS_BoldItalic;
			break;
		default:
			fs = OverlayConfig::FontStyle::FS_Regular;
			break;
		}
		OverlayConfig::TextAlignment	alignment;
		switch (text_alignment)
		{
		case 0:
			alignment = OverlayConfig::TextAlignment::TA_Left;
			break;
		case 1:
			alignment = OverlayConfig::TextAlignment::TA_Centre;
			break;
		case 2:
			alignment = OverlayConfig::TextAlignment::TA_Right;
			break;
		default:
			alignment = OverlayConfig::TextAlignment::TA_Centre;
			break;
		}

		OverlayConfigBuilder builder;
		builder
			.SetString(overlay_text)
			.SetFontColor(std::get<0>(font_color), std::get<1>(font_color), std::get<2>(font_color), std::get<3>(font_color))
			.SetFontName(font_name)
			.SetFontSize(font_size)
			.SetFontRotation(font_rotation)
			.SetFontStyle(fs)
			.SetLineAlignment(alignment)
			.SetTextAlignment(alignment)
			.SetDisplayOffset({ std::get<0>(display_offset), std::get<1>(display_offset), std::get<2>(display_offset), std::get<3>(display_offset) });
		;


		{
			log_printf("in RPMSetViewOverlay_Background, transfer this job to main_ui_thread\n");
		}

		// requre extend interface
		ins.AttachFromOtherThread((HWND)target_window, builder.Build());

		return true;
	}

}

void RPMClearViewOverlay(void* target_window)
{
	LOG_FUNC;
	ViewOverlyController::getInstance().Detach((HWND)target_window);
}


