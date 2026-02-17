//
// ModMusic.cpp - MOD/S3M-style procedural music engine
//

#include "pch.h"
#include "ModMusic.h"

#include <cmath>
#include <cstring>

namespace
{
    constexpr float PI = 3.14159265358979323846f;
    constexpr float TWO_PI = 2.0f * PI;
    constexpr int   SR = 44100;

    // MIDI note to frequency
    inline float NoteToFreq(int note)
    {
        return 440.0f * std::pow(2.0f, (note - 69) / 12.0f);
    }

    // Simple pseudo-random (deterministic for reproducibility)
    uint32_t s_rng = 12345;
    inline float Noise()
    {
        s_rng = s_rng * 1664525u + 1013904223u;
        return static_cast<float>(static_cast<int32_t>(s_rng)) / 2147483648.0f;
    }

    // Clamp to int16 range
    inline int16_t ToS16(float v)
    {
        if (v > 1.0f) v = 1.0f;
        if (v < -1.0f) v = -1.0f;
        return static_cast<int16_t>(v * 32767.0f);
    }
}

ModMusic::ModMusic() noexcept :
    m_xaudio2(nullptr),
    m_masterVoice(nullptr),
    m_nextVoice(0),
    m_accumulator(0.0),
    m_currentRow(0),
    m_currentOrder(0)
{
    std::memset(m_voices, 0, sizeof(m_voices));
    std::memset(m_xaBuffers, 0, sizeof(m_xaBuffers));
}

ModMusic::~ModMusic()
{
    for (int i = 0; i < NUM_CHANNELS; i++)
    {
        if (m_voices[i])
        {
            m_voices[i]->Stop();
            m_voices[i]->DestroyVoice();
        }
    }
    if (m_masterVoice)
        m_masterVoice->DestroyVoice();
    if (m_xaudio2)
        m_xaudio2->Release();
}

void ModMusic::Initialize()
{
    // Create XAudio2 engine
    HRESULT hr = XAudio2Create(&m_xaudio2, 0, XAUDIO2_DEFAULT_PROCESSOR);
    if (FAILED(hr)) return;

    hr = m_xaudio2->CreateMasteringVoice(&m_masterVoice);
    if (FAILED(hr)) return;

    // Create source voices (all same format — mono 16-bit 44100)
    WAVEFORMATEX wfx = {};
    wfx.wFormatTag = WAVE_FORMAT_PCM;
    wfx.nChannels = 1;
    wfx.nSamplesPerSec = SAMPLE_RATE;
    wfx.wBitsPerSample = 16;
    wfx.nBlockAlign = 2;
    wfx.nAvgBytesPerSec = SAMPLE_RATE * 2;

    for (int i = 0; i < NUM_CHANNELS; i++)
    {
        hr = m_xaudio2->CreateSourceVoice(&m_voices[i], &wfx);
        if (FAILED(hr)) m_voices[i] = nullptr;
    }

    SynthesizeInstruments();
    BuildSong();
}

// ============================================================
// Instrument synthesis — all procedural, no external data
// ============================================================
void ModMusic::SynthesizeInstruments()
{
    // --- 0: Kick drum (150→50Hz sine sweep, exponential decay) ---
    {
        const int len = static_cast<int>(SR * 0.25f);
        auto& inst = m_instruments[0];
        inst.baseFreq = 100.0f;
        inst.pcm.resize(len);
        float phase = 0.0f;
        for (int i = 0; i < len; i++)
        {
            float t = static_cast<float>(i) / SR;
            float env = std::exp(-t * 12.0f);
            float freq = 150.0f * std::exp(-t * 8.0f) + 50.0f;
            phase += freq / SR;
            float sample = std::sin(TWO_PI * phase) * env * 0.9f;
            // Add a click transient
            if (t < 0.005f)
                sample += std::sin(TWO_PI * 1000.0f * t) * (1.0f - t / 0.005f) * 0.3f;
            inst.pcm[i] = ToS16(sample);
        }
    }

    // --- 1: Clap/snap (layered soft noise bursts, no harsh transient) ---
    {
        const int len = static_cast<int>(SR * 0.15f);
        auto& inst = m_instruments[1];
        inst.baseFreq = 400.0f;
        inst.pcm.resize(len);
        float lp = 0.0f;
        for (int i = 0; i < len; i++)
        {
            float t = static_cast<float>(i) / SR;
            // 3 layered micro-bursts for clap texture
            float burst1 = (t < 0.008f) ? 1.0f : 0.0f;
            float burst2 = (t > 0.012f && t < 0.020f) ? 0.7f : 0.0f;
            float burst3 = (t > 0.024f && t < 0.035f) ? 0.5f : 0.0f;
            float burstEnv = burst1 + burst2 + burst3;
            // Smooth tail
            float tailEnv = std::exp(-t * 25.0f);
            float env = (burstEnv > 0.01f) ? burstEnv : tailEnv * 0.4f;
            // Soft filtered noise (lowpass)
            float n = Noise() * env;
            lp += 0.15f * (n - lp);
            // Gentle tonal body
            float tone = std::sin(TWO_PI * 400.0f * t) * std::exp(-t * 30.0f) * 0.15f;
            inst.pcm[i] = ToS16((lp + tone) * 0.7f);
        }
    }

    // --- 2: Hihat (soft metallic tick, gentle) ---
    {
        const int len = static_cast<int>(SR * 0.06f);
        auto& inst = m_instruments[2];
        inst.baseFreq = 8000.0f;
        inst.pcm.resize(len);
        float lp = 0.0f;
        for (int i = 0; i < len; i++)
        {
            float t = static_cast<float>(i) / SR;
            float env = std::exp(-t * 55.0f);
            // Mix of metallic tones instead of raw noise
            float metal = std::sin(TWO_PI * 6500.0f * t) * 0.3f
                        + std::sin(TWO_PI * 8200.0f * t) * 0.25f
                        + std::sin(TWO_PI * 11700.0f * t) * 0.15f;
            // Tiny bit of filtered noise for texture
            float n = Noise() * 0.15f;
            lp += 0.3f * (n - lp);
            inst.pcm[i] = ToS16((metal + lp) * env * 0.35f);
        }
    }

    // --- 3: Bass synth (warm sine + sub, round and bouncy) ---
    {
        const int len = static_cast<int>(SR * 0.3f);
        auto& inst = m_instruments[3];
        inst.baseFreq = NoteToFreq(36); // C2
        inst.pcm.resize(len);
        float phase = 0.0f;
        for (int i = 0; i < len; i++)
        {
            float t = static_cast<float>(i) / SR;
            // Snappy pluck envelope for bounce
            float env;
            if (t < 0.003f) env = t / 0.003f;
            else if (t < 0.04f) env = 1.0f - (t - 0.003f) / 0.037f * 0.3f; // quick drop to 0.7
            else if (t < 0.2f) env = 0.7f;
            else env = 0.7f * (1.0f - (t - 0.2f) / 0.1f);
            if (env < 0.0f) env = 0.0f;

            phase += inst.baseFreq / SR;
            if (phase >= 1.0f) phase -= 1.0f;
            // Warm sine + pitched-down sub
            float sine = std::sin(TWO_PI * phase) * 0.7f;
            float sub = std::sin(TWO_PI * phase * 0.5f) * 0.4f;
            inst.pcm[i] = ToS16((sine + sub) * env * 0.6f);
        }
    }

    // --- 4: Lead synth (bubbly pluck — sine + triangle, snappy) ---
    {
        const int len = static_cast<int>(SR * 0.2f);
        auto& inst = m_instruments[4];
        inst.baseFreq = NoteToFreq(60); // C4
        inst.pcm.resize(len);
        float phase1 = 0.0f, phase2 = 0.0f;
        float freq = inst.baseFreq;
        for (int i = 0; i < len; i++)
        {
            float t = static_cast<float>(i) / SR;
            // Plucky envelope: instant attack, quick decay, short sustain
            float env;
            if (t < 0.002f) env = t / 0.002f;
            else if (t < 0.03f) env = 1.0f - (t - 0.002f) / 0.028f * 0.5f;
            else if (t < 0.12f) env = 0.5f;
            else env = 0.5f * (1.0f - (t - 0.12f) / 0.08f);
            if (env < 0.0f) env = 0.0f;

            phase1 += freq / SR;
            phase2 += (freq * 2.0f) / SR; // octave harmonic
            if (phase1 >= 1.0f) phase1 -= 1.0f;
            if (phase2 >= 1.0f) phase2 -= 1.0f;
            // Triangle wave (softer than saw)
            float tri = (phase1 < 0.5f) ? (phase1 * 4.0f - 1.0f) : (3.0f - phase1 * 4.0f);
            // Sine octave for sparkle
            float oct = std::sin(TWO_PI * phase2) * 0.25f;
            // Pitch bend down slightly at start for "bubble pop" feel
            float bendAmt = (t < 0.015f) ? (1.0f - t / 0.015f) * 0.08f : 0.0f;
            float bent = std::sin(TWO_PI * phase1 * (1.0f + bendAmt));
            inst.pcm[i] = ToS16((tri * 0.4f + bent * 0.3f + oct) * env * 0.65f);
        }
    }

    // --- 5: Chord pad (3-voice detuned sines, slow attack/release) ---
    {
        const int len = static_cast<int>(SR * 0.5f);
        auto& inst = m_instruments[5];
        inst.baseFreq = NoteToFreq(60); // C4
        inst.pcm.resize(len);
        float p1 = 0.0f, p2 = 0.0f, p3 = 0.0f;
        float freq = inst.baseFreq;
        for (int i = 0; i < len; i++)
        {
            float t = static_cast<float>(i) / SR;
            float env;
            if (t < 0.05f) env = t / 0.05f;
            else if (t < 0.35f) env = 1.0f;
            else env = 1.0f - (t - 0.35f) / 0.15f;
            if (env < 0.0f) env = 0.0f;

            p1 += freq / SR;
            p2 += (freq * 1.003f) / SR;
            p3 += (freq * 0.997f) / SR;
            if (p1 >= 1.0f) p1 -= 1.0f;
            if (p2 >= 1.0f) p2 -= 1.0f;
            if (p3 >= 1.0f) p3 -= 1.0f;
            float v = std::sin(TWO_PI * p1) + std::sin(TWO_PI * p2) + std::sin(TWO_PI * p3);
            inst.pcm[i] = ToS16(v / 3.0f * env * 0.5f);
        }
    }

    // Build XAUDIO2_BUFFER descriptors for each instrument
    for (int i = 0; i < NUM_INSTRUMENTS; i++)
    {
        auto& buf = m_xaBuffers[i];
        buf.AudioBytes = static_cast<UINT32>(m_instruments[i].pcm.size() * sizeof(int16_t));
        buf.pAudioData = reinterpret_cast<const BYTE*>(m_instruments[i].pcm.data());
        buf.Flags = XAUDIO2_END_OF_STREAM;
    }
}

// ============================================================
// Song data — K-pop inspired dance beat, 128 BPM, ~64s loop
// Structured as: Intro(2) Verse(4) Chorus(4) Verse(4) Chorus(4)
//                Bridge(4) Chorus(4) Outro(4) → 30 bars, pad to 32
// ============================================================
void ModMusic::BuildSong()
{
    // MIDI note numbers:
    // C2=36 E2=40 F2=41 G2=43 A2=45 B2=47
    // C3=48 E3=52 F3=53 G3=55 A3=57 B3=59
    // C4=60 D4=62 E4=64 F4=65 G4=67 A4=69 B4=71
    // C5=72 D5=74 E5=76 F5=77 G5=79 A5=81 B5=83
    // C6=84

    const uint8_t V_HI = 220;
    const uint8_t V_MD = 170;
    const uint8_t V_LO = 120;
    const uint8_t V_GH = 90;

    // Instruments: 0=kick, 1=clap, 2=hihat, 3=bass, 4=lead, 5=pad

    auto emptyPattern = []() -> Pattern {
        Pattern p;
        for (int r = 0; r < 16; r++)
            for (int t = 0; t < 6; t++)
                p.notes[r][t] = { 0xFF, 0, 0 };
        return p;
    };

    // Helper: standard verse drum pattern
    auto verseDrums = [&](Pattern& p) {
        // Kick: 4otf + offbeat bounce
        p.notes[0][0]  = { 0, 48, V_HI };
        p.notes[4][0]  = { 0, 48, V_HI };
        p.notes[8][0]  = { 0, 48, V_HI };
        p.notes[12][0] = { 0, 48, V_HI };
        // Clap on 2 and 4
        p.notes[4][1]  = { 1, 60, V_MD };
        p.notes[12][1] = { 1, 60, V_MD };
        // Hihat 8ths
        for (int r = 0; r < 16; r += 2)
            p.notes[r][2] = { 2, 80, V_MD };
    };

    // Helper: chorus drum pattern (busier, more energy)
    auto chorusDrums = [&](Pattern& p) {
        // Kick: bouncy pattern
        p.notes[0][0]  = { 0, 48, V_HI };
        p.notes[3][0]  = { 0, 48, V_MD };
        p.notes[4][0]  = { 0, 48, V_HI };
        p.notes[7][0]  = { 0, 48, V_MD };
        p.notes[8][0]  = { 0, 48, V_HI };
        p.notes[11][0] = { 0, 48, V_MD };
        p.notes[12][0] = { 0, 48, V_HI };
        // Clap on 2 and 4 + offbeat accents
        p.notes[4][1]  = { 1, 60, V_HI };
        p.notes[12][1] = { 1, 60, V_HI };
        p.notes[14][1] = { 1, 60, V_GH };
        // Hihat 16ths (driving)
        for (int r = 0; r < 16; r++)
            p.notes[r][2] = { 2, 80, (r % 2 == 0) ? V_MD : V_GH };
    };

    // ================================================================
    // P0: Intro bar 1 — kick + hihat + hook tease
    // ================================================================
    {
        Pattern p = emptyPattern();
        p.notes[0][0]  = { 0, 48, V_HI };
        p.notes[4][0]  = { 0, 48, V_HI };
        p.notes[8][0]  = { 0, 48, V_HI };
        p.notes[12][0] = { 0, 48, V_HI };
        for (int r = 0; r < 16; r += 2)
            p.notes[r][2] = { 2, 80, V_MD };
        // Tease: just the chorus hook rhythm, no resolution
        p.notes[0][4]  = { 4, 79, V_MD };  // G5
        p.notes[3][4]  = { 4, 79, V_MD };  // G5
        p.notes[6][4]  = { 4, 84, V_HI };  // C6 (the hook jump)
        // Pad
        p.notes[0][5]  = { 5, 60, V_LO };  // C4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P1: Intro bar 2 — beat drop + answer
    // ================================================================
    {
        Pattern p = emptyPattern();
        verseDrums(p);
        // Bass enters
        p.notes[0][3]  = { 3, 36, V_HI };  // C2
        p.notes[3][3]  = { 3, 48, V_MD };  // C3
        p.notes[8][3]  = { 3, 43, V_HI };  // G2
        p.notes[11][3] = { 3, 55, V_MD };  // G3
        // Descending answer from the tease
        p.notes[0][4]  = { 4, 84, V_HI };  // C6
        p.notes[2][4]  = { 4, 81, V_MD };  // A5
        p.notes[4][4]  = { 4, 79, V_HI };  // G5
        p.notes[6][4]  = { 4, 76, V_MD };  // E5
        p.notes[8][4]  = { 4, 72, V_MD };  // C5
        m_patterns.push_back(p);
    }

    // ================================================================
    // P2: Verse A1 — rhythmic "talk-sing" style, repeated notes + jumps
    // Chords: C | Am
    // ================================================================
    {
        Pattern p = emptyPattern();
        verseDrums(p);
        // Bass: C → Am
        p.notes[0][3]  = { 3, 36, V_HI };  // C2
        p.notes[3][3]  = { 3, 48, V_LO };  // C3
        p.notes[6][3]  = { 3, 36, V_MD };
        p.notes[8][3]  = { 3, 45, V_HI };  // A2
        p.notes[11][3] = { 3, 57, V_LO };  // A3
        p.notes[14][3] = { 3, 45, V_MD };
        // Melody: K-pop verse — rhythmic, syncopated, centered on G, jumps to C
        p.notes[0][4]  = { 4, 79, V_HI };  // G5
        p.notes[2][4]  = { 4, 79, V_MD };  // G5 (rhythmic repeat)
        p.notes[3][4]  = { 4, 76, V_HI };  // E5 (drop)
        p.notes[5][4]  = { 4, 79, V_MD };  // G5
        p.notes[8][4]  = { 4, 76, V_HI };  // E5
        p.notes[10][4] = { 4, 76, V_MD };  // E5
        p.notes[11][4] = { 4, 72, V_HI };  // C5 (drop)
        p.notes[14][4] = { 4, 76, V_MD };  // E5
        // Pad
        p.notes[0][5]  = { 5, 60, V_LO };  // C4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P3: Verse A2 — response, pushes higher
    // Chords: F | G
    // ================================================================
    {
        Pattern p = emptyPattern();
        verseDrums(p);
        // Bass: F → G
        p.notes[0][3]  = { 3, 41, V_HI };  // F2
        p.notes[3][3]  = { 3, 53, V_LO };  // F3
        p.notes[6][3]  = { 3, 41, V_MD };
        p.notes[8][3]  = { 3, 43, V_HI };  // G2
        p.notes[11][3] = { 3, 55, V_LO };  // G3
        p.notes[14][3] = { 3, 43, V_MD };
        // Melody: same rhythm but pitched up, ends on a question
        p.notes[0][4]  = { 4, 81, V_HI };  // A5
        p.notes[2][4]  = { 4, 81, V_MD };  // A5
        p.notes[3][4]  = { 4, 79, V_HI };  // G5
        p.notes[5][4]  = { 4, 81, V_MD };  // A5
        p.notes[8][4]  = { 4, 84, V_HI };  // C6 (peak!)
        p.notes[10][4] = { 4, 81, V_MD };  // A5
        p.notes[12][4] = { 4, 79, V_HI };  // G5
        p.notes[15][4] = { 4, 79, V_GH };  // G5 (pickup)
        // Pad
        p.notes[0][5]  = { 5, 65, V_LO };  // F4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P4: Verse B1 — pre-chorus energy, syncopated staccato
    // Chords: Am | Em
    // ================================================================
    {
        Pattern p = emptyPattern();
        verseDrums(p);
        // Extra clap for buildup
        p.notes[14][1] = { 1, 60, V_GH };
        // Bass: Am → Em
        p.notes[0][3]  = { 3, 45, V_HI };  // A2
        p.notes[3][3]  = { 3, 57, V_LO };
        p.notes[6][3]  = { 3, 45, V_MD };
        p.notes[8][3]  = { 3, 40, V_HI };  // E2
        p.notes[11][3] = { 3, 52, V_LO };
        p.notes[14][3] = { 3, 40, V_MD };
        // Melody: punchy staccato, rhythmic chant feel
        p.notes[0][4]  = { 4, 76, V_HI };  // E5
        p.notes[1][4]  = { 4, 76, V_MD };  // E5
        p.notes[3][4]  = { 4, 79, V_HI };  // G5
        p.notes[4][4]  = { 4, 76, V_MD };  // E5
        p.notes[6][4]  = { 4, 72, V_HI };  // C5
        p.notes[8][4]  = { 4, 76, V_HI };  // E5
        p.notes[9][4]  = { 4, 76, V_MD };  // E5
        p.notes[11][4] = { 4, 79, V_HI };  // G5
        p.notes[12][4] = { 4, 81, V_HI };  // A5
        p.notes[14][4] = { 4, 79, V_MD };  // G5
        // Pad
        p.notes[0][5]  = { 5, 69, V_LO };  // A4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P5: Verse B2 — pre-chorus peak, launches into chorus
    // Chords: F | G (→ chorus)
    // ================================================================
    {
        Pattern p = emptyPattern();
        verseDrums(p);
        // Extra claps: buildup
        p.notes[10][1] = { 1, 60, V_GH };
        p.notes[14][1] = { 1, 60, V_MD };
        p.notes[15][1] = { 1, 60, V_GH };
        // Bass: F → G (dominant tension)
        p.notes[0][3]  = { 3, 41, V_HI };  // F2
        p.notes[4][3]  = { 3, 41, V_MD };
        p.notes[8][3]  = { 3, 43, V_HI };  // G2
        p.notes[12][3] = { 3, 43, V_HI };
        p.notes[14][3] = { 3, 47, V_HI };  // B2 (leading tone!)
        // Melody: "na na na" repeated note build → leap
        p.notes[0][4]  = { 4, 79, V_HI };  // G5
        p.notes[1][4]  = { 4, 79, V_MD };  // G5
        p.notes[2][4]  = { 4, 79, V_HI };  // G5
        p.notes[4][4]  = { 4, 81, V_HI };  // A5
        p.notes[5][4]  = { 4, 81, V_MD };  // A5
        p.notes[6][4]  = { 4, 81, V_HI };  // A5
        p.notes[8][4]  = { 4, 83, V_HI };  // B5
        p.notes[9][4]  = { 4, 83, V_MD };  // B5
        p.notes[10][4] = { 4, 83, V_HI };  // B5
        p.notes[12][4] = { 4, 84, V_HI };  // C6 ← launch!
        p.notes[14][4] = { 4, 84, V_HI };  // C6
        // Pad
        p.notes[0][5]  = { 5, 65, V_LO };  // F4
        p.notes[8][5]  = { 5, 67, V_LO };  // G4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P6: Chorus A — THE hook. Big jump, rhythmic, catchy.
    // Chords: C | G  (I-V)
    // ================================================================
    {
        Pattern p = emptyPattern();
        chorusDrums(p);
        // Bass: driving octave bounce C → G
        p.notes[0][3]  = { 3, 36, V_HI };  // C2
        p.notes[2][3]  = { 3, 48, V_MD };  // C3
        p.notes[4][3]  = { 3, 36, V_HI };
        p.notes[6][3]  = { 3, 48, V_MD };
        p.notes[8][3]  = { 3, 43, V_HI };  // G2
        p.notes[10][3] = { 3, 55, V_MD };  // G3
        p.notes[12][3] = { 3, 43, V_HI };
        p.notes[14][3] = { 3, 55, V_MD };
        // Melody: THE K-pop hook — signature 5th jump, rhythmic
        // G5 G5 — C6! — A5 G5 — E5 G5 —
        p.notes[0][4]  = { 4, 79, V_HI };  // G5
        p.notes[1][4]  = { 4, 79, V_MD };  // G5
        p.notes[3][4]  = { 4, 84, V_HI };  // C6 ★ THE jump
        p.notes[6][4]  = { 4, 81, V_MD };  // A5
        p.notes[8][4]  = { 4, 79, V_HI };  // G5
        p.notes[11][4] = { 4, 76, V_HI };  // E5
        p.notes[13][4] = { 4, 79, V_MD };  // G5
        // Pad: bright
        p.notes[0][5]  = { 5, 64, V_MD };  // E4
        p.notes[8][5]  = { 5, 67, V_MD };  // G4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P7: Chorus B — answer phrase, descends from peak
    // Chords: Am | F
    // ================================================================
    {
        Pattern p = emptyPattern();
        chorusDrums(p);
        // Bass: Am → F bounce
        p.notes[0][3]  = { 3, 45, V_HI };  // A2
        p.notes[2][3]  = { 3, 57, V_MD };  // A3
        p.notes[4][3]  = { 3, 45, V_HI };
        p.notes[6][3]  = { 3, 57, V_MD };
        p.notes[8][3]  = { 3, 41, V_HI };  // F2
        p.notes[10][3] = { 3, 53, V_MD };  // F3
        p.notes[12][3] = { 3, 41, V_HI };
        p.notes[14][3] = { 3, 53, V_MD };
        // Melody: answer — mirror rhythm, descend
        // C6 — A5 G5 — E5 — C5 E5 G5
        p.notes[0][4]  = { 4, 84, V_HI };  // C6
        p.notes[3][4]  = { 4, 81, V_HI };  // A5
        p.notes[4][4]  = { 4, 79, V_MD };  // G5
        p.notes[7][4]  = { 4, 76, V_HI };  // E5
        p.notes[9][4]  = { 4, 72, V_MD };  // C5
        p.notes[11][4] = { 4, 76, V_HI };  // E5
        p.notes[13][4] = { 4, 79, V_HI };  // G5 (pickup for repeat)
        // Pad
        p.notes[0][5]  = { 5, 69, V_MD };  // A4
        p.notes[8][5]  = { 5, 65, V_MD };  // F4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P8: Chorus C — ad-lib/flourish bar, runs + signature jump
    // Chords: F | G
    // ================================================================
    {
        Pattern p = emptyPattern();
        chorusDrums(p);
        // Bass: F → G
        p.notes[0][3]  = { 3, 41, V_HI };  // F2
        p.notes[2][3]  = { 3, 53, V_MD };
        p.notes[4][3]  = { 3, 41, V_HI };
        p.notes[6][3]  = { 3, 53, V_MD };
        p.notes[8][3]  = { 3, 43, V_HI };  // G2
        p.notes[10][3] = { 3, 55, V_MD };
        p.notes[12][3] = { 3, 43, V_HI };
        p.notes[14][3] = { 3, 55, V_MD };
        // Melody: rapid descending run → big jump (K-pop ad-lib energy)
        p.notes[0][4]  = { 4, 84, V_HI };  // C6
        p.notes[1][4]  = { 4, 81, V_MD };  // A5
        p.notes[2][4]  = { 4, 79, V_HI };  // G5
        p.notes[3][4]  = { 4, 76, V_MD };  // E5
        p.notes[4][4]  = { 4, 72, V_MD };  // C5
        p.notes[6][4]  = { 4, 72, V_MD };  // C5
        p.notes[7][4]  = { 4, 76, V_MD };  // E5
        p.notes[8][4]  = { 4, 79, V_HI };  // G5
        p.notes[9][4]  = { 4, 79, V_MD };  // G5
        p.notes[11][4] = { 4, 84, V_HI };  // C6 ★ jump again!
        p.notes[14][4] = { 4, 83, V_MD };  // B5 (leading tone)
        // Pad
        p.notes[0][5]  = { 5, 65, V_MD };  // F4
        p.notes[8][5]  = { 5, 67, V_MD };  // G4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P9: Bridge — contrast section, softer, Am key feel
    // Chords: Am | F
    // ================================================================
    {
        Pattern p = emptyPattern();
        // Sparse kick (half time)
        p.notes[0][0]  = { 0, 48, V_MD };
        p.notes[8][0]  = { 0, 48, V_MD };
        // Soft hihat
        for (int r = 0; r < 16; r += 4)
            p.notes[r][2] = { 2, 80, V_LO };
        // Bass: sustained
        p.notes[0][3]  = { 3, 45, V_MD };  // A2
        p.notes[8][3]  = { 3, 41, V_MD };  // F2
        // Melody: tender, legato feel
        p.notes[0][4]  = { 4, 72, V_MD };  // C5
        p.notes[4][4]  = { 4, 76, V_MD };  // E5
        p.notes[6][4]  = { 4, 74, V_LO };  // D5
        p.notes[8][4]  = { 4, 72, V_MD };  // C5
        p.notes[12][4] = { 4, 69, V_MD };  // A4
        // Pad: warm
        p.notes[0][5]  = { 5, 69, V_MD };  // A4
        p.notes[8][5]  = { 5, 65, V_MD };  // F4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P10: Bridge 2 — builds back toward chorus
    // Chords: Dm | G
    // ================================================================
    {
        Pattern p = emptyPattern();
        // Building drums
        p.notes[0][0]  = { 0, 48, V_MD };
        p.notes[4][0]  = { 0, 48, V_MD };
        p.notes[8][0]  = { 0, 48, V_HI };
        p.notes[12][0] = { 0, 48, V_HI };
        p.notes[12][1] = { 1, 60, V_MD };
        p.notes[14][1] = { 1, 60, V_GH };
        p.notes[15][1] = { 1, 60, V_MD };
        for (int r = 0; r < 16; r += 2)
            p.notes[r][2] = { 2, 80, (r >= 8) ? V_MD : V_LO };
        // Bass: Dm → G (tension)
        p.notes[0][3]  = { 3, 38, V_MD };  // D2
        p.notes[4][3]  = { 3, 38, V_MD };
        p.notes[8][3]  = { 3, 43, V_HI };  // G2
        p.notes[12][3] = { 3, 43, V_HI };
        p.notes[14][3] = { 3, 47, V_HI };  // B2 (leading tone)
        // Melody: rising tension
        p.notes[0][4]  = { 4, 74, V_MD };  // D5
        p.notes[2][4]  = { 4, 77, V_MD };  // F5
        p.notes[4][4]  = { 4, 74, V_MD };  // D5
        p.notes[8][4]  = { 4, 79, V_HI };  // G5
        p.notes[10][4] = { 4, 81, V_HI };  // A5
        p.notes[12][4] = { 4, 83, V_HI };  // B5
        p.notes[14][4] = { 4, 84, V_HI };  // C6 → back to chorus!
        // Pad
        p.notes[0][5]  = { 5, 62, V_MD };  // D4
        p.notes[8][5]  = { 5, 67, V_MD };  // G4
        m_patterns.push_back(p);
    }

    // ================================================================
    // P11: Outro / loop prep — energetic, smooth transition to bar 1
    // Chords: C | G
    // ================================================================
    {
        Pattern p = emptyPattern();
        chorusDrums(p);
        // Bass
        p.notes[0][3]  = { 3, 36, V_HI };  // C2
        p.notes[4][3]  = { 3, 48, V_MD };
        p.notes[8][3]  = { 3, 43, V_HI };  // G2
        p.notes[12][3] = { 3, 55, V_MD };
        // Melody: call-back to intro teaser, lands on G5 ready to resolve to C
        p.notes[0][4]  = { 4, 84, V_HI };  // C6
        p.notes[2][4]  = { 4, 79, V_MD };  // G5
        p.notes[4][4]  = { 4, 76, V_MD };  // E5
        p.notes[6][4]  = { 4, 72, V_MD };  // C5
        p.notes[8][4]  = { 4, 76, V_HI };  // E5
        p.notes[10][4] = { 4, 79, V_HI };  // G5
        // Pad
        p.notes[0][5]  = { 5, 64, V_MD };  // E4
        m_patterns.push_back(p);
    }

    // ================================================================
    // Order list: 32 bars = ~64 seconds
    //
    // Intro (2) → Verse 1 (4) → Chorus 1 (4) →
    // Verse 2 (4) → Chorus 2 (4) →
    // Bridge (4) → Chorus 3 (4) → Outro (4) → Buildup-to-loop (2)
    // ================================================================
    m_orderList = {
        0, 1,                 // Intro: teaser + beat drop (2 bars)
        2, 3, 4, 5,           // Verse 1: call, response, energy build, pre-chorus (4 bars)
        6, 7, 8, 6,           // Chorus 1: hook, response, flourish, hook again (4 bars)
        2, 3, 4, 5,           // Verse 2: same structure, familiarity (4 bars)
        6, 7, 8, 7,           // Chorus 2: hook, response, flourish, extra response (4 bars)
        9, 9, 10, 10,         // Bridge: contrast, builds back (4 bars)
        6, 7, 8, 6,           // Chorus 3: final hook payoff (4 bars)
        11, 7, 11, 8          // Outro: callbacks + smooth loop-back (4 bars)
    };
}

// ============================================================
// Play a note on the next available voice
// ============================================================
void ModMusic::PlayNote(int instrument, int note, int velocity)
{
    if (instrument < 0 || instrument >= NUM_INSTRUMENTS) return;
    if (!m_voices[m_nextVoice]) return;

    auto* voice = m_voices[m_nextVoice];
    m_nextVoice = (m_nextVoice + 1) % NUM_CHANNELS;

    // Stop any currently playing sound on this voice
    voice->Stop();
    voice->FlushSourceBuffers();

    // Set pitch via frequency ratio
    float targetFreq = NoteToFreq(note);
    float ratio = targetFreq / m_instruments[instrument].baseFreq;
    voice->SetFrequencyRatio(ratio);

    // Set volume from velocity
    float vol = static_cast<float>(velocity) / 255.0f;
    voice->SetVolume(vol);

    // Submit the sample buffer
    voice->SubmitSourceBuffer(&m_xaBuffers[instrument]);
    voice->Start();
}

// ============================================================
// Sequencer update — called every frame
// ============================================================
void ModMusic::Update(double elapsedSeconds)
{
    if (!m_xaudio2 || m_orderList.empty()) return;

    m_accumulator += elapsedSeconds;

    while (m_accumulator >= SECONDS_PER_ROW)
    {
        m_accumulator -= SECONDS_PER_ROW;

        // Get current pattern
        int patIdx = m_orderList[m_currentOrder];
        if (patIdx < 0 || patIdx >= static_cast<int>(m_patterns.size()))
        {
            m_currentOrder = 0;
            patIdx = m_orderList[0];
        }
        const Pattern& pat = m_patterns[patIdx];

        // Play all notes on this row
        for (int t = 0; t < NUM_TRACKS; t++)
        {
            const NoteEvent& ev = pat.notes[m_currentRow][t];
            if (ev.instrument != 0xFF)
            {
                PlayNote(ev.instrument, ev.note, ev.velocity);
            }
        }

        // Advance
        m_currentRow++;
        if (m_currentRow >= PATTERN_ROWS)
        {
            m_currentRow = 0;
            m_currentOrder++;
            if (m_currentOrder >= static_cast<int>(m_orderList.size()))
            {
                m_currentOrder = 0; // loop
            }
        }
    }
}

void ModMusic::Suspend()
{
    if (m_xaudio2)
        m_xaudio2->StopEngine();
}

void ModMusic::Resume()
{
    if (m_xaudio2)
        m_xaudio2->StartEngine();
}
