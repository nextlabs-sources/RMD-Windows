// hoopswrapper.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "helper.h"
#include "hoopswrapper.h"
#include <vector>

#define INITIALIZE_A3D_API
#include "A3DSDKIncludes.h"

static std::vector<std::string> vMissingPaths;

inline A3DInt32 PrintLogMessage(A3DUTF8Char* pMsg) {
	std::string searchStr = pMsg;
	std::size_t missing1 = searchStr.find("Not loaded");
	std::size_t missing2 = searchStr.find("File not found");
	if (missing1 != std::string::npos
		|| missing2 != std::string::npos)
	{
		std::string name = searchStr.substr(0, missing2);
		std::string pathAppendix = searchStr.substr(missing2 + strlen("File not found"));
		std::string t_name = helper::trim(name);
		std::string t_pathAppendix = helper::trim(pathAppendix);
		std::size_t pathFIdx = t_pathAppendix.find("0");
		if (pathFIdx != std::string::npos) {
			std::string path = pathAppendix.substr(pathFIdx + 1);
			std::string t_path = helper::trim(path);
			if (!t_path.empty()) {
				vMissingPaths.push_back(t_path);
			}
		}
		else {
			if (!t_name.empty()) {
				vMissingPaths.push_back(t_name);
			}
		}

		printf("%s\n", pMsg);
	}
	return fprintf(helper::GetLogFile(), "%s", pMsg ? pMsg : "");
}

//######################################################################################################################
inline A3DInt32 PrintLogWarning(A3DUTF8Char* pKod, A3DUTF8Char* pMsg)
{
	return fprintf(helper::GetLogFile(), "WAR %s - %s", pKod ? pKod : "", pMsg ? pMsg : "");
}

//######################################################################################################################
inline A3DInt32 PrintLogError(A3DUTF8Char* pKod, A3DUTF8Char* pMsg)
{
	FILE* pLogFile = helper::GetLogFile();
	if (pLogFile == stdout)
		pLogFile = stderr;
	fprintf(pLogFile, "ERR %s - %s", pKod ? pKod : "", pMsg ? pMsg : "");
	return fflush(pLogFile);
}

inline bool Contains(std::vector<std::string> &loaded, std::string &target) {
	std::vector<std::string>::iterator c;
	for (auto c = loaded.begin(); c != loaded.end(); c++) {
		if ((*c).compare(target) == 0) {
			return true;
		}
	}
	return false;
}

// call hoops exchange api.

NXSDK_API int HOOPS_EXCHANGE_GetAssemblyPathsFromModelFile(const wchar_t* filename, wchar_t** ppaths, wchar_t** pmissingpaths, uint32_t* pmissingCounts) {
	// sanity check
	if (filename == NULL) {
		// not found
		return A3D_ERROR;
	}
	// MessageBox(NULL, L"Test", L"Captions", MB_OK);

	// ### INITIALIZE HOOPS EXCHANGE
	// MessageBox(NULL, _T(HOOPS_BINARY_DIRECTORY), L"Captions", MB_OK);

	wchar_t buffer[MAX_PATH];
	GetModuleFileName(NULL, buffer, MAX_PATH);
	std::wstring::size_type pos = std::wstring(buffer).find_last_of(L"\\/");
	std::wstring str = std::wstring(buffer).substr(0, pos);
	A3DSDKHOOPSExchangeLoader sHoopsExchangeLoader(str.c_str());

	if (sHoopsExchangeLoader.m_eSDKStatus != A3D_SUCCESS) {
		//	MessageBox(NULL, L"TestError", L"Captions", MB_OK);
		return A3D_ERROR;
	}

	// This is a workaround to solve jt file missing paths problem.
	A3DDllSetCallbacksReport(PrintLogMessage, PrintLogWarning, PrintLogError);

	// Uncomment these lines to track memory leaks
	A3DUTF8Char sSrcFileNameUTF8[_MAX_PATH];
	wchar_t acSrcFileName[_MAX_PATH * 2];

	wcscpy_s(acSrcFileName, filename);
	A3DMiscUTF16ToUTF8(acSrcFileName, sSrcFileNameUTF8);

	////////////////////////////////////////////////////////////////////////////
	// 1- Check input file.
	////////////////////////////////////////////////////////////////////////////
	FILE* pFile = NULL;
	fopen_s(&pFile, sSrcFileNameUTF8, "rb");
	if (pFile)
	{
		fclose(pFile);
	}
	else
	{
		fprintf(stderr, "cannot open input file %s\n", sSrcFileNameUTF8);
		return A3D_LOAD_CANNOT_ACCESS_CADFILE;
	}
	////////////////////////////////////////////////////////////////////////////
	// 2- Read input file in mode "tree only".[For jt file incremental load mode not supported.]
	////////////////////////////////////////////////////////////////////////////
	A3DImport sImport(acSrcFileName); // see A3DSDKInternalConvert.hxx for import and export detailed parameters
	sImport.m_sLoadData.m_sIncremental.m_bLoadStructureOnly = false;
	sImport.m_sLoadData.m_sIncremental.m_bLoadNoDependencies = false;
	sImport.m_sLoadData.m_sAssembly.m_bUseRootDirectory = false;
	sImport.m_sLoadData.m_sAssembly.m_bRootDirRecursive = false;
	//sImport.m_sLoadData.m_sSpecifics.m_sJT.m_eReadTessellationLevelOfDetail = kA3DJTTessLODHigh;
	A3DStatus iRet = sHoopsExchangeLoader.Import(sImport);
	if (iRet != A3D_SUCCESS && iRet != A3D_LOAD_MISSING_COMPONENTS)
		return iRet;

	////////////////////////////////////////////////////////////////////////////
	// 3- Extract file names.
	////////////////////////////////////////////////////////////////////////////
	A3DUns32 nbFiles = 0, nbAssemblyFiles = 0, nbMissingFiles = 0;
	A3DUTF8Char** ppPaths = NULL, ** ppAssemblyPaths = NULL, ** ppMissingPaths = NULL;
	A3DAsmGetFilesPathFromModelFile(sHoopsExchangeLoader.m_psModelFile, &nbFiles, &ppPaths, &nbAssemblyFiles, &ppAssemblyPaths, &nbMissingFiles, &ppMissingPaths);

	std::string assemblyPaths;
	std::string missingPaths;
	A3DUns32 missingNums = nbMissingFiles;
	std::vector<std::string> loadedMissingPaths;

	for (size_t i = 0; i < nbAssemblyFiles; i++) {
		assemblyPaths.append(*(ppAssemblyPaths + i)).append(";");
	}
	for (size_t i = 0; i < nbMissingFiles; i++) {
		A3DUTF8Char* pPath = *(ppMissingPaths + i);
		loadedMissingPaths.push_back(std::string(pPath));
		missingPaths.append(pPath).append(";");
	}
	for (auto val : vMissingPaths) {
		if (Contains(loadedMissingPaths, val)) {
			continue;
		}
		++missingNums;
		missingPaths.append(val).append(";");
	}

	// Prepare data.
	{
		*ppaths = helper::allocStrInComMem(helper::utf82utf16(assemblyPaths));
		*pmissingpaths = helper::allocStrInComMem(helper::utf82utf16(missingPaths));
		*pmissingCounts = missingNums;
	}

	vMissingPaths.clear();
	return A3D_SUCCESS;
}