#include "stdafx.h"
#include "CommonFunction.h"
#include "Log.h"

CLog theLog;

#define MAX_LOG_MESSAGE_SIZE_CHARS 1024

class CLogContent
{
public:
	CLogContent() : m_wszLog{ 0 } {}
	static void Init();

public:
	void *operator new(size_t size);
	void operator delete(void*);

public:
	wchar_t m_wszLog[MAX_LOG_MESSAGE_SIZE_CHARS];
	int m_nlogLevel;

private:
	// private heap used for Alloc memory for object of this class.
	static HANDLE m_priHeap;
};

HANDLE CLogContent::m_priHeap = NULL;

void CLogContent::Init()
{
	if (m_priHeap==NULL)
	{
		m_priHeap = HeapCreate(0, 0, 0);
	}
}

void* CLogContent::operator new(size_t size)
{
	if (m_priHeap!=NULL)
	{
		return HeapAlloc(m_priHeap, 0, size);
	}
	return NULL;
}

void CLogContent::operator delete(void* p)
{
	if (m_priHeap!=NULL)
	{
		HeapFree(m_priHeap, 0, p);
	}
}

CLog::CLog()
{
	m_hlogFile = NULL;
}

CLog::~CLog()
{
	if (m_hlogFile!=NULL && m_hlogFile!=INVALID_HANDLE_VALUE)
	{
		FlushFileBuffers(m_hlogFile);
		CloseHandle(m_hlogFile);
		m_hlogFile = NULL;
	}
}

bool CLog::Init(int nLogLevel, DWORD logPolicy)
{
//	CleanLogFile(7 * 24 * 3600);

	//init logcontent
	CLogContent::Init();

	//Create log file
	std::wstring wstrLogFileName = GetLogFile();
	m_hlogFile = ::CreateFileW(wstrLogFileName.c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, 0, NULL);

	

#if ASYN_LOG
	InitializeCriticalSection(&m_csLogs);
	CloseHandle(CreateThread(NULL, 0, WriteLogThread, this, 0, NULL));
#endif
	
	return true;
}

int CLog::WriteLog(int lvl, const char* file, int line, const wchar_t* fmt, ...)
{
	int nLog = 0;
	std::shared_ptr<CLogContent> pLogContent(new CLogContent());
	//format log content
	va_list args;
	va_start(args, fmt);
	nLog = _vsnwprintf_s(pLogContent->m_wszLog, _countof(pLogContent->m_wszLog) - 1, fmt, args);
	va_end(args);

// whether config, 
#ifdef DEBUG
	::OutputDebugStringW(std::wstring(pLogContent->m_wszLog).c_str());
#endif // DEBUG


	if (m_hlogFile == NULL) {
		return nLog;
	}
	if (m_hlogFile == INVALID_HANDLE_VALUE) {
		return nLog;
	}

	DWORD dwBytesWriteen = 0;
	WriteFile(m_hlogFile, pLogContent->m_wszLog, (DWORD)(wcslen(pLogContent->m_wszLog) * sizeof(wchar_t)), &dwBytesWriteen, NULL);

	return nLog;
}

int CLog::WriteLog(const wchar_t* fmt, ...)
{
	int nLog = 0;
	std::shared_ptr<CLogContent> pLogContent(new CLogContent());
	wchar_t m_wszLog[MAX_LOG_MESSAGE_SIZE_CHARS] = { 0 };
	//format log content
	va_list args;
	va_start(args, fmt);
	nLog = _vsnwprintf_s(m_wszLog, MAX_LOG_MESSAGE_SIZE_CHARS-1, fmt, args);
	va_end(args);

	// whether config, 
#ifdef DEBUG
	::OutputDebugStringW(std::wstring(pLogContent->m_wszLog).c_str());
#endif // DEBUG


	if (m_hlogFile == NULL) {
		return nLog;
	}
	if (m_hlogFile == INVALID_HANDLE_VALUE) {
		return nLog;
	}

	DWORD dwBytesWriteen = 0;
	WriteFile(m_hlogFile, m_wszLog, (DWORD)(wcslen(m_wszLog) * sizeof(wchar_t)), &dwBytesWriteen, NULL);

	return nLog;
}

std::wstring CLog::GetLogFile()
{
	std::wstring strAppDataFolder = CommonFunction::GetProgramDataFolder();

	::OutputDebugStringW(strAppDataFolder.c_str());
	if (!strAppDataFolder.empty())
	{
		std::wstring strLogFile = strAppDataFolder + L"\\log";

		CreateDirectoryW(strLogFile.c_str(), NULL);
		strLogFile += L"\\";
		strLogFile += CommonFunction::GetLocalTimeString();
		strLogFile += L".txt";
		return strLogFile;
	}

	return L"";
}
