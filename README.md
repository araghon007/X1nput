# X1nput

X1nput is a Xinput hook that emulates Impulse Triggers on the Xbox One controller by sending HID requests to the controller.

There's no way for the game to know whether the controller supports impulse triggers using Xinput API, so this DLL just converts normal vibrations to trigger vibrations.

### Usage

1. Find out whether the game you're trying to use this on is 32-bit or 64-bit (You can do this by using task manager)

![32bit](https://user-images.githubusercontent.com/10870921/123274117-83ee9000-d503-11eb-911e-212974019daa.png)  ![64bit](https://user-images.githubusercontent.com/10870921/123274157-8e108e80-d503-11eb-986a-f855678b8659.png)

2. If it's 32-bit, use X1nputConfigurator32.exe otherwise use X1nputConfigurator.exe
																				 
3. Once the application is open, make sure your controller is connected to your PC, either via a USB cable, or by using the wireless dongle

4. Select your controller under the Devices list. I'd suggest finding your controller's device instance path and comparing it with what you see in X1nput Configurator. You can do so by opening device manager, finding your controller under Human Interface Devices -> right clicking -> properties -> details and in the drop down menu, select Device instance path.

Wired

![image](https://user-images.githubusercontent.com/10870921/123274500-daf46500-d503-11eb-94ff-4c5c013361b7.png)
![image](https://user-images.githubusercontent.com/10870921/123274567-ecd60800-d503-11eb-8f70-6b2ef86baf3b.png)
![image](https://user-images.githubusercontent.com/10870921/123274609-f65f7000-d503-11eb-83f2-0ff59fccb639.png)
![image](https://user-images.githubusercontent.com/10870921/123275531-ae8d1880-d504-11eb-9f32-707579d2386a.png)
![image](https://user-images.githubusercontent.com/10870921/123275427-9ae1b200-d504-11eb-891d-bfae55e6e78a.png)
![image](https://user-images.githubusercontent.com/10870921/123275284-7a195c80-d504-11eb-98ea-4f811822b028.png)
![wired](https://user-images.githubusercontent.com/10870921/123274370-c0ba8700-d503-11eb-9157-6517b8780107.png)

Wireless

![image](https://user-images.githubusercontent.com/10870921/123274799-1f800080-d504-11eb-909d-da1d7ba46261.png)
![image](https://user-images.githubusercontent.com/10870921/123275651-cbc1e700-d504-11eb-894e-772a11a24a48.png)
![image](https://user-images.githubusercontent.com/10870921/123274733-11ca7b00-d504-11eb-9917-d6c73de317c1.png)

5. Once you have your controller selected, press the Test button. Your controller should vibrate. If not, there's something wrong. Afterwards press Use to save your controller information in the configuration file.

6. Under Injectable, press Refresh. This process will take a while, but once it's done, you should see a list of applications you can inject the hook into. If you don't see the application you're interested in, try either the 32/64-bit version, or try running X1nput Configurator as administrator.

![image](https://user-images.githubusercontent.com/10870921/123276073-25c2ac80-d505-11eb-9130-eb7b8426144c.png)

7. Select the application and press Inject. The application should move over to the Injected panel (this will happen regardless of whether or not the hook was successful, as I am a bad programmer). The default settings use so-called pressure-dependent trigger vibration, which basically means that a specific trigger only vibrates if it's pressed in slightly. If the injection doesn't seem to work, you can also try using a different injector.

![image](https://user-images.githubusercontent.com/10870921/123276163-36732280-d505-11eb-902b-34231f256b06.png)


### Screenshots

![X1nputConfigurator_TP4sLlIWty](https://user-images.githubusercontent.com/10870921/123273492-fc088600-d502-11eb-92a1-c9a7a8dafdb5.png)
![X1nputConfigurator_A7KTfhFPWP](https://user-images.githubusercontent.com/10870921/123273501-fdd24980-d502-11eb-92c1-64b1bb59fe6b.png)


### Configuration

There are 2 ways to configure X1nput. Through X1nput Configurator, or manually using notepad. To configure X1nput through X1nput Configurator, do the following:

1. Press the Configure menu option.

2. Here you can see a whole lot of variables which I didn't bother fully explaining, mostly because even I don't quite know how they work.

3. Mess around, have fun and hit Save.

To manually modify the configuration file, find the X1nput.ini file and edit it with your text editor of choice. You can then manually move X1nput.ini to the folder of the game's executable. This can even allow for per-game configuration, as long as you disable Override Config in X1nput Configurator.

### Buidling

1. Open X1nput.sln using Visual Studio 2019 or higher.
2. Here it gets a little complicated, but basically just build the AnyCPU (which is 64-bit, mostly) and x86 configurations, then copy files from each of the build folders (making sure to rename the 64-bit version if X1nput.dll to X1nput64.dll and 32-bit version of X1nputConfigurator to X1nputConfigurator32(not really required))

### Why the switch
Well, I knew from the start that the way X1nput worked wouldn't really be sustainable, so after seeing people mess around with DualSense triggers by messing around with USB requests, I decided to do a bit of my own reverse engineering. Took me a long time and several rabbit holes to get where I am, and the reason I am releasing it in an unfinished state is so that a) I wouldn't lose interest again and let the project die (twice) and b) so that willing people could help me out with actually making this thing work. I was thinking about just making this another Xinput dll replacement, but after coming across r57zone's Xinput hook, that seemed like a way neater option. I'm also hoping that removing the need for all the WinRT stuff, games would no longer refuse to work, and this method might even work on older operating systems (which fun fact, you could actually use Windows.Gaming.Input on Windows 7, but it required copying some WinMD files)

### Drawbacks
Xbox One controller drivers use a proprietary form of communication called GIP (Gaming Input Protocol, because Microsoft is all about Gaming) and I thank my lucky stars that Impulse Triggers still work even through the HID protocol.

I wasn't able to test this over bluetooth though, since I don't have any bluetooth adapters.

Sadly, one of the things Microsoft's drivers do is reset vibration every time any app loses focus, so for example whenever you alt tab. This isn't really that bad if you stay in-game, but it's something to keep in mind.

Another thing that might be possible using this approach is support for multiple controllers (up to 4, because Xinput), however the HID library I'm using doesn't seem to be able to get the device's serial number, which would allow sending requests to a controller with the same VendorID and ProductID, but different serial number, and I'm not sure how important that is because I only own 1 Xbox One controller, so I can't really do much testing.

Oh, one very important thing, since this is hooking application code, you should steer clear of any games with anti-cheat.

### Todo
- Make building the project copy the DLLs properly (for now, you should build AnyCPU first and then x86 and if the DLLs don't seem to change, just copy them from X1nput/x86/Debug or X1nput/Debug)
- Clean up the code... again
- Do a lot more testing, try to add multi controller support
- Figure out how to inject modules to 32-bit apps from a 64-bit app, alleviating the need for a separate 32 bit injector.
- Add images and a proper tutorial

### Stuff used in this project
r57zone's XInputInjectDLL https://github.com/r57zone/XInputInjectDLL

libusb hidapi https://github.com/libusb/hidapi/

TasadaKageyu's minhook https://github.com/TsudaKageyu/minhook

and a bunch more tidbits you can see throughout the code

### Honorable mentions
CookiePLMonster for helping me out with the original version of X1nput

lindquest for adding pressure-dependent trigger vibration

r57zone for making so many useful projects, you should check out his OpenVR repositories

Everyone who was willing to put up with old X1nput and even report issues and whatnot

That one person who tested old X1nput with Cyberpunk 2077 (I thought from the issues page that the old DLL no longer worked)

And of course that one person who put X1nput on PCGW (Sorry, I forgot who you are)
