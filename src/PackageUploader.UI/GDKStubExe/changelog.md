# GDKStubExe Changelog

## PC x64 Platform Support

- Added `Debug|x64`, `Release|x64`, `Profile|x64` build configurations for native Windows PC
- `pch.h`: `#ifdef _GAMING_XBOX` conditionals separating Xbox headers from PC headers (`d3d12.h`, `dxgi1_6.h`, `d3dx12.h`); PIX macro stubs; `IID_GRAPHICS_PPV_ARGS` → `IID_PPV_ARGS` mapping
- `DeviceResources.cpp/.h`: Full DXGI swap chain code path for PC (`D3D12CreateDevice`, `IDXGISwapChain3`, standard `Present`) alongside Xbox's `PresentX`/`D3D12XboxCreateDevice`
- `Main.cpp`: Xbox PLM, XGameRuntime, and HDR guarded with `#ifdef`; PC uses `CoInitializeEx` and standard Win32 message handling (`WM_ACTIVATEAPP`, `WM_DESTROY`); try/catch error dialog for crash diagnosis
- Per-back-buffer fence tracking for PC frame synchronization (fixes `COMMAND_ALLOCATOR_SYNC` crash)
- Added `d3dx12.h` standard DirectX helper header for PC builds
- Updated `vcxproj`/`sln` with x64 platform configs linking `d3d12.lib`, `dxgi.lib`, `dxguid.lib`
- GDK edition checks scoped to `Gaming.*.x64` platforms only
- Added `.gitignore` entry for Xbox One build output layout

## Solution Explorer Cleanup

- Added `AnsiArtPS.hlsl`, `AnsiArtVS.hlsl`, `AnsiArtPS.h`, `AnsiArtVS.h`, `d3dx12.h`, `ModMusic.h`, `ModMusic.cpp` to vcxproj and filters
- Created **Shaders** filter folder for shader source and compiled bytecode headers

## MOD-Style K-Pop Music Engine

- Created `ModMusic.h`/`ModMusic.cpp` — XAudio2-based procedural music engine
- 6 instruments synthesized from math at startup: kick, clap, hihat, bass, lead, pad (~115KB PCM, zero bytes on disk)
- Pattern-based sequencer: 12 patterns, 16 rows × 6 tracks, 128 BPM, 16th-note resolution
- K-pop-inspired song structure: Intro → Verse → Chorus → Verse → Chorus → Bridge → Chorus → Outro (~64s loop)
- Rhythmic repeated notes with pentatonic jumps, signature hook with 4th-jump motif
- 8 source voices in round-robin for polyphony; pitch shifting via `SetFrequencyRatio()`
- Integrated into `Game.cpp` (Initialize, Update, Suspend, Resume)
- Added `#include <xaudio2.h>` to `pch.h`

## ANSI-Art Xbox Logo Animation

- Created `AnsiArtVS.hlsl` — fullscreen triangle vertex shader (no vertex buffer needed)
- Created `AnsiArtPS.hlsl` — pixel shader with 320×180 Xbox power-on logo embedded as static const uint arrays
- Sub-cell resolution using Unicode block element characters (quadrant blocks); 160×90 cell grid
- Animated glow effects: pulsing radiance and subtle rotating light rays driven by `g_time` constant buffer
- Pre-compiled with `fxc.exe` to `AnsiArtVS.h`/`AnsiArtPS.h` bytecode headers (SM 5.0, platform-agnostic)
- Root signature with single 32-bit constant (time) and graphics pipeline state integrated in `Game.cpp`

## Initial Project Setup

- GDK Xbox stub executable based on DirectX 12 template
- `DeviceResources.cpp/.h` — D3D12 device wrapper with Xbox frame pacing (`PresentX`, `WaitFrameEventX`)
- `Main.cpp` — Xbox entry point with PLM suspend/resume and HDR support
- `Game.cpp/.h` — render loop with `StepTimer`
- Build targets: `Gaming.Xbox.Scarlett.x64`, `Gaming.Xbox.XboxOne.x64`
