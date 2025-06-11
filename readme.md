## Instructions

1. Download GameLink from steam [here](https://store.steampowered.com/app/3376270/Game_Link/)
2. Run Gamelink, and click on 
3. Run this executable,  say yes to any networking popups you may see
4. You should see RotoVR appear in the list of devices , select it
5. Gamelink should auto detect any game you see in the game list , but you can also pick a game to configure it
6. To test you can select the gamepad plugin and start it and press the triggers and the Roto should move

## Enabling motion Compensation

1. Download **OXRMC**
2. Install it
3. edit the 
 ```
%LOCALAPPDATA%\OpenXR-MotionCompensation\OpenXR-MotionCompensation.ini
```

*(you can copy that line and paste the line above into an explorer window and it should open the file)*
4. set the tracker type to `flypt` or `rotovr` and the offset_down as shown below

```
[tracker]
type = flypt
offset_down = 68.000000
```

when in an OpenXr game press the keyboard shortcut `CTRL + INS` to enable motion compensation, you should hear a voice confirming it.
