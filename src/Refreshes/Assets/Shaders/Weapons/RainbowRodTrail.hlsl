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

float uTime GLOBAL_TIME;
float uWidth;
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
    float2 uv = input.Coord.xy;
    uv.y = (input.Coord.y - 0.5) / input.Coord.z + 0.5;
    
    uv.y = floor(uv.y * uWidth) / uWidth;
    uv.x = floor(uv.x * 256) / 256;
    
    float noise = tex2D(tex0, float2(uv.x * 2.5 - uTime * 3, uv.y / 5)).r * sin(uv.y * 3.1415);
    float noise2 = (1.0f - tex2D(tex0, float2(uv.x * 2 - uTime * 2, uv.y / 3 + 0.3f)).r) * sqrt(sin(uv.y * 3.1415)) - noise * 0.65f * (1 - uv.x);

    float color = smoothstep(0.2, 0.4f, pow(noise2 + 0.4 - uv.x, 2 + uv.x * 4));
    color = saturate(color + 0.7f * pow(noise2 * (1 - uv.x), 2));
    float bright = pow(saturate(noise2 + 0.1f - uv.x), 1.5 + uv.x * 3);
    
    return smoothstep(0.9, 0.1, bright + uv.x - 0.5f) * smoothstep(0.25, 0.3, bright) + color * input.Color;
}

technique Technique1
{
    pass RainbowRodPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}