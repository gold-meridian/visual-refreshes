sampler2D uImage0 : register(s0);

texture uTexture0;
sampler2D texture0 = sampler_state
{
    texture = <uTexture0>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};

float uTime;

float4 uMultColor;
float4 uAddColor;
float uColorAmount;

float hash(float2 p)
{
    float3 p3  = frac(float3(p.xyx) * .1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// https://www.shadertoy.com/view/Msf3WH
float noise(float2 p)
{
    const float K1 = 0.366025404; // (sqrt(3)-1)/2;
    const float K2 = 0.211324865; // (3-sqrt(3))/6;

    float2  i = floor( p + (p.x+p.y)*K1 );
    float2  a = p - i + (i.x+i.y)*K2;
    float m = step(a.y,a.x); 
    float2  o = float2(m,1.0-m);
    float2  b = a - o + K2;
    float2  c = a - 1.0 + 2.0*K2;
    float3  h = max( 0.5-float3(dot(a,a), dot(b,b), dot(c,c) ), 0.0 );
    float3  n = h*h*h*h*float3( dot(a,hash(i+0.0)), dot(b,hash(i+o)), dot(c,hash(i+1.0)));
    return dot( n, float3(70.0, 70.0, 70.0) );
}

float fbm3(float3 v) {
    float result = noise(v);
    result += noise(v * 2.) / 2.;
    result += noise(v * 4.) / 4.;
    result /= (1. + 1./2. + 1./4.);
    return result;
}

float4 MainPS(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0{
    float4 sample = tex2D(uImage0, uv);
    
	float4 finalColor = 0;
	// some bullshit
	float2 offset = float2(fbm3(uv.yxx), 0.0);
	float4 noise = tex2D(texture0, offset + float2(uTime, uTime));
	// fbm-esque stuff
    for (int i = 0; i < 4; i++)
    {
        finalColor += noise / (pow(2.0, i)) * uMultColor;
    }
    finalColor /= (1. + 1./2. + 1./4);
	
	finalColor = finalColor + uAddColor;
	finalColor = floor(finalColor * 4.0) / 4.0;

    return float4(finalColor.rgb, 1.0) * sample.a;
}

technique Technique1 {
    pass OutlinePass {
        PixelShader = compile ps_3_0 MainPS();
    }
}