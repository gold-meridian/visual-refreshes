sampler uImage0 : register(s0);

matrix uTransformMatrix;

float uSplitProgressStart;
float uSplitProgressEnd;

float uSplitWidthProgressStart;
float uSplitWidthProgressEnd;
float uSplitWidthStart;
float uSplitWidthEnd;

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

float inverselerp(float x, float y, float s)
{
    return (s - x) / (y - x);
}

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
    float2 normalizedUv = uv * 2.0 - 1.0;

    float splitProgress = saturate(inverselerp(uSplitProgressStart, uSplitProgressEnd, uv.x));
	float splitWidthProgress = saturate(inverselerp(uSplitWidthProgressStart, uSplitWidthProgressEnd, uv.x));
    float splitWidth = lerp(0.0, lerp(uSplitWidthStart, uSplitWidthEnd, splitWidthProgress), splitProgress);
    if (abs(normalizedUv.y) < splitWidth)
        return 0.0;
    return input.Color;
}

technique Technique1
{
    pass BasicTrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}