// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

// add headers that you want to pre-compile here
#include "framework.h"

// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
#ifndef WINVER				// Allow use of features specific to Windows XP or later.
#define WINVER 0x0501		// Change this to the appropriate value to target other versions of Windows.
#endif

#ifndef _WIN32_WINNT		// Allow use of features specific to Windows XP or later.                   
#define _WIN32_WINNT 0x0501	// Change this to the appropriate value to target other versions of Windows.
#endif						

#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
#define _WIN32_WINDOWS 0x0410 // Change this to the appropriate value to target Windows Me or later.
#endif

#ifndef _WIN32_IE			// Allow use of features specific to IE 6.0 or later.
#define _WIN32_IE _WIN32_IE_IE70	// Change this to the appropriate value to target other versions of IE.
#endif

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN		// Exclude rarely-used stuff from Windows headers
#endif

#ifndef NTDDI_VERSION
#define NTDDI_VERSION NTDDI_WIN7
#endif

#define _USING_V110_SDK71_
#define _ATL_XP_TARGETING

// Windows Header Files:


#include <string>
#include <vector>

//#include <afxwin.h>   


#include <shlwapi.h>
#include <ShellAPI.h>
#include <shlobj.h>
#include <UrlMon.h>
#include <msopc.h>
#include <atlbase.h>
#include <atlcom.h>

using namespace std;
using namespace ATL;

#include <assert.h>

#include <windows.h>
#include <iostream>
#include<algorithm>

#endif //PCH_H
