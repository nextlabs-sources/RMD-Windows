#pragma once

#include "stdafx.h"

#define NXSDK_API extern "C" __declspec(dllexport) 

NXSDK_API int HOOPS_EXCHANGE_GetAssemblyPathsFromModelFile(const wchar_t* filename, wchar_t** ppaths, wchar_t** pmissingpaths, uint32_t* pmissingCounts);