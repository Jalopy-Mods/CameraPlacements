## Installation

For using the mod, you need to have <b>[BepInEx](https://github.com/BepInEx/BepInEx/releases/download/v5.4.21/BepInEx_x64_5.4.21.0.zip)</b> installed.

To do that, you need to [download the zip file](https://github.com/BepInEx/BepInEx/releases/download/v5.4.21/BepInEx_x64_5.4.21.0.zip), extract it and copy the folder <b>BepInEx</b> into the game folder.
<br>
<img height="200" src="https://cdn.discordapp.com/attachments/897896186487390218/1098716879331270879/image.png" width="375"/>

Then, you just have to [download](https://github.com/MeblIkea/CameraPlacements/releases) the mod from the Releases, and you can copy the entire folder into the game folder (as you did with **BepInEx**) (and so you should have a **CameraPlacements** folder in your `BepInEx/Plugins`).



## Keybinds

**All the keybinds can be changed in the mod config file.**

| **KEY**                        | **DEFINITION**                                          |
| ------------------------------ | ------------------------------------------------------- |
| F10 (configurable)             | Open Menu (only in the main menu scene, or in-game)     |
| Up Arrow (configurable)        | Move Forward                                            |
| Back Arrow (configurable)      | Move Backward                                           |
| Left Arrow (configurable)      | Move Left                                               |
| Right Arrow (configurable)     | Move Right                                              |
| SPACE (configurable)           | Move Up                                                 |
| *Control + Mouse Wheel*        | Change camera FOV                                       |
| *Shift + Mouse Wheel*          | Change camera speed (*shift* increase the speed faster) |
| *Shift* + SPACE (configurable) | Preview / Quick Move a point                            |
| *ESCAPE*                       | Stop the current animation.                             |



## How does it works?

After loading the game, you can go to the main menu (or in-game, it's the same).
Press F10, and you should see a menu at the bottom-left corner. ⚠️**If you don't see it, there is an issue.**⚠️

Press `Switch view to freecam` to get in FreeCam. If you don't like the camera rolling, you can lock X&Z axis, it might be better.
You can manually change it's position / rotation, and *Control + Scroll* change the camera FOV, while just scrolling change the camera speed (Press *shift* while scrolling for increasing faster).

Press `Add Point` for adding a keyframe. It will create a new point at the FreeCam position (and will copy its rotation, Field Of View (FOV) and speed).
You can select a point with the slider, and change it's properties.
*(You can also edit it with Shift + Space (preview), and moving the camera will apply its modification to the poit)*

Wait time is how many milliseconds it takes, before starting (from this point).
Animation mode is the mod of animation. Triangle is linear, the Round is round (EaseInOut), and the Square is direct.

<img height="200" src="https://cdn.discordapp.com/attachments/897896186487390218/1100802109861023815/i0.jpg" width="375"/>

For playing the animation, click **Launch animation**.

You can give a name for your `Group` *(= save)*, and click `Manage Saves` for opening a new window, so you can manage the other saves (Load and Delete them).
<br>(Save files are saved in `C:\Users\%username%\AppData\LocalLow\Minskworks\Jalopy\ModSaves\CameraPoints`)

## Contact

If you have any questions, you can contact me on Discord: <b>Meb#2325</b>, or join <b>[Minskworks Discord server](https://discord.gg/TqCwKdR)</b>.
<b>Also, I'm open to feature suggestion/ mod suggestion =)</b>
