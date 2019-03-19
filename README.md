# X1nput

X1nput is Xinput rewritten using the [Windows.Gaming.Input API](https://docs.microsoft.com/en-us/uwp/api/windows.gaming.input) which has better support for Xbox One controllers, including impulse triggers.

There's no way for the game to know whether the controller supports impulse triggers using Xinput API, so this DLL just converts normal vibrations to trigger vibrations.

**Windows.Gaming.Input API requires Windows 10 to work.**

I'll try to improve the code and add a way to customize strength of the vibrations, but I'm hoping someone could learn from this code and write do something useful with it.

### Installation

1. Copy xinput1_3.dll from folder 32-bit (or 64-bit depending on the game) into the folder with game executable.

2. You may need to duplicate the file multiple times and rename each one to:  
	- xinput1_1.dll  
	- xinput1_2.dll  
	- xinput1_3.dll  
	- xinput1_4.dll  
	- xinput9_1_0.dll
																				 
3. If that doesn't work, try using the 64-bit DLL.

If the DLL causes the game to crash on startup, there's most likely no way to make it work with current version.

If you're unsure which DLLs does the game use, you can use [Process Explorer](https://docs.microsoft.com/en-us/sysinternals/downloads/process-explorer) from Sysinternals
* Press CTRL + D to view DLLs used by the selected application.
* Usually, if the application is using SYSWOW64, you should use the 32-bit DLLs.

### Configuration

1. Copy X1nput.ini into the same folder as the DLL.

2. To adjust vibration strength of motors and triggers, change strength for the corresponding side in X1nput.ini (e.g. LeftStrength=0.5).

3. To swap which side vibrates, change SwapSides to true (SwapSides=True).

4. To reload configuration in-game without having to restart, press both shoulder buttons and the start (menu) button.

### Buidling

1. Open X1nput.sln using Visual Studio 2015 or higher.
2. If you want to build a 32-bit version of the DLL, change the solution platform to X86 (Default is x64).

This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct
FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com)
with any additional questions or comments.
