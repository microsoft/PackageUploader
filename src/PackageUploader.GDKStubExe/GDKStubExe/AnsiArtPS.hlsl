// ANSI-art animation - extended block elements (quadrant + fractional).
// 145 frames, 80x45 cells, 32 colors.
// Compile: fxc /T ps_5_0 /E main /Fh AnsiArtPS.h /Vn g_AnsiArtPS /O3 AnsiArtPS.hlsl

cbuffer Constants : register(b0) { float g_time; };
StructuredBuffer<uint> g_cellData : register(t0);

static const uint CELLS_X    = 80;
static const uint CELLS_Y    = 45;
static const uint NUM_FRAMES = 145;
static const float FPS       = 24.0;

static const float3 g_palette[32] = {
    float3(0.000000, 0.000000, 0.000000),
    float3(0.116903, 0.785869, 0.258257),
    float3(0.021753, 0.492024, 0.110108),
    float3(0.012316, 0.174124, 0.104979),
    float3(0.013968, 0.320926, 0.098605),
    float3(0.427904, 0.955230, 0.554622),
    float3(0.654985, 0.938833, 0.721584),
    float3(0.014004, 0.124370, 0.077819),
    float3(0.954697, 0.978872, 0.962231),
    float3(0.151068, 0.904578, 0.289938),
    float3(0.028009, 0.566807, 0.124208),
    float3(0.012497, 0.278381, 0.100759),
    float3(0.011864, 0.246507, 0.102118),
    float3(0.218971, 0.585530, 0.300700),
    float3(0.015785, 0.367748, 0.098280),
    float3(0.098069, 0.098869, 0.098709),
    float3(0.479826, 0.556599, 0.502133),
    float3(0.011625, 0.197088, 0.099999),
    float3(0.017988, 0.424582, 0.101843),
    float3(0.011869, 0.108517, 0.068469),
    float3(0.069799, 0.832592, 0.192795),
    float3(0.037577, 0.644389, 0.145533),
    float3(0.013287, 0.178886, 0.090254),
    float3(0.012958, 0.141365, 0.083420),
    float3(0.053284, 0.724275, 0.173440),
    float3(0.012371, 0.219852, 0.103234),
    float3(0.305570, 0.333732, 0.311553),
    float3(0.025884, 0.025542, 0.025921),
    float3(0.017626, 0.086011, 0.058065),
    float3(0.013935, 0.158057, 0.090134),
    float3(0.180111, 0.197009, 0.183232),
    float3(0.259587, 0.931374, 0.407090)
};

float4 main(float4 pixPos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    uint frameIdx = uint(fmod(g_time * FPS, float(NUM_FRAMES)));

    float2 cellPos = uv * float2(CELLS_X, CELLS_Y);
    uint cx = min(uint(cellPos.x), CELLS_X - 1u);
    uint cy = min(uint(cellPos.y), CELLS_Y - 1u);
    float2 cellUV = frac(cellPos);

    // Unpack: char(6) | fg(5) | bg(5)
    uint cellIdx = frameIdx * (CELLS_X * CELLS_Y) + cy * CELLS_X + cx;
    uint packedPair = g_cellData[cellIdx / 2u];
    uint bits = (cellIdx & 1u) ? (packedPair >> 16u) : (packedPair & 0xFFFFu);
    uint charType = bits & 0x3Fu;
    uint fgIdx    = (bits >> 6u) & 0x1Fu;
    uint bgIdx    = (bits >> 11u) & 0x1Fu;

    float3 fg = g_palette[fgIdx];
    float3 bg = g_palette[bgIdx];

    // Determine coverage based on block character type
    float3 color;
    if (charType == 0u) {
        color = bg;
    } else if (charType == 1u) {
        color = fg;
    } else if (charType <= 8u) {
        float threshold = 1.0 - float(charType - 1u) / 8.0;
        color = (cellUV.y >= threshold) ? fg : bg;
    } else if (charType <= 15u) {
        float threshold = float(charType - 8u) / 8.0;
        color = (cellUV.x < threshold) ? fg : bg;
    } else if (charType <= 29u) {
        uint pattern = charType - 15u;
        bool isL = cellUV.x < 0.5;
        bool isT = cellUV.y < 0.5;
        uint quadrant = isT ? (isL ? 3u : 2u) : (isL ? 1u : 0u);
        color = ((pattern >> quadrant) & 1u) ? fg : bg;
    } else {
        color = bg;
    }

    return float4(max(color, 0.0), 1.0);
}
