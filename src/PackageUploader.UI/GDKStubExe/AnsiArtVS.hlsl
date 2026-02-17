// Fullscreen triangle vertex shader — no vertex buffer needed.
// Compile: fxc /T vs_5_0 /E main /Fh AnsiArtVS.h /Vn g_AnsiArtVS /O3 AnsiArtVS.hlsl

void main(uint id : SV_VertexID,
          out float4 pos : SV_Position,
          out float2 uv  : TEXCOORD0)
{
    uv  = float2((id << 1) & 2, id & 2);
    pos = float4(uv * float2(2, -2) + float2(-1, 1), 0, 1);
}
