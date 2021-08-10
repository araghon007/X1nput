# X1nput

X1nput is a Xinput hook that emulates Impulse Triggers on the Xbox One controller by sending HID requests to the controller.

There's no way for the game to know whether the controller supports impulse triggers using Xinput API, so this DLL just converts normal vibrations to trigger vibrations.

### Usage

1. Launch X1nputConfigurator.exe

![image](https://user-images.githubusercontent.com/10870921/124172515-3e553880-daaa-11eb-98d9-47b43e082a7f.png)

2. Press Refresh. This may take a short while.

![image](https://user-images.githubusercontent.com/10870921/124172613-5462f900-daaa-11eb-9fcc-f357f7255a1b.png)
									 
3. From the list, select the name of the game you want to use X1nput in and press Inject
![image](https://user-images.githubusercontent.com/10870921/124172887-a86ddd80-daaa-11eb-8e35-47ceedbbd0fe.png)

4. Enjoy trigger rumble

5. You can unload X1nput from a game at any time by selecting the game and pressing Unload
![image](https://user-images.githubusercontent.com/10870921/124177316-592aab80-dab0-11eb-9e4b-ce9bd0426fec.png)

### Manual Controller Setup

1. Press the Controller Setup menu option

![image](https://user-images.githubusercontent.com/10870921/124173670-a35d5e00-daab-11eb-824f-0d326b625702.png)

2. Untick the Automatic checkbox, select a controller from the list and press Test

![image](https://user-images.githubusercontent.com/10870921/124173745-bff99600-daab-11eb-80c7-7643541c4f25.png)

3. If your controller rumbles, press Use, otherwise try other controllers from the list

![image](https://user-images.githubusercontent.com/10870921/124174160-4dd58100-daac-11eb-95bc-e32b79975e04.png)

4. If you want to use multiple controllers, tick the Multi-Controller Support

![image](https://user-images.githubusercontent.com/10870921/124174372-93924980-daac-11eb-9836-66d89d86f5c3.png)

4a. Select a controller and press Test, to see if it's the one you're using

![image](https://user-images.githubusercontent.com/10870921/124174754-07cced00-daad-11eb-85e1-51fbf0af3532.png)

4b. Press Use 1-4 depending on which user index your controller is assigned. It should be the same as the player number in your selected game.

![image](https://user-images.githubusercontent.com/10870921/124174913-35199b00-daad-11eb-818f-c2f48305284c.png)

5. Close the window and if X1nput is already injected in a game, select it in the main window and press Refresh

![image](https://user-images.githubusercontent.com/10870921/124175062-66926680-daad-11eb-8b03-b2f7d5620178.png)


### Configuration

There are 2 ways to configure X1nput. Through X1nput Configurator, or manually using notepad. To configure X1nput through X1nput Configurator, do the following:

1. Press the Configure menu option.

![image](https://user-images.githubusercontent.com/10870921/124173225-0ac6de00-daab-11eb-869c-448ff2db76b7.png)

2. Here you can see a whole lot of variables which I won't explain here, but you can hover your mouse cursor over them to find out more.

3. Mess around, have fun and hit Save.

4. If X1nput is already injected in a game, select it and press Refresh

To manually modify the configuration file, find the X1nput.ini file and edit it with your text editor of choice. You can then manually move X1nput.ini to the folder of the game's executable. This can even allow for per-game configuration, as long as you disable Override Config in X1nput Configurator.

### Troubleshooting

- The application doesn't start - Make sure you have .NET Framework 4.8 Runtime installed and that your Windows 10 installation is up-to-date
- I can't see a game in the list - Try running X1nputConfigurator as administrator. Alternatively you can use any injector, as long as you copy X1nput.ini to the game's folder.
- I can't feel trigger rumble on my controller - Try pressing the triggers while your controller is vibrating. If that does work, refer to Configuration. Otherwise press Refresh and make sure the game is under Injected. If it is, try Manual Controller Setup.
- My controller doesn't vibrate at all after injecting - Try using a different connection method, make sure you're using the right drivers or try updating your Windows 10
- X1nput doesn't get injected - Try using a different injector

Report any unexpected behavior by opening an Issue


### Buidling

1. Open X1nput.sln using Visual Studio 2019 or higher
2. Build the solution using AnyCPU, 32 and 64-bit X1nput DLLs should be automatically copied to the output folder

### Why the switch
Well, I knew from the start that the way X1nput worked wouldn't really be sustainable, so after seeing people mess around with DualSense triggers by messing around with USB requests, I decided to do a bit of my own reverse engineering. Took me a long time and several rabbit holes to get where I am, and the reason I am releasing it in an unfinished state is so that a) I wouldn't lose interest again and let the project die (twice) and b) so that willing people could help me out with actually making this thing work. I was thinking about just making this another Xinput dll replacement, but after coming across r57zone's Xinput hook, that seemed like a way neater option. I'm also hoping that removing the need for all the WinRT stuff, games would no longer refuse to work, and this method might even work on older operating systems (which fun fact, you could actually use Windows.Gaming.Input on Windows 7, but it required copying some WinMD files)

### Drawbacks
Xbox One controller drivers use a proprietary form of communication called GIP (Gaming Input Protocol, because Microsoft is all about Gaming) and I thank my lucky stars that Impulse Triggers still work even through the HID protocol.

Sadly, one of the things Microsoft's drivers do is reset vibration every time any app loses focus, so for example whenever you alt tab. This isn't really that bad if you stay in-game, but it's something to keep in mind.

Oh, one very important thing, since this is hooking application code, you should steer clear of any games with anti-cheat.

### Todo
- Do more testing

### Stuff used in this project
r57zone's XInputInjectDLL https://github.com/r57zone/XInputInjectDLL

nefarius XinputHooker https://github.com/nefarius/XInputHooker

libusb hidapi https://github.com/libusb/hidapi/

TasadaKageyu's minhook https://github.com/TsudaKageyu/minhook

scena's PeNet https://github.com/secana/PeNet

and a bunch more tidbits you can see throughout the code

### Honorable mentions
CookiePLMonster for helping me out with the original version of X1nput

lindquest for adding pressure-dependent trigger vibration

r57zone for making so many useful projects, you should check out his OpenVR repositories

nefarius for the whole XInputHooker, I wouldn't be able to figure out the DetourDeviceIoControl thing otherwise (probably)

Everyone who was willing to put up with old X1nput and even report issues and whatnot

That one person who tested old X1nput with Cyberpunk 2077 (I thought from the issues page that the old DLL no longer worked)

And thanks to Effieee and others who added X1nput to the PCGamingWiki
