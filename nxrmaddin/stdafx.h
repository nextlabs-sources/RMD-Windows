#pragma once
#include "targetver.h"

// Windows Header Files:
#include <windows.h>
#include <Shlobj.h>
#include <objbase.h>
#include <wininet.h>
#include <stdlib.h>
#include <atlbase.h>

// c++
#include <iostream>
#include <string>
#include <vector>
#include <map>
#include <algorithm>
#include <mutex>
#include <thread>
#include <iomanip>
#include <ctime>
#include <chrono>
#include <regex>
#include <locale>
#include <sstream>
#include <functional>
#include <cctype>

// boost
//#include <boost/algorithm/string.hpp>
//#include <boost/filesystem.hpp>



#include "resource.h"


//#import "C:\Program Files (x86)\Microsoft Office\root\VFS\ProgramFilesCommonX86\Microsoft Shared\OFFICE16\MSO.DLL" rename("RGB","MsRGB") raw_interfaces_only rename_namespace("Office2016")
//#import "C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE" rename("RGB","MsRGB") rename("DialogBox","MsDialogBox") rename("CopyFile","MsCopyFile") raw_interfaces_only rename_namespace("Excel2016")
//#import "C:\Program Files (x86)\Microsoft Office\root\Office16\MSWORD.OLB" rename("ExitWindows","MsExitWindows") raw_interfaces_only rename_namespace("Word2016")
//#import "C:\Program Files (x86)\Microsoft Office\root\Office16\MSPPT.OLB" rename("RGB","MsRGB") raw_interfaces_only rename_namespace("PowerPoint2016")
//#import "C:\Program Files (x86)\Common Files\DESIGNER\MSADDNDR.OLB"  raw_interfaces_only


//
//   Office Common
//
#include "import/mso2016.tlh"
#include "import/msword2016.tlh"
#include "import/excel2016.tlh"
#include "import/msppt2016.tlh"



//
//	special for developer, using DbgView 
//
inline void dev_log(const wchar_t* str) {
#ifdef _DEBUG
	::OutputDebugStringW(str);
#endif // _DEBUG
}
#ifdef DEVLOG
#error "can't be this'"
#else
#ifdef _DEBUG
#define DEVLOG(str)   dev_log((str))
#define DEVLOG_FUN    dev_log(__FUNCTIONW__ L"\n");
#else
#define DEVLOG(str) (0)
#define DEVLOG_FUN 
#endif // _DEBUG
#endif // DEVLOG