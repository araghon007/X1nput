// Code by the one and only, r57zone https://github.com/r57zone/XInputInjectDLL

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define NOMINMAX
#include <Windows.h>
#include "MinHook.h"
#include <hidsdi.h>
#include <tchar.h>
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
	RIGHT, // High frequency rumble
	LEFT, // Low frequency rumble
	BOTH, // Sometimes games don't use high frequency rumble
	AUTO, // Detect both motors and choose higher speed
};

TriggerMotorLink RTriggerLink = BOTH;
TriggerMotorLink LTriggerLink = BOTH;

float ApplyTriggerMotorStrength(TriggerMotorLink link, float leftSpeed, float rightSpeed, float strength) {
	switch (link)
	{
		case RIGHT:
			return rightSpeed * strength;
			break;

		case LEFT:
			return leftSpeed * strength;
			break;

		case AUTO:
			return std::max(rightSpeed, leftSpeed) * strength;
			break;
	
		default:
			return (leftSpeed + rightSpeed) * strength / 2.0f;
			break;
	}
}

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
	LTriggerStrength = GetConfigFloat(_T("Triggers"), _T("LeftStrength"), _T("1.0"));
	RTriggerStrength = GetConfigFloat(_T("Triggers"), _T("RightStrength"), _T("1.0"));

	LTriggerLink = static_cast<TriggerMotorLink>(GetConfigInt(_T("Triggers"), _T("LeftTriggerLink"), 2));
	RTriggerLink = static_cast<TriggerMotorLink>(GetConfigInt(_T("Triggers"), _T("RightTriggerLink"), 2));

	LInputModifierBase = GetConfigFloat(_T("Triggers"), _T("LeftInputModifierBase"), _T("0.0"));
	RInputModifierBase = GetConfigFloat(_T("Triggers"), _T("RightInputModifierBase"), _T("0.0"));

	LMotorStrength = GetConfigFloat(_T("Motors"), _T("LeftStrength"), _T("1.0"));
	RMotorStrength = GetConfigFloat(_T("Motors"), _T("RightStrength"), _T("1.0"));
	MotorSwap = GetConfigBool(_T("Motors"), _T("SwapSides"), _T("False"));
}
#pragma endregion

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
	float leftTrigger;
	float rightTrigger;
};

#define MAX_STR 255
wchar_t wstr[MAX_STR];
int res;
unsigned char buf[9];
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

	if (dwIoControlCode == 0x002aac08) // Steam driver set state
	{
		BYTE* charInBuf = static_cast<BYTE*>(lpInBuffer);

		int vibrationOffset = nInBufferSize - 7; // Have to do this because the bluetooth buffer is shorter.

		float LSpeed = charInBuf[vibrationOffset+2] / 255.0f;
		float RSpeed = charInBuf[vibrationOffset+3] / 255.0f;

		HANDLE identifier = (HANDLE)(charInBuf[0] + (charInBuf[1] << 8) + (charInBuf[2] << 16) + (charInBuf[3] << 24)); // The first 4 bytes mean something

		float LInputModifier = LInputModifierBase > 1.0f ? (pow(LInputModifierBase, inputMap[identifier].leftTrigger) - 1.0f) / (LInputModifierBase - 1.0f) : 1.0f;
		float RInputModifier = RInputModifierBase > 1.0f ? (pow(RInputModifierBase, inputMap[identifier].rightTrigger) - 1.0f) / (RInputModifierBase - 1.0f) : 1.0f;

		float finalLTriggerStrength = LInputModifier * LTriggerStrength;
		float finalRTriggerStrength = RInputModifier * RTriggerStrength;

		charInBuf[vibrationOffset] = ApplyTriggerMotorStrength(LTriggerLink, LSpeed, RSpeed, finalLTriggerStrength) * 255; // Left trigger vibration
		charInBuf[vibrationOffset+1] = ApplyTriggerMotorStrength(RTriggerLink, LSpeed, RSpeed, finalRTriggerStrength) * 255; // Right trigger vibration
		charInBuf[vibrationOffset+2] = (MotorSwap ? RSpeed : LSpeed) * 255 * LMotorStrength; // Left rumble
		charInBuf[vibrationOffset+3] = (MotorSwap ? LSpeed : RSpeed) * 255 * RMotorStrength; // Right rumble

		// Yes, the Steam extended Xbox controller driver does natively support impulse triggers, they just chose not to do what I'm doing.
		retval = real_DeviceIoControl(
			hDevice,
			dwIoControlCode,
			charInBuf,
			nInBufferSize,
			lpOutBuffer,
			nOutBufferSize,
			lpBytesReturned,
			lpOverlapped
		);
	}
	else if (dwIoControlCode == 0x8000a010) // Microsoft driver set state
	{
		HidD_GetProductString(hDevice, wstr, MAX_STR);

		if (wcsstr(wstr, L"360")) { // Don't want to leave the poor old 360 controllers without vibration. Most likely need some better detection but I have nothing else to test this with.
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

			float LInputModifier = LInputModifierBase > 1.0f ? (pow(LInputModifierBase, inputMap[hDevice].leftTrigger) - 1.0f) / (LInputModifierBase - 1.0f) : 1.0f;
			float RInputModifier = RInputModifierBase > 1.0f ? (pow(RInputModifierBase, inputMap[hDevice].rightTrigger) - 1.0f) / (RInputModifierBase - 1.0f) : 1.0f;

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
		if (dwIoControlCode == 0x002aec04 && lpOutBuffer && nOutBufferSize > 0) // Steam driver get state
		{
			BYTE* charOutBuf = static_cast<BYTE*>(lpOutBuffer);

			HANDLE identifier = (HANDLE)(charOutBuf[0] + (charOutBuf[1] << 8) + (charOutBuf[2] << 16) + (charOutBuf[3] << 24)); // The first 4 bytes mean something

			int triggerOffset = 19;

			if (charOutBuf[20] > 3 || charOutBuf[22] > 3) // Man I dunno, the bluetooth stuff is weird
				triggerOffset = 22;

			inputMap[identifier].leftTrigger = (charOutBuf[triggerOffset] + (charOutBuf[triggerOffset+1] << 8)) / 1023.0f;
			inputMap[identifier].rightTrigger = (charOutBuf[triggerOffset+2] + (charOutBuf[triggerOffset+3] << 8)) / 1023.0f;
		}
		else if (dwIoControlCode == 0x8000e00c && lpOutBuffer && nOutBufferSize > 0) // Microsoft driver get state
		{
			BYTE* charOutBuf = static_cast<BYTE*>(lpOutBuffer);

			inputMap[hDevice].leftTrigger = charOutBuf[13] / 255.0f;
			inputMap[hDevice].rightTrigger = charOutBuf[14] / 255.0f;
		}
	}

	return retval;
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

			MH_CreateHookEx(DeviceIoControl, DetourDeviceIoControl, &real_DeviceIoControl);

			if (MH_EnableHook(MH_ALL_HOOKS) != MH_OK) {
				return FALSE;
			}

			break;
		}

		case DLL_PROCESS_DETACH:
		{
			MH_DisableHook(MH_ALL_HOOKS);
			MH_Uninitialize();
			break;
		}
	}
	return TRUE;
}