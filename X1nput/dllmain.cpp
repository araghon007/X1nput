/*
	Thanks to r57zone for his Xinput emulation library
	https://github.com/r57zone/XInput
*/

#include "stdafx.h"

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

#define XINPUT_CAPS_FFB_SUPPORTED       0x0001
#define XINPUT_CAPS_WIRELESS            0x0002
#define XINPUT_CAPS_PMD_SUPPORTED       0x0008
#define XINPUT_CAPS_NO_NAVIGATION       0x0010

//
// Flags for battery status level
//
#define BATTERY_TYPE_DISCONNECTED       0x00    // This device is not connected
#define BATTERY_TYPE_WIRED              0x01    // Wired device, no battery
#define BATTERY_TYPE_ALKALINE           0x02    // Alkaline battery source
#define BATTERY_TYPE_NIMH               0x03    // Nickel Metal Hydride battery source
#define BATTERY_TYPE_UNKNOWN            0xFF    // Cannot determine the battery type

// These are only valid for wireless, connected devices, with known battery types
// The amount of use time remaining depends on the type of device.
#define BATTERY_LEVEL_EMPTY             0x00
#define BATTERY_LEVEL_LOW               0x01
#define BATTERY_LEVEL_MEDIUM            0x02
#define BATTERY_LEVEL_FULL              0x03


#define XINPUT_DEVTYPE_GAMEPAD          0x01
#define XINPUT_DEVSUBTYPE_GAMEPAD       0x01

#define BATTERY_TYPE_DISCONNECTED		0x00

#define XUSER_MAX_COUNT                 4
#define XUSER_INDEX_ANY					0x000000FF

#define ERROR_DEVICE_NOT_CONNECTED		1167
#define ERROR_SUCCESS					0

#define LEFT_TRIGGER_STRENGTH			0.25f
#define RIGHT_TRIGGER_STRENGTH			0.25f

std::unique_ptr<DirectX::GamePad> m_gamePad;

bool initialized = false;

//
// Structures used by XInput APIs
//
typedef struct _XINPUT_GAMEPAD
{
	WORD                                wButtons;
	BYTE                                bLeftTrigger;
	BYTE                                bRightTrigger;
	SHORT                               sThumbLX;
	SHORT                               sThumbLY;
	SHORT                               sThumbRX;
	SHORT                               sThumbRY;
} XINPUT_GAMEPAD, *PXINPUT_GAMEPAD;

typedef struct _XINPUT_STATE
{
	DWORD                               dwPacketNumber;
	XINPUT_GAMEPAD                      Gamepad;
} XINPUT_STATE, *PXINPUT_STATE;

typedef struct _XINPUT_VIBRATION
{
	WORD                                wLeftMotorSpeed;
	WORD                                wRightMotorSpeed;
} XINPUT_VIBRATION, *PXINPUT_VIBRATION;

typedef struct _XINPUT_CAPABILITIES
{
	BYTE                                Type;
	BYTE                                SubType;
	WORD                                Flags;
	XINPUT_GAMEPAD                      Gamepad;
	XINPUT_VIBRATION                    Vibration;
} XINPUT_CAPABILITIES, *PXINPUT_CAPABILITIES;

typedef struct _XINPUT_BATTERY_INFORMATION
{
	BYTE BatteryType;
	BYTE BatteryLevel;
} XINPUT_BATTERY_INFORMATION, *PXINPUT_BATTERY_INFORMATION;

typedef struct _XINPUT_KEYSTROKE
{
	WORD    VirtualKey;
	WCHAR   Unicode;
	WORD    Flags;
	BYTE    UserIndex;
	BYTE    HidCode;
} XINPUT_KEYSTROKE, *PXINPUT_KEYSTROKE;

#define DLLEXPORT extern "C" __declspec(dllexport)

DLLEXPORT BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
	/*switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}*/
	return TRUE;
}

DLLEXPORT DWORD WINAPI XInputGetState(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE *pState)
{
	if (!initialized) {
		m_gamePad = std::make_unique<DirectX::GamePad>();
		initialized = true;
	}

	auto state = m_gamePad->GetState(dwUserIndex);

	if (state.connected) {

		DWORD keys = 0;

		pState->Gamepad.bRightTrigger = 0;
		pState->Gamepad.bLeftTrigger = 0;
		pState->Gamepad.sThumbLX = 0;
		pState->Gamepad.sThumbLY = 0;
		pState->Gamepad.sThumbRX = 0;
		pState->Gamepad.sThumbRY = 0;

		if (state.buttons.a) keys += XINPUT_GAMEPAD_A;
		if (state.buttons.x) keys += XINPUT_GAMEPAD_X; //E
		if (state.buttons.y) keys += XINPUT_GAMEPAD_Y;	//Q
		if (state.buttons.b) keys += XINPUT_GAMEPAD_B;	//CTRL

		pState->Gamepad.bLeftTrigger = state.triggers.left * 255;
		pState->Gamepad.bRightTrigger = state.triggers.right * 255;

		pState->Gamepad.sThumbLX = (state.thumbSticks.leftX >= 0) ? state.thumbSticks.leftX * 32767 : state.thumbSticks.leftX * 32768;
		pState->Gamepad.sThumbLY = (state.thumbSticks.leftY >= 0) ? state.thumbSticks.leftY * 32767 : state.thumbSticks.leftY * 32768;
		pState->Gamepad.sThumbRX = (state.thumbSticks.rightX >= 0) ? state.thumbSticks.rightX * 32767 : state.thumbSticks.rightX * 32768;
		pState->Gamepad.sThumbRY = (state.thumbSticks.rightY >= 0) ? state.thumbSticks.rightY * 32767 : state.thumbSticks.rightY * 32768;

		if (state.buttons.rightStick) keys += XINPUT_GAMEPAD_RIGHT_THUMB;
		if (state.buttons.leftStick) keys += XINPUT_GAMEPAD_LEFT_THUMB;
		if (state.buttons.leftShoulder) keys += XINPUT_GAMEPAD_LEFT_SHOULDER;
		if (state.buttons.rightShoulder) keys += XINPUT_GAMEPAD_RIGHT_SHOULDER;

		if (state.buttons.back) keys += XINPUT_GAMEPAD_BACK;
		if (state.buttons.start) keys += XINPUT_GAMEPAD_START;

		if (state.dpad.up) keys += XINPUT_GAMEPAD_DPAD_UP;
		if (state.dpad.down) keys += XINPUT_GAMEPAD_DPAD_DOWN;
		if (state.dpad.left) keys += XINPUT_GAMEPAD_DPAD_LEFT;
		if (state.dpad.right) keys += XINPUT_GAMEPAD_DPAD_RIGHT;

		pState->dwPacketNumber = state.packet;
		pState->Gamepad.wButtons = keys;

		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
		
}

DLLEXPORT DWORD WINAPI XInputSetState(_In_ DWORD dwUserIndex, _In_ XINPUT_VIBRATION *pVibration) 
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	
	if (state.connected) {

		m_gamePad->SetVibration(dwUserIndex, pVibration->wLeftMotorSpeed / 65535.0f, pVibration->wRightMotorSpeed / 65535.0f, pVibration->wLeftMotorSpeed / 65535.0f * LEFT_TRIGGER_STRENGTH, pVibration->wRightMotorSpeed / 65535.0f * RIGHT_TRIGGER_STRENGTH);

		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}


DLLEXPORT DWORD WINAPI XInputGetCapabilities(_In_ DWORD dwUserIndex, _In_ DWORD dwFlags, _Out_ XINPUT_CAPABILITIES *pCapabilities) 
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);

	if (state.connected) {
		
		pCapabilities->Type = XINPUT_DEVTYPE_GAMEPAD;

		pCapabilities->SubType = XINPUT_DEVSUBTYPE_GAMEPAD;
				
		pCapabilities->Flags += XINPUT_CAPS_FFB_SUPPORTED;
		
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT void WINAPI XInputEnable(_In_ BOOL enable)
{
}

DLLEXPORT DWORD WINAPI XInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	if (state.connected) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD WINAPI XInputGetBatteryInformation(_In_ DWORD dwUserIndex, _In_ BYTE devType, _Out_ XINPUT_BATTERY_INFORMATION *pBatteryInformation)
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	if (state.connected) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	if (state.connected) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD WINAPI XInputGetStateEx(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE *pState)
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	if (state.connected) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD WINAPI XInputWaitForGuideButton(_In_ DWORD dwUserIndex, _In_ DWORD dwFlag, _In_ LPVOID pVoid)
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	if (state.connected) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD XInputCancelGuideButtonWait(_In_ DWORD dwUserIndex)
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	if (state.connected) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD XInputPowerOffController(_In_ DWORD dwUserIndex)
{
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	if (state.connected) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}
