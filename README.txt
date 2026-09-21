RETRO PIPES 1.1
Procedurally generated 3D pipes inspired by the classic Windows screensaver.

START HERE
Double-click RetroPipes.exe. No installer or administrator access is needed.

LIVE WALLPAPER
Choose your colours, speed and pipe count, then click Start live wallpaper.
The pipes animate behind your desktop icons, separately on each monitor.
The controls move into the system tray (beside the clock; check the ^ menu).
Right-click the blue-and-gold pipes icon to open controls, make a new layout,
stop the wallpaper, or exit. Stopping reveals your existing background.
Wallpaper runs for the current session; it does not add itself to startup.
Changes to settings take effect when you start a new preview or wallpaper.

SCREENSAVER
Click Try screensaver for full-screen pipes. Move the mouse or press a key
to exit. To use it automatically when idle, keep this folder in a permanent
location, right-click RetroPipes.scr and choose Install. Windows opens Screen
Saver Settings; select your wait time and click Apply. Use Windows' own
"On resume, display logon screen" option if you want a password on resume.
The screensaver itself is an animation, not a screen-locking utility.

WINDOW PREVIEW
Click Preview in a window. Space pauses/resumes; R starts a new layout;
Esc closes it. Each scene is generated live and resets after it fills up.

COLOURS
Classic: bright glossy colours. Electric: cyan, pink and violet.
Chrome: silver tones. Slow scene rotation is optional.

NUMBER BOXES AND RANDOMIZATION
Type directly into the number box to the right of each slider.
The speed slider covers 1-10; its number box accepts 1-1000.
The pipe-count slider covers 1-9; its number box accepts 1-500.
The slider stays at its end when you enter a larger value; that larger
number is still used. Dragging the slider chooses a value in its usual range.
Settings are saved when starting a mode or closing the controls.

Enable Randomize settings each run to roll a new combination whenever you
start a window preview, screensaver, or wallpaper. Speed and count are chosen
between 1 and your entered values; colour theme and rotation are randomized.
All monitors share those settings for that run but grow different layouts.
Your manual settings remain saved. Automatic layout resets and R keep the
current run's settings. Stop and start again for another settings roll.

REQUIREMENTS AND NOTES
Built for 64-bit Windows 10/11, using Windows' .NET Framework 4.x and OpenGL.
No network access, downloaded assets or additional packages are required.
Wallpaper uses Explorer's WorkerW desktop surface. It was tested on this
Windows 10 PC. Windows shell changes or other wallpaper apps can affect it.
If Explorer restarts, reopen the controls and start the wallpaper again.
After changing monitor layout or display scaling, restart the wallpaper.
Settings are saved in %LOCALAPPDATA%\RetroPipes\settings.xml.
This is an original recreation, not Microsoft's original screensaver binary.
The executable is locally built and is not digitally signed.

COMMAND LINE
RetroPipes.exe --wallpaper    Start wallpaper with tray controls.
RetroPipes.exe /s             Full-screen screensaver on all monitors.
RetroPipes.scr /c             Settings/launcher.
RetroPipes.scr /p HWND        Preview embedded in a Windows preview window.

SOURCE
The source folder contains the complete C# source and rebuild script.
Public repository: https://github.com/S0MBRX/retro-pipes

UNINSTALL
Stop the wallpaper. If selected as your Windows screensaver, choose another
screensaver (or None) in Screen Saver Settings, then delete this folder.
Optionally delete %LOCALAPPDATA%\RetroPipes to remove saved settings.
