RETRO PIPES 1.8.1
Classic-inspired 3D pipes, fireworks, bubbles and a bouncing DVD logo, alone or layered together.

START HERE
Double-click RetroPipes.exe. No installer or administrator access is needed.
The controls have a Windows XP-style title bar, buttons and colours, with
Windows XP branding. Drag the title bar to move the window. The header shows
how many monitors are detected and the combined desktop size.

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
after 35-60 seconds (fixed DVD-only playback has no timed reset). Each reset also rerolls enabled random settings.

LAYERS
Choose Manual or Randomized from the top dropdown. Both use one settings frame.
Tick Pipes, Fireworks, Bubbles and/or DVD logo inside the frame to overlap effects.
Advanced opens teapots and effect amounts. In Randomized mode, it also shows
individual Shuffle choices and rotation/layer probabilities.
Changing modes keeps your entered numbers. Redundant preset buttons are gone.
Firework intensity accepts 1-30; bubble count accepts 1-100.

SPAN ALL SCREENS
Tick One continuous scene across all detected screens for one continuous scene across your desktop.
This works with wallpaper and the screensaver, including layered effects.
The pipe speed and total count stay the same; the scene has more space and
capacity. The fixed 105-second pipe cutoff is removed in this mode so paths
can grow naturally until the geometry budget is reached or paths are blocked. Random paths are not forced to visit
every screen. For one actual growing pipe, set Pipe count to 1.
Random settings roll once for the whole spanning scene. Untick this option
to return to independent scenes and random settings on each monitor.
Window preview shows the desktop's proportions. Restart wallpaper to apply.

AUTOMATIC DISPLAY SCALING
All displays reported by Windows are detected; there is no fixed monitor
count. One monitor-sized view is created for each screen. In spanning mode
all views share the same scene and clock, so adding screens never multiplies
the simulation speed. Scene space follows the combined desktop dimensions.
The app starts fresh views automatically when monitors are added, removed,
resized or rearranged, keeping your selected settings. Hardware and Windows
display support determine practical limits.

FREE-GROWING PIPE SHAPES
The old rectangular growth box and shallow depth limit are removed.
Any unoccupied neighbouring cell can be used. Pipes may grow beyond the
screen edges, with a gradual preference for visible space and returning
from far offscreen. This preference follows the camera and shared desktop.
Starting points have varied depth and are distributed through the view.
The classic right-angle turns and collision avoidance remain. Geometry
budgets and scene resets keep memory bounded without imposing spatial walls.

DRIFTING CAMERA ORBIT
Enable Drifting camera orbit for a slowly changing pitch, gentle roll and
shifting aim point. It moves above and below the scene rather than holding
the same downward angle. Shared screens use the same camera. Pipe growth
speed does not change. Disable it for the original fixed view.

DVD LOGO
Select DVD logo alone or overlap it with any of the other three effects.
The embedded transparent DVD-Video PNG includes the VIDEO detail and smooth edges. The logo bounces off edges and changes colour on impact. Separate screens
have independent logos; spanning shares one across the desktop rectangle,
including crossing internal monitor boundaries. Gaps can hide it temporarily.
Advanced contains its random appearance chance. Full Random includes DVD.
A fixed DVD-only scene bounces continuously without timed resets.

DEFAULT LIMITS
Speed defaults to 25, pipe count to 10. In Randomized mode these are upper
limits. Their sliders use the same ranges; typed overrides can go higher.
Full Random's second click restores these defaults, with only pipes selected.

TEAPOT EASTER EGG
Enabled by default, with a 0.5% chance of a Utah teapot at each pipe bend.
Open Advanced to change the chance from 0-100% or disable teapots.

COLOURS
Classic: bright glossy colours. Electric: cyan, pink and violet.
Chrome: silver tones. Drifting camera orbit is optional.
Each layout picks a random starting colour from the selected theme, so a
single pipe can use any of its colours, even with random settings turned off.

NUMBER BOXES AND RANDOMIZATION
Type directly into the number box to the right of each slider.
The speed slider covers 1-25; its number box accepts 1-1000.
The pipe-count slider covers 1-10; its number box accepts 1-500.
The slider stays at its end when you enter a larger value; that larger
number is still used. Dragging the slider chooses a value in its usual range.
Settings are saved when starting a mode or closing the controls.

Choose Randomized, then open Advanced and tick only the options to shuffle.
Speed and Count use 1 to your entered values; Colour chooses a theme.
Rotation chance rolls rotation on/off at your chosen percentage.
Pipes, Fireworks, Bubbles and DVD chance each roll their own layer on/off.
Unchecked Shuffle options follow their manual values.
Manual switches are dimmed while Shuffle controls the result.
Random effect density uses 1 to your firework intensity and bubble count.

Teapot Shuffle rolls its on/off switch (50% chance) and its per-bend probability
from zero to your entered maximum. Leave Shuffle off to keep it fixed.

Full Random is a toggle, not a button that replaces your numeric values.
First click: enable every layer, rotation, teapots and every shuffle option,
using your current numbers and probabilities. The button reads Full Random: On.
Second click: return to Manual and restore basic defaults (pipes only, speed 25,
count 10, Classic colours, rotation off, teapots at 0.5%, fireworks 4, bubbles 16).
Default shuffle choices and probabilities are restored too.
Your spanning/separate-screen choice stays unchanged. Preview or start to apply.
The on/off label reflects the current options, including after reopening.

Advanced keeps its values when closed. The main panel stays compact with the
effect switches, speed, count, colour preset and rotation. Custom colour editing
is not included in this version; the existing presets remain available.

Custom XP controls now clear stale pixels and clip text correctly during
partial repaints, fixing text that escaped its control and stuck on screen.

In separate-screen mode, each screen rolls independently on every new scene, including R
and the tray's Roll new scenes on every screen command. Your manual settings
stay saved. Random values can sometimes coincide across screens by chance.
Use Current screen settings in the tray to inspect each monitor's roll.
If all layer rolls are off, one manually selected layer remains visible
(DVD first, then Fireworks, Bubbles and Pipes). Choosing Manual uses
your manual settings on all screens with independent animation paths.

REQUIREMENTS AND NOTES
Built for 64-bit Windows 10/11, using Windows' .NET Framework 4.x and OpenGL.
No network access, downloaded assets or additional packages are required.
Wallpaper uses Explorer's WorkerW desktop surface. It was tested on this
Windows 10 PC. Windows shell changes or other wallpaper apps can affect it.
If Explorer restarts, wallpaper retries its desktop attachment automatically.
Detected monitor layout changes rebuild the views automatically.
Settings are saved in %LOCALAPPDATA%\RetroPipes\settings.xml.
This is an original recreation, not Microsoft's original screensaver binary.
Utah teapot model data: freeglut. See THIRD-PARTY-NOTICES.txt or app Credits.
Windows XP wordmark: Microsoft, via Wikimedia Commons. Flag: vector recreation.
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
