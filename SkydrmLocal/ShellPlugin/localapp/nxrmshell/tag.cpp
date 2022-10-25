#include "stdafx.h"
#include "tag.h"
#include "helper.h"

CTag::CTag():_bInitSuccss(false), _fnGetTags(nullptr)
{
}

CTag::~CTag()
{}

bool CTag::GetTags(const std::wstring& filePath, std::vector<TAGPAIR>& outVecTags) const
{
	if (_bInitSuccss) {
		std::vector<TAGPAIR> outTags;
		int ret = _fnGetTags(filePath.c_str(), outTags);
		if (ret == 0) { // succeed
			outVecTags = outTags;
			return true;
		}
	}

	return false;
}

std::wstring CTag::ParseTag(const std::vector<TAGPAIR>& tags)
{
	for (auto i : tags) {
		if (str_istarts_with(i.tagName, L"MSIP_Label_") && str_iends_with(i.tagName, L"_Name")) {
			return i.tagValue;
		}
	}

	return L"";
}

void CTag::InitTagLib(const std::wstring& strlibPath)
{
	HMODULE hM = LoadLibraryExW(strlibPath.c_str(), NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
	if (hM != NULL) {
		_fnGetTags = (pFun_GetTags)GetProcAddress(hM, "GetTags");
		if (_fnGetTags != NULL) {
			_bInitSuccss = true;
		}
	}
}
