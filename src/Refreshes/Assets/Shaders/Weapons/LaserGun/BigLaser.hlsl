sampler uImage0 : register(s0);

texture uTexture0;
sampler2D tex0 = sampler_state
{
    texture = <uTexture0>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float uTime;
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

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input){
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.Position = mul(input.Position, uTransformMatrix);
    output.Color = input.Color;
    output.Coord = input.Coord;
    return output;
}

float4 PixelShaderFunction(in VertexShaderOutput input) : COLOR0{
    float2 uv = input.Coord.xy;
    uv.y = (input.Coord.y - 0.5) / input.Coord.z + 0.5;

    float4 trail = tex2D(uImage0, float2(frac(uv.x - uTime * 30), uv.y));
    // float noise = tex2D(tex0, float2(frac(uv.x - uTime * 30), uv.y / 2)).r - pow(uv.x, 1.5f) * 1.2f + 0.5f;
    float noise = 0.5f;

    float4 trailColor = input.Color + (uGlowColor + smoothstep(0.2f, 0, uv.x) * 0.2f) * smoothstep(0.7f, 1.0f, trail.r) * smoothstep(0.4f, 0.7f, noise);
    
    float4 finalColor = trail.r * input.Color * 0.5f * noise + length(trail.rgb) * trailColor * smoothstep(0.1f, 0.25f, noise);

    float distanceFromCenter = abs(uv.y - 0.5f);
    float innerGlow = smoothstep(0.03f, 0.0f, distanceFromCenter) * 5.0f;
    finalColor += float4(1.0f, 1.0f, 1.0f, 1.0f) * innerGlow;
    
    return finalColor;
}

technique Technique1{
    pass BigLaserPass{
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}