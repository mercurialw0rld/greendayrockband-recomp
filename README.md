# Green Day: Rock Band for Windows

**v0.2.0-preview** is an unofficial port for 64-bit Windows 10 and 11 with a Direct3D 12-capable GPU. This release adds a dark launcher with the original Xbox 360 cover and includes the Quickplay audio fix. “Warning” was tested in Quickplay with Performance Mode enabled.

## Getting started

1. Extract the Windows ZIP to a folder you can write to.
2. Run **GreenDayRockBandLauncher.exe**. Under **Choose XEX**, select `default.xex` from your own extracted Xbox 360 copy. Keep the complete `gen` folder beside the XEX.
3. Once the launcher verifies your game files, select **Play**.

The supported disc has Title ID `454108E7`, Media ID `1BAD64D5`, and XEX SHA-256 `B778B0B18A3424F42AD3273A7808526A1556107B974861BDFF150964F9E66C3B`. The Windows ZIP contains the launcher and recompiled runtime. Supply your own Xbox 360 game files and any DLC you want to use.

## Controls and DLC

**Edit controls** lets you switch between guitar and drums and save separate profiles for each device. Connect the device before selecting it. The editor can map buttons, axes, and directions from controllers that Windows recognizes.

| Action | Keyboard guitar | Keyboard drums |
| --- | --- | --- |
| Frets or pads | 1–5 | D, F, J, K |
| Strum or kick | ↑ / ↓ | Space |
| Confirm / back | 1 or Space / 2 or Backspace | 1 / 2 |
| Whammy | W | |
| Overdrive | Tab | Complete a fill and hit the final green pad |
| Pause | Enter | Enter |

**Import DLC** installs Xbox 360 STFS packages for title `454108E7`. The launcher saves profiles, cache, and logs beside its executable. **Saves** and **Logs** open their respective folders.

## Credits

The port uses Direct3D 12, XAudio2, SDL, and the patched ReXGlue SDK from the earlier The Beatles: Rock Band port. ReXGlue and SDL license notices are in `licenses` in the Windows ZIP. [ReXGlue SDK](https://github.com/rexglue/rexglue-sdk)

The launcher displays the original North American Xbox 360 box art, including the original printed credits. [Cover scan in LaunchBox Games Database](https://gamesdb.launchbox-app.com/games/images/10217-green-day-rock-band)
