//
// ModMusic.h - MOD/S3M-style procedural music engine using XAudio2
//

#pragma once

#include "pch.h"

struct Instrument
{
    std::vector<int16_t> pcm;   // mono 16-bit PCM at 44100 Hz
    float                baseFreq; // frequency the sample was generated at
};

// A single note event in a pattern
struct NoteEvent
{
    uint8_t instrument; // instrument index (0-5), 0xFF = no note
    uint8_t note;       // MIDI note number (0 = C0, 60 = C4)
    uint8_t velocity;   // 0-255 volume
};

class ModMusic
{
public:
    ModMusic() noexcept;
    ~ModMusic();

    ModMusic(ModMusic&&) = delete;
    ModMusic& operator=(ModMusic&&) = delete;
    ModMusic(const ModMusic&) = delete;
    ModMusic& operator=(const ModMusic&) = delete;

    void Initialize();
    void Update(double elapsedSeconds);
    void Suspend();
    void Resume();

private:
    static constexpr int   SAMPLE_RATE = 44100;
    static constexpr int   NUM_INSTRUMENTS = 8;
    static constexpr int   NUM_CHANNELS = 14;  // polyphony
    static constexpr int   BPM = 120;
    static constexpr int   ROWS_PER_BEAT = 4;  // 16th note resolution
    static constexpr float SECONDS_PER_ROW = 60.0f / (BPM * ROWS_PER_BEAT);
    static constexpr int   PATTERN_ROWS = 16;  // rows per pattern
    static constexpr int   NUM_TRACKS = 8;     // parallel tracks

    void SynthesizeInstruments();
    void BuildSong();
    void PlayNote(int instrument, int note, int velocity);

    // XAudio2
    IXAudio2*               m_xaudio2;
    IXAudio2MasteringVoice* m_masterVoice;
    IXAudio2SourceVoice*    m_voices[NUM_CHANNELS];
    int                     m_nextVoice; // round-robin voice allocator

    // Instruments
    Instrument m_instruments[NUM_INSTRUMENTS]{};
    XAUDIO2_BUFFER m_xaBuffers[NUM_INSTRUMENTS]{};

    // Sequencer state
    double m_accumulator;   // time accumulator
    int    m_currentRow;    // current row in current pattern
    int    m_currentOrder;  // current position in order list

    // Song data: order list and patterns
    // Each pattern is PATTERN_ROWS rows × NUM_TRACKS tracks of NoteEvents
    struct Pattern
    {
        NoteEvent notes[16][8]; // [row][track]
    };

    std::vector<int>     m_orderList;   // pattern indices
    std::vector<Pattern> m_patterns;
};
