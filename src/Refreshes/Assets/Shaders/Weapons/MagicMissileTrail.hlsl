#include "../tmlbuild.h"

sampler uImage0 : register(s0);

texture uTexture0;
sampler2D tex0 = sampler_state
{
    texture = <uTexture0>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};

float uTime;
float uWidth;
float4 uGlowColor;
matrix uTransformMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.Position = mul(input.Position, uTransformMatrix);
    output.Color = input.Color;
    output.Coord = input.Coord;
    return output;
}

float4 PixelShaderFunction(in VertexShaderOutput input) : COLOR0
{
    float2 uv = input.Coord;
    uv.y = (input.Coord.y - 0.5) / input.Coord.z + 0.5;

    uv.y = floor(uv.y * uWidth) / uWidth;
    uv.x = floor(uv.x * 256) / 256;

    float beam = 1 - abs(1 - uv.y * 2);
    float noise = pow(tex2D(tex0, float2(uv.x * uv.x - uTime * 25, uv.y / 8)) + 0.33f, 3);
    
    float highEnergy = smoothstep(0.6f, 0.7f, beam * (1 - uv.x));
    float glow = pow(beam * beam * (noise + 0.5f) * sqrt(1 - uv.x) * 1.2f, 2) * sqrt(1 - uv.x);
    return (saturate(glow) + pow(beam * (1 - uv.x), 2)) * input.Color + highEnergy * lerp(input.Color, uGlowColor, highEnergy + 0.5f);
}

technique Technique1
{
    pass MagicMissilePass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}