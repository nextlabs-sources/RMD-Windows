#ifndef PCH_H
#define PCH_H

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>
#include <TlHelp32.h>


// atl & wtl
#include <atlbase.h>
#include <atlbase.h>
#include <atltypes.h>
#include <atlstr.h>
#include ".\wtl\Include\atlapp.h"
#include ".\wtl\Include\atlcrack.h"

#include <gdiplus.h>   // drawing watermark string must using it


// std
#include <string>
#include <vector>
#include <queue>
#include <sstream>
#include <map>
#include <algorithm>
#include <functional>
#include <cctype>
#include <cmath>  // using sin cos
#include <mutex>


#endif //PCH_H
