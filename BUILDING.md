# Building Green Day: Rock Band for Windows

This source archive contains the WinForms launcher, native compatibility code, and static recompilation configuration. See [README.md](README.md) for the supported Xbox 360 disc build and runtime setup.

## Build dependencies

The scripts reuse the Windows x64 toolchain from the earlier The Beatles: Rock Band port. Set `-BeatlesWorkspace` to that workspace. Its `work/tools` directory supplies Visual Studio Build Tools with Clang, CMake, Ninja, the Visual C++ runtime, and the license files. The scripts expect the patched ReXGlue SDK 0.10.0 at `work/graphics-sdk/out/install/beatles`. Use `-SdkDirectory` to point to another installation of the same patched SDK. The SDK and toolchain are separate from this source archive.

Edit `greendayrockband_manifest.toml` so `game_root` points to your extracted disc folder and `file_path` points to its `default.xex`. Use forward slashes in the TOML paths. Then run the following from the source folder in PowerShell.

```powershell
$beatlesWorkspace = 'C:/Projects/BeatlesRockBand'
.\Build.ps1 -BeatlesWorkspace $beatlesWorkspace
.\Build-Launcher.ps1 -BeatlesWorkspace $beatlesWorkspace -OutputDirectory .\portable
```

`Build.ps1` generates C++ from the XEX on the first build, then compiles the native runtime. The portable output in the example is `portable`.

## Compatibility code

- **Indirect calls.** The Beatles pointer scanner found 70 candidate destinations in this XEX. The PowerPC `.pdata` table identified 24 as addresses inside existing functions. The remaining 46 were tested as a batch in `config/pointers.toml`. The destination at `0x822002E8` is needed at startup.
- **Startup.** `src/startup_compat.cpp` passes the context to the original game loop at `0x82200238`. On Xbox, a deliberate write to address zero at `0x82200610` reaches that loop through the exception callback at `0x822005E8`. The port calls the continuation directly.
- **Quickplay audio.** The reader for “Warning” consumed the entire MOGG without initializing Vorbis. The wrapper at `0x82515E18` delegated a single 16-byte header block to the SDK stubs `XeKeysSetKey` and `XeKeysAesCbc`. `src/mogg_header_compat.cpp` uses Windows AES for that block. The native check covered both methods with separate and in-place buffers. [Audio header methods documented in Onyx](https://github.com/mtolly/onyx/blob/d6c9cec8bec82bf14954f091d732b3436571c020/haskell/packages/onyx-moggcrypt/cbits/keys.h)
