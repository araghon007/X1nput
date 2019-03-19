// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define NOMINMAX
// Windows Header Files
#include <windows.h>



// reference additional headers your program requires here

#include <tchar.h>
#include <assert.h>
#include <cstdint>
#include <iostream>
#include <roapi.h>
#include <wrl.h>
#include <algorithm>
#include <windows.gaming.input.h>
#pragma comment(lib, "runtimeobject.lib")