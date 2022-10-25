#pragma once
#include "stdafx.h"

HRESULT create_key_with_default_value(
	const HKEY	root,
	const WCHAR *parent,
	const WCHAR *key,
	const WCHAR *default_value);

HRESULT set_value_content(
	const HKEY	root,
	const WCHAR *key,
	const WCHAR *valuename,
	const WCHAR *content);

HRESULT delete_key(
	const HKEY	root,
	const WCHAR *parent,
	const WCHAR *key);

HBITMAP BitmapFromIcon(HICON hIcon);

int GetCurrentDPI();

// used by IContextMenu to query which files user selected
std::vector<std::wstring> query_selected_file(IDataObject *pdtobj);


// this is a workaround to get skydrm app exe path from specific registry
bool get_skydrm_exe_path(std::wstring & path);

// this is a workaround to get Viewer app exe path from specific registry
bool get_viewer_exe_path(std::wstring & path);

bool is_dir(const std::wstring& path);

bool is_nxl_suffix(const std::wstring& path);

bool str_istarts_with(const std::wstring& s, const std::wstring& s2);
bool str_iends_with(const std::wstring& s, const std::wstring& s2);
bool isOfficeFormatFile(const std::wstring& ext);

std::wstring GetTagLibPath();

std::wstring string2WString(const std::string& str);
std::string  wstring2String(const std::wstring& str);
void SplitString(const std::wstring& ws, std::vector<std::wstring>& v, const std::wstring& c);

enum FileRights
{
	RIGHT_VIEW,
	RIGHT_EDIT,
	RIGHT_PRINT,
	RIGHT_CLIPBOARD,
	RIGHT_SAVEAS,
	RIGHT_DECRYPT,
	RIGHT_SCREENCAPTURE,
	RIGHT_SEND,
	RIGHT_CLASSIFY,
	RIGHT_SHARE,
	RIGHT_DOWNLOAD,
	RIGHT_WATERMARK
};

static inline std::wstring rightsEnum2Str(FileRights rights)
{
	static const std::wstring arry[] =
	{
	  L"RIGHT_VIEW",
	  L"RIGHT_EDIT",
	  L"RIGHT_PRINT",
	  L"RIGHT_CLIPBOARD",
	  L"RIGHT_SAVEAS",
	  L"RIGHT_DECRYPT",
	  L"RIGHT_SCREENCAPTURE",
	  L"RIGHT_SEND",
	  L"RIGHT_CLASSIFY",
	  L"RIGHT_SHARE",
	  L"RIGHT_DOWNLOAD",
	  L"RIGHT_WATERMARK"
	};

	return arry[rights];
};


bool hasRights(const std::wstring& wstrFingerPrint, FileRights specifyRights);
bool isAdhoc(const std::wstring& wstrFingerPrint);
bool hasAdminRights(const std::wstring& wstrFingerPrint);
bool isFromMyVault(const std::wstring& wstrFingerPrint);
bool isFromProject(const std::wstring& wstrFingerPrint);
bool isFromSystemBucket(const std::wstring& wstrFingerPrint);
bool isSaaSRouter(const std::wstring& wstrFingerPrint);



class NamedPipeClient
{
public:
	NamedPipeClient(const std::wstring& pipeName, const std::wstring& filePath) :
		m_pipeName(pipeName), m_FilePath(filePath), m_receivedData(L"") {}

	~NamedPipeClient()
	{
		if (m_hPipe != NULL)
		{
			CloseHandle(m_hPipe);
		}
	}

	bool OnConnectPipe();

	void OnReadPipe();

	void OnWritePipe();

	std::wstring GetReceivedData() { return m_receivedData; }

private:
	HANDLE m_hPipe;
	std::wstring m_pipeName;

	std::wstring m_FilePath;
	std::wstring m_receivedData;
};

namespace fs {
	std::wstring convert_dos_full_file_path(const std::wstring& s);

	bool is_dos_path(const std::wstring& s);
}


