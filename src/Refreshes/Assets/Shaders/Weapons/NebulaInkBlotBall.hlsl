float2 uImageSize;
float uScale;

sampler uImage0 : register(s0);

float4 main(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 pixel = round(uv * uImageSize / 2.0) * 2.0 / uImageSize;
    
    float size = length(pixel - 0.5);
    float distance = size / uScale;
    float3 lightPos = (1, 1, 0);
    
    if (size > uScale)
    {
        return 0;
    }
    
    if (size <= uScale - 4 / uImageSize.x)
    {   
        return lerp(float4(0, 0, 0.2, 1), sampleColor, distance * distance);
    }
    else
    {
        if (size > (uScale - 2 / uImageSize.x))
        {
            return float4(0, 0, 1, 1);
        }
        else
        {
            return float4(1, 1, 1, 1);
        }
    }
    
    return 0;
}

#ifdef FX
technique Technique1
{
    pass ShaderPass
    {
        PixelShader = compile ps_3_0 main();
    }
}
#endif // FX
