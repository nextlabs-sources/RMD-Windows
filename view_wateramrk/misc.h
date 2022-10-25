#pragma once

#include <gdiplus.h>

// helper fun
std::vector<std::wstring> GetInstalledFonts();

Gdiplus::SizeF CalcTextSizeF(
	const Gdiplus::Graphics& drawing_surface,
	const CString& szText,
	const Gdiplus::StringFormat& strFormat,
	const Gdiplus::Font& font);


namespace gdi {
	std::vector<std::wstring> GetInstalledFonts();
	bool SaveFileAsBitmap(Gdiplus::Image* image, std::wstring path);
	Gdiplus::PointF CaculateRotated(Gdiplus::PointF& org, int angle);
	Gdiplus::RectF CaculateOutbound(Gdiplus::PointF(&org)[4]);
	Gdiplus::RectF CalculateMinimumEnclosingRectAfterRotate(const Gdiplus::SizeF& size, int rotate);
}

class MsgAntiReenter {
public:
	MsgAntiReenter() {
		if (InitializeCriticalSectionAndSpinCount(&cs, 0x80004000) == FALSE) {
			InitializeCriticalSection(&cs);
		}
	}
	~MsgAntiReenter() {
		DeleteCriticalSection(&cs);
	}
	void thread_enable(void) {
		EnterCriticalSection(&cs);
		disabled_thread[GetCurrentThreadId()]--;
		LeaveCriticalSection(&cs);
	}

	void thread_disable(void) {
		DWORD tid = GetCurrentThreadId();
		EnterCriticalSection(&cs);
		if (disabled_thread.find(tid) == disabled_thread.end()) {
			disabled_thread[tid] = 0;
		}
		disabled_thread[tid]++;
		LeaveCriticalSection(&cs);
	}

	bool is_thread_disabled(void) {
		bool result = false;
		DWORD tid = GetCurrentThreadId();
		EnterCriticalSection(&cs);
		if (disabled_thread.find(tid) != disabled_thread.end()) {
			if (disabled_thread[tid] > 0) {
				result = true;
			}
		}
		LeaveCriticalSection(&cs);
		return result;
	}

private:
	CRITICAL_SECTION     cs;
	std::map<DWORD, int>  disabled_thread;
};
class MsgAntiRenter_Control {
public:
	MsgAntiRenter_Control(MsgAntiReenter& mar) :_mar(mar) { mar.thread_disable(); }
	~MsgAntiRenter_Control() { _mar.thread_enable(); }
private:
	MsgAntiReenter& _mar;
};



class OverlayConfig {
	friend class OverlayConfigBuilder;
public:
	enum FontStyle {
		FS_Regular,
		FS_Bold,
		FS_Italic,
		FS_BoldItalic
	};

	enum TextAlignment {
		TA_Left,
		TA_Centre,
		TA_Right
	};

	OverlayConfig() {}
public: // getter;
	inline const std::wstring& GetString() { return m_str; }

	inline const std::wstring& GetFontName() { return m_font_name; }

	inline Gdiplus::FontStyle GetGdiFontStyle() {
		if (m_font_style == FS_Regular) {
			return Gdiplus::FontStyle::FontStyleRegular;
		}
		if (m_font_style == FS_Bold) {
			return Gdiplus::FontStyle::FontStyleBold;
		}
		if (m_font_style == FS_Italic) {
			return Gdiplus::FontStyle::FontStyleItalic;
		}
		if (m_font_style == FS_BoldItalic) {
			return Gdiplus::FontStyle::FontStyleBoldItalic;
		}

		// Notice: return FontStyleRegular by default
		return Gdiplus::FontStyle::FontStyleRegular;
	}

	inline Gdiplus::StringAlignment GetGdiTextAlignment() {
		if (m_text_alignment == TA_Left) {
			return Gdiplus::StringAlignment::StringAlignmentNear;
		}
		if (m_text_alignment == TA_Centre) {
			return Gdiplus::StringAlignment::StringAlignmentCenter;
		}
		if (m_text_alignment == TA_Right) {
			return Gdiplus::StringAlignment::StringAlignmentFar;
		}
		return Gdiplus::StringAlignment::StringAlignmentCenter;
	}

	inline Gdiplus::StringAlignment GetGdiLineAlignment() {
		if (m_line_alignment == TA_Left) {
			return Gdiplus::StringAlignment::StringAlignmentNear;
		}
		if (m_line_alignment == TA_Centre) {
			return Gdiplus::StringAlignment::StringAlignmentCenter;
		}
		if (m_line_alignment == TA_Right) {
			return Gdiplus::StringAlignment::StringAlignmentFar;
		}
		return Gdiplus::StringAlignment::StringAlignmentCenter;
	}

	inline Gdiplus::Color GetFontColor() { return Gdiplus::Color(m_font_color_A, m_font_color_R, m_font_color_G, m_font_color_B); }
	inline int GetFontSize() { return this->m_font_size; }
	inline int GetFontRotation() { return this->m_font_rotation; }
	inline CRect GetDisplayOffset() { return this->m_display_offset; }


public:
	inline bool operator ==(const OverlayConfig& rh) { return IsSameConfig(rh); }
	inline bool operator !=(const OverlayConfig& rh) { return !IsSameConfig(rh); }
private:
	bool IsSameConfig(const OverlayConfig& rh);

	inline void ResetAllParamByDefault() {
		m_str = L"";
		m_font_name = L"Arial";
		m_font_size = 20;
		m_font_rotation = -20;
		m_font_color_A = 50;
		m_font_color_R = 75;
		m_font_color_G = 75;
		m_font_color_B = 75;
		m_font_style = OverlayConfig::FontStyle::FS_Regular;
		m_line_alignment = OverlayConfig::TextAlignment::TA_Centre;
		m_text_alignment = OverlayConfig::TextAlignment::TA_Centre;
		m_display_offset = CRect();

	}

private:
	std::wstring m_str;
	int m_font_size;
	int m_font_rotation;  //i.e.  -10, -90, 10, 30, 45
	// RGB[0,0,0] is black, RGB[255,255,255] is white
	unsigned char m_font_color_A; // [0,255]: 0: fully transparent, 255: opacity
	unsigned char m_font_color_R;	// [0,255]  
	unsigned char m_font_color_G; // [0,255]
	unsigned char m_font_color_B; // [0,255]
	FontStyle m_font_style;
	TextAlignment m_text_alignment;
	TextAlignment m_line_alignment;
	std::wstring m_font_name;
	CRect m_display_offset;
};

class OverlayConfigBuilder {
	OverlayConfig _config;
public:
	OverlayConfigBuilder() {
		// set default
		_config.ResetAllParamByDefault();
	}

	OverlayConfigBuilder& SetString(const std::wstring& str) {
		_config.m_str = str;
		return *this;
	}

	OverlayConfigBuilder& SetFontName(const std::wstring& font_name) {
		if (font_name.empty()) {
			_config.m_font_name = GetDefaultFontName();
			return *this;
		}
		if (IsFontNameSupported(font_name)) {
			_config.m_font_name = font_name;
		}
		else {
			//throw std::exception("font name is not supported");
			_config.m_font_name = GetDefaultFontName();
		}
		return *this;
	}

	OverlayConfigBuilder& SetFontSize(int size) {
		if (size <= 0) {
			size = 20;// bydefault
		}
		else if (size > 72) {
			size = 72;
		}
		_config.m_font_size = size;
		return *this;
	}
	OverlayConfigBuilder& SetFontRotation(int rotation) {
		_config.m_font_rotation = rotation;
		return *this;
	}

	OverlayConfigBuilder& SetFontTransparency(unsigned char A) {
		_config.m_font_color_A = A;
		return *this;
	}


	OverlayConfigBuilder& SetFontColor(unsigned char A, unsigned char R, unsigned char G, unsigned char B) {
		_config.m_font_color_A = A;
		_config.m_font_color_R = R;
		_config.m_font_color_G = G;
		_config.m_font_color_B = B;
		return *this;
	}

	OverlayConfigBuilder& SetFontColor(unsigned char R, unsigned char G, unsigned char B) {
		_config.m_font_color_R = R;
		_config.m_font_color_G = G;
		_config.m_font_color_B = B;
		return *this;
	}

	OverlayConfigBuilder& SetFontStyle(OverlayConfig::FontStyle fs) {
		_config.m_font_style = fs;
		return *this;
	}

	OverlayConfigBuilder& SetTextAlignment(OverlayConfig::TextAlignment	alignment) {
		_config.m_text_alignment = alignment;
		return *this;
	}

	OverlayConfigBuilder& SetLineAlignment(OverlayConfig::TextAlignment	alignment) {
		_config.m_line_alignment = alignment;
		return *this;
	}

	OverlayConfigBuilder& SetDisplayOffset(const RECT& rc) {
		_config.m_display_offset = rc;
		return *this;
	}


	OverlayConfig Build() {
		ThrowIfInvalidParam();
		return _config;
	}
private:
	inline void ThrowIfInvalidParam() {
		if (_config.m_str.length() < 1) {
			throw std::exception("too little chars in watermark string");
		}
	}
	std::wstring GetDefaultFontName();
	bool IsFontNameSupported(const std::wstring& font_name);
};



