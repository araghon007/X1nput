// Code by the one and only, r57zone https://github.com/r57zone/XInputInjectDLL

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define NOMINMAX
#include <Windows.h>
#include "MinHook.h"
#include <tchar.h>
#include <cmath>
#include <algorithm>

#include "hidapi.h"

#define XINPUT_GAMEPAD_DPAD_UP          0x0001
#define XINPUT_GAMEPAD_DPAD_DOWN        0x0002
#define XINPUT_GAMEPAD_DPAD_LEFT        0x0004
#define XINPUT_GAMEPAD_DPAD_RIGHT       0x0008
#define XINPUT_GAMEPAD_START            0x0010
#define XINPUT_GAMEPAD_BACK             0x0020
#define XINPUT_GAMEPAD_LEFT_THUMB       0x0040
#define XINPUT_GAMEPAD_RIGHT_THUMB      0x0080
#define XINPUT_GAMEPAD_LEFT_SHOULDER    0x0100
#define XINPUT_GAMEPAD_RIGHT_SHOULDER   0x0200
#define XINPUT_GAMEPAD_A                0x1000
#define XINPUT_GAMEPAD_B                0x2000
#define XINPUT_GAMEPAD_X                0x4000
#define XINPUT_GAMEPAD_Y				0x8000

#define BATTERY_TYPE_DISCONNECTED		0x00

#define XUSER_MAX_COUNT                 4
#define XUSER_INDEX_ANY					0x000000FF

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

void GetConfig() {
	VendorID = GetConfigInt(_T("Controller"), _T("VendorID"), 1118);
	ProductID = GetConfigInt(_T("Controller"), _T("ProductID"), 746);
	
	LTriggerStrength = GetConfigFloat(_T("Triggers"), _T("LeftStrength"), _T("1.0"));
	RTriggerStrength = GetConfigFloat(_T("Triggers"), _T("RightStrength"), _T("1.0"));

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

int res;
unsigned char buf[9];
hid_device* handle;
int i;

XINPUT_STATE currState[4];

int testT = 0;

//Own GetState
DWORD WINAPI detourXInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{
	// first call the original function
	DWORD toReturn = hookedXInputGetState(dwUserIndex, pState);

	std::copy(pState, pState + 4, currState);

	return toReturn;
}

//Own SetState
DWORD WINAPI detourXInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{
	if(handle == NULL)
		handle = hid_open(VendorID, ProductID, NULL);

	if (handle != NULL) {

		float LSpeed = pVibration->wLeftMotorSpeed / 65535.0f;
		float RSpeed = pVibration->wRightMotorSpeed / 65535.0f;

		float LInputModifier = LInputModifierBase > 1.0f ? (pow(LInputModifierBase, currState[dwUserIndex].Gamepad.bLeftTrigger / 255.0f) - 1.0f) / (LInputModifierBase - 1.0f) : 1.0f;
		float RInputModifier = RInputModifierBase > 1.0f ? (pow(RInputModifierBase, currState[dwUserIndex].Gamepad.bRightTrigger / 255.0f) - 1.0f) / (RInputModifierBase - 1.0f) : 1.0f;

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

		res = hid_write(handle, buf, 9);

		if (res == -1) {
			hid_close(handle);
			handle = hid_open(VendorID, ProductID, NULL);
			res = hid_write(handle, buf, 9);
		}
	}

	return ERROR_SUCCESS;
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
			//1_4
			if (MH_CreateHookApiEx(L"XINPUT1_4", "XInputSetState", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
				MH_CreateHookApiEx(L"XINPUT1_4", "XInputGetState", &detourXInputGetState, &hookedXInputGetState);
			//1_3
			if (hookedXInputSetState == nullptr)
				if(MH_CreateHookApiEx(L"XINPUT1_3", "XInputSetState", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
					MH_CreateHookApiEx(L"XINPUT1_3", "XInputGetState", &detourXInputGetState, &hookedXInputGetState);

			//1_2
			if (hookedXInputSetState == nullptr)
				if(MH_CreateHookApiEx(L"XINPUT_1_2", "XInputSetState", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
					MH_CreateHookApiEx(L"XINPUT_1_2", "XInputGetState", &detourXInputGetState, &hookedXInputGetState);
			//1_1
			if (hookedXInputSetState == nullptr)
				if(MH_CreateHookApiEx(L"XINPUT_1_1", "XInputSetState", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
					MH_CreateHookApiEx(L"XINPUT_1_1", "XInputGetState", &detourXInputGetState, &hookedXInputGetState);
			//1.0
			if (hookedXInputSetState == nullptr)
				if(MH_CreateHookApiEx(L"XINPUT9_1_0", "XInputSetStateEx", &detourXInputSetState, &hookedXInputSetState) == MH_OK)
					MH_CreateHookApiEx(L"XINPUT9_1_0", "XInputGetStateEx", &detourXInputGetState, &hookedXInputGetState);

			if (MH_EnableHook(MH_ALL_HOOKS) == MH_OK) {
				GetConfig();

				res = hid_init();

				handle = hid_open(VendorID, ProductID, NULL);
			}

			break;
		}

		case DLL_PROCESS_DETACH:
		{
			if(handle != NULL)
				hid_close(handle);
			res = hid_exit();
			MH_DisableHook(MH_ALL_HOOKS);
			MH_Uninitialize();
			break;
		}
	}
	return TRUE;
}