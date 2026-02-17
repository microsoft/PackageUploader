# GDKStubExe Changelog

## "Living Our Values" Instrumental Reconstruction v7 (D Major, 120 BPM)

- **New reference track**: "Living Our Values" full instrumental mix (253s, 48kHz stereo)
- Demucs htdemucs source separation into drums/bass/other/vocals stems (vocals ~silent - instrumental track)
- Basic Pitch MIDI transcription: bass (738 notes), other (7,382 notes split into piano/lead/pad by pitch range)
- Spectral-band drum onset detection: kick (<200Hz, 404 hits), snare (200-5kHz, 803 hits), hat (>5kHz, 969 hits)
- 127 bars at 120 BPM, 7,363 total note events (avg 58.0 per bar)
- 8 instruments: kick, snare, hi-hat, bass, piano, pad, lead, strings
- Piano split across 2 tracks for polyphony; lead notes (C5+) on dedicated track

## Spectral-Band Drum Fix v6 ("떠나고 싶은 날" F# Major)

- Replaced BasicPitch drum classification with frequency-band onset detection
- Kick (<200Hz, 663 hits), snare (200-5kHz, 636 hits), hat (>5kHz, 781 hits)
- Improved drum synthesis: deeper kick with click, snare with wire rattle, metallic hat

## Demucs+BasicPitch Transcription v5 ("떠나고 싶은 날" F# Major)

- **Completely new approach**: AI source separation (Demucs htdemucs) splits audio into 4 isolated stems: drums, bass, vocals, other
- **Per-stem MIDI transcription** via Spotify's Basic Pitch neural network on each isolated stem
- Bass stem: 1,345 transcribed notes (MIDI 27-82), quantized to 771 active 16th-note rows
- Other/piano stem: 3,752 notes split into piano (MIDI <72) → 905 rows and melody (MIDI ≥72) → 389 rows
- Drums stem: 4,937 notes classified by pitch range into kick (<48) → 704 hits, snare (48-65) → 694 hits, hat (≥66) → 27 hits
- Total: 4,326 note events across 68 bars (avg 63.6 per bar vs ~20 in previous CQT-based versions)
- Every note comes from actual MIDI transcription of an isolated instrument stem, not estimated chords/chroma
- 8 tracks: kick, snare, hihat, bass, piano-low, piano-high, melody, arp
- Bass notes clamped to playback range (MIDI 24-60) for proper sub-bass
- Piano split across 2 tracks (low voice + high voice) for wider chord voicing

## Faithful Ballad Transcription v4 ("떠나고 싶은 날" style, F# Major)

- Per-16th-note CQT across 3 frequency bands with open root-5th piano voicings
- Superseded by v5 (CQT analysis on mixed signal couldn't isolate individual instruments)

## High-Energy Ballad Transcription v3 ("떠나고 싶은 날" style, F# Major)

- Major energy boost: raised all velocity levels and used actual 16th-note drum patterns
- Superseded by v4 (voicings too generic, bass not walking, piano not matching actual register)

## Precise Ballad Transcription v2 ("떠나고 싶은 날" style, F# Major)

- Complete rewrite using deep audio analysis (HPSS, CQT bass extraction, beat-aligned chroma, pyin melody)
- Bar-by-bar transcription: each of the 68 bars uses the actual detected chord, bass note, and energy level
- Superseded by v3 (lacked energy, drums too sparse, bass only 2 notes per bar)

## K-Pop Ballad Replication v1 ("떠나고 싶은 날" style, F# Major)

- First attempt: analyzed reference track using librosa audio analysis (tempo, key, chroma, melody, spectral bands)
- Key: F# major (detected via Krumhansl key profile correlation from chroma features)
- Tempo: 80 BPM (mid-tempo ballad, autocorrelation peak at 81 BPM)
- Chord progression: B(IV) → F#(I) → A#m(iii) with C#(V), G#m(ii), D#m(vi) for transitions
- 8 instruments: soft kick, rim/cross-stick, brush hi-hat, warm sine bass, piano (6-harmonic series), 5-voice supersaw pad, bell/singing melody, arp sparkle
- 14-voice polyphony (up from 10), 8 parallel tracks (up from 7)
- 27 unique patterns in a 68-bar arrangement (~204 seconds, matching original song length)
- Structure: Intro(4) → Verse1(8) → Pre-chorus(4) → Verse2(8) → Chorus(8) → Bridge(4) → Verse3(8) → Pre-chorus(4) → Chorus2(8) → Final(4) → Outro(8)
- Superseded by v2 (melody was discordant with harmony, didn't match reference track)

## K-Pop Song Rewrite (Ab Major Banger)

- Complete rewrite of the background song from scratch
- Key changed from C major to Ab major (classic K-pop key — BLACKPINK, TWICE territory)
- BPM increased from 128 to 130 (standard K-pop tempo)
- Chord progression: Ab(I) → Fm(vi) → Db(IV) → Eb(V) throughout
- 7 instruments (added arp/sparkle voice), 10-voice polyphony (up from 8)
- New instruments: punchy 808 kick with sub weight, 5-voice supersaw pad, bouncy 808 bass with pitch slide, bright pulse-wave lead, short arp ping
- 16 unique patterns with proper verse/pre-chorus/chorus/bridge/outro structure
- Catchy descending hook melody: Eb6→C6→Bb5→Ab5 with signature jump back to Eb6
- Pre-chorus "launch ramp" with ascending repeated-note build (Ab→Bb→C→Eb)
- 32-bar arrangement (~59 seconds) with seamless loop point

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
