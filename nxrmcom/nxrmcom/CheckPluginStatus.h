#pragma once
#include "plugingtypes.h"

bool IsWindows64Bit();

PLUGIN_STATUS CheckPluginStatus(HKEY hKeyAddins);

PLUGIN_STATUS CheckPluginStatus2(HKEY hRootKey, const wchar_t* wszAppType, const wchar_t* wszPlatform);

bool IsPluginWell(const wchar_t* wszAppType, const wchar_t* wszPlatform);