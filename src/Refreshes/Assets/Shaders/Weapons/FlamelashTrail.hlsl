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
    
    float noise = tex2D(tex0, float2(uv.x / 4 - uTime * 15, uv.y / 5)).r;
    
    float fuelWave = sin((pow(uv.x * 1.1f + noise * 0.4, 2) - uTime * 120) * 6.2832) * 0.5f + 0.5f;
    
    float trail = tex2D(uImage0, float2(frac(uv.x - uTime * 30), uv.y)).r * (1 - abs(1 - uv.y * 2));
    
    float lowHeat = trail * (fuelWave + 1.1 - uv.x * 2);
    float highHeat = trail * (fuelWave + 1 - uv.x * 3);
    float4 trailColor = input.Color + (uGlowColor + smoothstep(0.2f, 0, uv.x) * 0.1f) * smoothstep(0.2f, 1.0f, trail * highHeat) * smoothstep(0.47f, 0.5f, highHeat);

    return (smoothstep(0.0f, 0.35f, lowHeat * trail) + highHeat) * trailColor * 1.2f + smoothstep(0.5f, 0.6f, highHeat) * uGlowColor;

}

technique Technique1
{
    pass FlamelashPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}