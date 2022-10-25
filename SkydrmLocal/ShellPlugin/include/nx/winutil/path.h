

#ifndef __NX_WINUTIL_PATH_H__
#define __NX_WINUTIL_PATH_H__

#include <nx\common\result.h>

namespace NX {

	namespace win {

		class KnownDirs
		{
		public:
			KnownDirs() {}
			virtual ~KnownDirs() {}

		protected:
			virtual Result Initialize(_In_opt_ HANDLE hToken = NULL, bool createIfNotExits = false) = 0;

		protected:
			std::wstring GetKnownDirectory(const GUID& rfid, _In_opt_ HANDLE hToken = NULL, bool createIfNotExits = false);
		};

		class SystemDirs : public KnownDirs
		{
		public:
			SystemDirs();
			SystemDirs(const SystemDirs& rhs);
			~SystemDirs();

			SystemDirs& operator = (const SystemDirs& rhs);

			inline const std::wstring& GetSystemDir() const { return _system; }
			inline const std::wstring& GetWindowsDir() const { return _windows; }
			inline const std::wstring& GetWindowsTempDir() const { return _wintemp; }
			inline const std::wstring& GetUsersDir() const { return _users; }
			inline const std::wstring& GetFontsDir() const { return _fonts; }
			inline const std::wstring& GetProgramDataDir() const { return _programData; }
			inline const std::wstring& GetProgramFilesDir() const { return _programFiles; }
			inline const std::wstring& GetProgramFilesX86Dir() const { return _programFilesX86; }
			inline const std::wstring& GetProgramCommonFilesDir() const { return _programCommonFiles; }
			inline const std::wstring& GetProgramCommonFilesX86Dir() const { return _programCommonFilesX86; }

		protected:
			virtual Result Initialize(_In_opt_ HANDLE hToken = NULL, bool createIfNotExits = false);

		private:
			std::wstring	_system;
			std::wstring	_windows;
			std::wstring	_wintemp;
			std::wstring	_users;
			std::wstring	_fonts;
			std::wstring	_programData;
			std::wstring	_programFiles;
			std::wstring	_programFilesX86;
			std::wstring	_programCommonFiles;
			std::wstring	_programCommonFilesX86;
		};

		class UserDirs;

		class PublicUserDirs : public KnownDirs
		{
		public:
			PublicUserDirs();
			PublicUserDirs(const PublicUserDirs& rhs);
			~PublicUserDirs();

			PublicUserDirs& operator = (const PublicUserDirs& rhs);

			inline const std::wstring& GetUserProfileDir() const { return _profile; }
			inline const std::wstring& GetUserDesktopDir() const { return _desktop; }
			inline const std::wstring& GetUserDocumentsDir() const { return _documents; }
			inline const std::wstring& GetUserDownloadsDir() const { return _downloads; }
			inline const std::wstring& GetUserLibrariesDir() const { return _libraries; }
			inline const std::wstring& GetUserMusicDir() const { return _music; }
			inline const std::wstring& GetUserPicturesDir() const { return _pictures; }
			inline const std::wstring& GetUserVideosDir() const { return _videos; }

		protected:
			virtual Result Initialize(_In_opt_ HANDLE hToken = NULL, bool createIfNotExits = false);

		private:
			std::wstring	_profile;
			std::wstring	_desktop;
			std::wstring	_documents;
			std::wstring	_downloads;
			std::wstring	_libraries;
			std::wstring	_music;
			std::wstring	_pictures;
			std::wstring	_videos;
			friend class UserDirs;
		};

		class UserDirs : public PublicUserDirs
		{
		public:
			UserDirs();
			explicit UserDirs(_In_opt_ HANDLE hToken);
			UserDirs(const UserDirs& rhs);
			~UserDirs();

			UserDirs& operator = (const UserDirs& rhs);

			inline const std::wstring& GetUserInetCacheDir() const { return _inetCache; }
			inline const std::wstring& GetUserLocalAppDataDir() const { return _localAppData; }
			inline const std::wstring& GetUserLocalAppDataLowDir() const { return _localAppDataLow; }

		protected:
			virtual Result Initialize(_In_opt_ HANDLE hToken = NULL, bool createIfNotExits = false);

		private:
			std::wstring	_inetCache;
			std::wstring	_localAppData;
			std::wstring	_localAppDataLow;
		};

		const SystemDirs& GetSystemDirs();
		const PublicUserDirs& GetPublicUserDirs();
		const UserDirs& GetCurrentUserDirs();

		class FilePath
		{
		public:
			FilePath();
			explicit FilePath(const std::wstring& path, bool normalize = false);
			explicit FilePath(HANDLE h);
			FilePath(const FilePath& rhs);
			virtual ~FilePath();

			static bool IsDosDriveName(const std::wstring& path);
			static bool IsDosDriveRoot(const std::wstring& path);
			static bool IsGlobalPath(const std::wstring& path);
			static bool IsGlobalDosPath(const std::wstring& path);
			static bool IsGlobalUncPath(const std::wstring& path);
			static bool IsUncPath(const std::wstring& path);
			static bool IsDosPath(const std::wstring& path);
			static bool IsNtPath(const std::wstring& path);
			static bool IsRemoteNtPath(const std::wstring& path);
			static bool IsFileName(const std::wstring& path);

			static std::wstring GetCurrentDir();

			static std::wstring FileHandleToNtPath(HANDLE h);
			static std::wstring NtPathToDosPath(const std::wstring& path);
			static std::wstring DosPathToNtPath(const std::wstring& path);
			static std::wstring ToShortFilePath(const std::wstring& path);
			static std::wstring ToLongFilePath(const std::wstring& path);

			FilePath& operator = (const FilePath& rhs);

			inline bool Empty() const { return _fullPath.empty(); }
			inline const std::wstring& GetFullPath() const { return _fullPath; }

			void Clear();
			bool IsRemoteFile() const;
			std::wstring GetParentDir() const;
			std::wstring GetFileName() const;
			std::wstring GetFileExtension() const;

		protected:
			virtual std::wstring Normalize(const std::wstring& path);

		private:
			std::wstring	_fullPath;
		};

		class ModulePath : public FilePath
		{
		public:
			ModulePath();
			explicit ModulePath(HMODULE h);
			virtual ~ModulePath();
		protected:
			std::wstring HandleToPath(HMODULE h);
		};

	}	// NX::win

}   // NX

#endif
