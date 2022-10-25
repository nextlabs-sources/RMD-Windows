#pragma once

#include <windows.h>
#include <list>

class CLog
{
public:
	~CLog();
	CLog();
	bool Init(int nLogLevel, DWORD logPolicy);
	int WriteLog(int lvl,const char* file,int line,const wchar_t* fmt,...);
	int WriteLog(const wchar_t* fmt, ...);

protected:
	std::wstring GetLogFile();

	HANDLE   m_hlogFile;
};
extern CLog theLog;
