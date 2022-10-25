#pragma once
#include <string>
#include <experimental/filesystem>
#include "rmccore/utils/string.h"
using namespace std;

class CommonUtils
{
private:

public:

	static std::string GetRMXPasscode()
	{
		return "{6829b159-b9bb-42fc-af19-4a6af3c9fcf6}";
	}

	static bool StrIcompare(std::wstring c1, std::wstring c2)
	{
		transform(c1.begin(), c1.end(), c1.begin(), toupper);
		transform(c2.begin(), c2.end(), c2.begin(), toupper);
		return (c1 == c2);
	}

	static LONG NxRegQueryValueEx(HKEY hKey, const std::wstring &subkeyName, const std::wstring &valueName, std::wstring &value)
	{
		LONG lRet;
		HKEY _hKey;
		WCHAR tchData[255];
		DWORD dwSize;

		lRet = RegOpenKeyEx(
			hKey,         // handle to open key
			subkeyName.c_str(),  // subkey name
			0,   // reserved
			KEY_READ | KEY_WOW64_64KEY, // security access mask
			&_hKey    // handle to open key
		);

		if (lRet == ERROR_SUCCESS)
		{
			dwSize = sizeof(tchData);
			lRet = RegQueryValueEx(
				_hKey,            // handle to key
				valueName.c_str(),  // value name
				NULL,   // reserved
				NULL,       // type buffer
				(LPBYTE)tchData,        // data buffer
				&dwSize      // size of data buffer
			);
			if (lRet == ERROR_SUCCESS)
			{
				value.append(tchData);
			}
			else
			{

			}
		}
		else
		{

		}

		RegCloseKey(_hKey);

		return lRet;
	}

	static wstring SDRmFileRightToString(SDRmFileRight fileRight) {
		wstring result = L"";
		switch (fileRight)
		{
		case RIGHT_VIEW:
			result = L"VIEW";
			break;
		case RIGHT_EDIT:
			result = L"EDIT";
			break;
		case RIGHT_PRINT:
			result = L"PRINT";
			break;
		case RIGHT_CLIPBOARD:
			result = L"CLIPBOARD";
			break;
		case RIGHT_SAVEAS:
			result = L"SAVEAS";
			break;
		case RIGHT_DECRYPT:
			result = L"DECRYPT";
			break;
		case RIGHT_SCREENCAPTURE:
			result = L"SCREENCAPTURE";
			break;
		case RIGHT_SEND:
			result = L"SEND";
			break;
		case RIGHT_CLASSIFY:
			result = L"CLASSIFY";
			break;
		case RIGHT_SHARE:
			result = L"SHARE";
			break;
		case RIGHT_DOWNLOAD:
			result = L"DOWNLOAD";
			break;
		case RIGHT_WATERMARK:
			result = L"WATERMARK";
			break;
		default:
			break;
		}

		return result;
	}


	static std::wstring to_unlimitedpath(const std::wstring& filepath)
	{
		std::experimental::filesystem::path tfilepath(filepath);
		std::wstring unlimitedpath = tfilepath;

		long     length = 0;
		TCHAR*   buffer = NULL;
		// First obtain the size needed by passing NULL and 0.
		length = GetLongPathName(tfilepath.c_str(), NULL, 0);
		if (length > (long)(unlimitedpath.size() + 1))
		{
			buffer = new TCHAR[length];
			length = GetLongPathName(tfilepath.c_str(), buffer, length);
			if (length > 0)
				unlimitedpath = buffer;
			delete[] buffer;
		}
		else if (length == 0)
		{
			DWORD error = GetLastError();
			if (error == ERROR_FILE_NOT_FOUND)
			{
				// we need to seperate the file name and folder
				std::wstring _parentfolder = tfilepath.parent_path();
				length = GetLongPathName(_parentfolder.c_str(), NULL, 0);
				if (length > (long)(_parentfolder.size() + 1))
				{
					buffer = new TCHAR[length];
					length = GetLongPathName(_parentfolder.c_str(), buffer, length);
					if (length > 0)
						unlimitedpath = (std::wstring)buffer + L"\\" + (std::wstring)(tfilepath.filename());
					delete[] buffer;
				}
			}
		}

		if (unlimitedpath.length() < (MAX_PATH - 10)) // temp 
			return unlimitedpath;

		if (unlimitedpath.find(L"\\\\?") == 0)
			return unlimitedpath;

		if (unlimitedpath.find(L"\\\\") == 0)
		{
			unlimitedpath.erase(0, 1);
			unlimitedpath = L"\\\\?\\UNC" + unlimitedpath;
			return unlimitedpath;
		}

		unlimitedpath = L"\\\\?\\" + unlimitedpath;

		return unlimitedpath;
	}


	static std::string GuidToString(GUID guid)
	{
		char guid_cstr[39];
		snprintf(guid_cstr, sizeof(guid_cstr),
			"{%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x}",
			guid.Data1, guid.Data2, guid.Data3,
			guid.Data4[0], guid.Data4[1], guid.Data4[2], guid.Data4[3],
			guid.Data4[4], guid.Data4[5], guid.Data4[6], guid.Data4[7]);

		return std::string(guid_cstr);
	}

	static std::wstring FormatString(const wchar_t* format, ...)
	{
		va_list args;
		int     len = 0;
		std::wstring s;

		va_start(args, format);
		len = _vscwprintf_l(format, 0, args) + 1;
		vswprintf_s(RMCCORE::wstring_buffer(s, len), len, format, args);
		va_end(args);

		return s;
	}

	static std::wstring remove_extension(const std::wstring& filename) {
		size_t lastdot = filename.find_last_of(L".");
		if (lastdot == std::wstring::npos) return filename;
		return filename.substr(0, lastdot);
	}

	static std::string utf16toutf8(const std::wstring& ws)
	{
		std::string s;
		if (!ws.empty()) {
			const int reserved_size = (int)(ws.length() * 3 + 1);
			if (0 == WideCharToMultiByte(CP_UTF8, 0, ws.c_str(), (int)ws.length(), RMCCORE::string_buffer(s, reserved_size), (int)reserved_size, nullptr, nullptr)) {
				s.clear();
			}
		}
		return s;
	}

	static std::wstring utf8toutf16(const std::string& s)
	{
		std::wstring ws;
		if (!s.empty()) {
			const int reserved_size = (int)s.length();
			if (0 == MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.length(), RMCCORE::wstring_buffer(ws, reserved_size), (int)reserved_size)) {
				ws.clear();
			}
		}
		return ws;
	}

	static std::wstring to_wstring(const std::string& s)
	{
		return utf8toutf16(s);
	}

	static std::string to_string(const std::wstring& s)
	{
		return utf16toutf8(s);
	}

	static std::wstring to_wstring(const std::wstring& s)
	{
		return s;
	}

};




