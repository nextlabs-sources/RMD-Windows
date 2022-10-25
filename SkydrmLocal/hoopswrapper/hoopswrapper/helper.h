#pragma once
#include "stdafx.h"
#include <string>

namespace helper {

	inline char* allocStrInComMem(const std::string str) {
		char* buf = NULL;
		const char* p = str.c_str();
		// allco buf and copy p into buf
		buf = (char*)::CoTaskMemAlloc((str.size() + 1) * sizeof(char));
		strcpy_s(buf, str.size() + 1, p);

		return buf;
	}

	inline wchar_t* allocStrInComMem(const std::wstring wstr) {
		wchar_t* buf = NULL;
		const wchar_t* p = wstr.c_str();
		// allco buf and copy p into buf
		buf = (wchar_t*)::CoTaskMemAlloc((wstr.size() + 1) * sizeof(wchar_t));
		wcscpy_s(buf, wstr.size() + 1, p);

		return buf;
	}

	std::wstring utf82utf16(const std::string& str) {
		if (str.empty())
		{
			return std::wstring();
		}
		int num_chars = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), (int)str.length(), NULL, 0);
		std::wstring wstrTo;
		if (num_chars)
		{
			wstrTo.resize(num_chars + 1);
			if (MultiByteToWideChar(CP_UTF8, 0, str.c_str(), (int)str.length(), &wstrTo[0], num_chars))
			{
				wstrTo = std::wstring(wstrTo.c_str());
				return wstrTo;
			}
		}
		return std::wstring();
	}

	std::string& trim(std::string &s)
	{
		if (s.empty())
		{
			return s;
		}

		std::string::iterator c;
		// Erase whitespace before the string
		for (c = s.begin(); c != s.end() && iswspace(*c++);); s.erase(s.begin(), --c);

		// Erase whitespace after the string
		for (c = s.end(); c != s.begin() && iswspace(*--c);); s.erase(++c, s.end());

		return s;
	}

	//######################################################################################################################
	// single point of access to the static log file pointer from all translation units; avoids to add a .cpp file or face
	// multiple definitions error at link time
	// call with a non-null file name to create the log file
	// default return value is stdout
	inline FILE* GetLogFile(const wchar_t* pFileName = NULL)
	{
		// fflush does not work correctly on stdout and stderr if redirected to a file with freopen, so I need a dedicated FILE
		static FILE* pLogFile = NULL;
		if (pFileName)
		{
			if (pLogFile)
			{
				fclose(pLogFile);
				pLogFile = NULL;
			}
			_wfopen_s(&pLogFile, pFileName, L"w");
		}
		return pLogFile ? pLogFile : stdout;
	}

}

