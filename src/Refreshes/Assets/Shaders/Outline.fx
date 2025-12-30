sampler2D uImage0 : register(s0);
float2 uTexelSize;
float4 uOutlineColor;

float4 MainPS(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0{
    float4 sample = tex2D(uImage0, uv);
    
    if (sample.a > 0) {
        return sample * color;
    }

    float a = 0;
    a += tex2D(uImage0, uv + float2(uTexelSize.x, 0)).a;
    a += tex2D(uImage0, uv + float2(-uTexelSize.x, 0)).a;
    a += tex2D(uImage0, uv + float2(0, uTexelSize.y)).a;
    a += tex2D(uImage0, uv + float2(0, -uTexelSize.y)).a;
	float maxA = 0;
	maxA = max(maxA, tex2D(uImage0, uv + float2(uTexelSize.x, 0)).a);
	maxA = max(maxA, tex2D(uImage0, uv + float2(-uTexelSize.x, 0)).a);
	maxA = max(maxA, tex2D(uImage0, uv + float2(0, uTexelSize.y)).a);
	maxA = max(maxA, tex2D(uImage0, uv + float2(0, -uTexelSize.y)).a);

    if (a > 0) {
        return uOutlineColor * color.a * maxA;
    }

    return float4(0, 0, 0, 0);
}

technique Technique1 {
    pass OutlinePass {
        PixelShader = compile ps_2_0 MainPS();
    }
}