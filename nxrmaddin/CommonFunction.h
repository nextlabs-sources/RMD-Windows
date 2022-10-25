#ifndef COMMON_FUNCTION_H
#define COMMON_FUNCTION_H

#include <string>
#include <set>
#include <map>
#include "json.hpp"

class CommonFunction
{
public:
	static std::wstring GetFileFolder(const std::wstring& filePath);
	static std::wstring GetFileName(const std::wstring& filePath);
	static std::wstring GetProgramDataFolder();
	static std::wstring GetLocalTimeString();
	static SYSTEMTIME LocalTimeStringToSYSTEMTIME(std::wstring);
	static double IntervalOfSYSTEMTIME(SYSTEMTIME start, SYSTEMTIME end);
	static bool IsNXLFile(const wchar_t* wszFilePath);
	static bool IsNXLSuffix(const wchar_t* wszFilePath);
	static bool FileExist(const wchar_t* wszFullFilePath);
	static bool IsReadOnlyFile(const wchar_t* wszFullFilePath);

	static std::wstring GetClassPath(const std::wstring &classid);
	static std::wstring GetApplicationPath(const std::wstring &appname);

	// designed for clear all files in AutoRecovery folder
	// static void ClearFolderContents(const std::wstring& path); // NOTICE! VERY DANGEROUS

	static BSTR LoadCustomUIFromFile(WCHAR *FileName);

	// office AUTOSAVE FOLDER  %APPDATA%\Microsoft\PowerPoint

	//static bool GetAutoSaveDir_PPT(std::wstring& outPath);
	//static bool GetAutoSaveDir_WORD(std::wstring& outPath);
	//static bool GetAutoSaveDir_EXCEL(std::wstring& outPath);

	// Notice, this is not standard url encoder
	static inline std::wstring getURLEncoderName(const std::wstring path) {
		wchar_t buf[MAX_PATH] = { 0 };
		DWORD len = MAX_PATH;

		::InternetCanonicalizeUrlW(path.c_str(), buf, &len, 0);

		return std::wstring(buf, len);
	}


	// store nxl file that has been openned, only store its name'stem without suffix and urlencodede
	static std::vector<std::wstring> nxlFileNameStemURLEncoudered;

	static std::vector<std::wstring> nxlFilePath;
	static std::map<std::wstring, bool> openedNxlFilePath;


	static std::wstring GetOfficeVersion();
	static void SetOfficeVersion(const std::wstring& version);
	static std::wstring GetOfficeName();
	static void SetOfficeName(const std::wstring& name);

	static std::wstring GetProcessName(unsigned long processId);

	//static void TryingDeleteRegKeyInResiliency();

	static void NotifiyRMDAppToSyncNxlFile(const std::wstring nxlFilePath);

	static std::wstring RPMEditFindMap(const std::wstring filePath);

	static std::wstring String2WString(const std::string& str);

	static std::string Wstring2String(const std::wstring& wstr);

	static bool IsPathLenghtMoreThan259(const std::wstring& filePath);

	// trim from start (in place)
	static inline void ltrim(std::wstring &s) {
		s.erase(s.begin(), std::find_if(s.begin(), s.end(),
			std::not1(std::ptr_fun<int, int>(std::isspace))));
	}

	// trim from end (in place)
	static inline void rtrim(std::wstring &s) {
		s.erase(std::find_if(s.rbegin(), s.rend(),
			std::not1(std::ptr_fun<int, int>(std::isspace))).base(), s.end());
	}

	// trim from both ends (in place)
	static inline void trim(std::wstring &s) {
		ltrim(s);
		rtrim(s);
	}

	static inline bool is_win8andabove(void)
	{
		OSVERSIONINFOEXW osvi;
		DWORDLONG dwlConditionMask = 0;
		int op1 = VER_GREATER_EQUAL;

		// Initialize the OSVERSIONINFOEX structure.
		ZeroMemory(&osvi, sizeof(OSVERSIONINFOEXW));
		osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);
		osvi.dwMajorVersion = 6;
		osvi.dwMinorVersion = 2;
		VER_SET_CONDITION(dwlConditionMask, VER_MAJORVERSION, op1);
		VER_SET_CONDITION(dwlConditionMask, VER_MINORVERSION, op1);

		return 0 != VerifyVersionInfoW(&osvi, VER_MAJORVERSION | VER_MINORVERSION, dwlConditionMask);
     }


#ifdef NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR
	static bool IsSanctuaryFolder(const std::wstring & path, uint32_t * dirstatus, std::wstring & filetags);
#endif // NEXTLABS_FEATURE_SKYDRM_SANCTUARY_DIR


	static bool ReadTag(_In_ IDispatch* pDocObj, _Out_ std::vector<std::pair<std::wstring, std::wstring>>& vecTagPair);

	static std::wstring ParseTag(const std::vector<std::pair<std::wstring, std::wstring>>& tags);

};


namespace nextlabs {
	namespace utils {

		std::wstring get_app_name();
		void set_app_name(const std::wstring& name);


		inline std::string str_toupper(std::string s) {
			std::transform(s.begin(), s.end(), s.begin(),
				[](unsigned char c) { return std::toupper(c); } // correct
			);
			return s;
		}

		inline std::string str_tolower(std::string s) {
			std::transform(s.begin(), s.end(), s.begin(),
				[](unsigned char c) { return std::tolower(c); } // correct
			);
			return s;
		}


		inline std::wstring wstr_toupper(std::wstring s) {
			std::transform(s.begin(), s.end(), s.begin(),
				[](unsigned char c) { return std::toupper(c); } // correct
			);
			return s;
		}

		inline std::wstring wstr_tolower(std::wstring s) {
			std::transform(s.begin(), s.end(), s.begin(),
				[](unsigned char c) { return std::tolower(c); } // correct
			);
			return s;
		}

		inline std::wstring to_wstr(const std::string& s) {
			return std::wstring(s.begin(), s.end());
		}


		inline bool iconstain(const std::string& s, const std::string& sub) {
			auto s_ = str_tolower(s);
			auto sub_ = str_tolower(sub);
			return s_.find(sub_, 0) != decltype(s_)::npos;
		}

		inline bool iconstain(const std::wstring& s, const std::wstring& sub) {
			auto s_ = wstr_tolower(s);
			auto sub_ = wstr_tolower(sub);
			return s_.find(sub_, 0) != decltype(s_)::npos;
		}


		inline bool ibegin_with(const std::wstring& m, const std::wstring& s) {
			using namespace std;
			if (m.size() < s.size()) {
				return false;
			}
			auto m_it = m.cbegin();
			auto s_it = s.cbegin();
			for (; m_it != m.cend() && s_it != s.cend(); ++m_it, ++s_it) {
				if (tolower(*m_it) == tolower(*s_it)) {
					continue;
				}
				else {
					return false;
				}
			}
			// s must be get through
			if (s_it != s.cend()) {
				return false;
			}
			return true;
		}

		class Registry {
		public:
			class param {
				friend class Registry;
			public:
				param(const std::wstring& path, HKEY which_root = HKEY_LOCAL_MACHINE)
					:sub_key(path), access_right(KEY_READ| KEY_WOW64_64KEY), // always access 64bit, shutdown Registry-Visualization
					root(which_root), open_key(NULL) {}
			private:
				HKEY root; //HKEY_CLASSES_ROOT,HKEY_CURRENT_USER,HKEY_LOCAL_MACHINE
				std::wstring sub_key;
				REGSAM access_right;
				HKEY open_key;
				class close_guard {
					friend class Registry;
					HKEY _open_key;
					close_guard(HKEY open_key) :_open_key(open_key) {}
					~close_guard() { ::RegCloseKey(_open_key); }
				};
			};

			bool get(param& p, const std::wstring& name, std::wstring& out_value) {
				if (!_open(p)) {
					return false;
				}
				param::close_guard gurad(p.open_key);
				std::uint32_t length = _buflen(p, name);
				if (0 > length) {
					return false;
				}
				else if (0 == length) {
					out_value.clear();
					return true;
				}
				else {
					out_value.resize(length / 2);
				}

				if (!_get(p, name, (std::uint8_t*)out_value.data(), length)) {
					return false;
				}
				// str trim
				if (out_value.back() == '\0') {
					out_value.pop_back();
				}
				return true;

			}

			bool get(param& p, const std::wstring& name, std::uint32_t& out_value) {
				if (!_open(p)) {
					return false;
				}
				param::close_guard gurad(p.open_key);
				std::uint32_t length = _buflen(p, name);
				if (-1 == length || length != sizeof(out_value)) {
					return false;
				}
				return _get(p, name, (std::uint8_t*) & out_value, sizeof(out_value));
			}
		private:
			inline bool _open(param& p) {
				return ERROR_SUCCESS == ::RegOpenKeyExW(p.root, p.sub_key.c_str(), NULL, p.access_right, &p.open_key);
			}
			inline std::uint32_t _buflen(param& p, const std::wstring& name) {
				DWORD length = -1;
				DWORD type;
				::RegQueryValueExW(p.open_key, name.c_str(), NULL, &type, NULL, &length);
				return length;

			}
			inline bool _get(param& p, const std::wstring& name, std::uint8_t* buf, std::uint32_t buf_len) {
				return ERROR_SUCCESS == ::RegQueryValueExW(p.open_key, name.c_str(), NULL, NULL, buf, (DWORD*)&buf_len);
			}

		};

		inline bool get_SkyDRM_folder(std::wstring& folder) {
			wchar_t tmp[0x100] = { 0 };
			if (FAILED(::SHGetFolderPathW(NULL, CSIDL_LOCAL_APPDATA, NULL, SHGFP_TYPE_CURRENT, tmp))) {
				return false;
			}
			if (!::PathAppendW(tmp, LR"_(NextLabs\SkyDRM\)_")) {
				return false;
			}
			folder.assign(tmp);
			return true;
		}

		inline bool isAppStreamEnv() {
			utils::Registry::param rp(LR"(SOFTWARE\Amazon\MachineImage)", HKEY_LOCAL_MACHINE);
			utils::Registry r;
			std::wstring aminame;
			return r.get(rp, L"AMIName", aminame) && !aminame.empty();
		}

	}
}





/*
 design as a container to record rights with all nxl file that opened,
 once a nxl file closed, remove it in it;
*/
//class FileRights {
//public:
//	inline void insert(const std::wstring& path, const ULONGLONG rights) {
//		std::lock_guard<decltype(_mtx)> lock(_mtx);
//		_path2right[path] = rights; }
//	inline void remove(const std::wstring& path) {
//		std::lock_guard<decltype(_mtx)> lock(_mtx);
//		auto iter = _path2right.find(path);
//		if (iter != _path2right.end()) {
//			_path2right.erase(iter);
//		}
//	}
//	inline std::set<ULONGLONG> getRights() const {
//		std::set<ULONGLONG> rt;
//		std::lock_guard<decltype(_mtx)> lock(_mtx);
//
//		std::transform(std::cbegin(_path2right),std::cend(_path2right),
//			std::inserter(rt,rt.begin()),
//			[](const decltype(_path2right)::value_type& pair) {
//			return pair.second;
//			}
//		);
//
//		return std::move(rt);
//	}
//private:
//	mutable std::recursive_mutex _mtx;
//	std::map<std::wstring, ULONGLONG> _path2right;
//};
//
//inline ULONGLONG helper_get_minimium_right(ULONGLONG cur_rights, const FileRights& container) {
//	auto rs = container.getRights();
//	auto rt = cur_rights;
//	std::for_each(std::cbegin(rs),std::cend(rs),
//		[&rt](ULONGLONG i) {
//		rt &= i;
//	}
//	);
//	return rt;
//
//}



//void DisablePowerPointAutoRecoveryFeature(PowerPoint2016::_Application* pApp) {
//	// disable PowerPoint AutoRecovery feature
//	std::wstring path = L"Software\\Microsoft\\Office\\{Version}\\PowerPoint\\Options";
//
//	CComBSTR ver;
//	pApp->get_Version(&ver);
//
//	path.replace(path.find(L"{Version}"), sizeof("{Version}") - 1, ver);
//
//	//OutputDebugStringW(path.c_str());
//
//	CRegKey key;
//	if (ERROR_SUCCESS == key.Open(HKEY_CURRENT_USER, path.c_str(), KEY_WRITE))
//	{
//		key.SetDWORDValue(L"KeepUnsavedChanges", 0);
//		key.SetDWORDValue(L"SaveAutoRecoveryInfo", 0);
//	}
//}






#endif // !COMMON_FUNCTION_H

