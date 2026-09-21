RETRO PIPES 1.2
Classic-inspired 3D pipes, fireworks and bubbles, alone or layered together.

START HERE
Double-click RetroPipes.exe. No installer or administrator access is needed.

LIVE WALLPAPER
Choose your layers, colours and numbers, then click Start wallpaper.
The animations run behind your desktop icons, separately on each monitor.
The controls move into the system tray (beside the clock; check the ^ menu).
Right-click the blue-and-gold pipes icon to open controls, make a new layout,
stop the wallpaper, or exit. Stopping reveals your existing background.
Wallpaper runs for the current session; it does not add itself to startup.
Changes to settings take effect when you start a new preview or wallpaper.

SCREENSAVER
Click Try screensaver for full-screen animation. Move the mouse or press a key
to exit. To use it automatically when idle, keep this folder in a permanent
location, right-click RetroPipes.scr and choose Install. Windows opens Screen
Saver Settings; select your wait time and click Apply. Use Windows' own
"On resume, display logon screen" option if you want a password on resume.
The screensaver itself is an animation, not a screen-locking utility.

WINDOW PREVIEW
Click Window preview. Space pauses/resumes; R starts a new scene;
Esc closes it. Pipe scenes reset when full; effects-only scenes reset
after 35-60 seconds. Each reset also rerolls enabled random settings.

LAYERS
Use the Pipes only, Fireworks only, Bubbles only, or All three buttons.
Tick any combination of layer checkboxes to overlap those effects.
The preset buttons disable random layer selection until you re-enable it.
Firework intensity accepts 1-30; bubble count accepts 1-100.

TEAPOT EASTER EGG
Enabled by default, with a 0.5% chance of a Utah teapot at each pipe bend.
Change the chance from 0-100% to make it rarer or more common.

COLOURS
Classic: bright glossy colours. Electric: cyan, pink and violet.
Chrome: silver tones. Slow scene rotation is optional.
Each layout picks a random starting colour from the selected theme, so a
single pipe can use any of its colours, even with random settings turned off.

NUMBER BOXES AND RANDOMIZATION
Type directly into the number box to the right of each slider.
The speed slider covers 1-10; its number box accepts 1-1000.
The pipe-count slider covers 1-9; its number box accepts 1-500.
The slider stays at its end when you enter a larger value; that larger
number is still used. Dragging the slider chooses a value in its usual range.
Settings are saved when starting a mode or closing the controls.

Enable random cycle, then tick only the options you want randomized.
Speed and Count use 1 to your entered values; Colour chooses a theme.
Rotation chance rolls rotation on/off at your chosen percentage.
Pipes, Fireworks and Bubbles chance each roll their own layer on/off.
Unchecked options follow the manual settings on the left.
Random effect density uses 1 to your firework intensity and bubble count.

Pipes random: speed, count, colour and rotation.
Mix random: only the three layer choices.
All random: every available category. Teapot chance stays as entered.

Each screen rolls independently on start AND every new scene, including R
and the tray's Roll new scenes on every screen command. Your manual settings
stay saved. Random values can sometimes coincide across screens by chance.
Use Current screen settings in the tray to inspect each monitor's roll.
If all layer rolls are off, one manually selected layer remains visible
(Fireworks first, then Bubbles, then Pipes). Turning random cycle off uses
your manual settings on all screens with independent animation paths.

REQUIREMENTS AND NOTES
Built for 64-bit Windows 10/11, using Windows' .NET Framework 4.x and OpenGL.
No network access, downloaded assets or additional packages are required.
Wallpaper uses Explorer's WorkerW desktop surface. It was tested on this
Windows 10 PC. Windows shell changes or other wallpaper apps can affect it.
If Explorer restarts, reopen the controls and start the wallpaper again.
After changing monitor layout or display scaling, restart the wallpaper.
Settings are saved in %LOCALAPPDATA%\RetroPipes\settings.xml.
This is an original recreation, not Microsoft's original screensaver binary.
Utah teapot model data: freeglut. See THIRD-PARTY-NOTICES.txt or app Credits.
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
