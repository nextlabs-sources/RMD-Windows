#include "pch.h"
#include "misc.h"

// some Jave proejct, the first thread is not ui thread

//DWORD GetMainThreadID() {
//#ifndef MAKEULONGLONG
//#define MAKEULONGLONG(ldw, hdw) ((ULONGLONG(hdw) << 32) | ((ldw) & 0xFFFFFFFF))
//#endif
//
//#ifndef MAXULONGLONG
//#define MAXULONGLONG ((ULONGLONG)~((ULONGLONG)0))
//#endif
//
//	DWORD dwProcID = ::GetCurrentProcessId();
//	DWORD dwMainThreadID = 0;
//	ULONGLONG ullMinCreateTime = MAXULONGLONG;
//
//	HANDLE hThreadSnap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
//	if (hThreadSnap != INVALID_HANDLE_VALUE) {
//		THREADENTRY32 th32;
//		th32.dwSize = sizeof(THREADENTRY32);
//		BOOL bOK = TRUE;
//		for (bOK = Thread32First(hThreadSnap, &th32); bOK;
//			bOK = Thread32Next(hThreadSnap, &th32)) {
//			if (th32.th32OwnerProcessID == dwProcID) {
//				HANDLE hThread = OpenThread(THREAD_QUERY_INFORMATION,
//					TRUE, th32.th32ThreadID);
//				if (hThread) {
//					FILETIME afTimes[4] = { 0 };
//					if (GetThreadTimes(hThread,
//						&afTimes[0], &afTimes[1], &afTimes[2], &afTimes[3])) {
//						ULONGLONG ullTest = MAKEULONGLONG(afTimes[0].dwLowDateTime,
//							afTimes[0].dwHighDateTime);
//						if (ullTest && ullTest < ullMinCreateTime) {
//							ullMinCreateTime = ullTest;
//							dwMainThreadID = th32.th32ThreadID; // let it be main... :)
//						}
//					}
//					CloseHandle(hThread);
//				}
//			}
//		}
//		CloseHandle(hThreadSnap);
//	}
//	return dwMainThreadID;
//}

using namespace Gdiplus;


int GetEncoderClsid(const WCHAR* format, CLSID* pClsid) {
	UINT  num = 0;          // number of image encoders
	UINT  size = 0;         // size of the image encoder array in bytes

	ImageCodecInfo* pImageCodecInfo = NULL;

	GetImageEncodersSize(&num, &size);
	if (size == 0)
		return -1;  // Failure

	pImageCodecInfo = (ImageCodecInfo*)(malloc(size));
	if (pImageCodecInfo == NULL)
		return -1;  // Failure

	GetImageEncoders(num, size, pImageCodecInfo);

	for (UINT j = 0; j < num; ++j)
	{
		if (wcscmp(pImageCodecInfo[j].MimeType, format) == 0)
		{
			*pClsid = pImageCodecInfo[j].Clsid;
			free(pImageCodecInfo);
			return j;  // Success
		}
	}

	free(pImageCodecInfo);
	return -1;  // Failure
}


// calculate the size art-text used
Gdiplus::SizeF CalcTextSizeF(const Gdiplus::Font& font, const Gdiplus::StringFormat& strFormat, const CString& szText) {
	Gdiplus::GraphicsPath graphicsPathObj;
	Gdiplus::FontFamily fontfamily;
	font.GetFamily(&fontfamily);
	graphicsPathObj.AddString(szText, -1, &fontfamily, font.GetStyle(), font.GetSize(), PointF(0, 0), &strFormat);
	Gdiplus::RectF rcBound;
	graphicsPathObj.GetBounds(&rcBound);
	return Gdiplus::SizeF(rcBound.Width, rcBound.Height);
}


Gdiplus::SizeF CalcTextSizeF(
	const Gdiplus::Graphics& drawing_surface,
	const CString& szText,
	const Gdiplus::StringFormat& strFormat,
	const Font& font) {
	Gdiplus::RectF rcBound;
	drawing_surface.MeasureString(szText, -1, &font, Gdiplus::PointF(0, 0), &strFormat, &rcBound);
	return Gdiplus::SizeF(rcBound.Width, rcBound.Height);
}



std::vector<std::wstring> GetInstalledFonts() {
	Gdiplus::InstalledFontCollection ifc;
	std::vector<std::wstring> rt;
	auto count = ifc.GetFamilyCount();
	int actualFound = 0;

	Gdiplus::FontFamily* buf = new Gdiplus::FontFamily[count];
	ifc.GetFamilies(count, buf, &actualFound);
	for (int i = 0; i < actualFound; i++) {
		wchar_t name[0x20] = { 0 };
		buf[i].GetFamilyName(name);
		rt.push_back(name);
	}

	delete[] buf;
	return rt;
}

bool iequal(const std::wstring& l, const std::wstring& r) {
	if (l.size() != r.size()) {
		return false;
	}

	return std::equal(l.begin(), l.end(), r.begin(), r.end(), [](wchar_t i, wchar_t j) {
		return std::tolower(i) == std::tolower(j);
		});

}


namespace gdi {
	using namespace std;
	vector<wstring> GetInstalledFonts()
	{
		Gdiplus::InstalledFontCollection ifc;
		vector<wstring> rt;
		auto count = ifc.GetFamilyCount();
		int actualFound = 0;

		Gdiplus::FontFamily* buf = new Gdiplus::FontFamily[count];
		ifc.GetFamilies(count, buf, &actualFound);
		for (int i = 0; i < actualFound; i++) {
			wchar_t name[0x20] = { 0 };
			buf[i].GetFamilyName(name);
			rt.push_back(name);
		}

		delete[] buf;
		return rt;
	}

	bool SaveFileAsBitmap(Gdiplus::Image* image, std::wstring path)
	{

		CLSID clsid;
		GetEncoderClsid(L"image/bmp", &clsid);

		image->Save(path.c_str(), &clsid);

		return false;
	}

	Gdiplus::PointF CaculateRotated(Gdiplus::PointF& org, int angle)
	{
		static const double PI = std::acos(-1);
		Gdiplus::PointF rt;

		double radians = angle * PI / 180;

		rt.X = (Gdiplus::REAL)(org.X * std::cos(radians) - org.Y * std::sin(radians));
		rt.Y = (Gdiplus::REAL)(org.X * std::sin(radians) + org.Y * std::cos(radians));

		return rt;
	}


	// 给定四个点 如何计算 最小矩形正好包含所有信息?
	// 最小的x,y是原点,  最小的x和最大的x的差值就是宽	
	Gdiplus::RectF CaculateOutbound(Gdiplus::PointF(&org)[4])
	{

		vector<Gdiplus::REAL> Xs, Ys;
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

	// 给定一个矩形,默认水平放置,计算其旋转后可以包围此矩形的最小矩形
	Gdiplus::RectF CalculateMinimumEnclosingRectAfterRotate(const Gdiplus::SizeF& size, int rotate)
	{
		PointF org[4] = {
			{0,0},{0,size.Height},{size.Width, size.Height},  {size.Width, 0}
		};

		PointF org_r[4];
		for (int i = 0; i < 4; i++) {
			org_r[i] = gdi::CaculateRotated(org[i], rotate);
		}

		return gdi::CaculateOutbound(org_r);
	}
}




//
//  for config of overlay
// 

std::wstring OverlayConfigBuilder::GetDefaultFontName()
{
	// using Gdipuls provided default 
	std::vector<wchar_t> buf(0x30, 0);
	Gdiplus::FontFamily::GenericSansSerif()->GetFamilyName(buf.data(), LANG_NEUTRAL);
	return std::wstring(buf.data());
}


bool OverlayConfigBuilder::IsFontNameSupported(const std::wstring& font_name)
{
	auto cont = GetInstalledFonts();
	for (auto& i : cont) {
		if (iequal(i, font_name)) {
			return true;
		}
	}
	return false;
}

bool OverlayConfig::IsSameConfig(const OverlayConfig& rh)
{
	if (this == &rh) {
		return true;
	}
	// check all 
	if (this->m_str != rh.m_str) { return false; }
	if (this->m_font_size != rh.m_font_size) { return false; }
	if (this->m_font_rotation != rh.m_font_rotation) { return false; }
	if (this->m_font_color_A != rh.m_font_color_A) { return false; }
	if (this->m_font_color_R != rh.m_font_color_R) { return false; }
	if (this->m_font_color_G != rh.m_font_color_G) { return false; }
	if (this->m_font_color_B != rh.m_font_color_B) { return false; }
	if (this->m_font_style != rh.m_font_style) { return false; }
	if (!iequal(this->m_font_name, rh.m_font_name)) { return false; }
	if (this->m_display_offset != rh.m_display_offset) { return false; }

	return true;
}



