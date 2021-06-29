// Code by the one and only, r57zone https://github.com/r57zone/XInputInjectDLL

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define NOMINMAX
#include <Windows.h>
#include "MinHook.h"
#include <tchar.h>
#include <cmath>
#include <algorithm>

#include "hidapi.h"
#include <hidsdi.h>
#include <map>

#define CONFIG_PATH						_T(".\\X1nput.ini")

// Implementing changes from lindquest to add pressure-dependent vibration, among other things
float RTriggerStrength = 1.0f;
float LTriggerStrength = 1.0f;
float RMotorStrength = 1.0f;
float LMotorStrength = 1.0f;
float RInputModifierBase = 100.0f;
float LInputModifierBase = 100.0f;

bool MotorSwap = false;

enum TriggerMotorLink
{
	RIGHT,
	LEFT,
	BOTH
};

TriggerMotorLink RTriggerLink = RIGHT;
TriggerMotorLink LTriggerLink = RIGHT;

float ApplyTriggerMotorStrength(TriggerMotorLink link, float leftSpeed, float rightSpeed, float strength) {
	switch (link)
	{
	case RIGHT:
		return rightSpeed * strength;
		break;

	case LEFT:
		return leftSpeed * strength;
		break;

	default:
		return (leftSpeed + rightSpeed) * strength / 2.0f;
		break;
	}
}

int VendorID = 1118; // Microsoft
int ProductID = 746; // Default Xbox One Wireless Controller Product ID

bool Auto = true;

bool MultiController = false; // Multi-controller support
char One[256]; // First controller
char Two[256]; // Second controller
char Three[256]; // Third controller
char Four[256]; // Fourth controller

// Config related methods, thanks to xiaohe521, https://www.codeproject.com/Articles/10809/A-Small-Class-to-Read-INI-File
#pragma region Config loading
float GetConfigFloat(LPCTSTR AppName, LPCTSTR KeyName, LPCTSTR Default) {
	TCHAR result[256];
	GetPrivateProfileString(AppName, KeyName, Default, result, 256, CONFIG_PATH);
	return _tstof(result);
}

int GetConfigInt(LPCTSTR AppName, LPCTSTR KeyName, int Default)
{
	return GetPrivateProfileInt(AppName, KeyName, Default, CONFIG_PATH);
}

bool GetConfigBool(LPCTSTR AppName, LPCTSTR KeyName, LPCTSTR Default) {
	TCHAR result[256];
	GetPrivateProfileString(AppName, KeyName, Default, result, 256, CONFIG_PATH);
	// Thanks to CookiePLMonster for recommending _tcsicmp to me
	return _tcsicmp(result, _T("true")) == 0 ? true : false;
}

char* GetConfigString(LPCTSTR AppName, LPCTSTR KeyName, LPCTSTR Default) {
	TCHAR result[256];
	char res[256];
	GetPrivateProfileString(AppName, KeyName, Default, result, 256, CONFIG_PATH);
	wcstombs(res, result, wcslen(result) + 1);
	return res;
}

void GetConfig() {
	VendorID = GetConfigInt(_T("Controller"), _T("VendorID"), 1118);
	ProductID = GetConfigInt(_T("Controller"), _T("ProductID"), 746);
	
	LTriggerStrength = GetConfigFloat(_T("Triggers"), _T("LeftStrength"), _T("1.0"));
	RTriggerStrength = GetConfigFloat(_T("Triggers"), _T("RightStrength"), _T("1.0"));

	Auto = GetConfigBool(_T("Controllers"), _T("Auto"), _T("True"));

	MultiController = GetConfigBool(_T("Controllers"), _T("Enabled"), _T("False"));
	strcpy(One, GetConfigString(_T("Controllers"), _T("One"), NULL));
	strcpy(Two, GetConfigString(_T("Controllers"), _T("Two"), NULL));
	strcpy(Three, GetConfigString(_T("Controllers"), _T("Three"), NULL));
	strcpy(Four, GetConfigString(_T("Controllers"), _T("Four"), NULL));

	LTriggerLink = static_cast<TriggerMotorLink>(GetConfigInt(_T("Triggers"), _T("LeftTriggerLink"), 0));
	RTriggerLink = static_cast<TriggerMotorLink>(GetConfigInt(_T("Triggers"), _T("RightTriggerLink"), 0));

	LInputModifierBase = GetConfigFloat(_T("Triggers"), _T("LeftInputModifierBase"), _T("0.0"));
	RInputModifierBase = GetConfigFloat(_T("Triggers"), _T("RightInputModifierBase"), _T("0.0"));

	LMotorStrength = GetConfigFloat(_T("Motors"), _T("LeftStrength"), _T("1.0"));
	RMotorStrength = GetConfigFloat(_T("Motors"), _T("RightStrength"), _T("1.0"));
	MotorSwap = GetConfigBool(_T("Motors"), _T("SwapSides"), _T("False"));
}
#pragma endregion

typedef struct _XINPUT_GAMEPAD
{
	WORD                                wButtons;
	BYTE                                bLeftTrigger;
	BYTE                                bRightTrigger;
	SHORT                               sThumbLX;
	SHORT                               sThumbLY;
	SHORT                               sThumbRX;
	SHORT                               sThumbRY;
} XINPUT_GAMEPAD, * PXINPUT_GAMEPAD;

typedef struct _XINPUT_STATE
{
	DWORD                               dwPacketNumber;
	XINPUT_GAMEPAD                      Gamepad;
} XINPUT_STATE, * PXINPUT_STATE;

typedef struct _XINPUT_VIBRATION
{
	WORD                                wLeftMotorSpeed;
	WORD                                wRightMotorSpeed;
} XINPUT_VIBRATION, * PXINPUT_VIBRATION;


typedef DWORD(WINAPI* XINPUTSETSTATE)(DWORD, XINPUT_VIBRATION*);
typedef DWORD(WINAPI* XINPUTGETSTATE)(DWORD, XINPUT_STATE*);

// Pointer for calling original
static XINPUTSETSTATE hookedXInputSetState = nullptr;
static XINPUTGETSTATE hookedXInputGetState = nullptr;


static decltype(DeviceIoControl)* real_DeviceIoControl = DeviceIoControl;

// wrapper for easier setting up hooks for MinHook
template <typename T>
inline MH_STATUS MH_CreateHookEx(LPVOID pTarget, LPVOID pDetour, T** ppOriginal)
{
	return MH_CreateHook(pTarget, pDetour, reinterpret_cast<LPVOID*>(ppOriginal));
}

template <typename T>
inline MH_STATUS MH_CreateHookApiEx(LPCWSTR pszModule, LPCSTR pszProcName, LPVOID pDetour, T** ppOriginal)
{
	return MH_CreateHookApi(pszModule, pszProcName, pDetour, reinterpret_cast<LPVOID*>(ppOriginal));
}

struct input {
	BYTE leftTrigger;
	BYTE rightTrigger;
};

#define MAX_STR 255
wchar_t wstr[MAX_STR];
int res;
unsigned char buf[9];
hid_device* handle;
hid_device* handle1;
hid_device* handle2;
hid_device* handle3;
hid_device* handle4;
std::map<HANDLE, input> inputMap;

//
// Why didn't I try this earlier
// Hooks DeviceIoControl() API
// Thanks to nefarius https://github.com/nefarius/XInputHooker
// 
BOOL WINAPI DetourDeviceIoControl(
	HANDLE hDevice,
	DWORD dwIoControlCode,
	LPVOID lpInBuffer,
	DWORD nInBufferSize,
	LPVOID lpOutBuffer,
	DWORD nOutBufferSize,
	LPDWORD lpBytesReturned,
	LPOVERLAPPED lpOverlapped
)
{
	auto retval = 1;

	if (dwIoControlCode == 0x8000a010)
	{
		HidD_GetProductString(hDevice, wstr, MAX_STR);

		if (wcsstr(wstr, L"360")) { // Don't want to leave the poor old 360 controllers without vibration
			retval = real_DeviceIoControl(
				hDevice,
				dwIoControlCode,
				lpInBuffer,
				nInBufferSize,
				lpOutBuffer,
				nOutBufferSize,
				lpBytesReturned,
				lpOverlapped
			);
		}
		else {
			BYTE* charInBuf = static_cast<BYTE*>(lpInBuffer);
			float LSpeed = charInBuf[2] / 255.0f;
			float RSpeed = charInBuf[3] / 255.0f;

			float LInputModifier = LInputModifierBase > 1.0f ? (pow(LInputModifierBase, inputMap[hDevice].leftTrigger / 255.0f) - 1.0f) / (LInputModifierBase - 1.0f) : 1.0f;
			float RInputModifier = RInputModifierBase > 1.0f ? (pow(RInputModifierBase, inputMap[hDevice].rightTrigger / 255.0f) - 1.0f) / (RInputModifierBase - 1.0f) : 1.0f;

			float finalLTriggerStrength = LInputModifier * LTriggerStrength;
			float finalRTriggerStrength = RInputModifier * RTriggerStrength;

			buf[0] = 0x03; // HID report ID (3 for bluetooth, any for USB)
			buf[1] = 0x0F; // Motor flag mask(?)
			buf[2] = ApplyTriggerMotorStrength(LTriggerLink, LSpeed, RSpeed, finalLTriggerStrength) * 255; // Left trigger
			buf[3] = ApplyTriggerMotorStrength(RTriggerLink, LSpeed, RSpeed, finalRTriggerStrength) * 255; // Right trigger
			buf[4] = (MotorSwap ? RSpeed : LSpeed) * 255 * LMotorStrength; // Left rumble
			buf[5] = (MotorSwap ? LSpeed : RSpeed) * 255 * RMotorStrength; // Right rumble
			// "Pulse"
			buf[6] = 0xFF; // On time
			buf[7] = 0x00; // Off time 
			buf[8] = 0xFF; // Number of repeats
			WriteFile(hDevice, buf, 9, lpBytesReturned, lpOverlapped);
		}
	}
	else {
		retval = real_DeviceIoControl(
			hDevice,
			dwIoControlCode,
			lpInBuffer,
			nInBufferSize,
			lpOutBuffer,
			nOutBufferSize,
			lpBytesReturned,
			lpOverlapped
		);
		if (dwIoControlCode == 0x8000e00c && lpOutBuffer && nOutBufferSize > 0)
		{
			BYTE* charIOutBuf = static_cast<BYTE*>(lpOutBuffer);

			inputMap[hDevice].leftTrigger = charIOutBuf[13];
			inputMap[hDevice].rightTrigger = charIOutBuf[14];
		}
	}


	return retval;
}

//Own GetState
DWORD WINAPI detourXInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{
	return hookedXInputGetState(dwUserIndex, pState);
}

//Own SetState
DWORD WINAPI detourXInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{
	XINPUT_STATE pState;

	DWORD toReturn = hookedXInputGetState(dwUserIndex, &pState);

	if (toReturn == ERROR_SUCCESS) {
		if (MultiController) {
			if (handle1 == NULL && One[0] != '\0')
				handle1 = hid_open_path(One);
			if (handle2 == NULL && Two[0] != '\0')
				handle2 = hid_open_path(Two);
			if (handle3 == NULL && Three[0] != '\0')
				handle3 = hid_open_path(Three);
			if (handle4 == NULL && Four[0] != '\0')
				handle4 = hid_open_path(Four);
		}
		else {
			if (handle == NULL) {
				handle = hid_open(VendorID, ProductID, NULL);
			}
		}

		hid_device* currHandle = NULL;

		if (MultiController) {
			switch (dwUserIndex) {
				case 0:
					currHandle = handle1;
					break;
				case 1:
					currHandle = handle2;
					break;
				case 2:
					currHandle = handle3;
					break;
				case 3:
					currHandle = handle4;
					break;
			}
		}
		else {
			currHandle = handle;
		}

		if (currHandle != NULL) {

			float LSpeed = pVibration->wLeftMotorSpeed / 65535.0f;
			float RSpeed = pVibration->wRightMotorSpeed / 65535.0f;

			float LInputModifier = LInputModifierBase > 1.0f ? (pow(LInputModifierBase, pState.Gamepad.bLeftTrigger / 255.0f) - 1.0f) / (LInputModifierBase - 1.0f) : 1.0f;
			float RInputModifier = RInputModifierBase > 1.0f ? (pow(RInputModifierBase, pState.Gamepad.bRightTrigger / 255.0f) - 1.0f) / (RInputModifierBase - 1.0f) : 1.0f;

			float finalLTriggerStrength = LInputModifier * LTriggerStrength;
			float finalRTriggerStrength = RInputModifier * RTriggerStrength;

			buf[0] = 0x03; // HID report ID (3 for bluetooth, any for USB)
			buf[1] = 0x0F; // Motor flag mask(?)
			buf[2] = ApplyTriggerMotorStrength(LTriggerLink, LSpeed, RSpeed, finalLTriggerStrength) * 255; // Left trigger
			buf[3] = ApplyTriggerMotorStrength(RTriggerLink, LSpeed, RSpeed, finalRTriggerStrength) * 255; // Right trigger
			buf[4] = (MotorSwap ? RSpeed : LSpeed) * 255 * LMotorStrength; // Left rumble
			buf[5] = (MotorSwap ? LSpeed : RSpeed) * 255 * RMotorStrength; // Right rumble
			// "Pulse"
			buf[6] = 0xFF; // On time
			buf[7] = 0x00; // Off time 
			buf[8] = 0xFF; // Number of repeats

			res = hid_write(currHandle, buf, 9);

			if (res == -1) {
				hid_close(currHandle);

				if (MultiController) {
					switch (dwUserIndex) {
					case 0:
						currHandle = handle1 = hid_open_path(One);
						break;
					case 1:
						currHandle = handle2 = hid_open_path(Two);
						break;
					case 2:
						currHandle = handle3 = hid_open_path(Three);
						break;
					case 3:
						currHandle = handle4 = hid_open_path(Four);
						break;
					}
				}
				else {
					currHandle = handle = hid_open(VendorID, ProductID, NULL);
				}

				if (currHandle != NULL)
					res = hid_write(currHandle, buf, 9);
			}
		}
		else {
			toReturn = hookedXInputSetState(dwUserIndex, pVibration); // For example, if the controller is an Xbox 360 controller
		}
	}

	return toReturn;
}

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call) {
		case DLL_PROCESS_ATTACH:
		{
			MH_Initialize();

			GetConfig();

			if (Auto) {
				MH_CreateHookEx(DeviceIoControl, DetourDeviceIoControl, &real_DeviceIoControl);
			}
			else {
				//1_4
				if (MH_CreateHookApiEx(L"XINPUT1_4", "XInputSetState", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
					MH_CreateHookApiEx(L"XINPUT1_4", "XInputGetState", &detourXInputGetState, &hookedXInputGetState);
				//1_3
				if (hookedXInputSetState == nullptr)
					if (MH_CreateHookApiEx(L"XINPUT1_3", "XInputSetState", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
						MH_CreateHookApiEx(L"XINPUT1_3", "XInputGetState", &detourXInputGetState, &hookedXInputGetState);
				//1_2
				if (hookedXInputSetState == nullptr)
					if (MH_CreateHookApiEx(L"XINPUT_1_2", "XInputSetState", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
						MH_CreateHookApiEx(L"XINPUT_1_2", "XInputGetState", &detourXInputGetState, &hookedXInputGetState);
				//1_1
				if (hookedXInputSetState == nullptr)
					if (MH_CreateHookApiEx(L"XINPUT_1_1", "XInputSetState", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
						MH_CreateHookApiEx(L"XINPUT_1_1", "XInputGetState", &detourXInputGetState, &hookedXInputGetState);
				//1.0
				if (hookedXInputSetState == nullptr)
					if (MH_CreateHookApiEx(L"XINPUT9_1_0", "XInputSetStateEx", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
						MH_CreateHookApiEx(L"XINPUT9_1_0", "XInputGetStateEx", &detourXInputGetState, &hookedXInputGetState);
			}

			if (MH_EnableHook(MH_ALL_HOOKS) == MH_OK) {
				if (!Auto) {
					res = hid_init();
					if (MultiController) {
						if (One[0] != '\0')
							handle1 = hid_open_path(One);
						if (Two[0] != '\0')
							handle2 = hid_open_path(Two);
						if (Three[0] != '\0')
							handle3 = hid_open_path(Three);
						if (Four[0] != '\0')
							handle4 = hid_open_path(Four);
					}
					else {
						handle = hid_open(VendorID, ProductID, NULL);
					}
				}
			}

			break;
		}

		case DLL_PROCESS_DETACH:
		{
			if (!Auto) {
				if (MultiController) {
					if (handle1 != NULL)
						hid_close(handle1);
					if (handle2 != NULL)
						hid_close(handle2);
					if (handle3 != NULL)
						hid_close(handle3);
					if (handle4 != NULL)
						hid_close(handle4);
				}
				else {
					if (handle != NULL) {
						hid_close(handle);
					}
				}

				res = hid_exit();
			}
			MH_DisableHook(MH_ALL_HOOKS);
			MH_Uninitialize();
			break;
		}
	}
	return TRUE;
}