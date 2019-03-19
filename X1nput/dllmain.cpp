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
#define MAX_PLAYER_COUNT				8
#define XUSER_INDEX_ANY					0x000000FF

#define ERROR_DEVICE_NOT_CONNECTED		1167
#define ERROR_SUCCESS					0

#define CONFIG_PATH						_T(".\\X1nput.ini")


using namespace ABI::Windows::Foundation::Collections;
using namespace ABI::Windows::Gaming::Input;
using namespace Microsoft::WRL;
using namespace Microsoft::WRL::Wrappers;

const float c_XboxOneThumbDeadZone = .24f;  // Recommended Xbox One controller deadzone

ComPtr<IGamepadStatics> gamepadStatics;
ComPtr<IGamepad> gamepads[MAX_PLAYER_COUNT];
EventRegistrationToken mUserChangeToken[MAX_PLAYER_COUNT];

EventRegistrationToken gAddedToken;
EventRegistrationToken gRemovedToken;

int mMostRecentGamepad = 0;

HRESULT hr;

float RTriggerStrength = 0.25f;
float LTriggerStrength = 0.25f;
float RMotorStrength = 1.0f;
float LMotorStrength = 1.0f;

bool TriggerSwap = false;
bool MotorSwap = false;

// Config related methods, thanks to xiaohe521, https://www.codeproject.com/Articles/10809/A-Small-Class-to-Read-INI-File
#pragma region Config loading
float GetConfigFloat(LPCSTR AppName, LPCSTR KeyName, LPCSTR Default) {
	TCHAR result[256];
	GetPrivateProfileString(AppName, KeyName, Default, result, 256, CONFIG_PATH);
	return atof(result);
}

bool GetConfigBool(LPCSTR AppName, LPCSTR KeyName, LPCSTR Default) {
	TCHAR result[256];
	GetPrivateProfileString(AppName, KeyName, Default, result, 256, CONFIG_PATH);
	// Thanks to CookiePLMonster for recommending _tcsicmp to me
	return _tcsicmp(result, "true") == 0 ? true : false;
}

void GetConfig() {
	LTriggerStrength = GetConfigFloat(_T("Triggers"), _T("LeftStrength"), _T("0.25"));
	RTriggerStrength = GetConfigFloat(_T("Triggers"), _T("RightStrength"), _T("0.25"));
	TriggerSwap = GetConfigBool(_T("Triggers"), _T("SwapSides"), _T("False"));

	LMotorStrength = GetConfigFloat(_T("Motors"), _T("LeftStrength"), _T("1.0"));
	RMotorStrength = GetConfigFloat(_T("Motors"), _T("RightStrength"), _T("1.0"));
	MotorSwap = GetConfigBool(_T("Motors"), _T("SwapSides"), _T("False"));
}
#pragma endregion

// Gamepad scanning and gamepad related methods
#pragma region Stuff from GamePad.cpp

// DeadZone enum
enum DeadZone
{
	DEAD_ZONE_INDEPENDENT_AXES = 0,
	DEAD_ZONE_CIRCULAR,
	DEAD_ZONE_NONE,
};

float ApplyLinearDeadZone(float value, float maxValue, float deadZoneSize)
{
	if (value < -deadZoneSize)
	{
		// Increase negative values to remove the deadzone discontinuity.
		value += deadZoneSize;
	}
	else if (value > deadZoneSize)
	{
		// Decrease positive values to remove the deadzone discontinuity.
		value -= deadZoneSize;
	}
	else
	{
		// Values inside the deadzone come out zero.
		return 0;
	}

	// Scale into 0-1 range.
	float scaledValue = value / (maxValue - deadZoneSize);
	return std::max(-1.f, std::min(scaledValue, 1.f));
}

// Applies DeadZone to thumbstick positions
void ApplyStickDeadZone(float x, float y, DeadZone deadZoneMode, float maxValue, float deadZoneSize, _Out_ float& resultX, _Out_ float& resultY)
{
	switch (deadZoneMode)
	{
	case DEAD_ZONE_INDEPENDENT_AXES:
		resultX = ApplyLinearDeadZone(x, maxValue, deadZoneSize);
		resultY = ApplyLinearDeadZone(y, maxValue, deadZoneSize);
		break;

	case DEAD_ZONE_CIRCULAR:
	{
		float dist = sqrtf(x*x + y * y);
		float wanted = ApplyLinearDeadZone(dist, maxValue, deadZoneSize);

		float scale = (wanted > 0.f) ? (wanted / dist) : 0.f;

		resultX = std::max(-1.f, std::min(x * scale, 1.f));
		resultY = std::max(-1.f, std::min(y * scale, 1.f));
	}
	break;

	default: // GamePad::DEAD_ZONE_NONE
		resultX = ApplyLinearDeadZone(x, maxValue, 0);
		resultY = ApplyLinearDeadZone(y, maxValue, 0);
		break;
	}
}

// UserChanged Event
static HRESULT UserChanged(ABI::Windows::Gaming::Input::IGameController*, ABI::Windows::System::IUserChangedEventArgs*)
{
	return S_OK;
}

// Scans for gamepads (adds/removes gamepads from gamepads array)
void ScanGamePads()
{
	ComPtr<IVectorView<Gamepad*>> pads;
	hr = gamepadStatics->get_Gamepads(&pads);
	assert(SUCCEEDED(hr));

	unsigned int count = 0;
	hr = pads->get_Size(&count);
	assert(SUCCEEDED(hr));

	// Check for removed gamepads
	for (size_t j = 0; j < MAX_PLAYER_COUNT; ++j)
	{
		if (gamepads[j])
		{
			unsigned int k = 0;
			for (; k < count; ++k)
			{
				ComPtr<IGamepad> pad;
				HRESULT hr = pads->GetAt(k, pad.GetAddressOf());
				if (SUCCEEDED(hr) && (pad == gamepads[j]))
				{
					break;
				}
			}

			if (k >= count)
			{
				ComPtr<IGameController> ctrl;
				HRESULT hr = gamepads[j].As(&ctrl);
				if (SUCCEEDED(hr) && ctrl)
				{
					(void)ctrl->remove_UserChanged(mUserChangeToken[j]);
					mUserChangeToken[j].value = 0;
				}

				gamepads[j].Reset();
			}
		}
	}

	// Check for added gamepads
	for (unsigned int j = 0; j < count; ++j)
	{
		ComPtr<IGamepad> pad;
		hr = pads->GetAt(j, pad.GetAddressOf());
		if (SUCCEEDED(hr))
		{
			size_t empty = MAX_PLAYER_COUNT;
			size_t k = 0;
			for (; k < MAX_PLAYER_COUNT; ++k)
			{
				if (gamepads[k] == pad)
				{
					if (j == (count - 1))
						mMostRecentGamepad = static_cast<int>(k);
					break;
				}
				else if (!gamepads[k])
				{
					if (empty >= MAX_PLAYER_COUNT)
						empty = k;
				}
			}

			if (k >= MAX_PLAYER_COUNT)
			{
				// Silently ignore "extra" gamepads as there's no hard limit
				if (empty < MAX_PLAYER_COUNT)
				{
					gamepads[empty] = pad;
					if (j == (count - 1))
						mMostRecentGamepad = static_cast<int>(empty);

					ComPtr<IGameController> ctrl;
					hr = pad.As(&ctrl);
					if (SUCCEEDED(hr) && ctrl)
					{
						typedef __FITypedEventHandler_2_Windows__CGaming__CInput__CIGameController_Windows__CSystem__CUserChangedEventArgs UserHandler;
						hr = ctrl->add_UserChanged(Callback<UserHandler>(UserChanged).Get(), &mUserChangeToken[empty]);
						assert(SUCCEEDED(hr));
					}
				}
			}
		}
	}
}

// GamepadAdded Event
static HRESULT GamepadAdded(IInspectable *, ABI::Windows::Gaming::Input::IGamepad*)
{
	ScanGamePads();
	return S_OK;
}

// GamepadRemoved Event
static HRESULT GamepadRemoved(IInspectable *, ABI::Windows::Gaming::Input::IGamepad*)
{
	ScanGamePads();
	return S_OK;
}
#pragma endregion

/*
	Thanks to CookiePLMonster for suggesting this.
	I definitely should have asked how to implement it, but oh well, there's still a lot of time for fixing.
	Oddly enough, this seemed to have fixed HITMAN 2 once again. That game is really cursed. Before, only debug version of the DLL worked.
*/
#pragma region InitOnceExecuteOnce

// Global variable for one-time initialization structure
INIT_ONCE g_InitOnce = INIT_ONCE_STATIC_INIT; // Static initialization

// Initialization callback function 
BOOL CALLBACK InitHandleFunction(
	PINIT_ONCE InitOnce,
	PVOID Parameter,
	PVOID *lpContext);

// Returns a handle to an event object that is created only once
HANDLE InitializeGamepad()
{
	PVOID lpContext;
	BOOL  bStatus;

	// Execute the initialization callback function 
	bStatus = InitOnceExecuteOnce(&g_InitOnce,          // One-time initialization structure
		InitHandleFunction,   // Pointer to initialization callback function
		NULL,                 // Optional parameter to callback function (not used)
		&lpContext);          // Receives pointer to event object stored in g_InitOnce

// InitOnceExecuteOnce function succeeded. Return event object.
	if (bStatus)
	{
		return (HANDLE)lpContext;
	}
	else
	{
		return (INVALID_HANDLE_VALUE);
	}
}

// Initialization callback function that creates the event object 
BOOL CALLBACK InitHandleFunction(
	PINIT_ONCE InitOnce,        // Pointer to one-time initialization structure        
	PVOID Parameter,            // Optional parameter passed by InitOnceExecuteOnce            
	PVOID *lpContext)           // Receives pointer to event object           
{
	hr = RoInitialize(RO_INIT_MULTITHREADED);
	assert(SUCCEEDED(hr));

	hr = RoGetActivationFactory(HStringReference(L"Windows.Gaming.Input.Gamepad").Get(), __uuidof(IGamepadStatics), &gamepadStatics);
	assert(SUCCEEDED(hr));

	typedef __FIEventHandler_1_Windows__CGaming__CInput__CGamepad AddedHandler;
	hr = gamepadStatics->add_GamepadAdded(Callback<AddedHandler>(GamepadAdded).Get(), &gAddedToken);
	assert(SUCCEEDED(hr));

	typedef __FIEventHandler_1_Windows__CGaming__CInput__CGamepad RemovedHandler;
	hr = gamepadStatics->add_GamepadRemoved(Callback<RemovedHandler>(GamepadRemoved).Get(), &gRemovedToken);
	assert(SUCCEEDED(hr));

	GetConfig();

	ScanGamePads();

	return TRUE;
}

#pragma endregion

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

DLLEXPORT BOOL APIENTRY DllMain(HMODULE hModule,
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
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	auto gamepad = gamepads[dwUserIndex];

	GamepadReading state;
	hr = gamepad->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {

		DWORD keys = 0;

		float LeftThumbstickX;
		float LeftThumbstickY;
		float RightThumbstickX;
		float RightThumbstickY;

		ApplyStickDeadZone(state.LeftThumbstickX, state.LeftThumbstickY, DEAD_ZONE_INDEPENDENT_AXES, 1.f, c_XboxOneThumbDeadZone, LeftThumbstickX, LeftThumbstickY);

		ApplyStickDeadZone(state.RightThumbstickX, state.RightThumbstickY, DEAD_ZONE_INDEPENDENT_AXES, 1.f, c_XboxOneThumbDeadZone, RightThumbstickX, RightThumbstickY);

		pState->Gamepad.bRightTrigger = state.RightTrigger * 255;
		pState->Gamepad.bLeftTrigger = state.LeftTrigger * 255;
		pState->Gamepad.sThumbLX = (LeftThumbstickX >= 0) ? LeftThumbstickX * 32767 : LeftThumbstickX * 32768;
		pState->Gamepad.sThumbLY = (LeftThumbstickY >= 0) ? LeftThumbstickY * 32767 : LeftThumbstickY * 32768;
		pState->Gamepad.sThumbRX = (RightThumbstickX >= 0) ? RightThumbstickX * 32767 : RightThumbstickX * 32768;
		pState->Gamepad.sThumbRY = (RightThumbstickY >= 0) ? RightThumbstickY * 32767 : RightThumbstickY * 32768;

		if ((state.Buttons & GamepadButtons::GamepadButtons_A) != 0) keys += XINPUT_GAMEPAD_A;
		if ((state.Buttons & GamepadButtons::GamepadButtons_X) != 0) keys += XINPUT_GAMEPAD_X;
		if ((state.Buttons & GamepadButtons::GamepadButtons_Y) != 0) keys += XINPUT_GAMEPAD_Y;
		if ((state.Buttons & GamepadButtons::GamepadButtons_B) != 0) keys += XINPUT_GAMEPAD_B;

		if ((state.Buttons & GamepadButtons::GamepadButtons_RightThumbstick) != 0) keys += XINPUT_GAMEPAD_RIGHT_THUMB;
		if ((state.Buttons & GamepadButtons::GamepadButtons_LeftThumbstick) != 0) keys += XINPUT_GAMEPAD_LEFT_THUMB;
		if ((state.Buttons & GamepadButtons::GamepadButtons_RightShoulder) != 0) keys += XINPUT_GAMEPAD_RIGHT_SHOULDER;
		if ((state.Buttons & GamepadButtons::GamepadButtons_LeftShoulder) != 0) keys += XINPUT_GAMEPAD_LEFT_SHOULDER;

		if ((state.Buttons & GamepadButtons::GamepadButtons_View) != 0) keys += XINPUT_GAMEPAD_BACK;
		if ((state.Buttons & GamepadButtons::GamepadButtons_Menu) != 0) keys += XINPUT_GAMEPAD_START;

		if ((state.Buttons & GamepadButtons::GamepadButtons_DPadUp) != 0) keys += XINPUT_GAMEPAD_DPAD_UP;
		if ((state.Buttons & GamepadButtons::GamepadButtons_DPadDown) != 0) keys += XINPUT_GAMEPAD_DPAD_DOWN;
		if ((state.Buttons & GamepadButtons::GamepadButtons_DPadLeft) != 0) keys += XINPUT_GAMEPAD_DPAD_LEFT;
		if ((state.Buttons & GamepadButtons::GamepadButtons_DPadRight) != 0) keys += XINPUT_GAMEPAD_DPAD_RIGHT;

		// Press both shoulder buttons and the start button to reload configuration.
		if ((state.Buttons & GamepadButtons::GamepadButtons_RightShoulder) != 0 &&
			(state.Buttons & GamepadButtons::GamepadButtons_LeftShoulder) != 0 &&
			(state.Buttons & GamepadButtons::GamepadButtons_Menu) != 0) {
			GetConfig();
		}


		pState->dwPacketNumber = state.Timestamp;
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
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	auto gamepad = gamepads[dwUserIndex];

	GamepadReading state;
	hr = gamepad->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {

		GamepadVibration vibration;

		float LSpeed = pVibration->wLeftMotorSpeed / 65535.0f;
		float RSpeed = pVibration->wRightMotorSpeed / 65535.0f;

		vibration.LeftMotor = MotorSwap ? RSpeed * LMotorStrength : LSpeed * LMotorStrength;
		vibration.RightMotor = MotorSwap ? LSpeed * RMotorStrength : RSpeed * RMotorStrength;

		vibration.LeftTrigger = TriggerSwap ? RSpeed * LTriggerStrength : LSpeed * LTriggerStrength;
		vibration.RightTrigger = TriggerSwap ? LSpeed * RTriggerStrength : RSpeed * RTriggerStrength;

		gamepad->put_Vibration(vibration);

		return ERROR_SUCCESS;
	}

	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}


DLLEXPORT DWORD WINAPI XInputGetCapabilities(_In_ DWORD dwUserIndex, _In_ DWORD dwFlags, _Out_ XINPUT_CAPABILITIES *pCapabilities)
{
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	auto gamepad = gamepads[dwUserIndex];

	GamepadReading state;
	hr = gamepad->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {

		ComPtr<IGameController> gamepadInfo;
		gamepads[dwUserIndex].As(&gamepadInfo);

		boolean wireless;
		gamepadInfo->get_IsWireless(&wireless);

		pCapabilities->Type = XINPUT_DEVTYPE_GAMEPAD;

		pCapabilities->SubType = XINPUT_DEVSUBTYPE_GAMEPAD;

		pCapabilities->Flags += XINPUT_CAPS_FFB_SUPPORTED;

		if (wireless) pCapabilities->Flags += XINPUT_CAPS_WIRELESS;

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
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	auto gamepad = gamepads[dwUserIndex];

	GamepadReading state;
	hr = gamepad->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD WINAPI XInputGetBatteryInformation(_In_ DWORD dwUserIndex, _In_ BYTE devType, _Out_ XINPUT_BATTERY_INFORMATION *pBatteryInformation)
{
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	auto gamepad = gamepads[dwUserIndex];

	GamepadReading state;
	hr = gamepad->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
	/*

	ComPtr<IGameControllerBatteryInfo> battInf;
	gamepads[dwUserIndex].As(&battInf);

	ComPtr<IGameController> test;

	ComPtr<ABI::Windows::Devices::Power::IBatteryReport> battReport;
	battInf->TryGetBatteryReport(&battReport);

	//Can't find any information on IReference
	int Charge;
	battReport->get_RemainingCapacityInMilliwattHours(&Charge);

	*/
	/*
	InitializeGamepad();
	auto state = m_gamePad->GetCapabilities(dwUserIndex);
	if (state.connected) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
	*/
}

DLLEXPORT DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	GamepadReading state;
	hr = gamepads[dwUserIndex]->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD WINAPI XInputGetStateEx(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE *pState)
{
	return XInputGetState(dwUserIndex, pState);
}

DLLEXPORT DWORD WINAPI XInputWaitForGuideButton(_In_ DWORD dwUserIndex, _In_ DWORD dwFlag, _In_ LPVOID pVoid)
{
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	auto gamepad = gamepads[dwUserIndex];

	GamepadReading state;
	hr = gamepad->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD XInputCancelGuideButtonWait(_In_ DWORD dwUserIndex)
{
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	auto gamepad = gamepads[dwUserIndex];

	GamepadReading state;
	hr = gamepad->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}

DLLEXPORT DWORD XInputPowerOffController(_In_ DWORD dwUserIndex)
{
	InitializeGamepad();

	if (gamepads[dwUserIndex] == NULL) {
		return ERROR_DEVICE_NOT_CONNECTED;
	}

	auto gamepad = gamepads[dwUserIndex];

	GamepadReading state;
	hr = gamepad->GetCurrentReading(&state);

	if (SUCCEEDED(hr)) {
		return ERROR_SUCCESS;
	}
	else
	{
		return ERROR_DEVICE_NOT_CONNECTED;
	}
}
