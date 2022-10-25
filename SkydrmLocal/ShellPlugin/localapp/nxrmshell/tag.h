#pragma once
#include <string>

class CTag 
{
public:
	CTag();
	~CTag();

	typedef struct _tagPair {
		std::wstring tagName;
		std::wstring tagValue;
		bool bSuccess;
	} TAGPAIR, *LPTAGPAIR;

	typedef int(__stdcall* pFun_GetTags)(LPCWSTR pszFilename, OUT std::vector<TAGPAIR>& listTags);

	bool GetTags(const std::wstring& filePath, std::vector<TAGPAIR>& outVecTags) const;
	inline bool InitSucess() const { return _bInitSuccss; };

	// Return tag name directly
	std::wstring ParseTag(const std::vector<TAGPAIR>& tags);

	void InitTagLib(const std::wstring& strlibPath);

private:
	bool _bInitSuccss;
	pFun_GetTags _fnGetTags;
};